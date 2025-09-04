using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public readonly ref struct IL2CPPField : IUnityField
{
    private readonly IntPtr field;
    private readonly IL2CPP manager;

    internal IL2CPPField(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.field = address;
    }

    public string GetName() => manager.Helper.Process.ReadString(64, StringType.ASCII, field + manager.Offsets.field.name, 0);

    public int? GetOffset() => manager.Helper.Process.Read<int>(field + manager.Offsets.field.offset, out int value)
        ? value
        : null;
}
