namespace DsLauncherService.Args;

class GetInstalledCommandArgs : ICommandArgs
{
    public Dictionary<Guid, Guid> Installed { get; set; } = [];
}