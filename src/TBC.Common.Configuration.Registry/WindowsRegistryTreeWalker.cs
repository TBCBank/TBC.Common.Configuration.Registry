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

namespace TBC.Common.Configuration.Registry;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

#if NET5_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
internal sealed class WindowsRegistryTreeWalker : IDisposable
{
    private readonly SortedDictionary<string, string> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _context = new();
    private string _currentPath;
    private RegistryKey _rootKey;
    private readonly bool _optional;

    public WindowsRegistryTreeWalker(string rootKeyPath, RegistryHive registryHive = RegistryHive.LocalMachine, bool optional = true)
    {
        if (string.IsNullOrWhiteSpace(rootKeyPath))
        {
            throw new ArgumentNullException(nameof(rootKeyPath));
        }

        _optional = optional;

        _rootKey = registryHive switch
        {
            RegistryHive.LocalMachine => Registry.LocalMachine.OpenSubKey(rootKeyPath),
            RegistryHive.CurrentUser => Registry.CurrentUser.OpenSubKey(rootKeyPath),
            _ => throw new ArgumentOutOfRangeException(nameof(registryHive)),
        };

        if (_rootKey is null && !optional)
        {
            throw new InvalidOperationException($"Registry key '{rootKeyPath}' was not found.");
        }
    }

    private void EnterContext(string context)
    {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    private void ExitContext()
    {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    public IDictionary<string, string> ParseTree()
    {
        if (_optional && _rootKey is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        _data.Clear();
        VisitRegistryKey(_rootKey);
        return _data;
    }

    private void VisitRegistryKey(RegistryKey key)
    {
        string[] valueNames = key.GetValueNames();

        if (valueNames != null && valueNames.Length > 0)
        {
            foreach (string valueName in valueNames)
            {
                EnterContext(valueName);
                VisitRegistryValue(key, valueName);
                ExitContext();
            }
        }

        string[] keyNames = key.GetSubKeyNames();

        if (keyNames != null && keyNames.Length > 0)
        {
            foreach (string keyName in keyNames)
            {
                using var subKey = key.OpenSubKey(keyName);

                EnterContext(keyName);
                VisitRegistryKey(subKey);
                ExitContext();
            }
        }
    }

    private void VisitRegistryValue(RegistryKey parent, string valueName)
    {
        var path = _currentPath;
        var value = parent.GetValue(valueName)?.ToString();

#pragma warning disable IDE0057  // Substring can be simplified (.netstandard target does not contain Range operator support)

        // Special case: when writing (default) value, we dont want an empty string
        // as value name appended to the key!
        if (string.IsNullOrWhiteSpace(valueName))
        {
            // Turns this: 'Key:SubKey:0:' into 'Key:SubKey:0', as this is what ASP.NET Core likes.
            path = path.Substring(0, path.LastIndexOf(ConfigurationPath.KeyDelimiter, StringComparison.Ordinal));
        }

#pragma warning restore IDE0057

        _data[path] = value;
    }

    public void Dispose()
    {
        _rootKey?.Dispose();
        _rootKey = null;
    }
}
