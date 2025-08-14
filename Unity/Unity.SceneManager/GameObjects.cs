using JHelper.Common.ProcessInterop;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.SceneManager;

public readonly partial record struct Scene
{
    public IEnumerable<Transform> EnumRootGameObjects()
    {
        ProcessMemory process = manager.helper.Process;
        IntPtr address = this.address;

        return process.Is64Bit
            ? EnumRootGameObjectsInternal<long>(manager)
            : EnumRootGameObjectsInternal<int>(manager);

        IEnumerable<Transform> EnumRootGameObjectsInternal<T>(SceneManager sm) where T : unmanaged
        {
            if (!process.ReadPointer(address + sm.offsets.rootStorageContainer, out IntPtr list_first) || list_first == IntPtr.Zero)
                yield break;

            IntPtr current_list = list_first;

            T[] buf = ArrayPool<T>.Shared.Rent(3);
            try
            {
                while (true)
                {
                    if (!process.ReadArray<T>(current_list, buf.AsSpan(0, 3)))
                        break;

                    yield return new Transform(sm, Unsafe.ToIntPtr(buf[2]));

                    IntPtr first = Unsafe.ToIntPtr(buf[0]);

                    // If the first element is the same as the current list, we reached the end of the list
                    if (list_first == first)
                        break;

                    current_list = first;
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buf);
            }
        }
    }

    public Transform? GetRootGameObject(string name)
    {
        using (var enumerator = EnumRootGameObjects().Where(t => t.Name == name).GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current
                : null;
        }
    }
}