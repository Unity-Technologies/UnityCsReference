// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Mono/TypeCache.h")]
    public static partial class TypeCache
    {
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct TypeCollection : IList<Type>, IList
        {
            [NonSerialized]
            readonly IntPtr ptr;
            readonly int count;

            internal TypeCollection(IntPtr p, int s) { ptr = p; count = s; }

            public int Count => count;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public Type this[int index]
            {
                get
                {
                    if (index >= 0 && index < count)
                        return GetValue(ptr, index);
                    throw new IndexOutOfRangeException($"Index {index} is out of range of '{count}' Count.");
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            public bool Contains(Type item) => IndexOf(item) != -1;

            public bool Contains(object item) => IndexOf(item) != -1;

            public Enumerator GetEnumerator() => new Enumerator(ref this);

            public void CopyTo(Type[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                Internal_CopyTo(array, arrayIndex);
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as Type[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                Internal_CopyTo(typedArray, arrayIndex);
            }

            public int IndexOf(Type item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                for (int i = 0; i < count; ++i)
                {
                    // We can use == here as type object is unique and stored in a hashtable for the domain lifetime (Mono).
                    if (item == GetValue(ptr, i))
                        return i;
                }

                return -1;
            }

            public int IndexOf(object item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                var typedItem = item as Type;
                if (typedItem == null)
                    throw new ArgumentException(nameof(item) + " is not of type " + nameof(Type));

                return IndexOf(typedItem);
            }

            [ThreadSafe]
            static extern Type GetValue(IntPtr key, int index);

            [ThreadSafe]
            extern void Internal_CopyTo(Type[] array, int arrayIndex);

            IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<Type>.Add(Type item)
            {
                ThrowNotSupported();
            }

            void ICollection<Type>.Clear()
            {
                ThrowNotSupported();
            }

            bool ICollection<Type>.Remove(Type item)
            {
                ThrowNotSupported();
                return false;
            }

            void IList<Type>.Insert(int index, Type item)
            {
                ThrowNotSupported();
            }

            void IList<Type>.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            int IList.Add(object value)
            {
                ThrowNotSupported();
                return -1;
            }

            void IList.Clear()
            {
                ThrowNotSupported();
            }

            void IList.Insert(int index, object value)
            {
                ThrowNotSupported();
            }

            void IList.Remove(object value)
            {
                ThrowNotSupported();
            }

            void IList.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            static void ThrowNotSupported()
            {
                throw new NotSupportedException(nameof(TypeCollection) + " is read-only. Modification is not supported.");
            }

            public struct Enumerator : IEnumerator<Type>
            {
                readonly TypeCollection m_Collection;
                int m_Index;

                internal Enumerator(ref TypeCollection collection)
                {
                    m_Collection = collection;
                    m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_Collection.Count;
                }

                public Type Current => m_Collection[m_Index];

                void IEnumerator.Reset() => m_Index = -1;
                object IEnumerator.Current => Current;
            }

            class DebugView
            {
                readonly TypeCollection m_Collection;

                public DebugView(ref TypeCollection collection)
                {
                    m_Collection = collection;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public Type[] values
                {
                    get
                    {
                        var values = new Type[m_Collection.count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct MethodCollection : IList<MethodInfo>, IList
        {
            [NonSerialized]
            readonly IntPtr ptr;
            readonly int count;

            internal MethodCollection(IntPtr p, int s) { ptr = p; count = s; }

            public int Count => count;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public MethodInfo this[int index]
            {
                get
                {
                    if (index >= 0 && index < count)
                        return GetValue(ptr, index);
                    throw new IndexOutOfRangeException($"Index {index} is out of range of '{count}' Count.");
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            public bool Contains(MethodInfo item) => IndexOf(item) != -1;

            public bool Contains(object item) => IndexOf(item) != -1;

            public Enumerator GetEnumerator() => new Enumerator(ref this);

            public void CopyTo(MethodInfo[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                Internal_CopyTo(array, arrayIndex);
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as MethodInfo[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                Internal_CopyTo(typedArray, arrayIndex);
            }

            public int IndexOf(MethodInfo item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                for (int i = 0; i < count; ++i)
                {
                    if (item.Equals(GetValue(ptr, i)))
                        return i;
                }

                return -1;
            }

            public int IndexOf(object item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                var typedItem = item as MethodInfo;
                if (typedItem == null)
                    throw new ArgumentException(nameof(item) + " is not of type " + nameof(MethodInfo));

                return IndexOf(typedItem);
            }

            [ThreadSafe]
            static extern MethodInfo GetValue(IntPtr key, int index);

            [ThreadSafe]
            extern void Internal_CopyTo(MethodInfo[] array, int arrayIndex);

            IEnumerator<MethodInfo> IEnumerable<MethodInfo>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<MethodInfo>.Add(MethodInfo item)
            {
                ThrowNotSupported();
            }

            void ICollection<MethodInfo>.Clear()
            {
                ThrowNotSupported();
            }

            bool ICollection<MethodInfo>.Remove(MethodInfo item)
            {
                ThrowNotSupported();
                return false;
            }

            void IList<MethodInfo>.Insert(int index, MethodInfo item)
            {
                ThrowNotSupported();
            }

            void IList<MethodInfo>.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            int IList.Add(object value)
            {
                ThrowNotSupported();
                return -1;
            }

            void IList.Clear()
            {
                ThrowNotSupported();
            }

            void IList.Insert(int index, object value)
            {
                ThrowNotSupported();
            }

            void IList.Remove(object value)
            {
                ThrowNotSupported();
            }

            void IList.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            static void ThrowNotSupported()
            {
                throw new NotSupportedException(nameof(TypeCollection) + " is read-only. Modification is not supported.");
            }

            public struct Enumerator : IEnumerator<MethodInfo>
            {
                readonly MethodCollection m_Collection;
                int m_Index;

                internal Enumerator(ref MethodCollection collection)
                {
                    m_Collection = collection;
                    m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_Collection.Count;
                }

                public MethodInfo Current => m_Collection[m_Index];

                void IEnumerator.Reset() => m_Index = -1;
                object IEnumerator.Current => Current;
            }

            class DebugView
            {
                readonly MethodCollection m_Collection;

                public DebugView(ref MethodCollection collection)
                {
                    m_Collection = collection;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public MethodInfo[] Values
                {
                    get
                    {
                        var values = new MethodInfo[m_Collection.count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct FieldInfoCollection : IList<FieldInfo>, IList
        {
            [NonSerialized]
            readonly IntPtr ptr;
            readonly int count;

            internal FieldInfoCollection(IntPtr p, int s) { ptr = p; count = s; }

            public int Count => count;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public FieldInfo this[int index]
            {
                get
                {
                    if (index >= 0 && index < count)
                        return GetValue(ptr, index);
                    throw new IndexOutOfRangeException($"Index {index} is out of range of '{count}' Count.");
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            public bool Contains(FieldInfo item) => IndexOf(item) != -1;

            public bool Contains(object item) => IndexOf(item) != -1;

            public Enumerator GetEnumerator() => new Enumerator(ref this);

            public void CopyTo(FieldInfo[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                Internal_CopyTo(array, arrayIndex);
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as FieldInfo[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                Internal_CopyTo(typedArray, arrayIndex);
            }

            public int IndexOf(FieldInfo item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                for (int i = 0; i < count; ++i)
                {
                    if (item.Equals(GetValue(ptr, i)))
                        return i;
                }

                return -1;
            }

            public int IndexOf(object item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                var typedItem = item as FieldInfo;
                if (typedItem == null)
                    throw new ArgumentException(nameof(item) + " is not of type " + nameof(FieldInfo));

                return IndexOf(typedItem);
            }

            [ThreadSafe]
            static extern FieldInfo GetValue(IntPtr key, int index);

            [ThreadSafe]
            extern void Internal_CopyTo(FieldInfo[] array, int arrayIndex);

            IEnumerator<FieldInfo> IEnumerable<FieldInfo>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<FieldInfo>.Add(FieldInfo item)
            {
                ThrowNotSupported();
            }

            void ICollection<FieldInfo>.Clear()
            {
                ThrowNotSupported();
            }

            bool ICollection<FieldInfo>.Remove(FieldInfo item)
            {
                ThrowNotSupported();
                return false;
            }

            void IList<FieldInfo>.Insert(int index, FieldInfo item)
            {
                ThrowNotSupported();
            }

            void IList<FieldInfo>.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    ThrowNotSupported();
                }
            }

            int IList.Add(object value)
            {
                ThrowNotSupported();
                return -1;
            }

            void IList.Clear()
            {
                ThrowNotSupported();
            }

            void IList.Insert(int index, object value)
            {
                ThrowNotSupported();
            }

            void IList.Remove(object value)
            {
                ThrowNotSupported();
            }

            void IList.RemoveAt(int index)
            {
                ThrowNotSupported();
            }

            static void ThrowNotSupported()
            {
                throw new NotSupportedException(nameof(TypeCollection) + " is read-only. Modification is not supported.");
            }

            public struct Enumerator : IEnumerator<FieldInfo>
            {
                readonly FieldInfoCollection m_Collection;
                int m_Index;

                internal Enumerator(ref FieldInfoCollection collection)
                {
                    m_Collection = collection;
                    m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_Collection.Count;
                }

                public FieldInfo Current => m_Collection[m_Index];

                void IEnumerator.Reset() => m_Index = -1;
                object IEnumerator.Current => Current;
            }

            class DebugView
            {
                readonly FieldInfoCollection m_Collection;

                public DebugView(ref FieldInfoCollection collection)
                {
                    m_Collection = collection;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public FieldInfo[] Values
                {
                    get
                    {
                        var values = new FieldInfo[m_Collection.count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }

        [ThreadSafe]
        public static extern TypeCollection GetTypesWithAttribute(Type attrType);

        [ThreadSafe]
        public static extern MethodCollection GetMethodsWithAttribute(Type attrType);

        [ThreadSafe]
        public static extern FieldInfoCollection GetFieldsWithAttribute(Type attrType);

        [ThreadSafe]
        static extern TypeCollection GetTypesDerivedFromInterface(Type interfaceType);

        [ThreadSafe]
        static extern TypeCollection GetTypesDerivedFromType(Type parentType);
    }
}
