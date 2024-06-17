using DibBase.ModelBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncherService.Storage;

public class Recents : Entity
{
    public List<Guid> ProductGuids { get; set; } = [];
    public Guid UserGuid { get; set; }
}

public class RecentsEntityTypeConfiguration : IEntityTypeConfiguration<Recents>
{
    public void Configure(EntityTypeBuilder<Recents> builder)
    {
        builder.Property(d => d.ProductGuids)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(Guid.Parse)
                       .ToList()
            );
    }
}