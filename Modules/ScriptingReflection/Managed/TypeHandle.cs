// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Scripting.Reflection
{
    internal unsafe partial struct TypeHandle : IEquatable<TypeHandle>
    {
        IntPtr m_Ptr;

        internal TypeHandle(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        internal IntPtr Value => m_Ptr;

        public static TypeHandle Empty => default;

        public const int PublicKeyTokenLength = 8;

        // ECMA-335 II.23.1.5
        const uint k_FieldAttributeFieldAccessMask = 0x0007;
        const uint k_FieldAttributePrivate = 0x0001;
        const uint k_FieldAttributePublic = 0x0006;
        const uint k_FieldAttributeStatic = 0x0010;

        public bool Equals(TypeHandle other) => m_Ptr == other.m_Ptr;
        public override bool Equals(object obj) => obj is TypeHandle other && Equals(other);
        public override int GetHashCode() => m_Ptr.GetHashCode();
        public static bool operator ==(TypeHandle a, TypeHandle b) => a.m_Ptr == b.m_Ptr;
        public static bool operator !=(TypeHandle a, TypeHandle b) => a.m_Ptr != b.m_Ptr;

        // RuntimeTypeHandle.Value is not the backend pointer on CoreCLR, so this goes through the binding
        public static TypeHandle FromType(Type type)
        {
            return new TypeHandle(ClassFromType(type.TypeHandle.Value));
        }

        public Type GetManagedType()
        {
            return GetSystemType(m_Ptr);
        }

        public string GetName()
        {
            return GetClassName(m_Ptr);
        }

        public string GetFullName()
        {
            return GetClassFullName(m_Ptr);
        }

        public byte* GetNameCString()
        {
            return (byte*)GetClassNameCString(m_Ptr);
        }

        public byte* GetNamespaceCString()
        {
            return (byte*)GetClassNamespaceCString(m_Ptr);
        }

        public string GetAssemblyFullName()
        {
            return GetClassAssemblyFullName(m_Ptr);
        }

        // False, with the buffer untouched, when the assembly is not strong named
        public bool GetAssemblyPublicKeyToken(byte* outToken)
        {
            return GetClassAssemblyPubKeyToken(m_Ptr, (IntPtr)outToken);
        }

        public int SizeOf()
        {
            ScriptingReflectionAssert.IsTrue(IsValueType());

            return SizeOf(m_Ptr);
        }

        public bool IsClass()
        {
            return !IsValueType() && !IsInterface();
        }

        public bool IsInterface()
        {
            return IsInterface(m_Ptr);
        }

        public bool IsAbstract()
        {
            return IsAbstract(m_Ptr);
        }

        public bool IsSealed()
        {
            return IsSealed(m_Ptr);
        }

        public bool IsValueType()
        {
            return IsValueType(m_Ptr);
        }

        public bool IsPrimitive()
        {
            return IsPrimitive(m_Ptr);
        }

        // Unmanaged as C# defines it, so a pointer type counts
        public bool IsUnmanaged()
        {
            return IsUnmanaged(m_Ptr);
        }

        public bool IsPointer()
        {
            return IsPointer(m_Ptr);
        }

        public bool IsEnum()
        {
            return IsEnum(m_Ptr);
        }

        public bool IsArray()
        {
            return IsArray(m_Ptr);
        }

        public bool IsNested()
        {
            return GetDeclaringType(m_Ptr) != IntPtr.Zero;
        }

        public bool IsGenericType()
        {
            return IsGenericType(m_Ptr);
        }

        public bool IsGenericTypeDefinition()
        {
            return IsGenericTypeDefinition(m_Ptr);
        }

        public bool IsGenericParameter()
        {
            return IsGenericParameter(m_Ptr);
        }

        public bool ContainsGenericParameters()
        {
            return ContainsGenericParameters(m_Ptr);
        }

        public bool HasDefaultConstructor()
        {
            return HasDefaultConstructor(m_Ptr);
        }

        // Unlike Type.IsAssignableTo, ignores variance, array covariance, Nullable<T> and generic constraints
        public bool IsSubtypeOf(TypeHandle other)
        {
            if (m_Ptr == other.m_Ptr)
                return true;
            return IsSubtypeOf(m_Ptr, other.m_Ptr);
        }

        public bool IsSubclassOf(TypeHandle other)
        {
            if (m_Ptr == other.m_Ptr)
                return false;
            if (IsInterface())
                return false;
            return IsSubclassOf(m_Ptr, other.m_Ptr);
        }

        public bool HasInterface(TypeHandle interfaceType)
        {
            return HasInterface(m_Ptr, interfaceType.m_Ptr);
        }

        // Empty when there is no base class, as for System.Object
        public TypeHandle GetBaseType()
        {
            return new TypeHandle(GetParent(m_Ptr));
        }

        public TypeHandle GetDeclaringType()
        {
            return new TypeHandle(GetDeclaringType(m_Ptr));
        }

        public TypeHandle GetElementType()
        {
            return new TypeHandle(GetElementType(m_Ptr));
        }

        public TypeHandle GetGenericTypeDefinition()
        {
            return new TypeHandle(GetGenericTypeDefinition(m_Ptr));
        }

        public NativeArray<TypeHandle> GetGenericArguments(Allocator allocator = Allocator.Temp)
        {
            int count = GetGenericArguments(m_Ptr, IntPtr.Zero, 0);
            var result = new NativeArray<TypeHandle>(count, allocator);
            if (count > 0)
                GetGenericArguments(m_Ptr, (IntPtr)result.GetUnsafePtr(), count);

            return result;
        }

        public NativeArray<TypeHandle> GetInterfaces(Allocator allocator = Allocator.Temp)
        {
            int count = GetInterfaces(m_Ptr, IntPtr.Zero, 0);
            var result = new NativeArray<TypeHandle>(count, allocator);
            if (count > 0)
                GetInterfaces(m_Ptr, (IntPtr)result.GetUnsafePtr(), count);

            return result;
        }

        public NativeArray<FieldHandle> GetFields(Allocator allocator = Allocator.Temp)
        {
            int count = GetFields(m_Ptr, IntPtr.Zero, 0);
            var result = new NativeArray<FieldHandle>(count, allocator);
            if (count > 0)
                GetFields(m_Ptr, (IntPtr)result.GetUnsafePtr(), count);

            return result;
        }

        // Matches Type.GetFields(BindingFlags), which leaves out private fields of ancestors
        public NativeArray<FieldHandle> GetFields(BindingFlags flags, Allocator allocator = Allocator.Temp)
        {
            bool declaredOnly = (flags & BindingFlags.DeclaredOnly) != 0;
            bool flattenHierarchy = (flags & BindingFlags.FlattenHierarchy) != 0;

            int total = GetFieldsAndFlags(m_Ptr, declaredOnly, IntPtr.Zero, IntPtr.Zero, 0, out _);
            if (total == 0)
                return new NativeArray<FieldHandle>(0, allocator);

            var handles = new NativeArray<FieldHandle>(total, Allocator.Temp);
            var fieldFlags = new NativeArray<uint>(total, Allocator.Temp);
            GetFieldsAndFlags(m_Ptr, declaredOnly,
                (IntPtr)handles.GetUnsafePtr(), (IntPtr)fieldFlags.GetUnsafePtr(), total, out int declaredCount);

            var list = new NativeList<FieldHandle>(total, Allocator.Temp);
            for (int i = 0; i < total; ++i)
            {
                uint fieldFlag = fieldFlags[i];
                if (!MatchesBindingFlags(fieldFlag, flags))
                    continue;

                // Private fields from ancestors are never inherited, statics only with FlattenHierarchy.
                if (i >= declaredCount &&
                    (((fieldFlag & k_FieldAttributeStatic) != 0 && !flattenHierarchy) || (fieldFlag & k_FieldAttributeFieldAccessMask) == k_FieldAttributePrivate))
                    continue;

                list.Add(handles[i]);
            }

            var result = new NativeArray<FieldHandle>(list.AsArray(), allocator);
            list.Dispose();
            fieldFlags.Dispose();
            handles.Dispose();
            return result;
        }

        static bool MatchesBindingFlags(uint fieldFlags, BindingFlags flags)
        {
            bool isStatic = (fieldFlags & k_FieldAttributeStatic) != 0;
            if (isStatic && (flags & BindingFlags.Static) == 0)
                return false;
            if (!isStatic && (flags & BindingFlags.Instance) == 0)
                return false;

            bool isPublic = (fieldFlags & k_FieldAttributeFieldAccessMask) == k_FieldAttributePublic;
            if (isPublic && (flags & BindingFlags.Public) == 0)
                return false;
            if (!isPublic && (flags & BindingFlags.NonPublic) == 0)
                return false;

            return true;
        }

        public bool IsDefined(TypeHandle attributeType)
        {
            // IL2CPP matches every attribute for a null attribute class and CoreCLR aborts on one
            if (attributeType == Empty)
                return false;

            return HasAttribute(m_Ptr, attributeType.m_Ptr);
        }

        // The language defaults, true and false, when AttributeUsage declares neither
        public void GetAttributeUsage(out bool inherited, out bool allowMultiple)
        {
            if (m_Ptr == IntPtr.Zero)
            {
                inherited = true;
                allowMultiple = false;
                return;
            }

            GetAttributeUsage(m_Ptr, out inherited, out allowMultiple);
        }

        public AttributeHandleCollection GetCustomAttributes(TypeHandle attributeType, Allocator allocator = Allocator.Temp)
        {
            ScriptingReflectionAssert.IsTrue(AttributeHandleCollection.IsSupported(allocator));
            if (attributeType == Empty)
                return AttributeHandleCollection.Empty;

            var handles = stackalloc IntPtr[AttributeHandleCollection.k_StackCapacity];
            int count = GetCustomAttributes(m_Ptr, attributeType.m_Ptr, allocator,
                (IntPtr)handles, AttributeHandleCollection.k_StackCapacity);

            if (count <= AttributeHandleCollection.k_StackCapacity)
                return AttributeHandleCollection.FromBuffer(handles, count, allocator);

            var collection = AttributeHandleCollection.Allocate(count, allocator);
            GetCustomAttributes(m_Ptr, attributeType.m_Ptr, allocator, collection.UnsafeBuffer, count);
            return collection;
        }

        public AttributeHandleCollection GetCustomAttributes(TypeHandle attributeType, bool inherit, Allocator allocator = Allocator.Temp)
        {
            ScriptingReflectionAssert.IsTrue(AttributeHandleCollection.IsSupported(allocator));
            if (!inherit || attributeType == Empty)
                return GetCustomAttributes(attributeType, allocator);

            var results = new NativeList<AttributeHandle>(4, Allocator.Temp);
            var blockHeads = new NativeList<AttributeHandle>(2, Allocator.Temp);

            for (var current = this; current != Empty; current = current.GetBaseType())
            {
                int start = results.Length;
                int count = current.AppendAttributes(attributeType, allocator, ref results);
                if (count == 0)
                    continue;

                var head = results[start];
                if (current != this)
                {
                    int kept = start;
                    for (int i = start; i < start + count; i++)
                    {
                        if (IsInheritedPast(results[i], results, start))
                            results[kept++] = results[i];
                    }

                    results.ResizeUninitialized(kept);
                    if (kept == start)
                    {
                        AttributeHandleCollection.FreeBlock(head, allocator);
                        continue;
                    }
                }

                blockHeads.Add(head);
            }

            var collection = AttributeHandleCollection.FromBlocks(results, blockHeads, allocator);
            results.Dispose();
            blockHeads.Dispose();
            return collection;
        }

        // As System.Reflection decides, from the matched attribute's own usage rather than the queried type's
        static bool IsInheritedPast(AttributeHandle attribute, NativeList<AttributeHandle> derived, int derivedCount)
        {
            var type = attribute.GetAttributeType();
            type.GetAttributeUsage(out bool inherited, out bool allowMultiple);

            if (!inherited)
                return false;
            if (allowMultiple)
                return true;

            for (int i = 0; i < derivedCount; i++)
            {
                if (derived[i].GetAttributeType() == type)
                    return false;
            }

            return true;
        }

        int AppendAttributes(TypeHandle attributeType, Allocator allocator, ref NativeList<AttributeHandle> results)
        {
            var handles = stackalloc IntPtr[AttributeHandleCollection.k_StackCapacity];
            int count = GetCustomAttributes(m_Ptr, attributeType.m_Ptr, allocator,
                (IntPtr)handles, AttributeHandleCollection.k_StackCapacity);

            if (count > AttributeHandleCollection.k_StackCapacity)
            {
                var resized = new NativeArray<IntPtr>(count, Allocator.Temp);
                GetCustomAttributes(m_Ptr, attributeType.m_Ptr, allocator, (IntPtr)resized.GetUnsafePtr(), count);

                for (int i = 0; i < count; i++)
                    results.Add(new AttributeHandle(resized[i]));

                resized.Dispose();
                return count;
            }

            for (int i = 0; i < count; i++)
                results.Add(new AttributeHandle(handles[i]));

            return count;
        }
    }
}
