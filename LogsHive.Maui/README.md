# LogsHive.Maui

![Bee Logo](https://i.imgur.com/ggposCD.png)

MAUI-native crash, error monitoring, and memory leak detection for .NET MAUI apps. Capture exceptions, log events, monitor memory in production, with an SDK built specifically for MAUI.

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

The SaaS API is hosted at `https://logs-hive-api.conversion-hive.com`. You only need your API key and project ID, the URL is handled automatically by the SDK when `Mode = LogsHiveMode.SaaS`.

## Pricing

LogsHive is in early access, and right now that means one thing: it's free to use, full stop. Error capture, the dashboard, and memory leak monitoring (still under active development, see below) are all unlocked while we build this out with real MAUI developers.

Formal pricing tiers are on the way, shaped by what early users actually need rather than a guess. Get in now and you get the SDK for free during that window, with no card required.

Sign up and get your API key and project ID at [Logs Hive](https://logs-hive.conversion-hive.com/register).

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

// Log with tags, merged with your global Tags, per-call tag wins on conflict
LogsHiveClient.Log("Payment retried", new() { ["Flow"] = "Checkout" });

// Capture a caught exception (fire-and-forget)
try
{
    await LoadDataAsync();
}
catch (Exception ex)
{
    LogsHiveClient.Capture(ex);
}

// Capture with tags, still fire-and-forget
try
{
    await LoadDataAsync();
}
catch (Exception ex)
{
    LogsHiveClient.Capture(ex, new() { ["Screen"] = "HomePage" });
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

`Log`, `Capture`, and `CaptureAsync` all accept an optional `Dictionary<string, string>? tags` parameter. Per-call tags are merged with the global `op.Tags` you set at startup, if a key exists in both, the per-call value wins; every other global tag is still attached.

---

## Memory leak monitoring

> **Still under active development.** Capture is live, snapshots are validated, stored, and queryable via the API today. What's missing is the dashboard UI to actually view them. If you're on **self-hosted**, you can query your own database or API directly, so this is usable right now. If you're on **SaaS**, snapshots are being captured and stored, but there's no dashboard view to see them yet, that's next up. Detection thresholds and sensitivity are also still being tuned based on real-world feedback.

LogsHive can automatically detect memory leaks in production, on real user devices, across all platforms, without requiring a profiler or local reproduction.

### How it works

A background timer samples managed heap (`GC.GetTotalMemory`) and native working set (`Environment.WorkingSet`) at a configurable interval. If heap grows across N consecutive samples without recovering, LogsHive sends a memory snapshot to your API and flags the session as leaking. Thresholds and sensitivity are configured in the LogsHive dashboard, no code changes needed to tune them.

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

`MemoryMonitoringIntervalSeconds` is only validated when `EnableMemoryMonitoring` is `true`, setting it freely while monitoring is disabled will not cause an error.

### Manual only, no background monitor

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
// scope disposes → snapshots fire automatically, auto-labelled
// "before-gallery-load" and "after-gallery-load"
```

### What gets measured

| Field | Source | What it tells you |
|---|---|---|
| `managed.heapBytes` | `GC.GetTotalMemory(false)` | C# objects alive on the heap |
| `managed.gen0Collections` | `GC.CollectionCount(0)` | Short-lived object pressure |
| `managed.gen1Collections` | `GC.CollectionCount(1)` | Medium-lived object pressure |
| `managed.gen2Collections` | `GC.CollectionCount(2)` | Full GC pressure, rising with no heap drop = confirmed leak |
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

Set `SelfHostedUrl` to your base path, the SDK appends `/errors/capture` and `/memory/capture` automatically.

```csharp
op.SelfHostedUrl = "https://logs.yourcompany.com/api";
// errors → https://logs.yourcompany.com/api/errors/capture
// memory → https://logs.yourcompany.com/api/memory/capture

op.SelfHostedUrl = "https://logs.yourcompany.com/api/v1";
// errors → https://logs.yourcompany.com/api/v1/errors/capture
// memory → https://logs.yourcompany.com/api/v1/memory/capture
```

Every payload includes an `installationId` and a `sessionId`:

- **`installationId`** is generated once per device install and persisted in local app preferences, it stays the same across app restarts, so it identifies "this install," not "this launch."
- **`sessionId`** is generated fresh each time the app starts and is shared by every event and memory snapshot sent during that run, use it to group everything that happened in a single session together.

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

  "installationId": "a8f93c2e1b7f4d91",
  "sessionId": "7ac92fd301",

  "tags": {
    "Environment": "Production",
    "UserFlow": "Checkout"
  }
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
| `401 Unauthorized` | Invalid or missing API key, snapshot discarded |
| `429 Too Many Requests` | Monthly limit reached, snapshot dropped |

> Memory snapshots are not queued to disk, stale snapshots have no diagnostic value. If delivery fails the snapshot is dropped silently, never retried.

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

When `SendToServer` is `false` the SDK is fully active, it processes all events, logs them to the Output window, but sends nothing to the server. Nothing leaves the device.

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
| `SendToServer` | `bool` | Yes | `false` | When true, events are sent to the API. When false, SDK runs locally only, nothing leaves the device |
| `ApiKey` | `string` | SaaS only | `null` | Your API key |
| `ProjectId` | `string` | Yes | `-` | Routes events to the correct project |
| `AppName` | `string` | Yes | `UnknownApp` | Human-readable name shown in the dashboard |
| `SelfHostedUrl` | `string` | SelfHosted only | `null` | Your base URL, SDK appends endpoint paths automatically |
| `EnableLocalConsoleLogging` | `bool` | No | `false` | Writes all SDK activity to the Output window. Independent of `SendToServer` |
| `Tags` | `Dictionary<string, string>` | No | `{}` | Global tags attached to every captured event. Any tags passed at the call site override a matching key here |
| `EnableMemoryMonitoring` | `bool` | No | `false` | Enables automatic background memory leak detection |
| `MemoryMonitoringIntervalSeconds` | `int` | No | `30` | Sampling interval. Only validated when `EnableMemoryMonitoring` is `true`. Minimum: 10s |

---

## API reference

### `LogsHiveClient`

| Method | Description |
|---|---|
| `Log(message, tags?)` | Sends a free-form log message. Optional tags merge with the global `Tags`, per-call wins. Fire-and-forget |
| `Capture(ex, tags?)` | Captures an exception. Same tag-merge rule as `Log`. Fire-and-forget |
| `CaptureAsync(ex, tags?)` | Captures an exception and awaits delivery before continuing |
| `CaptureHeapAsync(params tags)` | Manually captures a heap snapshot with optional labels |
| `MeasureScope(tag)` | Returns a scope that captures heap on creation and on disposal, auto-prefixed `before-{tag}` / `after-{tag}` |
| `FlushAsync()` | Flushes the offline queue. Call on `OnStart` / `OnResume` |
| `GetQueueCountAsync()` | Returns the number of events pending in the offline queue |
| `IsInitialized` | Returns `true` once `UseLogsHive()` has run |

---

## Offline support

| Scenario | Behaviour |
|---|---|
| Successful send (2xx) | Event delivered, nothing queued |
| `401 Unauthorized` | Event discarded silently, not retried |
| `429 Too Many Requests` | Event queued to disk |
| No network | Event queued to disk |
| App restart / resume | Queue flushed automatically via `FlushAsync()` |

> Memory snapshots are **not queued**, stale snapshots carry no diagnostic value and are dropped silently on failure.

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