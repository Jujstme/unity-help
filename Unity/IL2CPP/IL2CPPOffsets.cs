using JHelper.Common.ProcessInterop;
using System;

namespace JHelper.UnityManagers.IL2CPP;

internal class IL2CPPOffsets
{
    internal Assembly assembly { get; }
    internal Image image { get; }
    internal Class klass { get; }
    internal FieldInfo field { get; }

    internal readonly struct Assembly(byte image, byte aname)
    {
        internal readonly byte image = image;
        internal readonly byte aname = aname;
    }

    internal readonly struct Image(byte typeCount, byte metadataHandle)
    {
        internal readonly byte typeCount = typeCount;
        internal readonly byte metadataHandle = metadataHandle; // For older IL2CPP version, this is Image.typeStart
    }

    internal readonly struct Class(byte name, byte namespaze, byte parent, byte fields, byte staticFields, short fieldCount)
    {
        internal readonly byte name = name;
        internal readonly byte namespaze = namespaze;
        internal readonly byte parent = parent;
        internal readonly byte fields = fields;
        internal readonly byte staticFields = staticFields;
        internal readonly short fieldCount = fieldCount;
    }

    internal readonly struct FieldInfo(byte name, byte offset, byte structSize)
    {
        internal readonly byte name = name;
        internal readonly byte offset = offset;
        internal readonly byte structSize = structSize;
    }

    internal IL2CPPOffsets(IL2CPPVersion version, ProcessMemory process)
    {
        if (process.Is64Bit)
        {
            if (version == IL2CPPVersion.Base)
            {
                assembly = new(0x0, 0x18);
                image = new(0x1C, 0x18);
                klass = new(0x10, 0x18, 0x58, 0x80, 0xB8, 0x114);
                field = new(0x0, 0x18, 0x20);
            }
            else if (version == IL2CPPVersion.V2019)
            {
                assembly = new(0x0, 0x18);
                image = new(0x1C, 0x18);
                klass = new(0x10, 0x18, 0x58, 0x80, 0xB8, 0x11C);
                field = new(0x0, 0x18, 0x20);
            }
            else if (version == IL2CPPVersion.V2020)
            {
                assembly = new(0x0, 0x18);
                image = new(0x18, 0x28);
                klass = new(0x10, 0x18, 0x58, 0x80, 0xB8, 0x120);
                field = new(0x0, 0x18, 0x20);
            }
            else if (version == IL2CPPVersion.V2022)
            {
                assembly = new(0x0, 0x18);
                image = new(0x18, 0x28);
                klass = new(0x10, 0x18, 0x58, 0x80, 0xB8, 0x124);
                field = new(0x0, 0x18, 0x20);
            }
            else
            {
                throw new NotSupportedException("Unknown version for the IL2CPP structs.");
            }
        }
        else
        {
            throw new NotSupportedException("JHelper only supports 64-bit IL2CPP.");
        }
    }
}
