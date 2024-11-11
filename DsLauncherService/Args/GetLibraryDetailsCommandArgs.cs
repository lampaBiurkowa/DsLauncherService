namespace DsLauncherService.Args;

class GetLibraryDetailsCommandArgs : ICommandArgs
{
    public IEnumerable<Guid> Installed { get; set; } = [];
    public long SizeBytes { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public bool IsDeveloper { get; set; }
}