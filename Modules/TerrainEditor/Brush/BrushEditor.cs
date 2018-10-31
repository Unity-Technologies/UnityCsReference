// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Brush))]
    internal class BrushEditor : Editor
    {
        [SerializeField]
        protected Vector2 m_Pos;

        SerializedProperty m_Mask;
        SerializedProperty m_Falloff;
        SerializedProperty m_RadiusScale;

        bool m_HasChanged = true;
        Texture2D m_PreviewTexture = null;

        static class Styles
        {
            public static GUIContent maskTexture = EditorGUIUtility.TrTextContent("Mask texture");
        }


        protected virtual void OnEnable()
        {
            m_Mask = serializedObject.FindProperty("m_Mask");
            m_Falloff = serializedObject.FindProperty("m_Falloff");
            m_RadiusScale = serializedObject.FindProperty("m_RadiusScale");
        }

        bool IsAnyReadOnly()
        {
            foreach (Brush b in targets)
            {
                if (b.readOnly)
                    return true;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            if (IsAnyReadOnly())
            {
                EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent("One or more selected brushes are read-only."));
                return;
            }

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            Texture2D mask = (Texture2D)EditorGUILayout.ObjectField(Styles.maskTexture,
                (Texture2D)m_Mask.objectReferenceValue, typeof(Texture2D), false);
            if (mask == null)
            {
                mask = Brush.DefaultMask();
                m_HasChanged = true;
            }

            m_Mask.objectReferenceValue = mask;
            EditorGUILayout.CurveField(m_Falloff, Color.white, new Rect(0, 0, 1, 1));
            EditorGUILayout.PropertyField(m_RadiusScale);
            m_HasChanged |= EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            foreach (Brush b in targets)
            {
                b.SetDirty(m_HasChanged);
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            Texture2D mask = (Texture2D)m_Mask.objectReferenceValue;
            if (mask == null)
            {
                mask = Brush.DefaultMask();
                m_HasChanged = true;
            }

            int texWidth = Mathf.Min(mask.width, 256);
            int texHeight = Mathf.Min(mask.height, 256);

            if (m_HasChanged || m_PreviewTexture == null)
            {
                m_PreviewTexture = Brush.GenerateBrushTexture(mask, m_Falloff.animationCurveValue, m_RadiusScale.floatValue, texWidth, texHeight, true);
                m_HasChanged = false;
            }

            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            if (m_PreviewTexture.alphaIsTransparency)
                EditorGUI.DrawTextureTransparent(wantedRect, m_PreviewTexture);
            else
                EditorGUI.DrawPreviewTexture(wantedRect, m_PreviewTexture);

            m_Pos = PreviewGUI.EndScrollView();
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            Brush brush = AssetDatabase.LoadMainAssetAtPath(assetPath) as Brush;

            if (brush == null)
                return null;

            if (brush.m_Mask == null)
                brush.m_Mask = Brush.DefaultMask();
            PreviewHelpers.AdjustWidthAndHeightForStaticPreview(brush.m_Mask.width, brush.m_Mask.height, ref width, ref height);
            return Brush.GenerateBrushTexture(brush.m_Mask, brush.m_Falloff, brush.m_RadiusScale, width, height, true);
        }
    }
}
