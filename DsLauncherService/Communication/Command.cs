namespace DsLauncherService.Communication
{
    internal class Command
    {
        public string Name { get; }
        public CommandArgs Args { get; }

        public Command(string name)
        {
            Name = name;
            Args = new CommandArgs();
        }

        public static Command Parse(string value)
        {
            using var reader = new StringReader(value);

            string? commandName = reader.ReadLine();
            if (string.IsNullOrEmpty(commandName))
            {
                throw new FormatException("Invalid command name.");
            }

            var command = new Command(commandName);

            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length == 0) break;

                int colonIndex = line.IndexOf(':');
                if (colonIndex == -1) break;

                string argName = line[..colonIndex];
                string argValue = line[(colonIndex + 1)..];

                command.Args.Add(argName, argValue);
            }

            return command;
        }
    }
}
