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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Win32;
    using Xunit;

    public class ConfigurationExtensionTest : IDisposable
    {
        private const string RootKey = @"SOFTWARE\TBC Bank\TBC.Extensions.Configuration.Registry";
        private RegistryKey _registryKey;
        private RegistryKey _defaultConnectionKey;
        private RegistryKey _inventory;
        private RegistryKey _users;

        public ConfigurationExtensionTest()
        {
            this.Setup();
        }

        [Fact(DisplayName = "WindowsRegistryConfigurationProvider Load")]
        public void WindowsRegistryConfigurationProvider_LoadKeyValuePairsFromRegistryKey()
        {
            var options = new WindowsRegistryConfigurationOptions(RootKey, RegistryHive.CurrentUser);
            var config = new WindowsRegistryConfigurationProvider(options);

            config.Load();

            Assert.Equal("TestConnectionString", config.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", config.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", config.Get("Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", config.Get("Inventory:Provider"));
        }

        [Fact(DisplayName = "AddWindowsRegistry Build")]
        public void AddWindowsRegistry_BuildConfiguration()
        {
            var settings = new ConfigurationBuilder()
                .AddWindowsRegistry(RootKey, RegistryHive.CurrentUser)
                .Build()
                .AsEnumerable()
                .Where(i => i.Value != null)
                .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);

            Assert.NotNull(settings);
            Assert.NotEmpty(settings);
        }

        [Fact(DisplayName = "WindowsRegistryConfigurationProvider Array")]
        public void WindowsRegistryConfigurationProvider_ReadArray()
        {
            var settings = new ConfigurationBuilder()
                .AddWindowsRegistry(RootKey, RegistryHive.CurrentUser)
                .Build();

            var usersSection = settings.GetSection("Users");

            var array = usersSection.AsEnumerable();

            Assert.NotNull(array);
            Assert.NotEmpty(array);

            var user1a = usersSection["0"];
            var user1b = settings["Users:0"];

            Assert.NotNull(user1a);
            Assert.NotNull(user1b);
            Assert.Equal(user1a, user1b);

            var user2a = usersSection["1"];
            var user2b = settings["Users:1"];

            Assert.NotNull(user2a);
            Assert.NotNull(user2b);
            Assert.Equal(user2a, user2b);
        }

        private void Setup()
        {
            // Volatile keys will not be persisted after next Windows restart

            _registryKey = Registry.CurrentUser.CreateSubKey(RootKey, true, RegistryOptions.Volatile);

            _defaultConnectionKey = _registryKey.CreateSubKey("DefaultConnection", true, RegistryOptions.Volatile);
            _inventory = _registryKey.CreateSubKey("Inventory", true, RegistryOptions.Volatile);
            _users = _registryKey.CreateSubKey("Users", true, RegistryOptions.Volatile);

            _defaultConnectionKey.SetValue("ConnectionString", "TestConnectionString", RegistryValueKind.String);
            _defaultConnectionKey.SetValue("Provider", "SqlClient", RegistryValueKind.String);

            _inventory.SetValue("ConnectionString", "AnotherTestConnectionString", RegistryValueKind.String);
            _inventory.SetValue("Provider", "MySql", RegistryValueKind.String);

            //
            // Array values
            // https://stackoverflow.com/a/41330941/96009
            //
            using var user1 = _users.CreateSubKey("0", true, RegistryOptions.Volatile);
            user1.SetValue(string.Empty, "User 1");

            using var user2 = _users.CreateSubKey("1", true, RegistryOptions.Volatile);
            user2.SetValue(string.Empty, "User 2");

            using var user3 = _users.CreateSubKey("2", true, RegistryOptions.Volatile);
            user3.SetValue(string.Empty, "User 3");
        }

        public void Dispose()
        {
            _defaultConnectionKey?.Dispose();
            _inventory?.Dispose();
            _users?.Dispose();
            _registryKey?.Dispose();

            _defaultConnectionKey = null;
            _inventory = null;
            _users = null;
            _registryKey = null;
        }
    }
}
