using JHelper.Logging;
using JHelper.UnityManagers.Abstractions;
using System;

namespace JHelper;

/// <summary>
/// A specific implementation of a pointer path resolver for games running under the Unity engine.
/// This class is responsible for resolving field offsets and dereferencing memory addresses
/// to access game data in Unity processes.
/// </summary>
public class UnityPointer
{
    // The name of the Unity class to start resolving the path from
    private readonly string className;

    // The number of parent classes to traverse upwards
    private readonly int noOfParents;

    // The fields or offsets used for pointer traversal (can be int offsets or string field names).
    private readonly dynamic[] fields;

    // The Unity manager class that provides access to process memory and image/class data.
    private readonly UnityManager manager;

    // Cached Unity image reference (resolved once).
    private UnityImage? image = null;

    // Cached starting class reference (resolved once).
    private UnityClass? startingClass = null;

    // The name of the Unity assembly where the class is located.
    private string assemblyName;


    // The base memory address of the resolved pointer chain.
    private IntPtr baseAddress;

    // The resolved integer offsets for the pointer path.
    private readonly int[] offsets;

    // Tracks how many offsets have already been resolved.
    private int resolvedOffsets;

    /// <summary>
    /// Creates a new UnityPointer for resolving pointer chains inside a Unity game.
    /// </summary>
    /// <param name="manager">The Unity manager instance that provides memory access and metadata.</param>
    /// <param name="assemblyName">The name of the Unity assembly containing the class.</param>
    /// <param name="className">The name of the Unity class to resolve.</param>
    /// <param name="noOfParents">The number of parent classes to traverse upward from the target class.</param>
    /// <param name="offsets">A series of offsets (int) or field names (string) used to traverse the pointer path.</param>
    public UnityPointer(UnityManager manager, string assemblyName, string className, int noOfParents, params dynamic[] offsets)
    {
        this.manager = manager;
        this.className = className;
        this.noOfParents = noOfParents;
        fields = offsets;
        this.assemblyName = assemblyName;

        baseAddress = IntPtr.Zero;
        this.offsets = new int[offsets.Length];
        resolvedOffsets = 0;
    }

    /// <summary>
    /// Attempts to resolve the field offsets and base address for the pointer chain.
    /// </summary>
    /// <returns>True if all offsets were successfully resolved; otherwise false.</returns>
    private bool FindOffsets()
    {
        // If already fully resolved, no need to recompute.
        if (resolvedOffsets == offsets.Length)
            return true;

        // Resolve Unity image if not already cached.
        if (image is null)
        {
            if (manager.GetImage(assemblyName) is not UnityImage tImage)
                return false;
            image = tImage;
        }

        // Resolve starting class (with parent traversal if needed).
        UnityClass _startingClass;
        if (startingClass is not null)
            _startingClass = startingClass;
        else
        {
            if (image.GetClass(className) is not UnityClass @class)
                return false;

            for (int i = 0; i < noOfParents; i++)
            {
                if (@class.GetParent() is not UnityClass parent)
                    return false;
                @class = parent;
            }

            startingClass = @class;
            _startingClass = @class;
        }

        // Resolve base address if not already done
        if (baseAddress == IntPtr.Zero)
        {
            IntPtr ptr = _startingClass.Static;
            if (ptr == IntPtr.Zero)
                return false;
            baseAddress = ptr;
        }

        // Reuse previously resolved offsets to avoid redundant lookups.
        IntPtr currentObject = baseAddress;
        for (int i = 0; i < resolvedOffsets; i++)
        {
            if (!manager.Helper.Process.ReadPointer(currentObject + offsets[i], out currentObject) || currentObject == IntPtr.Zero)
                return false;
        }


        // Resolve remaining offsets dynamically.
        for (int i = resolvedOffsets; i < offsets.Length; i++)
        {
            int currentOffset;
            if (fields[i] is int _of)
            {
                // Direct integer offset.
                currentOffset = _of;
            }
            else
            {
                // Later offsets require resolving the class of the current object pointer.
                if (fields[i] is not string _f)
                    return false;

                UnityClass? currentClass;

                if (i == 0)
                {
                    currentClass = _startingClass;
                }
                else
                {
                    IntPtr ptr;
                    if (manager is UnityManagers.IL2CPP.IL2CPP _)
                    {
                        if (!manager.Helper.Process.ReadPointer(currentObject, out ptr))
                            return false;
                    }
                    else if (manager is UnityManagers.Mono.Mono _)
                    {
                        if (!manager.Helper.Process.DerefOffsets(currentObject, out ptr, 0, 0) || ptr == IntPtr.Zero)
                            return false;
                    }
                    else
                        throw new InvalidOperationException();

                    /*
                    currentClass = manager._cachedImages
                        .SelectMany(assembly => assembly.Value._cachedClasses)
                        .FirstOrDefault(klass => klass.Address == ptr);
                    */

                    // Resolve class from pointer.
                    currentClass = image.GetClassByAddress(ptr);

                    if (currentClass is null)
                        return false;
                }

                // Resolve the field offset from the class.
                if (currentClass.GetFieldOffset(_f) is not int field)
                    return false;
                currentOffset = field;
            }

            offsets[i] = currentOffset;
            resolvedOffsets++;

            // Advance pointer using resolved offset.
            if (!manager.Helper.Process.ReadPointer(currentObject + currentOffset, out currentObject))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to resolve the pointer path for a Unity game
    /// and returns the memory address at the end of the path.
    /// </summary>
    /// <param name="address">The resolved memory address if successful.</param>
    /// <returns>True if the address was successfully resolved; otherwise false.</returns>
    public bool DerefOffsets(out IntPtr address)
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

    /// <summary>
    /// Attempts to dereference the resolved pointer and read the value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of value to read (must be unmanaged).</typeparam>
    /// <param name="value">The read value if successful; otherwise default.</param>
    /// <returns>True if the value was successfully read; otherwise false.</returns>
    public bool DerefValue<T>(out T value) where T : unmanaged
    {
        if (!DerefOffsets(out IntPtr address))
        {
            value = default;
            return false;
        }

        return manager.Helper.Process.Read(address, out value);
    }

    /// <summary>
    /// Dereferences the resolved pointer and returns the value of type <typeparamref name="T"/>.
    /// Returns the default value if unsuccessful.
    /// </summary>
    /// <typeparam name="T">The type of value to read (must be unmanaged).</typeparam>
    /// <returns>The value read from memory if successful; otherwise default.</returns>
    public T DerefValue<T>() where T : unmanaged => DerefValue(out T value)
        ? value
        : default;

    public void Info()
    {
        Log.Info($"  => Unity Pointer");
        Log.Info($"    => Base Unity Image: {assemblyName}");
        Log.Info($"    => Base class: {className}");
        Log.Info($"    => Traversing up {noOfParents} parents");
        Log.Info($"    => Fields:");
        foreach (var entry in fields)
            Log.Info($"      => {entry.ToString()}");
    }
}
