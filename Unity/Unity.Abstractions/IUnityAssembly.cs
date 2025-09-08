namespace JHelper.UnityManagers.Abstractions;

/// <summary>
/// Represents a Unity assembly within a Unity process.
/// Provides access to its name and the corresponding <see cref="UnityImage"/>.
/// </summary>
public interface IUnityAssembly
{
    /// <summary>
    /// Gets the name of the Unity assembly.
    /// </summary>
    /// <returns>
    /// The assembly name as a <see cref="string"/>.
    /// </returns>
    string GetName(UnityManager manager);

    /// <summary>
    /// Gets the <see cref="UnityImage"/> associated with this assembly.
    /// </summary>
    /// <returns>
    /// The <see cref="UnityImage"/> if available; otherwise <c>null</c>.
    /// </returns>
    UnityImage? GetImage(UnityManager manager);
}
