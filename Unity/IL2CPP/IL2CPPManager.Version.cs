using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public partial class IL2CPP
{
    public static IL2CPPVersion DetectVersion(ProcessMemory Memory)
    {
        ProcessModule UnityPlayer = Memory.Modules["UnityPlayer.dll"];

        int major = UnityPlayer.FileVersionInfo.FileMajorPart;
        if (major < 2019)
            return IL2CPPVersion.Base;

        IntPtr ptr = Memory.Scan(new ScanPattern(6, "48 2B ?? 48 2B ?? ?? ?? ?? ?? 48 F7 ?? 48") { OnFound = addr => addr + 0x4 + Memory.Read<int>(addr) }, Memory.Modules["GameAssembly.dll"]);            
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to identify the current version of IL2CPP");
        if (!Memory.ReadPointer(ptr, out ptr) || ptr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to read the IL2CPP version pointer");
        if (!Memory.Read(ptr + 0x4, out uint version) || version == 0 || version == 0xFFFFFFFF)
            throw new InvalidOperationException("Failed to read the IL2CPP version");

        return version switch
        {
            >= 27 => IL2CPPVersion.V2020,
            _ => IL2CPPVersion.V2019,
        };
    }
}

public enum IL2CPPVersion
{
    Base,
    V2019,
    V2020,
}
