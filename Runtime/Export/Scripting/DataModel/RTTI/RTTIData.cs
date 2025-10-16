// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable  enable

using System;

namespace Unity.DataModel;

internal readonly struct RttiData
{
    internal readonly ulong RuntimeOffset;
    internal readonly ulong ModelOffset;
    internal readonly uint CopySize; // Includes possible padding
    internal readonly RttiDataType RttiDataType;
    internal readonly Type? ElementType;
    internal readonly Schema Schema;
    internal readonly uint FieldIndex;

    internal RttiData(ulong runtimeOffset, ulong modelOffset, uint copySize, RttiDataType rttiDataType, Type? elementType, Schema schema, uint fieldIndex)
    {
        RuntimeOffset = runtimeOffset;
        ModelOffset = modelOffset;
        CopySize = copySize;
        RttiDataType = rttiDataType;
        ElementType = elementType;
        Schema = schema;
        FieldIndex = fieldIndex;
    }
}

internal enum RttiDataType
{
    DirectCopy    = 0,
    String        = 1,
    Array         = 2,
    List          = 3,
    Reference     = 4,
    EntityId      = 5,
    DynamicBuffer = 6,
}
