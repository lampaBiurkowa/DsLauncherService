using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-libraries")]
internal class GetLibrariesCommandHandler(GetLibrariesCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
