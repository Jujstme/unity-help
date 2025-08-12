using System.Collections.Generic;

namespace JHelper.UnityManagers.Interfaces;

public interface IUnityImage<Class, Field>
    where Class : struct, IUnityClass<Class, Field>
    where Field : struct, IUnityField
{
    IEnumerable<Class> EnumClasses();
    Class? GetClass(string className);
}
