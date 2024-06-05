using DsLauncherService.Args;
using DsLauncherService.Communication;

namespace DsLauncherService.Builders;

interface ICommandBuilder<T> where T : ICommandArgs
{
    string Name { get; }
    Task<Response<T>> Build(CancellationToken ct);
}