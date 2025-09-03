// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

#pragma warning disable CA1031, CA1032, CA1064, CA2000
#pragma warning disable MA0042, MA0048, MA0051, MA0071, MA0100, MA0134
#pragma warning disable S3218, S3236, S3871, S4070

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace TBC.Common.Configuration.Registry;

#if NET
[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage(Justification = "See the unit tests of https://github.com/microsoft/vs-threading library")]
#else
[ExcludeFromCodeCoverage]
#endif
internal static partial class RegistryKeyExtensions
{
    /// <summary>
    /// Returns a Task that completes when the specified registry key changes.
    /// </summary>
    /// <param name="registryKey">The registry key to watch for changes.</param>
    /// <param name="watchSubtree">
    /// <see langword="true"/> to watch the keys descendent keys as well; <see langword="false"/> to watch only this key without descendents.
    /// </param>
    /// <param name="change">Indicates the kinds of changes to watch for.</param>
    /// <param name="cancellationToken">
    /// A token that may be canceled to release the resources from watching for changes and complete the returned Task as canceled.
    /// </param>
    /// <returns>
    /// A task that completes when the registry key changes, the handle is closed, or upon cancellation.
    /// </returns>
    public static Task WaitForChangeAsync(this RegistryKey registryKey, bool watchSubtree = true, RegistryChangeNotificationFilters change = RegistryChangeNotificationFilters.Value | RegistryChangeNotificationFilters.Subkey, CancellationToken cancellationToken = default)
    {
        EnsureSupported();

#if NET
        ArgumentNullException.ThrowIfNull(registryKey);
#else
        _ = registryKey ?? throw new ArgumentNullException(nameof(registryKey));
#endif

        return WaitForRegistryChangeAsync(registryKey.Handle, watchSubtree, change, cancellationToken);
    }

