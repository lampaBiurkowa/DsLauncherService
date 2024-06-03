using System.Diagnostics;
using DibBase.Infrastructure;
using DsLauncher.ApiClient;
using DsLauncherService.Communication;
using DsLauncherService.Helpers;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("execute")]
internal class ExecuteCommandHandler(Repository<Installed> installedRepo) : ICommandHandler
{ 
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        var exePath = args.Get<string>("exePath");

        if (productGuid == default) return Command.Empty;

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!],
            ct: ct)).FirstOrDefault();

        if (currentInstallation == null) return Command.Empty;

        exePath = Path.Combine(currentInstallation.Library!.Path, productGuid.ToString(), exePath);
        if (PlatformResolver.GetPlatform() == Platform.Linux)
        {
            var markAsExe = CreateProces("chmod", $"+x {exePath}");
            markAsExe.Start();
            markAsExe.WaitForExit();
        }

        var executeProcess = CreateProces(exePath, workingDir: Path.GetDirectoryName(exePath));
        executeProcess.Start();
        return Command.Empty;
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
