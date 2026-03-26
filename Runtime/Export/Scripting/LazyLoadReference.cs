// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngineInternal;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    //----------------------------------------------------------------------------------------------------------------------
    // What is this : Serializable lazy reference to a UnityEngine.Object contained in an asset file, where referenced object
    //                is loaded only when accessed and not at deserialization of this struct.
    // Motivation(s):
    //  - Importer settings need to be able to reference assets but reading the settings from disk Must Not load the referenced
    //    assets: they may not have been imported yet.
    //  - Scenes with large number of assets can be slow to open because all referenced assets get loaded at the same time
    //    as the scene, even if they are not needed right away. Having lazy loading can make loading scene faster for the user.
    //    For example: MonoBehaviours that have references to assets but that do not execute in editor mode, do not need to have
    //         all the referenced assets loaded when editor opens a scene. Later when user enters play mode, those assets get loaded.
    //
    // Notes:
    //  - *** The memory layout of this struct must be identical to the native type: AssetReferenceMemoryLayout. ***
    //  - Not using bindings file as we don't want a wrapper class in this situation. but it must mirror it's native counter part to a 'T'.
    //----------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Serializable lazy reference to a <see cref="UnityEngine.Object"/> contained in an asset file, where referenced object is loaded only when accessed and not at deserialization of this struct.
    /// </summary>
    /// <typeparam name="T">The type of the asset.</typeparam>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct LazyLoadReference<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private EntityId m_EntityId;

        /// <summary>
        /// Determines if the reference is linked to an asset, loaded or not, valid or not.
        /// Calling this never triggers a load.
        /// </summary>
        public bool isSet => m_EntityId != EntityId.None;

        /// <summary>
        /// Convenience property that checks if the reference is broken: is set to something, but that something is not available/loadable at the moment for whatever reason.
        /// May trigger loading the referenced object into memory if the object is not already loaded.
        /// </summary>
        public bool isBroken => m_EntityId != EntityId.None && !UnityEngine.Object.DoesObjectWithInstanceIDExist(m_EntityId);

        /// <summary>
        /// Accessor to the referenced asset.
        /// May trigger loading the referenced object into memory if the object is not already loaded.
        /// </summary>
        public T asset
        {
            get
            {
                if (m_EntityId == EntityId.None)
                {
                    return null;
                }
                else
                {
                    return (T)Object.ForceLoadFromInstanceID(m_EntityId);
                }
            }
            set
            {
                if (value == null)
                {
                    m_EntityId = EntityId.None;
                }
                else
                {
                    if (!Object.IsPersistent(value))
                    {
                        throw new ArgumentException("Object that does not belong to a persisted asset cannot be set as the target of a LazyLoadReference.");
                    }
                    m_EntityId = value.GetEntityId();
                }
            }
        }

        /// <summary>
        /// EntityId of the referenced asset.
        /// Getting or setting this never triggers a load.
        /// </summary>
        public EntityId entityId
        {
            get => m_EntityId;
            set => m_EntityId = value;
        }

        [Obsolete("Use entityId instead, this will be removed in a future version", true)]
        public int instanceID
        {
            get => entityId;
            set => entityId = value;
        }

        /// <summary>
        /// Construct a <see cref="LazyLoadReference{T}"/> from asset reference.
        /// May trigger loading the referenced object into memory if the object is not already loaded.
        /// </summary>
        /// <param name="asset"></param>
        public LazyLoadReference(T asset)
        {
            if (asset == null)
            {
                m_EntityId = EntityId.None;
            }
            else
            {
                if (!Object.IsPersistent(asset))
                {
                    throw new ArgumentException("Object that does not belong to a persisted asset cannot be set as the target of a LazyLoadReference.");
                }
                m_EntityId = asset.GetEntityId();
            }
        }

        /// <summary>
        /// Construct a <see cref="LazyLoadReference{T}"/> from asset EntityId.
        /// Calling this never triggers a load.
        /// </summary>
        /// <param name="entityId"></param>
        public LazyLoadReference(EntityId entityId)
        {
            m_EntityId = entityId;
        }

        [Obsolete("Use LazyLoadReference(EntityId entityId) instead, this will be removed in a future version", true)]
        public LazyLoadReference(int instanceID)
        {
            m_EntityId = instanceID;
        }

        /// <summary>
        /// Implicit conversion from <see cref="T"/> asset to <see cref="LazyLoadReference{T}"/>.
        /// May trigger loading the referenced object into memory if the object is not already loaded.
        /// </summary>
        /// <param name="asset">The asset reference.</param>
        public static implicit operator LazyLoadReference<T>(T asset)
        {
            return new LazyLoadReference<T> { asset = asset };
        }

        /// <summary>
        /// Implicit conversion from asset entity ID to <see cref="LazyLoadReference{T}"/>.
        /// Calling this never triggers a load.
        /// </summary>
        /// <param name="entityId">The asset entity ID.</param>
        public static implicit operator LazyLoadReference<T>(EntityId entityId)
        {
            return new LazyLoadReference<T> { m_EntityId = entityId };
        }

        /// <summary>
        /// Implicit conversion from asset instance ID to <see cref="LazyLoadReference{T}"/>.
        /// Calling this never triggers a load.
        /// </summary>
        /// <param name="instanceID">The asset instance ID.</param>
        [Obsolete("Use LazyLoadReference(EntityId entityId) instead, this will be removed in a future version", true)]
        public static implicit operator LazyLoadReference<T>(int instanceID)
        {
            return new LazyLoadReference<T> { m_EntityId = instanceID };
        }
    }
}
