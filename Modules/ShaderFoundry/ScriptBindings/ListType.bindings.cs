// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    // The internal type is not actually used but is needed for how DataTypeStatic works.
    internal struct ListTypeInternal : IInternalType<ListTypeInternal>
    {
        internal FoundryHandle handle;

        ListTypeInternal IInternalType<ListTypeInternal>.ConstructInvalid() => new ListTypeInternal();
    }

    [FoundryAPI]
    // ListType is a special object to deal with polymorphic scenarios of lists.
    // This type is only used for retrieving data and is not valid for building lists.
    internal readonly struct ListType : IEquatable<ListType>, IPublicType<ListType>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ListType IPublicType<ListType>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ListType(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        internal IEnumerable<FoundryHandle> Handles
        {
            get
            {
                var size = container?.GetArraySize(handle) ?? 0;
                for (var i = 0u; i < size; ++i)
                    yield return container.GetArrayElement(handle, i);
            }
        }
        public IEnumerable<IPublicType> Values
        {
            get
            {
                foreach (var handle in Handles)
                    yield return container.ConstructTypeFromHandle(handle);
            }
        }
        public static IEnumerable<IPublicType> EnumeratePublicType(ShaderContainer container, FoundryHandle listHandle)
        {
            return new ListType(container, listHandle).Values;
        }
        public static IEnumerable<T> Enumerate<T>(ShaderContainer container, FoundryHandle listHandle) where T : struct, IPublicType<T>
        {
            var list = new ListType(container, listHandle);
            foreach (var handle in list.Handles)
                yield return PublicTypeStatic<T>.ConstructFromHandle(container, handle);
        }
        public static IEnumerable<string> EnumerateStrings(ShaderContainer container, FoundryHandle listHandle)
        {
            var list = new ListType(container, listHandle);
            foreach (var handle in list.Handles)
                yield return container.GetString(handle);
        }
        static internal FoundryHandle Build(ShaderContainer container, List<IPublicType> values)
        {
            if (values == null || container == null)
                return FoundryHandle.Invalid();

            var listHandle = container.CreateArray((uint)values.Count);
            for (var i = 0; i < values.Count; ++i)
                container.SetArrayElement(listHandle, (ulong)i, values[i].Handle);
            return listHandle;
        }
        static internal FoundryHandle Build<T>(ShaderContainer container, List<T> values) where T : struct, IPublicType<T>
        {
            if (values == null || container == null)
                return FoundryHandle.Invalid();

            var listHandle = container.CreateArray((uint)values.Count);
            for (var i = 0; i < values.Count; ++i)
                container.SetArrayElement(listHandle, (ulong)i, values[i].Handle);
            return listHandle;
        }
        static internal FoundryHandle Build(ShaderContainer container, List<string> values)
        {
            if (values == null || container == null)
                return FoundryHandle.Invalid();

            var listHandle = container.CreateArray((uint)values.Count);
            for (var i = 0; i < values.Count; ++i)
                container.SetArrayElement(listHandle, (ulong)i, container.AddString(values[i]));
            return listHandle;
        }
        internal static ListType Construct<T>(ShaderContainer container, List<T> values) where T : struct, IPublicType<T>
        {
            return new ListType(container, Build(container, values));
        }

        // private
        internal ListType(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
        }

        public static ListType Invalid => new ListType(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ListType other && this.Equals(other);
        public bool Equals(ListType other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ListType lhs, ListType rhs) => lhs.Equals(rhs);
        public static bool operator!=(ListType lhs, ListType rhs) => !lhs.Equals(rhs);
    }
}
