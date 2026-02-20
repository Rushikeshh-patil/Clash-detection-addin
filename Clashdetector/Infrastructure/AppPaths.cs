using System.IO;

namespace Clashdetector.Infrastructure;

public static class AppPaths
{
    public static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClashDetector");

    public static string SettingsFilePath => Path.Combine(RootDirectory, "settings.json");

    public static string LogsDirectory => Path.Combine(RootDirectory, "logs");
}
