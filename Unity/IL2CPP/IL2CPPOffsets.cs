using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.IL2CPP;

public readonly struct IL2CPPOffsets
{
    internal readonly byte MonoAssembly_Image;
    internal readonly byte MonoAssembly_Aname;
    internal readonly byte MonoAssemblyName_Name;
    internal readonly byte MonoImage_TypeCount;
    internal readonly byte MonoImage_MetadataHandle;
    internal readonly byte MonoClass_Name;
    internal readonly byte MonoClass_NameSpace;
    internal readonly byte MonoClass_Fields;
    internal readonly short MonoClass_FieldCount;
    internal readonly byte MonoClass_StaticFields;
    internal readonly byte MonoClass_Parent;
    internal readonly byte MonoClassField_StructSize;
    internal readonly byte MonoClassField_Name;
    internal readonly byte MonoClassField_Offset;

    internal IL2CPPOffsets(IL2CPPVersion version, ProcessMemory process)
    {
        if (process.Is64Bit)
        {
            if (version == IL2CPPVersion.Base)
            {
                MonoAssembly_Image = 0x0;
                MonoAssembly_Aname = 0x18;
                MonoAssemblyName_Name = 0x0;
                MonoImage_TypeCount = 0x1C;
                MonoImage_MetadataHandle = 0x18; // MonoImage.typeStart
                MonoClass_Name = 0x10;
                MonoClass_NameSpace = 0x18;
                MonoClass_Fields = 0x80;
                MonoClass_FieldCount = 0x114;
                MonoClass_StaticFields = 0xB8;
                MonoClass_Parent = 0x58;
                MonoClassField_StructSize = 0x20;
                MonoClassField_Name = 0x0;
                MonoClassField_Offset = 0x18;
            }
            else if (version == IL2CPPVersion.V2019)
            {
                MonoAssembly_Image = 0x0;
                MonoAssembly_Aname = 0x18;
                MonoAssemblyName_Name = 0x0;
                MonoImage_TypeCount = 0x1C;
                MonoImage_MetadataHandle = 0x18; // MonoImage.typeStart
                MonoClass_Name = 0x10;
                MonoClass_NameSpace = 0x18;
                MonoClass_Fields = 0x80;
                MonoClass_FieldCount = 0x11C;
                MonoClass_StaticFields = 0xB8;
                MonoClass_Parent = 0x58;
                MonoClassField_StructSize = 0x20;
                MonoClassField_Name = 0x0;
                MonoClassField_Offset = 0x18;
            }
            else
            {
                MonoAssembly_Image = 0x0;
                MonoAssembly_Aname = 0x18;
                MonoAssemblyName_Name = 0x0;
                MonoImage_TypeCount = 0x18;
                MonoImage_MetadataHandle = 0x28;
                MonoClass_Name = 0x10;
                MonoClass_NameSpace = 0x18;
                MonoClass_Fields = 0x80;
                MonoClass_FieldCount = 0x120;
                MonoClass_StaticFields = 0xB8;
                MonoClass_Parent = 0x58;
                MonoClassField_StructSize = 0x20;
                MonoClassField_Name = 0x0;
                MonoClassField_Offset = 0x18;
            }
        }
        else
        {
            throw new NotSupportedException("JHelper only supports 64-bit IL2CPP.");
        }
    }
}
