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
    public Image? GetImage(string assemblyName)
    {
        using (IEnumerator<Assembly> enumerator = GetAssemblies().Where(a => a.GetName() == assemblyName).GetEnumerator())
        {
            return enumerator.MoveNext()
                ? enumerator.Current.GetImage()
                : null;
        }
    }

    /// <summary>
    /// Gets the default Image for "Assembly-CSharp".
    /// </summary>
    public Image? GetDefaultImage() => GetImage("Assembly-CSharp");
}
