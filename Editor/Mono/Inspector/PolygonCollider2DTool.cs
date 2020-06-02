// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    [EditorTool("Edit Polygon Collider 2D", typeof(PolygonCollider2D))]
    class PolygonCollider2DTool : EditablePathTool
    {
        public override GUIContent toolbarIcon => PrimitiveBoundsHandle.editModeButton;

        protected override void CollectEditablePaths(List<EditablePath2D> paths)
        {
            foreach (var collider in targets)
            {
                if (!(collider is PolygonCollider2D polygon))
                    continue;

                for (int i = 0, c = polygon.pathCount; i < c; i++)
                    paths.Add(new PolygonColliderPath(polygon, i));
            }
        }
    }

    class PolygonColliderPath : EditablePath2D
    {
        public PolygonCollider2D m_Collider;
        int m_PathIndex;
        public LineHandle[] handles;
        List<Vector2> m_Points2D;

        public override Matrix4x4 localToWorldMatrix => m_Collider.transform.localToWorldMatrix;

        public PolygonColliderPath(PolygonCollider2D target, int shapeIndex) : base(true)
        {
            m_Collider = target;
            m_PathIndex = shapeIndex;
            m_Points2D = new List<Vector2>();
        }

        public override IList<Vector2> GetPoints()
        {
            Vector2 offset = m_Collider.offset;
            m_Collider.GetPath(m_PathIndex, m_Points2D);
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
            m_Collider.SetPath(m_PathIndex, m_Points2D);
        }
    }
}
