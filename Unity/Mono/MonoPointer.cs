using System;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

/// <summary>
/// A specific implementation of a pointer path resolver for games using the <see cref="Mono"/> backend
/// </summary>
public class MonoPointer(Mono manager, string assemblyName, string className, int noOfParents, params dynamic[] offsets)
{
    private readonly string className = className;
    private readonly int noOfParents = noOfParents;
    private readonly dynamic[] fields = offsets;
    private readonly Mono manager = manager;
    private readonly MonoImage image = manager.GetImage(assemblyName).GetValueOrDefault();

    private IntPtr baseAddress = IntPtr.Zero;
    private readonly int[] offsets = new int[offsets.Length];
    private int resolvedOffsets = 0;
    private MonoClass? startingClass = null;

    private bool FindOffsets()
    {
        if (resolvedOffsets == offsets.Length)
            return true;

        MonoClass _startingClass;
        if (startingClass.HasValue)
            _startingClass = startingClass.Value;
        else
        {
            MonoClass? @class = image.GetClass(className);
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
                MonoClass currentClass;

                if (i == 0)
                {
                    currentClass = _startingClass;
                }
                else
                {
                    if (!manager.Helper.Process.DerefOffsets(currentObject, out IntPtr ptr, 0, 0) || ptr == IntPtr.Zero)
                        return false;
                    currentClass = new MonoClass(manager, ptr);
                }

                if (fields[i] is not string _f)
                    return false;

                var field = currentClass.GetFieldOffset(_f); //.EnumFields().FirstOrDefault(f => f.GetName() == _f).GetOffset();
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
    /// Tries to resolve the pointer path for a game using the <see cref="Mono"/>
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

        address += offsets[^1];
        return true;
    }
}
