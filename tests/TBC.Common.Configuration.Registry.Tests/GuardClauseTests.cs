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

namespace TBC.Common.Configuration.Registry.Tests;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

public sealed class GuardClauseTests
{
    [Fact]
    public void WindowsRegistryTreeWalker_Arguments()
    {
        Assert.Throws<ArgumentNullException>("rootKeyPath", () => new WindowsRegistryTreeWalker(null));
        Assert.Throws<ArgumentNullException>("rootKeyPath", () => new WindowsRegistryTreeWalker(string.Empty));
        Assert.Throws<ArgumentNullException>("rootKeyPath", () => new WindowsRegistryTreeWalker(" "));
    }

    [Fact]
    public void WindowsRegistryConfigurationOptions_Arguments()
    {
        const RegistryHive hive = RegistryHive.CurrentUser;
        Assert.Throws<ArgumentNullException>("rootKey", () => new WindowsRegistryConfigurationOptions(null, hive));
        Assert.Throws<ArgumentNullException>("rootKey", () => new WindowsRegistryConfigurationOptions(string.Empty, hive));
        Assert.Throws<ArgumentNullException>("rootKey", () => new WindowsRegistryConfigurationOptions(" ", hive));
    }

    [Fact]
    public void WindowsRegistryConfigurationExtensions_Arguments()
    {
        var builder = new ConfigurationBuilder();
        Assert.Throws<ArgumentNullException>("builder", () => WindowsRegistryConfigurationExtensions.AddWindowsRegistry(null, null));
        Assert.Throws<ArgumentNullException>("rootKey", () => WindowsRegistryConfigurationExtensions.AddWindowsRegistry(builder, null));
        Assert.Throws<ArgumentNullException>("rootKey", () => WindowsRegistryConfigurationExtensions.AddWindowsRegistry(builder, string.Empty));
        Assert.Throws<ArgumentNullException>("rootKey", () => WindowsRegistryConfigurationExtensions.AddWindowsRegistry(builder, " "));
    }
}
