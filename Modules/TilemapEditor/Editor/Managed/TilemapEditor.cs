// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditor
{
    [CustomEditor(typeof(Tilemap))]
    [CanEditMultipleObjects]
    internal class TilemapEditor : Editor
    {
        private SerializedProperty m_AnimationFrameRate;
        private SerializedProperty m_TilemapColor;
        private SerializedProperty m_TileAnchor;
        private SerializedProperty m_Orientation;
        private SerializedProperty m_OrientationMatrix;

        private Tilemap tilemap { get { return (target as Tilemap); } }

        private readonly AnimBool m_ShowInfo = new AnimBool();
        private SavedBool m_ShowInfoFoldout;
        private TileBase[] usedTiles;
        private Sprite[] usedSprites;

        private static class Styles
        {
            public static readonly GUIContent animationFrameRateLabel = EditorGUIUtility.TrTextContent("Animation Frame Rate", "Frame rate for playing animated tiles in the tilemap");
            public static readonly GUIContent tilemapColorLabel = EditorGUIUtility.TrTextContent("Color", "Color tinting all Sprites from tiles in the tilemap");
            public static readonly GUIContent tileAnchorLabel = EditorGUIUtility.TrTextContent("Tile Anchor", "Anchoring position for Sprites from tiles in the tilemap");
            public static readonly GUIContent orientationLabel = EditorGUIUtility.TrTextContent("Orientation", "Orientation for tiles in the tilemap");

            public static readonly GUIContent usedTilesLabel = EditorGUIUtility.TrTextContent("Tiles", "Tiles used in the Tilemap");
            public static readonly GUIContent usedSpritesLabel = EditorGUIUtility.TrTextContent("Sprites", "Sprites used in the Tilemap");
            public static readonly GUIContent noneUsedLabel = EditorGUIUtility.TrTextContent("None");
        }

        private void OnEnable()
        {
            m_AnimationFrameRate = serializedObject.FindProperty("m_AnimationFrameRate");
            m_TilemapColor = serializedObject.FindProperty("m_Color");
            m_TileAnchor = serializedObject.FindProperty("m_TileAnchor");
            m_Orientation = serializedObject.FindProperty("m_TileOrientation");
            m_OrientationMatrix = serializedObject.FindProperty("m_TileOrientationMatrix");

            m_ShowInfo.valueChanged.AddListener(Repaint);
            m_ShowInfoFoldout = new SavedBool($"{target.GetType()}.ShowFoldout", false);
            m_ShowInfo.value = m_ShowInfoFoldout.value;
        }

        private void OnDisable()
        {
            if (tilemap != null)
                tilemap.ClearAllEditorPreviewTiles();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_AnimationFrameRate, Styles.animationFrameRateLabel);
            EditorGUILayout.PropertyField(m_TilemapColor, Styles.tilemapColorLabel);
            EditorGUILayout.PropertyField(m_TileAnchor, Styles.tileAnchorLabel);
            EditorGUILayout.PropertyField(m_Orientation, Styles.orientationLabel);
            GUI.enabled = (!m_Orientation.hasMultipleDifferentValues && Tilemap.Orientation.Custom == tilemap.orientation);
            if (targets.Length > 1)
                EditorGUILayout.PropertyField(m_OrientationMatrix, true);
            else
            {
                EditorGUI.BeginChangeCheck();
                var orientationMatrix = TileEditor.TransformMatrixOnGUI(tilemap.orientationMatrix);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tilemap, "Tilemap property change");
                    tilemap.orientationMatrix = orientationMatrix;
                }
            }
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();

            DrawInfoView();
        }

        private void DrawInfoView()
        {
            m_ShowInfoFoldout.value = m_ShowInfo.target = EditorGUILayout.Foldout(m_ShowInfo.target, "Info", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowInfo.faded))
            {
                if (targets.Length == 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    var tileCount = tilemap.GetUsedTilesCount();
                    var spriteCount = tilemap.GetUsedSpritesCount();
                    if (usedTiles == null || usedTiles.Length != tileCount)
                        usedTiles = new TileBase[tileCount];
                    if (usedSprites == null || usedSprites.Length != spriteCount)
                        usedSprites = new Sprite[spriteCount];
                    tilemap.GetUsedTilesNonAlloc(usedTiles);
                    tilemap.GetUsedSpritesNonAlloc(usedSprites);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.usedTilesLabel, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    if (tileCount > 0)
                    {
                        foreach (var tile in usedTiles)
                        {
                            EditorGUILayout.ObjectField(tile, typeof(TileBase), false);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(Styles.noneUsedLabel);
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.usedSpritesLabel, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    if (spriteCount > 0)
                    {
                        foreach (var sprite in usedSprites)
                        {
                            EditorGUILayout.ObjectField(sprite, typeof(Sprite), false);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(Styles.noneUsedLabel);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Cannot show Info properties when multiple Tilemaps are selected.", MessageType.Info);
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        // Called from SceneView code using reflection
        private bool HasFrameBounds()
        {
            return true;
        }

        // Called from SceneView code using reflection
        private Bounds OnGetFrameBounds()
        {
            Bounds localBounds = tilemap.localFrameBounds;
            Bounds bounds = new Bounds(tilemap.transform.TransformPoint(localBounds.center), Vector3.zero);
            for (int i = 0; i < 8; ++i)
            {
                Vector3 extent = localBounds.extents;
                extent.x = (i & 1) == 0 ? -extent.x : extent.x;
                extent.y = (i & 2) == 0 ? -extent.y : extent.y;
                extent.z = (i & 4) == 0 ? -extent.z : extent.z;
                var worldPoint = tilemap.transform.TransformPoint(localBounds.center + extent);
                bounds.Encapsulate(worldPoint);
            }
            return bounds;
        }

        [MenuItem("CONTEXT/Tilemap/Refresh All Tiles")]
        static internal void RefreshAllTiles(MenuCommand item)
        {
            Tilemap tilemap = (Tilemap)item.context;
            tilemap.RefreshAllTiles();
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("CONTEXT/Tilemap/Compress Tilemap Bounds")]
        static internal void CompressBounds(MenuCommand item)
        {
            Tilemap tilemap = (Tilemap)item.context;
            Undo.RegisterCompleteObjectUndo(tilemap, "Compress Tilemap Bounds");
            tilemap.CompressBounds();
        }
    }
}
