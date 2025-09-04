using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

public class MonoClass : UnityClass
{
    internal MonoClass(Mono manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    protected override string GetName() => Manager.Helper.Process.ReadString(128, StringType.ASCII, Address + ((Mono)Manager).Offsets.klass.name, 0x0);

    protected override string GetNamespace() => Manager.Helper.Process.ReadString(64, StringType.ASCII, Address + ((Mono)Manager).Offsets.klass.namespaze, 0x0);

    internal override void LoadFields()
    {
        Mono manager = (Mono)Manager;

        ProcessMemory process = Manager.Helper.Process;
        int fieldCountOffset = manager.Offsets.klass.fieldCount;
        int fieldsOffset = manager.Offsets.klass.fields;
        int monoClassFieldAlignment = manager.Offsets.field.alignment;

        MonoClass? thisClass = this;

        while (true)
        {
            if (thisClass is null)
                break;

            if (thisClass.Name == "Object" || thisClass.Namespace == "UnityEngine")
                break;

            if (process.Read<int>(thisClass.Address + fieldCountOffset, out int fieldCount) && fieldCount > 0)
            {
                if (process.ReadPointer(thisClass.Address + fieldsOffset, out IntPtr fields) && fields != IntPtr.Zero)
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var fi = new MonoField(manager, fields + i * monoClassFieldAlignment);
                        if (fi.GetOffset() is not int offset)
                            continue;
                        _cachedFields[fi.GetName()] = offset;
                    }
                }
            }

            // Move to parent class for next iteration.
            thisClass = thisClass.GetParent() as MonoClass;
        }
    }

    public override UnityClass? GetParent()
    {
        Mono manager = (Mono)Manager;

        if (!Manager.Helper.Process.ReadPointer(Address + manager.Offsets.klass.parent, out IntPtr parentAddr))
            return null;

        if (!Manager.Helper.Process.ReadPointer(parentAddr + manager.Offsets.klass.image, out IntPtr imageAddr))
            return null;

        UnityImage? parentImage = Manager._cachedImages.Values.FirstOrDefault(i => i.Address == imageAddr);
        if (parentImage is null)
        {
            Manager.LoadAssemblies();
            parentImage = Manager._cachedImages.Values.FirstOrDefault(i => i.Address == imageAddr);
        }

        if (parentImage is null)
            return null;

        if (parentImage.GetClassByAddress(parentAddr) is UnityClass realParent)
            return realParent;

        realParent = new MonoClass(manager, parentAddr);
        parentImage._cachedClasses.Add(realParent);
        return realParent;
    }

    public override IntPtr? GetStaticTable()
    {
        Mono manager = (Mono)Manager;

        ProcessMemory process = Manager.Helper.Process;

        if (!process.ReadPointer(Address + manager.Offsets.klass.runtimeInfo, out IntPtr runtimeInfo) || runtimeInfo == IntPtr.Zero)
            return IntPtr.Zero;

        if (!process.ReadPointer(runtimeInfo + process.PointerSize, out IntPtr vtables) || vtables == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr ptr;
        if (manager.Version == MonoVersion.V1 || manager.Version == MonoVersion.V1_cattrs)
        {
            ptr = vtables + manager.Offsets.klass.vtableSize;
        }
        else
        {
            vtables += manager.Offsets.vtable.vtable;

            if (!process.Read<int>(Address + manager.Offsets.klass.vtableSize, out int vtable_size) || vtable_size <= 0)
                return IntPtr.Zero;

            ptr = vtables + vtable_size * process.PointerSize;
        }

        return Manager.Helper.Process.ReadPointer(ptr, out IntPtr value)
            ? value
            : null;
    }
}
