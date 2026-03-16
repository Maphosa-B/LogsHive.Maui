namespace LogsHive.Maui.Services;

/// <summary>
/// Default implementation of <see cref="IDeviceInfoProvider"/> using
/// Microsoft.Maui.Devices APIs.
/// </summary>
internal sealed class DefaultDeviceInfoProvider : IDeviceInfoProvider
{
    public string Platform => DeviceInfo.Current.Platform.ToString();

    public string OperatingSystem =>
        $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";

    public string AppVersion =>
        AppInfo.Current.VersionString ?? "0.0.0";

    public string DeviceModel =>
        $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
}
