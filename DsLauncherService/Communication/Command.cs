namespace DsLauncherService.Communication
{
    internal class Command
    {
        public string Name { get; }
        public CommandHead Head { get; init; }
        public CommandArgs Args { get; init; }

        public Command(string name)
        {
            Name = name;
            Head = new CommandHead();
            Args = new CommandArgs();
        }

        public static readonly Command Empty = new Command(string.Empty);

        public override string ToString()
        {
            return $"[Command]: Name({Name})\n[Head]:\n{Head}\n[Args]:\n{Args}";
        }
    }
}
