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
            cmd.Args.Add(status.Key.ToString(), $"{status.Value.Percentage},{status.Value.Step}");
        
        return cmd.Args.Count > 0 ? Task.FromResult(cmd) : Task.FromResult(Command.Empty);
    }
}
