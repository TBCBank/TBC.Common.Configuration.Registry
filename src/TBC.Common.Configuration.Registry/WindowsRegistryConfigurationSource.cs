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

namespace TBC.Common.Configuration.Registry
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represents a Windows Registry key as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class WindowsRegistryConfigurationSource : IConfigurationSource
    {
        private readonly WindowsRegistryConfigurationOptions _options;

        /// <summary>
        /// Initializes a new instance with the specified options.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public WindowsRegistryConfigurationSource(WindowsRegistryConfigurationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Builds the <see cref="WindowsRegistryConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="WindowsRegistryConfigurationProvider"/></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new WindowsRegistryConfigurationProvider(_options);
    }
}
