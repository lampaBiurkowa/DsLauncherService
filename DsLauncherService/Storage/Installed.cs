using DibBase.ModelBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncherService.Storage;

public class Installed : Entity
{
    public Guid ProductGuid { get; set; }
    public Guid PackageGuid { get; set; }
    public Library? Library { get; set; }
    public long LibraryId { get; set; }
    public required string ExePath { get; set; }
}

public class InstalledConfiguration : IEntityTypeConfiguration<Installed>
{
    public void Configure(EntityTypeBuilder<Installed> builder)
    {
        builder.HasIndex(x => x.ProductGuid).IsUnique();
    }
}