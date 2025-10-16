// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.DataModel;

internal static class SerializeUtilities
{
    internal static readonly int RuntimeHeaderSize = IntPtr.Size * 2;

    private const BindingFlags DefaultFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;

    internal static int GetOffset(Type type, FieldInfo fieldInfo)
    {
        unsafe
        {
            var offset = EngineHelper.GetFieldOffset(fieldInfo);

            if (type.IsClass)
            {
                // We need to do this because the offset is relative to the start of the class, not the start of the fields
                // We are getting the pointer to the first field of the class
                offset -= RuntimeHeaderSize;
            }

            return offset;
        }
    }

    internal static int GetOffset(FieldInfo fieldInfo, bool isDeclaringTypeClass)
    {
        unsafe
        {
            var offset = EngineHelper.GetFieldOffset(fieldInfo);
            if (isDeclaringTypeClass)
            {
                // We need to do this because the offset is relative to the start of the class, not the start of the fields
                // We are getting the pointer to the first field of the class
                offset -= RuntimeHeaderSize;
            }

            return offset;
        }
    }

    internal static int OffsetOf(Type type, FieldInfo fieldInfo)
    {
        return GetOffset(type, fieldInfo);
    }

    private class ByteWrapper
    {
        internal byte firstByte;

        internal ByteWrapper(byte _firstByte)
        {
            firstByte = _firstByte;
        }
    }

    private static ref byte GetBasePointerObject<T>(ref T data)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        unsafe
        {
            byte* ptr = (byte*)handle.AddrOfPinnedObject().ToPointer();
            return ref *ptr;
        }
    }

    private static ref byte GetBasePointerContent<T>(ref T data)
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        unsafe
        {
            byte* ptr = (byte*)handle.AddrOfPinnedObject().ToPointer();
            return ref *ptr;
        }
    }

    internal static ref byte GetBasePointerForUdm<T>(ref T data)
    {
        if (data is string || data is IList || data is Array)
            return ref GetBasePointerObject(ref data);

        if (data.GetType().IsClass || IsBoxed(data))
            return ref GetBasePointerContent(ref data);

        return ref GetBasePointerObject(ref data);
    }

    internal static Type GetType(string name) => Type.GetType(name);

    internal static FieldInfo GetFieldInfo(Type t, string name, BindingFlags flags = DefaultFlags)
    {
        if (t.IsArray)
        {
            throw new Exception("Arrays do not have fields");
        }

        var field = t.GetField(name, flags);
        // Get field doesn't retrieve private fields on base classes, traverse up the hierarchy to find them
        // Example: ButtonClickedEvent -> UnityEvent -> UnityEventBase
        if (field == null && t.BaseType != null)
        {
            return GetFieldInfo(t.BaseType, name, flags);
        }
        return field;
    }

    internal static FieldInfo[] GetFields(Type t, BindingFlags flags = DefaultFlags)
    {
        if (t.IsArray)
        {
            throw new Exception("Arrays do not have fields");
        }

        var fields = t.GetFields(flags);
        // Get field doesn't retrieve private fields on base classes, traverse up the hierarchy to find them
        // Example: ButtonClickedEvent -> UnityEvent -> UnityEventBase
        if (t.BaseType != null)
        {
            var parentFields = GetFields(t.BaseType, flags);
            if (parentFields.Length != 0)
            {
                var allFields = new FieldInfo[fields.Length + parentFields.Length];
                fields.CopyTo(allFields, 0);
                parentFields.CopyTo(allFields, fields.Length);
                return allFields;
            }
        }
        return fields;
    }
    internal static bool IsBoxed<T>(T value)
    {
        return (typeof(T).IsInterface || typeof(T) == typeof(object)) &&
               value != null &&
               value.GetType().IsValueType;
    }

    internal static bool ExtendsANativeType(Type type)
    {
        return type.GetCustomAttributes(typeof(ExtensionOfNativeClassAttribute), true).Length != 0;
    }

    internal static bool ExtendsANativeType(UnityEngine.Object unityObj)
    {
        return !object.ReferenceEquals(null, unityObj) &&
            ExtendsANativeType(unityObj.GetType());
    }
}
