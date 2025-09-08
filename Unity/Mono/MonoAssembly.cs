using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace JHelper.UnityManagers.Mono;

public readonly record struct MonoAssembly : IUnityAssembly
{
    public IntPtr Address { get; }

    internal MonoAssembly(IntPtr address)
    {
        Address = address;
    }

    public string GetName(UnityManager manager)
    {
        var _manager = (Mono)manager;
        return _manager.Helper.Process.ReadString(128, StringType.ASCII, Address + _manager.Offsets.assembly.aname, 0);
    }

    public UnityImage? GetImage(UnityManager manager)
    {
        var _manager = (Mono)manager;
        if (_manager.Helper.Process.ReadPointer(Address + _manager.Offsets.assembly.image, out IntPtr value) || value == IntPtr.Zero)
            return null;

        return new MonoImage(_manager, value);
    }
}

public class MonoImage : UnityImage
{
    internal MonoImage(Mono manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    internal override IEnumerable<UnityClass> EnumClasses()
    {
        Mono manager = (Mono)Manager;

        ProcessMemory process = Manager.Helper.Process;
        IntPtr image = Address;
        int nextClassCache = manager.Offsets.klass.nextClassCache;

        return process.Is64Bit
            ? EnumClassesInternal<long>(manager)
            : EnumClassesInternal<int>(manager);

        IEnumerable<UnityClass> EnumClassesInternal<T>(Mono manager) where T : unmanaged
        {
            if (!process.Read<int>(image + manager.Offsets.image.classCache + manager.Offsets.hashTable.size, out int classCacheSize) || classCacheSize <= 0)
                yield break;

            if (!process.ReadPointer(image + manager.Offsets.image.classCache + manager.Offsets.hashTable.table, out IntPtr tableAddr) || tableAddr == IntPtr.Zero)
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
}
