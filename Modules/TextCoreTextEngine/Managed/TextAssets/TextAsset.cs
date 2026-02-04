// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Serialization;


namespace UnityEngine.TextCore.Text
{
    // Base class inherited by the various TextMeshPro Assets.
    [System.Serializable][ExcludeFromObjectFactory]
    public abstract class TextAsset : ScriptableObject
    {
        /// <summary>
        /// The version of the text asset class.
        /// Version 1.1.0 introduces new data structure to be compatible with new font asset structure.
        /// </summary>
        public string version
        {
            get { return m_Version; }
            internal set { m_Version = value; }
        }

        /// <summary>
        /// Instance ID of the TMP Asset
        /// </summary>
        public EntityId entityId
        {
            get
            {
                if (m_EntityId == EntityId.None)
                    m_EntityId = GetEntityId();

                return m_EntityId;
            }
        }

        [System.Obsolete("Use entityId instead.")]
        public int instanceID => entityId;

        /// <summary>
        /// HashCode based on the name of the asset.
        /// </summary>
        public int hashCode
        {
            get
            {
                if (m_HashCode == 0)
                    m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(name);

                return m_HashCode;
            }
            set => m_HashCode = value;
        }

        /// <summary>
        /// The material used by this asset.
        /// </summary>
        public Material material
        {
            get => m_Material;
            set => m_Material = value;
        }

        /// <summary>
        /// HashCode based on the name of the material assigned to this asset.
        /// </summary>
        public int materialHashCode
        {
            get
            {
                if (m_MaterialHashCode == 0)
                {
                    if (m_Material == null)
                        return 0;

                    m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Material.name);
                }

                return m_MaterialHashCode;
            }
            set => m_MaterialHashCode = value;
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        internal string m_Version;

        internal EntityId m_EntityId;

        internal int m_HashCode;

        [SerializeField][FormerlySerializedAs("material")]
        internal Material m_Material;

        internal int m_MaterialHashCode;

        private static Dictionary<EntityId, WeakReference<TextAsset>> kTextAssetByInstanceId = new Dictionary<EntityId, WeakReference<TextAsset>>();

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal static TextAsset GetTextAssetByID(EntityId id)
        {
            if (id == EntityId.None)
                return null;

            if (kTextAssetByInstanceId.TryGetValue(id, out var weakRef))
            {
                if (weakRef.TryGetTarget(out var asset))
                    return asset;

                kTextAssetByInstanceId.Remove(id);
                Debug.Assert(false, $"TextAsset with EntityId {id} was collected but is still referenced.");
            }
            else
            {
                Debug.Assert(false, $"TextAsset with EntityId {id} not found in the cache.");
            }

            return null;
        }

        internal virtual void OnDestroy()
        {
            kTextAssetByInstanceId.Remove(entityId);
        }

        internal virtual void OnEnable()
        {
            EnsureRegisteredInCache();
        }

        /// <summary>
        /// Ensures this TextAsset is registered in the cache with a valid WeakReference.
        /// This handles cases where a previous entry exists but the WeakReference is stale due to a domain reload
        /// or other reasons.
        /// </summary>
        internal void EnsureRegisteredInCache()
        {
            var id = entityId;
            if (kTextAssetByInstanceId.TryGetValue(id, out var existingRef))
            {
                // If the existing reference is still valid and points to this object, we're done
                if (existingRef.TryGetTarget(out var existingAsset) && existingAsset == this)
                    return;

                // Otherwise, the reference is stale or points to a different object - replace it
                kTextAssetByInstanceId[id] = new WeakReference<TextAsset>(this);
            }
            else
            {
                kTextAssetByInstanceId.Add(id, new WeakReference<TextAsset>(this));
            }
        }
    }
}
