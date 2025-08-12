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
        int childCount = 0;
        IntPtr childPtr = IntPtr.Zero;

        if (sceneManager.helper.Process.Is64Bit)
        {
            Span<long> buf = stackalloc long[3];
            if (sceneManager.helper.Process.ReadArray(address + sceneManager.offsets.childrenPointer, buf))
            {
                childCount = (int)buf[2];
                childPtr = (IntPtr)buf[0];
            }
        }
        else
        {
            Span<int> buf = stackalloc int[3];
            if (sceneManager.helper.Process.ReadArray(address + sceneManager.offsets.childrenPointer, buf))
            {
                childCount = (int)buf[2];
                childPtr = (IntPtr)buf[0];
            }
        }

        if (childCount > 0 && childCount <= 128)
        {
            IntPtr[] children = ArrayPool<IntPtr>.Shared.Rent(128);

            if (sceneManager.helper.Process.Is64Bit)
            {
                Span<long> buf = stackalloc long[childCount];
                if (sceneManager.helper.Process.ReadArray<long>(childPtr, buf))
                {
                    for (int i = 0; i < childCount; i++)
                    {
                        children[i] = (IntPtr)buf[i];
                    }
                }
            }
            else
            {
                Span<int> buf = stackalloc int[childCount];
                if (sceneManager.helper.Process.ReadArray<int>(childPtr, buf))
                {
                    for (int i = 0; i < childCount; i++)
                    {
                        children[i] = (IntPtr)buf[i];
                    }
                }
            }

            for (int i = 0; i < childCount; i++)
                yield return new Transform(sceneManager, children[i]);

            ArrayPool<IntPtr>.Shared.Return(children);
        }
    }

    public Transform? GetChild(string name)
    {
        return EnumChildren().FirstOrDefault(c => c.Name == name);
    }

    public IEnumerable<IntPtr> EnumClasses()
    {
        IntPtr gameObject = sceneManager.helper.Process.ReadPointer(address + sceneManager.offsets.gameObject);

        int numberOfComponents = 0;
        IntPtr mainObject = IntPtr.Zero;

        if (sceneManager.helper.Process.Is64Bit)
        {
            Span<long> buf = stackalloc long[3];
            if (sceneManager.helper.Process.ReadArray(gameObject + sceneManager.offsets.gameObject, buf))
            {
                numberOfComponents = (int)buf[2];
                mainObject = (IntPtr)buf[0];
            }
        }
        else
        {
            Span<int> buf = stackalloc int[3];
            if (sceneManager.helper.Process.ReadArray(gameObject + sceneManager.offsets.gameObject, buf))
            {
                numberOfComponents = (int)buf[2];
                mainObject = (IntPtr)buf[0];
            }
        }

        if (numberOfComponents > 0)
        {
            IntPtr[] components = ArrayPool<IntPtr>.Shared.Rent(128);

            if (sceneManager.helper.Process.Is64Bit)
            {
                Span<long> buf = stackalloc long[numberOfComponents * 2];
                if (sceneManager.helper.Process.ReadArray(mainObject, buf))
                {
                    for (int i = 1; i < numberOfComponents; i++)
                    {
                        components[i] = (IntPtr)buf[i * 2 + 1];
                    }
                }
            }
            else
            {
                Span<int> buf = stackalloc int[numberOfComponents * 2];
                if (sceneManager.helper.Process.ReadArray(mainObject, buf))
                {
                    for (int i = 1; i < numberOfComponents; i++)
                    {
                        components[i] = (IntPtr)buf[i * 2 + 1];
                    }
                }
            }

            for (int i = 1; i < numberOfComponents; i++)
            {
                if (sceneManager.helper.Process.ReadPointer(components[i] + sceneManager.offsets.klass, out IntPtr ptr) && ptr != IntPtr.Zero)
                    yield return ptr;
            }

            ArrayPool<IntPtr>.Shared.Return(components);
        }
    }

    public IntPtr? GetClassInstance(string name)
    {
        bool isIL2CPP = sceneManager.isIL2CPP;
        var manager = sceneManager;
        var baseAddress = address;

        return EnumClasses().FirstOrDefault(c => isIL2CPP
            ? manager.helper.Process.ReadString(128, baseAddress, 2 * manager.helper.Process.PointerSize, 0) == name
            : manager.helper.Process.ReadString(128, baseAddress, 0, manager.offsets.klassName, 0) == name);
    }
}