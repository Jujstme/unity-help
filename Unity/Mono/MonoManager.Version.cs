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

            if (module.GetDefaultImage() is not MonoImage image)
                throw new InvalidOperationException("Unable to identify the version of the Mono struct used by the game");

            if (image.EnumClasses().FirstOrDefault() is not MonoClass @class)
                throw new InvalidOperationException("Unable to identify the version of the Mono struct used by the game");

            if (!helper.Process.ReadPointer(@class.Address + module.Offsets.klass.name, out IntPtr ptr))
                throw new InvalidOperationException("Unable to identify the version of the Mono struct used by the game");

            return ptr == image.Address ? MonoVersion.V1_cattrs : MonoVersion.V1;
        }

        ProcessModule UnityPlayer = helper.Process.Modules["UnityPlayer.dll"];
        int major = UnityPlayer.FileVersionInfo.FileMajorPart;
        int minor = UnityPlayer.FileVersionInfo.FileMinorPart;

        return ForceVersion(major, minor);
    }

    public static MonoVersion ForceVersion(int major, int minor)
    {
        return (major, minor) switch
        {
            (2021, >= 2) or ( > 2021, _) => MonoVersion.V3,
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
