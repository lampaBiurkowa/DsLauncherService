﻿namespace DsLauncherService.Communication;

internal class Command(string name)
{
    public string Name { get; } = name;
    public CommandHead Head { get; init; } = [];
    public CommandArgs Args { get; init; } = [];

    public static readonly Command Empty = new(string.Empty);

    public override string ToString() => $"[Command]: Name({Name})\n[Head]:\n{Head}\n[Args]:\n{Args}";
}