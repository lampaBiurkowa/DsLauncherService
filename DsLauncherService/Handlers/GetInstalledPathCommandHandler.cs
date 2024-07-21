using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-installed-path")]
internal class GetInstalledPathCommandHandler(GetInstalledPathCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        if (productGuid == default) throw new();

        return await builder.Build(productGuid, ct);
    }
}
