namespace DsLauncherService.Communication
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
