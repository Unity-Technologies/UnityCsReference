// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    abstract class EditablePath2D
    {
        EditableLineHandle2D m_Handle;
        public EditableLineHandle2D handle => m_Handle;

        public abstract IList<Vector2> GetPoints();
        public abstract void SetPoints(Vector3[] points);
        public abstract Matrix4x4 localToWorldMatrix { get; }

        protected EditablePath2D(bool loop, int pointCount, int minPointCount)
        {
            m_Handle = new EditableLineHandle2D(new Vector3[pointCount], loop, minPointCount, EditableLineHandle2D.LineIntersectionHighlight.Always);
        }

        public void Update()
        {
            var cached = GetPoints();
            var points = m_Handle.points;
            if (points.Length != cached.Count)
                System.Array.Resize(ref points, cached.Count);
            for (int i = 0; i < points.Length; i++)
                points[i] = cached[i];
            m_Handle.points = points;
        }
    }
}
