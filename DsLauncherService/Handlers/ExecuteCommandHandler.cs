using System.Diagnostics;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Helpers;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("execute")]
internal class ExecuteCommandHandler(
    Repository<Installed> installedRepo,
    GameActivityService gameActivityService,
    EmptyCommandBuilder builder) : ICommandHandler<EmptyCommandArgs>
{ 
    public async Task<Response<EmptyCommandArgs>> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        var exePath = args.Get<string>("exePath");

        if (productGuid == default) throw new();

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!],
            ct: ct)).FirstOrDefault() ?? throw new();

        exePath = Path.Combine(currentInstallation.Library!.Path, productGuid.ToString(), exePath);
        if (PlatformResolver.GetPlatform() == Platform.Linux)
        {
            var markAsExe = CreateProces("chmod", $"+x {exePath}");
            markAsExe.Start();
            markAsExe.WaitForExit();
        }

        var executeProcess = CreateProces(exePath, workingDir: Path.GetDirectoryName(exePath));
        await gameActivityService.StartGame(productGuid, executeProcess, ct);
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
