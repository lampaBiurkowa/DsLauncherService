namespace DsLauncherService.Communication
{
    internal class CommandHead : CommandArgs
    {
        private const string WorkerIntervalKey = "workerinterval";
        private const string WorkerRepetitionsKey = "workerrepetitions";

        public CommandHead() { }

        public CommandHead(CommandArgs commandArgs)
            : base(commandArgs)
        { }

        public int WorkerInterval
        {
            get
            {
                if (TryGet(WorkerIntervalKey, out int workerInterval))
                {
                    return workerInterval;
                }

                return 1000;
            }
        }

        public int WorkerRepetitions
        {
            get
            {
                if (TryGet(WorkerRepetitionsKey, out int workerRepetitions))
                {
                    return workerRepetitions;
                }

                return 1;
            }
        }
    }
}
