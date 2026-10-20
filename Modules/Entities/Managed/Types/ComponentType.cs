// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// A struct to define a component including how it's accessed and what type of component it is
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public partial struct ComponentType : IEquatable<ComponentType>, IComparable<ComponentType>
    {
        /// <summary>
        /// The access of the component type
        /// </summary>
        public enum AccessMode
        {
            /// <summary>
            /// Access to read and write to the component type
            /// </summary>
            ReadWrite,
            /// <summary>
            /// Access to only read the component type
            /// </summary>
            ReadOnly,
            /// <summary>
            /// Excludes the component type when used in a query
            /// </summary>
            Exclude
        }

        /// <summary>
        /// A unique index of the component type
        /// </summary>
        public TypeIndex TypeIndex;
        /// <summary>
        /// The way the component type will be accessed
        /// </summary>
        public AccessMode AccessModeType;
        /// <summary>
        /// True if the component type is a <see cref="IBufferElementData"/>
        /// </summary>
        public bool IsBuffer => TypeIndex.IsBuffer;
        /// <summary>
        /// Obsolete. Use <see cref="IsCleanupComponent"/> instead.
        /// </summary>
        /// <remarks> **Obsolete.** Use <see cref="IsCleanupComponent"/> instead.
        ///
        /// True if the component type is a <see cref="ICleanupComponentData"/></remarks>
        [Obsolete("IsSystemStateComponent has been renamed to IsCleanupComponent. IsSystemStateComponent will be removed in a future package release. (UnityUpgradable) -> IsCleanupComponent", false)]
        public bool IsSystemStateComponent => TypeIndex.IsCleanupComponent;
        /// <summary>
        /// True if the component type is a <see cref="ICleanupComponentData"/>
        /// </summary>
        public bool IsCleanupComponent => TypeIndex.IsCleanupComponent;
        /// <summary>
        /// Obsolete. Use <see cref="IsCleanupComponent"/> instead.
        /// </summary>
        /// <remarks> **Obsolete.** Use <see cref="IsCleanupComponent"/> instead.
        ///
        /// True if the component type is a <see cref="ICleanupSharedComponentData"/>.</remarks>
        [Obsolete("IsSystemStateSharedComponent has been renamed to IsCleanupSharedComponent. IsSystemStateSharedComponent will be removed in a future package release. (UnityUpgradable) -> IsCleanupSharedComponent", false)]
        public bool IsSystemStateSharedComponent => TypeIndex.IsCleanupSharedComponent;
        /// <summary>
        /// True if the component type is a <see cref="ICleanupSharedComponentData"/>
        /// </summary>
        public bool IsCleanupSharedComponent => TypeIndex.IsCleanupSharedComponent;
        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ICleanupBufferElementData"/>.
        /// </summary>
        public bool IsCleanupBufferComponent => TypeIndex.IsCleanupBufferComponent;
        /// <summary>
        /// True if the component type is a <see cref="IComponentData"/>
        /// </summary>
        public bool IsComponent => TypeIndex.IsComponentType;
        /// <summary>
        /// True if the component type is a <see cref="ISharedComponentData"/>
        /// </summary>
        public bool IsSharedComponent => TypeIndex.IsSharedComponentType;
        /// <summary>
        /// True if the component type is a managed component
        /// </summary>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public bool IsManagedComponent => IsManagedComponentInternal;
        internal bool IsManagedComponentInternal => TypeIndex.IsManagedComponentInternal;
        /// <summary>
        /// True if the component type does not contain actual fields or data
        /// </summary>
        public bool IsZeroSized => TypeIndex.IsZeroSized;
        /// <summary>
        /// True if the component type is flagged as a chunk component type
        /// </summary>
        public bool IsChunkComponent => TypeIndex.IsChunkComponent;
        /// <summary>
        /// True if the component type is a <see cref="IEnableableComponent"/>
        /// </summary>
        public bool IsEnableable => TypeIndex.IsEnableable;
        /// <summary>
        /// True if any of the fields in the component type are type <see cref="Entity"/>
        /// </summary>
        public bool HasEntityReferences => TypeIndex.HasEntityReferences;
        /// <summary>
        /// The component type contains a <seealso cref="NativeContainerAttribute"/> decorated member. NativeContainer members found in nested member types will also cause this property to return true.
        /// </summary>
        public bool HasNativeContainer => TypeIndex.HasNativeContainer;
        /// <summary>
        /// True if the component type is appropriate for chunk serialization. Such types are blittable without containing pointer types or have been decorated with <seealso cref="ChunkSerializableAttribute"/>.
        /// </summary>
        public bool IsChunkSerializable => TypeIndex.IsChunkSerializable;

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadWrite"/> based on the generic type T.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType ReadWrite<T>()
        {
            return FromTypeIndex(TypeManager.GetTypeIndex<T>());
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadWrite"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType ReadWrite(Type type)
        {
            return FromTypeIndex(TypeManager.GetTypeIndex(type));
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadWrite"/> based on the typeIndex.
        /// </summary>
        /// <param name="typeIndex">The index</param>
        /// <returns>The component type</returns>
        public static ComponentType ReadWrite(TypeIndex typeIndex)
        {
            return FromTypeIndex(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadWrite"/> based on the typeIndex
        /// </summary>
        /// <param name="typeIndex">The index</param>
        /// <returns>The component type</returns>
        public static ComponentType FromTypeIndex(TypeIndex typeIndex)
        {
            ComponentType type;
            type.TypeIndex = typeIndex;
            type.AccessModeType = AccessMode.ReadWrite;
            return type;
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadOnly"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType ReadOnly(Type type)
        {
            ComponentType t = FromTypeIndex(TypeManager.GetTypeIndex(type));
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadOnly"/> based on the typeIndex.
        /// </summary>
        /// <param name="typeIndex">The index</param>
        /// <returns>The component type</returns>
        public static ComponentType ReadOnly(TypeIndex typeIndex)
        {
            ComponentType t = FromTypeIndex(typeIndex);
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.ReadOnly"/> based on the generic type T.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType ReadOnly<T>()
        {
            ComponentType t = ReadWrite<T>();
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.ReadWrite"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The chunk component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType ChunkComponent(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return FromTypeIndex(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.ReadWrite"/> based on the type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The chunk component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType ChunkComponent<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return FromTypeIndex(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.ReadOnly"/> based on the type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The chunk component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType ChunkComponentReadOnly<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return ReadOnly(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.ReadOnly"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The chunk component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType ChunkComponentReadOnly(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return ReadOnly(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.Exclude"/> based on the type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The chunk component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType ChunkComponentExclude<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return Exclude(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> that is a chunk component with <see cref="AccessMode.Exclude"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The chunk component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType ChunkComponentExclude(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return Exclude(typeIndex);
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.Exclude"/> based on the type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The component type</returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static ComponentType Exclude(Type type)
        {
            return Exclude(TypeManager.GetTypeIndex(type));
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.Exclude"/> based on the typeIndex.
        /// </summary>
        /// <param name="typeIndex">The index</param>
        /// <returns>The component type</returns>
        public static ComponentType Exclude(TypeIndex typeIndex)
        {
            ComponentType t = FromTypeIndex(typeIndex);
            t.AccessModeType = AccessMode.Exclude;
            return t;
        }

        /// <summary>
        /// Returns a <see cref="ComponentType"/> with <see cref="AccessMode.Exclude"/> based on the generic type T.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(Entity) })]
        public static ComponentType Exclude<T>()
        {
            return Exclude(TypeManager.GetTypeIndex<T>());
        }

        /// <summary>
        /// Create a component type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="accessModeType">The <see cref="AccessMode"/> of the component type. <see cref="AccessMode.ReadWrite"/> by default.</param>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public ComponentType(Type type, AccessMode accessModeType = AccessMode.ReadWrite)
        {
            TypeIndex = TypeManager.GetTypeIndex(type);
            AccessModeType = accessModeType;
        }

        /// <summary>
        /// Gets the managed <see cref="Type"/> based on the component's <see cref="TypeIndex"/>.
        /// </summary>
        /// <returns>The managed type</returns>
        [ExcludeFromBurstCompatTesting("Returns a managed Type")]
        public Type GetManagedType()
        {
            return TypeManager.GetType(TypeIndex);
        }

        /// <summary>
        /// Creates a new component type based on the type passed in
        /// </summary>
        /// <param name="type">The managed type</param>
        /// <returns>The new <see cref="ComponentType"/> with <see cref="AccessMode.ReadWrite"/></returns>
        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        public static implicit operator ComponentType(Type type)
        {
            return new ComponentType(type, AccessMode.ReadWrite);
        }

        /// <summary>
        /// Evaluates if one component type is less than the the other first by <see cref="TypeIndex"/>, then by <see cref="AccessMode"/>.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is less than the right-hand side's. If the type indices match, the <see cref="AccessModeType"/> is used.</returns>
        public static bool operator<(ComponentType lhs, ComponentType rhs)
        {
            if (lhs.TypeIndex == rhs.TypeIndex)
                return lhs.AccessModeType < rhs.AccessModeType;

            return lhs.TypeIndex < rhs.TypeIndex;
        }

        /// <summary>
        /// Evaluates if one component type is greater than the the other.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is less than the right-hand side's. If the type indices match, the <see cref="AccessModeType"/> fields are compared instead.</returns>
        public static bool operator>(ComponentType lhs, ComponentType rhs) => rhs < lhs;

        /// <summary>
        /// Evaluates if two component types are equal based on <see cref="TypeIndex"/> and <see cref="AccessModeType"/>.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>Returns true if both their type indices are equal and their access modes are equal.</returns>
        public static bool operator==(ComponentType lhs, ComponentType rhs) => lhs.Equals(rhs);

        /// <summary>
        /// Evaluates if two component types are not equal based on <see cref="TypeIndex"/> and <see cref="AccessModeType"/>.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>Returns true if their type indices are not equal or their access modes are not equal.</returns>
        public static bool operator!=(ComponentType lhs, ComponentType rhs) => !lhs.Equals(rhs);

        internal static unsafe bool CompareArray(ComponentType* type1, int typeCount1, ComponentType* type2,
            int typeCount2)
        {
            if (typeCount1 != typeCount2)
                return false;
            for (var i = 0; i < typeCount1; ++i)
                if (type1[i] != type2[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Combine multiple array of component type into one. Duplicate types are removed.
        /// It will allocate an array containing all component type.
        /// Useful for creating queries during initialization of systems.
        /// </summary>
        /// <param name="componentTypes"></param>
        /// <returns></returns>
        [ExcludeFromBurstCompatTesting("Takes a managed array")]
        unsafe public static ComponentType[] Combine(params ComponentType[][] componentTypes)
        {
            int count = 0;
            for (int i = 0; i != componentTypes.Length; i++)
                count += componentTypes[i].Length;

            var output = new ComponentType[count];
            int o = 0;
            for (int i = 0; i != componentTypes.Length; i++)
            {
                for (int k = 0; k < componentTypes[i].Length; ++k)
                    output[o+k] = componentTypes[i][k];
                o += componentTypes[i].Length;
            }

            fixed (ComponentType* types = output)
            {
                NativeSortExtension.Sort(types, output.Length);
            }

            if (count != 0)
            {
                o = 0;
                for (int i = 1; i < count;i++ )
                {
                    if (output[o].TypeIndex != output[i].TypeIndex)
                    {
                        o++;
                        output[o] = output[i];
                    }
                    else if (output[i].AccessModeType == AccessMode.Exclude && output[o].AccessModeType != AccessMode.Exclude)
                    {
                        throw new ArgumentException($"ComponentType.Combine {output[o]} and {output[i]} are conflicting.");
                    }
                }
                o++;

                if (o != count)
                    Array.Resize(ref output, o);
            }


            return output;
        }

        /// <summary>
        /// Returns a managed string of the component type
        /// </summary>
        /// <returns>A string of the component type</returns>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public override string ToString()
        {
            return ToFixedString().ToString();
        }

        static readonly FixedString32Bytes kMsg_None = "None";
        static readonly FixedString32Bytes kMsg_Space_Buffer = " [Buffer]";
        static readonly FixedString32Bytes kMsg_Space_Exclude = " [Exclude]";
        static readonly FixedString32Bytes kMsg_Space_Readonly = " [ReadOnly]";

        /// <summary>
        /// Returns a fixed string of the component type
        /// </summary>
        /// <returns>A <see cref="FixedString128Bytes"/> of the component type</returns>
        [GenerateTestsForBurstCompatibility]
        public FixedString128Bytes ToFixedString()
        {
            if (TypeIndex == TypeIndex.Null)
                return kMsg_None;

            var fs = new FixedString128Bytes();
            fs.Append(TypeManager.GetTypeInfo(TypeIndex).DebugTypeName);

            if (IsBuffer)
                fs.Append(kMsg_Space_Buffer);
            if (AccessModeType == AccessMode.Exclude)
                fs.Append(kMsg_Space_Exclude);
            if (AccessModeType == AccessMode.ReadOnly)
                fs.Append(kMsg_Space_Readonly);

            return fs;
        }

        /// <summary>
        /// Checks if this component type has the same <see cref="TypeIndex"/> and <see cref="AccessModeType"/> as the other component type.
        /// </summary>
        /// <param name="other">The other component type to compare to</param>
        /// <returns>True if the <see cref="TypeIndex"/> and <see cref="AccessModeType"/>of both are equal</returns>
        public bool Equals(ComponentType other) => TypeIndex == other.TypeIndex && AccessModeType == other.AccessModeType;

        /// <summary>
        /// Returns the sort order this component type compared to another
        /// </summary>
        /// <param name="other">The other component type</param>
        /// <returns>The sort order</returns>
		[GenerateTestsForBurstCompatibility]
        public int CompareTo(ComponentType other)
        {
            if (TypeIndex != other.TypeIndex)
                return TypeIndex.CompareTo(other.TypeIndex);
            return ((int)AccessModeType).CompareTo((int)other.AccessModeType);
        }

        /// <summary>
        /// Checks to see if an object is equal to this component type.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is a <see cref="ComponentType"/> and the object equals this component type</returns>
        [ExcludeFromBurstCompatTesting("Takes managed object")]
        public override bool Equals(object obj) => obj is ComponentType type && type == this;

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return TypeIndex.GetHashCode() * 397 ^ (int)AccessModeType;
            }
        }
    }
}
