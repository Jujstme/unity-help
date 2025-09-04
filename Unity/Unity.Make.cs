using JHelper.Common.MemoryUtils;
using System;
using JHelper.Common.ProcessInterop.API;

#if LIVESPLIT
using JHelper.LiveSplit;
#endif

#if !LIVESPLIT
namespace JHelper;
#endif

public partial class Unity
{
    /// <summary>
    /// Creates a <see cref="LazyWatcher{T}"/> for monitoring a value in memory.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the value to watch.</typeparam>
    /// <param name="assemblyName">The name of the assembly containing the class.</param>
    /// <param name="class">The name of the class containing the field or property.</param>
    /// <param name="parents">The number of parent objects to traverse upward before applying offsets.</param>
    /// <param name="offsets">A sequence of offsets leading to the target value.</param>
    /// <returns>An initialized lazy watcher for the specified value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this method is called inside the LiveSplit <c>startup {}</c> action.
    /// </exception>
    public LazyWatcher<T> Make<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
#if LIVESPLIT
        CheckIsNotStartup($".Make<{typeof(T)}>");
#endif
        JHelper.UnityPointer pointer = new(Manager, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T>(_tickCounter, (_, _) => pointer.DerefValue<T>());
    }

    public LazyWatcher<T> Make<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return Make<T>("Assembly-CSharp", @class, parents, offsets);
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{String}"/> for a string value in memory.
    /// </summary>
    public LazyWatcher<string> MakeString(string assemblyName, string @class, int parents, params dynamic[] offsets)
    {
#if LIVESPLIT
        CheckIsNotStartup($".MakeString");
#endif
        JHelper.UnityPointer pointer = new(Manager, assemblyName, @class, parents, offsets);
        return new LazyWatcher<string>(_tickCounter, (_, _) =>
        {
            if (!pointer.DerefOffsets(out IntPtr address) || !Process.ReadPointer(address, out address))
                return string.Empty;

            int pointerSize = Process.PointerSize;

            if (!Process.Read<int>(address + pointerSize * 2, out int length))
                return string.Empty;

            return Process.ReadString(length, StringType.Unicode, address + pointerSize * 2 + 4);
        });
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{String}"/> for a string value in the default <c>Assembly-CSharp</c> assembly.
    /// </summary>
    public LazyWatcher<string> MakeString(string @class, int parents, params dynamic[] offsets)
    {
        return MakeString("Assembly-CSharp", @class, parents, offsets);
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{T[]}"/> for an array in memory.
    /// </summary>
    public LazyWatcher<T[]> MakeArray<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
#if LIVESPLIT
        CheckIsNotStartup($".MakeArray<{typeof(T)}>");
#endif
        JHelper.UnityPointer pointer = new(Manager, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T[]>(_tickCounter, (_, _) =>
        {

            if (!pointer.DerefOffsets(out IntPtr address) || !Process.ReadPointer(address, out address))
                return [];

            int pointerSize = Process.PointerSize;

            if (!Process.Read<int>(address + pointerSize * 3, out int length))
                return [];

            T[] buffer = new T[length];
            Process.ReadArray<T>(address + pointerSize * 4, buffer);

            return buffer;
        });
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{T[]}"/> for an array in the default <c>Assembly-CSharp</c> assembly.
    /// </summary>
    public LazyWatcher<T[]> MakeArray<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return MakeArray<T>("Assembly-CSharp", @class, parents, offsets);
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{T[]}"/> for a generic list in memory.
    /// </summary>
    public LazyWatcher<T[]> MakeList<T>(string assemblyName, string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
#if LIVESPLIT
        CheckIsNotStartup($".MakeList<{typeof(T)}>");
#endif
        JHelper.UnityPointer pointer = new(Manager, assemblyName, @class, parents, offsets);
        return new LazyWatcher<T[]>(_tickCounter, (_, _) =>
        {
            if (!pointer.DerefOffsets(out IntPtr address) || !Process.ReadPointer(address, out address))
                return [];

            int pointerSize = Process.PointerSize;

            if (!Process.Read<int>(address + pointerSize * 3, out int count) || !Process.ReadPointer(address + pointerSize * 2, out IntPtr items))
                return [];

            T[] buffer = new T[count];
            Process.ReadArray<T>(items + pointerSize * 4, buffer);

            return buffer;
        });
    }

    /// <summary>
    /// Creates a <see cref="LazyWatcher{T[]}"/> for a generic list in memory.
    /// </summary>
    public LazyWatcher<T[]> MakeList<T>(string @class, int parents, params dynamic[] offsets) where T : unmanaged
    {
        return MakeList<T>("Assembly-CSharp", @class, parents, offsets);
    }

#if LIVESPLIT
    /// <summary>
    /// Validates that the current method is not being executed inside the LiveSplit <c>startup {}</c> action.
    /// </summary>
    /// <param name="method">The name of the method being validated (for the exception message).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the method is executed inside the <c>startup {}</c> action.
    /// </exception>
    private static void CheckIsNotStartup(object method)
    {
        if (Actions.CurrentAction == "startup")
            throw new InvalidOperationException($"The {method} method cannot be executed inside the 'startup {{}}' action.");
    }
#endif
}
