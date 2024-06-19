using DsLauncherService.Args;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;

namespace DsLauncherService.Communication;

internal class CommandService : BackgroundService
{
    private readonly ServerProvider server;
    private readonly CommandDispatcher dispatcher;
    private readonly HashSet<CommandExecutionMetadata> commands;

    private readonly object _lock = new();

    public CommandService(ServerProvider server, CommandDispatcher dispatcher)
    {
        this.server = server;
        this.dispatcher = dispatcher;

        commands = new HashSet<CommandExecutionMetadata>(new CommandExecutionMetadataEqualityComparer());

        server.GetRunningServerInstance().MessageReceived += (s, e) =>
        {
            var commandStr = Encoding.UTF8.GetString(e.Data);
            var command = CommandParser.Deserialize(commandStr);

            var execMetadata = new CommandExecutionMetadata(command);

            lock (_lock)
            {
                commands.Remove(execMetadata);
                commands.Add(execMetadata);
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (!stoppingToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                commands.RemoveWhere(execMetadata => execMetadata.ExecutionsRemaining == 0);
            }

            int elapsedTime = (int)stopwatch.ElapsedMilliseconds;

            var tasks = commands.ToArray().Select(execMetadata => Task.Run(async () =>
            {
                execMetadata.TimeUntilExecution -= elapsedTime;

                if (execMetadata.ShouldExecute)
                {
                    try
                    {
                        var response = await dispatcher.HandleCommand(execMetadata.Command, stoppingToken);

                        if (!string.IsNullOrWhiteSpace(response.Name))
                        {
                            await server.SendAsync(ResponseParser.Serialize(response));
                            execMetadata.ExecutionsRemaining--;
                        }
                        else
                        {
                            execMetadata.ExecutionsRemaining = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                        execMetadata.ExecutionsRemaining--;
                    }
                    finally
                    {
                        execMetadata.TimeUntilExecution = execMetadata.Command.Head.WorkerInterval;
                    }
                }
            }));
            
            stopwatch.Restart();
            await Task.WhenAll(tasks.Append(Task.Delay(10, stoppingToken)));
        }
    }
}
