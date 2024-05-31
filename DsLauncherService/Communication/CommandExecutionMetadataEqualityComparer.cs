using System.Diagnostics.CodeAnalysis;

namespace DsLauncherService.Communication
{
    internal class CommandExecutionMetadataEqualityComparer : IEqualityComparer<CommandExecutionMetadata> // :D\
    {
        public bool Equals(CommandExecutionMetadata? x, CommandExecutionMetadata? y)
        {
            return x?.Command.Name == y?.Command.Name;
        }

        public int GetHashCode([DisallowNull] CommandExecutionMetadata obj)
        {
            return obj.Command.Name.GetHashCode();
        }
    }
}
