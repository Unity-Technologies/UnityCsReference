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
        SerializedProperty m_BlackWhiteRemapMin;
        SerializedProperty m_BlackWhiteRemapMax;
        SerializedProperty m_InvertRemapRange;

        bool m_HasChanged = true;
        Texture2D m_PreviewTexture = null;

        static class Styles
        {
            public static GUIContent readonlyText = EditorGUIUtility.TrTextContent("One or more selected Brushes are read-only.");
            public static GUIContent maskTexture = EditorGUIUtility.TrTextContent("Mask Texture", "Texture Red channel controls the shape of the Brush");
            public static GUIContent falloff = EditorGUIUtility.TrTextContent("Falloff Curve", "Controls the Brush falloff curve, over the distance from the center of the Brush.");
            public static GUIContent radiusScale = EditorGUIUtility.TrTextContent("Falloff Radius Scale", "Controls the radius of the falloff curve.");
            public static GUIContent remap = EditorGUIUtility.TrTextContent("Brush Remap", "Remaps the grayscale values of the Brush");
            public static GUIContent remapInvert = EditorGUIUtility.TrTextContent("Brush Invert", "Inverts the Brush shape, swapping black and white");
        }


        protected virtual void OnEnable()
        {
            m_Mask = serializedObject.FindProperty("m_Mask");
            m_Falloff = serializedObject.FindProperty("m_Falloff");
            m_RadiusScale = serializedObject.FindProperty("m_RadiusScale");
            m_BlackWhiteRemapMin = serializedObject.FindProperty("m_BlackWhiteRemapMin");
            m_BlackWhiteRemapMax = serializedObject.FindProperty("m_BlackWhiteRemapMax");
            m_InvertRemapRange = serializedObject.FindProperty("m_InvertRemapRange");
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
                EditorGUILayout.HelpBox(Styles.readonlyText);
                return;
            }

            serializedObject.Update();

            bool brushChanged = false;
            EditorGUI.BeginChangeCheck();
            Texture2D origMask = (Texture2D)m_Mask.objectReferenceValue;
            Texture2D mask = (Texture2D)EditorGUILayout.ObjectField(Styles.maskTexture,
                origMask, typeof(Texture2D), false);
            if (mask == null)
            {
                mask = Brush.DefaultMask();
                brushChanged = true;
            }

            if (origMask != mask)
                m_Mask.objectReferenceValue = mask;

            EditorGUILayout.CurveField(m_Falloff, Color.white, new Rect(0, 0, 1, 1), Styles.falloff);
            EditorGUILayout.PropertyField(m_RadiusScale, Styles.radiusScale);

            float blackWhiteRemapMin = m_BlackWhiteRemapMin.floatValue;
            float blackWhiteRemapMax = m_BlackWhiteRemapMax.floatValue;
            EditorGUILayout.MinMaxSlider(Styles.remap, ref blackWhiteRemapMin, ref blackWhiteRemapMax, 0.0f, 1.0f);
            if (m_BlackWhiteRemapMin.floatValue != blackWhiteRemapMin || m_BlackWhiteRemapMax.floatValue != blackWhiteRemapMax)
            {
                m_BlackWhiteRemapMin.floatValue = blackWhiteRemapMin;
                m_BlackWhiteRemapMax.floatValue = blackWhiteRemapMax;
                brushChanged = true;
            }
            EditorGUILayout.PropertyField(m_InvertRemapRange, Styles.remapInvert);

            brushChanged |= EditorGUI.EndChangeCheck();
            m_HasChanged |= brushChanged;

            serializedObject.ApplyModifiedProperties();

            if (brushChanged)
            {
                foreach (Brush b in targets)
                {
                    b.SetDirty(true);
                }
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
                m_PreviewTexture = Brush.GenerateBrushTexture(mask, m_Falloff.animationCurveValue, m_RadiusScale.floatValue, m_BlackWhiteRemapMin.floatValue, m_BlackWhiteRemapMax.floatValue, m_InvertRemapRange.boolValue,  texWidth, texHeight, true);
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
            return Brush.GenerateBrushTexture(brush.m_Mask, brush.m_Falloff, brush.m_RadiusScale, brush.m_BlackWhiteRemapMin, brush.m_BlackWhiteRemapMax, brush.m_InvertRemapRange, width, height, true);
        }
    }
}
