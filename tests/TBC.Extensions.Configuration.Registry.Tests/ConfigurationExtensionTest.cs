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

namespace TBC.Extensions.Configuration.Registry.Tests
{
    using System;
    using Microsoft.Win32;
    using Xunit;

    public class ConfigurationExtensionTest : IDisposable
    {
        private const string rootKey = @"SOFTWARE\TBC Bank\TBC.Extensions.Configuration.Registry";
        private RegistryKey _registryKey;
        private RegistryKey _defaultConnectionKey;
        private RegistryKey _inventory;

        public ConfigurationExtensionTest()
        {
            this.Setup();
        }

        [Fact]
        public void LoadKeyValuePairsFromRegistryKey()
        {
            var configSrc = new WindowsRegistryConfigurationProvider(new WindowsRegistryConfigurationOptions(rootKey, RegistryHive.CurrentUser));

            configSrc.Load();

            Assert.Equal("TestConnectionString", configSrc.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", configSrc.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", configSrc.Get("Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", configSrc.Get("Inventory:Provider"));
        }

        private void Setup()
        {
            _registryKey = Registry.CurrentUser.CreateSubKey(rootKey, true, RegistryOptions.Volatile);

            _defaultConnectionKey = _registryKey.CreateSubKey("DefaultConnection", true, RegistryOptions.Volatile);
            _inventory = _registryKey.CreateSubKey("Inventory", true, RegistryOptions.Volatile);

            _defaultConnectionKey.SetValue("ConnectionString", "TestConnectionString", RegistryValueKind.String);
            _defaultConnectionKey.SetValue("Provider", "SqlClient", RegistryValueKind.String);

            _inventory.SetValue("ConnectionString", "AnotherTestConnectionString", RegistryValueKind.String);
            _inventory.SetValue("Provider", "MySql", RegistryValueKind.String);
        }

        public void Dispose()
        {
            _defaultConnectionKey?.Dispose();
            _inventory?.Dispose();
            _registryKey?.Dispose();
        }
    }
}
