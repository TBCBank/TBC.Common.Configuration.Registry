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

namespace Microsoft.Extensions.Configuration
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using TBC.Common.Configuration.Registry;

    /// <summary>
    /// Extension methods for adding <see cref="WindowsRegistryConfigurationProvider"/>.
    /// </summary>
    public static class WindowsRegistryConfigurationExtensions
    {
        /// <summary>
        /// Adds the Windows Registry configuration provider at <paramref name="rootKey"/>
        /// to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="rootKey">The root key path.</param>
        /// <param name="registryHive">Top-level Windows Registry hive.</param>
        /// <param name="optional">Whether or not the Registry key is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
#if NET5_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public static IConfigurationBuilder AddWindowsRegistry(
            this IConfigurationBuilder builder,
            string rootKey,
            RegistryHive registryHive = RegistryHive.LocalMachine,
            bool optional = true)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(rootKey))
            {
                throw new ArgumentNullException(nameof(rootKey));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return builder.Add(
                    new WindowsRegistryConfigurationSource(
                        new WindowsRegistryConfigurationOptions(rootKey, registryHive) { Optional = optional }));
            }

            return builder;
        }
    }
}
