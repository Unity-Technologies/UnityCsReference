// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [Serializable]
    [UnityRestricted]
    internal struct SubgraphAssetProperty
    {
        [SerializeField]
        GraphReference m_SubgraphReference;

        public GraphReference SubgraphReference => m_SubgraphReference;

        public SubgraphAssetProperty(GraphReference value)
        {
            m_SubgraphReference = value;
        }
    }
}
