using System.Text;
using ZYC.CoreToolkit;
using ZYC.CoreToolkit.Dotnet;

namespace ZYC.TilePyramid.Build.NuGet;

internal class Program
{
    private static async Task Main()
    {
        IOTools.SetCurrentDirectoryToEntryScriptFileDirectory();


        await IOTools.ClearPathAsync(BuildEnvironment.OutputPath);


        var tempSlnFile = BuildEnvironment.TempSlnFile;
        IOTools.DeleteFileIfExists(tempSlnFile);

        var csprojFiles = GetCsprojFilePaths(BuildEnvironment.RootFolder);
        ProjectTools.GenerateSln(csprojFiles, tempSlnFile);

        await CommandTools.ExecuteCommandAsync($"dotnet build {BuildEnvironment.EntryProjectCsprojPath} -c release");
        await CommandTools.ExecuteCommandAsync($"dotnet build {tempSlnFile} -c release");

        IOTools.DeleteFileIfExists(tempSlnFile);


        IOTools.DeleteDirectoryIfExists(BuildEnvironment.LogPath);
        IOTools.DeleteDirectoryIfExists(BuildEnvironment.NuGetCachePath);


        foreach (var csprojFile in csprojFiles)
        {
            await DotnetNuGetTools.PackProjectNoRestoreNoBuildAsync(
                csprojFile,
                BuildEnvironment.OutputPath,
                BuildEnvironment.BuildVersion);
        }


        var nupkgFiles = Directory.GetFiles(BuildEnvironment.OutputPath, "*.nupkg");
        foreach (var nupkgFile in nupkgFiles)
        {
            File.Delete(nupkgFile);
        }

        foreach (var csprojFile in csprojFiles)
        {
            await DotnetNuGetTools.PackProjectNoRestoreNoBuildAsync(
                csprojFile,
                BuildEnvironment.OutputPath,
                BuildEnvironment.BuildVersion);
        }


#if PUBLISH_NUGET_ORG
        var apiKey = await NuGetTrustedPublishingTools.GetApiKeyAsync("ZhuJianYun");
        await DotnetNuGetTools.PushNuGetAsync(
            BuildEnvironment.OutputPath,
            "https://api.nuget.org/v3/index.json",
            apiKey,
            null,
            false);
#endif

        //TODO-zyc Checkout ZYC.TilePyramid.Core and build

    }

    private static string[] GetCsprojFilePaths(string projectRootFolder)
    {
        var allCsprojFiles = IOTools.GetAllCsprojFiles(projectRootFolder);

        var result = new List<string>();

        foreach (var file in allCsprojFiles)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("<IgnoreFromPublish>true</IgnoreFromPublish>"))
            {
                continue;
            }

            result.Add(file);
        }

        return result.ToArray();
    }
}