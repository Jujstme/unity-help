using JHelper.Common.ProcessInterop.API;
using System;

namespace JHelper.UnityManagers.SceneManager;

/// <summary>
/// Represents a Unity scene within the game.
/// Encapsulates its memory address and provides access to metadata
/// such as index, path, and name.
/// </summary>
/// <param name="manager">
/// The <see cref="SceneManager"/> instance that owns and resolves this scene.
/// </param>
/// <param name="address">
/// The base memory address of the scene in the Unity process.
/// </param>
public readonly partial record struct Scene(SceneManager manager, IntPtr address)
{
    private readonly SceneManager manager = manager;
    private readonly IntPtr address = address;

    /// <summary>
    /// Indicates whether the scene address points to valid memory.
    /// </summary>
    public readonly bool IsValid => manager.helper.Process.Read<byte>(address, out _);

    /// <summary>
    /// Gets the build index of the scene.
    /// This index is unique per scene in the Unity build.
    /// </summary>
    public readonly int Index => manager.helper.Process.Read<int>(address + manager.offsets.buildIndex);

    /// <summary>
    /// Gets the full asset path of the scene in the game project.
    /// </summary>
    public readonly string Path => manager.helper.Process.ReadString(128, StringType.AutoDetect, address + manager.offsets.assetPath, 0);

    /// <summary>
    /// Gets the name of the scene, derived from its path.
    /// </summary>
    public readonly string Name
    {
        get
        {
            var path = Path;
            int start = path.LastIndexOf('/');
            int end = path.LastIndexOf('.');
            return start == -1 || end < start
                ? string.Empty
                : path[(start + 1)..end];
        }
    }
}