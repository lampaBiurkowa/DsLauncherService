using System.Diagnostics;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Services;

namespace DsLauncherService.Builders;

class GetDownloadsCommandBuilder(InstallationService installation) : ICommandBuilder
{
    public string Name => "get-downloads";
    
    readonly TimeSpan timeout = TimeSpan.FromSeconds(5);
    static readonly Stopwatch ResultsTimer = new();

    public Task<Response> Build(CancellationToken ct)
    {
        if (!ResultsTimer.IsRunning) ResultsTimer.Start();

        var downloads = installation.GetCurrentlyBeingInstalled();
        if (downloads.Keys.Count == 0)
        {
            if (ResultsTimer.Elapsed > timeout)
            {
                ResultsTimer.Reset();
                return Task.FromResult(new Response(string.Empty, new EmptyCommandArgs()));
            }
        }
        else 
            ResultsTimer.Restart();

        return Task.FromResult(new Response(Name, new GetDownloadsCommandArgs() { Downloads = new(downloads) }));
    }
}