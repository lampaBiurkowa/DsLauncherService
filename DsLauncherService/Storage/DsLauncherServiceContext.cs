using DibBase.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DsLauncherService.Storage;

public class DsLauncherServiceContext : DibContext
{
    public DbSet<Library> Library { get; set; }
    public DbSet<GameActivity> GameActivity { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<Theme> Theme { get; set; }
    public DbSet<Installed> Installed { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Database.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}