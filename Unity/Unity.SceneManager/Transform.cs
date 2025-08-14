using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;
using JHelper.Common.ProcessInterop.API;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.SceneManager;

public readonly record struct Transform(SceneManager manager, IntPtr address)
{
    private readonly IntPtr address = address;
    private readonly SceneManager sceneManager = manager;

    public string Name
    {
        get => sceneManager.helper.Process.ReadString(128, StringType.AutoDetect, address + sceneManager.offsets.gameObject, sceneManager.offsets.gameObjectName, 0);
    }

    public IEnumerable<Transform> EnumChildren()
    {
        ProcessMemory process = sceneManager.helper.Process;
        IntPtr addr = this.address;
        int childrenPointerOffset = sceneManager.offsets.childrenPointer;

        return process.Is64Bit
            ? EnumChildrenInternal<long>(sceneManager)
            : EnumChildrenInternal<int>(sceneManager);

        IEnumerable<Transform> EnumChildrenInternal<T>(SceneManager sm) where T : unmanaged
        {
            int childCount = 0;
            IntPtr childPtr = IntPtr.Zero;

            using (ArrayRental<T> rental = new(stackalloc T[3]))
            {
                Span<T> buf = rental.Span;
                if (process.ReadArray(addr + childrenPointerOffset, buf))
                {
                    childCount = Unsafe.ToInt(buf[2]);
                    childPtr = Unsafe.ToIntPtr(buf[0]);
                }
            }

            if (childCount > 0 && childCount <= 128)
            {
                T[] children = ArrayPool<T>.Shared.Rent(childCount);
                try
                {
                    if (!process.ReadArray(childPtr, children.AsSpan(0, childCount)))
                        yield break;

                    for (int i = 0; i < childCount; i++)
                        yield return new Transform(sm, Unsafe.ToIntPtr(children[i]));
                }
                finally
                {
                    ArrayPool<T>.Shared.Return(children);
                }
            }
        }
    }

    public Transform? GetChild(string name)
    {
        using (var enumerator = EnumChildren().Where(c => c.Name == name).GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current
                : null;
        }
    }

    public IEnumerable<IntPtr> EnumClasses()
    {
        IntPtr addr = address;

        return sceneManager.helper.Process.Is64Bit
            ? EnumClassesInternal<long>(sceneManager)
            : EnumClassesInternal<int>(sceneManager);

        IEnumerable<IntPtr> EnumClassesInternal<T>(SceneManager sceneManager) where T : unmanaged
        {
            IntPtr gameObject = sceneManager.helper.Process.ReadPointer(addr + sceneManager.offsets.gameObject);
            int numberOfComponents = 0;
            IntPtr mainObject = IntPtr.Zero;

            using (ArrayRental<T> rental = new(stackalloc T[3]))
            {
                Span<T> buf = rental.Span;
                if (sceneManager.helper.Process.ReadArray(gameObject + sceneManager.offsets.gameObject, buf))
                {
                    numberOfComponents = Unsafe.ToInt(buf[2]);
                    mainObject = Unsafe.ToIntPtr(buf[0]);
                }
            }

            if (numberOfComponents == 0)
                yield break;

            T[] components = ArrayPool<T>.Shared.Rent(numberOfComponents * 2);
            try
            {
                if (sceneManager.helper.Process.ReadArray(mainObject, components.AsSpan(0, numberOfComponents)))
                {
                    for (int i = 1; i < numberOfComponents; i++)
                        components[i] = components[i * 2 + 1];
                }

                for (int i = 1; i < numberOfComponents; i++)
                {
                    if (sceneManager.helper.Process.ReadPointer(Unsafe.ToIntPtr(components[i]) + sceneManager.offsets.klass, out IntPtr ptr) && ptr != IntPtr.Zero)
                        yield return ptr;
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(components);
            }
        }
    }

    public IntPtr? GetClassInstance(string name)
    {
        bool isIL2CPP = sceneManager.isIL2CPP;
        var manager = sceneManager;
        var baseAddress = address;

        using (var enumerator = EnumClasses().Where(c => isIL2CPP
            ? manager.helper.Process.ReadString(128, baseAddress, 2 * manager.helper.Process.PointerSize, 0) == name
            : manager.helper.Process.ReadString(128, baseAddress, 0, manager.offsets.klassName, 0) == name)
            .GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current
                : null;
        }
    }
}