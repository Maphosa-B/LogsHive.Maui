# LogsHive.Maui

![Bee Logo](https://i.imgur.com/ggposCD.png)

MAUI-native crash and error monitoring for .NET MAUI apps. Capture exceptions, log events, and monitor your app in production - with an SDK built specifically for MAUI.

[![NuGet](https://img.shields.io/nuget/v/LogsHive.Maui)](https://www.nuget.org/packages/LogsHive.Maui)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## Two ways to use LogsHive

### Self-hosted - Free forever

Host the LogsHive API on your own infrastructure at no cost. You own your data, your server, and your retention policy. Ideal for teams that already have a backend or prefer to keep error data in-house.

- No subscription fees
- No event limits
- No data leaving your infrastructure
- Full control over retention and storage

The self-hosted API expects a single endpoint:

```
POST /api/errors/capture
Content-Type: application/json
X-Api-Key: your_api_key  (optional - you control auth on your own server)

{
  "appName": "MyApp",
  "projectId": "your_project_id_here",
  "exceptionType": "System.NullReferenceException",
  "message": "Object reference not set to an instance of an object.",
  "stackTrace": "at MyApp.HomePage.LoadData()",
  "source": "MyApp.HomePage",
  "platform": "Android",
  "operatingSystem": "Android 14 (API 34)",
  "appVersion": "1.0.3",
  "deviceModel": "Samsung Galaxy S23",
  "capturedAt": "2026-03-16T10:45:00Z",
  "logMessage": null,
  "tags": {}
}
```

---

### SaaS - Hosted by Conversion Hive

Let us handle the infrastructure. Get a dashboard, alerts, and error grouping out of the box.

The SaaS API is hosted at `https://logs-hive-api.conversion-hive.com`. You only need your API key and project ID - the URL is handled automatically by the SDK when `Mode = LogsHiveMode.SaaS`.

## Pricing

| Plan | Price | Events / Month | Retention | Best For |
|-----|------|---------------|-----------|---------|
| Free | $0 | 5,000 | 7 days | Personal projects, small apps |
| Starter | $5 / month | 30,000 | 30 days | Indie developers and small SaaS |
| Pro | $15 / month | 100,000 | 90 days | Growing applications |
| Growth | $40 / month | 500,000 | 90 days | Production SaaS products |
| Enterprise | Custom | Custom | Custom | Large teams and high-volume systems |

Top-up available at $5 once-off for +30 000 events in the current period.

Sign up and get your API key and project ID at [Logs Hive](https://conversion-hive.com/logs-hive-details.html).

---

## Installation

```
dotnet add package LogsHive.Maui
```

Or search **LogsHive.Maui** in the Visual Studio NuGet Package Manager.

---

## Setup

### Step 1 - Initialize in `MauiProgram.cs`

```csharp
using LogsHive.Maui.Extensions;
using LogsHive.Maui.Models;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        builder.UseLogsHive(op =>
        {
            op.Mode        = LogsHiveMode.SaaS;
            op.ApiKey      = "lh_your_api_key_here";
            op.ProjectId   = "your_project_id_here";
            op.AppName     = "MyApp";
            op.Environment = LogsHiveEnvironmentType.Production;
        });

        return builder.Build();
    }
}
```

### Step 2 - Flush the queue in `App.xaml.cs`

```csharp
using LogsHive.Maui;

public partial class App : Application
{
    protected override async void OnStart()
    {
        base.OnStart();
        await LogsHiveClient.FlushAsync();
    }

    protected override async void OnResume()
    {
        base.OnResume();
        await LogsHiveClient.FlushAsync();
    }
}
```

That's it. LogsHive is now monitoring your app.

---

## Capturing errors

```csharp
using LogsHive.Maui;

// Log a message
LogsHiveClient.Log("User reached checkout");

// Capture a caught exception (fire-and-forget)
try
{
    await LoadDataAsync();
}
catch (Exception ex)
{
    LogsHiveClient.Capture(ex);
}

// Capture and await before continuing
try
{
    await CriticalOperationAsync();
}
catch (Exception ex)
{
    await LogsHiveClient.CaptureAsync(ex);
}
```

---

## Configuration options

### SaaS (recommended)

```csharp
builder.UseLogsHive(op =>
{
    op.Mode        = LogsHiveMode.SaaS;
    op.ApiKey      = "lh_your_api_key_here";
    op.ProjectId   = "your_project_id_here";
    op.AppName     = "MyApp";
    op.Environment = LogsHiveEnvironmentType.Production;
});
```

> The SaaS API URL (`https://logs-hive-api.conversion-hive.com`) is set automatically - you do not need to configure it.

### Self-hosted

```csharp
builder.UseLogsHive(op =>
{
    op.Mode          = LogsHiveMode.SelfHosted;
    op.SelfHostedUrl = "https://logs.yourcompany.com";
    op.ProjectId     = "your_project_id_here";
    op.AppName       = "MyApp";
    op.Environment   = LogsHiveEnvironmentType.Production;
});
```

> `ApiKey` is optional for self-hosted - you control authentication on your own server.

### Debug mode (no events sent)

```csharp
builder.UseLogsHive(op =>
{
    op.Mode        = LogsHiveMode.SaaS;
    op.ApiKey      = "lh_your_api_key_here";
    op.ProjectId   = "your_project_id_here";
    op.AppName     = "MyApp";
    op.Environment = LogsHiveEnvironmentType.Debug;
});
```

In debug mode the SDK is **inactive** - no events are sent or queued. All SDK activity is written to `Debug.WriteLine` so you can verify the plumbing in Visual Studio's Output window.

### Enable local console logging

Want to see SDK activity in your Output window while running in Production mode? Enable local console logging:

```csharp
builder.UseLogsHive(op =>
{
    op.Mode               = LogsHiveMode.SaaS;
    op.ApiKey             = "lh_your_api_key_here";
    op.ProjectId          = "your_project_id_here";
    op.AppName            = "MyApp";
    op.Environment        = LogsHiveEnvironmentType.Production;
    op.EnableLocalConsoleLogging = true;
});
```

With `EnableLocalConsoleLogging  = true` the SDK writes all internal activity — successful sends, 401s, 429s, queue operations, and network errors - to `Debug.WriteLine` (or `Android.Util.Log` on Android). This is independent of `Environment` so you can verify your integration is working in a production build without changing capture behaviour.

> Tip: disable this before shipping to avoid unnecessary output in release builds.

### Switch automatically by build

```csharp
builder.UseLogsHive(op =>
{
    op.Mode      = LogsHiveMode.SaaS;
    op.ApiKey    = "lh_your_api_key_here";
    op.ProjectId = "your_project_id_here";
    op.AppName   = "MyApp";

#if DEBUG
    op.Environment = LogsHiveEnvironmentType.Debug;
    op.EnableLocalConsoleLogging = true;
#else
    op.Environment = LogsHiveEnvironmentType.Production;
#endif
});
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Mode` | `LogsHiveMode` | Yes | `SaaS` | `SaaS` or `SelfHosted` |
| `Environment` | `LogsHiveEnvironmentType` | Yes | `Debug` | `Production` (events sent) or `Debug` (no events sent) |
| `ApiKey` | `string` | SaaS only | `null` | Your API key from [Logs Hive](https://conversion-hive.com/logs-hive-details.html) |
| `ProjectId` | `string` | Yes | — | Your project ID — routes events to the correct project |
| `AppName` | `string` | Yes | `UnknownApp` | Human-readable name shown in the dashboard |
| `SelfHostedUrl` | `string` | SelfHosted only | `null` | Base URL of your self-hosted instance |
| `EnableLocalLogging` | `bool` | No | `false` | Writes SDK activity to the Output window. Independent of `Environment` |
| `Tags` | `Dictionary<string, string>` | No | `{}` | Global tags attached to every captured event |

---

## API reference

### `LogsHiveClient`

| Method | Description |
|---|---|
| `Log(message)` | Sends a free-form log message. Fire-and-forget |
| `Capture(ex)` | Captures an exception. Fire-and-forget |
| `CaptureAsync(ex)` | Captures an exception. Awaitable |
| `FlushAsync()` | Flushes the offline queue. Call on `OnStart` / `OnResume` |
| `GetQueueCountAsync()` | Returns the number of events pending in the offline queue |
| `IsInitialized` | Returns `true` if the SDK has been initialized |

---

## Offline support

LogsHive automatically queues events to disk when your device is offline or when the API returns a `429 Too Many Requests` response. The queue is stored as `logshive_queue.json` in the app's data directory and holds a maximum of 500 entries.

| Scenario | Behaviour |
|---|---|
| Successful send (2xx) | Event delivered, nothing queued |
| `401 Unauthorized` | Event discarded silently |
| `429 Too Many Requests` | Event queued to disk |
| No network | Event queued to disk |
| App restart / resume | Queue flushed automatically via `FlushAsync()` |

You can check how many events are pending at any time:

```csharp
var pending = await LogsHiveClient.GetQueueCountAsync();
if (pending > 0)
    StatusLabel.Text = $"{pending} events pending sync";
```

---

## Supported platforms

| Platform | Minimum version |
|---|---|
| Android | API 21 (Android 5.0) |
| iOS | 15.0 |
| Mac Catalyst | 15.0 |
| Windows | 10.0.17763.0 |

---

## License

MIT © [Conversion Hive](https://conversion-hive.com)
