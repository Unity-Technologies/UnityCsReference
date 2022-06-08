// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    abstract class EditablePath2DTool : Collider2DToolbase
    {
        List<EditablePath2D> m_EditablePaths = new List<EditablePath2D>();

        void OnEnable()
        {
            CacheColliderData();
            Undo.undoRedoEvent += OnUndoRedo;
            Selection.selectionChanged += CacheColliderData;
        }

        void OnDisable()
        {
            Undo.undoRedoEvent -= OnUndoRedo;
            Selection.selectionChanged -= CacheColliderData;
        }

        void OnUndoRedo(in UndoRedoInfo info)
        {
            CacheColliderData();
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

            CacheColliderData();

            foreach (var data in m_EditablePaths)
            {
                data.Update();

                // The Collider2D matrix is a 2D projection of the transforms rotation onto the X/Y plane about the transforms origin.
                var handleMatrix = data.localToWorldMatrix;
                handleMatrix.SetRow(0, Vector4.Scale(handleMatrix.GetRow(0), new Vector4(1f, 1f, 0f, 1f)));
                handleMatrix.SetRow(1, Vector4.Scale(handleMatrix.GetRow(1), new Vector4(1f, 1f, 0f, 1f)));
                handleMatrix.SetRow(2, new Vector4(0f, 0f, 1f, data.localToWorldMatrix.GetColumn(3).z));

                using (new Handles.DrawingScope(Color.green, handleMatrix))
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
