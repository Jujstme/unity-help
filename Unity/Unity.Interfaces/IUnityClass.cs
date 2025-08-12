using System;
using System.Collections.Generic;

namespace JHelper.UnityManagers.Interfaces;

public interface IUnityClass<Class, Field>
    where Class : struct, IUnityClass<Class, Field>
    where Field : struct, IUnityField
{
    public string GetName();
    public string GetNamespace();
    public IEnumerable<Field> EnumFields();
    public Class? GetParent();
    public int? GetFieldOffset(string fieldName);
    public IntPtr? GetStaticTable();
}
