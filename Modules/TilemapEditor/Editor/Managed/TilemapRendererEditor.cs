// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(TilemapRenderer))]
    [CanEditMultipleObjects]
    internal class TilemapRendererEditor : RendererEditorBase
    {
        private SerializedProperty m_Material;
        private SerializedProperty m_SortOrder;
        private SerializedProperty m_MaskInteraction;

        private TilemapRenderer renderer { get { return target as TilemapRenderer; } }

        private static class Styles
        {
            public static readonly GUIContent materialLabel = EditorGUIUtility.TextContent("Material");
            public static readonly GUIContent focusLabel = EditorGUIUtility.TextContent("Focus On");
            public static readonly GUIContent rendererOverlayTitleLabel = EditorGUIUtility.TextContent("Tilemap Renderer");
            public static readonly GUIContent multipleRendererFocusInfoBox = EditorGUIUtility.TextContent("Disabled for multi-selection");
        }
        public override void OnEnable()
        {
            base.OnEnable();

            m_Material = serializedObject.FindProperty("m_Materials.Array"); // Only allow to edit one material
            m_SortOrder = serializedObject.FindProperty("m_SortOrder");
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");

            EnableFocus();
            SceneView.onSceneGUIDelegate += OnSceneViewGUI;
        }

        public void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
            DisableFocus();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Material.GetArrayElementAtIndex(0), Styles.materialLabel, true);
            EditorGUILayout.PropertyField(m_SortOrder);
            RenderSortingLayerFields();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_MaskInteraction);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            SceneViewOverlay.Window(Styles.rendererOverlayTitleLabel, DisplayFocusMode, (int)SceneViewOverlay.Ordering.TilemapRenderer, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        private void DisplayFocusMode(Object displayTarget, SceneView sceneView)
        {
            var oldFocus = TilemapEditorUserSettings.focusMode;
            var multipleRendererFocus = targets.Length > 1;

            using (new EditorGUI.DisabledGroupScope(multipleRendererFocus))
            {
                var focus = (TilemapEditorUserSettings.FocusMode)EditorGUILayout.EnumPopup(Styles.focusLabel, oldFocus);
                if (focus != oldFocus)
                {
                    DisableFocus();
                    TilemapEditorUserSettings.focusMode = focus;
                    EnableFocus();
                }
            }

            if (multipleRendererFocus)
            {
                EditorGUILayout.HelpBox(Styles.multipleRendererFocusInfoBox.text, MessageType.Info);
            }
        }

        private void EnableFocus()
        {
            // Disable focus mode if more than TilemapRenderer is enabled
            if (targets.Length > 1)
                return;

            switch (TilemapEditorUserSettings.focusMode)
            {
                case TilemapEditorUserSettings.FocusMode.Tilemap:
                {
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.SetSceneViewFiltering(true);
                    HierarchyProperty.FilterSingleSceneObject(renderer.gameObject.GetInstanceID(), false);
                    break;
                }
                case TilemapEditorUserSettings.FocusMode.Grid:
                {
                    var tilemap = renderer.GetComponent<Tilemap>();
                    if (tilemap != null && tilemap.layoutGrid != null)
                    {
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.SetSceneViewFiltering(true);
                        HierarchyProperty.FilterSingleSceneObject(tilemap.layoutGrid.gameObject.GetInstanceID(), false);
                    }
                    break;
                }
                default:
                {
                    break;
                }
            }
        }

        private void DisableFocus()
        {
            if (TilemapEditorUserSettings.focusMode == TilemapEditorUserSettings.FocusMode.None)
                return;

            HierarchyProperty.ClearSceneObjectsFilter();
            SceneView.lastActiveSceneView.SetSceneViewFiltering(false);
        }
    }
}
