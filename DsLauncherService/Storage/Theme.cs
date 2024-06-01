using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class Theme : Entity, INamed
{
    public required string Name { get; set; }
}
