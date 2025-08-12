using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;

namespace JHelper.UnityManagers.Mono;

/// <summary>
/// Represents a single Mono field definition from Unity's metadata.
/// Provides access to its name and memory offset.
/// </summary>
public readonly record struct MonoField : IUnityField
{
    /// <summary>
    /// Memory address of this field in the target process.
    /// </summary>
    private readonly IntPtr field;

    private readonly Mono manager;

    internal MonoField(Mono manager, IntPtr address)
    {
        this.manager = manager;
        this.field = address;
    }

    /// <summary>
    /// Reads the name of this field from process memory.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, field + manager.Offsets.MonoClassField_Name, 0x0);
    }

    /// <summary>
    /// Reads the field's memory offset within its containing class or structure.
    /// </summary>
    /// <returns>
    /// The offset in bytes, or <c>null</c> if the offset could not be read.
    /// </returns>
    public int? GetOffset()
    {
        return manager.Helper.Process.Read<int>(field + manager.Offsets.MonoClassField_Offset, out int value)
            ? value
            : null;
    }
}
