namespace DsLauncherService.Communication
{
    internal class CommandExecutionMetadata
    {
        public Command Command { get; }
        public int ExecutionsRemaining { get; set; } 
        public int TimeUntilExecution { get; set; } 

        public CommandExecutionMetadata(Command command)
        {
            Command = command;
            ExecutionsRemaining = command.Head.WorkerRepetitions;
        }

        public bool ShouldExecute
        {
            get => ExecutionsRemaining != 0 && TimeUntilExecution <= 0;
        }
    }
}
