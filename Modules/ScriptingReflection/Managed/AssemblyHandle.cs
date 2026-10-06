// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Scripting.Reflection
{
    internal unsafe partial struct AssemblyHandle : IEquatable<AssemblyHandle>
    {
        IntPtr m_Ptr;

        AssemblyHandle(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        public static AssemblyHandle Empty => default;

        public bool Equals(AssemblyHandle other) => m_Ptr == other.m_Ptr;
        public override bool Equals(object obj) => obj is AssemblyHandle other && Equals(other);
        public override int GetHashCode() => m_Ptr.GetHashCode();
        public static bool operator ==(AssemblyHandle a, AssemblyHandle b) => a.m_Ptr == b.m_Ptr;
        public static bool operator !=(AssemblyHandle a, AssemblyHandle b) => a.m_Ptr != b.m_Ptr;

        public static AssemblyHandle FromAssembly(Assembly assembly)
        {
            return new AssemblyHandle(AssemblyHandleFromAssembly(assembly));
        }

        public static NativeArray<AssemblyHandle> GetAll(Allocator allocator = Allocator.Temp)
        {
            int count = GetAllAssemblies(IntPtr.Zero, 0);
            var result = new NativeArray<AssemblyHandle>(count, allocator);
            if (count > 0)
                GetAllAssemblies((IntPtr)result.GetUnsafePtr(), count);

            return result;
        }

        public bool References(AssemblyHandle reference)
        {
            return ReferencesAssembly(m_Ptr, reference.m_Ptr);
        }

        public bool IsDefined(TypeHandle attributeType)
        {
            if (attributeType == TypeHandle.Empty)
                return false;

            return AssemblyHasAttribute(m_Ptr, attributeType.Value);
        }

        public AttributeHandleCollection GetCustomAttributes(TypeHandle attributeType, Allocator allocator = Allocator.Temp)
        {
            ScriptingReflectionAssert.IsTrue(AttributeHandleCollection.IsSupported(allocator));
            if (attributeType == TypeHandle.Empty)
                return AttributeHandleCollection.Empty;

            var handles = stackalloc IntPtr[AttributeHandleCollection.k_StackCapacity];
            int count = GetAssemblyCustomAttributes(m_Ptr, attributeType.Value, allocator,
                (IntPtr)handles, AttributeHandleCollection.k_StackCapacity);

            if (count <= AttributeHandleCollection.k_StackCapacity)
                return AttributeHandleCollection.FromBuffer(handles, count, allocator);

            var collection = AttributeHandleCollection.Allocate(count, allocator);
            GetAssemblyCustomAttributes(m_Ptr, attributeType.Value, allocator, collection.UnsafeBuffer, count);
            return collection;
        }

        // skipTypeAndNested and its nested types come back as Empty rather than being resolved
        public NativeArray<TypeHandle> GetTypes(Allocator allocator = Allocator.Temp, TypeHandle skipTypeAndNested = default)
        {
            int count = GetAssemblyTypes(m_Ptr, skipTypeAndNested.Value, IntPtr.Zero, 0);
            var result = new NativeArray<TypeHandle>(count, allocator);
            if (count > 0)
                GetAssemblyTypes(m_Ptr, skipTypeAndNested.Value, (IntPtr)result.GetUnsafePtr(), count);

            return result;
        }
    }
}
