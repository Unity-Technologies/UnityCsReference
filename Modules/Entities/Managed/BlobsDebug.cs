// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.DebugProxies
{
    readonly unsafe struct BlobAssetReferenceProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object m_Root;
        public object Value => m_Root;

        public BlobAssetReferenceProxy(BlobAssetReference<T> bar)
        {
            if (!bar.IsCreated)
                m_Root = null;
            else if (typeof(T).IsPrimitive)
                m_Root = bar.Value;
            else
                m_Root = new BlobStruct<T>(new IntPtr(bar.GetUnsafePtr()));
        }
    }

    [DebuggerDisplay("{Value,nq}", Name = "{Key,nq}", Type = "{TypeName,nq}")]
    struct BlobMember
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string Key;
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string TypeName => Value.GetType().Name;
    }

    struct BlobStructProxy<T> where T : struct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private BlobStruct<T> m_Struct;
        public BlobStructProxy(BlobStruct<T> struc)
        {
            m_Struct = struc;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public BlobMember[] Entries => m_Struct.Members;
    }

    struct BlobPtrProxy<T> where T : struct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private BlobPtrDebug<T> m_Ptr;
        public BlobPtrProxy(BlobPtrDebug<T> ptr)
        {
            m_Ptr = ptr;
        }

        public object Value => m_Ptr.Value;
    }

    [DebuggerDisplay("{Description,nq}")]
    unsafe struct BlobArrayDebug<T> where T : struct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly void* m_BasePtr;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object[] m_Entries;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string Description => "BlobArray<" + typeof(T).Name + "> (" + Length + ')';
        public override string ToString() => Description;
        public int Length => UnsafeUtility.AsRef<BlobArray<T>>(m_BasePtr).Length;

        public BlobArrayDebug(IntPtr basePtr)
        {
            m_BasePtr = basePtr.ToPointer();
            m_Entries = null;
        }

        void Init()
        {
            if (m_Entries != null)
                return;
            ref var blobArr = ref UnsafeUtility.AsRef<BlobArray<T>>(m_BasePtr);
            int offset = blobArr.m_OffsetPtr;
            byte* arrBasePtr = (byte*) m_BasePtr + offset;
            // we might know the concrete type, but if T might need to be wrapped, so object[] it is
            var arr = new object[blobArr.Length];
            int length = blobArr.Length;
            int size = UnsafeUtility.SizeOf<T>();
            for (int i = 0; i < length; i++)
            {
                var innerBasePtr = arrBasePtr + size * i;
                arr[i] = BlobProxy.UnpackValue(innerBasePtr, typeof(T));
            }

            m_Entries = arr;
        }

        // Note, we have to expose this here directly instead of through a proxy. Putting this into a debugger proxy and
        // hiding the root will make VS2017 sort all of the entries and show them in the order 0, 10, 11, 12, 1, 2 etc.
        public object[] Entries
        {
            get
            {
                Init();
                return m_Entries;
            }
        }
    }

    [DebuggerTypeProxy(typeof(BlobStructProxy<>))]
    [DebuggerDisplay("{Description,nq}")]
    unsafe struct BlobStruct<T> where T : struct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly void* m_BasePtr;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private BlobMember[] m_Members;

        public string Description => UnsafeUtility.AsRef<T>(m_BasePtr).ToString();
        public override string ToString() => Description;

        public BlobStruct(IntPtr basePtr)
        {
            m_BasePtr = basePtr.ToPointer();
            m_Members = null;
        }

        void Init()
        {
            if (m_Members != null)
                return;
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            m_Members = new BlobMember[fields.Length];
            for (int i = 0; i < m_Members.Length; i++)
            {
                var offset = (byte*) m_BasePtr + UnsafeUtility.GetFieldOffset(fields[i]);
                m_Members[i] = new BlobMember
                {
                    Key = fields[i].Name,
                    Value = BlobProxy.UnpackValue(offset, fields[i].FieldType)
                };
            }
        }

        public BlobMember[] Members
        {
            get
            {
                Init();
                return m_Members;
            }
        }
    }

    [DebuggerTypeProxy(typeof(BlobPtrProxy<>))]
    [DebuggerDisplay("{Description,nq}")]
    unsafe struct BlobPtrDebug<T> where T : struct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly void* m_BasePtr;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object m_Value;

        public string Description => "BlobPtr<" + typeof(T).Name + '>';
        public override string ToString() => Description;

        public BlobPtrDebug(IntPtr basePtr)
        {
            m_BasePtr = basePtr.ToPointer();
            m_Value = null;
        }

        void Init()
        {
            if (m_Value != null)
                return;
            var referencedOffset = (byte*) m_BasePtr + UnsafeUtility.AsRef<BlobPtr<T>>(m_BasePtr).m_OffsetPtr;
            m_Value = BlobProxy.UnpackValue(referencedOffset, typeof(T));
        }

        public object Value
        {
            get
            {
                Init();
                return m_Value;
            }
        }
    }

    static class BlobProxy
    {
        static unsafe object GetPrimitive<T>(IntPtr basePtr) where T : unmanaged
        {
            return *(T*)basePtr.ToPointer();
        }

        static unsafe string UnpackString(void* basePtr)
        {
            // can't get the BlobString itself because that will trigger the Blob asset safety system
            ref var blobString = ref UnsafeUtility.AsRef<BlobString>(basePtr);
            return blobString.ToString();
        }

        internal static unsafe object UnpackValue(void* basePtr, Type type)
        {
            if (type == typeof(BlobString))
                return UnpackString(basePtr);
            if (type.IsGenericType)
            {
                var typeDef = type.GetGenericTypeDefinition();
                if (typeDef == typeof(BlobArray<>))
                {
                    var arrType = typeof(BlobArrayDebug<>).MakeGenericType(type.GetGenericArguments());
                    return Activator.CreateInstance(arrType, new IntPtr(basePtr));
                }
                if (typeDef == typeof(BlobPtr<>))
                {
                    var ptrType = typeof(BlobPtrDebug<>).MakeGenericType(type.GetGenericArguments());
                    return Activator.CreateInstance(ptrType, new IntPtr(basePtr));
                }
            }

            if (type.IsValueType && !type.IsPrimitive)
            {
                var structType = typeof(BlobStruct<>).MakeGenericType(type);
                return Activator.CreateInstance(structType, new IntPtr(basePtr));
            }

            var makePrimitive = typeof(BlobProxy).GetMethod("GetPrimitive", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return makePrimitive.Invoke(null, new object[] { new IntPtr(basePtr) });
        }
    }
}
