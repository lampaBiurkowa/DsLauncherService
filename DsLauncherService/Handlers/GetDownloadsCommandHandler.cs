using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-downloads")]
internal class GetDownloadsCommandHandler(GetDownloadsCommandBuilder builder) : ICommandHandler
{ 
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
