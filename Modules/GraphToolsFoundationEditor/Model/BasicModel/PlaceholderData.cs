// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    class PlaceholderData : ISerializationCallbackReceiver
    {
        [SerializeField, Obsolete]
#pragma warning disable CS0618
        List<SerializableGUID> m_BlockGuids;
#pragma warning restore CS0618

        [SerializeField]
        List<Hash128> m_BlockHashGuids;

        [SerializeField]
        string m_GroupTitle;

        [SerializeField]
        Vector2 m_Position;

        public List<Hash128> BlockGuids
        {
            get => m_BlockHashGuids;
            set => m_BlockHashGuids = value;
        }

        public string GroupTitle
        {
            get => m_GroupTitle;
            set => m_GroupTitle = value;
        }

        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
#pragma warning disable CS0612
#pragma warning disable CS0618
            m_BlockGuids = new List<SerializableGUID>(m_BlockHashGuids.Count);
            foreach (var guid in m_BlockHashGuids)
            {
                m_BlockGuids.Add(guid);
            }
#pragma warning restore CS0618
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
#pragma warning disable CS0612
            if (m_BlockGuids != null)
            {
                m_BlockHashGuids = new List<Hash128>(m_BlockGuids.Count);
                foreach (var guid in m_BlockGuids)
                {
                    m_BlockHashGuids.Add(guid);
                }

                m_BlockGuids = null;
            }
#pragma warning restore CS0612
        }
    }
}
