// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class SpeedTree9ShaderGUI : ShaderGUI
    {
        private static class Styles
        {
            public static GUIContent colorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) and Opacity (A)");
            public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal", "Normal (RGB)");
            public static GUIContent extraMapText = EditorGUIUtility.TrTextContent("Extra", "Smoothness (R), Metallic (G), AO (B)");
            public static GUIContent subsurfaceMapText = EditorGUIUtility.TrTextContent("Subsurface", "Subsurface (RGB)");

            public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness value");
            public static GUIContent metallicText = EditorGUIUtility.TrTextContent("Metallic", "Metallic value");

            public static GUIContent twoSidedText = EditorGUIUtility.TrTextContent("Two-Sided", "Set this material to render as two-sided");
            public static GUIContent hueVariationText = EditorGUIUtility.TrTextContent("Hue Variation", "Hue variation Color (RGB) and Amount (A)");
            public static GUIContent normalMappingText = EditorGUIUtility.TrTextContent("Normal Map", "Enable normal mapping");
            public static GUIContent subsurfaceText = EditorGUIUtility.TrTextContent("Subsurface", "Enable subsurface scattering");
            public static GUIContent subsurfaceIndirectText = EditorGUIUtility.TrTextContent("Indirect Subsurface", "Scalar on subsurface from indirect light");

            public static GUIContent windSharedText = EditorGUIUtility.TrTextContent("Shared Motion", "Wind quality setting");
            public static GUIContent windBranch1Text = EditorGUIUtility.TrTextContent("Branch1 Motion", "Wind quality setting");
            public static GUIContent windBranch2Text = EditorGUIUtility.TrTextContent("Branch2 Motion", "Wind quality setting");
            public static GUIContent windRippleText = EditorGUIUtility.TrTextContent("Ripple Motion", "Wind quality setting");
            public static GUIContent windShimmerText = EditorGUIUtility.TrTextContent("Shimmer Motion", "Wind quality setting");

            public static GUIContent billboardText = EditorGUIUtility.TrTextContent("Billboard", "Enable billboard features (crossfading, etc.)");
            public static GUIContent billboardShadowFadeText = EditorGUIUtility.TrTextContent("Shadow Fade", "Fade shadow effect on billboards");

            public static GUIContent primaryMapsText = EditorGUIUtility.TrTextContent("Maps");
            public static GUIContent optionsText = EditorGUIUtility.TrTextContent("Options");
            public static GUIContent advancedText = EditorGUIUtility.TrTextContent("Advanced Options");
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0.0f;

            {
                GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);

                // color
                var colorTexProp = ShaderGUI.FindProperty("_MainTex", properties);
                var colorProp = ShaderGUI.FindProperty("_ColorTint", properties);
                materialEditor.TexturePropertySingleLine(Styles.colorText, colorTexProp, null, colorProp);

                // normal
                var normalTexProp = ShaderGUI.FindProperty("_NormalMap", properties);
                materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalTexProp);

                // extra
                var extraTexProp = ShaderGUI.FindProperty("_ExtraTex", properties);
                materialEditor.TexturePropertySingleLine(Styles.extraMapText, extraTexProp, null);
                if (extraTexProp.textureValue == null)
                {
                    var glossProp = ShaderGUI.FindProperty("_Glossiness", properties);
                    materialEditor.ShaderProperty(glossProp, Styles.smoothnessText, 2);
                    var metallicProp = ShaderGUI.FindProperty("_Metallic", properties);
                    materialEditor.ShaderProperty(metallicProp, Styles.metallicText, 2);
                }

                // subsurface
                var ssTexProp = ShaderGUI.FindProperty("_SubsurfaceTex", properties);
                var ssProp = ShaderGUI.FindProperty("_SubsurfaceColor", properties);
                materialEditor.TexturePropertySingleLine(Styles.subsurfaceMapText, ssTexProp, null, ssProp);

                // other options
                EditorGUILayout.Space();
                GUILayout.Label(Styles.optionsText, EditorStyles.boldLabel);

                MakeAlignedProperty(FindProperty("_TwoSided", properties), Styles.twoSidedText, materialEditor, true);
                MakeCheckedProperty(FindProperty("_HueVariationKwToggle", properties), FindProperty("_HueVariationColor", properties), Styles.hueVariationText, materialEditor);
                MakeAlignedProperty(FindProperty("_NormalMapKwToggle", properties), Styles.normalMappingText, materialEditor, true);

                // subsurface
                var subsurfaceToggle = FindProperty("_SubsurfaceKwToggle", properties);
                MakeAlignedProperty(subsurfaceToggle, Styles.subsurfaceText, materialEditor, true);
                if (subsurfaceToggle.floatValue > 0.0f)
                {
                    var sssIndirectProp = ShaderGUI.FindProperty("_SubsurfaceIndirect", properties);
                    materialEditor.ShaderProperty(sssIndirectProp, Styles.subsurfaceIndirectText, 2);
                }

                // billboard
                var billboardToggle = FindProperty("_BillboardKwToggle", properties);
                bool hasBillboard = billboardToggle.floatValue > 0.0f;
                MakeAlignedProperty(billboardToggle, Styles.billboardText, materialEditor, true);
                if (hasBillboard)
                {
                    var prop = ShaderGUI.FindProperty("_BillboardShadowFade", properties);
                    materialEditor.ShaderProperty(prop, Styles.billboardShadowFadeText, 2);
                }

                // leaf facing
                MaterialProperty propLeafFacing = FindProperty("_LeafFacingKwToggle", properties);
                MakeAlignedProperty(propLeafFacing, EditorGUIUtility.TrTextContent("Leaf Facing", "Toggles the effect that renders the leaves facing the camera."), materialEditor, true);

                // wind
                MaterialProperty propWindShared  = FindProperty("_WIND_SHARED", properties);
                MaterialProperty propWindBranch1 = !hasBillboard ? FindProperty("_WIND_BRANCH1", properties) : null;
                MaterialProperty propWindBranch2 = !hasBillboard ? FindProperty("_WIND_BRANCH2", properties) : null;
                MaterialProperty propWindRipple  = !hasBillboard ? FindProperty("_WIND_RIPPLE" , properties) : null;
                MaterialProperty propWindShimmer = !hasBillboard ? FindProperty("_WIND_SHIMMER", properties) : null;
                const bool DOUBLE_WIDE = true;

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
                MakeAlignedProperty(propWindShared, Styles.windSharedText, materialEditor, DOUBLE_WIDE);
                if (!hasBillboard) // 3D-only wind props
                {
                    MakeAlignedProperty(propWindBranch1, Styles.windBranch1Text, materialEditor, DOUBLE_WIDE);
                    MakeAlignedProperty(propWindBranch2, Styles.windBranch2Text, materialEditor, DOUBLE_WIDE);
                    MakeAlignedProperty(propWindRipple, Styles.windRippleText, materialEditor, DOUBLE_WIDE);
                    if (propWindRipple.floatValue > 0.0f)
                    {
                        MakeAlignedProperty(propWindShimmer, Styles.windShimmerText, materialEditor, DOUBLE_WIDE);
                    }
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        static void MakeAlignedProperty(MaterialProperty prop, GUIContent text, MaterialEditor materialEditor, bool doubleWide = false)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2.0f);
            r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2.0f : 1.0f);

            materialEditor.ShaderProperty(r, prop, text);
        }

        static void MakeCheckedProperty(MaterialProperty keywordToggleProp, MaterialProperty prop, GUIContent text, MaterialEditor materialEditor, bool doubleWide = false)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2.0f);
            r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth / 2;

            materialEditor.ShaderProperty(r, keywordToggleProp, text);

            using (new EditorGUI.DisabledScope(keywordToggleProp.floatValue == 0.0f))
            {
                r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * (doubleWide ? 2.0f : 1.0f);
                r.x += EditorGUIUtility.fieldWidth / 2;

                materialEditor.ShaderProperty(r, prop, " ");
            }
        }

        public override void ValidateMaterial(Material material)
        {
            SetKeyword(material, "EFFECT_EXTRA_TEX", material.GetTexture("_ExtraTex"));
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}
