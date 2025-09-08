using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public readonly struct IL2CPPField : IUnityField
{
    public IntPtr Address { get; }

    internal IL2CPPField(IntPtr address)
    {
        Address = address;
    }

    public string GetName(UnityManager manager)
    {
        var _manager = (IL2CPP)manager;
        return _manager.Helper.Process.ReadString(64, StringType.ASCII, Address + _manager.Offsets.field.name, 0);
    }

    public int? GetOffset(UnityManager manager)
    {
        var _manager = (IL2CPP)manager;
        return _manager.Helper.Process.Read<int>(Address + _manager.Offsets.field.offset, out int value)
            ? value
            : null;
    }
}
