using DsLauncherService.Args;
using DsLauncherService.Communication;

namespace DsLauncherService.Builders;

class EmptyCommandBuilder : ICommandBuilder
{
    public string Name => string.Empty;

    public Task<Response> Build(CancellationToken ct) =>
        Task.FromResult(new Response(Name, new EmptyCommandArgs()));
}