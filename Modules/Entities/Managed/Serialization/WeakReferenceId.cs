// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Enumeration of weak reference types.
    /// </summary>
    public enum WeakReferenceGenerationType : short
    {
        /// <summary>
        /// Unknown generation type, this usually indicates that the <seealso cref="UntypedWeakReferenceId"/> has not been initialized.
        /// </summary>
        Unknown,
        /// <summary>
        /// Reference to an object derived from <seealso cref="UnityEngine.Object"/>.
        /// </summary>
        UnityObject,
        /// <summary>
        /// Texture asset for DOTS Runtime.
        /// </summary>
        Texture,
        /// <summary>
        /// Reference to a GameObject based scene.
        /// </summary>
        GameObjectScene,
        /// <summary>
        /// Reference to an Entity based scene.
        /// </summary>
        EntityScene,
        /// <summary>
        /// Reference to a converted prefab.
        /// </summary>
        EntityPrefab,
        /// <summary>
        /// Reference to a collection of referenced <seealso cref="UnityEngine.Object"/>s from a sub scene.
        /// </summary>
        SubSceneObjectReferences
    };

    // Workaround to be able to store UntypedWeakReferenceId in a blob.
    // Usually we want to issue an error when storing UntypedWeakReferenceId in a blob to alert users that this is not yet supported
    // But in this specific case we know what we are doing. This type must be binary compatible with  UntypedWeakReferenceId
    internal struct UnsafeUntypedWeakReferenceId
    {
        public UnsafeUntypedWeakReferenceId(UntypedWeakReferenceId weakAssetRef)
        {
            GlobalId = weakAssetRef.GlobalId;
            GenerationType = weakAssetRef.GenerationType;
        }
        public RuntimeGlobalObjectId GlobalId;
        public WeakReferenceGenerationType GenerationType;
    }

    /// <summary>
    /// Used to identify weakly referenced data.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct UntypedWeakReferenceId : IEquatable<UntypedWeakReferenceId>
    {
        /// <summary>
        /// The global object id.  This matches the editor global id.
        /// </summary>
        public RuntimeGlobalObjectId GlobalId;

        /// <summary>
        /// The type of reference this is.
        /// It is aliased to RuntimeGlobalObjectId.SceneObjectIdentifier1.
        /// </summary>
        public WeakReferenceGenerationType GenerationType;

        /// <summary>
        /// Construct a reference given the global id and generation type.
        /// </summary>
        /// <param name="globalObjId">The global object id.</param>
        /// <param name="genType">The generation type.</param>
        public UntypedWeakReferenceId(RuntimeGlobalObjectId globalObjId, WeakReferenceGenerationType genType)
        {
            GlobalId = globalObjId;
            GenerationType = genType;
        }

        /// <summary>
        /// Construct new UntypedWeakReferenceId.
        /// </summary>
        /// <param name="guid">The asset guid.</param>
        /// <param name="localFileIdentifier">The object local identifier.</param>
        /// <param name="idType">The object type.</param>
        /// <param name="genType">The id generation type.</param>
        public UntypedWeakReferenceId(Hash128 guid, long localFileIdentifier, int idType, WeakReferenceGenerationType genType)
        {
            GlobalId = new RuntimeGlobalObjectId { AssetGUID = guid, SceneObjectIdentifier0 = localFileIdentifier, IdentifierType = idType };
            GenerationType = genType;
        }

        /// <summary>
        /// Checks if this reference is equal to another reference.
        /// </summary>
        /// <param name="other">The id to compare to.</param>
        /// <returns>True if the GenerationType and GlobalId are equal.</returns>
        public bool Equals(UntypedWeakReferenceId other) => GenerationType == other.GenerationType && GlobalId.Equals(other.GlobalId);

        /// <summary>
        /// Converts the id to a string.
        /// </summary>
        /// <returns>The string representation of this id.</returns>
        public override string ToString() => GlobalId.ToString();

        /// <summary>
        /// Checks if this reference is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the other object is an <seealso cref="UntypedWeakReferenceId"/> and is equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is UntypedWeakReferenceId other && Equals(other);
        }

        /// <inheritdoc/>
        public bool IsValid => GlobalId.IsValid;

        /// <summary>
        /// Returns the hash code of this id.
        /// </summary>
        /// <returns>The hash code of this id.</returns>
        public override int GetHashCode() => GlobalId.GetHashCode();

        /// <inheritdoc/>
        public static bool operator ==(UntypedWeakReferenceId left, UntypedWeakReferenceId right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(UntypedWeakReferenceId left, UntypedWeakReferenceId right)
        {
            return !left.Equals(right);
        }

        // This module can't name UnityEngine.Object, so the editor conversions live in the package's
        // UntypedWeakReferenceIdEditor; these stubs point existing calls there. They can't be (UnityUpgradable):
        // the API updater validation can't load the package assembly that holds the target.

        /// <summary>
        /// Moved to UntypedWeakReferenceIdEditor.CreateFromObjectInstance.
        /// </summary>
        /// <param name="obj">The object to reference.</param>
        /// <returns>Never returns.</returns>
        [Obsolete("CreateFromObjectInstance moved to Unity.Entities.Serialization.UntypedWeakReferenceIdEditor.CreateFromObjectInstance in the com.unity.entities package.", true)]
        public static UntypedWeakReferenceId CreateFromObjectInstance(object obj) => throw new NotSupportedException();

        /// <summary>
        /// Moved to UntypedWeakReferenceIdEditor.GetEditorObject.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns>Never returns.</returns>
        [Obsolete("GetEditorObject moved to Unity.Entities.Serialization.UntypedWeakReferenceIdEditor.GetEditorObject in the com.unity.entities package.", true)]
        public static object GetEditorObject(UntypedWeakReferenceId id) => throw new NotSupportedException();
    }
}
