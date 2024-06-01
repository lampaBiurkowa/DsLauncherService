using System.IO.Compression;
using System.Runtime.InteropServices;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("install")]
internal class InstallCommandHandler(
    Repository<Installed> installedRepo,
    Repository<Library> libraryRepo,
    // DsLauncherClientFactory clientFactory,
    InstallationService installationService
    // CacheService cache
    ) : ICommandHandler
{ 
    // const string LATEST_PACKAGE_GUID_HEADER = "Latest-Package";

    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        // var token = cache.GetToken();
        var productGuid = args.Get<Guid>("productGuid");
        args.TryGet<Guid>("packageGuid", out var packageGuid);
        args.TryGet<string>("library", out var libraryName);

        if (productGuid == default) return Command.Empty;

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!],
            ct: ct)).FirstOrDefault();

        if (currentInstallation == null)
        {
            var passedLibrary = (await libraryRepo.GetAll(restrict: x => x.Path == libraryName, ct: ct)).FirstOrDefault() ?? throw new();
            installationService.RegisterFullInstall(productGuid, passedLibrary, ct);
        }
        else if (packageGuid != default)
            installationService.RegisterUpdateToVersion(currentInstallation, packageGuid, ct);
        else
            installationService.RegisterUpdateToLatest(currentInstallation, ct);



        // var client = clientFactory.CreateClient(token);
        // var platform = GetPlatform();
        // var sourceGuid = currentInstallation?.PackageGuid;

        // FileResponse file;
        // string? library = currentInstallation?.Library?.Path;
        // if (packageGuid != default && currentInstallation != null)
        // {
        //     if (sourceGuid == null) return Command.Empty;
        //     file = await client.Ndib_GetUpdateAsync((Guid)sourceGuid, packageGuid, platform, ct);

        //     currentInstallation.PackageGuid = packageGuid;
        //     await installedRepo.UpdateAsync(currentInstallation, ct);
        // }
        // else if (sourceGuid != null && currentInstallation != null)
        // {
        //     file = await client.Ndib_GetUpdate2Async((Guid)sourceGuid, platform, ct);
        //     var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        //     if (!parseResult) return Command.Empty;

        //     currentInstallation.PackageGuid = latestPackageGuid;
        //     await installedRepo.UpdateAsync(currentInstallation, ct);
        // }
        // else if (!string.IsNullOrWhiteSpace(libraryName))
        // {
        //     var passedLibrary = (await libraryRepo.GetAll(restrict: x => x.Path == libraryName, ct: ct)).FirstOrDefault();
        //     if (passedLibrary == null)
        //         return Command.Empty;
            
        //     file = await client.Ndib_GetWholeAsync(productGuid, platform, ct);
        //     var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        //     if (!parseResult) return Command.Empty;

        //     library = libraryName;
        //     await installedRepo.InsertAsync(new()
        //     {
        //         LibraryId = passedLibrary.Id,
        //         ProductGuid = productGuid,
        //         PackageGuid = latestPackageGuid
        //     }, ct);
        // }
        // else
        //     return Command.Empty;
        
        // if (string.IsNullOrWhiteSpace(library)) return Command.Empty;

        // var zipPath = $"{GenerateTempPath()}.zip";
        // var extractDirPath = GenerateTempPath();
        // using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        // await file.Stream.CopyToAsync(fileStream, ct);
        // ZipFile.ExtractToDirectory(zipPath, extractDirPath);
        // File.Delete(zipPath);
        // var gamePath = Path.Combine(library, productGuid.ToString());
        // if (!Directory.Exists(gamePath))
        //     Directory.CreateDirectory(gamePath);

        // CopyAndReplaceFiles(extractDirPath, gamePath);
        // await installedRepo.CommitAsync(ct);

        return Command.Empty;
    }

    // static Platform GetPlatform()
    // {
    //     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    //         return Platform.Win;
    //     else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    //         return Platform.Linux;
    //     else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    //         return Platform.Mac;

    //     return default; //TODO log/exception
    // }

    // static string GenerateTempPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    // static void CopyAndReplaceFiles(string src, string dst)
    // {
    //     foreach (var sourceFilePath in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
    //     {
    //         var relativePath = Path.GetRelativePath(src, sourceFilePath);
    //         var targetFilePath = Path.Combine(dst, relativePath);

    //         var targetFileDir = Path.GetDirectoryName(targetFilePath);
    //         if (targetFileDir != null && !Directory.Exists(targetFileDir))
    //             Directory.CreateDirectory(targetFileDir);

    //         File.Copy(sourceFilePath, targetFilePath, true);
    //     }
    // }
}
