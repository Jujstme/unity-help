using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

/// <summary>
/// Represents an IL2CPP assembly in the target Unity game process.
/// </summary>
public readonly record struct IL2CPPAssembly : IUnityAssembly<IL2CPPImage, IL2CPPClass, IL2CPPField>
{
    /// <summary>
    /// The memory address of the assembly in the target process.
    /// </summary>
    internal readonly IntPtr assembly;

    private readonly IL2CPP manager;

    internal IL2CPPAssembly(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.assembly = address;
    }

    /// <summary>
    /// Retrieves the IL2CPP image associated with this assembly.
    /// Returns null if the image pointer is invalid or cannot be read.
    /// </summary>
    public IL2CPPImage? GetImage()
    {
        return manager.Helper.Process.ReadPointer(assembly + manager.Offsets.MonoAssembly_Image, out IntPtr value) && value != IntPtr.Zero
            ? new IL2CPPImage(manager, value)
            : null;
    }

    /// <summary>
    /// Gets the name of the assembly as a string.
    /// </summary>
    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, assembly + manager.Offsets.MonoAssembly_Aname + manager.Offsets.MonoAssemblyName_Name, 0);
    }
}

/// <summary>
/// Represents an IL2CPP image, which contains metadata such as types and classes within an assembly.
/// </summary>
public readonly record struct IL2CPPImage : IUnityImage<IL2CPPClass, IL2CPPField>
{
    /// <summary>
    /// The memory address of the image in the target process.
    /// </summary>
    internal readonly IntPtr image;

    private readonly IL2CPP manager;

    internal IL2CPPImage(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.image = address;
    }

    /// <summary>
    /// Enumerates all classes defined in this image.
    /// Reads type count and metadata pointers, then iterates through class pointers.
    /// </summary>
    public IEnumerable<IL2CPPClass> EnumClasses()
    {
        ProcessMemory process = manager.Helper.Process;
        IntPtr image = this.image;

        return process.Is64Bit
            ? EnumClassesInternal<long>(manager)
            : EnumClassesInternal<int>(manager);

        IEnumerable<IL2CPPClass> EnumClassesInternal<T>(IL2CPP manager) where T : unmanaged
        {
            int typeCount = process.Read<int>(image + manager.Offsets.MonoImage_TypeCount);
            if (typeCount == 0)
                yield break;

            IntPtr metadataPtr = manager.Version != IL2CPPVersion.Base && manager.Version != IL2CPPVersion.V2019
                ? process.ReadPointer(image + manager.Offsets.MonoImage_MetadataHandle)
                : image + manager.Offsets.MonoImage_MetadataHandle;

            if (metadataPtr == IntPtr.Zero)
                yield break;

            if (!process.Read<int>(metadataPtr, out int metadataHandle) || metadataHandle == 0)
                yield break;

            if (!process.ReadPointer(manager.TypeInfoDefinitionTable, out IntPtr typeInfoTablePtr) || typeInfoTablePtr == IntPtr.Zero)
                yield break;

            IntPtr ptr = typeInfoTablePtr + metadataHandle * process.PointerSize;

            T[] buffer = ArrayPool<T>.Shared.Rent(typeCount);
            try
            {
                if (process.ReadArray(ptr, buffer.AsSpan(0, typeCount)))
                {
                    for (int i = 0; i < typeCount; i++)
                    {
                        IntPtr klass = Unsafe.ToIntPtr(buffer[i]);
                        if (klass != IntPtr.Zero)
                            yield return new IL2CPPClass(manager, klass);
                    }
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Retrieves the class with the specified name from the image.
    /// Returns null if the class is not found.
    /// </summary>
    /// <param name="className">The name of the class to find.</param>
    public IL2CPPClass? GetClass(string className)
    {
        using (var enumerator = EnumClasses().Where(c => c.GetName() == className).GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current
                : null;
        }
    }
}
