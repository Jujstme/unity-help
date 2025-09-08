using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Collections.Generic;

namespace JHelper.UnityManagers.IL2CPP;

public class IL2CPPClass : UnityClass
{
    internal IL2CPPClass(IL2CPP manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    protected override IEnumerable<IUnityField> EnumFields()
    {
        IL2CPP manager = (IL2CPP)Manager;

        ProcessMemory process = Manager.Helper.Process;
        int fieldCountOffset = manager.Offsets.klass.fieldCount;
        int fieldsOffset = manager.Offsets.klass.fields;
        IL2CPPClass? thisClass = this;

        while (true)
        {
            if (thisClass is null)
                break;

            // Stop if we've reached base object types or UnityEngine types.
            if (thisClass.Name == "Object" || thisClass.Namespace == "UnityEngine")
                break;

            if (process.Read<ushort>(thisClass.Address + fieldCountOffset, out ushort fieldCount) && fieldCount > 0 && fieldCount != ushort.MaxValue)
            {
                if (process.ReadPointer(thisClass.Address + fieldsOffset, out IntPtr fields) && fields != IntPtr.Zero)
                {
                    for (int i = 0; i < fieldCount; i++)
                        yield return new IL2CPPField(fields + i * manager.Offsets.field.structSize);
                }
            }

            // Move to parent class for next iteration.
            thisClass = thisClass.GetParent() as IL2CPPClass;
        }
    }

    protected override string GetName() => Manager.Helper.Process.ReadString(128, StringType.ASCII, Address + ((IL2CPP)Manager).Offsets.klass.name, 0);

    protected override string GetNamespace() => Manager.Helper.Process.ReadString(64, StringType.ASCII, Address + ((IL2CPP)Manager).Offsets.klass.namespaze, 0);

    public override UnityClass? GetParent()
    {
        var manager = (IL2CPP)this.Manager;

        return manager.Helper.Process.ReadPointer(Address + manager.Offsets.klass.parent, out IntPtr parent) && parent != IntPtr.Zero
            ? new IL2CPPClass(manager, parent)
            : null;
    }

    public override IntPtr? GetStaticTable() => Manager.Helper.Process.ReadPointer(Address + ((IL2CPP)Manager).Offsets.klass.staticFields, out IntPtr value)
        ? value
        : null;
}
