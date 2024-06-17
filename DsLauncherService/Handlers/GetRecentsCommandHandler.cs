using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-recent")]
internal class GetRecentsCommandHandler(GetRecentsCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
