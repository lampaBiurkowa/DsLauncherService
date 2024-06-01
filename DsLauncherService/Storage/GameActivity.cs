using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class GameActivity : Entity
{
    public DateTime StarDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid GameGuid { get; set; }
    public Guid UserGuid { get; set; }
}
