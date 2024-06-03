using System.Runtime.InteropServices;
using DsLauncher.ApiClient;

namespace DsLauncherService.Helpers;

static class PlatformResolver
{
    public static Platform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Platform.Win;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Platform.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Platform.Mac;

        throw new();
    }
}