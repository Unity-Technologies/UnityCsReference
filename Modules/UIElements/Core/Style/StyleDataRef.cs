// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal unsafe struct StyleDataRef<T> : IEquatable<StyleDataRef<T>> where T : unmanaged, IStyleDataGroup<T>
    {
        static readonly StyleDataType k_Type = StyleDataAllocator.GetType<T>();
        private static readonly int k_SizeOf = UnsafeUtility.SizeOf<T>();
        private static readonly int k_IntDataCount = k_SizeOf / sizeof(int);

        struct RefCounted
        {
            private int m_RefCount;
#pragma warning disable CS0649 // Incremented on the native side
            private uint m_Id;
#pragma warning restore CS0649

            public T value;
            public int refCount => m_RefCount;
            public uint id => m_Id;

            public static RefCounted* Create(in T value)
            {
                var result = (RefCounted*)StyleDataAllocator.Allocate(k_Type);
                result->value = value;
                return result;
            }

            public static void Dispose(RefCounted* self)
            {
                self->value.Dispose();
                StyleDataAllocator.Free((IntPtr)self, k_Type);
            }

            public void Acquire() => ++ m_RefCount;

            public void Release()
            {
                --m_RefCount;
            }

            public static RefCounted* Copy(RefCounted* self)
            {
                return Create(self->value.Copy());
            }

            public static bool ValueEquals(RefCounted* self, RefCounted* other)
            {
                return UnsafeUtility.MemCmp(&self->value, &other->value, k_SizeOf) == 0;
            }

            public static int GetValueHashCode(RefCounted* self) => StyleHashUtility.Hash32(&self->value, k_IntDataCount);
        }

        private RefCounted* m_Ref;

        public int refCount => m_Ref != null ? m_Ref->refCount : 0;
        public uint id => m_Ref != null ? m_Ref->id : 0;

        public bool IsAlive() => m_Ref != null;

        public StyleDataRef<T> Acquire()
        {
            m_Ref->Acquire();
            return this;
        }

        public void Release()
        {
            m_Ref->Release();

            if (m_Ref->refCount == 0)
            {
                RefCounted.Dispose(m_Ref);
            }

            m_Ref = null;
        }

        public void SafeRelease()
        {
            if (IsAlive())
                Release();
        }

        public void CopyFrom(StyleDataRef<T> other)
        {
            if (m_Ref->refCount == 1)
            {
                m_Ref->value.CopyFrom(ref other.m_Ref->value);
            }
            else
            {
                m_Ref->Release();
                m_Ref = other.m_Ref;
                m_Ref->Acquire();
            }
        }

        public readonly T* GetValuePtr() => &m_Ref->value;

        public readonly ref readonly T Read() => ref m_Ref->value;

        public ref T Write()
        {
            if (m_Ref->refCount == 1)
                return ref m_Ref->value;

            var oldRef = m_Ref;
            m_Ref = RefCounted.Copy(m_Ref);
            oldRef->Release();

            return ref m_Ref->value;
        }

        private static readonly T k_Default = new T().GetDefault();
        public static StyleDataRef<T> Create()
        {
            return new StyleDataRef<T> { m_Ref = RefCounted.Create(k_Default) };
        }

        public override int GetHashCode()
        {
            return m_Ref != null ? RefCounted.GetValueHashCode(m_Ref) : 0;
        }

        public static bool operator==(StyleDataRef<T> lhs, StyleDataRef<T> rhs)
        {
            return lhs.m_Ref == rhs.m_Ref || RefCounted.ValueEquals(lhs.m_Ref, rhs.m_Ref);
        }

        public static bool operator!=(StyleDataRef<T> lhs, StyleDataRef<T> rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(StyleDataRef<T> other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return obj is StyleDataRef<T> other && Equals(other);
        }

        public bool ReferenceEquals(StyleDataRef<T> other)
        {
            return m_Ref == other.m_Ref;
        }
    }
}
