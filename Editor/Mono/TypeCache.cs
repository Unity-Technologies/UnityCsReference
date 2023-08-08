// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace UnityEditor
{
    public static partial class TypeCache
    {
        public static TypeCollection GetTypesWithAttribute<T>()
            where T : Attribute
        {
            return GetTypesWithAttribute(typeof(T));
        }

        public static MethodCollection GetMethodsWithAttribute<T>()
            where T : Attribute
        {
            return GetMethodsWithAttribute(typeof(T));
        }

        public static FieldInfoCollection GetFieldsWithAttribute<T>()
            where T : Attribute
        {
            return GetFieldsWithAttribute(typeof(T));
        }

        public static TypeCollection GetTypesDerivedFrom<T>()
        {
            var parentType = typeof(T);
            return GetTypesDerivedFrom(parentType);
        }

        public static TypeCollection GetTypesDerivedFrom(Type parentType)
        {
            return parentType.IsInterface ?
                new TypeCollection(Internal_GetTypesDerivedFromInterface(parentType)) :
                new TypeCollection(Internal_GetTypesDerivedFromType(parentType));
        }

        public static TypeCollection GetTypesWithAttribute(Type attrType)
        {
            return new TypeCollection(Internal_GetTypesWithAttribute(attrType));
        }

        public static MethodCollection GetMethodsWithAttribute(Type attrType)
        {
            return new MethodCollection(Internal_GetMethodsWithAttribute(attrType));
        }

        public static FieldInfoCollection GetFieldsWithAttribute(Type attrType)
        {
            return new FieldInfoCollection(Internal_GetFieldsWithAttribute(attrType));
        }

        public static TypeCollection GetTypesWithAttribute<T>(string assemblyName)
            where T : Attribute
        {
            return GetTypesWithAttribute(typeof(T), assemblyName);
        }

        public static MethodCollection GetMethodsWithAttribute<T>(string assemblyName)
            where T : Attribute
        {
            return GetMethodsWithAttribute(typeof(T), assemblyName);
        }

        public static FieldInfoCollection GetFieldsWithAttribute<T>(string assemblyName)
            where T : Attribute
        {
            return GetFieldsWithAttribute(typeof(T), assemblyName);
        }

        public static TypeCollection GetTypesDerivedFrom<T>(string assemblyName)
        {
            var parentType = typeof(T);
            return GetTypesDerivedFrom(parentType, assemblyName);
        }

        public static TypeCollection GetTypesDerivedFrom(Type parentType, string assemblyName)
        {
            return parentType.IsInterface ?
                new TypeCollection(Internal_GetTypesDerivedFromInterfaceFromAssembly(parentType, assemblyName)) :
                new TypeCollection(Internal_GetTypesDerivedFromTypeFromAssembly(parentType, assemblyName));
        }

        public static TypeCollection GetTypesWithAttribute(Type attrType, string assemblyName)
        {
            return new TypeCollection(Internal_GetTypesWithAttributeFromAssembly(attrType, assemblyName));

        }

        public static MethodCollection GetMethodsWithAttribute(Type attrType, string assemblyName)
        {
            return new MethodCollection(Internal_GetMethodsWithAttributeFromAssembly(attrType, assemblyName));
        }

        public static FieldInfoCollection GetFieldsWithAttribute(Type attrType, string assemblyName)
        {
            return new FieldInfoCollection(Internal_GetFieldsWithAttributeFromAssembly(attrType, assemblyName));
        }

        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct TypeCollection : IList<Type>, IList
        {
            [NonSerialized]
            readonly Type[] listOfTypes;

            internal TypeCollection(Type[] types) { listOfTypes = types; }

            public int Count => listOfTypes.Length;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public Type this[int index]
            {
                get
                {
                    return listOfTypes[index];
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
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                for (int i = 0; i < Count; ++i)
                    array[i + arrayIndex] = listOfTypes[i];
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as Type[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                for (int i = 0; i < Count; ++i)
                    typedArray[i + arrayIndex] = listOfTypes[i];
            }

            public int IndexOf(Type item)
            {
                return Array.IndexOf<Type>(listOfTypes, item);
            }

            public int IndexOf(object item)
            {
                return Array.IndexOf(listOfTypes, item);
            }

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
                        var values = new Type[m_Collection.Count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct MethodCollection : IList<MethodInfo>, IList
        {
            [NonSerialized]
            readonly MethodInfo[] listOfMethods;

            internal MethodCollection(MethodInfo[] methods) { listOfMethods = methods; }

            public int Count => listOfMethods.Length;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public MethodInfo this[int index]
            {
                get
                {
                    return listOfMethods[index];
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
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                for (int i = 0; i < Count; ++i)
                    array[i + arrayIndex] = listOfMethods[i];
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as MethodInfo[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                for (int i = 0; i < Count; ++i)
                    typedArray[i + arrayIndex] = listOfMethods[i];
            }

            public int IndexOf(MethodInfo item)
            {
                return Array.IndexOf<MethodInfo>(listOfMethods, item);
            }

            public int IndexOf(object item)
            {
                return Array.IndexOf(listOfMethods, item);
            }

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
                        var values = new MethodInfo[m_Collection.Count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public struct FieldInfoCollection : IList<FieldInfo>, IList
        {
            [NonSerialized]
            readonly FieldInfo[] listOfFields;

            internal FieldInfoCollection(FieldInfo[] fields) { listOfFields = fields; }

            public int Count => listOfFields.Length;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public bool IsSynchronized => true;

            object ICollection.SyncRoot => null;

            public FieldInfo this[int index]
            {
                get
                {
                    return listOfFields[index];
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
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException("arrayIndex");

                for (int i = 0; i < Count; ++i)
                    array[i + arrayIndex] = listOfFields[i];
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(nameof(array));
                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                if (arrayIndex + Count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                var typedArray = array as FieldInfo[];
                if (typedArray == null)
                    throw new ArrayTypeMismatchException(nameof(array));

                for (int i = 0; i < Count; ++i)
                    typedArray[i + arrayIndex] = listOfFields[i];
            }

            public int IndexOf(FieldInfo item)
            {
                return Array.IndexOf<FieldInfo>(listOfFields, item);
            }

            public int IndexOf(object item)
            {
                return Array.IndexOf(listOfFields, item);
            }

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
                        var values = new FieldInfo[m_Collection.Count];
                        m_Collection.CopyTo(values, 0);
                        return values;
                    }
                }
            }
        }
    }
}
