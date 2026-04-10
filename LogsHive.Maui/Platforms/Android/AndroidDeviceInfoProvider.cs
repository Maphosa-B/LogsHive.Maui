#if ANDROID
using Android.OS;
using LogsHive.Maui.Interfaces;

namespace LogsHive.Maui.Platforms.Android;

/// <summary>
/// Android-specific device info provider.
/// Falls back gracefully when APIs are unavailable.
/// </summary>
internal sealed class AndroidDeviceInfoProvider : IDeviceInfoProvider
{
    public string Platform => "Android";

    public string OperatingSystem =>
        $"Android {Build.VERSION.Release} (API {(int)Build.VERSION.SdkInt})";

    public string AppVersion =>
        AppInfo.Current.VersionString ?? "0.0.0";

    public string DeviceModel =>
        $"{Build.Manufacturer} {Build.Model}".Trim();
}
#endif