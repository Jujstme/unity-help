using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;

namespace JHelper.UnityManagers.Mono;

public readonly struct MonoField : IUnityField
{
    public IntPtr Address { get; }

    internal MonoField(IntPtr address)
    {
        Address = address;
    }

    public string GetName(UnityManager manager)
    {
        var _manager = (Mono)manager;
        return _manager.Helper.Process.ReadString(128, StringType.ASCII, Address + _manager.Offsets.field.name, 0x0);
    }

    public int? GetOffset(UnityManager manager)
    {
        var _manager = (Mono)manager;
        return _manager.Helper.Process.Read<int>(Address + _manager.Offsets.field.offset, out int value)
            ? value
            : null;
    }
}
