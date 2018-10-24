// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Linq;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(PolygonCollider2D))]
    [CanEditMultipleObjects]
    internal class PolygonCollider2DEditor : Collider2DEditorBase
    {
        private readonly PolygonEditorUtility m_PolyUtility = new PolygonEditorUtility();

        private SerializedProperty m_Points;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Points = serializedObject.FindProperty("m_Points");
            m_AutoTiling = serializedObject.FindProperty("m_AutoTiling");
            m_Points.isExpanded = false;
        }

        public override void OnInspectorGUI()
        {
            // Start a vertical group which we'll use to note the layout rect as a drag-drop target.
            EditorGUILayout.BeginVertical();
            bool disableEditCollider = !CanEditCollider();
            if (disableEditCollider)
            {
                EditorGUILayout.HelpBox(Styles.s_ColliderEditDisableHelp.text, MessageType.Info);
                if (editingCollider)
                    EditMode.QuitEditMode();
            }
            else
                BeginColliderInspector();

            // Grab this as the offset to the top of the drag target.
            base.OnInspectorGUI();

            if (targets.Length == 1)
            {
                EditorGUI.BeginDisabledGroup(editingCollider);
                EditorGUILayout.PropertyField(m_Points, true);
                EditorGUI.EndDisabledGroup();
            }

            EndColliderInspector();

            FinalizeInspectorGUI();

            // End the vertical layout and use the layout rect as the drag-drop target.
            EditorGUILayout.EndVertical();
            HandleDragAndDrop(GUILayoutUtility.GetLastRect());
        }

        // Copy collider from sprite if it is drag&dropped to the inspector
        private void HandleDragAndDrop(Rect targetRect)
        {
            if (Event.current.type != EventType.DragPerform && Event.current.type != EventType.DragUpdated)
                return;

            // Check we're dropping onto the polygon collider editor.
            if (!targetRect.Contains(Event.current.mousePosition))
                return;

            foreach (var obj in DragAndDrop.objectReferences.Where(obj => obj is Sprite || obj is Texture2D))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform)
                {
                    var sprite = obj is Sprite ? obj as Sprite : SpriteUtility.TextureToSprite(obj as Texture2D);

                    // Copy collider to all selected components
                    foreach (var collider in targets.Select(target => target as PolygonCollider2D))
                    {
                        Vector2[][] paths;
                        UnityEditor.Sprites.SpriteUtility.GenerateOutlineFromSprite(sprite, 0.25f, 200, true, out paths);
                        collider.pathCount = paths.Length;
                        for (int i = 0; i < paths.Length; ++i)
                            collider.SetPath(i, paths[i]);

                        // Stop editing
                        m_PolyUtility.StopEditing();

                        DragAndDrop.AcceptDrag();
                    }
                }

                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }

        protected override void OnEditStart()
        {
            // We don't support multiselection editing
            if (target == null)
                return;

            m_PolyUtility.StartEditing(target as Collider2D);
        }

        protected override void OnEditEnd()
        {
            m_PolyUtility.StopEditing();
        }

        public void OnSceneGUI()
        {
            if (!editingCollider)
                return;

            m_PolyUtility.OnSceneGUI();
        }
    }
}
