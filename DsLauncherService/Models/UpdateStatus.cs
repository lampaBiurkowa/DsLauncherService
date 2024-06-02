namespace DsLauncherService.Models;

internal enum UpdateStep { Download, Install, Verification }

internal class UpdateStatus
{
    public Guid ProductGuid { get; set; }
    public float Percentage { get; set; }
    public UpdateStep Step { get; set; }
}