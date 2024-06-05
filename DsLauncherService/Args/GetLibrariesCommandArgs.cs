namespace DsLauncherService.Args;

class LibraryEntry
{
    public required string Path { get; set; }
    public required string Name { get; set; }
}

class GetLibrariesCommandArgs : ICommandArgs
{
    public IEnumerable<LibraryEntry> Libraries { get; set; } = [];
}