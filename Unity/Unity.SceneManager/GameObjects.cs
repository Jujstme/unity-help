using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.SceneManager;

public readonly partial record struct Scene
{
    public IEnumerable<Transform> EnumRootGameObjects()
    {
        IntPtr list_first = manager.helper.Process.ReadPointer(address + manager.offsets.rootStorageContainer);
        IntPtr current_list = list_first;


        if (manager.helper.Process.Is64Bit)
        {
            long[] buf = ArrayPool<long>.Shared.Rent(3);
            try
            {
                while (true)
                {
                    if (current_list == IntPtr.Zero)
                        break;

                    IntPtr first;

                    if (!manager.helper.Process.ReadArray<long>(current_list, buf.AsSpan(0, 3)))
                        break;

                    first = (IntPtr)buf[0];

                    current_list = list_first == first
                        ? IntPtr.Zero
                        : first;

                    yield return new Transform(manager, (IntPtr)buf[2]);
                }
            }
            finally
            {
                ArrayPool<long>.Shared.Return(buf);
            }
        }
        else
        {
            int[] buf = ArrayPool<int>.Shared.Rent(3);
            try
            {
                while (true)
                {
                    if (current_list == IntPtr.Zero)
                        break;

                    IntPtr first;

                    if (!manager.helper.Process.ReadArray<int>(current_list, buf.AsSpan(0, 3)))
                        break;

                    first = (IntPtr)buf[0];

                    current_list = list_first == first
                        ? IntPtr.Zero
                        : first;

                    yield return new Transform(manager, (IntPtr)buf[2]);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buf);
            }
        }
    }

    public Transform? GetRootGameObject(string name)
    {
        return EnumRootGameObjects()
            .FirstOrDefault(t => t.Name == name);
    }
}