using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Models;
using DsLauncherService.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DsLauncherService.Services;

class InstallationService(
    CacheService cache,
    DsLauncherClientFactory clientFactory,
    IServiceProvider serviceProvider,
    IOptions<DsLauncherOptions> launcherOptions)
{
    readonly List<UpdateStatus> updates = [];

    public List<UpdateStatus> GetCurrentlyBeingInstalled() => updates;

    public void RegisterUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToVersion(installed, dstPackageGuid, ct)), ct);

    public void RegisterUpdateToLatest(Installed installed, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToLatest(installed, ct)), ct);

    public void RegisterFullInstall(Guid productGuid, Library library, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(productGuid, () => DoFullInstall(productGuid, library, ct)), ct);

    async Task DelegateInstallTask(Guid productGuid, Func<Task<bool>> task)
    {
        if (GetCurrentlyBeingInstalled().Any(x => x.ProductGuid == productGuid)) throw new();

        updates.Add(new() { ProductGuid = productGuid, Percentage = 0 });
        var result = false;
        try
        {
            result = await task();
        }
        catch {}
        
        updates.Remove(updates.First(x => x.ProductGuid == productGuid));
    }

    async Task<bool> DoFullInstall(Guid productGuid, Library library, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        using var stream = new MemoryStream();
        var client = new DsLauncherNdibApiClient(launcherOptions);
        var latestPackageGuid = await client.DownloadWhole(cache.GetToken() ?? throw new(), productGuid, GetPlatform(), stream, (o, progress) => SetUpdateState(productGuid, UpdateStep.Download, progress));
        if (latestPackageGuid == default) throw new();

        await repo.InsertAsync(new()
        {
            LibraryId = library.Id,
            ProductGuid = productGuid,
            PackageGuid = latestPackageGuid
        }, ct);

        var productPath = GetProductPath(productGuid, library.Path);
        Install(productGuid, productPath, stream);
        var verified = await VerifyInstallation(productGuid, productPath, latestPackageGuid, ct);

        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        var sourceGuid = installed.PackageGuid;
        using var stream = new MemoryStream();
        var client = new DsLauncherNdibApiClient(launcherOptions);
        await client.ChangeToVersion(cache.GetToken() ?? throw new(), sourceGuid, dstPackageGuid, GetPlatform(), stream, (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));

        installed.PackageGuid = dstPackageGuid;
        await repo.UpdateAsync(installed, ct);

        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        Install(installed.PackageGuid, productPath, stream);
        RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, dstPackageGuid, ct));
        var verified = await VerifyInstallation(installed.ProductGuid, productPath, dstPackageGuid, ct);
        
        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoUpdateToLatest(Installed installed, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();

        var sourceGuid = installed.PackageGuid;
        using var stream = new MemoryStream();
        var client = new DsLauncherNdibApiClient(launcherOptions);
        var latestPackageGuid = await client.UpdateToLatest(cache.GetToken() ?? throw new(), sourceGuid, GetPlatform(), stream, (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));
        if (latestPackageGuid == default) throw new();
        
        installed.PackageGuid = latestPackageGuid;
        await repo.UpdateAsync(installed, ct);

        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        Install(installed.PackageGuid, productPath, stream);
        RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, latestPackageGuid, ct));
        var verified = await VerifyInstallation(installed.ProductGuid, productPath, latestPackageGuid, ct);
        
        await repo.CommitAsync(ct);
        return verified;
    }

    static string GetProductPath(Guid productGuid, string library) => Path.Combine(library, productGuid.ToString());

    void Install(Guid productGuid, string productPath, Stream response)
    {
        var extractDirPath = GenerateTempPath();
        ZipFile.ExtractToDirectory(response, extractDirPath);
        if (!Directory.Exists(productPath))
            Directory.CreateDirectory(productPath);

        CopyAndReplaceFiles(productGuid, extractDirPath, productPath);
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

    async Task<bool> VerifyInstallation(Guid productGuid, string productPath, Guid packageGuid, CancellationToken ct)
    {
        var remoteHash = await GetClient().Ndib_GetVerificationHashAsync(packageGuid, GetPlatform(), ct);
        var localHash = GetFileHashes(productGuid, productPath, remoteHash.Keys.ToList());
        return remoteHash.Keys.Count == localHash.Keys.Count && remoteHash.Keys.All(k => localHash.ContainsKey(k) && localHash[k] == remoteHash[k]);
    }

    Dictionary<string, string> GetFileHashes(Guid productGuid, string directoryPath, List<string> paths)
    {
        var fileHashes = new Dictionary<string, string>();
        var i = 0;
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(directoryPath, path);
            fileHashes[path] = ComputeFileHash(fullPath);  
            SetUpdateState(productGuid, UpdateStep.Verification, ++i / (float)paths.Count);
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

    void CopyAndReplaceFiles(Guid productGuid, string src, string dst)
    {
        var files = Directory.GetFiles(src, "*", SearchOption.AllDirectories).ToList();
        var i = 0;
        foreach (var sourceFilePath in files)
        {
            var relativePath = Path.GetRelativePath(src, sourceFilePath);
            var targetFilePath = Path.Combine(dst, relativePath);

            var targetFileDir = Path.GetDirectoryName(targetFilePath);
            if (targetFileDir != null && !Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir);

            File.Copy(sourceFilePath, targetFilePath, true);
            SetUpdateState(productGuid, UpdateStep.Install, (++i) / (float)files.Count);
        }
    }

    DsLauncherClient GetClient()
    {
        var token = cache.GetToken() ?? throw new();
        return clientFactory.CreateClient(token);
    }

    void SetUpdateState(Guid productGuid, UpdateStep step, float percentage)
    {
        var update = updates.FirstOrDefault(x => x.ProductGuid == productGuid);
        if (update != null)
        {
            if (step != update.Step || percentage >= update.Percentage)
                update.Percentage = percentage;

            update.Step = step;
        }
        Console.WriteLine($"{productGuid} {percentage} {step}");
    }
}