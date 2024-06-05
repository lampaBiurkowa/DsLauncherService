using DsLauncherService.Communication;

namespace DsLauncherService.Builders;

interface ICommandBuilder
{
    string Name { get; }
    Task<Response> Build(CancellationToken ct);
}