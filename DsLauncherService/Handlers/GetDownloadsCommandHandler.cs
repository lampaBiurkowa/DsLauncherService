using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-downloads")]
internal class GetDownloadsCommandHandler(GetDownloadsCommandBuilder builder) : ICommandHandler<GetDownloadsCommandArgs>
{ 
    public async Task<Response<GetDownloadsCommandArgs>> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
    // {
    //     var cmd = new Command("get-downloads");
    //     foreach (var status in installationService.GetCurrentlyBeingInstalled())
    //         cmd.Args.Add(status.Key.ToString(), $"{status.Value.Percentage},{status.Value.Step}");
        
    //     return cmd.Args.Count > 0 ? Task.FromResult(cmd) : Task.FromResult(Command.Empty);
    // }
}
