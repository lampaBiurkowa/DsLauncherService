using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Builders;

class GetDownloadsCommandBuilder(InstallationService installation) : ICommandBuilder
{
    public string Name => "get-downloads";

    public Task<Response> Build(CancellationToken ct) =>
        Task.FromResult(new Response(Name, new GetDownloadsCommandArgs() { Downloads = new(installation.GetCurrentlyBeingInstalled()) }));
}