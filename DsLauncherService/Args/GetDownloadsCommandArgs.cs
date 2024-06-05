using DsLauncherService.Models;

namespace DsLauncherService.Args;

class GetDownloadsCommandArgs : ICommandArgs
{
    public Dictionary<Guid, UpdateStatus> Downloads { get; set; } = [];
}