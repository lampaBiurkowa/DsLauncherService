using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class Library : Entity
{
    public required string Path { get; set; }
}
