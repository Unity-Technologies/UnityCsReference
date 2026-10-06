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
            public static readonly GUIContent colorText = L10n.TextContent("Color", "Color (RGB) and Opacity (A)", null, null);
            public static readonly GUIContent normalMapText = L10n.TextContent("Normal", "Normal (RGB)", null, null);
            public static readonly GUIContent extraMapText = L10n.TextContent("Extra", "Smoothness (R), Metallic (G), AO (B)", null, null);
            public static readonly GUIContent subsurfaceMapText = L10n.TextContent("Subsurface", "Subsurface (RGB)", null, null);

            public static readonly GUIContent smoothnessText = L10n.TextContent("Smoothness", "Smoothness value", null, null);
            public static readonly GUIContent metallicText = L10n.TextContent("Metallic", "Metallic value", null, null);

            public static readonly GUIContent twoSidedText = L10n.TextContent("Two-Sided", "Set this material to render as two-sided", null, null);
            public static readonly GUIContent hueVariationText = L10n.TextContent("Hue Variation", "Hue variation Color (RGB) and Amount (A)", null, null);
            public static readonly GUIContent normalMappingText = L10n.TextContent("Normal Map", "Enable normal mapping", null, null);
            public static readonly GUIContent subsurfaceText = L10n.TextContent("Subsurface", "Enable subsurface scattering", null, null);
            public static readonly GUIContent subsurfaceIndirectText = L10n.TextContent("Indirect Subsurface", "Scalar on subsurface from indirect light", null, null);

            public static readonly GUIContent windSharedText = L10n.TextContent("Shared Motion", "Wind quality setting", null, null);
            public static readonly GUIContent windBranch1Text = L10n.TextContent("Branch1 Motion", "Wind quality setting", null, null);
            public static readonly GUIContent windBranch2Text = L10n.TextContent("Branch2 Motion", "Wind quality setting", null, null);
            public static readonly GUIContent windRippleText = L10n.TextContent("Ripple Motion", "Wind quality setting", null, null);
            public static readonly GUIContent windShimmerText = L10n.TextContent("Shimmer Motion", "Wind quality setting", null, null);

            public static readonly GUIContent billboardText = L10n.TextContent("Billboard", "Enable billboard features (crossfading, etc.)", null, null);
            public static readonly GUIContent billboardShadowFadeText = L10n.TextContent("Shadow Fade", "Fade shadow effect on billboards", null, null);

            public static readonly GUIContent primaryMapsText = L10n.TextContent("Maps", null, null, null);
            public static readonly GUIContent optionsText = L10n.TextContent("Options", null, null, null);
            public static readonly GUIContent advancedText = L10n.TextContent("Advanced Options", null, null, null);
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
                MakeAlignedProperty(propLeafFacing, L10n.TextContent("Leaf Facing", "Toggles the effect that renders the leaves facing the camera.", null, null), materialEditor, true);

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
