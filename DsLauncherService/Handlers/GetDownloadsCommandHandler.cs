using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("get-downloads")]
internal class GetDownloadsCommandHandler(InstallationService installationService) : ICommandHandler
{ 
    public Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var cmd = new Command("get-downloads");

        foreach (var status in installationService.GetCurrentlyBeingInstalled())
            cmd.Args.Add(status.ProductGuid.ToString(), $"{status.Percentage},{status.Step}");
        
        return Task.FromResult(cmd);
    }
}
