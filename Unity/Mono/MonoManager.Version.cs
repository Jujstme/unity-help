using JHelper.Common.ProcessInterop;
using System;
using System.Linq;

namespace JHelper.UnityManagers.Mono;

public partial class Mono
{
#if LIVESPLIT
    public static MonoVersion DetectVersion(global::Unity helper)
#else
    public static MonoVersion DetectVersion(Unity helper)
#endif
    {
        if (helper.Process.Modules.TryGetValue("mono.dll", out _))
        {
            // If the module mono.dll is present, then it's either V1 or V1Cattrs.
            // In order to distinguish between them, we check the first class listed in the
            // default Assembly-CSharp image and check for the pointer to its name, assuming it's using V1.
            // If such pointer matches the address to the assembly image instead, then it's V1Cattrs.
            // The idea is taken from https://github.com/Voxelse/Voxif/blob/main/Voxif.Helpers/Voxif.Helpers.UnityHelper/UnityHelper.cs#L343-L344
            Mono module = new(helper, MonoVersion.V1);
            MonoImage image = module.GetDefaultImage().GetValueOrDefault();
            MonoClass @class = image.EnumClasses().First();

            if (!helper.Process.ReadPointer(@class.@class + module.Offsets.MonoClass_Name, out IntPtr ptr))
                throw new Exception("Unable to identify the version of the Mono struct used by the game");

            return ptr == image.image ? MonoVersion.V1_cattrs : MonoVersion.V1;
        }

        ProcessModule UnityPlayer = helper.Process.Modules["UnityPlayer.dll"];
        int major = UnityPlayer.FileVersionInfo.FileMajorPart;
        int minor = UnityPlayer.FileVersionInfo.FileMinorPart;

        return (major, minor) switch
        {
            (2021, >= 2) or (> 2021, _) => MonoVersion.V3,
            _ => MonoVersion.V2
        };
    }
}

public enum MonoVersion
{
    V1,
    V1_cattrs,
    V2,
    V3,
}
