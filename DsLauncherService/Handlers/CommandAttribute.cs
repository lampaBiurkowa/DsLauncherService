namespace DsLauncherService.Handlers
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class CommandAttribute : Attribute
    {
        public string CommandName { get; }

        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
