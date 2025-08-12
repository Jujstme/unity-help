using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop.API;
using System;

namespace JHelper.UnityManagers.Mono;

public partial class Mono
{
    public LazyWatcher<T> Make<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        MonoPointer pointer = new(this, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T>(Helper._tickCounter, (_, _) => pointer.Deref(out IntPtr address) ? Helper.Process.Read<T>(address) : default);
    }

    public LazyWatcher<T> Make<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return Make<T>("Assembly-CSharp", @class, parents, offsets);
    }

    public LazyWatcher<string> MakeString(string assemblyName, string @class, int parents, params dynamic[] offsets)
    {
        MonoPointer pointer = new(this, assemblyName, @class, parents, offsets);
        return new LazyWatcher<string>(Helper._tickCounter, (_, _) =>
        {
            if (!pointer.Deref(out IntPtr address) || !Helper.Process.ReadPointer(address, out address))
                return string.Empty;

            int pointerSize = Helper.Process.PointerSize;

            if (!Helper.Process.Read<int>(address + pointerSize * 2, out int length))
                return string.Empty;

            return Helper.Process.ReadString(length, StringType.Unicode, address + pointerSize * 2 + 4);
        });
    }

    public LazyWatcher<string> MakeString(string @class, int parents, params dynamic[] offsets)
    {
        return MakeString("Assembly-CSharp", @class, parents, offsets);
    }

    public LazyWatcher<T[]> MakeArray<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        MonoPointer pointer = new(this, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T[]>(Helper._tickCounter, (_, _) =>
        {

            if (!pointer.Deref(out IntPtr address) || !Helper.Process.ReadPointer(address, out address))
                return [];

            int pointerSize = Helper.Process.PointerSize;

            if (!Helper.Process.Read<int>(address + pointerSize * 3, out int length))
                return [];

            T[] buffer = new T[length];
            Helper.Process.ReadArray<T>(address + pointerSize * 4, buffer);

            return buffer;
        });
    }

    public LazyWatcher<T[]> MakeArray<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return MakeArray<T>("Assembly-CSharp", @class, parents, offsets);
    }

    public LazyWatcher<T[]> MakeList<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        MonoPointer pointer = new(this, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T[]>(Helper._tickCounter, (_, _) =>
        {
            if (!pointer.Deref(out IntPtr address) || !Helper.Process.ReadPointer(address, out address))
                return [];

            int pointerSize = Helper.Process.PointerSize;

            if (!Helper.Process.Read<int>(address + pointerSize * 3, out int count) || !Helper.Process.ReadPointer(address + pointerSize * 2, out IntPtr items))
                return [];

            T[] buffer = new T[count];
            Helper.Process.ReadArray<T>(items + pointerSize * 4, buffer);

            return buffer;
        });
    }

    public LazyWatcher<T[]> MakeList<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return MakeList<T>("Assembly-CSharp", @class, parents, offsets);
    }
}
