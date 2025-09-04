using JHelper.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Abstractions;

/// <summary>
/// Base abstraction for managing Unity assemblies and classes within a Unity game.
/// A UnityManager provides access to loaded assemblies (<see cref="UnityImage"/>)
/// and their contained classes (<see cref="UnityClass"/>).
/// 
/// Implementations differ depending on whether the game uses IL2CPP or Mono.
/// </summary>
public abstract class UnityManager
{
    /// <summary>
    /// The parent Unity helper instance that owns this Unity manager.
    /// </summary>
#pragma warning disable CS8618
#if LIVESPLIT
    internal global::Unity Helper;
#else
    internal Unity Helper;
#endif
#pragma warning restore CS8618

    /// <summary>
    /// A local cache of Unity assembly images
    /// </summary>
    internal Dictionary<string, UnityImage> _cachedImages = new();

    /// <summary>
    /// Forces the implementing manager to load and cache all Unity assemblies.
    /// Must be implemented in derived classes (e.g., IL2CPP or Mono).
    /// </summary>
    public abstract void LoadAssemblies();

    /// <summary>
    /// Attempts to retrieve a Unity image by its assembly name.
    /// If the image is not cached, assemblies will be loaded before retrying.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to retrieve.</param>
    /// <returns>
    /// The <see cref="UnityImage"/> if found; otherwise <c>null</c>.
    /// </returns>
    public UnityImage? GetImage(string assemblyName)
    {
        if (_cachedImages.TryGetValue(assemblyName, out var image))
            return image;

        LoadAssemblies();

        if (_cachedImages.TryGetValue(assemblyName, out image))
            return image;
        
        return null;
    }

    /// <summary>
    /// Gets the default Unity image, which usually corresponds to "Assembly-CSharp".
    /// This assembly typically contains most user-defined scripts.
    /// </summary>
    /// <returns>
    /// The default <see cref="UnityImage"/> if available; otherwise <c>null</c>.
    /// </returns>
    public UnityImage? GetDefaultImage() => GetImage("Assembly-CSharp");

    /// <summary>
    /// Retrieves a <see cref="UnityImage"/> by name.
    /// Throws if the image cannot be found.
    /// </summary>
    /// <param name="name">The name of the Unity assembly to retrieve.</param>
    /// <returns>The resolved <see cref="UnityImage"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the image cannot be found.</exception>
    public UnityImage this[string name] => GetImage(name) is UnityImage image
        ? image
        : throw new InvalidOperationException($"Unity Assembly \"{name}\" not found");

    /// <summary>
    /// Logs all currently identified assemblies (images).
    /// </summary>
    public void PrintAssemblies()
    {
        Log.Info("  => Currently identified assemblies:");
        LoadAssemblies();
        foreach (var c in _cachedImages.OrderBy(i => i.Key))
            Log.Info($"    => {c.Key}");
    }
}
