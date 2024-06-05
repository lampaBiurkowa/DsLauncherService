using DsLauncherService.Builders;
using DsLauncherService.Communication;

namespace DsLauncherService.Handlers;

[Command("get-library-details")]
internal class GetLibryDetailsCommandHandler(GetLibraryDetailsCommandBuilder builder) : ICommandHandler
{
    public async Task<Response> Handle(CommandArgs args, CancellationToken ct)
    {
        var path = args.Get<string>("path");
        return await builder.Build(path, ct);
    }
}
