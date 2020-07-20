// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    [EditorTool("Edit Edge Collider 2D", typeof(EdgeCollider2D))]
    class EdgeCollider2DTool : EditablePathTool
    {
        public override GUIContent toolbarIcon => PrimitiveBoundsHandle.editModeButton;

        protected override void CollectEditablePaths(List<EditablePath2D> paths)
        {
            foreach (var collider in targets)
            {
                if (collider is EdgeCollider2D edgeCollider2D)
                    paths.Add(new EdgeColliderPath(edgeCollider2D));
            }
        }
    }

    class EdgeColliderPath : EditablePath2D
    {
        List<Vector2> m_Points2D;
        EdgeCollider2D m_Collider;

        public override Matrix4x4 localToWorldMatrix => m_Collider.transform.localToWorldMatrix;

        public EdgeColliderPath(EdgeCollider2D target) : base(false)
        {
            m_Collider = target;
            m_Points2D = new List<Vector2>();
            Update();
        }

        public override IList<Vector2> GetPoints()
        {
            Vector2 offset = m_Collider.offset;
            m_Collider.GetPoints(m_Points2D);
            for (int i = 0, c = m_Points2D.Count; i < c; i++)
                m_Points2D[i] += offset;
            return m_Points2D;
        }

        public override void SetPoints(Vector3[] points)
        {
            Vector2 offset = m_Collider.offset;
            int pointCount = points.Length;
            m_Points2D.Clear();
            m_Points2D.Capacity = pointCount;
            for (int i = 0; i < pointCount; i++)
                m_Points2D.Add((Vector2)points[i] - offset);
            Undo.RecordObject(m_Collider, "Edit Collider");
            m_Collider.SetPoints(m_Points2D);
        }
    }
}
