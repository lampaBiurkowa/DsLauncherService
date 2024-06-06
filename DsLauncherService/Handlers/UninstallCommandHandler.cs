using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Handlers;

[Command("uninstall")]
internal class UninstallCommandHandler(
    InstallationService installationService,
    GetInstalledCommandBuilder builder) : ICommandHandler
{ 
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        await installationService.Uninstall(productGuid, ct);

        return await builder.Build(ct);
    }
}
