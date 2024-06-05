namespace DsLauncherService.Args;

class GetCredentialsCommandArgs : ICommandArgs
{
    public required string Token { get; set; }
    public Guid UserGuid { get; set; }
}