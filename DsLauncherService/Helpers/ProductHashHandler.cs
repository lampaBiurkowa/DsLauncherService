using System.Security.Cryptography;

namespace DsLauncherService.Helpers;

static class ProductHashHandler
{
    public static Dictionary<string, string> GetFileHashes(string directoryPath, List<string> paths)
    {
        var fileHashes = new Dictionary<string, string>();
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(directoryPath, path);
            fileHashes[path] = ComputeFileHash(fullPath);  
        } 

        return fileHashes;
    }

    static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}