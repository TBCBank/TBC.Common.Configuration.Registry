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

#pragma warning disable CA1416, CA2208, MA0015, S3928

namespace TBC.Common.Configuration.Registry;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

/// <summary>
/// Represents a Windows Registry key as an <see cref="IConfigurationSource"/>.
/// </summary>
public class WindowsRegistryConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The root key path.
    /// </summary>
    [DisallowNull]
    public string? RootKey { get; set; }

    /// <summary>
    /// The top-level Windows Registry node.
    /// </summary>
    public RegistryHive RegistryHive { get; set; } = RegistryHive.LocalMachine;

    /// <summary>
    /// Determines if loading the configuration is optional.
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Determines if the source will be reloaded when the underlying Registry key changes.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// An action that's called if an uncaught exception occurs in <see cref="WindowsRegistryConfigurationProvider.Load"/>.
    /// </summary>
    public Action<LoadExceptionContext>? OnLoadException { get; set; }

    /// <summary>
    /// Builds the <see cref="WindowsRegistryConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="WindowsRegistryConfigurationProvider"/>.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(this.RootKey))
        {
            throw new ArgumentNullException(nameof(RootKey));
        }

        OnLoadException ??= DefaultExceptionHandler;

        return new WindowsRegistryConfigurationProvider(this);
    }

    private static void DefaultExceptionHandler(LoadExceptionContext context)
    {
        context.Ignore = context.Provider.Source.Optional;
    }
}
