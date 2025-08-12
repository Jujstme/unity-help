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
        int classCacheSize = manager.Helper.Process.Read<int>(image + manager.Offsets.MonoImage_ClassCache + manager.Offsets.MonoInternalHashtable_size);

        if (classCacheSize == 0)
            yield break;

        IntPtr tableAddr = classCacheSize == 0
            ? IntPtr.Zero
            : manager.Helper.Process.ReadPointer(image + manager.Offsets.MonoImage_ClassCache + manager.Offsets.MonoInternalHashtable_table);

        if (tableAddr == IntPtr.Zero)
            yield break;

        if (manager.Helper.Process.Is64Bit)
        {
            long[] buf64 = ArrayPool<long>.Shared.Rent(classCacheSize);
            try
            {
                if (manager.Helper.Process.ReadArray<long>(tableAddr, buf64.AsSpan(0, classCacheSize)))
                {
                    for (int i = 0; i < classCacheSize; i++)
                    {
                        IntPtr table = (IntPtr)buf64[i];
                        while (table != IntPtr.Zero)
                        {
                            if (!manager.Helper.Process.Read<long>(table, out long @class) || @class == 0)
                                break;

                            yield return new MonoClass(manager, (IntPtr)@class);
                            table = manager.Helper.Process.ReadPointer(table + manager.Offsets.MonoClassDef_NextClassCache);
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<long>.Shared.Return(buf64);
            }
        }
        else
        {
            int[] buf32 = ArrayPool<int>.Shared.Rent(classCacheSize);
            try
            {
                if (manager.Helper.Process.ReadArray<int>(tableAddr, buf32.AsSpan(0, classCacheSize)))
                {
                    for (int i = 0; i < classCacheSize; i++)
                    {
                        IntPtr table = (IntPtr)buf32[i];
                        while (table != IntPtr.Zero)
                        {
                            if (!manager.Helper.Process.Read<int>(table, out int @class) || @class == 0)
                                break;
                            
                            yield return new MonoClass(manager, (IntPtr)@class);
                            table = manager.Helper.Process.ReadPointer(table + manager.Offsets.MonoClassDef_NextClassCache);
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buf32);
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
        return EnumClasses()
            .FirstOrDefault(c => c.GetName() == className);
    }
}
