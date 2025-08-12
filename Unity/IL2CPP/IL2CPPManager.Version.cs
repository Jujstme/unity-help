using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public partial class IL2CPP
{
    public static IL2CPPVersion DetectVersion(ProcessMemory Memory)
    {
        ProcessModule UnityPlayer = Memory.Modules["UnityPlayer.dll"];
        IntPtr ptr;

        // 202X
        if (Memory.Scan(new ScanPattern(0, "00 32 30 32 ?? 2E"), UnityPlayer) != IntPtr.Zero)
        {
            ptr = Memory.Scan(new ScanPattern(6, "48 2B ?? 48 2B ?? ?? ?? ?? ?? 48 F7 ?? 48"), Memory.Modules["GameAssembly.dll"]);
            
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to identify the current version of IL2CPP");

            int version = Memory.Read<int>(ptr + 0x4 + Memory.Read<int>(ptr), 0x4);
            
            if (version >= 27)
                return IL2CPPVersion.V2020;
            else
                return IL2CPPVersion.V2019;
        }
        else if (Memory.Scan(new ScanPattern(0, "00 32 30 31 39 2E"), UnityPlayer) != IntPtr.Zero)
        {
            return IL2CPPVersion.V2019;
        }
        else
        {
            return IL2CPPVersion.Base;
        }
    }
}

public enum IL2CPPVersion
{
    Base,
    V2019,
    V2020,
}
