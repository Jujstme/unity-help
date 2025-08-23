using JHelper.Common.ProcessInterop;
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
        ProcessMemory process = manager.Helper.Process;
        int fieldCountOffset = manager.Offsets.klass.fieldCount;
        int fieldsOffset = manager.Offsets.klass.fields;
        IL2CPPClass? thisClass = this;

        while (true)
        {
            if (thisClass is null)
                break;

            // Stop if we've reached base object types or UnityEngine types.
            if (thisClass.Value.GetName() == "Object" || thisClass.Value.GetNamespace() == "UnityEngine")
                break;

            if (process.Read<ushort>(thisClass.Value.@class + fieldCountOffset, out ushort fieldCount) && fieldCount > 0 && fieldCount != ushort.MaxValue)
            {
                if (process.ReadPointer(thisClass.Value.@class + fieldsOffset, out IntPtr fields) && fields != IntPtr.Zero)
                {
                    for (int i = 0; i < fieldCount; i++)
                        yield return new IL2CPPField(manager, fields + i * manager.Offsets.field.structSize);
                }
            }

            // Move to parent class for next iteration.
            thisClass = thisClass.Value.GetParent();
        }
    }

    /// <summary>
    /// Gets the memory offset for a given field name.
    /// Handles both standard names and auto-property backing fields.
    /// </summary>
    public int? GetFieldOffset(string fieldName)
    {
        using (var enumerator = EnumFields()
            .Where(f => {
                string name = f.GetName();
                return name.EndsWith("k__BackingField")
                    ? name == "<" + fieldName + ">k__BackingField"
                    : name == fieldName;
            })
            .GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current.GetOffset()
                : null;
        }
    }

    /// <summary>
    /// Retrieves the name of this class.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, @class + manager.Offsets.klass.name, 0);
    }

    /// <summary>
    /// Retrieves the namespace of this class.
    /// </summary>
    public string GetNamespace()
    {
        return manager.Helper.Process.ReadString(64, StringType.ASCII, @class + manager.Offsets.klass.namespaze, 0);
    }

    /// <summary>
    /// Retrieves the parent (base) class for this type, if any.
    /// </summary>
    public IL2CPPClass? GetParent()
    {
        return manager.Helper.Process.ReadPointer(@class + manager.Offsets.klass.parent, out IntPtr value)
            ? new IL2CPPClass(manager, value)
            : null;
    }

    /// <summary>
    /// Retrieves the address of this class's static field table.
    /// </summary>
    public IntPtr? GetStaticTable()
    {
        return manager.Helper.Process.ReadPointer(@class + manager.Offsets.klass.staticFields, out IntPtr value)
            ? value
            : null;
    }
}
