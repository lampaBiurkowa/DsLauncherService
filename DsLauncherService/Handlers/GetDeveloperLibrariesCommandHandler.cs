using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-developer-libraries")]
internal class GetDeveloperLibrariesCommandHandler(GetDeveloperLibrariesCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct) =>
        await builder.Build(ct);
}
