using Microsoft.Extensions.Hosting;
using System.Text;

namespace DsLauncherService.Communication
{
    internal class CommandService : BackgroundService
    {
        private readonly CommandDispatcher _dispatcher;
        private readonly Queue<Command> _commandQueue = new();

        public CommandService(ServerProvider server, CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            server.GetRunningServerInstance().MessageReceived += (s, e) =>
            {
                _commandQueue.Enqueue(Command.Parse(Encoding.UTF8.GetString(e.Data)));
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_commandQueue.TryDequeue(out var command))
                {
                    await _dispatcher.HandleCommand(command, stoppingToken);
                }

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
