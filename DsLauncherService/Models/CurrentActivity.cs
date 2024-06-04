using System.Diagnostics;

namespace DsLauncherService.Models;

class CurrentActivity
{
    public DateTime StartDate { get; set; }
    public Guid ProductGuid { get; set; }
    public required Process Process { get; set; }
    public long LocalDataId { get; set; }
}