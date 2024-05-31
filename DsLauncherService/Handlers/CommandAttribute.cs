namespace DsLauncherService.Handlers;

[AttributeUsage(AttributeTargets.Class)]
sealed class CommandAttribute(string commandName) : Attribute
{
    public string CommandName => commandName;
}
