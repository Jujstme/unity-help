using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

/// <summary>
/// Represents a managed Mono class definition inside the target Unity process.
/// Provides metadata access (name, namespace, fields) and runtime information 
/// through direct memory reading.
/// </summary>
public readonly record struct MonoClass : IUnityClass<MonoClass, MonoField>
{
    /// <summary>
    /// Memory address of this class in the target process.
    /// </summary>
    internal readonly IntPtr @class;

    private readonly Mono manager;

    internal MonoClass(Mono manager, IntPtr address)
    {
        this.manager = manager;
        this.@class = address;
    }

    /// <summary>
    /// Retrieves the name of this class.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, @class + manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_Name, 0x0);
    }

    /// <summary>
    /// Retrieves the namespace of this class.
    /// </summary>
    public string GetNamespace()
    {
        return manager.Helper.Process.ReadString(64, StringType.ASCII, @class + manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_Namespace, 0x0);
    }

    /// <summary>
    /// Enumerates all fields in this class, including inherited fields,
    /// stopping when a base class is "Object" or in the "UnityEngine" namespace.
    /// </summary>
    public IEnumerable<MonoField> EnumFields()
    {
        // Storing as local variables should be much faster than accessing properties repeatedly.
        ProcessMemory process = manager.Helper.Process;
        int fieldCountOffset = manager.Offsets.MonoClassDef_FieldCount;
        int fieldsOffset = manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_Fields;
        int monoClassFieldAlignment = manager.Offsets.MonoClassFieldAlignment;

        MonoClass? thisClass = this;

        while (true)
        {
            if (thisClass is null)
                break;

            if (thisClass.Value.GetName() == "Object" || thisClass.Value.GetNamespace() == "UnityEngine")
                break;

            if (process.Read<int>(thisClass.Value.@class + fieldCountOffset, out int fieldCount) && fieldCount > 0)
            {
                if (process.ReadPointer(thisClass.Value.@class + fieldsOffset, out IntPtr fields) && fields != IntPtr.Zero)
                {
                    for (int i = 0; i < fieldCount; i++)
                        yield return new MonoField(manager, fields + i * monoClassFieldAlignment);
                }
            }

            // Move to parent class for next iteration.
            thisClass = thisClass.Value.GetParent();
        }
    }

    /// <summary>
    /// Retrieves the parent (base) class for this type, if any.
    /// </summary>
    public MonoClass? GetParent()
    {
        return manager.Helper.Process.DerefOffsets(@class + manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_Parent, out IntPtr value, 0, 0)
            ? new MonoClass(manager, value)
            : null;
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
    /// Retrieves the address of this class's static field table.
    /// </summary>
    public IntPtr? GetStaticTable()
    {
        ProcessMemory process = manager.Helper.Process;

        if (!process.ReadPointer(@class + manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_Runtime_Info, out IntPtr runtimeInfo) || runtimeInfo == IntPtr.Zero)
            return null;

        if (!process.ReadPointer(runtimeInfo + manager.Offsets.MonoClassRuntimeInfo_Domain_VTables, out IntPtr vtables) || vtables == IntPtr.Zero)
            return null;

        IntPtr ptr;
        if (manager.Version == MonoVersion.V1 || manager.Version == MonoVersion.V1_cattrs)
        {
            ptr = vtables + manager.Offsets.MonoClass_VTableSize;
        }
        else
        {
            vtables += manager.Offsets.MonoVTable_VTable;

            if (!process.Read<int>(@class + manager.Offsets.MonoClassDef_Klass + manager.Offsets.MonoClass_VTableSize, out int vtable_size) || vtable_size == 0)
                return null;

            ptr = vtables + vtable_size * process.PointerSize;
        }

        if (!process.ReadPointer(ptr, out IntPtr addr) || ptr == IntPtr.Zero)
            return null;

        return addr;
    }
}
