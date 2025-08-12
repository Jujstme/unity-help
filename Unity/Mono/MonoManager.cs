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

        ProcessModule MonoModule = Helper.Process.Modules.First(m => new string[] { "mono.dll", "mono-2.0-bdwgc.dll" }.Contains(m.ModuleName));

        Offsets = new MonoOffsets(Version, Helper.Process);
        IntPtr RootdomainFunctionAddress = MonoModule.Symbols["mono_assembly_foreach"];

        if (Helper.Process.Is64Bit)
        {
            IntPtr ptr = Helper.Process.Scan(new ScanPattern(3, "48 8B 0D"), RootdomainFunctionAddress, 0x100);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to resolve the Mono assemblies addresses");

            Assemblies = ptr + 0x4 + Helper.Process.Read<int>(ptr);
        }
        else
        {
            ScanPattern[] patterns =
            [
                new ScanPattern(2, "FF 35"),
                new ScanPattern(2, "8B 0D"),
            ];

            IntPtr ptr = IntPtr.Zero;
            foreach (ScanPattern pattern in patterns)
            {
                ptr = Helper.Process.Scan(pattern, RootdomainFunctionAddress, 0x100);
                if (ptr != IntPtr.Zero)
                    break;
            }
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to resolve the Mono assemblies addresses");
            if (!Helper.Process.ReadPointer(ptr, out ptr))
                throw new InvalidOperationException("Failed to resolve the Mono assemblies addresses");
            Assemblies = ptr;
        }
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
        IntPtr assembly = Helper.Process.ReadPointer(Assemblies);

        if (Helper.Process.Is64Bit)
        {
            long[] buffer = ArrayPool<long>.Shared.Rent(2);
            try
            {
                while (assembly != IntPtr.Zero)
                {
                    if (!Helper.Process.ReadArray<long>(assembly, buffer.AsSpan(0, 2)))
                        break;
                    assembly = (IntPtr)buffer[1];
                    yield return new MonoAssembly(this, (IntPtr)buffer[0]);
                }
            }
            finally
            {
                ArrayPool<long>.Shared.Return(buffer);
            }
        }
        else
        {
            int[] buffer = ArrayPool<int>.Shared.Rent(2);
            try
            {
                while (assembly != IntPtr.Zero)
                {
                    if (!Helper.Process.ReadArray<int>(assembly, buffer.AsSpan(0, 2)))
                        break;
                    assembly = (IntPtr)buffer[1];
                    yield return new MonoAssembly(this, (IntPtr)buffer[0]);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buffer);
            }
        }
    }
}
