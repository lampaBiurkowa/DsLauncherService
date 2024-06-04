using DibBase.Infrastructure;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("install")]
internal class InstallCommandHandler(
    Repository<Installed> installedRepo,
    Repository<Library> libraryRepo,
    InstallationService installationService) : ICommandHandler
{ 
    public async Task<Command> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        args.TryGet<Guid>("packageGuid", out var packageGuid);
        args.TryGet<string>("library", out var libraryName);

        if (productGuid == default) return Command.Empty;

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!],
            ct: ct)).FirstOrDefault();

        if (currentInstallation == null)
        {
            var passedLibrary = (await libraryRepo.GetAll(restrict: x => x.Path == libraryName, ct: ct)).FirstOrDefault() ?? throw new();
            installationService.RegisterFullInstall(productGuid, passedLibrary, ct);
        }
        else if (packageGuid != default)
            installationService.RegisterUpdateToVersion(currentInstallation, packageGuid, ct);
        else
            installationService.RegisterUpdateToLatest(currentInstallation, ct);

        return new Command("get-installed")
        {
            Args = 
            {
                { "get-installed", (await installedRepo.GetAll(ct: ct)).Select(x => $"{x.ProductGuid},{x.PackageGuid}") }
            }
        };
    }
}
