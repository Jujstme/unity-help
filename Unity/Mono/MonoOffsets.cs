using JHelper.Common.ProcessInterop;

namespace JHelper.UnityManagers.Mono;

public readonly struct MonoOffsets
{
    internal readonly byte MonoAssembly_Aname;
    internal readonly byte MonoAssembly_Image;
    internal readonly short MonoImage_ClassCache;
    internal readonly byte MonoInternalHashtable_table;
    internal readonly byte MonoInternalHashtable_size;
    internal readonly short MonoClassDef_NextClassCache;
    internal readonly byte MonoClassDef_Klass;
    internal readonly byte MonoClass_Name;
    internal readonly byte MonoClass_Namespace;
    internal readonly byte MonoClass_Fields;
    internal readonly short MonoClassDef_FieldCount;
    internal readonly short MonoClass_Runtime_Info;
    internal readonly byte MonoClass_VTableSize;
    internal readonly byte MonoClass_Parent;
    internal readonly byte MonoClassField_Name;
    internal readonly byte MonoClassField_Offset;
    internal readonly byte MonoClassRuntimeInfo_Domain_VTables;
    internal readonly byte MonoVTable_VTable;
    internal readonly byte MonoClassFieldAlignment;

    internal MonoOffsets(MonoVersion version, ProcessMemory process)
    {
        if (process.Is64Bit)
        {
            if (version == MonoVersion.V1)
            {
                MonoAssembly_Aname = 0x10;
                MonoAssembly_Image = 0x58;
                MonoImage_ClassCache = 0x3D0;
                MonoInternalHashtable_table = 0x20;
                MonoInternalHashtable_size = 0x18;
                MonoClassDef_NextClassCache = 0x100;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x48;
                MonoClass_Namespace = 0x50;
                MonoClass_Fields = 0xA8;
                MonoClassDef_FieldCount = 0x94;
                MonoClass_Runtime_Info = 0xF8;
                MonoClass_VTableSize = 0x18; // MonoVTable.data
                MonoClass_Parent = 0x30;
                MonoClassField_Name = 0x8;
                MonoClassField_Offset = 0x18;
                MonoClassRuntimeInfo_Domain_VTables = 0x8;
                MonoVTable_VTable = 0x48;
                MonoClassFieldAlignment = 0x20;
            }
            else if (version == MonoVersion.V1_cattrs)
            {
                MonoAssembly_Aname = 0x10;
                MonoAssembly_Image = 0x58;
                MonoImage_ClassCache = 0x3D0;
                MonoInternalHashtable_table = 0x20;
                MonoInternalHashtable_size = 0x18;
                MonoClassDef_NextClassCache = 0x108;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x50;
                MonoClass_Namespace = 0x58;
                MonoClass_Fields = 0xB0;
                MonoClassDef_FieldCount = 0x9C;
                MonoClass_Runtime_Info = 0x100;
                MonoClass_VTableSize = 0x18; // MonoVTable.data
                MonoClass_Parent = 0x30;
                MonoClassField_Name = 0x8;
                MonoClassField_Offset = 0x18;
                MonoClassRuntimeInfo_Domain_VTables = 0x8;
                MonoVTable_VTable = 0x48;
                MonoClassFieldAlignment = 0x20;
            }
            else if (version == MonoVersion.V2)
            {
                MonoAssembly_Aname = 0x10;
                MonoAssembly_Image = 0x60;
                MonoImage_ClassCache = 0x4C0;
                MonoInternalHashtable_table = 0x20;
                MonoInternalHashtable_size = 0x18;
                MonoClassDef_NextClassCache = 0x108;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x48;
                MonoClass_Namespace = 0x50;
                MonoClass_Fields = 0x98;
                MonoClassDef_FieldCount = 0x100;
                MonoClass_Runtime_Info = 0xD0;
                MonoClass_VTableSize = 0x5C;
                MonoClass_Parent = 0x30;
                MonoClassField_Name = 0x8;
                MonoClassField_Offset = 0x18;
                MonoClassRuntimeInfo_Domain_VTables = 0x8;
                MonoVTable_VTable = 0x40;
                MonoClassFieldAlignment = 0x20;
            }
            else
            {
                MonoAssembly_Aname = 0x10;
                MonoAssembly_Image = 0x60;
                MonoImage_ClassCache = 0x4D0;
                MonoInternalHashtable_table = 0x20;
                MonoInternalHashtable_size = 0x18;
                MonoClassDef_NextClassCache = 0x108;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x48;
                MonoClass_Namespace = 0x50;
                MonoClass_Fields = 0x98;
                MonoClassDef_FieldCount = 0x100;
                MonoClass_Runtime_Info = 0xD0;
                MonoClass_VTableSize = 0x5C;
                MonoClass_Parent = 0x30;
                MonoClassField_Name = 0x8;
                MonoClassField_Offset = 0x18;
                MonoClassRuntimeInfo_Domain_VTables = 0x8;
                MonoVTable_VTable = 0x48;
                MonoClassFieldAlignment = 0x20;
            }
        }
        else
        {
            if (version == MonoVersion.V1)
            {
                MonoAssembly_Aname = 0x8;
                MonoAssembly_Image = 0x40;
                MonoImage_ClassCache = 0x2A0;
                MonoInternalHashtable_table = 0x14;
                MonoInternalHashtable_size = 0xC;
                MonoClassDef_NextClassCache = 0xA8;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x30;
                MonoClass_Namespace = 0x34;
                MonoClass_Fields = 0x74;
                MonoClassDef_FieldCount = 0x64;
                MonoClass_Runtime_Info = 0xA4;
                MonoClass_VTableSize = 0xC; // MonoVTable.data
                MonoClass_Parent = 0x24;
                MonoClassField_Name = 0x4;
                MonoClassField_Offset = 0xC;
                MonoClassRuntimeInfo_Domain_VTables = 0x4;
                MonoVTable_VTable = 0x28;
                MonoClassFieldAlignment = 0x10;
            }
            else if (version == MonoVersion.V1_cattrs)
            {
                MonoAssembly_Aname = 0x8;
                MonoAssembly_Image = 0x40;
                MonoImage_ClassCache = 0x2A0;
                MonoInternalHashtable_table = 0x14;
                MonoInternalHashtable_size = 0xC;
                MonoClassDef_NextClassCache = 0xAC;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x34;
                MonoClass_Namespace = 0x38;
                MonoClass_Fields = 0x78;
                MonoClassDef_FieldCount = 0x68;
                MonoClass_Runtime_Info = 0xA8;
                MonoClass_VTableSize = 0xC; // MonoVTable.data
                MonoClass_Parent = 0x24;
                MonoClassField_Name = 0x4;
                MonoClassField_Offset = 0xC;
                MonoClassRuntimeInfo_Domain_VTables = 0x4;
                MonoVTable_VTable = 0x28;
                MonoClassFieldAlignment = 0x10;
            }
            else if (version == MonoVersion.V2)
            {
                MonoAssembly_Aname = 0x8;
                MonoAssembly_Image = 0x44;
                MonoImage_ClassCache = 0x354;
                MonoInternalHashtable_table = 0x14;
                MonoInternalHashtable_size = 0xC;
                MonoClassDef_NextClassCache = 0xA8;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x2C;
                MonoClass_Namespace = 0x30;
                MonoClass_Fields = 0x60;
                MonoClassDef_FieldCount = 0xA4;
                MonoClass_Runtime_Info = 0x84;
                MonoClass_VTableSize = 0x38;
                MonoClass_Parent = 0x20;
                MonoClassField_Name = 0x4;
                MonoClassField_Offset = 0xC;
                MonoClassRuntimeInfo_Domain_VTables = 0x4;
                MonoVTable_VTable = 0x28;
                MonoClassFieldAlignment = 0x10;
            }
            else
            {
                MonoAssembly_Aname = 0x8;
                MonoAssembly_Image = 0x48;
                MonoImage_ClassCache = 0x35C;
                MonoInternalHashtable_table = 0x14;
                MonoInternalHashtable_size = 0xC;
                MonoClassDef_NextClassCache = 0xA0;
                MonoClassDef_Klass = 0x0;
                MonoClass_Name = 0x2C;
                MonoClass_Namespace = 0x30;
                MonoClass_Fields = 0x60;
                MonoClassDef_FieldCount = 0x9C;
                MonoClass_Runtime_Info = 0x7C;
                MonoClass_VTableSize = 0x38;
                MonoClass_Parent = 0x20;
                MonoClassField_Name = 0x4;
                MonoClassField_Offset = 0xC;
                MonoClassRuntimeInfo_Domain_VTables = 0x4;
                MonoVTable_VTable = 0x2C;
                MonoClassFieldAlignment = 0x10;
            }
        }
    }
}
