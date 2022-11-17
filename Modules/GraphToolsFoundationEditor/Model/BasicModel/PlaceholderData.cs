// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    class PlaceholderData
    {
        [SerializeField]
        List<SerializableGUID> m_BlockGuids;

        [SerializeField]
        string m_GroupTitle;

        [SerializeField]
        Vector2 m_Position;

        public List<SerializableGUID> BlockGuids
        {
            get => m_BlockGuids;
            set => m_BlockGuids = value;
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
    }
}
