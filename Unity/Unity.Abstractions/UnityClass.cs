using JHelper.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Abstractions;

/// <summary>
/// Abstraction over a Unity class definition within a Unity assembly.
/// Provides metadata access such as name, namespace, parent class,
/// static table, and field offsets.
/// </summary>
public abstract class UnityClass
{
    /// <summary>
    /// The memory address of this Unity class object in the target process
    /// </summary>
    public IntPtr Address { get; internal set; }

#pragma warning disable CS8618
    /// <summary>
    /// The UnityManager that owns this class.
    /// </summary>
    internal UnityManager Manager;
#pragma warning restore CS8618

    /// <summary>
    /// Cached dictionary of field names and their corresponding memory offsets.
    /// </summary>
    internal Dictionary<string, int> _cachedFields = new();

    /// <summary>
    /// Gets the name of this Unity class.
    /// The value is cached after the first lookup.
    /// </summary>
    public string Name
    {
        get
        {
            if (field is not null)
                return field;

            string name = GetName();
            field = name;
            return field;
        }

        init
        {
            field = null;
        }
    }

    /// <summary>
    /// Gets the namespace of this Unity class.
    /// The value is cached after the first lookup.
    /// </summary>
    public string Namespace
    {
        get
        {
            if (field is not null)
                return field;

            string name = GetNamespace();
            field = name;
            return field;
        }

        init
        {
            field = null;
        }
    }

    /// <summary>
    /// Gets the parent class of this Unity class.
    /// The value is cached after the first lookup.
    /// </summary>
    public UnityClass Parent
    {
        get
        {
            if (field is not null)
                return field;
            field = GetParent()!;
            return field;
        }

        init
        {
            field = null;
        }
    }

    /// <summary>
    /// Gets the address for the static fields for this Unity class.
    /// Used for resolving static fields in Unity's memory.
    /// The value is cached after the first lookup.
    /// </summary>
    public IntPtr Static
    {
        get
        {
            if (field != IntPtr.Zero)
                return field;
            field = GetStaticTable() is IntPtr value
                ? value
                : IntPtr.Zero;
            return field;
        }

        init
        {
            field = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Retrieves the class name from Unity’s metadata.
    /// </summary>
    protected abstract string GetName();

    /// <summary>
    /// Retrieves the class namespace from Unity’s metadata.
    /// </summary>
    protected abstract string GetNamespace();

    /// <summary>
    /// Retrieves the parent class of this Unity class from Unity’s metadata.
    /// </summary>
    public abstract UnityClass? GetParent();

    /// <summary>
    /// Retrieves the static table of this Unity class from Unity’s metadata.
    /// </summary>
    public abstract IntPtr? GetStaticTable();

    /// <summary>
    /// Forces the class to load and cache its field offsets.
    /// </summary>
    internal abstract void LoadFields();

    /// <summary>
    /// Attempts to get the memory offset of a given field by name.
    /// Searches both direct field names and compiler-generated backing fields.
    /// </summary>
    /// <param name="fieldName">The name of the field to resolve.</param>
    /// <returns>The memory offset if found; otherwise null.</returns>
    public int? GetFieldOffset(string fieldName)
    {
        // Try lookup in cached fields.
        if (_cachedFields.TryGetValue(fieldName, out var offset))
            return offset;

        // Handle compiler-generated backing field naming convention.
        string backingField = $"<{fieldName}>k__BackingField";
        if (_cachedFields.TryGetValue(backingField, out offset))
            return offset;

        // Force reload of field definitions.
        LoadFields();

        // Retry lookup after loading.
        return _cachedFields.TryGetValue(fieldName, out offset) || _cachedFields.TryGetValue(backingField, out offset)
            ? offset
            : null;
    }

    /// <summary>
    /// Retrieves the offset for the specified field by name.
    /// Throws if the field cannot be found.
    /// </summary>
    /// <param name="name">The name of the field to retrieve.</param>
    /// <returns>The offset for the requested field.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the field cannot be found.</exception>
    public int this[string name] => GetFieldOffset(name) is int offset
        ? offset
        : throw new InvalidOperationException($"Field offset for \"{name}\" not found in {Name}");

    /// <summary>
    /// Prints all cached fields of this Unity class to the log.
    /// Includes offsets and static table information if available.
    /// </summary>
    public void PrintFields()
    {
        LoadFields();

        Log.Info($"  =>  Requested fields for class: {Name}");
        
        if (Static != IntPtr.Zero)
            Log.Info($"      =>  Static table found at address: 0x{Static.ToString("X")}");

        foreach (var c in _cachedFields.OrderBy(p => p.Value))
            Log.Info($"    =>  0x{c.Value.ToString("X")}: {c.Key}");
    }
}
