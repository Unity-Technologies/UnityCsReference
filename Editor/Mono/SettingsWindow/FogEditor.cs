// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(RenderSettings))]
    internal class FogEditor : Editor
    {
        internal class Styles
        {
            public static readonly GUIContent FogWarning = EditorGUIUtility.TrTextContent("Fog has no effect on opaque objects when using Deferred Shading rendering. Use the Global Fog image effect instead, which supports opaque objects.");
            public static readonly GUIContent FogDensity = EditorGUIUtility.TrTextContent("Density", "Controls the density of the fog effect in the Scene when using Exponential or Exponential Squared modes.");
            public static readonly GUIContent FogLinearStart = EditorGUIUtility.TrTextContent("Start", "Controls the distance from the camera where the fog will start in the Scene.");
            public static readonly GUIContent FogLinearEnd = EditorGUIUtility.TrTextContent("End", "Controls the distance from the camera where the fog will completely obscure objects in the Scene.");
            public static readonly GUIContent FogEnable = EditorGUIUtility.TrTextContent("Fog", "Specifies whether fog is used in the Scene or not.");
            public static readonly GUIContent FogColor = EditorGUIUtility.TrTextContent("Color", "Controls the color of that fog drawn in the Scene.");
            public static readonly GUIContent FogMode = EditorGUIUtility.TrTextContent("Mode", "Controls the mathematical function determining the way fog accumulates with distance from the camera. Options are Linear, Exponential, and Exponential Squared.");
        }

        protected SerializedProperty m_Fog;
        protected SerializedProperty m_FogColor;
        protected SerializedProperty m_FogMode;
        protected SerializedProperty m_FogDensity;
        protected SerializedProperty m_LinearFogStart;
        protected SerializedProperty m_LinearFogEnd;

        protected SerializedObject m_RenderSettings;

        SerializedObject renderSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_RenderSettings == null || m_RenderSettings.targetObject != RenderSettings.GetRenderSettings())
                {
                    m_RenderSettings = new SerializedObject(RenderSettings.GetRenderSettings());

                    m_Fog = m_RenderSettings.FindProperty("m_Fog");
                    m_FogColor = m_RenderSettings.FindProperty("m_FogColor");
                    m_FogMode = m_RenderSettings.FindProperty("m_FogMode");
                    m_FogDensity = m_RenderSettings.FindProperty("m_FogDensity");
                    m_LinearFogStart = m_RenderSettings.FindProperty("m_LinearFogStart");
                    m_LinearFogEnd = m_RenderSettings.FindProperty("m_LinearFogEnd");
                }

                return m_RenderSettings;
            }
        }

        public override void OnInspectorGUI()
        {
            renderSettings.Update();

            EditorGUILayout.PropertyField(m_Fog, Styles.FogEnable);
            if (m_Fog.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FogColor, Styles.FogColor);
                EditorGUILayout.PropertyField(m_FogMode, Styles.FogMode);

                if ((FogMode)m_FogMode.intValue != FogMode.Linear)
                {
                    EditorGUILayout.PropertyField(m_FogDensity, Styles.FogDensity);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_LinearFogStart, Styles.FogLinearStart);
                    EditorGUILayout.PropertyField(m_LinearFogEnd, Styles.FogLinearEnd);
                }

                if (SceneView.IsUsingDeferredRenderingPath())
                    EditorGUILayout.HelpBox(Styles.FogWarning.text, MessageType.Info);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            renderSettings.ApplyModifiedProperties();
        }
    }
}
