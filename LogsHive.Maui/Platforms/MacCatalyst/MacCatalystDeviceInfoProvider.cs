#if MACCATALYST
using Foundation;
using LogsHive.Maui.Interfaces;

namespace LogsHive.Maui.Platforms.MacCatalyst;

/// <summary>
/// Mac Catalyst-specific device info provider.
/// </summary>
internal sealed class MacCatalystDeviceInfoProvider : IDeviceInfoProvider
{
    public string Platform => "MacCatalyst";

    public string OperatingSystem =>
        $"macOS {NSProcessInfo.ProcessInfo.OperatingSystemVersionString}";

    public string AppVersion =>
        AppInfo.Current.VersionString ?? "0.0.0";

    public string DeviceModel =>
        $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
}
#endif
