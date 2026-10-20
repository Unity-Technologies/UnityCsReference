// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Entities
{
    /// <summary>
    /// A utility structure that stores a reference of an <see cref="UnityEngine.Object"/> for Entities. Allows references to be stored on unmanaged component.
    /// </summary>
    /// <typeparam name="T">Type of the Object that is going to be referenced by UnityObjectRef.</typeparam>
    /// <remarks>
    /// Stores the Object's instance ID, and serializes the asset references it holds in subscenes. This is the recommended
    /// way to store references to Unity assets in Entities because it remains unmanaged.
    ///
    /// Serialization is supported on <see cref="IComponentData"/> <see cref="ISharedComponentData"/> and <see cref="IBufferElementData"/>.
    ///
    /// Just as when referencing an asset in a Monobehaviour, the asset will not be collected by any asset garbage collection (such as calling <see cref="Resources.UnloadUnusedAssets()"/>).
    ///
    /// For more information, refer to [Reference Unity objects in your code](xref:reference-unity-objects).
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct UnityObjectRef<T> : IEquatable<UnityObjectRef<T>>
        where T : Object
    {
        [SerializeField]
        internal UntypedUnityObjectRef Id;

        /// <summary>
        /// The EntityId of the referenced object, or <see cref="EntityId.None"/> if it references nothing.
        /// </summary>
        public EntityId EntityId => Id.entityId;

        /// <summary>
        /// Implicitly converts an <see cref="UnityEngine.Object"/> to an <see cref="UnityObjectRef{T}"/>.
        /// </summary>
        /// <param name="instance">Instance of the Object to store as a reference.</param>
        /// <returns>A UnityObjectRef referencing instance</returns>
        public static implicit operator UnityObjectRef<T>(T instance)
        {
            var entityId = instance == null ? EntityId.None : instance.GetEntityId();

            return FromEntityId(entityId);
        }

        /// <summary>
        /// Creates a reference to the object with the given EntityId.
        /// </summary>
        /// <param name="entityId">The EntityId of the object to reference.</param>
        /// <returns>A UnityObjectRef referencing the object.</returns>
        public static UnityObjectRef<T> FromEntityId(EntityId entityId)
        {
            var result = new UnityObjectRef<T>{Id = new UntypedUnityObjectRef{ entityId = entityId }};
            return result;
        }

        /// <summary>
        /// Implicitly converts an <see cref="UnityObjectRef{T}"/> to an <see cref="UnityEngine.Object"/>.
        /// </summary>
        /// <param name="unityObjectRef">Reference used to access the Object.</param>
        /// <returns>The instance of type T referenced by unityObjectRef.</returns>
        public static implicit operator T(UnityObjectRef<T> unityObjectRef)
        {
            var entityId = unityObjectRef.Id.entityId;
            if (entityId == EntityId.None)
                return null;

            // Fast path: lock-free resident lookup, no native crossing.
            var obj = UnityEngine.Object.GetManagedObject<T>(entityId);
            if (!ReferenceEquals(obj, null))
                return obj;

            // Miss: not resident, or no managed wrapper yet (e.g. a baked / deserialized
            // ref on first access). Resolve via native, which loads the asset from its
            // serialized file if still mapped and materializes the wrapper.
            return (T)Resources.EntityIdToObject(entityId);
        }

        /// <summary>
        /// Object being referenced by this <see cref="UnityObjectRef{T}"/>.
        /// </summary>
        public T Value
        {
            [ExcludeFromBurstCompatTesting("Returns managed object")]
            get => this;
            [ExcludeFromBurstCompatTesting("Sets managed object")]
            set => this = value;
        }

        /// <summary>
        /// Checks if this reference and another reference are equal.
        /// </summary>
        /// <param name="other">The UnityObjectRef to compare for equality.</param>
        /// <returns>True if the two lists are equal.</returns>
        public bool Equals(UnityObjectRef<T> other)
        {
            return Id.entityId == other.Id.entityId;
        }

        /// <summary>
        /// Checks if this object references the same UnityEngine.Object as another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True, if the <paramref name="obj"/> parameter is a UnityEngine.Object instance that points to the same
        /// instance as this.</returns>
        public override bool Equals(object obj)
        {
            return obj is UnityObjectRef<T> other && Equals(other);
        }


        /// <summary>
        /// Overload of the 'bool' operator to check for the validity of the instance ID.
        /// </summary>
        /// <param name="obj">The object to check for validity.</param>
        /// <returns>True, if the instance ID is valid.</returns>
        public static implicit operator bool(UnityObjectRef<T> obj)
        {
            return obj.IsValid();
        }

        /// <summary>
        /// Computes a hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Id.entityId.GetHashCode();
        }

        /// <summary>
        /// Returns 'true' if the UnityObjectRef is still valid, as in the Object still exists.
        /// </summary>
        /// <returns>Valid state.</returns>
        public bool IsValid()
        {
            return Resources.EntityIdIsValid(Id.entityId);
        }

        /// <summary>
        /// Returns true if two <see cref="UnityObjectRef{T}"/> are equal.
        /// </summary>
        /// <param name="left">The first reference to compare for equality.</param>
        /// <param name="right">The second reference to compare for equality.</param>
        /// <returns>True if the two references are equal.</returns>
        public static bool operator ==(UnityObjectRef<T> left, UnityObjectRef<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true if two <see cref="UnityObjectRef{T}"/> are not equal.
        /// </summary>
        /// <param name="left">The first reference to compare for equality.</param>
        /// <param name="right">The second reference to compare for equality.</param>
        /// <returns>True if the two references are not equal.</returns>
        public static bool operator !=(UnityObjectRef<T> left, UnityObjectRef<T> right)
        {
            return !left.Equals(right);
        }
    }
}
