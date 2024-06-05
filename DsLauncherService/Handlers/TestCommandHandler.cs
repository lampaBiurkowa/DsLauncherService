using DsLauncherService.Communication;

namespace DsLauncherService.Handlers
{
    // [Command("test")]
    // internal class TestCommandHandler : ICommandHandler
    // {
    //     public async Task<Command> Handle(CommandArgs args, CancellationToken cancellationToken)
    //     {
    //         if (args.TryGet<int>("testnum", out var testArg))
    //         {
    //             await Task.Delay(10, cancellationToken);

    //             var response = new Command("testresponse");
    //             response.Args.Add("testnumresponse", testArg + 1);
    //             return response;
    //         }

    //         return Command.Empty;
    //     }
    // }
}
