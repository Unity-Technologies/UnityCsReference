// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorShaderStripping : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorShaderStripping, UxmlTraits> { }

        internal class Styles
        {
            public static readonly GUIContent shaderStrippingSettings = EditorGUIUtility.TrTextContent("Shader Stripping");

            public static readonly GUIContent lightmapModes = EditorGUIUtility.TrTextContent("Lightmap Modes");
            public static readonly GUIContent lightmapPlain = EditorGUIUtility.TrTextContent("Baked Non-Directional", "Include support for baked non-directional lightmaps.");
            public static readonly GUIContent lightmapDirCombined = EditorGUIUtility.TrTextContent("Baked Directional", "Include support for baked directional lightmaps.");
            public static readonly GUIContent lightmapKeepShadowMask = EditorGUIUtility.TrTextContent("Baked Shadowmask", "Include support for baked shadow occlusion.");
            public static readonly GUIContent lightmapKeepSubtractive = EditorGUIUtility.TrTextContent("Baked Subtractive", "Include support for baked subtractive lightmaps.");
            public static readonly GUIContent lightmapDynamicPlain = EditorGUIUtility.TrTextContent("Realtime Non-Directional", "Include support for realtime non-directional lightmaps.");
            public static readonly GUIContent lightmapDynamicDirCombined = EditorGUIUtility.TrTextContent("Realtime Directional", "Include support for realtime directional lightmaps.");
            public static readonly GUIContent lightmapFromScene = EditorGUIUtility.TrTextContent("Import From Current Scene", "Calculate lightmap modes used by the current scene.");

            public static readonly GUIContent fogModes = EditorGUIUtility.TrTextContent("Fog Modes");
            public static readonly GUIContent fogLinear = EditorGUIUtility.TrTextContent("Linear", "Include support for Linear fog.");
            public static readonly GUIContent fogExp = EditorGUIUtility.TrTextContent("Exponential", "Include support for Exponential fog.");
            public static readonly GUIContent fogExp2 = EditorGUIUtility.TrTextContent("Exponential Squared", "Include support for Exponential Squared fog.");
            public static readonly GUIContent fogFromScene = EditorGUIUtility.TrTextContent("Import From Current Scene", "Calculate fog modes used by the current scene.");

            public static readonly GUIContent instancingVariants = EditorGUIUtility.TrTextContent("Instancing Variants");
            public static readonly GUIContent brgVariants = EditorGUIUtility.TrTextContent("BatchRendererGroup Variants");
        }

        SerializedProperty m_LightmapStripping;
        SerializedProperty m_LightmapKeepPlain;
        SerializedProperty m_LightmapKeepDirCombined;
        SerializedProperty m_LightmapKeepDynamicPlain;
        SerializedProperty m_LightmapKeepDynamicDirCombined;
        SerializedProperty m_LightmapKeepShadowMask;
        SerializedProperty m_LightmapKeepSubtractive;
        SerializedProperty m_FogStripping;
        SerializedProperty m_FogKeepLinear;
        SerializedProperty m_FogKeepExp;
        SerializedProperty m_FogKeepExp2;
        SerializedProperty m_InstancingStripping;
        SerializedProperty m_BrgStripping;

        protected override void Initialize()
        {
            m_LightmapStripping = m_SerializedObject.FindProperty("m_LightmapStripping");
            m_LightmapKeepPlain = m_SerializedObject.FindProperty("m_LightmapKeepPlain");
            m_LightmapKeepDirCombined = m_SerializedObject.FindProperty("m_LightmapKeepDirCombined");
            m_LightmapKeepDynamicPlain = m_SerializedObject.FindProperty("m_LightmapKeepDynamicPlain");
            m_LightmapKeepDynamicDirCombined = m_SerializedObject.FindProperty("m_LightmapKeepDynamicDirCombined");
            m_LightmapKeepShadowMask = m_SerializedObject.FindProperty("m_LightmapKeepShadowMask");
            m_LightmapKeepSubtractive = m_SerializedObject.FindProperty("m_LightmapKeepSubtractive");
            m_FogStripping = m_SerializedObject.FindProperty("m_FogStripping");
            m_FogKeepLinear = m_SerializedObject.FindProperty("m_FogKeepLinear");
            m_FogKeepExp = m_SerializedObject.FindProperty("m_FogKeepExp");
            m_FogKeepExp2 = m_SerializedObject.FindProperty("m_FogKeepExp2");
            m_InstancingStripping = m_SerializedObject.FindProperty("m_InstancingStripping");
            m_BrgStripping = m_SerializedObject.FindProperty("m_BrgStripping");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.LabelField(Styles.shaderStrippingSettings, EditorStyles.boldLabel);

            bool calcLightmapStripping = false, calcFogStripping = false;

            EditorGUILayout.PropertyField(m_LightmapStripping, Styles.lightmapModes);

            if (m_LightmapStripping.intValue != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LightmapKeepPlain, Styles.lightmapPlain);
                EditorGUILayout.PropertyField(m_LightmapKeepDirCombined, Styles.lightmapDirCombined);
                EditorGUILayout.PropertyField(m_LightmapKeepDynamicPlain, Styles.lightmapDynamicPlain);
                EditorGUILayout.PropertyField(m_LightmapKeepDynamicDirCombined, Styles.lightmapDynamicDirCombined);
                EditorGUILayout.PropertyField(m_LightmapKeepShadowMask, Styles.lightmapKeepShadowMask);
                EditorGUILayout.PropertyField(m_LightmapKeepSubtractive, Styles.lightmapKeepSubtractive);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(GUIContent.Temp(" "), EditorStyles.miniButton);

                if (GUILayout.Button(Styles.lightmapFromScene, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    calcLightmapStripping = true;

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(m_FogStripping, Styles.fogModes);
            if (m_FogStripping.intValue != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FogKeepLinear, Styles.fogLinear);
                EditorGUILayout.PropertyField(m_FogKeepExp, Styles.fogExp);
                EditorGUILayout.PropertyField(m_FogKeepExp2, Styles.fogExp2);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(GUIContent.Temp(" "), EditorStyles.miniButton);

                if (GUILayout.Button(Styles.fogFromScene, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    calcFogStripping = true;

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(m_InstancingStripping, Styles.instancingVariants);
            EditorGUILayout.PropertyField(m_BrgStripping, Styles.brgVariants);

            if (check.changed)
                m_SerializedObject.ApplyModifiedProperties();

            // need to do these after ApplyModifiedProperties, since it changes their values from native code
            if (calcLightmapStripping)
                ShaderUtil.CalculateLightmapStrippingFromCurrentScene();
            if (calcFogStripping)
                ShaderUtil.CalculateFogStrippingFromCurrentScene();
        }
    }
}
