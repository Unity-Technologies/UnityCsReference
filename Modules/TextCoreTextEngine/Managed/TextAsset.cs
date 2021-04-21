// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        public int instanceID
        {
            get
            {
                if (m_InstanceID == 0)
                    m_InstanceID = GetInstanceID();

                return m_InstanceID;
            }
        }

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

        internal int m_InstanceID;

        internal int m_HashCode;

        [SerializeField][FormerlySerializedAs("material")]
        internal Material m_Material;

        internal int m_MaterialHashCode;
    }
}
