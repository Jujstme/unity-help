using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;

namespace JHelper.UnityManagers.IL2CPP;

/// <summary>
/// Represents a single IL2CPP field definition from Unity's metadata.
/// Provides access to its name and memory offset.
/// </summary>
public readonly record struct IL2CPPField : IUnityField
{
    /// <summary>
    /// Memory address of this field in the target process.
    /// </summary>
    private readonly IntPtr field;

    private readonly IL2CPP manager;

    internal IL2CPPField(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.field = address;
    }

    /// <summary>
    /// Reads the name of this field from process memory.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(64, StringType.ASCII, field + manager.Offsets.field.name, 0);
    }

    /// <summary>
    /// Reads the field's memory offset within its containing class or structure.
    /// </summary>
    /// <returns>
    /// The offset in bytes, or <c>null</c> if the offset could not be read.
    /// </returns>
    public int? GetOffset()
    {
        return manager.Helper.Process.Read<int>(field + manager.Offsets.field.offset, out int value)
            ? value
            : null;
    }
}
