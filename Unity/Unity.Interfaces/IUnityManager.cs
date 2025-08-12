using System.Collections.Generic;

namespace JHelper.UnityManagers.Interfaces;

public interface IUnityManager<Assembly, Class, Image, Field>
    where Assembly : struct, IUnityAssembly<Image, Class, Field>
    where Class : struct, IUnityClass<Class, Field>
    where Image : struct, IUnityImage<Class, Field>
    where Field : struct, IUnityField
{
    public IEnumerable<Assembly> GetAssemblies();
    public Image? GetImage(string assemblyName);
    public Image? GetDefaultImage();
}
