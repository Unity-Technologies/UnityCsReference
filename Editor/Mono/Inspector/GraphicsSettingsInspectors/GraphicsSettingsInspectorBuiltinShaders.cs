// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorBuiltinShaders : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorBuiltinShaders, UxmlTraits> { }
        internal class Styles
        {
            public static readonly GUIContent deferredString = EditorGUIUtility.TrTextContent("Deferred", "Shader used for Deferred Shading.");
            public static readonly GUIContent deferredReflString = EditorGUIUtility.TrTextContent("Deferred Reflections", "Shader used for Deferred reflection probes.");
            public static readonly GUIContent screenShadowsString = EditorGUIUtility.TrTextContent("Screen Space Shadows", "Shader used for screen-space cascaded shadows.");
            public static readonly GUIContent depthNormalsString = EditorGUIUtility.TrTextContent("Depth Normals", "Shader used for depth and normals texture when enabled on a Camera.");

            public static readonly GUIContent motionVectorsString =
                EditorGUIUtility.TrTextContent("Motion Vectors", "Shader for generation of Motion Vectors when the rendering camera has renderMotionVectors set to true.");

            public static readonly GUIContent lightHaloString = EditorGUIUtility.TrTextContent("Light Halo", "Default Shader used for light halos.");
            public static readonly GUIContent lensFlareString = EditorGUIUtility.TrTextContent("Lens Flare", "Default Shader used for lens flares.");
        }

        public override bool BuiltinOnly => true;

        BuiltinShaderSettings m_Deferred;
         BuiltinShaderSettings m_DeferredReflections;
         BuiltinShaderSettings m_ScreenSpaceShadows;
         BuiltinShaderSettings m_DepthNormals;
         BuiltinShaderSettings m_MotionVectors;
         BuiltinShaderSettings m_LightHalo;
         BuiltinShaderSettings m_LensFlare;

        protected override void Initialize()
        {
            m_Deferred = new BuiltinShaderSettings(Styles.deferredString, "m_Deferred", m_SerializedObject);
            m_DeferredReflections = new BuiltinShaderSettings(Styles.deferredReflString, "m_DeferredReflections", m_SerializedObject);
            m_ScreenSpaceShadows = new BuiltinShaderSettings(Styles.screenShadowsString, "m_ScreenSpaceShadows", m_SerializedObject);
            m_DepthNormals = new BuiltinShaderSettings(Styles.depthNormalsString, "m_DepthNormals", m_SerializedObject);
            m_MotionVectors = new BuiltinShaderSettings(Styles.motionVectorsString, "m_MotionVectors", m_SerializedObject);
            m_LightHalo = new BuiltinShaderSettings(Styles.lightHaloString, "m_LightHalo", m_SerializedObject);
            m_LensFlare = new BuiltinShaderSettings(Styles.lensFlareString, "m_LensFlare", m_SerializedObject);

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();
            using var settingsScope = new LabelWidthScope();
            using var wideScreenScope = new WideScreenScope(this);
            m_Deferred.DoGUI();

            // deferred reflections being off affects forward vs deferred style probe rendering;
            // need to reload shaders for new platform macro to live update
            using (var internalCheck = new EditorGUI.ChangeCheckScope())
            {
                m_DeferredReflections.DoGUI();
                if (internalCheck.changed)
                    ShaderUtil.ReloadAllShaders();
            }

            m_ScreenSpaceShadows.DoGUI();
            m_DepthNormals.DoGUI();
            m_MotionVectors.DoGUI();
            m_LightHalo.DoGUI();
            m_LensFlare.DoGUI();

            if (check.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }

        internal readonly struct BuiltinShaderSettings
        {
            internal enum BuiltinShaderMode
            {
                None = 0,
                Builtin,
                Custom
            }

            readonly SerializedProperty m_Mode;
            readonly SerializedProperty m_Shader;
            readonly GUIContent m_Label;

            internal BuiltinShaderSettings(GUIContent label, string name, SerializedObject serializedObject)
            {
                m_Mode = serializedObject.FindProperty(name + ".m_Mode");
                m_Shader = serializedObject.FindProperty(name + ".m_Shader");
                m_Label = label;
            }

            internal void DoGUI()
            {
                EditorGUILayout.PropertyField(m_Mode, m_Label);
                if (m_Mode.intValue == (int)BuiltinShaderMode.Custom)
                    EditorGUILayout.PropertyField(m_Shader);
            }
        }
    }
}
