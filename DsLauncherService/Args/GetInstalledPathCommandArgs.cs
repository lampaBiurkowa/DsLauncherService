namespace DsLauncherService.Args;

class GetInstalledPathCommandArgs : ICommandArgs
{
    public required string Path { get; set; }
}