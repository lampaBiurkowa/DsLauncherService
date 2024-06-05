using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-installed")]
internal class GetInstalledCommandHandler(GetInstalledCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
