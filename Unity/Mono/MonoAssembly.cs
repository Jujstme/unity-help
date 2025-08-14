using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

/// <summary>
/// Represents a loaded Mono assembly within a hooked Unity process.
/// Provides access to the assembly's name and its <see cref="MonoImage"/>.
/// </summary>
public readonly record struct MonoAssembly : IUnityAssembly<MonoImage, MonoClass, MonoField>
{
    /// <summary>
    /// The memory address of the assembly in the target process.
    /// </summary>
    internal readonly IntPtr assembly;

    private readonly Mono manager;

    internal MonoAssembly(Mono manager, IntPtr address)
    {
        this.manager = manager;
        assembly = address;
    }

    /// <summary>
    /// Reads the assembly's name from memory.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, assembly + manager.Offsets.MonoAssembly_Aname, 0);
    }

    /// <summary>
    /// Retrieves the <see cref="MonoImage"/> associated with this assembly.
    /// </summary>
    public MonoImage? GetImage()
    {
        if (!manager.Helper.Process.ReadPointer(assembly + manager.Offsets.MonoAssembly_Image, out IntPtr value) || value == IntPtr.Zero)
            return null;

        return new MonoImage(manager, value);
    }
}

/// <summary>
/// Represents a Mono image.
/// Contains metadata and references to <see cref="MonoClass"/> instances.
/// </summary>
public readonly record struct MonoImage : IUnityImage<MonoClass, MonoField>
{
    /// <summary>
    /// The memory address of the image in the target process.
    /// </summary>
    internal readonly IntPtr image;

    private readonly Mono manager;

    internal MonoImage(Mono manager, IntPtr address)
    {
        this.manager = manager;
        image = address;
    }

    /// <summary>
    /// Enumerates all classes in the image by reading Mono's internal class cache table.
    /// </summary>
    /// <remarks>
    /// The Mono internal hashtable stores linked lists of classes in buckets.
    /// We iterate through each bucket and traverse linked lists until no more entries are found.
    /// </remarks>
    public IEnumerable<MonoClass> EnumClasses()
    {
        // Defining locals as they are generally faster to access
        ProcessMemory process = manager.Helper.Process;
        IntPtr image = this.image;
        int nextClassCache = manager.Offsets.MonoClassDef_NextClassCache;

        return process.Is64Bit
            ? EnumClassesInternal<long>(manager)
            : EnumClassesInternal<int>(manager);

        IEnumerable<MonoClass> EnumClassesInternal<T>(Mono manager) where T : unmanaged
        {
            if (!process.Read<int>(image + manager.Offsets.MonoImage_ClassCache + manager.Offsets.MonoInternalHashtable_size, out int classCacheSize) || classCacheSize == 0)
                yield break;

            if (!process.ReadPointer(image + manager.Offsets.MonoImage_ClassCache + manager.Offsets.MonoInternalHashtable_table, out IntPtr tableAddr) || tableAddr == IntPtr.Zero)
                yield break;

            T[] buf = ArrayPool<T>.Shared.Rent(classCacheSize);
            try
            {
                if (process.ReadArray(tableAddr, buf.AsSpan(0, classCacheSize)))
                {
                    for (int i = 0; i < classCacheSize; i++)
                    {
                        IntPtr table = Unsafe.ToIntPtr(buf[i]);
                        while (true)
                        {
                            if (!process.Read<T>(table, out T @class))
                                break;
                            IntPtr klass = Unsafe.ToIntPtr(@class);

                            if (klass == IntPtr.Zero)
                                break;
                            yield return new MonoClass(manager, klass);

                            if (!process.ReadPointer(table + nextClassCache, out table) || table == IntPtr.Zero)
                                break;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buf);
            }
        }
    }

    /// <summary>
    /// Retrieves the class with the specified name from the image.
    /// Returns null if the class is not found.
    /// </summary>
    /// <param name="className">The name of the class to find.</param>
    public MonoClass? GetClass(string className)
    {
        using (IEnumerator<MonoClass> enumerator = EnumClasses().Where(c => c.GetName() == className).GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current
                : null;
        }
    }
}
