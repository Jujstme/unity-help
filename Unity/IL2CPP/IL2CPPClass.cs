using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

public class IL2CPPClass : UnityClass
{
    internal IL2CPPClass(IL2CPP manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    internal override void LoadFields()
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
                    {
                        var fi = new IL2CPPField(manager, fields + i * manager.Offsets.field.structSize);
                        if (fi.GetOffset() is not int offset)
                            continue;
                        string name = fi.GetName();
                        _cachedFields[name] = offset;
                    }
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
        if (!Manager.Helper.Process.ReadPointer(Address + ((IL2CPP)Manager).Offsets.klass.parent, out IntPtr parent) || parent == IntPtr.Zero)
            return null;

        if (!Manager.Helper.Process.ReadPointer(parent, out IntPtr imageAddr) || imageAddr == IntPtr.Zero)
            return null;

        UnityImage? parentImage = Manager._cachedImages.Values.FirstOrDefault(i => i.Address == imageAddr);
        if (parentImage is null)
        {
            Manager.LoadAssemblies();
            parentImage = Manager._cachedImages.Values.FirstOrDefault(i => i.Address == imageAddr);
        }

        if (parentImage is null)
            return null;

        if (parentImage.GetClassByAddress(parent) is UnityClass realParent)
            return realParent;

        realParent = new IL2CPPClass((IL2CPP)Manager, parent);
        parentImage._cachedClasses.Add(realParent);
        return realParent;
    }

    public override IntPtr? GetStaticTable() => Manager.Helper.Process.ReadPointer(Address + ((IL2CPP)Manager).Offsets.klass.staticFields, out IntPtr value)
        ? value
        : null;
}
