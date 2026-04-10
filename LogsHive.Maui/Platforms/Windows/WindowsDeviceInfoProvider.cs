#if WINDOWS
using LogsHive.Maui.Interfaces;

namespace LogsHive.Maui.Platforms.Windows;

/// <summary>
/// Windows-specific device info provider.
/// </summary>
internal sealed class WindowsDeviceInfoProvider : IDeviceInfoProvider
{
    public string Platform => "Windows";

    public string OperatingSystem =>
        $"Windows {System.Environment.OSVersion.Version}";

    public string AppVersion =>
        AppInfo.Current.VersionString ?? "0.0.0";

    public string DeviceModel =>
        $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
}
#endif
