using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Builders;

class GetDownloadsCommandBuilder(InstallationService installation) : ICommandBuilder<GetDownloadsCommandArgs>
{
    public string Name => "get-downloads";

    public Task<Response<GetDownloadsCommandArgs>> Build(CancellationToken ct) =>
        Task.FromResult(new Response<GetDownloadsCommandArgs>(Name, new() { Downloads = new(installation.GetCurrentlyBeingInstalled()) }));
}