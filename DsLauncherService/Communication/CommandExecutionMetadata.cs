namespace DsLauncherService.Communication;

internal class CommandExecutionMetadata(Command command)
{
    public Command Command => command;
    public int ExecutionsRemaining { get; set; } = command.Head.WorkerRepetitions;
    public int TimeUntilExecution { get; set; }

    public bool ShouldExecute => ExecutionsRemaining != 0 && TimeUntilExecution <= 0;
}