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

namespace TBC.Common.Configuration.Registry.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

public sealed class ConfigurationExtensionTest : IDisposable
{
    private const string RootKey = @"SOFTWARE\TBC Bank\TBC.Common.Configuration.Registry";
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
        var builder = new ConfigurationBuilder();

        builder.AddWindowsRegistry(RootKey, RegistryHive.CurrentUser);

        var config = builder.Build();
        try
        {
            Assert.Equal("TestConnectionString", config.GetValue<string>("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", config.GetValue<string>("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", config.GetValue<string>("Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", config.GetValue<string>("Inventory:Provider"));
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Fact(DisplayName = "WindowsRegistryConfigurationProvider ReloadOnChange")]
    public async Task WindowsRegistryConfigurationProvider_ObserveReloadOnChange()
    {
        var builder = new ConfigurationBuilder();

        builder.AddWindowsRegistry(static x =>
        {
            x.RootKey = RootKey;
            x.RegistryHive = RegistryHive.CurrentUser;
            x.ReloadOnChange = true;
        });

        var reloadHappened = false;

        var config = builder.Build();
        try
        {
            Assert.Equal("TestConnectionString", config["defaultconnection:ConnectionString"]);
            Assert.Equal("SqlClient", config["DEFAULTCONNECTION:PROVIDER"]);
            Assert.Equal("AnotherTestConnectionString", config["Inventory:CONNECTIONSTRING"]);
            Assert.Equal("MySql", config["Inventory:Provider"]);

            config.GetReloadToken().RegisterChangeCallback(s => { reloadHappened = true; }, state: null);

            using (var editKey = Registry.CurrentUser.OpenSubKey(RootKey, writable: true))
            using (var connStr = editKey.OpenSubKey("DefaultConnection", writable: true))
            {
                connStr.SetValue("Provider", "SQLite", RegistryValueKind.String);
                editKey.Flush();
            }

            await Task.Delay(1200, TestContext.Current.CancellationToken);  // Change monitoring happens in background. We can sleep

            Assert.True(reloadHappened);
            Assert.Equal("SQLite", config["DEFAULTCONNECTION:PROVIDER"]);
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Fact(DisplayName = "WindowsRegistryConfigurationProvider Optional Non-Existent")]
    public void WindowsRegistryConfigurationProvider_Optional_NonExistent()
    {
        var builder = new ConfigurationBuilder();

        builder.AddWindowsRegistry(static x =>
        {
            x.RootKey = $"SOFTWARE\\TBC Bank\\DoesNotExist_{Guid.NewGuid():N}";
            x.RegistryHive = RegistryHive.CurrentUser;
            x.ReloadOnChange = true;
        });

        var config = builder.Build();
        try
        {
            Assert.Null(config["defaultconnection:ConnectionString"]);
            Assert.Null(config["DEFAULTCONNECTION:PROVIDER"]);
            Assert.Null(config["Inventory:CONNECTIONSTRING"]);
            Assert.Null(config["Inventory:Provider"]);
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Fact(DisplayName = "AddWindowsRegistry Build")]
    public void AddWindowsRegistry_BuildConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddWindowsRegistry(RootKey, RegistryHive.CurrentUser)
            .Build();
        try
        {
            var settings = config
                .AsEnumerable()
                .Where(i => i.Value != null)
                .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);

            Assert.NotNull(settings);
            Assert.NotEmpty(settings);
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Fact(DisplayName = "AddWindowsRegistry Optional")]
    public void AddWindowsRegistry_BuildOptionalConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddWindowsRegistry($"SOFTWARE\\TBC Bank\\DoesNotExist\\{Guid.NewGuid():N}", RegistryHive.CurrentUser, optional: true)
            .Build();
        try
        {
            Assert.NotNull(config);
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Fact(DisplayName = "WindowsRegistryConfigurationProvider Array")]
    public void WindowsRegistryConfigurationProvider_ReadArray()
    {
        var config = new ConfigurationBuilder()
            .AddWindowsRegistry(RootKey, RegistryHive.CurrentUser)
            .Build();
        try
        {
            var usersSection = config.GetSection("Users");

            var array = usersSection.AsEnumerable();

            Assert.NotNull(array);
            Assert.NotEmpty(array);

            var user1a = usersSection["0"];
            var user1b = config["Users:0"];

            Assert.NotNull(user1a);
            Assert.NotNull(user1b);
            Assert.Equal(user1a, user1b);

            var user2a = usersSection["1"];
            var user2b = config["Users:1"];

            Assert.NotNull(user2a);
            Assert.NotNull(user2b);
            Assert.Equal(user2a, user2b);
        }
        finally
        {
            (config as IDisposable)?.Dispose();
        }
    }

    [Theory(DisplayName = "WindowsRegistryTreeWalker Unsupported Hive")]
    [InlineData(RegistryHive.ClassesRoot)]
    [InlineData(RegistryHive.CurrentConfig)]
    [InlineData(RegistryHive.PerformanceData)]
    [InlineData(RegistryHive.Users)]
    public void WindowsRegistryTreeWalker_UnsupportedHive(RegistryHive hive)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new WindowsRegistryTreeWalker(RootKey, hive, optional: false);
        });
    }

    [Theory]
    [InlineData("SOFTWARE\\TBC Bank\\DoesNotExist1\\6475D6101A5443E5AEB62A971D98C394", RegistryHive.LocalMachine)]
    [InlineData("SOFTWARE\\TBC Bank\\DoesNotExist2\\57E4A5F3F1E34F679A0D4016595DF227", RegistryHive.LocalMachine)]
    [InlineData("SOFTWARE\\TBC Bank\\DoesNotExist3\\84852EF9580F4983A0DF4760800F568F", RegistryHive.CurrentUser)]
    [InlineData("SOFTWARE\\TBC Bank\\DoesNotExist4\\43D648A261694197BE049D25E9B3A789", RegistryHive.CurrentUser)]
    public void WindowsRegistryTreeWalker_RootKeyNotFound(string rootKey, RegistryHive hive)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = new WindowsRegistryTreeWalker(rootKey, hive, optional: false);
        });
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
    }
}
