using System.Collections.Generic;
using System.Linq;

namespace JHelper.UnityManagers.Interfaces;

public abstract class UnityManager<Assembly, Class, Image, Field> : IUnityManager<Assembly, Class, Image, Field>
    where Assembly : struct, IUnityAssembly<Image, Class, Field>
    where Class : struct, IUnityClass<Class, Field>
    where Image : struct, IUnityImage<Class, Field>
    where Field : struct, IUnityField
{
    public abstract IEnumerable<Assembly> GetAssemblies();

    /// <summary>
    /// Gets an image by its assembly name.
    /// </summary>
    public Image? GetImage(string assemblyName) => GetAssemblies().FirstOrDefault(a => a.GetName() == assemblyName).GetImage();

    /// <summary>
    /// Gets the default Image for "Assembly-CSharp".
    /// </summary>
    public Image? GetDefaultImage() => GetImage("Assembly-CSharp");
}
