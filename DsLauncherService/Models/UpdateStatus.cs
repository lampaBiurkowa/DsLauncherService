namespace DsLauncherService.Models;

internal enum UpdateStep { Download, Install, Verification }

internal class UpdateStatus
{
    public float Percentage { get; set; }
    public UpdateStep Step { get; set; }
}