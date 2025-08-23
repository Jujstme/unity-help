using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public partial class IL2CPP
{
    public static IL2CPPVersion DetectVersion(ProcessMemory Memory)
    {
        ProcessModule UnityPlayer = Memory.Modules["UnityPlayer.dll"];

        int major = UnityPlayer.FileVersionInfo.FileMajorPart;
        int minor = UnityPlayer.FileVersionInfo.FileMinorPart;

        return ForceVersion(major, minor);
    }

    public static IL2CPPVersion ForceVersion(int major, int minor)
    {
        return (major, minor) switch
        {
            ( >= 2023, _) => IL2CPPVersion.V2023,
            ( >= 2021 and < 2023, _) or (2020, >= 2) => IL2CPPVersion.V2020,
            ( >= 2019 and < 2020, _) => IL2CPPVersion.V2019,
            _ => IL2CPPVersion.Base
        };
    }
}

public enum IL2CPPVersion
{
    Base,
    V2019,
    V2020,
    V2023,
}
