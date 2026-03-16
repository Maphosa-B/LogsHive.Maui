namespace LogsHive.Maui.Services;

/// <summary>
/// Abstracts device/environment metadata so platform implementations
/// can be swapped or mocked in tests.
/// </summary>
internal interface IDeviceInfoProvider
{
    string Platform { get; }
    string OperatingSystem { get; }
    string AppVersion { get; }
    string DeviceModel { get; }
}
