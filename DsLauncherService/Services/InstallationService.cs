using System.Collections.Concurrent;
using System.IO.Compression;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Helpers;
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
    readonly ConcurrentDictionary<Guid, UpdateStatus> updates = [];

    public ConcurrentDictionary<Guid, UpdateStatus> GetCurrentlyBeingInstalled() => updates;

    public void RegisterUpdateToVersion(Installed installed, Guid dstPackageGuid, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToVersion(installed, dstPackageGuid, ct)), ct);

    public void RegisterUpdateToLatest(Installed installed, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(installed.PackageGuid, () => DoUpdateToLatest(installed, ct)), ct);

    public void RegisterFullInstall(Guid productGuid, Library library, CancellationToken ct) =>
        Task.Run(() => DelegateInstallTask(productGuid, () => DoFullInstall(productGuid, library, ct)), ct);

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
    
    async Task<bool> ExecuteInstallTask(Func<DsLauncherNdibApiClient, Repository<Installed>, MemoryStream, Task<Installed>> task, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Repository<Installed>>();
        using var stream = new MemoryStream();
        var client = new DsLauncherNdibApiClient(launcherOptions);

        var installed = await task(client, repo, stream);

        var verified = await VerifyInstallation(installed, ct);
        await repo.CommitAsync(ct);
        return verified;
    }

    async Task<bool> DoFullInstall(Guid productGuid, Library library, CancellationToken ct)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            var latestPackageGuid = await client.DownloadWhole(cache.GetToken() ?? throw new(), productGuid, PlatformResolver.GetPlatform(), stream, (o, progress) => SetUpdateState(productGuid, UpdateStep.Download, progress));
            if (latestPackageGuid == default) throw new();

            var installed = new Installed
            {
                LibraryId = library.Id,
                ProductGuid = productGuid,
                PackageGuid = latestPackageGuid,
                ExePath = await GetExePath(latestPackageGuid, ct)
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
            RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, dstPackageGuid, ct));
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
            RemoveFiles(productPath, await GetFilesToRemove(sourceGuid, latestPackageGuid, ct));
            return installed;
        }, ct);
    }

    async Task<bool> DoInstallationRepair(Installed installed, CancellationToken ct)
    {
        return await ExecuteInstallTask(async (client, repo, stream) =>
        {
            var latestPackageGuid = await client.DownloadWholeVersion(
                cache.GetToken() ?? throw new(),
                installed.ProductGuid,
                PlatformResolver.GetPlatform(),
                installed.PackageGuid,
                stream,
                (o, progress) => SetUpdateState(installed.ProductGuid, UpdateStep.Download, progress));
                
            if (latestPackageGuid == default) throw new();

            installed.ExePath = await GetExePath(latestPackageGuid, ct);
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

    async Task<List<string>> GetFilesToRemove(Guid srcPackageGuid, Guid dstPackageGuid, CancellationToken ct)
    {
        var srcTask = GetClient().Ndib_GetVerificationHashAsync(srcPackageGuid, PlatformResolver.GetPlatform(), ct);
        var dstTask = GetClient().Ndib_GetVerificationHashAsync(dstPackageGuid, PlatformResolver.GetPlatform(), ct);
        await Task.WhenAll(srcTask, dstTask);

        var src = srcTask.Result;
        var dst = dstTask.Result;

        return src.Keys.Except(dst.Keys).ToList();
    }

    async Task<bool> VerifyInstallation(Installed installed, CancellationToken ct)
    {
        var productPath = GetProductPath(installed.ProductGuid, installed.Library!.Path);
        var remoteHash = await GetClient().Ndib_GetVerificationHashAsync(installed.PackageGuid, PlatformResolver.GetPlatform(), ct);
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

    DsLauncherClient GetClient() => clientFactory.CreateClient(cache.GetToken() ?? throw new());

    void SetUpdateState(Guid productGuid, UpdateStep step, float percentage = 100)
    {
        if (updates.TryGetValue(productGuid, out UpdateStatus? value))
        {
            if (step != value.Step || percentage >= value.Percentage)
            {
                value.Percentage = percentage;
                Console.WriteLine($"{productGuid} {percentage} {step}");
            }

            value.Step = step;
        }
    }

    async Task<string> GetExePath(Guid packageGuid, CancellationToken ct)
    {
        var package = await GetClient().Package_Get2Async(packageGuid, ct);
        return PlatformResolver.GetPlatform() switch
        {
            Platform.Win => package.WindowsExePath,
            Platform.Linux => package.LinuxExePath,
            Platform.Mac => package.MacExePath,
            _ => throw new()
        };
    }
}