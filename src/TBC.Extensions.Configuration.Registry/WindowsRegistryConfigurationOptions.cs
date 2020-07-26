/*
 * Copyright (c) 2019 TBC Bank
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

namespace TBC.Extensions.Configuration.Registry
{
    using System;
    using Microsoft.Win32;

    /// <summary>
    /// Options for <see cref="WindowsRegistryConfigurationSource"/>.
    /// </summary>
    public class WindowsRegistryConfigurationOptions
    {
        /// <summary>
        /// The root key path.
        /// </summary>
        public string RootKey { get; set; }

        /// <summary>
        /// The top-level Windows Registry node.
        /// </summary>
        public RegistryHive RegistryHive { get; set; } = RegistryHive.LocalMachine;

        /// <summary>
        /// Initializes a new instance with the specified options.
        /// </summary>
        /// <param name="rootKey">The root key path.</param>
        /// <param name="registryHive">The top-level Windows Registry node.</param>
        public WindowsRegistryConfigurationOptions(string rootKey, RegistryHive registryHive)
        {
            if (string.IsNullOrWhiteSpace(rootKey))
            {
                throw new ArgumentNullException(nameof(rootKey));
            }

            this.RootKey = rootKey;
            this.RegistryHive = registryHive;
        }
    }
}
