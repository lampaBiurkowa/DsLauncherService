using Microsoft.Extensions.Hosting;
using System.Text;

namespace DsLauncherService.Communication
{
    internal class CommandService : BackgroundService
    {
        private readonly CommandDispatcher dispatcher;
        private readonly Queue<Command> commandQueue = new();

        public CommandService(ServerProvider server, CommandDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;

            server.GetRunningServerInstance().MessageReceived += (s, e) =>
            {
                commandQueue.Enqueue(Command.Parse(Encoding.UTF8.GetString(e.Data)));
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (commandQueue.TryDequeue(out var command))
                {
                    await dispatcher.HandleCommand(command, stoppingToken);
                }

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
