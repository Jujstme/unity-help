namespace JHelper.UnityManagers.Abstractions;

/// <summary>
/// Represents a field within a Unity class definition.
/// Provides access to the field's name and its memory offset,
/// if available.
/// </summary>
public interface IUnityField
{
    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    /// <returns>
    /// The field's name as a <see cref="string"/>.
    /// </returns>
    public string GetName();

    /// <summary>
    /// Gets the memory offset of the field, if available.
    /// </summary>
    /// <returns>
    /// The field's offset as an <see cref="int"/> if known; 
    /// otherwise <c>null</c>.
    /// </returns>
    public int? GetOffset();
}
