
# Windows Registry based .NET Core Configuration Provider

[![NuGet version (TBC.Common.Configuration.Registry)](https://img.shields.io/nuget/v/TBC.Common.Configuration.Registry.svg)](https://www.nuget.org/packages/TBC.Common.Configuration.Registry/)
[![CI](https://github.com/TBCBank/TBC.Common.Configuration.Registry/actions/workflows/main.yml/badge.svg)](https://github.com/TBCBank/TBC.Common.Configuration.Registry/actions/workflows/main.yml)
[![CodeQL](https://github.com/TBCBank/TBC.Common.Configuration.Registry/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/TBCBank/TBC.Common.Configuration.Registry/actions/workflows/codeql-analysis.yml)
[![Downloads](https://img.shields.io/nuget/dt/TBC.Common.Configuration.Registry)](https://www.nuget.org/packages/TBC.Common.Configuration.Registry/)

Allows loading configuration data from a Windows Registry key.

## Usage

### Install NuGet package

```powershell
Install-Package TBC.Common.Configuration.Registry
```

```batch
dotnet add package TBC.Common.Configuration.Registry
```

```xml
<PackageReference Include="TBC.Common.Configuration.Registry" Version="2.0.1" />
```

### Example: Add Windows Registry provider to builder pipeline

```csharp
public static class Program
{
    // ...

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureAppConfiguration((builderContext, config) =>
            {
                if (OperatingSystem.IsWindows())
                {
                    config.AddWindowsRegistry(@"SOFTWARE\MyCompany\MyApp");
                }
            });
}
```

### Example: Add Windows Registry provider to builder pipeline (Minimal API)

```csharp
var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Configuration.AddWindowsRegistry(@"SOFTWARE\MyCompany\MyApp");
}

// ...
```

### Example: A .REG file to import into Windows Registry

```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\Software\MyCompany]

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp]

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig]

; Numbers in .REG files can only be hexadecimal

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\DefaultConnection]
"ConnectionString"="Server=example.com; Integrated Security=True; Database=MyDB"
"Provider"="System.Data.SqlClient"
"TimeoutSeconds"=dword:00000078

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\MyWebService]
"Url"="https://example.com/myapi"
"ApiKey"="VGVzdFNlcnZpY2U="

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\MyUsers]

; Example: array/list/collection values

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\MyUsers\0]
@="User1"

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\MyUsers\1]
@="User2"

[HKEY_LOCAL_MACHINE\Software\MyCompany\MyApp\MyConfig\MyUsers\2]
@="User3"
```

### Example: A corresponding IOptions<T> class

Create an [`IOptions<T>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.options.ioptions-1) implementation:

```csharp
public class MyConfigOptions : IOptions<MyConfigOptions>
{
    public MyConfigOptions Value => this;

    public DatabaseConfig DefaultConnection { get; set; }
    public WebServiceConfig MyWebService { get; set; }
    public List<string> MyUsers { get; set; }
}

public record DatabaseConfig
{
    public string ConnectionString { get; set; }
    public string Provider { get; set; }
    public int TimeoutSeconds { get; set; }
}

public record WebServiceConfig
{
    public string Url { get; set; }
    public string ApiKey { get; set; }
}
```

Register options in `Startup.ConfigureServices()` method:

```csharp
services.AddOptions<MyConfigOptions>().BindConfiguration("MyConfig");
```

Now you can inject `IOptions<MyConfigOptions>` into transient services and `IOptionsMonitor<MyConfigOptions>` into singleton ones.
