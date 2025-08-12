namespace JHelper.UnityManagers.Interfaces;

public interface IUnityAssembly<Image, Class, Field>
    where Image : struct, IUnityImage<Class, Field>
    where Class : struct, IUnityClass<Class, Field>
    where Field : struct, IUnityField
{
    string GetName();
    Image? GetImage();
}
