using System.Diagnostics;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Helpers;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("execute")]
internal class ExecuteCommandHandler(
    Repository<Installed> installedRepo,
    Repository<Recents> recentsRepo,
    GameActivityService gameActivityService,
    CacheService cache,
    EmptyCommandBuilder builder) : ICommandHandler
{ 
    const int MAX_RECENT_GAMES = 5;

    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        if (productGuid == default) throw new();

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library],
            ct: ct)).FirstOrDefault() ?? throw new();

        var exePath = Path.Combine(currentInstallation.Library!.Path, productGuid.ToString(), currentInstallation.ExePath);
        if (PlatformResolver.GetPlatform() == Platform.Linux)
        {
            var markAsExe = CreateProces("chmod", $"+x {exePath}");
            markAsExe.Start();
            markAsExe.WaitForExit();
        }

        var executeProcess = CreateProces(exePath, workingDir: Path.GetDirectoryName(exePath));
        await gameActivityService.StartGame(productGuid, executeProcess, ct);

        var recents = (await recentsRepo.GetAll(restrict: x => x.UserGuid == cache.GetUser(), ct: ct)).FirstOrDefault();
        var isFirstGame = recents == null;
        recents ??= new Recents { UserGuid = cache.GetUser() ?? throw new() };
        recents.ProductGuids.Remove(currentInstallation.ProductGuid);
        recents.ProductGuids.Insert(0, currentInstallation.ProductGuid);
        recents.ProductGuids = recents.ProductGuids.GetRange(0, Math.Min(recents.ProductGuids.Count, MAX_RECENT_GAMES));
        if (isFirstGame)
            await recentsRepo.InsertAsync(recents, ct);
        else
            await recentsRepo.UpdateAsync(recents, ct);
        await recentsRepo.CommitAsync(ct);

        return await builder.Build(ct);
    }

    static Process CreateProces(string command, string? args = null, string? workingDir = null)
    {
        var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.WorkingDirectory = workingDir;

        return process;
    }
}
