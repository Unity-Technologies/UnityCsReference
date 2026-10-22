// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using UnityEngine;
using System.Text;
using static Unity.Entities.TypeManager;
using Unity.Scripting.LifecycleManagement;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    /// <summary>
    /// Can exclude components which are unknown at the time of creating the query that have been declared
    /// to write to the same component.
    ///
    /// This allows for extending systems of components safely without editing the previously existing systems.
    ///
    /// The goal is to have a way for systems that expect to transform data from one set of components (inputs) to
    /// another (output[s]) be able to declare that explicit transform, and they exclusively know about one set of
    /// inputs. If there are other inputs that want to write to the same output, the query shouldn't match because it's
    /// a nonsensical/unhandled setup. It's both a way to guard against nonsensical components (having two systems write
    /// to the same output value), and a way to "turn off" existing systems/queries by putting a component with the same
    /// write lock on an entity, letting another system handle it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
    public class WriteGroupAttribute : Attribute
    {
        /// <summary>
        /// <see cref="WriteGroupAttribute"/>
        /// </summary>
        /// <param name="targetType">Output type the component decorated with this attribute (input) expects to be written by.</param>
        public WriteGroupAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Output type the component decorated with this attribute (input) expects to be written by.
        /// </summary>
        public Type TargetType;
    }

    /// <summary>
    /// Prevents a Component Type from being registered in the TypeManager during TypeManager.Initialize().
    /// Types that are not registered will not be recognized by EntityManager.
    /// </summary>
    public class DisableAutoTypeRegistrationAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute that indicates that the component should be removed at the end of each baking iteration.
    /// </summary>
    /// <remarks>
    /// Components decorated with the [TemporaryBakingType] attribute are stripped
    /// after each baking iteration, so you can use them in baking systems
    /// to identify entities that a baker has modified during the current baking iteration.
    ///
    /// Components with the [TemporaryBakingType] attribute are not exported in the runtime data.
    /// </remarks>
    /// <seealso cref="BakingTypeAttribute"/>
    public class TemporaryBakingTypeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute that indicates that a component should persist in the baking world, but shouldn't be exported with the runtime data.
    /// </summary>
    /// <remarks>
    /// During incremental baking, components with the [BakingType] attribute persist in the baking world.
    /// This allows baking systems to process entities which haven't been modified during the current baking iteration,
    /// but which are dependencies to the final result of the baking.
    ///
    /// Components with the [BakingType] attribute are not exported in the runtime data.
    /// </remarks>
    /// <seealso cref="TemporaryBakingTypeAttribute"/>
    public class BakingTypeAttribute : Attribute
    {
    }

    /// <summary>
    /// Provides a unique id for component types as well as quick lookup information about the component type itself.
    /// This value is fully deterministic at runtime but should not be considered deterministic across builds
    /// and thus should not be serialized. For serialization, please prefer <see cref="TypeManager.TypeInfo.StableTypeHash"/> instead.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct TypeIndex : IComparable<TypeIndex>, IEquatable<TypeIndex>
    {
        /// <summary>
        /// Raw value used to identify Component types at runtime.
        /// <remarks>
        /// This value should not be serialized as it is not guaranteed to be deterministic across builds (but is during runtime).
        /// For deterministic serialization of types please use <seealso cref="TypeManager.TypeInfo.StableTypeHash"/>
        /// </remarks>
        /// </summary>
        [FieldOffset(0)]
        public int Value;

        /// <summary>
        /// An invalid <seealso cref="Unity.Entities.TypeIndex"/> which does not map to a valid component type.
        /// </summary>
        public static TypeIndex Null { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  default; } }

        /// <summary>
        /// The component type inherits from <seealso cref="IBufferElementData"/>
        /// </summary>
        public bool IsBuffer { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return   (Value & TypeManager.BufferComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type is a transform type
        /// </summary>
        public bool IsTransform { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return   (Value & TypeManager.TransformComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="ICleanupComponentData"/>
        /// </summary>
        public bool IsCleanupComponent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.CleanupComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="ICleanupSharedComponentData"/>
        /// </summary>
        public bool IsCleanupSharedComponent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.CleanupSharedComponentTypeFlag) == TypeManager.CleanupSharedComponentTypeFlag; } }

        /// <summary>
        /// The component type inherits from <seealso cref="ICleanupBufferElementData"/>
        /// </summary>
        public bool IsCleanupBufferComponent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & TypeManager.CleanupBufferComponentTypeFlag) == TypeManager.CleanupBufferComponentTypeFlag; } }

        /// <summary>
        /// The component type inherits from <seealso cref="IComponentData"/>
        /// </summary>
        public bool IsComponentType { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & (TypeManager.SharedComponentTypeFlag | TypeManager.BufferComponentTypeFlag)) == 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="ISharedComponentData"/>
        /// </summary>
        public bool IsSharedComponentType { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.SharedComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="System.IEquatable{T}"/>
        /// </summary>
        public bool IsIEquatable { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.IEquatableTypeFlag) != 0; } }

        /// <summary>
        /// The component type <seealso cref="TypeIndex.IsManagedType"/> and inherits from <seealso cref="IComponentData"/>
        /// </summary>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public bool IsManagedComponent => IsManagedComponentInternal;

        internal bool IsManagedComponentInternal { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & (TypeManager.ManagedComponentTypeFlag | TypeManager.ChunkComponentTypeFlag | TypeManager.SharedComponentTypeFlag)) == TypeManager.ManagedComponentTypeFlag; } }

        /// <summary>
        /// The component type <seealso cref="TypeIndex.IsManagedType"/> and inherits from <seealso cref="ISharedComponentData"/>
        /// </summary>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public bool IsManagedSharedComponent => false;

        /// <summary>
        /// The component type requires managed storage due to being a class type, and/or contains reference types
        /// </summary>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public bool IsManagedType => IsManagedTypeInternal;

        internal bool IsManagedTypeInternal { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.ManagedComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type allocates 0 bytes in Chunk storage
        /// </summary>
        public bool IsZeroSized { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.ZeroSizeInChunkTypeFlag) != 0; } }

        /// <summary>
        /// The component type is used as a chunk component (a component mapped to a Chunk rather than <seealso cref="Entity"/>)
        /// </summary>
        public bool IsChunkComponent { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & TypeManager.ChunkComponentTypeFlag) != 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="IEnableableComponent"/>
        /// </summary>
        public bool IsEnableable { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.EnableableComponentFlag) != 0; } }

        /// <summary>
        /// The component type inherits from <seealso cref="IRefCounted"/>
        /// </summary>
        public bool IsRefCounted { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.IRefCountedComponentFlag) != 0; } }

        /// <summary>
        /// The component type has an OnAddedCallback.
        /// </summary>
        public bool HasOnAddedCallback { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.HasOnAddedCallbackFlag) != 0; } }
        /// <summary>
        /// The component type has an OnRemovedCallback.
        /// </summary>
        public bool HasOnRemovedCallback { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & TypeManager.HasOnRemovedCallbackFlag) != 0; } }

        /// <summary>
        /// The component type contains an <seealso cref="Entity"/> member. Entity members found in nested member types will also cause this property to return true.
        /// </summary>
        public bool HasEntityReferences { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.HasNoEntityReferencesFlag) == 0; } }

        /// <summary>
        /// The component type contains a <seealso cref="NativeContainerAttribute"/> decorated member. NativeContainer members found in nested member types will also cause this property to return true.
        /// </summary>
        public bool HasNativeContainer { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & TypeManager.HasNativeContainerFlag) != 0; } }

        /// <summary>
        /// The component type is appropriate for chunk serialization. Such types are blittable without containing pointer types or have been decorated with <seealso cref="ChunkSerializableAttribute"/>.
        /// </summary>
        public bool IsChunkSerializable { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return (Value & TypeManager.IsNotChunkSerializableTypeFlag) == 0; } }

        /// <summary>
        /// The component type is decorated with the <seealso cref="TemporaryBakingTypeAttribute"/> attribute.
        /// </summary>
        public bool IsTemporaryBakingType { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.TemporaryBakingTypeFlag) != 0; } }

        /// <summary>
        /// The component type is decorated with the <seealso cref="BakingTypeAttribute"/> attribute.
        /// </summary>
        public bool IsBakingOnlyType { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  (Value & TypeManager.BakingOnlyTypeFlag) != 0; } }

        /// <summary>
        /// Zero-based index for the <seealso cref="Unity.Entities.TypeIndex"/> stored in Value (the type index with no flags).
        /// </summary>
        public int Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  Value & TypeManager.ClearFlagsMask; } }


        /// <summary>
        /// Type flags stored in Value
        /// </summary>
        public int Flags { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return  Value & ~TypeManager.ClearFlagsMask; } }

        /// <summary>
        /// Implicit conversion from TypeIndex to an int.
        /// </summary>
        /// <param name="ti">TypeIndex to convert.</param>
        /// <returns>TypeIndex.Value integer representation.</returns>
        public static implicit operator int(TypeIndex ti) => ti.Value;

        /// <summary>
        /// Implicit conversion from an int to a TypeIndex.
        /// </summary>
        /// <param name="value">int to convert</param>
        /// <returns>TypeIndex representation of the int</returns>
        public static implicit operator TypeIndex(int value) => new TypeIndex { Value = value };

        /// <summary>
        /// <seealso cref="Unity.Entities.TypeIndex"/> instances are equal if they refer to the same component type instance.
        /// </summary>
        /// <remarks>
        /// Note that two <seealso cref="Unity.Entities.TypeIndex"/> for the same Component Type may not always be equal. For example, a type inheriting from IComponentData could be used as a
        /// Chunk Component in one Archetype but not in another. If those two TypeIndices were compared they would not match even though they are for the same System.Type.
        /// </remarks>
        /// <param name="lhs"><seealso cref="Unity.Entities.TypeIndex"/> on left side of the equality expression</param>
        /// <param name="rhs"><seealso cref="Unity.Entities.TypeIndex"/> on right side of the equality expression</param>
        /// <returns>True, if both TypeIndices are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TypeIndex lhs, TypeIndex rhs)
        {
            return lhs.Value == rhs.Value;
        }

        /// <summary>
        /// <seealso cref="Unity.Entities.TypeIndex"/> instances are equal if they refer to the same component type instance.
        /// </summary>
        /// <remarks>
        /// Note that two <seealso cref="Unity.Entities.TypeIndex"/> for the same Component Type may not always be equal. For example, a type inheriting from <seealso cref="Unity.Entities.IComponentData"/> could be used as a
        /// Chunk Component in one Archetype but not in another. If those two TypeIndices were compared they would not match even though they are for the same System.Type.
        /// </remarks>
        /// <param name="lhs"><seealso cref="Unity.Entities.TypeIndex"/> on left side of the equality expression.</param>
        /// <param name="rhs"><seealso cref="Unity.Entities.TypeIndex"/> on right side of the equality expression.</param>
        /// <returns>True, if both TypeIndices are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TypeIndex lhs, TypeIndex rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Evaluates if one <seealso cref="Unity.Entities.TypeIndex"/> is less than the other.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is less than the right-hand side's.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator<(TypeIndex lhs, TypeIndex rhs)
        {
            return lhs.Value < rhs.Value;
        }

        /// <summary>
        /// Evaluates if one <seealso cref="Unity.Entities.TypeIndex"/> is greater than the other.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is greater than the right-hand side's.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator>(TypeIndex lhs, TypeIndex rhs)
        {
            return lhs.Value > rhs.Value;
        }

        /// <summary>
        /// Evaluates if one <seealso cref="Unity.Entities.TypeIndex"/> is less than or equal to the other.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is less than or equal to the right-hand side's.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator<=(TypeIndex lhs, TypeIndex rhs)
        {
            return lhs.Value <= rhs.Value;
        }

        /// <summary>
        /// Evaluates if one <seealso cref="Unity.Entities.TypeIndex"/> is greater than or equal to the other.
        /// </summary>
        /// <param name="lhs">The left-hand side</param>
        /// <param name="rhs">The right-hand side</param>
        /// <returns>True if the left-hand side's <see cref="TypeIndex"/> is greater than or equal to the right-hand side's.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator>=(TypeIndex lhs, TypeIndex rhs)
        {
            return lhs.Value >= rhs.Value;
        }

        /// <summary>
        /// Compare this <seealso cref="Unity.Entities.TypeIndex"/> against a given one
        /// </summary>
        /// <param name="other">The other TypeIndex to compare to</param>
        /// <returns>Difference between TypeIndex values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(TypeIndex other)
        {
            return Value - other.Value;
        }

        /// <summary>
        /// <seealso cref="Unity.Entities.TypeIndex"/> instances are equal if they refer to the same component type instance.
        /// </summary>
        /// <remarks>
        /// Note that two <seealso cref="Unity.Entities.TypeIndex"/> for the same Component Type may not always be equal. For example, a type inheriting from <seealso cref="Unity.Entities.IComponentData"/> could be used as a
        /// Chunk Component in one Archetype but not in another. If those two TypeIndices were compared they would not match even though they are for the same System.Type.
        /// </remarks>
        /// <param name="compare">The object to compare to this <seealso cref="Unity.Entities.TypeIndex"/>.</param>
        /// <returns>True, if the compare parameter contains a <seealso cref="Unity.Entities.TypeIndex"/> object equal to this <seealso cref="Unity.Entities.TypeIndex"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object compare)
        {
            return (compare is TypeIndex compareTypeIndex && Equals(compareTypeIndex));
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Value;
        }

        /// <summary>
        /// <seealso cref="Unity.Entities.TypeIndex"/> instances are equal if they refer to the same component type instance.
        /// </summary>
        /// <remarks>
        /// Note that two  for the <seealso cref="Unity.Entities.TypeIndex"/> same Component Type may not always be equal. For example, a type inheriting from IComponentData could be used as a
        /// Chunk Component in one Archetype but not in another. If those two TypeIndices were compared they would not match even though they are for the same System.Type.
        /// </remarks>
        /// <param name="typeIndex">The other <seealso cref="Unity.Entities.TypeIndex"/>.</param>
        /// <returns>True, if the <seealso cref="Unity.Entities.TypeIndex"/> instances are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TypeIndex typeIndex)
        {
            return typeIndex.Value == Value;
        }

        /// <summary>
        /// Provides a debugging string.
        /// </summary>
        /// <returns>A string containing the entity index and generational version.</returns>
        public override string ToString()
        {
            return ToFixedString().ToString();
        }

        /// <summary>
        /// Provides a Burst compatible debugging string.
        /// </summary>
        /// <returns>A string containing the entity index and generational version.</returns>
        [GenerateTestsForBurstCompatibility]
        public FixedString128Bytes ToFixedString()
        {
            var fs = new FixedString128Bytes();
            fs.Append(TypeManager.GetTypeInfo(this).DebugTypeName);
            return fs;
        }
    }

    /// <summary>
    /// The TypeManager registers all Components and Systems available at runtime. Information about components and systems
    /// can be retrieved at runtime via static methods on TypeManager. The Initialize() method must be invoked before the
    /// TypeManager can be used.
    /// </summary>
    public static unsafe partial class TypeManager
    {
        /// <summary>
        /// Attribute to force the <see cref="TypeInfo.MemoryOrdering"/> for a component to a specific value.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct)]
        public class ForcedMemoryOrderingAttribute : Attribute
        {
            /// <summary>
            /// Force the <see cref="TypeInfo.MemoryOrdering"/> for the component to a specific value.
            /// </summary>
            /// <param name="ordering">Value to force the MemoryOrdering to</param>
            public ForcedMemoryOrderingAttribute(ulong ordering)
            {
                MemoryOrdering = ordering;
            }

            /// <summary>
            /// The forced MemoryOrdering value
            /// </summary>
            public ulong MemoryOrdering;
        }

        /// <summary>
        /// [TypeOverrides] can be applied to a component that is known to contain Entity, Blob and/or UnityObject references,
        /// in order to reduce time taken during serialization operations.
        /// </summary>
        /// <remarks>
        /// Use this attribute to skip the reference patching done during serialization operations
        /// when you know the component type will never contain Entity, Blob and/or UnityObject references.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
        public class TypeOverridesAttribute : Attribute
        {
            /// <summary>
            /// Force the component's <see cref="TypeIndex.HasEntityReferences"/> to be false if set to true here.
            /// Otherwise, the value will only be false if the component actually has no entity references.
            /// </summary>
            public bool  HasNoEntityReferences;
            /// <summary>
            /// Force the component's <see cref="TypeInfo.BlobAssetRefOffsetCount"/> to be zero if set to false.
            /// Otherwise, the value will only be zero if the component actually has a blob reference.
            /// </summary>
            public bool  HasNoBlobReferences;
            /// <summary>
            /// Configures the TypeManager to set the component type's <see cref="TypeInfo.UnityObjectRefOffsetCount"/> to zero.
            /// </summary>
            /// <remarks>
            /// You can use this to bypass the TypeManager check and force the component type's reference to zero.
            /// Set to `false` to set the component type's <see cref="TypeInfo.UnityObjectRefOffsetCount"/> to zero. Set to `true` to set the value to zero only when the component has a Unity object reference.
            /// </remarks>
            public bool  HasNoUnityObjectReferences;

            /// <summary>
            /// <see cref="TypeOverridesAttribute"/>
            /// </summary>
            /// <param name="hasNoEntityReferences">Set to true to ignore entity references in type.</param>
            /// <param name="hasNoBlobReferences">Set to true to ignore blob asset references in type.</param>
            /// <param name="hasNoUnityObjectReferences">Set to true to ignore unity object references in type.</param>
            public TypeOverridesAttribute(bool hasNoEntityReferences, bool hasNoBlobReferences, bool hasNoUnityObjectReferences = false)
            {
                HasNoEntityReferences = hasNoEntityReferences;
                HasNoBlobReferences = hasNoBlobReferences;
                HasNoUnityObjectReferences = hasNoUnityObjectReferences;
            }
        }
        /// <summary>
        /// [ForceReferenceSearch] can be applied to a component that is known to contain Entity, Blob and/or UnityObject references,
        /// in case that the reference is nested beyond the max search depth and can therefore not normally be found.
        /// </summary>
        /// <remarks>
        /// If a type nesting exceeds the max search depth without finding a reference type, it will assume there are none
        /// to be found to avoid a heavy (potentially unnecessary) performance cost of recursing further down.
        /// If you know that there is a reference further down that needs to be caught and the performance hit is acceptable
        /// you can add this attribute to forcibly mark the type as having a certain type of reference.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
        public class ForceReferenceAttribute : Attribute
        {
            /// <summary>
            /// Force the component's <see cref="TypeIndex.HasEntityReferences"/> to be true if set to true here.
            /// Otherwise, the value will only be true if the component actually has entity references that can be found before the maxdepth is hit.
            /// </summary>
            public bool  HasEntityReferences;
            /// <summary>
            /// Configures the TypeManager to force the component to be marked to have BlobAssetReferences.
            /// Otherwise, this will only be set if the component actually has a blob reference that can be found before the maxdepth is hit.
            /// </summary>
            public bool  HasBlobReferences;
            /// <summary>
            /// Configures the TypeManager to  force the component to be marked to have UnityObjectReferences.
            /// Otherwise, this will only be set if the component actually has a UnityObjectReferences that can be found before the maxdepth is hit.
            /// </summary>
            public bool  HasUnityObjectReferences;

            /// <summary>
            /// <see cref="ForceReferenceAttribute"/>
            /// </summary>
            /// <param name="hasEntityReferences"></param>
            /// <param name="hasBlobReferences"></param>
            /// <param name="hasUnityObjectReferences"></param>
            public ForceReferenceAttribute(bool hasEntityReferences, bool hasBlobReferences, bool hasUnityObjectReferences = false)
            {
                HasEntityReferences = hasEntityReferences;
                HasBlobReferences = hasBlobReferences;
                HasUnityObjectReferences = hasUnityObjectReferences;
            }
        }

        /// <summary>
        /// TypeVersion allows you to override the <see cref="TypeManager.TypeInfo.StableTypeHash"/> for a component type
        /// to a specific value rather than using the default generated hash.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct)]
        public class TypeVersionAttribute : Attribute
        {
            /// <summary>
            /// Override the StableTypeHash for the component type.
            /// </summary>
            /// <param name="version">The version to override the StableTypeHash.</param>
            public TypeVersionAttribute(int version)
            {
                TypeVersion = version;
            }

            /// <summary>
            /// The StableTypeHash override
            /// </summary>
            public int TypeVersion;
        }

        /// <summary>
        /// Specifies categories of types the TypeManager manages.
        /// </summary>
        public enum TypeCategory : int
        {
            /// <summary>
            /// Implements IComponentData (can be either a struct or a class)
            /// </summary>
            ComponentData,
            /// <summary>
            /// Implements IBufferElementData (struct only)
            /// </summary>
            BufferData,
            /// <summary>
            /// Implement ISharedComponentData (can be either a struct or a class)
            /// </summary>
            ISharedComponentData,
            /// <summary>
            /// Is an Entity
            /// </summary>
            EntityData,
            /// <summary>
            /// Inherits from UnityEngine.Object (class only)
            /// </summary>
            UnityEngineObject,
            /// <summary>
            /// Represents transform data
            /// </summary>
            TransformData
        }

        /// <summary>
        /// Maximum number of unique component types supported by the <seealso cref="TypeManager"/>/>
        /// </summary>
        public const int MaximumTypesCount = 1 << 12;

        internal const int MaximumSystemTypesCount = MaximumTypesCount;

		/// <summary>
        /// Bitflag set for component types that represent a transform/>.
        /// </summary>
        public const int TransformComponentTypeFlag = 1 << 16;

        /// <summary>
        /// Bitflag set for component types which have an OnRemoved callback
        /// </summary>
        public const int HasOnRemovedCallbackFlag = 1 << 15;

        /// <summary>
        /// Bitflag set for component types which have an OnAdded callback
        /// </summary>
        public const int HasOnAddedCallbackFlag = 1 << 14;

        /// <summary>
        /// Bitflag set for component types that do not contain an <seealso cref="Entity"/> member.
        /// Entity members found in nested member types will cause this bitflag to not be set.
        /// </summary>
        public const int HasNoEntityReferencesFlag = 1 << 17; // this flag is inverted to ensure the type id of Entity can still be 1

        /// <summary>
        /// Bitflag set if a component is not appropriate to be included in chunk serialization.
        /// </summary>
        public const int IsNotChunkSerializableTypeFlag = 1 << 18; // this flag is inverted to ensure the type id of Entity can still be 1

        /// <summary>
        /// Bitflag set for component types with NativeContainer data <seealso cref="NativeContainerAttribute"/>.
        /// </summary>
        public const int HasNativeContainerFlag = 1 << 19;

        /// <summary>
        /// Bitflag set for component types decorated with the <seealso cref="BakingTypeAttribute"/> attribute.
        /// </summary>
        public const int BakingOnlyTypeFlag = 1 << 20;

        /// <summary>
        /// Bitflag set for component types decorated with the <seealso cref="TemporaryBakingTypeAttribute"/> attribute.
        /// </summary>
        public const int TemporaryBakingTypeFlag = 1 << 21;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="IRefCounted"/>.
        /// </summary>
        public const int IRefCountedComponentFlag = 1 << 22;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="System.IEquatable{T}"/>.
        /// </summary>
        public const int IEquatableTypeFlag = 1 << 23;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="IEnableableComponent"/>.
        /// </summary>
        public const int EnableableComponentFlag = 1 << 24;

        /// <summary>
        /// Obsolete. Use <see cref="CleanupComponentTypeFlag"/> instead.
        /// </summary>
        [Obsolete("SystemStateTypeFlag has been renamed to CleanupComponentTypeFlag. SystemStateTypeFlag will be removed in a future package release. (UnityUpgradable) -> CleanupComponentTypeFlag", false)]
        public const int SystemStateTypeFlag = 1 << 25;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ISystemStateComponentData"/>.
        /// </summary>
        public const int CleanupComponentTypeFlag = 1 << 25;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="IBufferElementData"/>.
        /// </summary>
        public const int BufferComponentTypeFlag = 1 << 26;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ISharedComponentData"/>.
        /// </summary>
        public const int SharedComponentTypeFlag = 1 << 27;

        /// <summary>
        /// Bitflag set for component types requiring managed
        /// storage due to being a class type and/or containing managed references.
        /// </summary>
        public const int ManagedComponentTypeFlag = 1 << 28;

        /// <summary>
        /// Bitflag set for component types converted into Chunk Components.
        /// </summary>
        public const int ChunkComponentTypeFlag = 1 << 29;

        /// <summary>
        /// Bitflag set for component types which allocate 0 bytes in Chunk storage
        /// </summary>
        public const int ZeroSizeInChunkTypeFlag = 1 << 30;

        /// <summary>
        /// Obsolete. Use <see cref="CleanupSharedComponentTypeFlag"/>instead.
        /// </summary>
        [Obsolete("SystemStateSharedComponentTypeFlag has been renamed to CleanupSharedComponentTypeFlag. SystemStateSharedComponentTypeFlag will be removed in a future package release. (UnityUpgradable) -> CleanupSharedComponentTypeFlag", false)]
        public const int SystemStateSharedComponentTypeFlag = CleanupComponentTypeFlag | SharedComponentTypeFlag;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ICleanupSharedComponentData"/>.
        /// </summary>
        public const int CleanupSharedComponentTypeFlag = CleanupComponentTypeFlag | SharedComponentTypeFlag;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ICleanupBufferElementData"/>.
        /// </summary>
        public const int CleanupBufferComponentTypeFlag = CleanupComponentTypeFlag | BufferComponentTypeFlag;

        /// <summary>
        /// Bitflag set for component types inheriting from <seealso cref="ISharedComponentData"/> and requiring managed
        /// storage due to containing managed references.
        /// </summary>
        public const int ManagedSharedComponentTypeFlag = ManagedComponentTypeFlag | SharedComponentTypeFlag;

        // Update this clear mask if a flag is added or removed.
        /// <summary>
        /// Bit mask to clear all flag bits from a <seealso cref="TypeIndex"/> />
        /// </summary>
        public const int ClearFlagsMask = MaximumTypesCount-1;

        /// <summary>
        /// Maximum number of <seealso cref="Entity"/> instances stored in a given <seealso cref="Chunk"/>/>
        /// </summary>
        public const int MaximumChunkCapacity = 128;

        /// <summary>
        /// Maximum platform alignment supported when aligning component data in a <seealso cref="Chunk"/>/>
        /// </summary>
        public const int MaximumSupportedAlignment = 16;

        /// <summary>
        /// BufferCapacity is by default calculated as DefaultBufferCapacityNumerator / sizeof(BufferElementDataType)
        /// thus for a 1 byte component, the maximum number of elements possible to be stored in chunk memory before
        /// the buffer is allocated separately from chunk data, is DefaultBufferCapacityNumerator elements.
        /// For a 2 byte sized component, (DefaultBufferCapacityNumerator / 2) elements can be stored, etc...
        /// </summary>
        public const int DefaultBufferCapacityNumerator = 128;

        const int                                       kInitialComponentCount = 2; // one for 'null' and one for 'Entity'
        [NoAutoStaticsCleanup] // Shutdown tears the statics down as code unloads.
        static bool                                     s_Initialized;
        [NoAutoStaticsCleanup]
        static NativeHashMap<int, TypeInfo>             s_TypeInfos;
        [NoAutoStaticsCleanup]
        static UnsafeParallelHashMap<ulong, TypeIndex>  s_StableTypeHashToTypeIndex;
        [NoAutoStaticsCleanup]
        static NativeList<EntityOffsetInfo>             s_EntityOffsetList;
        [NoAutoStaticsCleanup]
        static NativeList<EntityOffsetInfo>             s_BlobAssetRefOffsetList;
        [NoAutoStaticsCleanup]
        static NativeList<EntityOffsetInfo>             s_WeakAssetRefOffsetList;
        [NoAutoStaticsCleanup]
        static NativeList<EntityOffsetInfo>             s_UnityObjectRefOffsetList;
        [NoAutoStaticsCleanup]
        static NativeList<TypeIndex>                    s_WriteGroupList;
        [NoAutoStaticsCleanup]
        static NativeArray<int>                         s_WriteGroupOffsets;
        [NoAutoStaticsCleanup]
        static NativeList<FastEquality.TypeInfo>        s_FastEqualityTypeInfoList;
        [NoAutoStaticsCleanup]
        static Dictionary<int, Type>                    s_Types;
        [NoAutoStaticsCleanup]
        static UnsafeList<UnsafeText>                   s_TypeNames;
        [NoAutoStaticsCleanup]
        static NativeHashMap<int, ulong>                s_TypeFullNameHashes;

        //key is child, value is parent
        [NoAutoStaticsCleanup]
        static UnsafeParallelHashMap<TypeIndex, TypeIndex> s_DescendantMap;

        /// <summary>
        /// Used by codegen. Function pointer wrapper for unmanaged <seealso cref="ISharedComponentData"/> method overloads
        /// </summary>
        public struct SharedComponentFnPtrs
        {
            /// <summary>
            /// Used by codegen. FunctionPointer to a shared component's Retain method
            /// </summary>
            public FunctionPointer<IRefCounted.RefCountDelegate> RetainFn;

            /// <summary>
            /// Used by codegen. FunctionPointer to a shared component's Release method
            /// </summary>
            public FunctionPointer<IRefCounted.RefCountDelegate> ReleaseFn;

            /// <summary>
            /// Used by codegen. FunctionPointer to a shared component's Equals method
            /// </summary>
            public FunctionPointer<FastEquality.TypeInfo.CompareEqualDelegate> EqualsFn;

            /// <summary>
            /// Used by codegen. FunctionPointer to a shared component's GetHashCode method
            /// </summary>
            public FunctionPointer<FastEquality.TypeInfo.GetHashCodeDelegate> GetHashCodeFn;
        }

        /// <summary>
        /// Used by codegen. Managed delegates for <seealso cref="ISharedComponentData"/> method overloads, called from code that isn't Burst compiled
        /// </summary>
        public struct ManagedSharedComponentFnPtrs
        {
            /// <summary>
            /// Used by codegen. Delegate to a shared component's Retain method
            /// </summary>
            public IRefCounted.RefCountDelegate RetainFn;

            /// <summary>
            /// Used by codegen. Delegate to a shared component's Release method
            /// </summary>
            public IRefCounted.RefCountDelegate ReleaseFn;

            /// <summary>
            /// Used by codegen. Delegate to a shared component's Equals method
            /// </summary>
            public FastEquality.TypeInfo.CompareEqualDelegate EqualsFn;

            /// <summary>
            /// Used by codegen. Delegate to a shared component's GetHashCode method
            /// </summary>
            public FastEquality.TypeInfo.GetHashCodeDelegate GetHashCodeFn;
        }

        [NoAutoStaticsCleanup]
        static ManagedSharedComponentFnPtrs[]   s_SharedComponentFns_gcDefeat;
        [NoAutoStaticsCleanup]
        private static UnsafeList<SharedComponentFnPtrs> s_SharedComponent_FunctionPointers;

        /// <summary>
        /// Delegate type for component lifecycle callbacks (IDebugOnAdded/IDebugOnRemoved).
        /// Used by ILPP-generated wrapper code.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ComponentLifecycleDelegate(Entity* entity, IntPtr componentPtr);

        [UnityEngine.Internal.ExcludeFromDocs]
        public struct OnAddedDelegateHolder
        {
            public unsafe delegate* unmanaged[Cdecl]<Entity*, void*, void> FunctionPointer;
        }

        static readonly SharedStatic<UnsafeList<OnAddedDelegateHolder>> s_OnAdded_FunctionPointers
            = SharedStatic<UnsafeList<OnAddedDelegateHolder>>.GetOrCreate<OnAddedDelegateHolder>();

        // GC-defeat array to keep delegates alive for non-Burst callbacks
        [NoAutoStaticsCleanup]
        static Delegate[] s_OnAdded_GCDefeat;

        [UnityEngine.Internal.ExcludeFromDocs]
        public struct OnRemovedDelegateHolder
        {
            public unsafe delegate* unmanaged[Cdecl]<Entity*, void*, void> FunctionPointer;
        }

        static readonly SharedStatic<UnsafeList<OnRemovedDelegateHolder>> s_OnRemoved_FunctionPointers
            = SharedStatic<UnsafeList<OnRemovedDelegateHolder>>.GetOrCreate<OnRemovedDelegateHolder>();

        // GC-defeat array to keep delegates alive for non-Burst callbacks
        [NoAutoStaticsCleanup]
        static Delegate[] s_OnRemoved_GCDefeat;

        // True if any component type has registered lifecycle callbacks.
        // Used to short-circuit callback checks in hot paths.
        struct AnyDebugCallbacksRegisteredKey {}
        static readonly SharedStatic<bool> s_AnyDebugCallbacksRegistered
            = SharedStatic<bool>.GetOrCreate<AnyDebugCallbacksRegisteredKey>();

        internal static bool AnyDebugCallbacksRegistered => s_AnyDebugCallbacksRegistered.Data;

        /// <summary>
        /// Enumerable list of all component <see cref="TypeInfo"/> values.
        /// </summary>
#pragma warning disable 0618 // GetAllTypes is obsolete; AllTypes is the same snapshot and is kept for external callers.
        public static IEnumerable<TypeInfo> AllTypes => GetAllTypes();
#pragma warning restore 0618

        internal static NativeHashMap<int, TypeInfo>.Enumerator GetTypeInfoEnumerator()
        {
            return s_TypeInfos.GetEnumerator();
        }

        [Obsolete("Don't assume the ordering here")]
        public static TypeInfo[] GetAllTypes()
        {
            using var values = s_TypeInfos.GetValueArray(Allocator.Temp);
            return values.ToArray();
        }

        /// <summary>
        /// Returns true if the TypeManager has been initialized, otherwise false.
        /// </summary>
        internal static bool IsInitialized => s_Initialized;

        [NoAutoStaticsCleanup]
        static Dictionary<Type, TypeIndex>  s_ManagedTypeToIndex;
        [NoAutoStaticsCleanup]
        static Dictionary<Type, Exception>  s_FailedTypeBuildException;

        /// <summary>
        /// Offset into managed objects to read instance data
        /// </summary>
        [NoAutoStaticsCleanup]
        public static int                   ObjectOffset;
        [NoAutoStaticsCleanup]
        internal static Type                UnityEngineObjectType;

        // TODO: this creates a dependency on UnityEngine, but makes splitting code in separate assemblies easier. We need to remove it during the biggere refactor.
        struct ObjectOffsetType
        {
#pragma warning disable 0169 // only read through SizeOf
            void* v0;
            // Object layout in CoreCLR is different than in Mono or IL2CPP, as it has only one
            // pointer field as the object header. It is probably a bad idea to depend on VM internal
            // like this at all.
#pragma warning restore 0169
        }

        /// <summary>
        /// Register a UnityEngine.Object type with the TypeManager.
        /// </summary>
        /// <param name="type">The type to register</param>
        /// <exception cref="ArgumentException">Thrown if the type does not inherit from UnityEngine.Object.</exception>
        public static void RegisterUnityEngineObjectType(Type type)
        {
            if (type == null || !type.IsClass || type.IsInterface || type.FullName != "UnityEngine.Object")
                throw new ArgumentException($"{type} must be typeof(UnityEngine.Object).");
            UnityEngineObjectType = type;
        }


        /// <summary>
        /// Stores the byte offset into a component where an <see cref="Entity"/> member is stored.
        /// </summary>
        public struct EntityOffsetInfo
        {
            /// <summary>
            /// Byte offset into a component's instance data where an Entity member field begins.
            /// </summary>
            public int Offset;
        }


        /// <summary>
        /// Provides information about component types such as their runtime size, how much space they use in <seealso cref="Chunk"/> storage,
        /// their <see cref="TypeIndex"/>, how many entity references they contain, and more.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public readonly struct TypeInfo
        {
            /// <summary>
            /// Used internally to construct a TypeInfo.
            /// </summary>
            /// <param name="typeIndex">TypeIndex to use</param>
            /// <param name="category">TypeCategory the component belongs to</param>
            /// <param name="entityOffsetCount">How many entity references a component contains (recursively)</param>
            /// <param name="entityOffsetStartIndex">Index into the entity offset array where this component's entity offset data begins</param>
            /// <param name="memoryOrdering">The memory order for this component</param>
            /// <param name="stableTypeHash">The stable type hash for this component</param>
            /// <param name="bufferCapacity">Number of elements that can be stored in chunk memory before falling back to a native allocation</param>
            /// <param name="sizeInChunk">Number of bytes the component requires in a chunk</param>
            /// <param name="elementSize">Size of the component. For Buffer components, this is the size of the element type</param>
            /// <param name="alignmentInBytes">Alignment of the component</param>
            /// <param name="maximumChunkCapacity">Max number of instances of this component to allow in a single chunk</param>
            /// <param name="hasBlobRefs">Value for if this component has any blob references (in case we force this value true/false)</param>
            /// <param name="blobAssetRefOffsetCount">Number of blob asset references the component really contains</param>
            /// <param name="blobAssetRefOffsetStartIndex">Index into the blob asset reference array where this component's blob asset reference data begins</param>
            /// <param name="weakAssetRefOffsetCount">Number of weak asset references this component contains</param>
            /// <param name="weakAssetRefOffsetStartIndex">Index into the weak asset reference array where this component's weak asset reference data begins</param>
            /// <param name="hasUnityObjectRefs">Value for if this component has any Unity Object references (in case we force this value true/false)</param>
            /// <param name="unityObjectRefOffsetCount">Number of Unity Object references the component really contains</param>
            /// <param name="unityObjectRefOffsetStartIndex">Index into the Unity Object reference array where this component's Unity Object reference data begins</param>
            /// <param name="typeSize">Size of the component type</param>
            /// <param name="bloomFilterMask">The bloom filter mask for this component, used to accelerate "is type T in set of types S" checks. The default value of zero is safe, but ineffective.</param>
            public TypeInfo(int typeIndex, TypeCategory category, int entityOffsetCount, int entityOffsetStartIndex,
                            ulong memoryOrdering, ulong stableTypeHash, int bufferCapacity, int sizeInChunk, int elementSize,
                            int alignmentInBytes, int maximumChunkCapacity,
                            bool hasBlobRefs, int blobAssetRefOffsetCount, int blobAssetRefOffsetStartIndex,
                            int weakAssetRefOffsetCount, int weakAssetRefOffsetStartIndex,
                            bool hasUnityObjectRefs, int unityObjectRefOffsetCount, int unityObjectRefOffsetStartIndex,
                            int typeSize, ulong bloomFilterMask = 0L)
            {
                TypeIndex = new TypeIndex() { Value = typeIndex };
                Category = category;
                EntityOffsetCount = entityOffsetCount;
                EntityOffsetStartIndex = entityOffsetStartIndex;
                MemoryOrdering = memoryOrdering;
                StableTypeHash = stableTypeHash;
                BloomFilterMask = bloomFilterMask;
                BufferCapacity = bufferCapacity;
                SizeInChunk = sizeInChunk;
                ElementSize = elementSize;
                AlignmentInBytes = alignmentInBytes;
                MaximumChunkCapacity = maximumChunkCapacity;
                _HasBlobAssetRefs = hasBlobRefs ? 1 : 0;
                _HasUnityObjectRefs = hasUnityObjectRefs ? 1 : 0;
                BlobAssetRefOffsetCount = blobAssetRefOffsetCount;
                BlobAssetRefOffsetStartIndex = blobAssetRefOffsetStartIndex;
                WeakAssetRefOffsetCount = weakAssetRefOffsetCount;
                WeakAssetRefOffsetStartIndex = weakAssetRefOffsetStartIndex;
                UnityObjectRefOffsetCount = unityObjectRefOffsetCount;
                UnityObjectRefOffsetStartIndex = unityObjectRefOffsetStartIndex;
                TypeSize = typeSize;
            }

            /// <summary>
            /// Deprecated. Used internally to construct a TypeInfo. Write group information is no longer stored in
            /// TypeInfo; the <paramref name="writeGroupCount"/> and <paramref name="writeGroupStartIndex"/> arguments are ignored.
            /// </summary>
            /// <param name="typeIndex">TypeIndex to use</param>
            /// <param name="category">TypeCategory the component belongs to</param>
            /// <param name="entityOffsetCount">How many entity references a component contains (recursively)</param>
            /// <param name="entityOffsetStartIndex">Index into the entity offset array where this component's entity offset data begins</param>
            /// <param name="memoryOrdering">The memory order for this component</param>
            /// <param name="stableTypeHash">The stable type hash for this component</param>
            /// <param name="bufferCapacity">Number of elements that can be stored in chunk memory before falling back to a native allocation</param>
            /// <param name="sizeInChunk">Number of bytes the component requires in a chunk</param>
            /// <param name="elementSize">Size of the component. For Buffer components, this is the size of the element type</param>
            /// <param name="alignmentInBytes">Alignment of the component</param>
            /// <param name="maximumChunkCapacity">Max number of instances of this component to allow in a single chunk</param>
            /// <param name="writeGroupCount">Ignored.</param>
            /// <param name="writeGroupStartIndex">Ignored.</param>
            /// <param name="hasBlobRefs">Value for if this component has any blob references (in case we force this value true/false)</param>
            /// <param name="blobAssetRefOffsetCount">Number of blob asset references the component really contains</param>
            /// <param name="blobAssetRefOffsetStartIndex">Index into the blob asset reference array where this component's blob asset reference data begins</param>
            /// <param name="weakAssetRefOffsetCount">Number of weak asset references this component contains</param>
            /// <param name="weakAssetRefOffsetStartIndex">Index into the weak asset reference array where this component's weak asset reference data begins</param>
            /// <param name="hasUnityObjectRefs">Value for if this component has any Unity Object references (in case we force this value true/false)</param>
            /// <param name="unityObjectRefOffsetCount">Number of Unity Object references the component really contains</param>
            /// <param name="unityObjectRefOffsetStartIndex">Index into the Unity Object reference array where this component's Unity Object reference data begins</param>
            /// <param name="typeSize">Size of the component type</param>
            /// <param name="bloomFilterMask">The bloom filter mask for this component, used to accelerate "is type T in set of types S" checks. The default value of zero is safe, but ineffective.</param>
            [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
            public TypeInfo(int typeIndex, TypeCategory category, int entityOffsetCount, int entityOffsetStartIndex,
                            ulong memoryOrdering, ulong stableTypeHash, int bufferCapacity, int sizeInChunk, int elementSize,
                            int alignmentInBytes, int maximumChunkCapacity, int writeGroupCount, int writeGroupStartIndex,
                            bool hasBlobRefs, int blobAssetRefOffsetCount, int blobAssetRefOffsetStartIndex,
                            int weakAssetRefOffsetCount, int weakAssetRefOffsetStartIndex,
                            bool hasUnityObjectRefs, int unityObjectRefOffsetCount, int unityObjectRefOffsetStartIndex,
                            int typeSize, ulong bloomFilterMask = 0L)
                : this(typeIndex, category, entityOffsetCount, entityOffsetStartIndex, memoryOrdering, stableTypeHash,
                    bufferCapacity, sizeInChunk, elementSize, alignmentInBytes, maximumChunkCapacity,
                    hasBlobRefs, blobAssetRefOffsetCount, blobAssetRefOffsetStartIndex,
                    weakAssetRefOffsetCount, weakAssetRefOffsetStartIndex,
                    hasUnityObjectRefs, unityObjectRefOffsetCount, unityObjectRefOffsetStartIndex,
                    typeSize, bloomFilterMask)
            {
            }

            /// <summary>
            /// <seealso cref="TypeIndex"/>
            /// </summary>
            public   readonly TypeIndex     TypeIndex;

            /// <summary>
            /// The number of bytes used in a <seealso cref="Chunk"/> to store an instance of this component.
            /// </summary>
            /// <remarks>Note that this includes internal capacity and header overhead for buffers. Also, note
            /// that components with no member variables will have a SizeInChunk of 0, but will have a
            /// <seealso cref="TypeSize"/> of GREATER than 0 (since C# does not allow for zero-sized types).</remarks>
            public   readonly int           SizeInChunk;

            /// <summary>
            /// The size of an element store in buffer components. For non-buffer component types, this is the same as <see cref="SizeInChunk"/>
            /// </summary>
            public   readonly int           ElementSize;

            /// <summary>
            /// The maximum number of elements that can be stored in a buffer component instance.
            /// </summary>
            public   readonly int           BufferCapacity;

            /// <summary>
            /// Sort order for component types in <see cref="Chunk"/> storage. By default this is equivalent to <seealso cref="StableTypeHash"/>.
            /// Order is sorted from lowest to highest.
            /// </summary>
            public   readonly ulong         MemoryOrdering;

            /// <summary>
            /// Hash used to uniquely identify a component based on its runtime memory footprint.
            /// </summary>
            /// <remarks>
            /// This value is deterministic across builds provided that the underlying type layout
            /// of the component hasn't changed. For example, renaming a member doesn't affect things,
            /// however changing a member's type causes the parent StableTypeHash to change).
            ///
            /// This hash is NOT expected to be stable across versions.
            /// </remarks>
            /// <seealso cref="TypeHash"/>
            public   readonly ulong         StableTypeHash;

            /// <summary>
            /// Bitmask used to accelerate "is type set A a subset of type set B?" queries.
            /// </summary>
            /// <remarks>
            /// This value is a much lower-entropy hash than <see cref="StableTypeHash"/>, specialized for one particular
            /// use case. It is not guaranteed to be unique for all types in the application.
            /// </remarks>
            /// <seealso cref="TypeHash"/>
            public   readonly ulong         BloomFilterMask;

            /// <summary>
            /// The alignment requirement for the component. For buffer types, this is the alignment requirement of the element type.
            /// </summary>
            public   readonly int           AlignmentInBytes;

            /// <summary>
            /// <seealso cref="TypeCategory"/>
            /// </summary>
            public   readonly TypeCategory  Category;

            /// <summary>
            /// Number of <seealso cref="Entity"/> references this component can store.
            /// </summary>
            public   readonly int           EntityOffsetCount;

            internal readonly int           EntityOffsetStartIndex;
            private  readonly int           _HasBlobAssetRefs;
            private  readonly int           _HasUnityObjectRefs;

            /// <summary>
            /// Number of <seealso cref="BlobAssetReference{T}"/>s this component can store.
            /// </summary>
            public   readonly int           BlobAssetRefOffsetCount;
            internal readonly int           BlobAssetRefOffsetStartIndex;

            /// <summary>
            /// Number of <seealso cref="WeakReference{T}"/>s this component can store.
            /// </summary>
            public   readonly int           WeakAssetRefOffsetCount;
            internal readonly int           WeakAssetRefOffsetStartIndex;

            internal readonly int           UnityObjectRefOffsetCount;
            internal readonly int           UnityObjectRefOffsetStartIndex;

            /// <summary>
            /// Maximum number of instances of this component allowed to be stored in a <seealso cref="Chunk"/>.
            /// </summary>
            public   readonly int           MaximumChunkCapacity;

            /// <summary>
            /// Blittable size of the component type.
            /// </summary>
            public   readonly int           TypeSize;

            /// <summary>
            /// Alignment of this type in a chunk.  Normally the same as AlignmentInBytes, but that
            /// might be less than this value for buffer elements, whereas the buffer itself must be aligned to <seealso cref="MaximumSupportedAlignment"/>.
            /// </summary>
            public int  AlignmentInChunkInBytes
            {
                get
                {
                    if (Category == TypeCategory.BufferData)
                        return MaximumSupportedAlignment;
                    return AlignmentInBytes;
                }
            }

            /// <summary>
            /// <seealso cref="TypeIndex.IsTemporaryBakingType"/>
            /// </summary>
            public bool TemporaryBakingType => IsTemporaryBakingType(TypeIndex);

            /// <summary>
            /// <seealso cref="TypeIndex.IsBakingOnlyType"/>
            /// </summary>
            public bool BakingOnlyType => IsBakingOnlyType(TypeIndex);

            /// <summary>
            /// <seealso cref="TypeIndex.IsEnableable"/>
            /// </summary>
            public bool EnableableType => IsEnableableType(TypeIndex);

            /// <summary>
            /// Returns true if the component does not require space in <seealso cref="Chunk"/> memory
            /// </summary>
            public bool IsZeroSized => SizeInChunk == 0;

            /// <summary>
            /// Deprecated. Number of components which specify this component as the target type in a <seealso cref="WriteGroupAttribute"/>.
            /// </summary>
            /// <remarks>Write group data is not stored in the TypeInfo itself; it is looked up in the TypeManager's
            /// write group table by <see cref="TypeIndex"/>. Only valid after <see cref="TypeManager.Initialize"/> has completed.</remarks>
            [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
            public int WriteGroupCount => GetWriteGroupCount(TypeIndex);

            /// <summary>
            /// Deprecated. Returns true if a component with a <seealso cref="WriteGroupAttribute"/> specifies this component as its `TargetType`.
            /// </summary>
            [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
            public bool HasWriteGroups => WriteGroupCount > 0;

            /// <summary>
            /// For struct IComponentData, a value of true guarantees that there are <seealso cref="BlobAssetReference{T}"/> fields in this component.
            /// For class based IComponentData, a value of true means it is possible, but not guaranteed, that there are blob asset references. (Polymorphic <seealso cref="BlobAssetReference{T}"/> members can not be proven statically)
            /// </summary>
            public bool HasBlobAssetRefs => _HasBlobAssetRefs != 0;

            /// <summary>
            /// For struct IComponentData, a value of true guarantees that there are <seealso cref="WeakReference{T}"/> fields in this component.
            /// For class based IComponentData, a value of true means it is possible, but not guaranteed, that there are WeakReferences. (Polymorphic <seealso cref="WeakReference{T}"/> members can not be proven statically)
            /// </summary>
            public bool HasWeakAssetRefs => WeakAssetRefOffsetCount != 0;

            /// <summary>
            /// Returns whether there are any UnityObjectRefs fields in this component.
            /// </summary>
            /// <remarks>
            /// For struct IComponentData a value of `true` means that there are UnityObjectRefs fields in this component.
            /// For class based IComponentData, a value of `true` means that there might be UnityObjectRefs fields in this component. This is because polymorphic <see cref="UnityObjectRef{T}"/> members can't be proven statically.
            /// A value of `false` means there are no UnityObjectRefs fields in this component.
            /// </remarks>
            public bool HasUnityObjectRefs => _HasUnityObjectRefs != 0;

            /// <summary>
            /// Returns the System.Type for the component this <seealso cref="TypeInfo"/> is describing.
            /// </summary>
            /// <remarks>Unlike other properties, this property performs a lookup in order to not store managed data in <seealso cref="TypeInfo"/> as that would prevent Burst compilation.</remarks>
            [ExcludeFromBurstCompatTesting("Returns managed Type")]
            public Type Type => TypeManager.GetType(TypeIndex);

            /// <summary>
            /// Provides a HPC# / Burst compatible name of the component type when building with DEBUG defined. Otherwise the name is empty.
            /// </summary>
            [GenerateTestsForBurstCompatibility]
            public NativeText.ReadOnly DebugTypeName
            {
                get
                {
                    var pUnsafeText = GetTypeName(TypeIndex);
                    var ro = new NativeText.ReadOnly(pUnsafeText, SharedSafetyHandle.Ref.Data);
                    return ro;
                }
            }
        }

        internal static EntityOffsetInfo* GetEntityOffsetsPointer()
        {
            return (EntityOffsetInfo*) SharedEntityOffsetInfos.Ref.Data;
        }

        internal static EntityOffsetInfo* GetEntityOffsets(in TypeInfo typeInfo)
        {
            return GetEntityOffsetsPointer() + typeInfo.EntityOffsetStartIndex;
        }

        /// <summary>
        /// Gets a pointer to entity offsets for a given type index.
        /// </summary>
        /// <remarks>
        /// This always returns a pointer even if the given type has
        /// no entity offsets. Always check and iterate over the returned pointer using the
        /// returned count.
        /// </remarks>
        /// <param name="typeIndex">The TypeIndex to review.</param>
        /// <param name="count"></param>
        /// <returns>Returns a pointer to the entity offsets.</returns>
        [GenerateTestsForBurstCompatibility]
        public static EntityOffsetInfo* GetEntityOffsets(TypeIndex typeIndex, out int count)
        {
            var typeInfo = GetTypeInfoPointer(typeIndex);
            count = typeInfo->EntityOffsetCount;
            return GetEntityOffsets(*typeInfo);
        }

        internal static EntityOffsetInfo* GetBlobAssetRefOffsetsPointer()
        {
            return (EntityOffsetInfo*) SharedBlobAssetRefOffsets.Ref.Data;
        }

        // Note this function will always return a pointer even if the given type has
        // no BlobAssetReference offsets. Always check/iterate the returned pointer
        // against the TypeInfo.BlobAssetReferenceCount
        internal static EntityOffsetInfo* GetBlobAssetRefOffsets(in TypeInfo typeInfo)
        {
            return GetBlobAssetRefOffsetsPointer() + typeInfo.BlobAssetRefOffsetStartIndex;
        }

        internal static EntityOffsetInfo* GetWeakAssetRefOffsetsPointer()
        {
            return (EntityOffsetInfo*)SharedWeakAssetRefOffsets.Ref.Data;
        }

        internal static EntityOffsetInfo* GetWeakAssetRefOffsets(in TypeInfo typeInfo)
        {
            return GetWeakAssetRefOffsetsPointer() + typeInfo.WeakAssetRefOffsetStartIndex;
        }

        internal static EntityOffsetInfo* GetUnityObjectRefOffsetsPointer()
        {
            return (EntityOffsetInfo*)SharedUnityObjectRefOffsets.Ref.Data;
        }

        internal static EntityOffsetInfo* GetUnityObjectRefOffsets(in TypeInfo typeInfo)
        {
            return GetUnityObjectRefOffsetsPointer() + typeInfo.UnityObjectRefOffsetStartIndex;
        }

        internal static UnsafeParallelHashMapData* GetDescendantMapPointer()
        {
            return (UnsafeParallelHashMapData*)SharedDescendantMap.Ref.Data;
        }

        internal static UnsafeParallelHashMapData* GetStableTypeHashMapPointer()
        {
            return (UnsafeParallelHashMapData*)SharedStableTypeHashes.Ref.Data;
        }

        internal static TypeIndex* GetWriteGroupsPointer()
        {
            return (TypeIndex*)SharedWriteGroups.Ref.Data;
        }

        internal static int* GetWriteGroupOffsetsPointer()
        {
            return (int*)SharedWriteGroupOffsets.Ref.Data;
        }

        /// <summary>
        /// Returns the number of component types which specify <paramref name="typeIndex"/> as the target of a
        /// <see cref="WriteGroupAttribute"/>.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        internal static int GetWriteGroupCount(TypeIndex typeIndex)
        {
            var offsets = GetWriteGroupOffsetsPointer() + typeIndex.Index;
            return offsets[1] - offsets[0];
        }

        /// <summary>
        /// Returns a pointer to the <see cref="TypeIndex"/> values of all component types which specify
        /// <paramref name="typeIndex"/> as the target of a <see cref="WriteGroupAttribute"/>.
        /// </summary>
        /// <param name="typeIndex">The write group target type.</param>
        /// <param name="count">Number of entries in the returned array.</param>
        /// <returns>Pointer to <paramref name="count"/> TypeIndex values (with flags). Never null when count > 0.</returns>
        [GenerateTestsForBurstCompatibility]
        internal static TypeIndex* GetWriteGroups(TypeIndex typeIndex, out int count)
        {
            var offsets = GetWriteGroupOffsetsPointer() + typeIndex.Index;
            var start = offsets[0];
            count = offsets[1] - start;
            return GetWriteGroupsPointer() + start;
        }

        /// <summary>
        /// Deprecated. Retrieves a pointer to an array of WriteGroups for the provided TypeInfo.
        /// </summary>
        /// <param name="typeInfo">TypeInfo for the component with a WriteGroup attribute</param>
        /// <returns>Returns a pointer to an array of WriteGroups for the provided TypeInfo.</returns>
        [GenerateTestsForBurstCompatibility]
        [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
        public  static TypeIndex* GetWriteGroups(in TypeInfo typeInfo)
        {
            var writeGroups = GetWriteGroups(typeInfo.TypeIndex, out var count);
            return count == 0 ? null : writeGroups;
        }

        internal static UnsafeText* GetTypeNamesPointer()
        {
            return (UnsafeText*)SharedTypeNames.Ref.Data;
        }

        internal static UnsafeText* GetTypeName(TypeIndex typeIndex)
        {
            // Engine modules never define DEBUG, which script compiles in the editor do; names follow the define the
            // rest of Entities uses for debug names.
            return GetTypeNamesPointer() + typeIndex.Index;
        }

        internal static FixedString128Bytes GetTypeNameFixed(TypeIndex typeIndex)
        {
            return new FixedString128Bytes(*GetTypeName(typeIndex));
        }

        /// <summary>
        /// Retrieve the TypeInfo for the <see cref="TypeIndex"/>.
        /// </summary>
        /// <param name="typeIndex">TypeIndex to review the TypeInfo for</param>
        /// <returns>Returns the TypeInfo for the component corresponding to the TypeInfo.</returns>
        [GenerateTestsForBurstCompatibility]
        public static ref readonly TypeInfo GetTypeInfo(TypeIndex typeIndex)
        {
            return ref *GetTypeInfoPointer(typeIndex);
        }

        /// <summary>
        /// Retrieve the TypeInfo for the component T.
        /// </summary>
        /// <typeparam name="T">Component type to get TypeInfo fo</typeparam>
        /// <returns>The TypeInfo for the component corresponding to type T.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        public static ref readonly TypeInfo GetTypeInfo<T>()
        {
            return ref *GetTypeInfoPointer(GetTypeIndex<T>());
        }

        internal static TypeInfo GetTypeInfoNoSharedStatic(TypeIndex typeIndex)
        {
            return s_TypeInfos[typeIndex.Index];
        }

        internal static HashMapHelper<int>* GetTypeInfoDataPointer()
        {
            return (HashMapHelper<int>*)SharedTypeInfos.Ref.Data;
        }

        // Retrieves a pointer to the TypeInfo stored in the given TypeInfo map for the provided TypeIndex.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TypeInfo* GetTypeInfoPointer(HashMapHelper<int>* typeInfoMap, TypeIndex typeIndex)
        {
            var entry = typeInfoMap->Find(typeIndex.Index);
            if (entry < 0)
                entry = 0;
            return (TypeInfo*)(typeInfoMap->Ptr + sizeof(TypeInfo) * entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TypeInfo* GetTypeInfoPointer(TypeIndex typeIndex)
        {
            return GetTypeInfoPointer(GetTypeInfoDataPointer(), typeIndex);
        }

        internal static FastEquality.TypeInfo* GetFastEqualityTypeInfoPointer()
        {
            return (FastEquality.TypeInfo*) SharedFastEqualityTypeInfo.Ref.Data;
        }

        internal static HashMapHelper<int>* GetFullTypeNameHashesDataPointer()
        {
            return (HashMapHelper<int>*)SharedTypeFullNameHashes.Ref.Data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong* GetFullNameHashPointer(HashMapHelper<int>* fullNameHashMap, TypeIndex typeIndex)
        {
            var entry = fullNameHashMap->Find(typeIndex.Index);
            if (entry < 0)
                entry = 0;
            return (ulong*)(fullNameHashMap->Ptr + sizeof(ulong) * entry);
        }

        /// <summary>
        /// Gets the System.Type for the component represented by <see cref="TypeIndex"/>.
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component type</param>
        /// <returns>The System.Type for the component.</returns>
        public static Type GetType(TypeIndex typeIndex)
        {
            if (s_Types.TryGetValue(typeIndex.Index, out var type))
                return type;
            return null;
        }

        /// <summary>
        /// Gets the total number of components managed by the <seealso cref="TypeManager"/>.
        /// </summary>
        /// <returns>Returns the total number of components managed by the <seealso cref="TypeManager"/>.</returns>
        public static int GetTypeCount()
        {
            return s_TypeInfos.IsCreated ? s_TypeInfos.Count : 0;
        }

        /// <summary>
        /// <seealso cref="TypeIndex.IsBuffer"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a buffer component</returns>
        public static bool IsBuffer(TypeIndex typeIndex) => (typeIndex.Value & BufferComponentTypeFlag) != 0;

        /// <summary>
        /// <seealso cref="TypeIndex.IsTransform"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a transform component</returns>
        public static bool IsTransform(TypeIndex typeIndex) => (typeIndex.Value & TransformComponentTypeFlag) != 0;

        /// <summary>
        /// Obsolete. Use <see cref="TypeIndex.IsCleanupComponent"/> instead.
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a cleanup component</returns>
        [Obsolete("IsSystemStateComponent() has been renamed to IsCleanupComponent(). IsSystemStateComponent() will be removed in a future package release. (UnityUpgradable) -> IsCleanupComponent(*)", false)]
        public static bool IsSystemStateComponent(TypeIndex typeIndex) => typeIndex.IsCleanupComponent;

        /// <summary>
        /// <seealso cref="TypeIndex.IsCleanupComponent"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a cleanup component</returns>
        public static bool IsCleanupComponent(TypeIndex typeIndex) => typeIndex.IsCleanupComponent;

        /// <summary>
        /// Obsolete. Use <see cref="TypeIndex.IsCleanupSharedComponent"/> instead.
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a cleanup shared component</returns>
        [Obsolete("IsSystemStateSharedComponent() has been renamed to IsCleanupSharedComponent(). IsSystemStateSharedComponent() will be removed in a future package release. (UnityUpgradable) -> IsCleanupSharedComponent(*)", false)]
        public static bool IsSystemStateSharedComponent(TypeIndex typeIndex) => typeIndex.IsCleanupSharedComponent;

        /// <summary>
        /// <seealso cref="TypeIndex.IsCleanupSharedComponent"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component is a cleanup shared component</returns>
        public static bool IsCleanupSharedComponent(TypeIndex typeIndex) => typeIndex.IsCleanupSharedComponent;

        /// <summary>
        /// <seealso cref="TypeIndex.IsSharedComponentType"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>If the component is a shared component</returns>
        public static bool IsSharedComponentType(TypeIndex typeIndex) => typeIndex.IsSharedComponentType;

        /// <summary>
        /// <seealso cref="TypeIndex.IsIEquatable"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component inherits from <seealso cref="IEquatable{T}"/></returns>
        public static bool IsIEquatable(TypeIndex typeIndex) => typeIndex.IsIEquatable;

        /// <summary>
        /// <seealso cref="TypeIndex.IsManagedComponent"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type <seealso cref="TypeIndex.IsManagedType"/> and inherits from <seealso cref="IComponentData"/></returns>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public static bool IsManagedComponent(TypeIndex typeIndex) => typeIndex.IsManagedComponentInternal;

        /// <summary>
        /// <seealso cref="TypeIndex.IsManagedSharedComponent"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type <seealso cref="TypeIndex.IsManagedType"/> and inherits from <seealso cref="ISharedComponentData"/></returns>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public static bool IsManagedSharedComponent(TypeIndex typeIndex) => false;

        /// <summary>
        /// <seealso cref="TypeIndex.IsManagedType"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type requires managed storage due to being a class type, and/or contains reference types</returns>
        [Obsolete("Managed component types are deprecated. This helper will be removed once managed components are no longer supported. First deprecated in 6.6.", true)]
        public static bool IsManagedType(TypeIndex typeIndex) => typeIndex.IsManagedTypeInternal;

        /// <summary>
        /// <seealso cref="TypeIndex.IsZeroSized"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type does not require space in <seealso cref="Chunk"/> memory.</returns>
        public static bool IsZeroSized(TypeIndex typeIndex) => typeIndex.IsZeroSized;

        /// <summary>
        /// <seealso cref="TypeIndex.IsChunkComponent"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type is used as a chunk component (a component mapped to a Chunk rather than <seealso cref="Entity"/>)</returns>
        public static bool IsChunkComponent(TypeIndex typeIndex) => typeIndex.IsChunkComponent;

        /// <summary>
        /// <seealso cref="TypeIndex.IsEnableable"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type inherits from <seealso cref="IEnableableComponent"/></returns>
        public static bool IsEnableable(TypeIndex typeIndex) => typeIndex.IsEnableable;

        /// <summary>
        /// <seealso cref="TypeIndex.HasEntityReferences"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type contains any member fields that are of type <seealso cref="Entity"/></returns>
        public static bool HasEntityReferences(TypeIndex typeIndex) => typeIndex.HasEntityReferences;

        /// <summary>
        /// <seealso cref="TypeIndex.IsTemporaryBakingType"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type is decorated with the <seealso cref="TemporaryBakingTypeAttribute"/> attribute.</returns>
        public static bool IsTemporaryBakingType(TypeIndex typeIndex) => typeIndex.IsTemporaryBakingType;

        /// <summary>
        /// <seealso cref="TypeIndex.IsEnableable"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type is decorated with the <seealso cref="EnableableComponentFlag"/> attribute.</returns>
        public static bool IsEnableableType(TypeIndex typeIndex) => typeIndex.IsEnableable;

        /// <summary>
        /// <seealso cref="TypeIndex.IsBakingOnlyType"/>
        /// </summary>
        /// <param name="typeIndex">TypeIndex for a component</param>
        /// <returns>Returns if the component type is decorated with the <seealso cref="BakingTypeAttribute"/> attribute.</returns>
        public static bool IsBakingOnlyType(TypeIndex typeIndex) => typeIndex.IsBakingOnlyType;

        /// <summary>
        /// Creates a new TypeIndex that allows the passed in 'typeindex' to be used as a chunk component.
        /// </summary>
        /// <param name="typeIndex">The TypeIndex to use as a chunk component.</param>
        /// <returns>Returns the new TypeIndex.</returns>
        public static TypeIndex MakeChunkComponentTypeIndex(TypeIndex typeIndex) => new TypeIndex{ Value = typeIndex.Value | ChunkComponentTypeFlag | ZeroSizeInChunkTypeFlag };

        /// <summary>
        /// Used to determine if a component inherits from another component type
        /// </summary>
        /// <param name="typeIndex">Child type to test if it inherits from baseTypeIndex</param>
        /// <param name="baseTypeIndex">Parent type typeIndex may have inherited from</param>
        /// <returns>Returns if child is a subclass of the parent component type</returns>
        [GenerateTestsForBurstCompatibility]
        public static bool IsDescendantOf(TypeIndex typeIndex, TypeIndex baseTypeIndex)
        {
            UnsafeParallelHashMap<TypeIndex, TypeIndex> map = default;
            map.m_Buffer = GetDescendantMapPointer();

            var parent = typeIndex;
            var iterationCount = 0;
            while (parent != baseTypeIndex)
            {
                iterationCount++;
                if (!map.TryGetValue(parent, out var newparent))
                    break;
                parent = newparent;
            }

            return parent == baseTypeIndex;
        }

        /// <summary>
        /// Returns how many component types are known to the <see cref="TypeManager"/> that inherit from a given component type
        /// </summary>
        /// <param name="typeIndex">Parent type</param>
        /// <returns>Returns count of component types inheriting from typeIndex's component type</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        public static int GetDescendantCount(TypeIndex typeIndex)
        {
            UnsafeParallelHashMap<TypeIndex, TypeIndex> map = default;
            map.m_Buffer = GetDescendantMapPointer();
            var overallCount = 0;
            var keyarray = map.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < map.Count(); i++)
            {
                var startingIndex = keyarray[i];
                var currentIndex = startingIndex;
                var iterationCount = 0;
                while (map.TryGetValue(currentIndex, out var parent))
                {
                    iterationCount++;
                    if (parent == typeIndex)
                    {
                        overallCount++;
                        break;
                    }
                    currentIndex = parent;
                }
            }

            return overallCount;
        }

        /// <summary>
        /// Returns if any component inherits from the provided <seealso cref="TypeIndex"/>
        /// </summary>
        /// <param name="baseTypeIndex">Parent type</param>
        /// <returns>Returns if any component inherits from baseTypeIndex</returns>
        [GenerateTestsForBurstCompatibility]
        public static bool HasDescendants(TypeIndex baseTypeIndex)
        {
            UnsafeParallelHashMap<TypeIndex, TypeIndex> map = default;
            map.m_Buffer = GetDescendantMapPointer();
            var keyarray = map.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < map.Count(); i++)
            {
                var currentIndex = keyarray[i];
                var iterationCount = 0;

                while (map.TryGetValue(currentIndex, out var parent))
                {
                    iterationCount++;

                    if (parent == baseTypeIndex)
                    {
                        return true;
                    }
                    currentIndex = parent;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculate a hash for a component's full type name. Used at TypeManager initialization internally.
        /// </summary>
        /// <param name="fullName">Type.FullName of a component type</param>
        /// <returns>Hash of component type name</returns>
        public static ulong CalculateFullNameHash(string fullName)
        {
            return TypeHash.FNV1A64(fullName);
        }

        /// <summary>
        /// Retrieves a pre-calculated hash for a component's full type name.
        /// </summary>
        /// <param name="typeIndex">TypeIndex for the component to lookup</param>
        /// <returns>Hash of component type name</returns>
        [GenerateTestsForBurstCompatibility]
        public static ulong GetFullNameHash(TypeIndex typeIndex)
        {
            return *GetFullNameHashPointer(GetFullTypeNameHashesDataPointer(), typeIndex);
        }

        [ExcludeFromBurstCompatTesting("Takes a managed Type")]
        private static void AddTypeInfoToTables(Type type, TypeInfo typeInfo, string typeName)
        {
            if (!s_StableTypeHashToTypeIndex.TryAdd(typeInfo.StableTypeHash, typeInfo.TypeIndex))
            {
                var previousTypeIndexNoFlags = s_StableTypeHashToTypeIndex[typeInfo.StableTypeHash].Index;
                if (!ReferenceEquals(s_Types[previousTypeIndexNoFlags], type))
                    throw new ArgumentException($"{type} and {s_Types[previousTypeIndexNoFlags]} have a conflict in the stable type hash. Use the [TypeVersion(...)] attribute to force a different stable type hash for one of them.");

                // Same type registered again, e.g. from an ILPP type registry and then reprocessed by
                // reflection (wrong codegen sizes, lifecycle callbacks): last entry wins, matching the
                // duplicate handling in RegisterAssemblyTypes.
                s_StableTypeHashToTypeIndex[typeInfo.StableTypeHash] = typeInfo.TypeIndex;
            }

            s_TypeInfos[typeInfo.TypeIndex.Index] = typeInfo;

            s_Types[typeInfo.TypeIndex.Index] =  type;

            SetTypeName(typeInfo.TypeIndex.Index, typeName);
            s_TypeFullNameHashes[typeInfo.TypeIndex.Index] =  TypeHash.FNV1A64(typeName);

            if (type != null)
            {
                SharedTypeIndex.Get(type) = typeInfo.TypeIndex;
                if (!s_ManagedTypeToIndex.TryAdd(type, typeInfo.TypeIndex))
                {
                    s_ManagedTypeToIndex[type] = typeInfo.TypeIndex;
                }
            }
        }

[OnCodeUnloading]
        static void OnCodeUnloading()
        {
            if (s_Initialized)
                Shutdown();
        }

        /// <summary>
        /// Initializes the TypeManager with all ECS type information. May be called multiple times; only the first call
        /// will do any work. Always must be called from the main thread.
        /// </summary>
        public static void Initialize()
        {
            if (s_Initialized)
                return;

            // Synchronous Burst compilation takes forever, so in the editor don't waste time for
            // users who are trying to iterate and turn the option off while we generate type info
            bool originalBurstCompileSyncValue = BurstCompiler.Options.EnableBurstCompileSynchronously;
            BurstCompiler.Options.EnableBurstCompileSynchronously = false;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                if (!EditorLightShim.Hooks.CurrentThreadIsMainThread())
                    throw new InvalidOperationException("Must be called from the main thread");

                ObjectOffset = UnsafeUtility.SizeOf<ObjectOffsetType>();
                s_ManagedTypeToIndex = new Dictionary<Type, TypeIndex>(1000);
                s_FailedTypeBuildException = new Dictionary<Type, Exception>();

                s_TypeInfos = new NativeHashMap<int, TypeInfo>(MaximumTypesCount, Allocator.Persistent);
                s_DescendantMap = new UnsafeParallelHashMap<TypeIndex, TypeIndex> (MaximumTypesCount, Allocator.Persistent);
                s_StableTypeHashToTypeIndex = new UnsafeParallelHashMap<ulong, TypeIndex>(MaximumTypesCount, Allocator.Persistent);
                s_EntityOffsetList = new NativeList<EntityOffsetInfo>(Allocator.Persistent);
                s_BlobAssetRefOffsetList = new NativeList<EntityOffsetInfo>(Allocator.Persistent);
                s_WeakAssetRefOffsetList = new NativeList<EntityOffsetInfo>(Allocator.Persistent);
                s_UnityObjectRefOffsetList = new NativeList<EntityOffsetInfo>(Allocator.Persistent);
                s_WriteGroupList = new NativeList<TypeIndex>(Allocator.Persistent);
                s_WriteGroupOffsets = new NativeArray<int>(MaximumTypesCount + 1, Allocator.Persistent);
                s_FastEqualityTypeInfoList = new NativeList<FastEquality.TypeInfo>(MaximumTypesCount, Allocator.Persistent);
                s_FastEqualityTypeInfoList.Resize(MaximumTypesCount, NativeArrayOptions.ClearMemory);
                s_Types = new Dictionary<int, Type>();
                s_TypeNames = new UnsafeList<UnsafeText>(MaximumTypesCount, Allocator.Persistent);
                s_TypeNames.Resize(MaximumTypesCount, NativeArrayOptions.ClearMemory);
                s_TypeFullNameHashes = new NativeHashMap<int, ulong>(MaximumTypesCount, Allocator.Persistent);
                s_SharedComponent_FunctionPointers = new UnsafeList<SharedComponentFnPtrs>(MaximumTypesCount, Allocator.Persistent);
                s_SharedComponentFns_gcDefeat = new ManagedSharedComponentFnPtrs[MaximumTypesCount];
                s_OnAdded_FunctionPointers.Data = new UnsafeList<OnAddedDelegateHolder>(MaximumTypesCount, Allocator.Persistent);
                s_OnAdded_FunctionPointers.Data.Resize(MaximumTypesCount);
                s_OnAdded_GCDefeat = new Delegate[MaximumTypesCount];
                s_OnRemoved_FunctionPointers.Data = new UnsafeList<OnRemovedDelegateHolder>(MaximumTypesCount, Allocator.Persistent);
                s_OnRemoved_FunctionPointers.Data.Resize(MaximumTypesCount);
                s_OnRemoved_GCDefeat = new Delegate[MaximumTypesCount];

                FastEquality.Initialize();
                InitializeSystemsState();

                // There are some types that must be registered first such as a null component and Entity
                RegisterSpecialComponents();
                RegisterSpecialSystems();
                Assert.IsTrue(kInitialComponentCount == s_TypeInfos.Count);

                InitializeAllComponentTypes();

                InitializeAllSystemTypes();


                // Must occur after we've constructed s_TypeInfos
                InitializeSharedStatics();

                EntityNameStorage.Initialize();
                TransformUnionCallbacks.Initialize();
            }
            catch
            {
                s_Initialized = true;
                Shutdown();
                throw;
            }
            finally
            {
                // Save the time since profiler might not catch the first frame.
                stopWatch.Stop();
                Console.WriteLine($"TypeManager.Initialize took: {stopWatch.ElapsedMilliseconds}ms");

                BurstCompiler.Options.EnableBurstCompileSynchronously = originalBurstCompileSyncValue;
            }

            s_Initialized = true;
        }

        internal static void InitializeSharedStatics()
        {
            // Calling InitializeSharedStatics when they are already initialized is wasteful but not an error.
            // So we check if the handle exists before creating it to avoid leaking it if it does.
            if (AtomicSafetyHandle.IsDefaultValue(SharedSafetyHandle.Ref.Data))
            {
                SharedSafetyHandle.Ref.Data = AtomicSafetyHandle.Create();
            }
            Shared_SharedComponentData_FnPtrs.Ref.Data = new IntPtr(s_SharedComponent_FunctionPointers.Ptr);
            SharedTypeInfos.Ref.Data = new IntPtr(s_TypeInfos.m_Data);
            SharedDescendantMap.Ref.Data = new IntPtr(s_DescendantMap.m_Buffer);
            SharedEntityOffsetInfos.Ref.Data = new IntPtr(s_EntityOffsetList.GetUnsafePtr());
            SharedBlobAssetRefOffsets.Ref.Data = new IntPtr(s_BlobAssetRefOffsetList.GetUnsafePtr());
            SharedWeakAssetRefOffsets.Ref.Data = new IntPtr(s_WeakAssetRefOffsetList.GetUnsafePtr());
            SharedUnityObjectRefOffsets.Ref.Data = new IntPtr(s_UnityObjectRefOffsetList.GetUnsafePtr());
            SharedStableTypeHashes.Ref.Data = new IntPtr(s_StableTypeHashToTypeIndex.m_Buffer);
            SharedWriteGroups.Ref.Data = new IntPtr(s_WriteGroupList.GetUnsafePtr());
            SharedWriteGroupOffsets.Ref.Data = new IntPtr(s_WriteGroupOffsets.GetUnsafePtr());
            SharedFastEqualityTypeInfo.Ref.Data = new IntPtr(s_FastEqualityTypeInfoList.GetUnsafePtr());
            SharedTypeNames.Ref.Data = new IntPtr(s_TypeNames.Ptr);
            SharedTypeFullNameHashes.Ref.Data = new IntPtr(s_TypeFullNameHashes.m_Data);
            InitializeSystemSharedStatics();
        }

        internal static void ShutdownSharedStatics()
        {
            // Calling ShutdownSharedStatics when the safety handle is missing can happen when the
            // initialization of the type manager is interrupted because it ran into a problem.
            // When it happens, it shouldn't fail here so we're checking is the handle is valid first.
            if (!AtomicSafetyHandle.IsDefaultValue(SharedSafetyHandle.Ref.Data))
            {
                AtomicSafetyHandle.CheckDeallocateAndThrow(SharedSafetyHandle.Ref.Data);
                AtomicSafetyHandle.Release(SharedSafetyHandle.Ref.Data);
                SharedSafetyHandle.Ref.Data = default;
            }
            Shared_SharedComponentData_FnPtrs.Ref.Data = default;
            SharedTypeInfos.Ref.Data = default;
            SharedDescendantMap.Ref.Data = default;
            SharedEntityOffsetInfos.Ref.Data = default;
            SharedBlobAssetRefOffsets.Ref.Data = default;
            SharedWeakAssetRefOffsets.Ref.Data = default;
            SharedUnityObjectRefOffsets.Ref.Data = default;
            SharedStableTypeHashes.Ref.Data = default;
            SharedWriteGroups.Ref.Data = default;
            SharedWriteGroupOffsets.Ref.Data = default;
            SharedFastEqualityTypeInfo.Ref.Data = default;
            SharedTypeNames.Ref.Data = default;
            SharedTypeFullNameHashes.Ref.Data = default;

            SharedSystemTypeNames.Ref.Data = default;
            SharedSystemAttributes.Ref.Data = default;
            SharedSystemCount.Ref.Data = default;
            SharedSystemTypeInfos.Ref.Data = default;
        }

        static void RegisterSpecialComponents()
        {
            // Push Null TypeInfo -- index 0 is reserved for null/invalid in all arrays index by TypeIndex.Index
            AddFastEqualityInfo(0, null);
            AddTypeInfoToTables(null,
                new TypeInfo(0, TypeCategory.ComponentData, 0, -1,
                    0L, 0L,  -1, 0, 0, 0,
                    TypeManager.MaximumChunkCapacity, false, 0,
                    -1, 0, -1, false, 0, -1, 0, 0L),
                "null");

            // Push Entity TypeInfo
            var entityTypeIndex = new TypeIndex { Value = 1 };
            ulong entityStableTypeHash;

            AddFastEqualityInfo(entityTypeIndex.Index, typeof(Entity));
            entityStableTypeHash = TypeHash.CalculateStableTypeHash(typeof(Entity));

            // Entity is special and is treated as having an entity offset at 0 (itself)
            s_EntityOffsetList.Add(new EntityOffsetInfo() { Offset = 0 });
            AddTypeInfoToTables(typeof(Entity),
                new TypeInfo(1, TypeCategory.EntityData, entityTypeIndex.Value, 0,
                    0, entityStableTypeHash, -1, UnsafeUtility.SizeOf<Entity>(),
                    UnsafeUtility.SizeOf<Entity>(), CalculateAlignmentInChunk(sizeof(Entity)),
                    TypeManager.MaximumChunkCapacity, false, 0,
                    -1, 0, -1, false, 0, -1, UnsafeUtility.SizeOf<Entity>(),
                    bloomFilterMask:0L),
                "Unity.Entities.Entity");

            SharedTypeIndex<Entity>.Ref.Data = entityTypeIndex;
        }

        /// <summary>
        /// Removes all ECS type information and any allocated memory. May only be called once globally, and must be
        /// called from the main thread.
        /// </summary>
        [EntitiesCleanup]
        public static void Shutdown()
        {
            // TODO, with module loaded type info, we cannot shutdown

            if (!s_Initialized)
                return;

            s_Initialized = false;

            s_Types.Clear();

            ShutdownSystemsState();

            s_FailedTypeBuildException = null;
            s_ManagedTypeToIndex.Clear();
            CoreModuleLightShim.Hooks.ClearCompanionComponentTypes();
            s_PendingLifecycleCallbacks = null;
            s_SharedComponentFns_gcDefeat = null;
            s_OnAdded_GCDefeat = null;
            s_OnRemoved_GCDefeat = null;

            DisposeNative();

            ShutdownSharedStatics();
            EntityNameStorage.Shutdown();
            TransformUnionCallbacks.Shutdown();

            FastEquality.Shutdown();
        }

        static void DisposeNative()
        {
            s_TypeInfos.Dispose();
            s_DescendantMap.Dispose();
            s_StableTypeHashToTypeIndex.Dispose();
            s_EntityOffsetList.Dispose();
            s_BlobAssetRefOffsetList.Dispose();
            s_WeakAssetRefOffsetList.Dispose();
            s_UnityObjectRefOffsetList.Dispose();
            s_WriteGroupList.Dispose();
            s_WriteGroupOffsets.Dispose();
            s_SharedComponent_FunctionPointers.Dispose();
            s_OnAdded_FunctionPointers.Data.Dispose();
            s_OnRemoved_FunctionPointers.Data.Dispose();
            s_AnyDebugCallbacksRegistered.Data = false;

            foreach (var info in s_FastEqualityTypeInfoList)
                info.Dispose();
            s_FastEqualityTypeInfoList.Dispose();

            foreach (var name in s_TypeNames)
            {
                if (name.IsCreated)
                    name.Dispose();
            }
            s_TypeNames.Dispose();
            s_TypeFullNameHashes.Dispose();
        }

        private static TypeIndex FindTypeIndex(Type type)
        {
            if (type == null)
                return TypeIndex.Null;

            var res = TypeIndex.Null;
            s_ManagedTypeToIndex.TryGetValue(type, out res);

            return res;
        }

        internal static bool TryGetTypeIndex(Type type, out TypeIndex index)
        {
            if (type == null)
            {
                index = TypeIndex.Null;
                return true;
            }

            TypeIndex res;
            if (s_ManagedTypeToIndex.TryGetValue(type, out res))
            {
                index = res;
                return true;
            }

            index = TypeIndex.Null;
            return false;
        }

        [BurstDiscard]
        static void ManagedException<T>()
        {
            ManagedException(typeof(T));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void BurstException<T>()
        {
            throw new ArgumentException($"Unknown Type:`{typeof(T)}` All Entities component types must be registered with the TypeManager. " +
                $"For generic components and UnityEngine components, register with [RegisterGenericComponentType] and [RegisterUnityEngineComponentType] respectively. " +
                $"For component types known only at runtime, register with TypeManager.GetOrCreateTypeIndex().");
        }

        static void ManagedException(Type type)
        {
            Assert.IsTrue(s_Initialized, "Ensure TypeManager.Initialize has been called before using the TypeManager");
            // When the type is known but failed to build, we repeat the reason why it failed to build instead.
            if (type != null && s_FailedTypeBuildException.TryGetValue(type, out var exception))
                throw new ArgumentException(exception.Message);
            // Otherwise it wasn't registered at all
            else
            throw new ArgumentException($"Unknown Type:`{(type == null ? "null" : type)}` All Entities component types must be registered with the TypeManager. " +
                $"For generic components and UnityEngine components, register with [RegisterGenericComponentType] and [RegisterUnityEngineComponentType] respectively. " +
                $"For component types known only at runtime, register with TypeManager.GetOrCreateTypeIndex().");
        }

        /// <summary>
        /// Fetches the TypeIndex for a given T.
        /// </summary>
        /// <typeparam name="T">A component type</typeparam>
        /// <returns>Returns the TypeIndex for the corresponding T component type. Otherwise <seealso cref="TypeIndex.Null"/> is returned.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        public static TypeIndex GetTypeIndex<T>()
        {
            var index = SharedTypeIndex<T>.Ref.Data;

            if (index == TypeIndex.Null)
            {
                ManagedException<T>();
                BurstException<T>();
            }

            return index;
        }

        /// <summary>
        /// Fetches the TypeIndex for a given System.Type.
        /// </summary>
        /// <param name="type">A component type</param>
        /// <returns>Returns the TypeIndex for the corresponding System.Type component type. Otherwise <seealso cref="TypeIndex.Null"/> is returned.</returns>
        public static TypeIndex GetTypeIndex(Type type)
        {
            var index = FindTypeIndex(type);

            if (index == TypeIndex.Null)
                ManagedException(type);

            return index;
        }

        /// <summary>
        /// Compares two component instances to one another
        /// </summary>
        /// <typeparam name="T">Type of the component instances</typeparam>
        /// <param name="left">Left-hand side of the comparison</param>
        /// <param name="right">Right-hand side of the comparison</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        public static bool Equals<T>(ref T left, ref T right) where T : struct
        {
            var typeIndex = GetTypeIndex<T>();
            if (typeIndex.IsSharedComponentType && IsIEquatable(typeIndex))
            {
                return SharedComponentEquals(UnsafeUtility.AddressOf(ref left), UnsafeUtility.AddressOf(ref right), typeIndex);
            }

            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref left), UnsafeUtility.AddressOf(ref right), UnsafeUtility.SizeOf<T>()) == 0;
        }

        /// <summary>
        /// Compares two component instances to one another
        /// </summary>
        /// <param name="left">Left-hand side of the comparison</param>
        /// <param name="right">Right-hand side of the comparison</param>
        /// <param name="typeIndex">TypeIndex for the two instances being compared</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility]
        public static bool Equals(void* left, void* right, TypeIndex typeIndex)
        {
            if (typeIndex.IsSharedComponentType)
                return SharedComponentEquals(left, right, typeIndex);

            var typeInfo = GetTypeInfo(typeIndex);
            return UnsafeUtility.MemCmp(left, right, typeInfo.TypeSize) == 0;
        }

        [BurstDiscard]
        static void SetIfNotBurst(ref bool answer)
        {
            answer = true;
        }

        /// <summary>
        /// Used internally to determine if Burst is compiling the code path calling this function
        /// </summary>
        /// <returns>Returns true if Burst compiled this method</returns>
        static bool IsBursted()
        {
            var ret = false;
            SetIfNotBurst(ref ret);
            return !ret;
        }

        /// <summary>
        /// Compares two shared component instances to one another
        /// </summary>
        /// <param name="left">Left-hand side of the comparison</param>
        /// <param name="right">Right-hand side of the comparison</param>
        /// <param name="typeIndex">TypeIndex for the two instances being compared</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility]
        internal static bool SharedComponentEquals(void* left, void* right, TypeIndex typeIndex)
        {
            if (typeIndex.IsIEquatable)
            {
                CallManagedEquals(left, right, typeIndex, out bool wasequal, out var didwrite);
                if (didwrite)
                    return wasequal;

                return GetIEquatable_EqualsFn(typeIndex).Invoke(left, right);
            }

            var typeInfo = GetTypeInfoPointer(typeIndex);
            return UnsafeUtility.MemCmp(left, right, typeInfo->TypeSize) == 0;
        }

        [BurstDiscard]
        private static void CallManagedEquals(void* left, void* right, TypeIndex typeIndex, out bool wasequal, out bool didwrite)
        {
            didwrite = true;
            wasequal = s_SharedComponentFns_gcDefeat[typeIndex.Index].EqualsFn.Invoke(left, right);
        }

        /// <summary>
        /// Generates a hash for a given shared component instance
        /// </summary>
        /// <param name="data">Pointer to component instance to hash</param>
        /// <param name="typeIndex">TypeIndex for the component</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility]
        internal static int SharedComponentGetHashCode(void* data, TypeIndex typeIndex)
        {
            // A null pointer represents a default shared component value; hashing it would dereference null, so use the default hash of 0.
            if (data == null)
                return 0;

            if (typeIndex.IsIEquatable)
            {
                CallManagedGetHashCode(data, typeIndex, out var hashCode, out var didwrite);
                if (didwrite)
                    return hashCode;

                var ghc = GetIEquatable_GetHashCodeFn(typeIndex);

                if (ghc.IsCreated)
                    return ghc.Invoke(data);
            }

            var typeInfo = GetTypeInfoPointer(typeIndex);
            return (int)XXHash.Hash32((byte*)data, typeInfo->TypeSize);
        }

        [BurstDiscard]
        private static void CallManagedGetHashCode(
            void* data,
            TypeIndex typeIndex,
            out int hashCodeWithBurst,
            out bool didwrite)
        {
            var ghc = s_SharedComponentFns_gcDefeat[typeIndex.Index].GetHashCodeFn;
            if (ghc != null)
            {
                hashCodeWithBurst = ghc.Invoke(data);
                didwrite = true;
            }
            else
            {
                hashCodeWithBurst = 0;
                didwrite = false;
            }
        }

        /// <summary>
        /// Compares two component instances to one another
        /// </summary>
        /// <param name="left">Left-hand side of the comparison</param>
        /// <param name="right">Right-hand side of the comparison</param>
        /// <param name="typeIndex">TypeIndex for the two instances being compared</param>
        /// <returns>Returns true if the types are equal.</returns>
        public static bool Equals(object left, object right, TypeIndex typeIndex)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (typeIndex.IsManagedTypeInternal)
            {
                throw new ArgumentException($"'{GetType(typeIndex)}' is a UnityEngine.Object type, which can't be an entity component and can't be compared as one.");
            }
            else
            {
                    var leftHandle = GCHandle.Alloc(left, GCHandleType.Pinned);
                    var rightHandle = GCHandle.Alloc(right, GCHandleType.Pinned);
                    var leftptr = (byte*)leftHandle.AddrOfPinnedObject();
                    var rightptr = (byte*)rightHandle.AddrOfPinnedObject();

                    var result = Equals(leftptr, rightptr, typeIndex);

                    leftHandle.Free();
                    rightHandle.Free();

                    return result;
            }
        }

        /// <summary>
        /// Compares two component instances to one another
        /// </summary>
        /// <param name="left">Left-hand side of the comparison</param>
        /// <param name="right">Right-hand side of the comparison</param>
        /// <param name="typeIndex">TypeIndex for the two instances being compared</param>
        /// <returns>Returns true if the types are equal.</returns>
        public static bool Equals(object left, void* right, TypeIndex typeIndex)
        {
            var leftHandle = GCHandle.Alloc(left, GCHandleType.Pinned);
            var leftptr = (byte*)leftHandle.AddrOfPinnedObject();

            var result = Equals(leftptr, right, typeIndex);

            leftHandle.Free();
            return result;
        }

        /// <summary>
        /// Generates a hash for a given component instance
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="val">Component instance to hash</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        public static int GetHashCode<T>(ref T val) where T : struct
        {
            var typeIndex = GetTypeIndex<T>();

            return GetHashCode(UnsafeUtility.AddressOf(ref val), typeIndex);

        }

        /// <summary>
        /// Generates a hash for a given component instance
        /// </summary>
        /// <param name="val">Pointer to component instance to hash</param>
        /// <param name="typeIndex">TypeIndex for the component</param>
        /// <returns>Returns true if the types are equal.</returns>
        [GenerateTestsForBurstCompatibility]
        public static int GetHashCode(void* val, TypeIndex typeIndex)
        {
            if (typeIndex.IsSharedComponentType)
                return SharedComponentGetHashCode(val, typeIndex);

            var typeInfo = GetTypeInfo(typeIndex);
            return (int)XXHash.Hash32((byte*)val, typeInfo.TypeSize);
        }

        /// <summary>
        /// Generates a hash for a given component instance
        /// </summary>
        /// <param name="val">Component instance to hash</param>
        /// <param name="typeIndex">TypeIndex for the component</param>
        /// <returns>Returns true if the types are equal.</returns>
        public static int GetHashCode(object val, TypeIndex typeIndex)
        {
            if (typeIndex.IsManagedTypeInternal)
            {
                throw new ArgumentException($"'{GetType(typeIndex)}' is a UnityEngine.Object type, which can't be an entity component and can't be hashed as one.");
            }
            else
            {
                var handle = GCHandle.Alloc(val, GCHandleType.Pinned);
                var ptr = (byte*)handle.AddrOfPinnedObject();
                var result = GetHashCode(ptr, typeIndex);

                handle.Free();
                return result;
            }
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="indexInTypeArray">TypeIndex.Index</param>
        /// <param name="fn">Equals function</param>
        /// <param name="burstCompile">Is burst enabled</param>
        public static void SetIEquatable_EqualsFn(int indexInTypeArray, Delegate fn, bool burstCompile)
        {
            IntPtr fnPtr;

            if (burstCompile)
                fnPtr = BurstCompiler.CompileFunctionPointer(fn).Value;
            else
                fnPtr = Marshal.GetFunctionPointerForDelegate(fn);

            s_SharedComponentFns_gcDefeat[indexInTypeArray & ClearFlagsMask].EqualsFn =
                (FastEquality.TypeInfo.CompareEqualDelegate) fn;
            s_SharedComponent_FunctionPointers.Ptr[indexInTypeArray & ClearFlagsMask].EqualsFn =
                new FunctionPointer<FastEquality.TypeInfo.CompareEqualDelegate>(fnPtr);
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="indexInTypeArray">TypeIndex.Index</param>
        /// <param name="fn">GetHashCode function</param>
        /// <param name="burstCompile">Is burst enabled</param>
        public static void SetIEquatable_GetHashCodeFn(int indexInTypeArray, Delegate fn, bool burstCompile)
        {
            IntPtr fnPtr;

            if (burstCompile)
                fnPtr = BurstCompiler.CompileFunctionPointer(fn).Value;
            else
                fnPtr = Marshal.GetFunctionPointerForDelegate(fn);

            s_SharedComponentFns_gcDefeat[indexInTypeArray & ClearFlagsMask].GetHashCodeFn =
                (FastEquality.TypeInfo.GetHashCodeDelegate) fn;

            s_SharedComponent_FunctionPointers.Ptr[indexInTypeArray & ClearFlagsMask].GetHashCodeFn =
                new FunctionPointer<FastEquality.TypeInfo.GetHashCodeDelegate>(fnPtr);
        }


        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="indexInTypeArray">TypeIndex.Index</param>
        /// <param name="fn">Retain function</param>
        /// <param name="burstCompile">Is burst enabled</param>
        public static void SetIRefCounted_RetainFn(int indexInTypeArray, IRefCounted.RefCountDelegate fn, bool burstCompile)
        {
            IntPtr fnPtr;

            if (burstCompile)
                fnPtr = BurstCompiler.CompileFunctionPointer(fn).Value;
            else
                fnPtr = Marshal.GetFunctionPointerForDelegate(fn);

            s_SharedComponentFns_gcDefeat[indexInTypeArray & ClearFlagsMask].RetainFn = fn;

            s_SharedComponent_FunctionPointers.Ptr[indexInTypeArray & ClearFlagsMask].RetainFn =
                new FunctionPointer<IRefCounted.RefCountDelegate>(fnPtr);
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="indexInTypeArray">TypeIndex.Index</param>
        /// <param name="fn">Release function</param>
        /// <param name="burstCompile">Is burst enabled</param>
        public static void SetIRefCounted_ReleaseFn(int indexInTypeArray, IRefCounted.RefCountDelegate fn, bool burstCompile)
        {
            IntPtr fnPtr;

            if (burstCompile)
                fnPtr = BurstCompiler.CompileFunctionPointer(fn).Value;
            else
                fnPtr = Marshal.GetFunctionPointerForDelegate(fn);

            s_SharedComponentFns_gcDefeat[indexInTypeArray & ClearFlagsMask].ReleaseFn = fn;

            s_SharedComponent_FunctionPointers.Ptr[indexInTypeArray & ClearFlagsMask].ReleaseFn =
                new FunctionPointer<IRefCounted.RefCountDelegate>(fnPtr);
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <returns>FunctionPointer</returns>
        public static FunctionPointer<FastEquality.TypeInfo.CompareEqualDelegate> GetIEquatable_EqualsFn(TypeIndex typeIndex)
        {
            return ((SharedComponentFnPtrs*) Shared_SharedComponentData_FnPtrs.Ref.Data)[typeIndex.Index].EqualsFn;
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <returns>FunctionPointer</returns>
        public static FunctionPointer<FastEquality.TypeInfo.GetHashCodeDelegate> GetIEquatable_GetHashCodeFn(TypeIndex typeIndex)
        {
            return ((SharedComponentFnPtrs*) Shared_SharedComponentData_FnPtrs.Ref.Data)[typeIndex.Index].GetHashCodeFn;
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <param name="data">Component instance</param>
        public static void CallIRefCounted_Retain(TypeIndex typeIndex, IntPtr data)
        {
            if (IsBursted())
                ((SharedComponentFnPtrs*) Shared_SharedComponentData_FnPtrs.Ref.Data)[typeIndex.Index].RetainFn.Invoke(data);
            else
            {
                CallManagedRetain(typeIndex, data);
            }
        }

        [BurstDiscard]
        private static void CallManagedRetain(TypeIndex typeindex, IntPtr data)
        {
            s_SharedComponentFns_gcDefeat[typeindex.Index].RetainFn.Invoke(data);
        }

        /// <summary>
        /// Used by codegen
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <param name="data">Component instance</param>
        public static void CallIRefCounted_Release(TypeIndex typeIndex, IntPtr data)
        {
            if (IsBursted())
                ((SharedComponentFnPtrs*) Shared_SharedComponentData_FnPtrs.Ref.Data)[typeIndex.Index]
                    .ReleaseFn.Invoke(data);
            else
                CallManagedRelease(typeIndex, data);
        }

        [BurstDiscard]
        private static void CallManagedRelease(TypeIndex typeindex, IntPtr data)
        {
            s_SharedComponentFns_gcDefeat[typeindex.Index].ReleaseFn.Invoke(data);
        }

        class PendingLifecycleCallback
        {
            public Type ComponentType;
            public ComponentLifecycleDelegate OnAddedFn;
            public ComponentLifecycleDelegate OnRemovedFn;
            public bool HasOnAdded;
            public bool HasOnRemoved;
            public bool OnAddedIsBurst;
            public bool OnRemovedIsBurst;
        }

        [NoAutoStaticsCleanup]
        static List<PendingLifecycleCallback> s_PendingLifecycleCallbacks;

        /// <summary>
        /// Used by ILPP-generated code to register component lifecycle callbacks.
        /// Called from generated EarlyInit() methods.
        /// </summary>
        /// <param name="componentType">Component Type</param>
        /// <param name="onAddedFn">OnAdded delegate (or null)</param>
        /// <param name="onRemovedFn">OnRemoved delegate (or null)</param>
        /// <param name="hasOnAdded">Whether OnAdded callback exists</param>
        /// <param name="hasOnRemoved">Whether OnRemoved callback exists</param>
        /// <param name="onAddedIsBurst">Whether OnAdded is Burst-compiled</param>
        /// <param name="onRemovedIsBurst">Whether OnRemoved is Burst-compiled</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [ExcludeFromBurstCompatTesting("Uses managed delegates")]
        public static unsafe void RegisterComponentLifecycleCallback(
            Type componentType,
            ComponentLifecycleDelegate onAddedFn,
            ComponentLifecycleDelegate onRemovedFn,
            bool hasOnAdded,
            bool hasOnRemoved,
            bool onAddedIsBurst,
            bool onRemovedIsBurst)
        {
            if (!s_Initialized)
            {
                s_PendingLifecycleCallbacks ??= new List<PendingLifecycleCallback>();
                s_PendingLifecycleCallbacks.Add(new PendingLifecycleCallback
                {
                    ComponentType = componentType,
                    OnAddedFn = onAddedFn,
                    OnRemovedFn = onRemovedFn,
                    HasOnAdded = hasOnAdded,
                    HasOnRemoved = hasOnRemoved,
                    OnAddedIsBurst = onAddedIsBurst,
                    OnRemovedIsBurst = onRemovedIsBurst
                });
                return;
            }

            RegisterComponentLifecycleCallbackInternal(componentType, onAddedFn, onRemovedFn, hasOnAdded, hasOnRemoved, onAddedIsBurst, onRemovedIsBurst);
        }

        static unsafe void RegisterComponentLifecycleCallbackInternal(
            Type componentType,
            ComponentLifecycleDelegate onAddedFn,
            ComponentLifecycleDelegate onRemovedFn,
            bool hasOnAdded,
            bool hasOnRemoved,
            bool onAddedIsBurst,
            bool onRemovedIsBurst)
        {
            var typeIndex = GetTypeIndex(componentType);
            var index = typeIndex.Index & ClearFlagsMask;

            if (hasOnAdded && onAddedFn != null)
            {
                IntPtr fnPtr;
                if (onAddedIsBurst)
                {
                    fnPtr = BurstCompiler.CompileFunctionPointer(onAddedFn).Value;
                }
                else
                {
                    fnPtr = Marshal.GetFunctionPointerForDelegate(onAddedFn);
                    s_OnAdded_GCDefeat[index] = onAddedFn;
                }

                s_OnAdded_FunctionPointers.Data[index] = new OnAddedDelegateHolder
                {
                    FunctionPointer = (delegate* unmanaged[Cdecl]<Entity*, void*, void>)fnPtr
                };
                s_AnyDebugCallbacksRegistered.Data = true;
            }

            if (hasOnRemoved && onRemovedFn != null)
            {
                IntPtr fnPtr;
                if (onRemovedIsBurst)
                {
                    fnPtr = BurstCompiler.CompileFunctionPointer(onRemovedFn).Value;
                }
                else
                {
                    fnPtr = Marshal.GetFunctionPointerForDelegate(onRemovedFn);
                    s_OnRemoved_GCDefeat[index] = onRemovedFn;
                }

                s_OnRemoved_FunctionPointers.Data[index] = new OnRemovedDelegateHolder
                {
                    FunctionPointer = (delegate* unmanaged[Cdecl]<Entity*, void*, void>)fnPtr
                };
                s_AnyDebugCallbacksRegistered.Data = true;
            }
        }

        /// <summary>
        /// Processes any lifecycle callbacks that were registered before TypeManager was initialized.
        /// Called from EntityManager.Initialize() after TypeManager.Initialize().
        /// </summary>
        [ExcludeFromBurstCompatTesting("Uses managed delegates")]
        public static void InitializePendingLifecycleCallbacks()
        {
            if (s_PendingLifecycleCallbacks == null)
                return;

            foreach (var pending in s_PendingLifecycleCallbacks)
            {
                RegisterComponentLifecycleCallbackInternal(
                    pending.ComponentType,
                    pending.OnAddedFn,
                    pending.OnRemovedFn,
                    pending.HasOnAdded,
                    pending.HasOnRemoved,
                    pending.OnAddedIsBurst,
                    pending.OnRemovedIsBurst);
            }

            s_PendingLifecycleCallbacks = null;
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe delegate* unmanaged[Cdecl]<Entity*, void*, void> GetOnAddedCallback(TypeIndex typeindex)
        {
            return s_OnAdded_FunctionPointers.Data[typeindex.Index & ClearFlagsMask].FunctionPointer;
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe delegate* unmanaged[Cdecl]<Entity*, void*, void> GetOnRemovedCallback(TypeIndex typeindex)
        {
            return s_OnRemoved_FunctionPointers.Data[typeindex.Index & ClearFlagsMask].FunctionPointer;
        }

        /// <summary>
        /// Returns the TypeIndex for a given <seealso cref="TypeInfo.StableTypeHash"/>
        /// </summary>
        /// <param name="stableTypeHash">Component <seealso cref="TypeInfo.StableTypeHash"/></param>
        /// <returns>Component TypeIndex, otherwise <seealso cref="TypeIndex.Null"/></returns>
        [GenerateTestsForBurstCompatibility]
        public static TypeIndex GetTypeIndexFromStableTypeHash(ulong stableTypeHash)
        {
            UnsafeParallelHashMap<ulong, TypeIndex> map = default;
            map.m_Buffer = GetStableTypeHashMapPointer();

            if (map.TryGetValue(stableTypeHash, out var typeIndex))
                return typeIndex;
            return TypeIndex.Null;
        }

        /// <summary>
        /// Used by codegen. Create an instance of a component type from a given buffer of data
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <param name="data">Component instance data</param>
        /// <returns>Boxed component instance</returns>
        public static unsafe object ConstructComponentFromBuffer(TypeIndex typeIndex, void* data)
        {
            var tinfo = GetTypeInfo(typeIndex);
            Type type = GetType(typeIndex);
            object obj = Activator.CreateInstance(type);
            if (data!=null && tinfo.TypeSize != 0)
            {
                var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
                var ptr = ((byte*)handle.AddrOfPinnedObject());
                UnsafeUtility.MemCpy(ptr, data, tinfo.TypeSize);
                handle.Free();
            }

            return obj;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal static void CheckComponentType(Type type)
        {
            int typeCount = 0;
            var interfaces = new Type[]
            {
                typeof(IComponentData),
                typeof(IBufferElementData),
                typeof(ISharedComponentData),
                typeof(TransformRef)
            };

            foreach (Type t in interfaces)
            {
                if (t.IsAssignableFrom(type))
                    ++typeCount;
            }

            if (typeCount > 1)
                throw new ArgumentException($"Component {type} can only implement one of IComponentData, ISharedComponentData and IBufferElementData");
        }

        /// <summary>
        /// Deprecated. Used by codegen. Returns list of all type indices for components who have a WriteGroup on the provided type
        /// </summary>
        /// <param name="typeIndex">Component type index</param>
        /// <returns>List of type indices</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new Type[] { typeof(Entity) })]
        [Obsolete("Write groups are deprecated and will be removed in a future release. (RemovedAfter Entities 6.7)")]
        public static NativeArray<TypeIndex> GetWriteGroupTypes(TypeIndex typeIndex)
        {
            var writeGroups = GetWriteGroups(typeIndex, out var writeGroupCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TypeIndex>(writeGroups, writeGroupCount, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, SharedSafetyHandle.Ref.Data);
            return arr;
        }

        // TODO: Fix our wild alignment requirements for chunk memory (easier said than done)
        /// <summary>
        /// Our alignment calculations for types are taken from the perspective of the alignment of the type _specifically_ when
        /// stored in chunk memory. This means a type's natural alignment may not match the AlignmentInChunk value. Our current scheme is such that
        /// an alignment of 'MaximumSupportedAlignment' is assumed unless the size of the type is smaller than 'MaximumSupportedAlignment' and is a power of 2.
        /// In such cases we use the type size directly, thus if you have a type that naturally aligns to 4 bytes and has a size of 8, the AlignmentInChunk will be 8
        /// as long as 8 is less than 'MaximumSupportedAlignment'.
        /// </summary>
        /// <param name="sizeOfTypeInBytes"></param>
        /// <returns></returns>
        internal static int CalculateAlignmentInChunk(int sizeOfTypeInBytes)
        {
            int alignmentInBytes = MaximumSupportedAlignment;
            if (sizeOfTypeInBytes < alignmentInBytes && CollectionHelper.IsPowerOfTwo(sizeOfTypeInBytes))
                alignmentInBytes = sizeOfTypeInBytes;

            return alignmentInBytes;
        }

        internal class TypeTreeNode
        {
            public Type Type;
            public TypeTreeNode Parent;
            public List<TypeTreeNode> Children;
        }

        private static TypeTreeNode[] BuildTypeTree(List<Type> componentTypes, HashSet<Type> componentTypeSet, Dictionary<Type, int> descendantCountByType)
        {
            var typeTree = new Dictionary<Type, TypeTreeNode>(MaximumTypesCount);

            TypeTreeNode Add(Type type)
            {
                var newNode = new TypeTreeNode
                {
                    Type = type,
                    Children = null,
                    Parent = null
                };

                // Find base node if any and add as child
                var baseNode = LookupOrAdd(type.BaseType);
                if (baseNode != null)
                {
                    if(baseNode.Children == null)
                        baseNode.Children = new List<TypeTreeNode>();

                    baseNode.Children.Add(newNode);
                    newNode.Parent = baseNode;
                }

                typeTree[type] = newNode;

                return newNode;
            }

            TypeTreeNode LookupOrAdd(Type type)
            {
                if (type == null)
                    return null;

                if (!componentTypeSet.Contains(type))
                    return null;

                if (typeTree.ContainsKey(type))
                    return typeTree[type];

                return Add(type);
            }

            int DepthFirst(TypeTreeNode node)
            {
                int descendantCount = 1;
                if (node.Children != null)
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        descendantCount += DepthFirst(node.Children[i]);
                    }
                }

                // Take one off to not include itself in the count
                descendantCountByType[node.Type] = descendantCount - 1;

                return descendantCount;
            }

            for (int i = 0; i < componentTypes.Count; i++)
            {
                LookupOrAdd(componentTypes[i]);
            }

            // Depth First traverse nodes
            var typeTreeNodes = new TypeTreeNode[typeTree.Count];
            typeTree.Values.CopyTo(typeTreeNodes, 0);

            for (int i = 0; i < typeTreeNodes.Length; i++)
            {
                var node = typeTreeNodes[i];
                if (node.Parent != null)
                    continue;

                DepthFirst(node);
            }

            return typeTreeNodes;
        }


        private static bool IsSupportedComponentType(Type type)
        {
            return typeof(IComponentData).IsAssignableFrom(type)
                || typeof(ISharedComponentData).IsAssignableFrom(type)
                || typeof(IBufferElementData).IsAssignableFrom(type)
                || typeof(TransformRef).IsAssignableFrom(type)
                ;
        }

        static void AddUnityEngineObjectTypeToListIfSupported(HashSet<Type> componentTypeSet, Type type)
        {
            if (IsSupportedUnityEngineObjectType(type))
                componentTypeSet.Add(type);
        }

        static bool IsSupportedUnityEngineObjectType(Type type)
        {
            if (!UnityEngineObjectType.IsAssignableFrom(type))
                return false;
            if (type.ContainsGenericParameters)
                return false;
            if (type.IsAbstract && !CoreModuleLightShim.Hooks.ComponentType.IsAssignableFrom(type))
                return false;
            return true;
        }

        static bool IsInstantiableComponentType(Type type)
        {
            if (type.IsAbstract)
                return false;

            if (!type.IsValueType && typeof(IComponentData).IsAssignableFrom(type))
                throw new ArgumentException($"Type '{type.FullName}' inherits from IComponentData but is a class. " +
                    $"Class-based IComponentData is no longer supported. " +
                    $"Change it to a struct, using UnityObjectRef<T> for any UnityEngine.Object references.");

            if (!type.IsValueType)
                return false;

            // Don't register open generics here.  It's an open question
            // on whether we should support them for components at all,
            // as with them we can't ever see a full set of component types
            // in use.
            if (type.ContainsGenericParameters)
                return false;

            if (Attribute.IsDefined(type, typeof(DisableAutoTypeRegistrationAttribute)))
                return false;

            return true;
        }
        static void AddComponentTypeToListIfSupported(HashSet<Type> typeSet, Type type)
        {
            if (!IsInstantiableComponentType(type))
                return;

            typeSet.Add(type);
        }
        [NoAutoStaticsCleanup]
        static readonly EntitiesProfilerMarker k_InitializeAllComponentTypesMarker = new EntitiesProfilerMarker(nameof(InitializeAllComponentTypes));

        static void InitializeAllComponentTypes()
        {
            try
            {
                k_InitializeAllComponentTypesMarker.Begin();
                var combinedComponentTypeSet = new HashSet<Type>();
                var assemblies = CoreModuleLightShim.Hooks.GetLoadedAssemblies();

                AddComponentTypeToListIfSupported(combinedComponentTypeSet, typeof(TransformRef));

                UnityEngineObjectType = CoreModuleLightShim.Hooks.UnityObjectType;


                foreach (var assembly in assemblies)
                {
                    IsAssemblyReferencingEntitiesOrUnityEngine(assembly, out var isAssemblyReferencingEntities,
                        out var isAssemblyReferencingUnityEngine);

                    if (!isAssemblyReferencingEntities)
                        continue;

                    if (assembly.IsDefined(typeof(DisableAutoTypeRegistrationAttribute)))
                        continue;

                    Type[] assemblyTypes;
                    try
                    {
#pragma warning disable RS0030 // Assembly.GetTypes: Entities discovers component and system types by scanning assemblies.
                        assemblyTypes = assembly.GetTypes();
#pragma warning restore RS0030
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        assemblyTypes = Array.FindAll(e.Types, t => t != null);
                        Scripting.LowLevel.Debug.LogWarning($"TypeManager failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : CoreModuleLightShim.Hooks.GetLoadedAssemblyPath(assembly))}");
                    }

                    if (isAssemblyReferencingUnityEngine)
                    {
                        foreach (var type in assemblyTypes)
                        {
                            AddUnityEngineObjectTypeToListIfSupported(combinedComponentTypeSet, type);
                        }
                    }

                    // Register ComponentData types
                    if (isAssemblyReferencingEntities)
                    {
                        foreach (var type in assemblyTypes)
                        {
                            if (IsSupportedComponentType(type))
                                AddComponentTypeToListIfSupported(combinedComponentTypeSet, type);
                        }
                    }

                    // Register ComponentData concrete generics
                    foreach (var registerGenericComponentTypeAttribute in assembly.GetCustomAttributes<RegisterGenericComponentTypeAttribute>())
                    {
                        var type = registerGenericComponentTypeAttribute.ConcreteType;

                        if (IsSupportedComponentType(type))
                            combinedComponentTypeSet.Add(type);
                    }

                    // Register UnityEngine components
                    foreach (var unityEngineType in assembly.GetCustomAttributes<RegisterUnityEngineComponentTypeAttribute>())
                    {
                        var type = unityEngineType.ConcreteType;

                        if (IsSupportedComponentType(type) || IsSupportedUnityEngineObjectType(type))
                            combinedComponentTypeSet.Add(type);
                    }
            }


                var componentTypeCount = combinedComponentTypeSet.Count;

                //Types to process by reflection include types
                //that have variable size based on bitness and so will have wrong
                //info on 32 bit platforms.


                var typesToProcessByReflection = new List<Type>(combinedComponentTypeSet);

                var descendantCountByType = new Dictionary<Type, int>();


                var typesToReprocess = Array.Empty<Type>();
                var combinedComponentTypes = new List<Type>(combinedComponentTypeSet);
                typesToProcessByReflection.AddRange(typesToReprocess);


                //at this point, componentTypes needs to be the combined set, and same with componentTypeSet

                var typeTreeNodes = BuildTypeTree(combinedComponentTypes, combinedComponentTypeSet, descendantCountByType);

                /*
                 * In order to call this function, we need the descendant counts to be filled in properly already.
                 * so we have to save the list of stuff that we're going to do by reflection,
                 * call the other thing to make the integrated descendant info,
                 * and then come back and call this on the limited list of types that we have to do by reflection.
                 */
                AddAllComponentTypes(typesToProcessByReflection, descendantCountByType);
                /*
                 * now that type indices have been built, we can use them as keys in our hash map
                 */
                GatherSharedComponentMethods(s_ManagedTypeToIndex);

                foreach (var typeNode in typeTreeNodes)
                {
                    var typeIndex = GetTypeIndex(typeNode.Type);
                    if (typeNode.Parent != null)
                        s_DescendantMap[typeIndex] = GetTypeIndex(typeNode.Parent.Type);
                }

                // All component types (ILPP and reflection registered) now have their final TypeIndex, so the
                // write group table can be built directly from the managed Type -> TypeIndex mapping.
                InitializeWriteGroups();
            }
            finally
            {
                // If any components have errors, ensure we log the errors and tell the user as continuing to use invalid components
                // could lead to crashes due to the unsafe nature of how invoke burst instance methods from generic code paths
                if (s_FailedTypeBuildException.Count > 0)
                {
                    throw new ArgumentException("TypeManager initialization failed. Please fix all exceptions logged above before continuing.");
                }

                k_InitializeAllComponentTypesMarker.End();
            }
        }

        static IEnumerable<Type> GetRefCountedTypes(Dictionary<Type, TypeIndex> indexByType)
        {
            return EditorLightShim.Hooks.GetTypesDerivedFrom(typeof(IRefCounted));
        }

        private static void GatherSharedComponentMethods(Dictionary<Type, TypeIndex> indexByType)
        {
            foreach (var type in GetRefCountedTypes(indexByType))
            {

                var indexInTypeArray = indexByType[type];

                var methodInfo = type.GetMethod("__codegen__Retain", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                IRefCounted.RefCountDelegate retainDelegate = (IRefCounted.RefCountDelegate)methodInfo
                    .CreateDelegate(typeof(IRefCounted.RefCountDelegate));
                SetIRefCounted_RetainFn(indexInTypeArray,
                    retainDelegate,
                    Attribute.IsDefined(methodInfo, typeof(BurstCompileAttribute)));

                methodInfo = type.GetMethod("__codegen__Release", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                IRefCounted.RefCountDelegate releaseDelegate = (IRefCounted.RefCountDelegate)methodInfo
                    .CreateDelegate(typeof(IRefCounted.RefCountDelegate));
                SetIRefCounted_ReleaseFn(indexInTypeArray,
                    releaseDelegate,
                    Attribute.IsDefined(methodInfo, typeof(BurstCompileAttribute)));
            }

            foreach (var type in indexByType.Keys)
            {
                var indexInTypeArray = indexByType[type];


                if (!typeof(ISharedComponentData).IsAssignableFrom(type) ||
                    !Array.Exists(type.GetInterfaces(), i => i.Name == "IEquatable`1"))
                    continue;

                var methodInfo = type.GetMethod("__codegen__Equals", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] {typeof(void*), typeof(void*)}, null);

                var equalsDelegate = methodInfo.CreateDelegate(typeof(FastEquality.TypeInfo.CompareEqualDelegate));
                SetIEquatable_EqualsFn(indexInTypeArray,
                    equalsDelegate,
                    Attribute.IsDefined(methodInfo, typeof(BurstCompileAttribute)));

                methodInfo = type.GetMethod("__codegen__GetHashCode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (methodInfo != null)
                {
                    var ghcDelegate = methodInfo.CreateDelegate(typeof(FastEquality.TypeInfo.GetHashCodeDelegate));
                    SetIEquatable_GetHashCodeFn(indexInTypeArray,
                        ghcDelegate,
                        Attribute.IsDefined(methodInfo, typeof(BurstCompileAttribute)));
                }

            }
        }

        internal class BuildComponentCache
        {
            public Dictionary<Type, ulong> TypeHashCache;
            public Dictionary<Type, bool> NestedNativeContainerCache;
            public Dictionary<Type, EntityRemapUtility.EntityBlobRefResult> HasEntityOrBlobAssetReferenceCache;
            public Dictionary<Type, List<FastEquality.LayoutInfo>> FastEqualityLayoutInfoCache;
            public Dictionary<Type, (bool, bool)> ChunkSerializableCache;
            public HashSet<Type> AllowedComponentCache;
            public HashSet<Type> CalculateFieldOffsetsUnmanagedCache;

            public BuildComponentCache(int initialCapacity = 0)
            {
                TypeHashCache = new Dictionary<Type, ulong>(initialCapacity);
                NestedNativeContainerCache = new Dictionary<Type, bool>(initialCapacity);
                HasEntityOrBlobAssetReferenceCache = new Dictionary<Type, EntityRemapUtility.EntityBlobRefResult>(initialCapacity);
                FastEqualityLayoutInfoCache = new Dictionary<Type, List<FastEquality.LayoutInfo>>(initialCapacity);
                AllowedComponentCache = new HashSet<Type>(initialCapacity);
                ChunkSerializableCache = new Dictionary<Type, (bool isChunkSerializable, bool hasChunkSerializableAttribute)>();
                CalculateFieldOffsetsUnmanagedCache = new HashSet<Type>(initialCapacity);
            }
        }

        private static void AddAllComponentTypes(List<Type> componentTypes, Dictionary<Type, int> descendantCountByType)
        {
            BuildComponentCache caches = new BuildComponentCache(componentTypes.Count);

            for (int i = 0; i < componentTypes.Count; i++)
            {
                var type = componentTypes[i];
                try
                {
                    /*
                     * We used to check here if FindTypeIndex(type) returned null, and error if it didn't, since it was a
                     * duplicate.
                     *
                     * However, we are now actually okay if this type already exists, because we intend to call this sometimes with some
                     * types that already have type indices. This is because when running on 32-bit, the type info we
                     * computed via codegen may be wrong if the type contains pointers. So if we detect that,
                     * we add that type to a list to be reprocessed and have the correct typeinfo added.
                     *
                     * And, it's simpler to just reprocess it and append the correct typeinfo at the end than it would be
                     * to go patch the typeinfo where it is.
                     */
                    var typeInfo = BuildComponentType(type, caches);

                    AddTypeInfoToTables(type, typeInfo, type.FullName);
                    CoreModuleLightShim.Hooks.RegisterCompanionComponentTypeIfWrapper(type, typeInfo.TypeIndex);
                }
                catch (Exception e)
                {
                    if (type != null)
                    {
                        // Explicitly clear the shared type index.
                        // This is a workaround for a bug in burst where the shared static doesn't get reset to zero on domain reload.
                        // Can be removed once it is fixed in burst.
                        SharedTypeIndex.Get(type) = TypeIndex.Null;
                        s_FailedTypeBuildException[type] = e;
                    }

                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// A single (target, writer) edge of the write group graph: <see cref="Writer"/> has a
        /// <see cref="WriteGroupAttribute"/> naming <see cref="Target"/>. Sorted by target then writer so that
        /// writers end up grouped per target and duplicates are adjacent.
        /// </summary>
        struct WriteGroupEdge : IComparable<WriteGroupEdge>
        {
            public int Target;       // TypeIndex.Index of the write group target
            public TypeIndex Writer; // Full TypeIndex (with flags) of the component carrying the attribute

            public int CompareTo(WriteGroupEdge other)
            {
                var c = Target.CompareTo(other.Target);
                return c != 0 ? c : Writer.Value.CompareTo(other.Writer.Value);
            }
        }

        /// <summary>
        /// Builds the write group table (<see cref="s_WriteGroupOffsets"/> / <see cref="s_WriteGroupList"/>) from the
        /// <see cref="WriteGroupAttribute"/>s on every registered component type.
        /// </summary>
        /// <remarks>
        /// This runs once, after all component types have been registered (both from ILPP type registries and via
        /// reflection), so it only needs the final Type -> TypeIndex mapping and does not depend on the order in which
        /// type indices were assigned or on which assembly a type or its write group target came from.
        /// </remarks>
        static void InitializeWriteGroups()
        {
#pragma warning disable 0618 // Write groups are obsolete. They will be removed in a future release.
            var attributeType = typeof(WriteGroupAttribute);
            var edges = new NativeList<WriteGroupEdge>(64, Allocator.Temp);

            foreach (var kvp in s_ManagedTypeToIndex)
            {
                var type = kvp.Key;
                if (!type.IsDefined(attributeType, false))
                    continue;

                foreach (WriteGroupAttribute attr in type.GetCustomAttributes(attributeType, false))
                {
                    if (attr.TargetType == null || !s_ManagedTypeToIndex.TryGetValue(attr.TargetType, out var targetTypeIndex))
                    {
                        // If the target failed to build, that error has already been logged and will fail initialization.
                        if (attr.TargetType != null && s_FailedTypeBuildException.ContainsKey(attr.TargetType))
                            continue;
                        throw new ArgumentException($"[WriteGroup(typeof({attr.TargetType}))] on {type}: the target type is not a registered component type.");
                    }

                    edges.Add(new WriteGroupEdge { Target = targetTypeIndex.Index, Writer = kvp.Value });
                }
            }
#pragma warning restore 0618

            edges.Sort();

            // CSR layout: offsets[t] is the index of the first writer of target t in s_WriteGroupList, and
            // offsets[t + 1] is one past the last. Count into offsets[t + 1], then prefix-sum to turn counts into offsets.
            var offsets = (int*)s_WriteGroupOffsets.GetUnsafePtr();
            UnsafeUtility.MemClear(offsets, s_WriteGroupOffsets.Length * sizeof(int));
            s_WriteGroupList.Clear();

            for (int i = 0; i < edges.Length; ++i)
            {
                var edge = edges[i];
                if (i > 0 && edge.CompareTo(edges[i - 1]) == 0)
                    continue; // same attribute declared more than once

                s_WriteGroupList.Add(edge.Writer);
                offsets[edge.Target + 1]++;
            }

            for (int t = 1; t < s_WriteGroupOffsets.Length; ++t)
                offsets[t] += offsets[t - 1];

            edges.Dispose();
        }

        /// <summary>
        /// Determines if an assembly refers to Unity.Entities.dll
        /// </summary>
        /// <param name="assembly">An Assembly</param>
        /// <returns>Returns if assembly refers to Unity.Entities.dll</returns>
        public static bool IsAssemblyReferencingEntities(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            if (IsEntitiesAssemblyName(assemblyName))
                return true;

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var referenced in referencedAssemblies)
                if (IsEntitiesReference(assemblyName, referenced.Name))
                    return true;
            return false;
        }

        const string k_EntitiesModuleAssemblyName = "UnityEngine.EntitiesModule";

        static bool IsEntitiesAssemblyName(string assemblyName) =>
            assemblyName == k_EntitiesModuleAssemblyName || assemblyName.Contains("Unity.Entities");

        // Other engine modules reference the Entities module without declaring Entities types, so only script
        // assemblies count as referencing it.
        static bool IsEntitiesReference(string assemblyName, string referencedAssemblyName) =>
            referencedAssemblyName.Contains("Unity.Entities") ||
            (referencedAssemblyName == k_EntitiesModuleAssemblyName && !assemblyName.StartsWith("UnityEngine.") && !assemblyName.StartsWith("UnityEditor."));

        internal static void IsAssemblyReferencingEntitiesOrUnityEngine(Assembly assembly, out bool referencesEntities, out bool referencesUnityEngine)
        {
            const string kUnityEngineAssemblyName = "UnityEngine";
            var assemblyName = assembly.GetName().Name;

            referencesEntities = false;
            referencesUnityEngine = false;

            if (IsEntitiesAssemblyName(assemblyName))
                referencesEntities = true;

            if (assemblyName.Contains(kUnityEngineAssemblyName))
                referencesUnityEngine = true;

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var referencedAssembly in referencedAssemblies)
            {
                var referencedAssemblyName = referencedAssembly.Name;

                if (!referencesEntities && IsEntitiesReference(assemblyName, referencedAssemblyName))
                    referencesEntities = true;
                if (!referencesUnityEngine && referencedAssemblyName.Contains(kUnityEngineAssemblyName))
                    referencesUnityEngine = true;
            }
        }

        internal static void CheckIsAllowedAsComponentData(Type type, string baseTypeDesc, HashSet<Type> allowedComponentCache = null)
        {
            if (UnsafeUtility.IsUnmanaged(type))
                return;

            // it can't be used -- so we expect this to find and throw
            ThrowOnDisallowedComponentData(type, type, baseTypeDesc, allowedComponentCache);

            // if something went wrong and the above didn't throw, then throw
            throw new ArgumentException($"{type} cannot be used as component data for unknown reasons (BUG)");
        }

        internal static void CheckTypeOverrideAndUpdateHasReferences(Type type, Dictionary<Type, EntityRemapUtility.EntityBlobRefResult> cache, ref bool hasEntityReferences, ref bool hasBlobReferences, ref bool hasUnityObjectReferences)
        {
            // Components can opt out of certain patching operations as an optimization.
            var typeOverride = type.GetCustomAttribute<TypeOverridesAttribute>(true);
            if (typeOverride != null)
            {
                if (typeOverride.HasNoEntityReferences)
                    hasEntityReferences = false;
                if (typeOverride.HasNoBlobReferences)
                    hasBlobReferences = false;
                if (typeOverride.HasNoUnityObjectReferences)
                    hasUnityObjectReferences = false;
            }
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal static void ThrowOnDisallowedComponentData(Type type, Type baseType, string baseTypeDesc, HashSet<Type> cache)
        {
            if (type.IsPrimitive)
                return;

            // if it's a pointer, we assume you know what you're doing
            if (type.IsPointer)
                return;

            // The cache check is lower than the above early outs since checking the cache is more expensive
            // than the above checks
            if (cache.Contains(type))
                return;

            cache.Add(type);

            if (!type.IsValueType || type.IsByRef || type.IsClass || type.IsInterface || type.IsArray)
            {
                if (type == baseType)
                    throw new ArgumentException(
                        $"{type} is a {baseTypeDesc} and thus must be a struct containing only primitive or blittable members.");

                throw new ArgumentException($"{baseType} contains a field of {type}, which is neither primitive nor blittable.");
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ThrowOnDisallowedComponentData(field.FieldType, baseType, baseTypeDesc, cache);
            }
        }

        internal static bool DoesComponentContainNativeContainer(Type type, Type baseType, Dictionary<Type, bool> nestedContainerCache)
        {
            if (type.IsPrimitive)
                return false;

            if (type == typeof(TransformRef))
            {
                // TransformRef is a [NativeContainer] so that an individual one can be passed into a job and its
                // safety handles will be patched. However, it also needs to work as a TypeIndex for TransformTypeHandle
                // and TransformLookup, which trigger a nested container error when scheduled. Since TransformRefs
                // created from TransformAccessor(TransformTypeHandle) and TransformLookup inherit the safety handles
                // from their respective container, they will be valid, patched handles in a job. So for now, we
                // manually prevent TransformRef from generating a nested container warning.
                //
                // TODO DOTS-10299: Collections, like NativeArray<TransformRef>, will not properly patch the
                //  TransformRef safety handles if passed to a job, so those need to be avoided until we find a way to
                //  prevent them.
                return false;
            }

            if (nestedContainerCache.TryGetValue(type, out bool hasContainer))
                return hasContainer;

            if (type.IsArray)
            {
                hasContainer = false;
                var elementType = type.GetElementType();
                if (elementType != null)
                    hasContainer = DoesComponentContainNativeContainer(elementType, baseType, nestedContainerCache);
                // We may have inserted the ElementType as part of the recursive call above if the type contains
                // a circular reference (which is not valid for ValueTypes but is possible for ReferenceTypes).
                // Since we don't know the answer for hasContainer, and it can't have changed since the recursive
                // call we use TryAdd, and don't need to do a Contains and then update the value; TryAdd is good enough.
                nestedContainerCache.TryAdd(type, hasContainer);
                return hasContainer;
            }

            hasContainer = Attribute.IsDefined(type, typeof(NativeContainerAttribute));
            nestedContainerCache.Add(type, hasContainer);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                hasContainer |= DoesComponentContainNativeContainer(field.FieldType, baseType, nestedContainerCache);
            }

            nestedContainerCache[type] = hasContainer;
            return hasContainer;
        }

        // A component type is "chunk serializable" if it meets the following rules:
        // - It is decorated with [ChunkSerializable] attribute (this is an override for all other rules)
        // - The type is blittable AND
        // - The type does not contain any pointer types (including IntPtr) AND
        // - If the type is a shared component, it does not contain an entity reference
        private static bool IsComponentChunkSerializable(Type type, TypeCategory category, bool hasEntityReference, BuildComponentCache cache)
        {
            var isSerializable = IsTypeValidForSerialization(type, cache.ChunkSerializableCache);

            // Shared Components are expected to be handled specially when serializing and are not required to be blittable.
            // They cannot contain an entity reference today as they are not patched however for unmanaged components
            // we should be able to correct this behaviour DOTS-7613
            if (!isSerializable.hasChunkSerializableAttribute && category == TypeCategory.ISharedComponentData && hasEntityReference)
            {
                isSerializable.isSerializable = false;
            }

            return isSerializable.isSerializable;
        }


        // True when a component is valid to using in world serialization. A component IsSerializable when it is valid to blit
        // the data across storage media. Thus components containing pointers have an IsSerializable of false as the component
        // is blittable but no longer valid upon deserialization.
        private static (bool isSerializable, bool hasChunkSerializableAttribute) IsTypeValidForSerialization(Type type, Dictionary<Type, (bool isChunkSerializable, bool hasChunkSerializableAttribute)> cache)
        {
            var result = (false, false);
            if (cache.TryGetValue(type, out result))
                return result;

            if (Attribute.IsDefined(type, typeof(ChunkSerializableAttribute)))
            {
                result = (true, true);
                cache.Add(type, result);
                return result;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.IsStatic)
                    continue;

                if (field.FieldType.IsPointer || (field.FieldType == typeof(UIntPtr) || field.FieldType == typeof(IntPtr)))
                {
                    return result;
                }
                else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                {
                    if (!IsTypeValidForSerialization(field.FieldType, cache).isSerializable)
                    {
                        cache.Add(type, result);
                        return result;
                    }
                }
            }

            result = (true, false);
            cache.Add(type, result);
            return result;
        }

        // https://stackoverflow.com/a/27851610
        static bool IsZeroSizeStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive &&
                Array.TrueForAll(t.GetFields((BindingFlags)0x34), fi => IsZeroSizeStruct(fi.FieldType));
        }

        internal static TypeIndex GetOrCreateTypeIndexUnsafe(Type type)
        {
            var typeInfo = BuildComponentType(type, null);
            AddTypeInfoToTables(type, typeInfo, type.FullName);
            var ret = GetTypeIndex(type);

            if (CoreModuleLightShim.Hooks.UnityObjectType.IsAssignableFrom(type.BaseType))
            {
                if (!TryGetTypeIndex(type.BaseType, out var parentIndex))
                {
                    s_DescendantMap[ret] = GetOrCreateTypeIndexUnsafe(type.BaseType);
                }
                else
                {
                    s_DescendantMap[ret] = parentIndex;
                }
            }

            return ret;
        }

        internal static TypeIndex GetOrCreateTypeIndex(Type type)
        {

            var hadit = TryGetTypeIndex(type, out var ret);
            if (!hadit)
            {
                if (IsSupportedUnityEngineObjectType(type))
                {
                    //we do not actually want to shut down shared statics, since
                    //adding a unityengine object will never cause anything to reallocate.
                    ret = GetOrCreateTypeIndexUnsafe(type);
                }
                else
                {
                    // this will throw, which is what we want in this case.
                    return GetTypeIndex(type);
                }

            }
            return ret;
        }

        private static ulong ComputeBloomFilterMask(ulong typeHash)
        {
            // This function effectively computes k different hashes from the input typeHash to a single bit in the output
            // mask. If k is too low, the odds increase that multiple types will have the same bloomFilterMask. If k is
            // too high, the odds increase that bitwise-or'ing multiple masks together will have so many bits set that a
            // missing type is "hidden". Either way, the net result is a higher false positive rate, reducing
            // the effectiveness of the Bloom filter early-out check.
            // k=5 seems to strike a reasonable balance, given the number of unique component types and the number
            // of types per archetype in typical DOTS applications.
            const int k = 5;
            const int maxShift = 8 * sizeof(ulong);
            uint seed = (uint)((typeHash & 0xFFFFFFFF) ^ (typeHash >> 32));
            var rng = new Unity.Mathematics.Random(seed != 0 ? seed : 17);
            ulong mask = 0;
            for(int i = 0; i < k; i++)
            {
                mask |= 1UL << rng.NextInt(maxShift);
            }
            return mask;
        }

        internal static TypeInfo BuildComponentType(Type type, BuildComponentCache caches)
        {
            if (caches == null) caches = new BuildComponentCache();
            if (!AtomicSafetyHandle.IsDefaultValue(SharedSafetyHandle.Ref.Data) &&
                (UnityEngineObjectType == null || !UnityEngineObjectType.IsAssignableFrom(type)))
            {
                // Adding stuff to the native lists whose pointers are cached by Burst statics cause undefined behavior.
                throw new InvalidOperationException(
                    $"Cannot call {nameof(BuildComponentType)} on {type} when the shared statics are initialized. Please ensure types are built before the type manager initialization completes, or wrap this call in a {nameof(ShutdownSharedStatics)} / {nameof(InitializeSharedStatics)} pair.");
            }

            CheckComponentType(type);

            var sizeInChunk = 0;
            TypeCategory category;
            int bufferCapacity = -1;
            var memoryOrdering = TypeHash.CalculateMemoryOrdering(type, out var hasCustomMemoryOrder, caches.TypeHashCache);
            // The stable type hash is the same as the memory order if the user hasn't provided a custom memory ordering
            var stableTypeHash = !hasCustomMemoryOrder ? memoryOrdering : TypeHash.CalculateStableTypeHash(type, null, caches.TypeHashCache);
            var bloomFilterMask = ComputeBloomFilterMask(stableTypeHash);
            bool isManaged = type.IsClass;
            var isRefCounted = typeof(IRefCounted).IsAssignableFrom(type);
            var maxChunkCapacity = MaximumChunkCapacity;
            var valueTypeSize = 0;

            var maxCapacityAttribute = type.GetCustomAttribute<MaximumChunkCapacityAttribute>();
            if (maxCapacityAttribute != null && maxCapacityAttribute.Capacity < maxChunkCapacity)
                maxChunkCapacity = maxCapacityAttribute.Capacity;

            int entityOffsetIndex = s_EntityOffsetList.Length;
            int blobAssetRefOffsetIndex = s_BlobAssetRefOffsetList.Length;
            int weakAssetRefOffsetIndex = s_WeakAssetRefOffsetList.Length;
            int unityObjectRefOffsetIndex = s_UnityObjectRefOffsetList.Length;

            int elementSize = 0;
            int alignmentInBytes = 0;
            if (type.IsInterface)
                throw new ArgumentException($"{type} is an interface. It must be a concrete type.");
            bool hasNativeContainer = !isManaged && DoesComponentContainNativeContainer(type, type, caches.NestedNativeContainerCache);
            bool hasOnAddedCallback = false;
            bool hasOnRemovedCallback = false;
            bool hasEntityReferences = false;
            bool hasBlobReferences = false;
            bool hasWeakAssetReferences = false;
            bool hasUnityObjectRefs = false;

            if (typeof(IComponentData).IsAssignableFrom(type) && !isManaged)
            {
                CheckIsAllowedAsComponentData(type, nameof(IComponentData), caches.AllowedComponentCache);

                category = TypeCategory.ComponentData;

                valueTypeSize = UnsafeUtility.SizeOf(type);
                alignmentInBytes = CalculateAlignmentInChunk(valueTypeSize);

                if (TypeManager.IsZeroSizeStruct(type))
                    sizeInChunk = 0;
                else
                    sizeInChunk = valueTypeSize;

                if (typeof(IDebugOnAdded).IsAssignableFrom(type))
                {
                    hasOnAddedCallback = true;
                }

                if (typeof(IDebugOnRemoved).IsAssignableFrom(type))
                {
                    hasOnRemovedCallback = true;
                }

                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(type, out hasEntityReferences, out hasBlobReferences, out hasWeakAssetReferences, out hasUnityObjectRefs, ref s_EntityOffsetList, ref s_BlobAssetRefOffsetList, ref s_WeakAssetRefOffsetList, ref s_UnityObjectRefOffsetList, caches.CalculateFieldOffsetsUnmanagedCache);
            }
            else if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
                CheckIsAllowedAsComponentData(type, nameof(IBufferElementData), caches.AllowedComponentCache);

                category = TypeCategory.BufferData;

                valueTypeSize = UnsafeUtility.SizeOf(type);
                // TODO: Implement UnsafeUtility.AlignOf(type)
                alignmentInBytes = CalculateAlignmentInChunk(valueTypeSize);

                elementSize = valueTypeSize;

                var capacityAttribute = (InternalBufferCapacityAttribute)type.GetCustomAttribute(typeof(InternalBufferCapacityAttribute));
                if (capacityAttribute != null)
                    bufferCapacity = capacityAttribute.Capacity;
                else
                    bufferCapacity = DefaultBufferCapacityNumerator / elementSize; // Rather than 2*cachelinesize, to make it cross platform deterministic

                sizeInChunk = sizeof(BufferHeader) + bufferCapacity * elementSize;
                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(type, out hasEntityReferences, out hasBlobReferences, out hasWeakAssetReferences,
                    out hasUnityObjectRefs, ref s_EntityOffsetList, ref s_BlobAssetRefOffsetList, ref s_WeakAssetRefOffsetList, ref s_UnityObjectRefOffsetList, caches.CalculateFieldOffsetsUnmanagedCache);
            }
            else if (typeof(TransformRef) == type)
            {
                // Transform data is stored in the chunk as TransformUnion.
                // TransformRef is the public interface.
                category = TypeCategory.TransformData;
                valueTypeSize = sizeof(TransformUnion);
                alignmentInBytes = CalculateAlignmentInChunk(valueTypeSize);

                sizeInChunk = valueTypeSize;

                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(type, out hasEntityReferences, out hasBlobReferences, out hasWeakAssetReferences,
                    out hasUnityObjectRefs, ref s_EntityOffsetList, ref s_BlobAssetRefOffsetList, ref s_WeakAssetRefOffsetList, ref s_UnityObjectRefOffsetList, caches.CalculateFieldOffsetsUnmanagedCache);
            }
            else if (typeof(ISharedComponentData).IsAssignableFrom(type))
            {
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an ISharedComponentData, and thus must be a struct.");
                hasNativeContainer = DoesComponentContainNativeContainer(type, type, caches.NestedNativeContainerCache);
                valueTypeSize = UnsafeUtility.SizeOf(type);

                category = TypeCategory.ISharedComponentData;

                if (!UnsafeUtility.IsUnmanaged(type))
                    throw new ArgumentException($"Type '{type.FullName}' implements ISharedComponentData but contains managed fields. " +
                        $"Shared components with managed fields are no longer supported. " +
                        $"Make it unmanaged, using UnityObjectRef<T> for any UnityEngine.Object references.");

                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(type, out hasEntityReferences, out hasBlobReferences, out hasWeakAssetReferences, out hasUnityObjectRefs, ref s_EntityOffsetList, ref s_BlobAssetRefOffsetList, ref s_WeakAssetRefOffsetList, ref s_UnityObjectRefOffsetList, caches.CalculateFieldOffsetsUnmanagedCache);
            }
            else if (type.IsClass)
            {
                category = TypeCategory.UnityEngineObject;
                sizeInChunk = sizeof(int);
                alignmentInBytes = sizeof(int);
                hasEntityReferences = false;
                hasBlobReferences = false;
                hasUnityObjectRefs = false;

                if (UnityEngineObjectType == null)
                    throw new ArgumentException(
                        $"{type} cannot be used from EntityManager. If it inherits UnityEngine.Component, you must first register TypeManager.UnityEngineObjectType or include the Unity.Entities.Hybrid assembly in your build.");
                if (!UnityEngineObjectType.IsAssignableFrom(type))
                    throw new ArgumentException($"{type} must inherit {UnityEngineObjectType}.");
            }
            else
            {
                throw new ArgumentException($"{type} is not a valid component.");
            }

            // If the type explicitly overrides entity/blob reference attributes account for that now
            CheckTypeOverrideAndUpdateHasReferences(type, caches.HasEntityOrBlobAssetReferenceCache, ref hasEntityReferences, ref hasBlobReferences, ref hasUnityObjectRefs);

            int entityOffsetCount = hasEntityReferences ? s_EntityOffsetList.Length - entityOffsetIndex : 0;
            int blobAssetRefOffsetCount = hasBlobReferences ? s_BlobAssetRefOffsetList.Length - blobAssetRefOffsetIndex : 0;
            int unityObjectRefOffsetCount = hasUnityObjectRefs ? s_UnityObjectRefOffsetList.Length - unityObjectRefOffsetIndex : 0;
            int weakAssetRefOffsetCount =  s_WeakAssetRefOffsetList.Length - weakAssetRefOffsetIndex;

            // Cleanup shared components are also considered cleanup components
            bool isCleanupSharedComponent = typeof(ICleanupSharedComponentData).IsAssignableFrom(type);
            bool isCleanupBufferElement = typeof(ICleanupBufferElementData).IsAssignableFrom(type);
            bool isCleanupComponent = isCleanupSharedComponent || isCleanupBufferElement || typeof(ICleanupComponentData).IsAssignableFrom(type);

            bool isEnableableComponent = typeof(IEnableableComponent).IsAssignableFrom(type);
            if (isEnableableComponent)
            {
                if (!(category == TypeCategory.ComponentData || category == TypeCategory.BufferData) || isCleanupComponent)
                    throw new ArgumentException($"IEnableableComponent is not supported for type {type}. Only IComponentData and IBufferElementData can be disabled. Cleanup components are not supported.");
            }

            bool isTemporaryBakingType = Attribute.IsDefined(type, typeof(TemporaryBakingTypeAttribute));
            bool isBakingOnlyType = Attribute.IsDefined(type, typeof(BakingTypeAttribute));
            var isIEquatable = Array.Exists(type.GetInterfaces(), i => i.Name.Contains("IEquatable"));
            var isChunkSerializable = IsComponentChunkSerializable(type, category, hasEntityReferences, caches);

            var typeIndex = CoreModuleLightShim.Hooks.GetComponentTypeId(type);
            if (typeIndex < 0)
                throw new ArgumentException($"Can't find RuntimeTypeIndex for type {type}");

            AddFastEqualityInfo(typeIndex, type, category == TypeCategory.UnityEngineObject, caches.FastEqualityLayoutInfoCache);

            if (typeIndex != 0)
            {
                if (sizeInChunk == 0)
                    typeIndex |= ZeroSizeInChunkTypeFlag;

                if (category == TypeCategory.TransformData)
                    typeIndex |= TransformComponentTypeFlag;

                if (category == TypeCategory.ISharedComponentData)
                    typeIndex |= SharedComponentTypeFlag;

                if (isCleanupComponent)
                    typeIndex |= CleanupComponentTypeFlag;

                if (isCleanupSharedComponent)
                    typeIndex |= CleanupSharedComponentTypeFlag;

                if (bufferCapacity >= 0)
                    typeIndex |= BufferComponentTypeFlag;

                if (!hasEntityReferences)
                    typeIndex |= HasNoEntityReferencesFlag;

                if (hasNativeContainer)
                    typeIndex |= HasNativeContainerFlag;

                if (isManaged)
                    typeIndex |= ManagedComponentTypeFlag;

                if (isEnableableComponent)
                    typeIndex |= EnableableComponentFlag;

                if (isRefCounted)
                    typeIndex |= IRefCountedComponentFlag;

                if (isTemporaryBakingType)
                    typeIndex |= TemporaryBakingTypeFlag;

                if (isBakingOnlyType)
                    typeIndex |= BakingOnlyTypeFlag;

                if (isIEquatable)
                    typeIndex |= IEquatableTypeFlag;

                if (!isChunkSerializable)
                    typeIndex |= IsNotChunkSerializableTypeFlag;

                if (hasOnAddedCallback)
                    typeIndex |= HasOnAddedCallbackFlag;

                if (hasOnRemovedCallback)
                    typeIndex |= HasOnRemovedCallbackFlag;

                // Note: Lifecycle callbacks are now registered via ILPP-generated code in ComponentLifecycleCallbacksPostprocessor.
                // The generated EarlyInit() method calls RegisterComponentLifecycleCallback() for each component type.
            }

            return new TypeInfo(typeIndex, category, entityOffsetCount, entityOffsetIndex,
                memoryOrdering, stableTypeHash, bufferCapacity, sizeInChunk,
                elementSize > 0 ? elementSize : sizeInChunk, alignmentInBytes,
                maxChunkCapacity,
                hasBlobReferences, blobAssetRefOffsetCount, blobAssetRefOffsetIndex,
                weakAssetRefOffsetCount, weakAssetRefOffsetIndex, hasUnityObjectRefs, unityObjectRefOffsetCount,
                unityObjectRefOffsetIndex, valueTypeSize, bloomFilterMask);
        }

        private struct SharedTypeIndex
        {
            public static ref TypeIndex Get(Type componentType)
            {
                return ref SharedStatic<TypeIndex>.GetOrCreate(typeof(TypeManagerKeyContext), componentType).Data;
            }
        }

        // A type can be registered more than once: an ILPP registry entry later reprocessed by reflection.
        private static void SetTypeName(int typeIndexNoFlags, string typeName)
        {
            var previousName = s_TypeNames[typeIndexNoFlags];
            if (previousName.IsCreated)
                previousName.Dispose();

            var unsafeName = new UnsafeText(Encoding.UTF8.GetByteCount(typeName), Allocator.Persistent);
            unsafeName.CopyFrom(typeName);
            s_TypeNames[typeIndexNoFlags] = unsafeName;
        }

        private static void AddFastEqualityInfo(int typeIndexNoFlags, Type type, bool isUnityEngineObject = false, Dictionary<Type, List<FastEquality.LayoutInfo>> cache = null)
        {
            s_FastEqualityTypeInfoList[typeIndexNoFlags].Dispose();
            s_FastEqualityTypeInfoList[typeIndexNoFlags] = (type == null || isUnityEngineObject)
                ? FastEquality.TypeInfo.Null
                : FastEquality.CreateTypeInfo(type);
        }


        private struct TypeManagerKeyContext { }
        private struct SharedTypeInfos
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedTypeInfos>();
        }
        private struct SharedDescendantMap
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedDescendantMap>();
        }
        private struct SharedEntityOffsetInfos
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedEntityOffsetInfos>();
        }
        private struct SharedStableTypeHashes
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedStableTypeHashes>();
        }
        private struct SharedBlobAssetRefOffsets
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedBlobAssetRefOffsets>();
        }
        private struct SharedWeakAssetRefOffsets
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedWeakAssetRefOffsets>();
        }
        private struct SharedUnityObjectRefOffsets
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedUnityObjectRefOffsets>();
        }
        private struct SharedWriteGroups
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedWriteGroups>();
        }
        private struct SharedWriteGroupOffsets
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedWriteGroupOffsets>();
        }
        private struct SharedFastEqualityTypeInfo
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedFastEqualityTypeInfo>();
        }
        private struct SharedTypeNames
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedTypeNames>();
        }
        private struct SharedTypeFullNameHashes
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedTypeFullNameHashes>();
        }
        private struct SharedSafetyHandle
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<AtomicSafetyHandle> Ref = SharedStatic<AtomicSafetyHandle>.GetOrCreate<TypeManagerKeyContext, AtomicSafetyHandle>();
        }

        internal struct SharedTypeIndex<TComponent>
        {
            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<TypeIndex> Ref = SharedStatic<TypeIndex>.GetOrCreate<TypeManagerKeyContext, TComponent>();
        }

        private sealed class Shared_SharedComponentData_FnPtrs
        {
            private Shared_SharedComponentData_FnPtrs()
            {
            }

            [NoAutoStaticsCleanup]
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, Shared_SharedComponentData_FnPtrs>();
        }
    }
}
