using JHelper.Common.ProcessInterop;
using JHelper.UnityManagers.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

/// <summary>
/// Provides access to Mono memory structures for a hooked Unity process.
/// Handles version detection, memory offset resolution, and retrieval of assemblies and images.
/// </summary>
/// <remarks>
/// This class reads metadata and runtime information directly from the target process's memory.
/// It relies on <see cref="MonoOffsets"/> for correct structure layout.
/// </remarks>
public partial class Mono : UnityManager<MonoAssembly, MonoClass, MonoImage, MonoField>
{
    /// <summary>
    /// The parent Unity helper instance that owns this manager.
    /// </summary>
#if LIVESPLIT
    internal global::Unity Helper;
#else
    internal Unity Helper;
#endif

    /// <summary>
    /// The detected Mono metadata structure version.
    /// </summary>
    internal MonoVersion Version { get; }

    /// <summary>
    /// Mono's version-specific memory offsets used for reading Unity runtime data.
    /// </summary>
    internal MonoOffsets Offsets { get; }

    /// <summary>
    /// Pointer to the Mono assemblies collection in the target process.
    /// </summary>
    internal IntPtr Assemblies { get; }

#if LIVESPLIT
    public Mono(global::Unity helper, MonoVersion version)
#else
    public Mono(Unity helper, MonoVersion version)
#endif
    {
        Helper = helper;
        Version = version;

        ProcessMemory process = Helper.Process;
        ProcessModule monoModule = process.Modules.First(m => new string[] { "mono.dll", "mono-2.0-bdwgc.dll" }.Contains(m.ModuleName));
        IntPtr rootdomainFunctionAddress = monoModule.Symbols["mono_assembly_foreach"];
        Offsets = new MonoOffsets(Version, process);

        if (process.Is64Bit)
        {
            Assemblies = process.Scan(new ScanPattern(3, "48 8B 0D") { OnFound = addr => addr + 0x4 + process.Read<int>(addr) }, rootdomainFunctionAddress, 0x100);
        }
        else
        {
            ScanPattern[] patterns =
            [
                new ScanPattern(2, "FF 35") { OnFound = addr => (IntPtr)process.Read<int>(addr) },
                new ScanPattern(2, "8B 0D") { OnFound = addr => (IntPtr)process.Read<int>(addr) },
            ];

            Assemblies = patterns.Select(pattern => process.Scan(pattern, rootdomainFunctionAddress, 0x100)).FirstOrDefault(addr => addr != IntPtr.Zero);
        }

        if (Assemblies == IntPtr.Zero)
            throw new InvalidOperationException("Failed to resolve the Mono assemblies addresses");
    }

#if LIVESPLIT
    public Mono(global::Unity helper)
#else
    public Mono(Unity helper)
#endif
        : this(helper, DetectVersion(helper)) { }

    /// <summary>
    /// Enumerates all assemblies currently loaded.
    /// </summary>
    public override IEnumerable<MonoAssembly> GetAssemblies()
    {
        ProcessMemory process = Helper.Process;
        IntPtr assemblies = Assemblies;

        return process.Is64Bit
            ? GetAssembliesInternal<long>()
            : GetAssembliesInternal<int>();

        IEnumerable<MonoAssembly> GetAssembliesInternal<T>() where T : unmanaged
        {
            if (!process.ReadPointer(assemblies, out IntPtr assembly) || assembly == IntPtr.Zero)
                yield break;

            T[] buffer = ArrayPool<T>.Shared.Rent(2);
            try
            {
                while (assembly != IntPtr.Zero)
                {
                    if (!process.ReadArray<T>(assembly, buffer.AsSpan(0, 2)))
                        break;
                    yield return new MonoAssembly(this, Unsafe.ToIntPtr(buffer[0]));
                    assembly = Unsafe.ToIntPtr(buffer[1]);
                }
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer);
            }
        }
    }
}
