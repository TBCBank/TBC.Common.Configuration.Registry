
# Windows Registry based ASP.NET Core Configuration Provider

Allows loading configuration data from Windows Registry key.

## Usage

### Install NuGet package

```powershell
Install-Package TBC.Extensions.Configuration.Registry -IncludePrerelease
```

### Add Windows Registry configuration provider to builder pipeline

```csharp
public class Program
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
                config.AddWindowsRegistry("SOFTWARE\\MyCompany\\MyApp\\ConfigKey1");
            });
}
```
