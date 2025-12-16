// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

// Enum to describe the field type
internal enum FieldType
{
    Type_char,
    Type_int8,
    Type_bool,
    Type_byte,
    Type_int16,
    Type_uint16,
    Type_int32,
    Type_uint32,
    Type_int64,
    Type_uint64,
    Type_float,
    Type_double,

    Type_ObjectID,
    Type_UTF8String,
    Type_UTF16String,
    Type_Reference,

    // For completeness from the point of view of the MetadataReader
    Type_NoSupported,
    Type_ValueType,
    Type_GenericField,

    Type_SystemType,
    Type_Pointer
}
