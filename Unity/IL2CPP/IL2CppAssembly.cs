using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace JHelper.UnityManagers.IL2CPP;

public readonly struct IL2CPPAssembly : IUnityAssembly
{
    public IntPtr Address { get; }

    internal IL2CPPAssembly(IntPtr address)
    {
        Address = address;
    }

    public UnityImage? GetImage(UnityManager manager)
    {
        var _manager = (IL2CPP)manager;
        return _manager.Helper.Process.ReadPointer(Address + _manager.Offsets.assembly.image, out IntPtr value) && value != IntPtr.Zero
            ? new IL2CPPImage(_manager, value)
            : null;
    }

    public string GetName(UnityManager manager)
    {
        var _manager = (IL2CPP)manager;
        return _manager.Helper.Process.ReadString(128, StringType.ASCII, Address + _manager.Offsets.assembly.aname, 0);
    }
}

public class IL2CPPImage : UnityImage
{
    internal IL2CPPImage(IL2CPP manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    internal override IEnumerable<UnityClass> EnumClasses()
    {
        ProcessMemory process = Manager.Helper.Process;
        IntPtr image = Address;

        return process.Is64Bit
            ? EnumClassesInternal<long>((IL2CPP)Manager)
            : EnumClassesInternal<int>((IL2CPP)Manager);

        IEnumerable<UnityClass> EnumClassesInternal<T>(IL2CPP manager) where T : unmanaged
        {
            int typeCount = process.Read<int>(image + manager.Offsets.image.typeCount);
            if (typeCount == 0)
                yield break;

            IntPtr metadataPtr = manager.Version != IL2CPPVersion.Base && manager.Version != IL2CPPVersion.V2019
                ? process.ReadPointer(image + manager.Offsets.image.metadataHandle)
                : image + manager.Offsets.image.metadataHandle;

            if (metadataPtr == IntPtr.Zero)
                yield break;

            if (!process.Read<int>(metadataPtr, out int metadataHandle))
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
}
