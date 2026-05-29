// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Profiling
{
    [Serializable]
    record TilemapHierarchyBaseNode
    {
        public string name ="Unknown";
        int m_MeshCountValue = 0;
        int m_ChunkCountValue = 0;
        public string meshCount = "";
        public string chunkCount = "";
        public string icon;
        public int id;
        public EntityId entityId;
        public static int Compare(TilemapHierarchyBaseNode a, TilemapHierarchyBaseNode b, string propertyToCompare)
        {
            switch (propertyToCompare)
            {
                case "name":
                    return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                case "chunkCount":
                    return a.m_ChunkCountValue.CompareTo(b.m_ChunkCountValue);
                case "meshCount":
                    return a.m_MeshCountValue.CompareTo(b.m_MeshCountValue);
                default:
                    return 0;
            }
        }

        public int meshCountValue
        {
            get => m_MeshCountValue;
            set
            {
                m_MeshCountValue = value;
                meshCount = value.ToString();
            }
        }

        public int chunkCountValue
        {
            get => m_ChunkCountValue;
            set
            {
                m_ChunkCountValue = value;
                chunkCount = value.ToString();
            }
        }
    }

    [Serializable]
    record TilemapChunkRecord :TilemapHierarchyBaseNode
    {
        public int chunkIdX;
        public int chunkIdY;
    }

    [Serializable]
    record TilemapHierarchyNodeData :TilemapHierarchyBaseNode
    {
        public List<TilemapChunkRecord> chunkRecord = new ();

        public virtual bool Equals(TilemapHierarchyNodeData other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return entityId == other.entityId;
        }

        public override int GetHashCode()
        {
            return entityId.GetHashCode();
        }
    }
}
