using System.Collections.Concurrent;
using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Helpers;
using DsLauncherService.Models;
using DsLauncherService.Storage;
using DsNdib.ApiClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DsLauncherService.Services;

class InstallationService(
    CacheService cache,
    DsLauncherClientFactory launcherClientFactory,
    DsNdibClientFactory ndibClientFactory,
    IServiceProvider serviceProvider,
    IOptions<DsNdibOptions> ndibOptions)
{
    readonly ConcurrentDictionary<Guid, UpdateStatus> updates = [];

    public ConcurrentDictionary<Guid, UpdateStatus> GetCurrentlyBeingInstalled() => updates;

    public void RegisterUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToVersion(installed, dstPackageGuid, ct)), ct);

    public void RegisterUpdateToLatest(Installed installed, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToLatest(installed, ct)), ct);

    public void RegisterFullInstall(Guid productGuid, Library library, Guid packageGuid = default, CancellationToken ct = default) =>
        Task.Run(() => DelegateInstallTask(productGuid, () => DoFullInstall(productGuid, library, packageGuid, ct)), ct);

    public void RegisterInstallationRepair(Installed installed, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoInstallationRepair(installed, ct)), ct);

    public async Task Uninstall(Guid productGuid, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var installedRepo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();
        var recentsRepo = scope.ServiceProvider.GetRequiredService<Repository<Recents>>();

        var installed = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library], ct: ct)).FirstOrDefault() ?? throw new();

        var productPath = GetProductPath(productGuid, installed.Library!.Path);
        await installedRepo.DeleteAsync(installed.Id, ct);
        Directory.Delete(productPath, true);

        var recents = await recentsRepo.GetAll(ct: ct);
        foreach (var r in recents)
            if (r.ProductGuids.Contains(installed.ProductGuid))
            {
                r.ProductGuids.RemoveAll(x => x == installed.ProductGuid);
                await recentsRepo.UpdateAsync(r, ct);
            }

        await installedRepo.CommitAsync(ct);
    }

    async Task DelegateInstallTask(Guid productGuid, Func<Task<bool>> task)
    {
        if (updates.ContainsKey(productGuid)) throw new();

        updates.TryAdd(productGuid, new() { Step = UpdateStep.Download, Percentage = 0 });
        var result = false;
        try
        {
            result = await task();
        }
        catch {}
        
        updates.Remove(productGuid, out var _);
    }
    
    async Task<bool> ExecuteInstallTask(Func<DsNdibDownloadClient, Repository<Installed>, MemoryStream, Task<Installed>> task, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();
        using var stream = new MemoryStream();
        var client = new DsNdibDownloadClient(ndibOptions);

        var installed = await task(client, repo, stream);

        var verified = await VerifyInstallation(installed, ct);
        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoFullInstall(Guid productGuid, Library library, Guid packageGuid = default, CancellationToken ct = default)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            if (packageGuid == default)
                packageGuid = await client.DownloadWhole(cache.GetToken() ?? throw new(), productGuid, PlatformResolver.GetPlatform(), stream, (o, progress) => SetUpdateState(productGuid, UpdateStep.Download, progress));
            else
                await client.DownloadWholeVersion(cache.GetToken() ?? throw new(), productGuid, PlatformResolver.GetPlatform(), packageGuid, stream, (o, progress) => SetUpdateState(productGuid, UpdateStep.Download, progress));

            if (packageGuid == default) throw new();

            var installed = new Installed
            {
                LibraryId = library.Id,
                ProductGuid = productGuid,
                PackageGuid = packageGuid,
                ExePath = await GetExePath(packageGuid, ct)
            };
            await repo.InsertAsync(installed, ct);

            var productPath = GetProductPath(productGuid, library.Path);
            SetUpdateState(productGuid, UpdateStep.Finalizing);
            Install(productPath, stream);
            installed.Library = library; //HZD - puttin after commit as this property is needed later :|
            return installed;
        }, ct);
    }

    async Task<bool> DoUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            var sourceGuid = installed.PackageGuid;
            await client.ChangeToVersion(cache.GetToken() ?? throw new(), sourceGuid, dstPackageGuid, PlatformResolver.GetPlatform(), stream, (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));

            installed.PackageGuid = dstPackageGuid;
            installed.ExePath = await GetExePath(installed.PackageGuid, ct);
            await repo.UpdateAsync(installed, ct);

            var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
            SetUpdateState(installed.ProductGuid, UpdateStep.Finalizing);
            Install(productPath, stream);
            RemoveFiles(productPath, await GetFilesToRemove(installed.ProductGuid, sourceGuid, dstPackageGuid, ct));
            return installed;
        }, ct);
    }

    async Task<bool> DoUpdateToLatest(Installed installed, CancellationToken ct)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            var sourceGuid = installed.PackageGuid;
            var latestPackageGuid = await client.UpdateToLatest(cache.GetToken() ?? throw new(), sourceGuid, PlatformResolver.GetPlatform(), stream, (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));
            if (latestPackageGuid == default) throw new();
            
            installed.PackageGuid = latestPackageGuid;
            installed.ExePath = await GetExePath(installed.PackageGuid, ct);
            await repo.UpdateAsync(installed, ct);

            var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
            SetUpdateState(installed.ProductGuid, UpdateStep.Finalizing);
            Install(productPath, stream);
            RemoveFiles(productPath, await GetFilesToRemove(installed.ProductGuid, sourceGuid, latestPackageGuid, ct));
            return installed;
        }, ct);
    }

    async Task<bool> DoInstallationRepair(Installed installed, CancellationToken ct)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            await client.DownloadWholeVersion(
                cache.GetToken() ?? throw new(),
                installed.ProductGuid,
                PlatformResolver.GetPlatform(),
                installed.PackageGuid,
                stream,
                (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));
                
            installed.ExePath = await GetExePath(installed.PackageGuid, ct);
            await repo.UpdateAsync(installed, ct);

            var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
            SetUpdateState(installed.ProductGuid, UpdateStep.Finalizing);
            Install(productPath, stream);
            return installed;
        }, ct);
    }

    static string GetProductPath(Guid productGuid, string library) => Path.Combine(library, productGuid.ToString());

    static void Install(string productPath, Stream response)
    {
        var extractDirPath = GenerateTempPath();
        ZipFile.ExtractToDirectory(response, extractDirPath);
        if (!Directory.Exists(productPath))
            Directory.CreateDirectory(productPath);

        CopyAndReplaceFiles(extractDirPath, productPath);
        Directory.Delete(extractDirPath, true);
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

    async Task<List<string>> GetFilesToRemove(Guid product, Guid srcPackage, Guid dstPackage, CancellationToken ct)
    {
        var srcTask = GetNdibClient().Download_GetVerificationHashAsync(product, srcPackage, PlatformResolver.GetPlatform(), ct);
        var dstTask = GetNdibClient().Download_GetVerificationHashAsync(product, dstPackage, PlatformResolver.GetPlatform(), ct);
        await Task.WhenAll(srcTask, dstTask);

        var src = srcTask.Result;
        var dst = dstTask.Result;

        return src.Keys.Except(dst.Keys).ToList();
    }

    async Task<bool> VerifyInstallation(Installed installed, CancellationToken ct)
    {
        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        var remoteHash = await GetNdibClient().Download_GetVerificationHashAsync(installed.ProductGuid, installed.PackageGuid, PlatformResolver.GetPlatform(), ct);
        var localHash = ProductHashHandler.GetFileHashes(productPath, remoteHash.Keys.ToList());

        return remoteHash.Keys.Count == localHash.Keys.Count && remoteHash.Keys.All(k => localHash.ContainsKey(k) && localHash[k] == remoteHash[k]);
    }

    static string GenerateTempPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    static void CopyAndReplaceFiles(string src, string dst)
    {
        var files = Directory.GetFiles(src, "*", SearchOption.AllDirectories).ToList();
        foreach (var sourceFilePath in files)
        {
            var relativePath = Path.GetRelativePath(src, sourceFilePath);
            var targetFilePath = Path.Combine(dst, relativePath);

            var targetFileDir = Path.GetDirectoryName(targetFilePath);
            if (targetFileDir != null && !Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir);

            File.Copy(sourceFilePath, targetFilePath, true);
        }
    }

    DsLauncherClient GetLauncherClient() => launcherClientFactory.CreateClient(cache.GetToken() ?? throw new());

    DsNdibClient GetNdibClient() => ndibClientFactory.CreateClient(cache.GetToken() ?? throw new());

    void SetUpdateState(Guid productGuid, UpdateStep step, float percentage = 100)
    {
        if (updates.TryGetValue(productGuid, out UpdateStatus? value))
        {
            if (step != value.Step || percentage >= value.Percentage)
                value.Percentage = percentage;

            value.Step = step;
        }
    }

    async Task<string> GetExePath(Guid packageGuid, CancellationToken ct)
    {
        var package = await GetLauncherClient().Package_GetAsync(packageGuid, ct);
        return PlatformResolver.GetPlatform() switch
        {
            Platform.Win => package.WindowsExePath ?? string.Empty,
            Platform.Linux => package.LinuxExePath ?? string.Empty,
            Platform.Mac => package.MacExePath ?? string.Empty,
            _ => throw new()
        };
    }
}