using System.Diagnostics;

namespace DsLauncherService.Models;

class CurrentActivity(Guid productGuid, Process process, DateTime startDate, long localDataId)
{
    public Guid ProductGuid { get; } = productGuid;
    public Process Process { get; } = process;
    public DateTime StartDate { get; } = startDate;
    public long LocalDataId { get; } = localDataId;
}