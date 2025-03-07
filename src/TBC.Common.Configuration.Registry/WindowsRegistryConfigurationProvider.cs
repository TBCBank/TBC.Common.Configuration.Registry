/*
 * Copyright (c) 2025 TBC Bank
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#nullable enable

#pragma warning disable CA1031

namespace TBC.Common.Configuration.Registry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using CultureInfo = System.Globalization.CultureInfo;

/// <summary>
/// Provides configuration key-value pairs that are obtained from a Windows Registry key.
/// </summary>
#if NET
[SupportedOSPlatform("windows")]
#endif
public class WindowsRegistryConfigurationProvider : ConfigurationProvider, IDisposable
{
    private CancellationTokenSource? _cancellationToken;
    private Task? _reloaderTask;

    /// <summary>
    /// Initializes a new instance with the specified options.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public WindowsRegistryConfigurationProvider(WindowsRegistryConfigurationSource options)
    {
        Source = options ?? throw new ArgumentNullException(nameof(options));
    }

    internal WindowsRegistryConfigurationSource Source { get; }

    /// <summary>
    /// Loads configuration data from Windows Registry key.
    /// </summary>
    public override void Load()
    {
        try
        {
            using var regWalker = new WindowsRegistryTreeWalker(Source.RootKey, Source.RegistryHive, Source.Optional);
            this.Data = regWalker.ParseTree();
        }
        catch (Exception error)
        {
            HandleException(ExceptionDispatchInfo.Capture(error));
        }

        // Schedule a background reloader task only if none exists and reload on changes is requested
        if (_reloaderTask is null && Source.ReloadOnChange)
        {
            _cancellationToken = new CancellationTokenSource();
            _reloaderTask = WaitForRegistryChangesAsync();
        }
    }

    /// <summary>
    ///   Disposes the provider.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///   Disposes the provider.
    /// </summary>
    /// <param name="disposing">
    ///   <see langword="true"/> if invoked from <see cref="IDisposable.Dispose"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _cancellationToken is not null)
        {
            try
            {
                _cancellationToken.Cancel();
                _cancellationToken.Dispose();
            }
            catch
            {
                // Dispose should not throw
            }
        }
    }

#pragma warning disable S3928, MA0015, MA0051

    private Task WaitForRegistryChangesAsync()
    {
        if (_cancellationToken is null)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(Source.RootKey))
        {
            throw new ArgumentNullException(nameof(Source.RootKey));
        }

        var rootKey = Source.RegistryHive switch
        {
            RegistryHive.LocalMachine => Registry.LocalMachine.OpenSubKey(Source.RootKey!, writable: false),
            RegistryHive.CurrentUser => Registry.CurrentUser.OpenSubKey(Source.RootKey!, writable: false),
            _ => throw new ArgumentOutOfRangeException(nameof(Source.RegistryHive)),
        };

        if (rootKey is null)
        {
            var exception = new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                "Failed to open the Registry key '{0}'.", Source.RootKey));
            HandleException(ExceptionDispatchInfo.Capture(exception));
            // Not going to monitor non-existent key; roach out
            return Task.CompletedTask;
        }

        return CoreAsync();

        async Task CoreAsync()
        {
            var token = _cancellationToken.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Re-subscribe for changes
                    await rootKey.WaitForChangeAsync(
                        watchSubtree: true,
                        change: RegistryChangeNotificationFilters.Subkey | RegistryChangeNotificationFilters.Value,
                        cancellationToken: token).ConfigureAwait(false);

                    // This delay should help avoid triggering an excessive reloads before an entire subtree is completely written
                    await Task.Delay(250, token).ConfigureAwait(false);

                    using var regWalker = new WindowsRegistryTreeWalker(Source.RootKey, Source.RegistryHive, Source.Optional);
                    this.Data = regWalker.ParseTree();
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    // Matches the behavior of the FileConfigurationProvider: empty out the Data and invoke user-supplied error handler
                    Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    var exception = new InvalidDataException(string.Format(CultureInfo.InvariantCulture,
                        "Failed to load configuration from Registry key '{0}'.", Source.RootKey), error);
                    HandleException(ExceptionDispatchInfo.Capture(exception));
                }

                this.OnReload();
            }
        }
    }

    private void HandleException(ExceptionDispatchInfo info)
    {
        var ignoreException = false;
        var customAction = Source.OnLoadException;
        if (customAction is not null)
        {
            var context = new LoadExceptionContext(this, info.SourceException);
            customAction.Invoke(context);
            ignoreException = context.Ignore;
        }

        if (!ignoreException)
        {
            info.Throw();
        }
    }
}
