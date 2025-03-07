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

namespace TBC.Common.Configuration.Registry;

using System;

/// <summary>
/// Contains information about a key/value load exception.
/// </summary>
public class LoadExceptionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadExceptionContext"/> class.
    /// </summary>
    /// <param name="provider">The configuration provider.</param>
    /// <param name="exception">An exception instance.</param>
    internal LoadExceptionContext(WindowsRegistryConfigurationProvider provider, Exception exception)
    {
        Provider = provider;
        Exception = exception;
    }

    /// <summary>
    /// The <see cref="WindowsRegistryConfigurationProvider"/> that caused the exception.
    /// </summary>
    public WindowsRegistryConfigurationProvider Provider { get; }

    /// <summary>
    /// The exception that occurred in <see cref="WindowsRegistryConfigurationProvider.Load"/>.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// A value that indicates whether the exception should be rethrown.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the exception should be rethrown; otherwise, <see langword="false"/>.
    /// </value>
    public bool Ignore { get; set; }
}