    /// <summary>
    /// Returns a Task that completes when the specified registry key changes.
    /// </summary>
    /// <param name="registryKeyHandle">The handle to the open registry key to watch for changes.</param>
    /// <param name="watchSubtree">
    /// <see langword="true"/> to watch the keys descendent keys as well; <see langword="false"/> to watch only this key without descendents.
    /// </param>
    /// <param name="change">Indicates the kinds of changes to watch for.</param>
    /// <param name="cancellationToken">
    /// A token that may be canceled to release the resources from watching for changes and complete the returned Task as canceled.
    /// </param>
    /// <returns>
    /// A task that completes when the registry key changes, the handle is closed, or upon cancellation.
    /// </returns>
    private static async Task WaitForRegistryChangeAsync(SafeRegistryHandle registryKeyHandle, bool watchSubtree, RegistryChangeNotificationFilters change, CancellationToken cancellationToken)
    {
        IDisposable? dedicatedThreadReleaser = null;
        try
        {
            using ManualResetEvent evt = new(initialState: false);
            REG_NOTIFY_FILTER dwNotifyFilter = (REG_NOTIFY_FILTER)change;

            static void DoNotify(SafeRegistryHandle registryKeyHandle, bool watchSubtree, REG_NOTIFY_FILTER change, WaitHandle evt)
            {
                var win32Error = Interop.AdvApi32.RegNotifyChangeKeyValue(
                    registryKeyHandle,
                    watchSubtree,
                    change,
                    evt.SafeWaitHandle,
                    fAsynchronous: true);
                if (win32Error != 0)
                {
                    throw new Win32Exception(win32Error);
                }
            }

            if (LightUps.IsWindows8OrLater)
            {
                dwNotifyFilter |= REG_NOTIFY_FILTER.REG_NOTIFY_THREAD_AGNOSTIC;
                DoNotify(registryKeyHandle, watchSubtree, dwNotifyFilter, evt);
            }
            else
            {
                // Engage our downlevel support by using a single, dedicated thread to guarantee
                // that we request notification on a thread that will not be destroyed later.
                // Although we *could* await this, we synchronously block because our caller expects
                // subscription to have begun before we return: for the async part to simply be notification.
                // This async method we're calling uses .ConfigureAwait(false) internally so this won't
                // deadlock if we're called on a thread with a single-thread SynchronizationContext.
                void registerAction() => DoNotify(registryKeyHandle, watchSubtree, dwNotifyFilter, evt);
                dedicatedThreadReleaser = DownlevelRegistryWatcherSupport.ExecuteOnDedicatedThreadAsync(registerAction).GetAwaiter().GetResult();
            }

            await evt.ToTask(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            dedicatedThreadReleaser?.Dispose();
        }
    }

    private static void EnsureSupported()
    {
#if NET
        if (!OperatingSystem.IsWindowsVersionAtLeast(7))
#else
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
        {
            throw new PlatformNotSupportedException();
        }
    }

    /// <summary>
    /// A completed task with a <see langword="true"/> result.
    /// </summary>
    public static readonly Task<bool> TrueTask = Task.FromResult(true);

    /// <summary>
    /// A completed task with a <see langword="false"/> result.
    /// </summary>
    public static readonly Task<bool> FalseTask = Task.FromResult(false);

    /// <summary>
    /// Creates a TPL Task that returns <see langword="true"/> when a <see cref="WaitHandle"/> is signaled
    /// or returns <see langword="false"/> if a timeout occurs first.
    /// </summary>
    /// <param name="handle">
    /// The handle whose signal triggers the task to be completed. Do not use a <see cref="Mutex"/> here.
    /// </param>
    /// <param name="timeout">
    /// The timeout (in milliseconds) after which the task will return <see langword="false"/> if the handle is not signaled by that time.
    /// </param>
    /// <param name="cancellationToken">
    /// A token whose cancellation will cause the returned Task to immediately complete in a canceled state.
    /// </param>
    /// <returns>
    /// A Task that completes when the handle is signaled or times out, or when the caller's cancellation token is canceled.
    /// If the task completes because the handle is signaled, the task's result is <see langword="true"/>.
    /// If the task completes because the handle is not signaled prior to the timeout, the task's result is <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The completion of the returned task is asynchronous with respect to the code that actually signals the wait handle.
    /// </remarks>
    internal static Task<bool> ToTask(this WaitHandle handle, int timeout = Timeout.Infinite, CancellationToken cancellationToken = default)
    {
#if NET
        ArgumentNullException.ThrowIfNull(handle);
#else
        _ = handle ?? throw new ArgumentNullException(nameof(handle));
#endif

        // Check whether the handle is already signaled as an optimization.
        // But even for WaitOne(0) the CLR can pump messages if called on the UI thread, which the caller may not
        // be expecting at this time, so be sure there is no message pump active by controlling the SynchronizationContext.
        using (NoMessagePumpSyncContext.Default.Apply())
        {
            if (handle.WaitOne(0))
            {
                return TrueTask;
            }
            else if (timeout == 0)
            {
                return FalseTask;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var tcs = new TaskCompletionSource<bool>();

        RegisteredWaitHandle callbackHandle = ThreadPool.RegisterWaitForSingleObject(
            handle,
            static (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut),
            state: tcs,
            millisecondsTimeOutInterval: timeout,
            executeOnlyOnce: true);

        if (cancellationToken.CanBeCanceled)
        {
            // Arrange that if the caller signals their cancellation token that we complete the task
            // we return immediately. Because of the continuation we've scheduled on that task, this
            // will automatically release the wait handle notification as well.
            CancellationTokenRegistration cancellationRegistration =
                cancellationToken.Register(
                    static state =>
                    {
                        var tuple = (Tuple<TaskCompletionSource<bool>, CancellationToken>)state!;
                        tuple.Item1.TrySetCanceled(tuple.Item2);
                    },
                    Tuple.Create(tcs, cancellationToken));

            // We have a cancellation token registration and a wait handle registration to release.
            // Each time this code executes, allocate one tuple as a state object to reduce from allocating an implicit closure *and* a delegate.
            _ = tcs.Task.ContinueWith(
                static (_, state) =>
                {
                    var tuple = (Tuple<RegisteredWaitHandle, CancellationTokenRegistration>)state!;
                    tuple.Item1.Unregister(waitObject: null); // release resources for the async callback
                    tuple.Item2.Dispose(); // release memory for cancellation token registration
                },
                Tuple.Create(callbackHandle, cancellationRegistration),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else
        {
            // Since the cancellation token was the default one, the only thing we need to track is clearing the RegisteredWaitHandle,
            // so do this such that we allocate as few objects as possible.
            _ = tcs.Task.ContinueWith(
                static (_, state) => ((RegisteredWaitHandle)state!).Unregister(waitObject: null),
                callbackHandle,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        return tcs.Task;
    }

    /// <summary>
    /// Applies the specified <see cref="SynchronizationContext"/> to the caller's context.
    /// </summary>
    /// <param name="syncContext">The synchronization context to apply.</param>
    /// <param name="checkForChangesOnRevert">
    /// A value indicating whether to check that the applied SyncContext is still the current one when the original is restored.
    /// </param>
    internal static SpecializedSyncContext Apply(this SynchronizationContext? syncContext, bool checkForChangesOnRevert = true)
    {
        return SpecializedSyncContext.Apply(syncContext, checkForChangesOnRevert);
    }

    /// <summary>
    /// A SynchronizationContext whose synchronously blocking Wait method does not allow any reentrancy via the message pump.
    /// </summary>
    private sealed class NoMessagePumpSyncContext : SynchronizationContext
    {
        /// <summary>
        /// A shared singleton.
        /// </summary>
        private static readonly NoMessagePumpSyncContext DefaultInstance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="NoMessagePumpSyncContext"/> class.
        /// </summary>
        public NoMessagePumpSyncContext()
        {
            // This is required so that our override of Wait is invoked.
            this.SetWaitNotificationRequired();
        }

        /// <summary>
        /// Gets a shared instance of this class.
        /// </summary>
        public static SynchronizationContext Default
        {
            get { return DefaultInstance; }
        }

        /// <summary>
        /// Synchronously blocks without a message pump.
        /// </summary>
        /// <param name="waitHandles">
        /// An array of type <see cref="IntPtr"/> that contains the native operating system handles.
        /// </param>
        /// <param name="waitAll">true to wait for all handles; false to wait for any handle.</param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/> (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// The array index of the object that satisfied the wait.
        /// </returns>
        public override unsafe int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
#if NET
            ArgumentNullException.ThrowIfNull(waitHandles);
#else
            _ = waitHandles ?? throw new ArgumentNullException(nameof(waitHandles));
#endif

            // On .NET Framework we must take special care to NOT end up in a call to CoWait (which lets in RPC calls).
            // Off Windows, we can't p/invoke to kernel32, but it appears that .NET Core never calls CoWait, so we can rely on default behavior.
            // We're just going to use the OS as the switch instead of the framework so that (one day) if we drop our .NET Framework specific target,
            // and if .NET Core ever adds CoWait support on Windows, we'll still behave properly.
#if NET
            if (OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
            {
                fixed (IntPtr* pHandles = waitHandles)
                {
                    return (int)Interop.Kernel32.WaitForMultipleObjects((uint)waitHandles.Length, pHandles, waitAll, (uint)millisecondsTimeout);
                }
            }

            return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
        }
    }

    /// <summary>
    /// A structure that applies and reverts changes to the <see cref="SynchronizationContext"/>.
    /// </summary>
    internal readonly struct SpecializedSyncContext : IDisposable
    {
        /// <summary>
        /// A flag indicating whether the non-default constructor was invoked.
        /// </summary>
        private readonly bool initialized;

        /// <summary>
        /// The SynchronizationContext to restore when <see cref="Dispose"/> is invoked.
        /// </summary>
        private readonly SynchronizationContext? prior;

        /// <summary>
        /// The SynchronizationContext applied when this struct was constructed.
        /// </summary>
        private readonly SynchronizationContext? appliedContext;

        /// <summary>
        /// A value indicating whether to check that the applied SyncContext is still the current one when the original is restored.
        /// </summary>
        private readonly bool checkForChangesOnRevert;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecializedSyncContext"/> struct.
        /// </summary>
        private SpecializedSyncContext(SynchronizationContext? syncContext, bool checkForChangesOnRevert)
        {
            this.initialized = true;
            this.prior = SynchronizationContext.Current;
            this.appliedContext = syncContext;
            this.checkForChangesOnRevert = checkForChangesOnRevert;
            SynchronizationContext.SetSynchronizationContext(syncContext);
        }

        /// <summary>
        /// Applies the specified <see cref="SynchronizationContext"/> to the caller's context.
        /// </summary>
        /// <param name="syncContext">The synchronization context to apply.</param>
        /// <param name="checkForChangesOnRevert">
        /// A value indicating whether to check that the applied SyncContext is still the current one when the original is restored.
        /// </param>
        public static SpecializedSyncContext Apply(SynchronizationContext? syncContext, bool checkForChangesOnRevert = true)
        {
            return new SpecializedSyncContext(syncContext, checkForChangesOnRevert);
        }

        /// <summary>
        /// Reverts the SynchronizationContext to its previous instance.
        /// </summary>
        public void Dispose()
        {
            if (this.initialized)
            {
                if (this.checkForChangesOnRevert && SynchronizationContext.Current != this.appliedContext)
                {
                    const string message = "A recoverable error has been detected.";
                    Debug.WriteLine(message);
                    Debug.Assert(condition: false, message);
                }

                SynchronizationContext.SetSynchronizationContext(this.prior);
            }
        }
    }

    /// <summary>
    /// Provides a dedicated thread for requesting registry change notifications.
    /// </summary>
    /// <remarks>
    /// For versions of Windows prior to Windows 8, requesting registry change notifications
    /// required that the thread that made the request remain alive or else the watcher would
    /// simply signal the event and stop watching for changes.
    /// This class provides a single, dedicated thread for requesting such notifications
    /// so that they don't get canceled when a thread happens to exit.
    /// The dedicated thread is released when no one is watching the registry any more.
    /// </remarks>
    private static class DownlevelRegistryWatcherSupport
    {
        /// <summary>
        /// The size of the stack allocated for a thread that expects to stay within just a few methods in depth.
        /// </summary>
        /// <remarks>
        /// The default stack size for a thread is 1MB.
        /// </remarks>
        private const int SmallThreadStackSize = 100 * 1024;

        /// <summary>
        /// The object to lock when accessing any fields.
        /// This is also the object that is waited on by the dedicated thread,
        /// and may be pulsed by others to wake the dedicated thread to do some work.
        /// </summary>
        private static readonly object SyncObject = new();

        /// <summary>
        /// A queue of actions the dedicated thread should take.
        /// </summary>
        private static readonly Queue<Tuple<Action, TaskCompletionSource<EmptyStruct>>> PendingWork = new();

        /// <summary>
        /// The number of callers that still have an interest in the survival of the dedicated thread.
        /// The dedicated thread will exit when this value reaches 0.
        /// </summary>
        private static int keepAliveCount;

        /// <summary>
        /// The thread that should stay alive and be dequeuing <see cref="PendingWork"/>.
        /// </summary>
        private static Thread? liveThread;

        /// <summary>
        /// Executes some action on a long-lived thread.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        /// <returns>
        /// A task that either faults with the exception thrown by <paramref name="action"/>
        /// or completes after successfully executing the delegate
        /// with a result that should be disposed when it is safe to terminate the long-lived thread.
        /// </returns>
        /// <remarks>
        /// This thread never posts to <see cref="SynchronizationContext.Current"/>, so it is safe
        /// to call this method and synchronously block on its result.
        /// </remarks>
        internal static async Task<IDisposable> ExecuteOnDedicatedThreadAsync(Action action)
        {
#if NET
            ArgumentNullException.ThrowIfNull(action);
#else
            _ = action ?? throw new ArgumentNullException(nameof(action));
#endif

            var tcs = new TaskCompletionSource<EmptyStruct>();
            bool keepAliveCountIncremented = false;
            try
            {
                lock (SyncObject)
                {
                    PendingWork.Enqueue(Tuple.Create(action, tcs));

                    try
                    {
                        // This block intentionally left blank.
                    }
                    finally
                    {
                        // We make these two assignments within a finally block
                        // to guard against an untimely ThreadAbortException causing
                        // us to execute just one of them.
                        keepAliveCountIncremented = true;
                        ++keepAliveCount;
                    }

                    if (keepAliveCount == 1)
                    {
                        Assumes.Null(liveThread);
                        liveThread = new Thread(Worker, SmallThreadStackSize)
                        {
                            IsBackground = true,
                            Name = "Registry watcher",
                        };
                        liveThread.Start();
                    }
                    else
                    {
                        // There *could* temporarily be multiple threads in some race conditions.
                        // Pulse all of them so that the live one is sure to get the message.
                        Monitor.PulseAll(SyncObject);
                    }
                }

                await tcs.Task.ConfigureAwait(false);
                return new ThreadHandleRelease();
            }
            catch
            {
                if (keepAliveCountIncremented)
                {
                    // Our caller will never have a chance to release their claim on the dedicated thread,
                    // so do it for them.
                    ReleaseRefOnDedicatedThread();
                }

                throw;
            }
        }

        /// <summary>
        /// Decrements the count of interested parties in the live thread,
        /// and helps it to terminate if necessary.
        /// </summary>
        private static void ReleaseRefOnDedicatedThread()
        {
            lock (SyncObject)
            {
                if (--keepAliveCount == 0)
                {
                    liveThread = null;

                    // Wake up any obsolete thread(s) so they can go to exit.
                    Monitor.PulseAll(SyncObject);
                }
            }
        }

        /// <summary>
        /// Executes thread-affinitized work from a queue until both the queue is empty
        /// and any lingering interest in the survival of the dedicated thread has been released.
        /// </summary>
        /// <remarks>
        /// This method serves as the <see cref="ThreadStart"/> for our dedicated thread.
        /// </remarks>
        private static void Worker()
        {
            while (true)
            {
                Tuple<Action, TaskCompletionSource<EmptyStruct>>? work = null;
                lock (SyncObject)
                {
                    if (Thread.CurrentThread != liveThread)
                    {
                        // Regardless of our PendingWork and keepAliveCount,
                        // it isn't meant for this thread any more.
                        // This happens when keepAliveCount (at least temporarily)
                        // hits 0, so this thread must be assumed to be on its exit path,
                        // and another thread will be spawned to process new requests.
                        Assumes.True(liveThread is not null || (keepAliveCount == 0 && PendingWork.Count == 0));
                        return;
                    }

                    if (PendingWork.Count > 0)
                    {
                        work = PendingWork.Dequeue();
                    }
                    else if (keepAliveCount == 0)
                    {
                        // No work, and no reason to stay alive. Exit the thread.
                        return;
                    }

                    // Sleep until another thread wants to wake us up with a Pulse.
                    Monitor.Wait(SyncObject);
                }

                if (work is not null)
                {
                    try
                    {
                        work.Item1();
                        work.Item2.SetResult(EmptyStruct.Instance);
                    }
                    catch (Exception ex)
                    {
                        work.Item2.SetException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Decrements the dedicated thread use counter by at most one upon disposal.
        /// </summary>
        private sealed class ThreadHandleRelease : IDisposable
        {
            /// <summary>
            /// A value indicating whether this instance has already been disposed.
            /// </summary>
            private bool disposed;

            /// <summary>
            /// Release the keep alive count reserved by this instance.
            /// </summary>
            public void Dispose()
            {
                lock (SyncObject)
                {
                    if (!this.disposed)
                    {
                        this.disposed = true;
                        ReleaseRefOnDedicatedThread();
                    }
                }
            }
        }
    }

    internal static partial class Interop
    {
        internal static partial class AdvApi32
        {
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#if NET
            [LibraryImport("advapi32.dll")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
#else
            [DllImport("advapi32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
#endif
            internal static
#if NET
                partial
#else
                extern
#endif
                int RegNotifyChangeKeyValue(
                SafeHandle hKey,
                [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
                REG_NOTIFY_FILTER dwNotifyFilter,
                SafeHandle hEvent,
                [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous
            );
        }

        internal static partial class Kernel32
        {
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#if NET
            [LibraryImport("kernel32.dll")]
            [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
#else
            [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
#endif
            internal static unsafe
#if NET
                partial
#else
                extern
#endif
                uint WaitForMultipleObjects(
                uint nCount,
                IntPtr* lpHandles,
                [MarshalAs(UnmanagedType.Bool)] bool bWaitAll,
                uint dwMilliseconds
            );
        }
    }

    /// <summary>
    /// An empty struct.
    /// </summary>
    /// <remarks>
    /// This can save 4 bytes over <see langword="object"/> when a type argument is required for a generic type, but entirely unused.
    /// </remarks>
    internal readonly struct EmptyStruct
    {
        /// <summary>
        /// Gets an instance of the empty struct.
        /// </summary>
        internal static EmptyStruct Instance
        {
            get { return default; }
        }
    }

    /// <summary>
    /// A non-generic class used to store statics that do not vary by generic type argument.
    /// </summary>
    internal static class LightUps
    {
        private static readonly Version Windows8Version = new(6, 2, 9200);

        /// <summary>
        /// Gets a value indicating whether the current operating system is Windows 8 or later.
        /// </summary>
        internal static bool IsWindows8OrLater
        {
            get => Environment.OSVersion.Version >= Windows8Version;
        }
    }
}

[Flags]
internal enum REG_NOTIFY_FILTER : uint
{
    REG_NOTIFY_CHANGE_NAME       = 0x00000001u, // Create or delete (child)
    REG_NOTIFY_CHANGE_ATTRIBUTES = 0x00000002u,
    REG_NOTIFY_CHANGE_LAST_SET   = 0x00000004u, // time stamp
    REG_NOTIFY_CHANGE_SECURITY   = 0x00000008u,
    REG_NOTIFY_THREAD_AGNOSTIC   = 0x10000000u, // Not associated with a calling thread, can only be used
                                                // for async user event based notification
    REG_LEGAL_CHANGE_FILTER =
        REG_NOTIFY_CHANGE_NAME       |
        REG_NOTIFY_CHANGE_ATTRIBUTES |
        REG_NOTIFY_CHANGE_LAST_SET   |
        REG_NOTIFY_CHANGE_SECURITY   |
        REG_NOTIFY_THREAD_AGNOSTIC,
}

/// <summary>
/// The various types of data within a registry key that generate notifications when changed.
/// </summary>
[Flags]
internal enum RegistryChangeNotificationFilters
{
    /// <summary>
    /// Notify the caller if a subkey is added or deleted.
    /// </summary>
    Subkey = (int)REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME,

    /// <summary>
    /// Notify the caller of changes to the attributes of the key,
    /// such as the security descriptor information.
    /// </summary>
    Attributes = (int)REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_ATTRIBUTES,

    /// <summary>
    /// Notify the caller of changes to a value of the key. This can
    /// include adding or deleting a value, or changing an existing value.
    /// </summary>
    Value = (int)REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET,

    /// <summary>
    /// Notify the caller of changes to the security descriptor of the key.
    /// </summary>
    Security = (int)REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_SECURITY,
}

internal static class Assumes
{
    /// <summary>
    /// Throws an public exception if a condition evaluates to false.
    /// </summary>
    [DebuggerStepThrough]
    public static void True(
        [DoesNotReturnIf(false)] bool condition,
        [CallerArgumentExpression(nameof(condition)), Localizable(false)] string? message = null)
    {
        if (!condition)
        {
            Fail(message);
        }
    }

    /// <summary>
    /// Throws <see cref="InternalErrorException"/> if the specified value is not null.
    /// </summary>
    /// <typeparam name="T">The type of value to test.</typeparam>
    [DebuggerStepThrough]
    public static void Null<T>(T? value)
        where T : class
    {
        if (value is not null)
        {
            FailNull<T>();
        }
    }

    /// <summary>
    /// Throws <see cref="InternalErrorException"/> if the specified value is not null.
    /// </summary>
    /// <typeparam name="T">The type of value to test.</typeparam>
    [DebuggerStepThrough]
    public static void Null<T>(T? value)
        where T : struct
    {
        if (value.HasValue)
        {
            FailNull<T>();
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailNull<T>()
    {
        Fail(string.Format(CultureInfo.InvariantCulture, "Unexpected non-null value of type '{0}'.", typeof(T).Name));
    }

    /// <summary>
    /// Throws an public exception.
    /// </summary>
    /// <returns>
    /// Nothing, as this method always throws. The signature allows for 'throwing' Fail so C# knows execution will stop.
    /// </returns>
    [DebuggerStepThrough]
    [DoesNotReturn]
    private static Exception Fail([Localizable(false)] string? message = null)
    {
        var exception = new InternalErrorException(message);
        bool proceed = true; // allows debuggers to skip the throw statement
        if (proceed)
        {
            throw exception;
        }
        else
        {
#pragma warning disable CS8763, CA2201
            return new Exception();
#pragma warning restore CS8763, CA2201
        }
    }

    /// <summary>
    /// The exception that is thrown when an internal assumption failed.
    /// </summary>
    [Serializable]
    private sealed class InternalErrorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalErrorException"/> class.
        /// </summary>
        [DebuggerStepThrough]
        public InternalErrorException(string? message = null)
            : base(message ?? "An internal error occurred.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalErrorException"/> class.
        /// </summary>
        [DebuggerStepThrough]
        public InternalErrorException(string? message, Exception? innerException)
            : base(message ?? "An internal error occurred.", innerException)
        {
        }

#if !NET
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalErrorException"/> class.
        /// </summary>
        [DebuggerStepThrough]
        private InternalErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
