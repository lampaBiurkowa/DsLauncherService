using DsLauncherService.Args;
using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-libraries")]
internal class GetLibrariesCommandHandler(GetLibrariesCommandBuilder builder) : ICommandHandler<GetLibrariesCommandArgs>
{
    public async Task<Response<GetLibrariesCommandArgs>> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
