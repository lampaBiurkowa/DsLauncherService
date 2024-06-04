using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class Activity : Entity
{
    public DateTime StarDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid ProductGuid { get; set; }
    public Guid UserGuid { get; set; }
}
