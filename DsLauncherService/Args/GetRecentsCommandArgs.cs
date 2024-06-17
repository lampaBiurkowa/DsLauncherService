namespace DsLauncherService.Args;

class GetRecentsCommandArgs : ICommandArgs
{
    public List<Guid> Recents { get; set; } = [];
}