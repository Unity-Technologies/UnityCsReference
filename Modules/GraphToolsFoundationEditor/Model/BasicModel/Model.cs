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
    abstract class Model
    {
        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
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
        protected Model(SerializableGUID guid)
        {
            m_Guid = guid;
        }

        /// <summary>
        /// Sets the unique identifier of the model.
        /// </summary>
        /// <param name="value">The new GUID.</param>
        public virtual void SetGuid(SerializableGUID value)
        {
            m_Guid = value;
        }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }
    }
}
