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
        LineHandle m_Handle;
        public LineHandle handle => m_Handle;

        public abstract IList<Vector2> GetPoints();
        public abstract void SetPoints(Vector3[] points);
        public abstract Matrix4x4 localToWorldMatrix { get; }

        protected EditablePath2D(bool loop, int count = 0)
        {
            m_Handle = new LineHandle(new Vector3[count], loop, LineHandle.LineIntersectionHighlight.Always);
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

    abstract class EditablePathTool : EditorTool
    {
        public override GUIContent toolbarIcon => PrimitiveBoundsHandle.editModeButton;

        List<EditablePath2D> m_EditablePaths = new List<EditablePath2D>();

        void OnEnable()
        {
            CacheColliderData();
            Undo.undoRedoPerformed += CacheColliderData;
            Selection.selectionChanged += CacheColliderData;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= CacheColliderData;
            Selection.selectionChanged -= CacheColliderData;
        }

        void CacheColliderData()
        {
            m_EditablePaths.Clear();
            CollectEditablePaths(m_EditablePaths);
        }

        protected abstract void CollectEditablePaths(List<EditablePath2D> paths);

        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();

            foreach (var data in m_EditablePaths)
            {
                data.Update();

                using (new Handles.DrawingScope(Color.green, data.localToWorldMatrix))
                {
                    EditorGUI.BeginChangeCheck();
                    data.handle.OnGUI(-Vector3.forward, Vector3.right, Vector3.up);
                    if (EditorGUI.EndChangeCheck())
                        data.SetPoints(data.handle.points);
                }
            }
        }
    }
}
