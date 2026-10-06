// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

// Native code fills FieldHandle, so the C# side never assigns its fields.
#pragma warning disable 649

namespace Unity.Scripting.Reflection
{
    // CoreCLR needs the declaring class to resolve a field declared on a generic type
    internal unsafe partial struct FieldHandle : IEquatable<FieldHandle>
    {
        IntPtr m_Ptr;
        IntPtr m_Parent;

        public static FieldHandle Empty => default;

        public bool Equals(FieldHandle other) => m_Ptr == other.m_Ptr && m_Parent == other.m_Parent;
        public override bool Equals(object obj) => obj is FieldHandle other && Equals(other);
        public override int GetHashCode() => m_Ptr.GetHashCode() * 31 + m_Parent.GetHashCode();
        public static bool operator ==(FieldHandle a, FieldHandle b) => a.Equals(b);
        public static bool operator !=(FieldHandle a, FieldHandle b) => !a.Equals(b);

        public string GetName()
        {
            return GetFieldName(m_Ptr, m_Parent);
        }

        public TypeHandle GetFieldType()
        {
            return new TypeHandle(GetFieldType(m_Ptr, m_Parent));
        }

        public bool IsFieldTypePointer()
        {
            return IsFieldTypePointer(m_Ptr, m_Parent);
        }

        public int GetOffset()
        {
            return GetFieldOffset(m_Ptr, m_Parent);
        }

        public bool IsStatic()
        {
            return IsFieldStatic(m_Ptr, m_Parent);
        }

        public AttributeHandleCollection GetCustomAttributes(TypeHandle attributeType, Allocator allocator = Allocator.Temp)
        {
            ScriptingReflectionAssert.IsTrue(AttributeHandleCollection.IsSupported(allocator));
            if (attributeType == TypeHandle.Empty)
                return AttributeHandleCollection.Empty;

            var handles = stackalloc IntPtr[AttributeHandleCollection.k_StackCapacity];
            int count = GetFieldCustomAttributes(m_Ptr, m_Parent, attributeType.Value, allocator,
                (IntPtr)handles, AttributeHandleCollection.k_StackCapacity);

            if (count <= AttributeHandleCollection.k_StackCapacity)
                return AttributeHandleCollection.FromBuffer(handles, count, allocator);

            var collection = AttributeHandleCollection.Allocate(count, allocator);
            GetFieldCustomAttributes(m_Ptr, m_Parent, attributeType.Value, allocator, collection.UnsafeBuffer, count);
            return collection;
        }
    }
}
