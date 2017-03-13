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
            public static readonly GUIContent FogWarning = EditorGUIUtility.TextContent("Fog has no effect on opaque objects when using Deferred Shading rendering. Use the Global Fog image effect instead, which supports opaque objects.");
            public static readonly GUIContent FogDensity = EditorGUIUtility.TextContent("Density|Controls the density of the fog effect in the Scene when using Exponential or Exponential Squared modes.");
            public static readonly GUIContent FogLinearStart = EditorGUIUtility.TextContent("Start|Controls the distance from the camera where the fog will start in the Scene.");
            public static readonly GUIContent FogLinearEnd = EditorGUIUtility.TextContent("End|Controls the distance from the camera where the fog will completely obscure objects in the Scene.");
            public static readonly GUIContent FogEnable = EditorGUIUtility.TextContent("Fog|Specifies whether fog is used in the Scene or not.");
            public static readonly GUIContent FogColor = EditorGUIUtility.TextContent("Color|Controls the color of that fog drawn in the Scene.");
            public static readonly GUIContent FogMode = EditorGUIUtility.TextContent("Mode|Controls the mathematical function determining the way fog accumulates with distance from the camera. Options are Linear, Exponential, and Exponential Squared.");
        }

        protected SerializedProperty m_Fog;
        protected SerializedProperty m_FogColor;
        protected SerializedProperty m_FogMode;
        protected SerializedProperty m_FogDensity;
        protected SerializedProperty m_LinearFogStart;
        protected SerializedProperty m_LinearFogEnd;

        public virtual void OnEnable()
        {
            m_Fog = serializedObject.FindProperty("m_Fog");
            m_FogColor = serializedObject.FindProperty("m_FogColor");
            m_FogMode = serializedObject.FindProperty("m_FogMode");
            m_FogDensity = serializedObject.FindProperty("m_FogDensity");
            m_LinearFogStart = serializedObject.FindProperty("m_LinearFogStart");
            m_LinearFogEnd = serializedObject.FindProperty("m_LinearFogEnd");
        }

        public virtual void OnDisable() {}

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
