// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Abstract base class for all models.
    /// </summary>
    [Serializable]
    abstract class Model : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_Guid;
#pragma warning restore CS0618

        [SerializeField, HideInInspector]
        Hash128 m_HashGuid;

#pragma warning disable CS0612
        internal static string obsoleteGuidFieldName_Internal = nameof(m_Guid);
#pragma warning restore CS0612
        internal static string hashGuidFieldName_Internal = nameof(m_HashGuid);

        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        public Hash128 Guid
        {
            get
            {
                if (!m_HashGuid.isValid)
                    AssignNewGuid();
                return m_HashGuid;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        protected Model()
        {
            AssignNewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Model"/> class.
        /// </summary>
        protected Model(Hash128 guid)
        {
            m_HashGuid = guid;
        }

        /// <summary>
        /// Sets the unique identifier of the model.
        /// </summary>
        /// <param name="value">The new GUID.</param>
        public virtual void SetGuid(Hash128 value)
        {
            m_HashGuid = value;
        }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        public void AssignNewGuid()
        {
            m_HashGuid = Hash128Extensions.Generate();
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
#pragma warning disable CS0612
            m_Guid = m_HashGuid;
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
#pragma warning disable CS0612
            m_HashGuid = m_Guid;
#pragma warning restore CS0612
        }
    }
}
