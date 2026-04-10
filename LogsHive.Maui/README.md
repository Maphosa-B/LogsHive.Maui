# LogsHive.Maui

![Bee Logo](https://i.imgur.com/ggposCD.png)

MAUI-native crash, error monitoring, and memory leak detection for .NET MAUI apps. Capture exceptions, log events, monitor memory in production — with an SDK built specifically for MAUI.

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

### SaaS - Hosted by Conversion Hive

Let us handle the infrastructure. Get a dashboard, alerts, and error grouping out of the box.

The SaaS API is hosted at `https://logs-hive-api.conversion-hive.com`. You only need your API key and project ID — the URL is handled automatically by the SDK when `Mode = LogsHiveMode.SaaS`.

## Pricing

| Plan | Price | Events / Month | Retention | Best For |
|-----|------|---------------|-----------|---------|
| Free | $0 | 5,000 | 7 days | Personal projects, small apps |
| Starter | $5 / month | 30,000 | 30 days | Indie developers and small SaaS |
| Pro | $15 / month | 100,000 | 90 days | Growing applications |
| Growth | $40 / month | 500,000 | 90 days | Production SaaS products |
| Enterprise | Custom | Custom | Custom | Large teams and high-volume systems |

Top-up available at $5 once-off for +30,000 events in the current period.

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
            op.Mode         = LogsHiveMode.SaaS;
            op.ApiKey       = "lh_your_api_key_here";
            op.ProjectId    = "your_project_id_here";
            op.AppName      = "MyApp";
            op.SendToServer = true;
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

## Memory leak monitoring

LogsHive can automatically detect memory leaks in production — on real user devices, across all platforms — without requiring a profiler or local reproduction.

### How it works

A background timer samples managed heap (`GC.GetTotalMemory`) and native working set (`Environment.WorkingSet`) at a configurable interval. If heap grows across N consecutive samples without recovering, LogsHive sends a memory snapshot to your API and flags the session as leaking. Thresholds and sensitivity are configured in the LogsHive dashboard — no code changes needed to tune them.

### Enable in `MauiProgram.cs`

```csharp
builder.UseLogsHive(op =>
{
    op.Mode         = LogsHiveMode.SaaS;
    op.ApiKey       = "lh_your_api_key_here";
    op.ProjectId    = "your_project_id_here";
    op.AppName      = "MyApp";
    op.SendToServer = true;

    // enable automatic memory leak detection
    op.EnableMemoryMonitoring          = true;
    op.MemoryMonitoringIntervalSeconds = 30; // default: 30s, minimum: 10s
});
```

`MemoryMonitoringIntervalSeconds` is only validated when `EnableMemoryMonitoring` is `true` — setting it freely while monitoring is disabled will not cause an error.

### Manual only — no background monitor

```csharp
builder.UseLogsHive(op =>
{
    op.Mode         = LogsHiveMode.SaaS;
    op.ApiKey       = "lh_your_api_key_here";
    op.ProjectId    = "your_project_id_here";
    op.AppName      = "MyApp";
    op.SendToServer = true;
    // EnableMemoryMonitoring not set → no background timer, zero overhead
});

// manual captures still work anywhere
await LogsHiveClient.CaptureHeapAsync("before-gallery-load");
await LoadGalleryAsync();
await LogsHiveClient.CaptureHeapAsync("after-gallery-load");
```

### Scoped measurement (recommended)

```csharp
await using var scope = LogsHiveClient.MeasureScope("gallery-load");
await LoadGalleryAsync();
// scope disposes → after snapshot fires automatically
```

### What gets measured

| Field | Source | What it tells you |
|---|---|---|
| `managed.heapBytes` | `GC.GetTotalMemory(false)` | C# objects alive on the heap |
| `managed.gen0Collections` | `GC.CollectionCount(0)` | Short-lived object pressure |
| `managed.gen1Collections` | `GC.CollectionCount(1)` | Medium-lived object pressure |
| `managed.gen2Collections` | `GC.CollectionCount(2)` | Full GC pressure — rising with no heap drop = confirmed leak |
| `native.workingSetBytes` | `Environment.WorkingSet` | Total OS-committed RAM including native allocations |

### Trigger reasons

| Reason | Meaning |
|---|---|
| `SustainedGrowth` | Heap grew on N consecutive samples without recovery |
| `AbsoluteThreshold` | Heap crossed the MB ceiling configured in the dashboard |
| `Manual` | Developer called `CaptureHeapAsync()` or used `MeasureScope` |
| `SessionEnd` | Final snapshot when app goes to background |

---

## Self-hosted API endpoints

Set `SelfHostedUrl` to your base path — the SDK appends `/errors/capture` and `/memory/capture` automatically.

```csharp
op.SelfHostedUrl = "https://logs.yourcompany.com/api";
// errors → https://logs.yourcompany.com/api/errors/capture
// memory → https://logs.yourcompany.com/api/memory/capture

op.SelfHostedUrl = "https://logs.yourcompany.com/api/v1";
// errors → https://logs.yourcompany.com/api/v1/errors/capture
// memory → https://logs.yourcompany.com/api/v1/memory/capture
```

### Error capture

```
POST {SelfHostedUrl}/errors/capture
Content-Type: application/json
X-Api-Key: your_api_key

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

### Memory snapshot capture

```
POST {SelfHostedUrl}/memory/capture
Content-Type: application/json
X-Api-Key: your_api_key

{
  "appName": "MyApp",
  "projectId": "your_project_id_here",
  "sessionId": "d82bca4b7f",
  "capturedAt": "2026-04-10T08:22:41Z",
  "triggerReason": "SustainedGrowth",
  "managed": {
    "heapBytes": 148897792,
    "gen0Collections": 4,
    "gen1Collections": 2,
    "gen2Collections": 3
  },
  "native": {
    "workingSetBytes": 250609664
  },
  "device": {
    "platform": "Android",
    "osVersion": "14",
    "deviceModel": "Pixel 7",
    "appVersion": "1.4.0"
  },
  "tags": ["after-gallery-load"]
}
```

**Expected responses:**

| Status | Meaning |
|---|---|
| `202 Accepted` | Snapshot received and stored |
| `400 Bad Request` | Missing `projectId` or `sessionId` |
| `401 Unauthorized` | Invalid or missing API key — snapshot discarded |
| `429 Too Many Requests` | Monthly limit reached — snapshot dropped |

> Memory snapshots are not queued to disk — stale snapshots have no diagnostic value. If delivery fails the snapshot is dropped silently.

---

## Configuration options

### SaaS

```csharp
builder.UseLogsHive(op =>
{
    op.Mode         = LogsHiveMode.SaaS;
    op.ApiKey       = "lh_your_api_key_here";
    op.ProjectId    = "your_project_id_here";
    op.AppName      = "MyApp";
    op.SendToServer = true;
});
```

### Self-hosted

```csharp
builder.UseLogsHive(op =>
{
    op.Mode          = LogsHiveMode.SelfHosted;
    op.SelfHostedUrl = "https://logs.yourcompany.com/api";
    op.ProjectId     = "your_project_id_here";
    op.AppName       = "MyApp";
    op.SendToServer  = true;
});
```

### Local development (nothing sent to server)

```csharp
builder.UseLogsHive(op =>
{
    op.Mode                      = LogsHiveMode.SaaS;
    op.ApiKey                    = "lh_your_api_key_here";
    op.ProjectId                 = "your_project_id_here";
    op.AppName                   = "MyApp";
    op.SendToServer              = false;
    op.EnableLocalConsoleLogging = true;
});
```

When `SendToServer` is `false` the SDK is fully active — it processes all events, logs them to the Output window, but sends nothing to the server. Nothing leaves the device.

### Switch automatically by build

```csharp
builder.UseLogsHive(op =>
{
    op.Mode      = LogsHiveMode.SaaS;
    op.ApiKey    = "lh_your_api_key_here";
    op.ProjectId = "your_project_id_here";
    op.AppName   = "MyApp";

#if DEBUG
    op.SendToServer              = false;
    op.EnableLocalConsoleLogging = true;
#else
    op.SendToServer           = true;
    op.EnableMemoryMonitoring = true;
#endif
});
```

---

## Options reference

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Mode` | `LogsHiveMode` | Yes | `SaaS` | `SaaS` or `SelfHosted` |
| `SendToServer` | `bool` | Yes | `false` | When true, events are sent to the API. When false, SDK runs locally only — nothing leaves the device |
| `ApiKey` | `string` | SaaS only | `null` | Your API key |
| `ProjectId` | `string` | Yes | — | Routes events to the correct project |
| `AppName` | `string` | Yes | `UnknownApp` | Human-readable name shown in the dashboard |
| `SelfHostedUrl` | `string` | SelfHosted only | `null` | Your base URL — SDK appends endpoint paths automatically |
| `EnableLocalConsoleLogging` | `bool` | No | `false` | Writes all SDK activity to the Output window. Independent of `SendToServer` |
| `Tags` | `Dictionary<string, string>` | No | `{}` | Global tags attached to every captured event |
| `EnableMemoryMonitoring` | `bool` | No | `false` | Enables automatic background memory leak detection |
| `MemoryMonitoringIntervalSeconds` | `int` | No | `30` | Sampling interval. Only validated when `EnableMemoryMonitoring` is `true`. Minimum: 10s |

---

## API reference

### `LogsHiveClient`

| Method | Description |
|---|---|
| `Log(message)` | Sends a free-form log message. Fire-and-forget |
| `Capture(ex)` | Captures an exception. Fire-and-forget |
| `CaptureAsync(ex)` | Captures an exception. Awaitable |
| `CaptureHeapAsync(tags)` | Manually captures a heap snapshot with optional labels |
| `MeasureScope(tag)` | Returns a scope that captures heap before and after an operation |
| `FlushAsync()` | Flushes the offline queue. Call on `OnStart` / `OnResume` |
| `GetQueueCountAsync()` | Returns the number of events pending in the offline queue |
| `IsInitialized` | Returns `true` if the SDK has been initialized |

---

## Offline support

| Scenario | Behaviour |
|---|---|
| Successful send (2xx) | Event delivered, nothing queued |
| `401 Unauthorized` | Event discarded silently |
| `429 Too Many Requests` | Event queued to disk |
| No network | Event queued to disk |
| App restart / resume | Queue flushed automatically via `FlushAsync()` |

> Memory snapshots are **not queued** — stale snapshots carry no diagnostic value and are dropped silently on failure.

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