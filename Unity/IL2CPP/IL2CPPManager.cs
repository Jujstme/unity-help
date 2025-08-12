using JHelper.Common.ProcessInterop;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Collections.Generic;

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
        if (!helper.Process.Is64Bit)
            throw new InvalidOperationException("32-bit versions of IL2CPP are not supported.");

        if (!helper.Process.Modules.TryGetValue("GameAssembly.dll", out ProcessModule module))
            throw new KeyNotFoundException("GameAssembly.dll not found in process modules.");

        Helper = helper;
        Version = version;
        Offsets = new IL2CPPOffsets(version, helper.Process);

        // Locate IL2CPP assemblies pointer
        Assemblies = helper.Process.Scan(new ScanPattern(12, "48 FF C5 80 3C ?? 00 75 ?? 48 8B 1D") { OnFound = (addr) => addr + 0x4 + helper.Process.Read<int>(addr) }, module);
        if (Assemblies == IntPtr.Zero)
            throw new Exception("Failed while trying to resolve IL2CPP assemblies.");

        // Locate TypeInfoDefinitionTable pointer (different patterns may be required for different Unity versions)
        TypeInfoDefinitionTable = helper.Process.Scan(new ScanPattern(-4, "48 83 3C ?? 00 75 ?? 8B C? E8") { OnFound = (addr) => helper.Process.ReadPointer(addr + 0x4 + helper.Process.Read<int>(addr)) }, module);
        if (TypeInfoDefinitionTable == IntPtr.Zero)
            TypeInfoDefinitionTable = helper.Process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 4C 8D 34 F0") { OnFound = (addr) => helper.Process.ReadPointer(addr + 0x4 + helper.Process.Read<int>(addr)) }, module);
        if (TypeInfoDefinitionTable == IntPtr.Zero)
            throw new Exception("Failed while trying to resolve IL2CPP assemblies.");
    }

    /// <summary>
    /// Enumerates all assemblies currently loaded in IL2CPP.
    /// </summary>
    public override IEnumerable<IL2CPPAssembly> GetAssemblies()
    {
        int count = 0;
        IntPtr assemblies = IntPtr.Zero;

        if (Helper.Process.Is64Bit)
        {
            Span<long> buffer = stackalloc long[2];
            if (Helper.Process.ReadArray<long>(Assemblies, buffer))
            {
                count = (int)((buffer[1] - buffer[0]) / Helper.Process.PointerSize);
                assemblies = (IntPtr)buffer[0];
            }
        }
        else
        {
            Span<int> buffer = stackalloc int[2];
            if (Helper.Process.ReadArray<int>(Assemblies, buffer))
            {
                count = (buffer[1] - buffer[0]) / Helper.Process.PointerSize;
                assemblies = (IntPtr)buffer[0];
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (Helper.Process.ReadPointer(assemblies + i * Helper.Process.PointerSize, out IntPtr addr) && addr != IntPtr.Zero)
                yield return new IL2CPPAssembly(this, addr);
        }
    }
}
