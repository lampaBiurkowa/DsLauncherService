namespace DsLauncherService.Args;

class GetLibraryDetailsCommandArgs : ICommandArgs
{
    public IEnumerable<Guid> Installed { get; set; } = [];
    public long SizeBytes { get; set; }
}