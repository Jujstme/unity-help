using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.IL2CPP;

/// <summary>
/// Provides access to IL2CPP-specific memory structures for a hooked Unity process.
/// Handles version detection, memory offset resolution, and retrieval of assemblies and images.
/// </summary>
/// <remarks>
/// This class reads metadata and runtime information directly from the target process's memory.
/// It relies on <see cref="IL2CPPOffsets"/> for correct structure layout per Unity/IL2CPP version.
/// </remarks>
public partial class IL2CPP : UnityManager<IL2CPPAssembly, IL2CPPClass, IL2CPPImage, IL2CPPField>
{
    /// <summary>
    /// The parent Unity helper instance that owns this IL2CPP manager.
    /// </summary>
#if LIVESPLIT
    internal global::Unity Helper;
#else
    internal Unity Helper;
#endif

    /// <summary>
    /// The detected IL2CPP metadata structure version.
    /// </summary>
    internal IL2CPPVersion Version { get; }

    /// <summary>
    /// IL2CPP-specific memory offsets used for reading Unity runtime data.
    /// </summary>
    internal IL2CPPOffsets Offsets { get; }

    /// <summary>
    /// Pointer to the IL2CPP assemblies collection in the target process.
    /// </summary>
    internal IntPtr Assemblies { get; }

    /// <summary>
    /// Pointer to the TypeInfoDefinitionTable, which contains metadata about types in IL2CPP.
    /// </summary>
    internal IntPtr TypeInfoDefinitionTable { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IL2CPP"/> manager, automatically detecting the IL2CPP version.
    /// </summary>
    /// <param name="helper">The parent Unity helper.</param>
#if LIVESPLIT
    public IL2CPP(global::Unity helper)
#else
    public IL2CPP(Unity helper)
#endif
        : this(helper, DetectVersion(helper.Process)) { }

#if LIVESPLIT
    private IL2CPP(global::Unity helper, IL2CPPVersion version)
#else
    private IL2CPP(Unity helper, IL2CPPVersion version)
#endif
    {
        ProcessMemory process = helper.Process;

        if (!process.Is64Bit)
            throw new InvalidOperationException("32-bit versions of IL2CPP are not supported.");

        if (!process.Modules.TryGetValue("GameAssembly.dll", out ProcessModule module))
            throw new KeyNotFoundException("GameAssembly.dll not found in process modules.");

        Helper = helper;
        Version = version;
        Offsets = new IL2CPPOffsets(version, process);

        // Locate IL2CPP assemblies pointer
        Assemblies = process.Scan(new ScanPattern(5, "75 ?? 48 8B 1D ?? ?? ?? ?? 48 3B 1D") {  OnFound = (addr) => addr + 0x4 + process.Read<int>(addr) }, module);
        if (Assemblies == IntPtr.Zero)
            throw new Exception("Failed while trying to resolve IL2CPP assemblies.");

        // Locate TypeInfoDefinitionTable pointer (different patterns may be required for different Unity versions)
        {
            IntPtr pMetadata = process.Scan(new ScanPattern(0, "67 6C 6F 62 61 6C 2D 6D 65 74 61 64 61 74 61 2E 64 61 74 00"), module);
            if (pMetadata == IntPtr.Zero)
                throw new Exception("Failed while trying to resolve IL2CPP assemblies (pMetadata).");

            IntPtr lea = process.ScanAll(new ScanPattern(3, "48 8D 0D"), module.BaseAddress, module.ModuleMemorySize).FirstOrDefault(addr => addr + 0x4 + process.Read<int>(addr) == pMetadata);
            if (lea == IntPtr.Zero)
                throw new Exception("Failed while trying to resolve IL2CPP assemblies (lea).");

            IntPtr shr = process.Scan(new ScanPattern(3, "48 C1 E9"), lea, 0x200);
            if (shr == IntPtr.Zero)
                throw new Exception("Failed while trying to resolve IL2CPP assemblies (shr).");

            TypeInfoDefinitionTable = process.Scan(new ScanPattern(3, "48 89 05") { OnFound = addr => addr + 0x4 + process.Read<int>(addr) }, shr, 0x100);
        }

        if (TypeInfoDefinitionTable == IntPtr.Zero)
            throw new Exception("Failed while trying to resolve IL2CPP assemblies (TypeInfoDefinitionTable).");
    }

    /// <summary>
    /// Enumerates all assemblies currently loaded in IL2CPP.
    /// </summary>
    public override IEnumerable<IL2CPPAssembly> GetAssemblies()
    {
        ProcessMemory process = Helper.Process;

        return process.Is64Bit
            ? GetAssembliesInternal<long>()
            : GetAssembliesInternal<int>();

        IEnumerable<IL2CPPAssembly> GetAssembliesInternal<T>() where T : unmanaged
        {
            int count = 0;
            IntPtr assemblies = IntPtr.Zero;

            using (ArrayRental<T> buffer = new(stackalloc T[2]))
            {
                Span<T> buf = buffer.Span;
                if (!process.ReadArray<T>(Assemblies, buf))
                    yield break;
                count = (int)(((nint)Unsafe.ToIntPtr(buf[1]) - (nint)Unsafe.ToIntPtr(buf[0])) / process.PointerSize);
                assemblies = Unsafe.ToIntPtr(buf[0]);
            }

            if (count == 0 || assemblies == IntPtr.Zero)
                yield break;

            T[] addresses = ArrayPool<T>.Shared.Rent(count);
            try
            {
                if (!process.ReadArray(assemblies, addresses.AsSpan(0, count)))
                    yield break;

                for (int i = 0; i < count; i++)
                {
                    if (Unsafe.ToIntPtr(addresses[i]) != IntPtr.Zero)
                        yield return new IL2CPPAssembly(this, Unsafe.ToIntPtr(addresses[i]));
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(addresses);
            }
        }
    }
}
