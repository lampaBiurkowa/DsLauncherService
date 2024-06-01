using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class Settings : Entity
{
    public Theme? Theme { get; set; }
    public long ThemeId { get; set; }
    public Guid UserGuid { get; set; }
}
