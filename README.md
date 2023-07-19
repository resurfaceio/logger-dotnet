# resurfaceio-logger-dotnet
Easily log API requests and responses to your own <a href="https://resurface.io">security data lake</a>.

[![License](https://img.shields.io/github/license/resurfaceio/logger-dotnet)](https://github.com/resurfaceio/logger-dotnet/blob/master/LICENSE)
[![Contributing](https://img.shields.io/badge/contributions-welcome-green.svg)](https://github.com/resurfaceio/logger-dotnet/blob/master/CONTRIBUTING.md)

## Contents

<ul>
<li><a href="#dependencies">Dependencies</a></li>
<li><a href="#installing_with_nuget">Installing With NuGet</a></li>
<li><a href="#logging_from_asp_dotnet">Logging From ASP.NET</a></li>
<li><a href="#logging_with_api">Logging With API</a></li>
<li><a href="#privacy">Protecting User Privacy</a></li>
</ul>

<a name="dependencies"/>

## Dependencies

Requires .NET 5 or later. No other dependencies to conflict with your app.

<a name="installing_with_nuget"/>

## Installing with NuGet

`dotnet add package TestUsageLogger`

<a name="logging_from_asp_dotnet"/>

## Logging From ASP.NET

After <a href="#installing_with_nuget">installing the module</a>, add the following call to the Resurface middleware on `Startup.Configure`:

```csharp
using Resurfaceio;

public class Startup
{
    // ...
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseHttpLoggerForNET();
        
        // ...
    }
}
```

## Logging With API

Loggers can be directly integrated into your application using our [API](API.md). This requires the most effort compared with
the options described above, but also offers the greatest flexibility and control.

[API documentation](API.md)

<a name="privacy"/>

## Protecting User Privacy

Loggers always have an active set of <a href="https://resurface.io/rules.html">rules</a> that control what data is logged
and how sensitive data is masked. All of the examples above apply a predefined set of rules (`include debug`),
but logging rules are easily customized to meet the needs of any application.

<a href="https://resurface.io/rules.html">Logging rules documentation</a>

---
<small>&copy; 2016-2023 <a href="https://resurface.io">Graylog, Inc.</a></small>
