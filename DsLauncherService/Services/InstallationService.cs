using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Communication;
using DsLauncherService.Models;
using DsLauncherService.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DsLauncherService.Services;

class InstallationService(
    CacheService cache,
    DsLauncherClientFactory clientFactory,
    IServiceProvider serviceProvider)
{
    const string LATEST_PACKAGE_GUID_HEADER = "Latest-Package";

    readonly List<UpdateStatus> updates = [];

    public List<UpdateStatus> GetCurrentlyBeingInstalled() => updates;

    public void RegisterUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, DoUpdateToVersion(installed, dstPackageGuid, ct)), ct);

    public void RegisterUpdateToLatest(Installed installed, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, DoUpdateToLatest(installed, ct)), ct);

    public void RegisterFullInstall(Guid productGuid, Library library, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(productGuid, DoFullInstall(productGuid, library, ct)), ct);

    async Task DelegateInstallTask(Guid productGuid, Task<bool> task)
    {
        if (GetCurrentlyBeingInstalled().Any(x => x.ProductGuid == productGuid)) throw new();

        updates.Add(new() { ProductGuid = productGuid, Percentage = 0 });
        var result = false;
        try
        {
            result = await task;
        }
        catch {}
        
        updates.Remove(updates.First(x => x.ProductGuid == productGuid));
        var cmd = new Command("install-completed");
        cmd.Args.Add("succeeded", result);
    }

    async Task<bool> DoFullInstall(Guid productGuid, Library library, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        var file = await GetClient().Ndib_GetWholeAsync(productGuid, GetPlatform(), ct);
        var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        if (!parseResult) throw new();

        await repo.InsertAsync(new()
        {
            LibraryId = library.Id,
            ProductGuid = productGuid,
            PackageGuid = latestPackageGuid
        }, ct);

        var productPath = GetProductPath(productGuid, library.Path);
        Install(productPath, file.Stream);
        var verified = await VerifyInstallation(productPath, latestPackageGuid, ct);

        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        var sourceGuid = installed.PackageGuid;
        var file = await GetClient().Ndib_GetUpdateAsync(sourceGuid, dstPackageGuid, GetPlatform(), ct);

        installed.PackageGuid = dstPackageGuid;
        await repo.UpdateAsync(installed, ct);

        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        Install(productPath, file.Stream);
        RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, dstPackageGuid, ct));
        var verified = await VerifyInstallation(productPath, dstPackageGuid, ct);
        
        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoUpdateToLatest(Installed installed, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        var sourceGuid = installed.PackageGuid;
        var file = await GetClient().Ndib_GetUpdate2Async(sourceGuid, GetPlatform(), ct);
        var parseResult = Guid.TryParse(file.Headers.FirstOrDefault(x => x.Key == LATEST_PACKAGE_GUID_HEADER).Value.First(), out var latestPackageGuid);
        if (!parseResult) throw new();

        installed.PackageGuid = latestPackageGuid;
        await repo.UpdateAsync(installed, ct);

        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        Install(productPath, file.Stream);
        RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, latestPackageGuid, ct));
        var verified = await VerifyInstallation(productPath, latestPackageGuid, ct);
        
        await repo.CommitAsync(ct);
        return verified;
    }

    static string GetProductPath(Guid productGuid, string library) => Path.Combine(library, productGuid.ToString());

    static void Install(string productPath, Stream response)
    {
        var extractDirPath = GenerateTempPath();
        ZipFile.ExtractToDirectory(response, extractDirPath);
        if (!Directory.Exists(productPath))
            Directory.CreateDirectory(productPath);

        CopyAndReplaceFiles(extractDirPath, productPath);
    }

    static void RemoveFiles(string productPath, List<string> paths)
    {
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(productPath, path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }

    async Task<List<string>> GetFilesToRemove(Guid srcPackageGuid, Guid dstPackageGuid, CancellationToken ct)
    {
        var srcTask = GetClient().Ndib_GetVerificationHashAsync(srcPackageGuid, GetPlatform(), ct);
        var dstTask = GetClient().Ndib_GetVerificationHashAsync(dstPackageGuid, GetPlatform(), ct);
        await Task.WhenAll(srcTask, dstTask);

        var src = srcTask.Result;
        var dst = dstTask.Result;

        return src.Keys.Except(dst.Keys).ToList();
    }

    async Task<bool> VerifyInstallation(string productPath, Guid packageGuid, CancellationToken ct)
    {
        var remoteHash = await GetClient().Ndib_GetVerificationHashAsync(packageGuid, GetPlatform(), ct);
        var localHash = GetFileHashes(productPath, remoteHash.Keys.ToList());
        return remoteHash.Keys.Count == localHash.Keys.Count && remoteHash.Keys.All(k => localHash.ContainsKey(k) && localHash[k] == remoteHash[k]);
    }

    static Dictionary<string, string> GetFileHashes(string directoryPath, List<string> paths)
    {
        var fileHashes = new Dictionary<string, string>();

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(directoryPath, path);
            fileHashes[path] = ComputeFileHash(fullPath);  
        } 

        return fileHashes;
    }

    static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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