using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using JHelper.UnityManagers.Abstractions;
using System;
using System.Buffers;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

public readonly record struct MonoAssembly : IUnityAssembly
{
    internal readonly IntPtr assembly;
    private readonly Mono manager;

    internal MonoAssembly(Mono manager, IntPtr address)
    {
        this.manager = manager;
        assembly = address;
    }

    public string GetName()
    {
        return manager.Helper.Process.ReadString(128, StringType.ASCII, assembly + manager.Offsets.assembly.aname, 0);
    }

    public UnityImage? GetImage()
    {
        if (!manager.Helper.Process.ReadPointer(assembly + manager.Offsets.assembly.image, out IntPtr value) || value == IntPtr.Zero)
            return null;

        return new MonoImage(manager, value);
    }
}

public class MonoImage : UnityImage
{
    internal MonoImage(Mono manager, IntPtr address)
    {
        Manager = manager;
        Address = address;
    }

    internal override void LoadClasses()
    {
        Mono manager = (Mono)Manager;

        ProcessMemory process = Manager.Helper.Process;
        IntPtr image = Address;
        int nextClassCache = manager.Offsets.klass.nextClassCache;

        if (process.Is64Bit)
            EnumClassesInternal<long>(manager);
        else
            EnumClassesInternal<int>(manager);

        void EnumClassesInternal<T>(Mono manager) where T : unmanaged
        {
            if (!process.Read<int>(image + manager.Offsets.image.classCache + manager.Offsets.hashTable.size, out int classCacheSize) || classCacheSize == 0)
                return;

            if (!process.ReadPointer(image + manager.Offsets.image.classCache + manager.Offsets.hashTable.size, out IntPtr tableAddr) || tableAddr == IntPtr.Zero)
                return;

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

                            if (!_cachedClasses.Any(k => k.Address == klass))
                                _cachedClasses.Add(new MonoClass(manager, klass));

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
