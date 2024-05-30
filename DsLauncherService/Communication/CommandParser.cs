using System.Text;

namespace DsLauncherService.Communication
{
    internal static class CommandParser
    {
        public static Command Deserialize(string value)
        {
            using var reader = new StringReader(value);

            string? commandName = reader.ReadLine();
            if (string.IsNullOrEmpty(commandName))
            {
                throw new FormatException("Invalid command name.");
            }

            var command = new Command(commandName)
            {
                Head = new CommandHead(DeserializeArgs(reader)),
                Args = DeserializeArgs(reader)
            };

            return command;
        }

        public static string Serialize(Command command)
        {
            var builder = new StringBuilder();
            builder.AppendLine(command.Name);

            if (command.Head.Count > 0)
            {
                builder.AppendLine(SerializeArgs(command.Head));
            }

            if (command.Args.Count > 0)
            {
                builder.AppendLine("");
                builder.AppendLine(SerializeArgs(command.Args));
            }

            return builder.ToString();
        }

        private static CommandArgs DeserializeArgs(StringReader reader)
        {
            var args = new CommandArgs();

            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) break;

                int colonIndex = line.IndexOf(':');
                if (colonIndex == -1) break;

                string argName = line[..colonIndex];
                string argValue = line[(colonIndex + 1)..];

                args.Add(argName, argValue);
            }

            return args;
        }

        private static string SerializeArgs(CommandArgs args)
        {
            return string.Join(Environment.NewLine, args.Select(arg =>
            {
                return $"{arg.Key}:{arg.Value}";
            }));
        }
    }
}
