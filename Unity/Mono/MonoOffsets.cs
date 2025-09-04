using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.Mono;

internal class MonoOffsets
{
    internal readonly Assembly assembly;
    internal readonly Image image;
    internal readonly HashTable hashTable;
    internal readonly Class klass;
    internal readonly FieldInfo field;
    internal readonly MonoVTable vtable;

    internal readonly struct Assembly(byte aname, byte image)
    {
        internal readonly byte aname = aname;
        internal readonly byte image = image;
    }

    internal readonly struct Image(short classCache)
    {
        internal readonly short classCache = classCache;
    }

    internal readonly struct HashTable(byte size, byte table)
    {
        internal readonly byte size = size;
        internal readonly byte table = table;
    }

    internal readonly struct Class(byte parent, byte image, byte name, byte namespaze, byte vtableSize, byte fields, short runtimeInfo, short fieldCount, short nextClassCache)
    {
        internal readonly byte parent = parent;
        internal readonly byte image = image;
        internal readonly byte name = name;
        internal readonly byte namespaze = namespaze;
        internal readonly byte vtableSize = vtableSize;  // On mono V1 and V1_cattrs, this offset represents MonoVTable.data
        internal readonly byte fields = fields;
        internal readonly short runtimeInfo = runtimeInfo;
        internal readonly short fieldCount = fieldCount;
        internal readonly short nextClassCache = nextClassCache;
    }

    internal readonly struct FieldInfo(byte name, byte offset, byte alignment)
    {
        internal readonly byte name = name;
        internal readonly byte offset = offset;
        internal readonly byte alignment = alignment;
    }


    internal readonly struct MonoVTable(byte vtable)
    {
        internal readonly byte vtable = vtable;
    }

    internal MonoOffsets(MonoVersion version, ProcessMemory process)
    {
        if (process.Is64Bit)
        {
            if (version == MonoVersion.V3)
            {
                assembly = new(0x10, 0x60);
                image = new(0x4D0);
                hashTable = new(0x18, 0x20);
                klass = new(0x30, 0x40, 0x48, 0x50, 0x5C, 0x98, 0xD0, 0x100, 0x108);
                field = new(0x8, 0x18, 0x20);
                vtable = new(0x48);
            }
            else if (version == MonoVersion.V2)
            {
                assembly = new(0x10, 0x60);
                image = new(0x4C0);
                hashTable = new(0x18, 0x20);
                klass = new(0x30, 0x40, 0x48, 0x50, 0x5C, 0x98, 0xD0, 0x100, 0x108);
                field = new(0x8, 0x18, 0x20);
                vtable = new(0x40);
            }
            else if (version == MonoVersion.V1_cattrs)
            {
                assembly = new(0x10, 0x58);
                image = new(0x3D0);
                hashTable = new(0x18, 0x20);
                klass = new(0x30, 0x48, 0x50, 0x58, 0x18, 0xB0, 0x100, 0x9C, 0x108);
                field = new(0x8, 0x18, 0x20);
                vtable = new(0x48);
            }
            else if (version == MonoVersion.V1)
            {
                assembly = new(0x10, 0x58);
                image = new(0x3D0);
                hashTable = new(0x18, 0x20);
                klass = new(0x30, 0x40, 0x48, 0x50, 0x18, 0xA8, 0xF8, 0x94, 0x100);
                field = new(0x8, 0x18, 0x20);
                vtable = new(0x48);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        else
        {
            if (version == MonoVersion.V3)
            {
                assembly = new(0x8, 0x48);
                image = new(0x35C);
                hashTable = new(0x0C, 0x14);
                klass = new(0x20, 0x28, 0x2C, 0x30, 0x38, 0x60, 0x7C, 0x9C, 0xA0);
                field = new(0x4, 0xC, 0x10);
                vtable = new(0x2C);
            }
            else if (version == MonoVersion.V2)
            {
                assembly = new(0x8, 0x44);
                image = new(0x354);
                hashTable = new(0x0C, 0x14);
                klass = new(0x20, 0x28, 0x2C, 0x30, 0x38, 0x60, 0x84, 0xA4, 0xA8);
                field = new(0x4, 0xC, 0x10);
                vtable = new(0x28);
            }
            else if (version == MonoVersion.V1_cattrs)
            {
                assembly = new(0x8, 0x40);
                image = new(0x2A0);
                hashTable = new(0x0C, 0x14);
                klass = new(0x24, 0x30, 0x34, 0x38, 0xC, 0x78, 0xA8, 0x68, 0xAC);
                field = new(0x4, 0xC, 0x10);
                vtable = new(0x28);
            }
            else if (version == MonoVersion.V1)
            {
                assembly = new(0x8, 0x40);
                image = new(0x2A0);
                hashTable = new(0x0C, 0x14);
                klass = new(0x24, 0x2C, 0x30, 0x34, 0xC, 0x74, 0xA4, 0x64, 0xA8);
                field = new(0x4, 0xC, 0x10);
                vtable = new(0x28);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
