using DibBase.ModelBase;

namespace DsLauncherService.Storage;

public class Installed : Entity
{
    public Guid ProductGuid { get; set; }
    public Guid PackageGuid { get; set; }
    public Library? Library { get; set; }
    public long LibraryId { get; set; }
}
