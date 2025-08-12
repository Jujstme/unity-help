using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

/// <summary>
/// Represents a managed IL2CPP class definition inside the target Unity game
/// and provides access to its fields, metadata, and hierarchy.
/// </summary>
/// <remarks>
/// This struct uses direct process memory reading to extract Unity's IL2CPP class metadata,
/// so all operations assume the underlying memory layout matches the expected IL2CPP version.
/// </remarks>
public readonly record struct IL2CPPClass : IUnityClass<IL2CPPClass, IL2CPPField>
{
    /// <summary>
    /// Memory address of this class in the target process.
    /// </summary>
    internal readonly IntPtr @class;

    private readonly IL2CPP manager;

    internal IL2CPPClass(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.@class = address;
    }

    /// <summary>
    /// Enumerates all fields in this class, including inherited fields,
    /// stopping when a base class is "Object" or in the "UnityEngine" namespace.
    /// </summary>
    public IEnumerable<IL2CPPField> EnumFields()
    {
        IL2CPPClass? thisClass = this;

        while (true)
        {
            if (thisClass is null)
                break;

            // Stop if we've reached base object types or UnityEngine types.
            if (thisClass.Value.GetName() is not string name || name == "Object" || thisClass.Value.GetNamespace() is not string nameSpace || nameSpace == "UnityEngine")
                break;

            int fieldCount = manager.Helper.Process.Read<short>(@class + manager.Offsets.MonoClass_FieldCount);

            IntPtr fields = fieldCount != 0 ? manager.Helper.Process.ReadPointer(@class + manager.Offsets.MonoClass_Fields) : IntPtr.Zero;

            // Move to parent class for next iteration.
            thisClass = thisClass.Value.GetParent();

            // Yield current class's fields.
            if (fieldCount > 0 && fields != IntPtr.Zero)
            {
                for (int i = 0; i < fieldCount; i++)
                    yield return new IL2CPPField(manager, fields + i * manager.Offsets.MonoClassField_StructSize);
            }
        }
    }

    /// <summary>
    /// Gets the memory offset for a given field name.
    /// Handles both standard names and auto-property backing fields.
    /// </summary>
    public int? GetFieldOffset(string fieldName)
    {
        return EnumFields()
            .FirstOrDefault(f => {
                string name = f.GetName();
                return name.EndsWith("k__BackingField")
                    ? name == "<" + fieldName + ">k__BackingField"
                    : name == fieldName;
            })
            .GetOffset();
    }

    /// <summary>
    /// Retrieves the name of this class.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, @class + manager.Offsets.MonoClass_Name, 0);
    }

    /// <summary>
    /// Retrieves the namespace of this class.
    /// </summary>
    public string GetNamespace()
    {
        return manager.Helper.Process.ReadString(64, StringType.ASCII, @class + manager.Offsets.MonoClass_NameSpace, 0);
    }

    /// <summary>
    /// Retrieves the parent (base) class for this type, if any.
    /// </summary>
    public IL2CPPClass? GetParent()
    {
        return manager.Helper.Process.ReadPointer(@class + manager.Offsets.MonoClass_Parent, out IntPtr value)
            ? new IL2CPPClass(manager, value)
            : null;
    }

    /// <summary>
    /// Retrieves the address of this class's static field table.
    /// </summary>
    public IntPtr? GetStaticTable()
    {
        return manager.Helper.Process.ReadPointer(@class + manager.Offsets.MonoClass_StaticFields, out IntPtr value)
            ? value
            : null;
    }
}
