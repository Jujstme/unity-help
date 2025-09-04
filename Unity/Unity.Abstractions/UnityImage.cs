using JHelper.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Abstractions;

/// <summary>
/// Represents a Unity image (assembly) loaded in a Unity process.
/// A Unity image contains one or more <see cref="UnityClass"/> definitions.
/// Provides methods for retrieving classes by name or address.
/// </summary>
public abstract class UnityImage
{
    /// <summary>
    /// The memory address of this Unity image in the target process.
    /// </summary>
    public IntPtr Address { get; internal set; }

#pragma warning disable CS8618
    /// <summary>
    /// The name of this Unity image (e.g., "Assembly-CSharp").
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// The Unity manager instance that owns this class.
    /// </summary>
    internal UnityManager Manager;
#pragma warning restore CS8618

    /// <summary>
    /// Cached list of classes contained within this image.
    /// </summary>
    internal List<UnityClass> _cachedClasses = new();

    /// <summary>
    /// Forces the image to load and cache its contained classes.
    /// </summary>
    internal abstract void LoadClasses();

    /// <summary>
    /// Attempts to find a <see cref="UnityClass"/> by its class name.
    /// </summary>
    /// <param name="fullName">The name of the class to retrieve.</param>
    /// <returns>
    /// The <see cref="UnityClass"/> if found; otherwise <c>null</c>.
    /// </returns>
    public UnityClass? GetClass(string fullName)
    {
        int index = fullName.LastIndexOf('.');

        if (index == -1) // No namespace specified
        {
            if (_cachedClasses.FirstOrDefault(c => c.Name == fullName) is UnityClass klass)
                return klass;
            LoadClasses();
            return _cachedClasses.FirstOrDefault(c => c.Name == fullName) is UnityClass kklass
                ? kklass
                : null;
        }
        else if (index == 0) // Root Namespace
        {
            string className = fullName.Substring(1);
            if (_cachedClasses.FirstOrDefault(c => c.Name == className && string.IsNullOrEmpty(c.Namespace)) is UnityClass klass)
                return klass;
            LoadClasses();
            return _cachedClasses.FirstOrDefault(c => c.Name == className && string.IsNullOrEmpty(c.Namespace)) is UnityClass kklass
                ? kklass
                : null;
        }
        else
        {
            string namespaze = fullName.Substring(0, index);
            string className = fullName.Substring(index + 1);

            if (_cachedClasses.FirstOrDefault(c => c.Name == className && c.Namespace == namespaze) is UnityClass klass)
                return klass;
            LoadClasses();
            return _cachedClasses.FirstOrDefault(c => c.Name == className && c.Namespace == namespaze) is UnityClass kklass
                ? kklass
                : null;
        }
    }

    /// <summary>
    /// Attempts to find a <see cref="UnityClass"/> by its memory address.
    /// </summary>
    /// <param name="address">The memory address of the class.</param>
    /// <returns>
    /// The <see cref="UnityClass"/> if found; otherwise <c>null</c>.
    /// </returns>
    internal UnityClass? GetClassByAddress(IntPtr address)
    {
        // First check cached classes.
        if (_cachedClasses.FirstOrDefault(c => c.Address == address) is UnityClass klass)
            return klass;

        // Force reload of all classes if not found.
        LoadClasses();

        // Retry lookup after loading.
        return _cachedClasses.FirstOrDefault(c => c.Address == address) is UnityClass kklass
            ? kklass
            : null;
    }

    /// <summary>
    /// Retrieves a <see cref="UnityClass"/> by name.
    /// Throws if the class cannot be found.
    /// </summary>
    /// <param name="name">The name of the class to retrieve.</param>
    /// <returns>The resolved <see cref="UnityClass"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the class cannot be found.</exception>
    public UnityClass this[string name] => GetClass(name) is UnityClass image
        ? image
        : throw new InvalidOperationException($"Unity Class \"{name}\" not found");

    /// <summary>
    /// Logs all classes contained within this image, sorted by name.
    /// Includes namespace and memory address information.
    /// </summary>
    public void PrintClasses()
    {
        Log.Info($"  => Current Assembly: {Name}");
        LoadClasses();
        foreach (var c in _cachedClasses.OrderBy(c => c.Name))
        {
            if (string.IsNullOrEmpty(c.Namespace))
                Log.Info($"    => {c.Name}: 0x{Address.ToString("X")}");
            else
                Log.Info($"    => {c.Namespace}.{c.Name}: 0x{Address.ToString("X")}");
        }
    }
}
