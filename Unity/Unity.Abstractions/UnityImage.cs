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
    public IntPtr Address { get; protected set; }

#pragma warning disable CS8618
    /// <summary>
    /// The Unity manager instance that owns this class.
    /// </summary>
    internal UnityManager Manager { get; set; }
#pragma warning restore CS8618

    /// <summary>
    /// Forces the image to load and cache its contained classes.
    /// </summary>
    internal abstract IEnumerable<UnityClass> EnumClasses();

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
            using (var enumerator = EnumClasses().Where(c => c.Name == fullName).GetEnumerator())
            {
                return enumerator.MoveNext()
                    ? enumerator.Current
                    : null;
            }
        }
        else if (index == 0) // Root Namespace
        {
            string className = fullName.Substring(1);
            using (var enumerator = EnumClasses().Where(c => c.Name == className && string.IsNullOrEmpty(c.Namespace)).GetEnumerator())
            {
                return enumerator.MoveNext()
                    ? enumerator.Current
                    : null;
            }
        }
        else
        {
            string namespaze = fullName.Substring(0, index);
            string className = fullName.Substring(index + 1);
            using (var enumerator = EnumClasses().Where(c => c.Name == className && c.Namespace == namespaze).GetEnumerator())
            {
                return enumerator.MoveNext()
                    ? enumerator.Current
                    : null;
            }
        }
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
        Log.Info($"  => Requested classes for the current Unity Image:");
        foreach (var c in EnumClasses().OrderBy(c => c.Name))
        {
            var name = c.Name;
            var namespaze = c.Name;

            if (string.IsNullOrEmpty(namespaze))
                Log.Info($"    => {name}: 0x{c.Address.ToString("X")}");
            else
                Log.Info($"    => {namespaze}.{name}: 0x{c.Address.ToString("X")}");
        }
    }
}
