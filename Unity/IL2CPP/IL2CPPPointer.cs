using System;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

/// <summary>
/// A specific implementation of a pointer path resolver for games using the <see cref="IL2CPP"/> backend
/// </summary>
public class IL2CPPPointer
{
    private readonly string className;
    private readonly int noOfParents;
    private readonly dynamic[] fields;
    private readonly IL2CPP manager;
    private readonly IL2CPPImage image;

    private IntPtr baseAddress;
    private readonly int[] offsets;
    private int resolvedOffsets;
    private IL2CPPClass? startingClass;

    public IL2CPPPointer(IL2CPP manager, string assemblyName, string className, int noOfParents, params dynamic[] offsets)
    {
        this.manager = manager;
        this.image = manager.GetImage(assemblyName).GetValueOrDefault();
        this.className = className;
        this.noOfParents = noOfParents;
        this.fields = offsets;

        baseAddress = IntPtr.Zero;
        this.offsets = new int[offsets.Length];
        resolvedOffsets = 0;
        startingClass = null;
    }

    private bool FindOffsets()
    {
        if (resolvedOffsets == offsets.Length)
            return true;

        IL2CPPClass _startingClass;
        if (startingClass.HasValue)
            _startingClass = startingClass.Value;
        else
        {
            IL2CPPClass? @class = image.GetClass(className);
            if (!@class.HasValue)
                return false;

            for (int i = 0; i < noOfParents; i++)
            {
                @class = @class.Value.GetParent();
                if (!@class.HasValue)
                    return false;
            }

            startingClass = @class.Value;
            _startingClass = @class.Value;
        }

        if (baseAddress == IntPtr.Zero)
        {
            IntPtr? ptr = _startingClass.GetStaticTable();
            if (!ptr.HasValue || ptr.Value == IntPtr.Zero)
                return false;
            baseAddress = ptr.Value;
        }

        IntPtr currentObject = baseAddress;
        for (int i = 0; i < resolvedOffsets; i++)
        {
            if (!manager.Helper.Process.ReadPointer(currentObject + offsets[i], out currentObject))
                return false;
        }
        if (currentObject == IntPtr.Zero)
            return false;

        // We keep track of the already resolved offsets in order to skip resolving them again
        for (int i = resolvedOffsets; i < offsets.Length; i++)
        {
            int currentOffset;
            if (fields[i] is int _of)
                currentOffset = _of;
            else
            {
                IL2CPPClass currentClass;

                if (i == 0)
                {
                    currentClass = _startingClass;
                }
                else
                {
                    if (!manager.Helper.Process.ReadPointer(currentObject, out IntPtr ptr))
                        return false;
                    currentClass = new IL2CPPClass(manager, ptr);
                }

                if (fields[i] is not string _f)
                    return false;

                var field = currentClass.GetFieldOffset(_f);
                if (field.HasValue)
                    currentOffset = field.Value;
                else
                    return false;
            }

            offsets[i] = currentOffset;
            resolvedOffsets++;

            if (!manager.Helper.Process.ReadPointer(currentObject + currentOffset, out currentObject))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to resolve the pointer path for a game using the <see cref="IL2CPP"/>
    /// backend and gives the memory address at the end of the path.
    /// </summary>
    public bool Deref(out IntPtr address)
    {
        if (!FindOffsets())
        {
            address = IntPtr.Zero;
            return false;
        }

        address = baseAddress;

        for (int i = 0; i < offsets.Length - 1; i++)
        {
            if (!manager.Helper.Process.ReadPointer(address + offsets[i], out address))
                return false;
        }

        address += offsets[offsets.Length - 1];
        return true;
    }
}
