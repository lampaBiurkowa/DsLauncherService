using DibBase.Infrastructure;
using DsLauncherService.Builders;
using DsLauncherService.Communication;
using DsLauncherService.Services;
using DsLauncherService.Storage;

namespace DsLauncherService.Handlers;

[Command("install")]
internal class InstallCommandHandler(
    Repository<Installed> installedRepo,
    Repository<Library> libraryRepo,
    InstallationService installationService,
    GetInstalledCommandBuilder builder) : ICommandHandler
{ 
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var productGuid = args.Get<Guid>("productGuid");
        var exePath = args.Get<string>("exePath");
        args.TryGet<Guid>("packageGuid", out var packageGuid);
        args.TryGet<string>("library", out var libraryName);

        if (productGuid == default) throw new();

        var currentInstallation = (await installedRepo.GetAll(
            restrict: x => x.ProductGuid == productGuid,
            expand: [x => x.Library!],
            ct: ct)).FirstOrDefault();

        if (currentInstallation == null)
        {
            var passedLibrary = (await libraryRepo.GetAll(restrict: x => x.Path == libraryName, ct: ct)).FirstOrDefault() ?? throw new();
            installationService.RegisterFullInstall(productGuid, passedLibrary, exePath, ct);
        }
        else if (packageGuid != default)
            installationService.RegisterUpdateToVersion(currentInstallation, packageGuid, exePath, ct);
        else
            installationService.RegisterUpdateToLatest(currentInstallation, exePath, ct);

        return await builder.Build(ct);
    }
}
