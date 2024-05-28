namespace DsLauncherService.Models;

internal class Credentials
{
    public required Guid UserGuid { get; set; }
    public required string PasswordHash { get; set; }
    public required string Token { get; set; }
}