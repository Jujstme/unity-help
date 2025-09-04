using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;

namespace JHelper.UnityManagers.Mono;

public readonly ref struct MonoField : IUnityField
{
    private readonly IntPtr field;
    private readonly Mono manager;

    internal MonoField(Mono manager, IntPtr address)
    {
        this.manager = manager;
        this.field = address;
    }

    public string GetName() => manager.Helper.Process.ReadString(128, StringType.ASCII, field + manager.Offsets.field.name, 0x0);

    public int? GetOffset() => manager.Helper.Process.Read<int>(field + manager.Offsets.field.offset, out int value)
            ? value
            : null;
}
