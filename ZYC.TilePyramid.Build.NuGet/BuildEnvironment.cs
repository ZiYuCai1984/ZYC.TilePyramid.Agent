using ZYC.CoreToolkit;

namespace ZYC.TilePyramid.Build.NuGet;

public static class BuildEnvironment
{
    public static string RootFolder => GetProjectRootFolderPath();

    public static string OutputPath => Path.Combine(RootFolder, "_bin", "app");

    public static string TempSlnFile => Path.Combine(RootFolder, "_temp.sln");

    public static string LogPath => Path.Combine(OutputPath, "Logs");

    public static string NuGetCachePath => Path.Combine(OutputPath, ".cache");

    public static string BuildProjectPath => Path.Combine(RootFolder, "ZYC.TilePyramid.Build.NuGet");

    public static string EntryProjectPath => Path.Combine(RootFolder, "Entry");

    public static string EntryProjectCsprojPath => Path.Combine(EntryProjectPath, "Entry.csproj");

    public static string BuildVersion => "1.0.1";


    public static string GetProjectRootFolderPath()
    {
        var directoryPath = IOTools.GetCallerDirectoryPath();
        var directoryInfo = new DirectoryInfo(directoryPath);
        var path = directoryInfo.Parent!.FullName;
        return path;
    }
}