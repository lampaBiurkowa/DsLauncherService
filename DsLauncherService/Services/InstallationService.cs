using System.IO.Compression;
using System.Runtime.InteropServices;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Models;
using DsLauncherService.Storage;

namespace DsLauncherService.Services;

class InstallationService(
    CacheService cache,
    Repository<Installed> repo,
    DsLauncherClientFactory clientFactory)
{
    const string LATEST_PACKAGE_GUID_HEADER = "Latest-Package";

    public List<UpdateStatus> Updates { get; set; } = [];

    public void RegisterFullInstall(Guid productGuid, Library library, CancellationToken ct) =>
        Task.Run(() => DoFullInstall(productGuid, library, ct), ct);

    public void RegisterUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct) =>
        Task.Run(() => DoUpdateToVersion(installed, dstPackageGuid, ct), ct);

    public void RegisterUpdateToLatest(Installed installed, CancellationToken ct) =>
        Task.Run(() => DoUpdateToLatest(installed, ct), ct);

    async Task DoFullInstall(Guid productGuid, Library library, CancellationToken ct)
    {
        var file = await GetClient().Ndib_GetWholeAsync(productGuid, GetPlatform(), ct);
        var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        if (!parseResult) throw new();

        await repo.InsertAsync(new()
        {
            LibraryId = library.Id,
            ProductGuid = productGuid,
            PackageGuid = latestPackageGuid
        }, ct);

        await Install(productGuid, library.Path, file.Stream, ct);
        await repo.CommitAsync(ct);
    }

    async Task DoUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct)
    {
        var file = await GetClient().Ndib_GetUpdateAsync(installed.PackageGuid, dstPackageGuid, GetPlatform(), ct);

        installed.PackageGuid = dstPackageGuid;
        await repo.UpdateAsync(installed, ct);

        await Install(installed.PackageGuid, installed.Library!.Path, file.Stream, ct);
        await repo.CommitAsync(ct);
    }

    async Task DoUpdateToLatest(Installed installed, CancellationToken ct)
    {
        var file = await GetClient().Ndib_GetUpdate2Async(installed.PackageGuid, GetPlatform(), ct);
        var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        if (!parseResult) throw new();

        installed.PackageGuid = latestPackageGuid;
        await repo.UpdateAsync(installed, ct);

        await Install(installed.PackageGuid, installed.Library!.Path, file.Stream, ct);
        await repo.CommitAsync(ct);
    }

    static async Task Install(Guid productGuid, string library, Stream response, CancellationToken ct)
    {
        var zipPath = $"{GenerateTempPath()}.zip";
        var extractDirPath = GenerateTempPath();
        using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        await response.CopyToAsync(fileStream, ct);
        ZipFile.ExtractToDirectory(zipPath, extractDirPath);
        File.Delete(zipPath);
        var gamePath = Path.Combine(library, productGuid.ToString());
        if (!Directory.Exists(gamePath))
            Directory.CreateDirectory(gamePath);

        CopyAndReplaceFiles(extractDirPath, gamePath);
    }

    static Platform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Platform.Win;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Platform.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Platform.Mac;

        throw new();
    }

    static string GenerateTempPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    static void CopyAndReplaceFiles(string src, string dst)
    {
        foreach (var sourceFilePath in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(src, sourceFilePath);
            var targetFilePath = Path.Combine(dst, relativePath);

            var targetFileDir = Path.GetDirectoryName(targetFilePath);
            if (targetFileDir != null && !Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir);

            File.Copy(sourceFilePath, targetFilePath, true);
        }
    }

    DsLauncherClient GetClient()
    {
        var token = cache.GetToken() ?? throw new();
        return clientFactory.CreateClient(token);
    }
}