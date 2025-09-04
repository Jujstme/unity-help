using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Buffers;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

public readonly record struct IL2CPPAssembly : IUnityAssembly
{
    internal readonly IntPtr assembly;
    private readonly IL2CPP manager;

    internal IL2CPPAssembly(IL2CPP manager, IntPtr address)
    {
        this.manager = manager;
        this.assembly = address;
    }

    public UnityImage? GetImage()
    {
        return manager.Helper.Process.ReadPointer(assembly + manager.Offsets.assembly.image, out IntPtr value) && value != IntPtr.Zero
            ? new IL2CPPImage(manager, value)
            : null;
    }

    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, assembly + manager.Offsets.assembly.aname, 0);
    }
}

public class IL2CPPImage : UnityImage
{
    internal IL2CPPImage(IL2CPP manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    internal override void LoadClasses()
    {
        ProcessMemory process = Manager.Helper.Process;
        IntPtr image = Address;

        if (process.Is64Bit)
            LoadClassesInternal<long>((IL2CPP)Manager);
        else
            LoadClassesInternal<int>((IL2CPP)Manager);

        void LoadClassesInternal<T>(IL2CPP manager) where T : unmanaged
        {
            int typeCount = process.Read<int>(image + manager.Offsets.image.typeCount);
            if (typeCount == 0)
                return;

            IntPtr metadataPtr = manager.Version != IL2CPPVersion.Base && manager.Version != IL2CPPVersion.V2019
                ? process.ReadPointer(image + manager.Offsets.image.metadataHandle)
                : image + manager.Offsets.image.metadataHandle;

            if (metadataPtr == IntPtr.Zero)
                return;

            if (!process.Read<int>(metadataPtr, out int metadataHandle) || metadataHandle == 0)
                return;

            if (!process.ReadPointer(manager.TypeInfoDefinitionTable, out IntPtr typeInfoTablePtr) || typeInfoTablePtr == IntPtr.Zero)
                return;

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
                        {
                            if (_cachedClasses.Any(k => k.Address == klass))
                                continue;
                            
                            _cachedClasses.Add(new IL2CPPClass(manager, klass));
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer);
            }
        }
    }
}
