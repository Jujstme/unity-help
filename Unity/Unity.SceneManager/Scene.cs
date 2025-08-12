using JHelper.Common.ProcessInterop.API;
using System;

namespace JHelper.UnityManagers.SceneManager;

public readonly partial record struct Scene(SceneManager manager, IntPtr address)
{
    private readonly SceneManager manager = manager;
    private readonly IntPtr address = address;

    /// <summary>
    /// Returns true if the address of the scene still points to valid memory
    /// </summary>
    /// <returns></returns>
    public readonly bool IsValid
    {
        get => manager.helper.Process.Read<byte>(address, out _);
    }

    /// <summary>
    /// Returns the build index of the scene. This index is unique to each scene in the game.
    /// </summary>
    public readonly int Index
    {
        get => manager.helper.Process.Read<int>(address + manager.offsets.buildIndex);
    }

    /// <summary>
    /// Returns the full path to the scene
    /// </summary>
    public readonly string Path
    {
        get => manager.helper.Process.ReadString(128, StringType.AutoDetect, address + manager.offsets.assetPath, 0);
    }

    /// <summary>
    /// Returns the name of the scene
    /// </summary>
    public readonly string Name
    {
        get
        {
            var path = Path;
            int start = path.LastIndexOf('/');
            int end = path.LastIndexOf('.');
            return start == -1 || end < start ? string.Empty : path[(start + 1)..end];
        }
    }
}