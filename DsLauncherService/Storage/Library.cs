using DibBase.ModelBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DsLauncherService.Storage;

public class Library : Entity
{
    public required string Path { get; set; }
    public required string Name { get; set; }
    public bool IsDeveloper { get; set; }
}

public class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder)
    {
        builder.HasIndex(x => x.Path).IsUnique();
    }
}