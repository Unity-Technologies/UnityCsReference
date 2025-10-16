// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    partial class UserContextNodeModelImp : ContextNodeModel, IUserNodeModelImp
    {
        [NonSerialized]
        new List<BlockNode> m_Blocks;

        public IReadOnlyList<BlockNode> Blocks
        {
            get
            {
                BuildBlocksList();
                return m_Blocks;
            }
        }

        void BuildBlocksList()
        {
            if (m_Blocks == null)
            {
                m_Blocks = new List<BlockNode>(base.m_Blocks.Count);
                foreach (var block in base.m_Blocks)
                {
                    if (block != null)
                        m_Blocks.Add(((UserBlockNodeModelImp)block).Node);
                }
            }
        }

        public void AddBlock(UserBlockNodeModelImp userBlockNodeModelImp)
        {
            if (m_Blocks == null)
                BuildBlocksList();
            else
            {
                //The node is added to the block list before this is called so BuildBlocksList() will add the added block as well.
                var index = userBlockNodeModelImp.GetIndex();
                m_Blocks.Insert(index, userBlockNodeModelImp.Node);
            }
        }

        public void RemoveBlock(UserBlockNodeModelImp userBlockNodeModelImp)
        {
            ((IUserNodeModelImp)userBlockNodeModelImp).CallOnDisable();
            m_Blocks?.Remove(userBlockNodeModelImp.Node);
        }

        void IUserNodeModelImp.CallOnEnable()
        {
            Node?.OnEnable();
            foreach (var block in GetGraphElementModels())
            {
                if (block is IUserNodeModelImp userBlockNodeModelImp)
                {
                    userBlockNodeModelImp.CallOnEnable();
                }
            }
        }

        void IUserNodeModelImp.CallOnDisable()
        {
            foreach (var block in GetGraphElementModels())
            {
                if (block is IUserNodeModelImp userBlockNodeModelImp)
                {
                    userBlockNodeModelImp.CallOnDisable();
                }
            }
            Node?.OnDisable();
        }
    }
}
