// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Scripting.Reflection
{
    internal unsafe partial struct AttributeHandle : IEquatable<AttributeHandle>
    {
        IntPtr m_ArgInfo;

        // Each argument occupies one eight byte slot on every backend
        const int k_ArgSize = 8;

        internal AttributeHandle(IntPtr argInfo)
        {
            m_ArgInfo = argInfo;
        }

        internal IntPtr Value => m_ArgInfo;

        public static AttributeHandle Empty => default;

        public bool Equals(AttributeHandle other) => m_ArgInfo == other.m_ArgInfo;
        public override bool Equals(object obj) => obj is AttributeHandle other && Equals(other);
        public override int GetHashCode() => m_ArgInfo.GetHashCode();
        public static bool operator ==(AttributeHandle a, AttributeHandle b) => a.m_ArgInfo == b.m_ArgInfo;
        public static bool operator !=(AttributeHandle a, AttributeHandle b) => a.m_ArgInfo != b.m_ArgInfo;

        public IntPtr GetArg(int index)
        {
            if (m_ArgInfo == IntPtr.Zero || index < 0)
                return IntPtr.Zero;

            return GetAttributeArgValue(m_ArgInfo, index);
        }

        public T GetArg<T>(int index) where T : unmanaged
        {
            ScriptingReflectionAssert.IsTrue(sizeof(T) <= k_ArgSize);
            if (sizeof(T) > k_ArgSize)
                return default;

            IntPtr value = GetArg(index);
            if (value == IntPtr.Zero)
                return default;

            return *(T*)value;
        }

        // The attribute's own type, which may derive from the type the query asked for
        public TypeHandle GetAttributeType()
        {
            if (m_ArgInfo == IntPtr.Zero)
                return TypeHandle.Empty;

            return new TypeHandle(GetAttributeClass(m_ArgInfo));
        }

        // Only for a Type argument; the attribute's signature decides between this and GetArg<T>
        public TypeHandle GetTypeArg(int index)
        {
            if (m_ArgInfo == IntPtr.Zero || index < 0)
                return TypeHandle.Empty;

            return new TypeHandle(GetAttributeArgClass(m_ArgInfo, index));
        }
    }
}
