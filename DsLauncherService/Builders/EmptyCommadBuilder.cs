using DsLauncherService.Args;
using DsLauncherService.Communication;

namespace DsLauncherService.Builders;

class EmptyCommandBuilder : ICommandBuilder<EmptyCommandArgs>
{
    public string Name => string.Empty;

    public Task<Response<EmptyCommandArgs>> Build(CancellationToken ct) =>
        Task.FromResult(new Response<EmptyCommandArgs>(Name, new()));
}