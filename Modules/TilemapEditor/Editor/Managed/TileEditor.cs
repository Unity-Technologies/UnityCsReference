// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditor
{
    [CustomEditor(typeof(Tile))]
    [CanEditMultipleObjects]
    internal class TileEditor : Editor
    {
        private const float k_PreviewWidth = 32;
        private const float k_PreviewHeight = 32;

        private SerializedProperty m_Color;
        private SerializedProperty m_ColliderType;
        private SerializedProperty m_Sprite;

        private Tile tile
        {
            get { return (target as Tile); }
        }

        private static class Styles
        {
            public static readonly GUIContent invalidMatrixLabel = EditorGUIUtility.TextContent("Invalid Matrix|No valid Position / Rotation / Scale components available for this matrix");
            public static readonly GUIContent resetMatrixLabel = EditorGUIUtility.TextContent("Reset Matrix");
            public static readonly GUIContent previewLabel = EditorGUIUtility.TextContent("Preview|Preview of tile with attributes set");

            public static readonly GUIContent positionLabel = EditorGUIUtility.TextContent("Position");
            public static readonly GUIContent rotationLabel = EditorGUIUtility.TextContent("Rotation");
            public static readonly GUIContent scaleLabel = EditorGUIUtility.TextContent("Scale");
        }

        public void OnEnable()
        {
            m_Color = serializedObject.FindProperty("m_Color");
            m_ColliderType = serializedObject.FindProperty("m_ColliderType");
            m_Sprite = serializedObject.FindProperty("m_Sprite");
        }

        public override void OnInspectorGUI()
        {
            DoTilePreview(tile.sprite, tile.color, Matrix4x4.identity);

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Sprite);
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_ColliderType);

            serializedObject.ApplyModifiedProperties();
        }

        public static void DoTilePreview(Sprite sprite, Color color, Matrix4x4 transform)
        {
            if (sprite == null)
                return;

            Rect guiRect = EditorGUILayout.GetControlRect(false, k_PreviewHeight);
            guiRect = EditorGUI.PrefixLabel(guiRect, new GUIContent(Styles.previewLabel));
            Rect previewRect = new Rect(guiRect.xMin, guiRect.yMin, k_PreviewWidth, k_PreviewHeight);
            Rect borderRect = new Rect(guiRect.xMin - 1, guiRect.yMin - 1, k_PreviewWidth + 2, k_PreviewHeight + 2);

            if (Event.current.type == EventType.Repaint)
                EditorStyles.textField.Draw(borderRect, false, false, false, false);

            Texture2D texture = SpriteUtility.RenderStaticPreview(sprite, color, 32, 32, transform);
            EditorGUI.DrawTextureTransparent(previewRect, texture, ScaleMode.StretchToFill);
        }

        public static Matrix4x4 TransformMatrixOnGUI(Matrix4x4 matrix)
        {
            Matrix4x4 val = matrix;
            if (matrix.ValidTRS())
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = Round(matrix.GetColumn(3), 3);
                Vector3 euler = Round(matrix.rotation.eulerAngles, 3);
                Vector3 scale = Round(matrix.lossyScale, 3);
                pos = EditorGUILayout.Vector3Field(Styles.positionLabel, pos);
                euler = EditorGUILayout.Vector3Field(Styles.rotationLabel, euler);
                scale = EditorGUILayout.Vector3Field(Styles.scaleLabel, scale);

                if (EditorGUI.EndChangeCheck() && scale.x != 0f && scale.y != 0f && scale.z != 0f)
                {
                    val = Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale);
                }
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Label(Styles.invalidMatrixLabel);
                if (GUILayout.Button(Styles.resetMatrixLabel))
                {
                    val = Matrix4x4.identity;
                }
                GUILayout.EndVertical();
            }
            return val;
        }

        private static Vector3 Round(Vector3 value, int digits)
        {
            float mult = Mathf.Pow(10.0f, (float)digits);
            return new Vector3(
                Mathf.Round(value.x * mult) / mult,
                Mathf.Round(value.y * mult) / mult,
                Mathf.Round(value.z * mult) / mult
                );
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            return SpriteUtility.RenderStaticPreview(tile.sprite, tile.color, width, height);
        }
    }
}
