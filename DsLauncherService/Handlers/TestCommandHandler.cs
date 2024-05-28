using DsLauncherService.Communication;

namespace DsLauncherService.Handlers
{
    [Command("test")]
    internal class TestCommandHandler : ICommandHandler
    {
        public Task Handle(CommandArgs args, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
