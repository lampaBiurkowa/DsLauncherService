using DibBase.Infrastructure;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("repair-installation")]
internal class RepairInstallationCommandHandler(
    Repository<Installed> installedRepo,
    InstallationService installationService,
    GetInstalledCommandBuilder builder) : ICommandHandler
{ 
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        var exePath = args.Get<string>("exePath");

        if (productGuid == default) throw new();

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!], //required for installation service
            ct: ct)).FirstOrDefault() ?? throw new();

        installationService.RegisterInstallationRepair(currentInstallation, ct);

        return await builder.Build(ct);
    }
}
