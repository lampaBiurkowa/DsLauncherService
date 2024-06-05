using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-installed")]
internal class GetInstalledCommandHandler(GetInstalledCommandBuilder builder) : ICommandHandler<GetInstalledCommandArgs>
{
    public async Task<Response<GetInstalledCommandArgs>> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
