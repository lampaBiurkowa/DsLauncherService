using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetLibraryDetailsCommandBuilder(
    Repository<Library> libraryRepo,
    Repository<Installed> installedRepo) : ICommandBuilder
{
    public string Name => "get-library-details";

    public async Task<Response> Build(string path, CancellationToken ct)
    {
        var library = (await libraryRepo.GetAll(restrict: x => x.Path == path, ct: ct)).FirstOrDefault() ?? throw new();
        var installed = (await installedRepo.GetAll(restrict: x => x.LibraryId == library.Id, ct: ct)).Select(x => x.ProductGuid);
        return new Response(Name, new GetLibraryDetailsCommandArgs() { Installed = installed, SizeBytes = GetDirectorySize(library.Path) });
    }

    static long GetDirectorySize(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"The directory '{directoryPath}' does not exist.");

        long totalSize = 0;
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
            totalSize += new FileInfo(file).Length;

        return totalSize;
    }
}