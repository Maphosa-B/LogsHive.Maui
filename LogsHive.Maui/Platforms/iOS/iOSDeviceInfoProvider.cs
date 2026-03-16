#if IOS
using LogsHive.Maui.Services;
using UIKit;

namespace LogsHive.Maui.Platforms.iOS;

/// <summary>
/// iOS-specific device info provider.
/// </summary>
internal sealed class iOSDeviceInfoProvider : IDeviceInfoProvider
{
    public string Platform => "iOS";

    public string OperatingSystem =>
        $"iOS {UIDevice.CurrentDevice.SystemVersion}";

    public string AppVersion =>
        AppInfo.Current.VersionString ?? "0.0.0";

    public string DeviceModel =>
        UIDevice.CurrentDevice.Model ?? DeviceInfo.Current.Model;
}
#endif
