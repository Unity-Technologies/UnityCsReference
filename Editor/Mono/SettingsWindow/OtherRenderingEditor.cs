// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(RenderSettings))]
    internal class OtherRenderingEditor : Editor
    {
        internal static class Styles
        {
            public static readonly GUIContent HaloStrength = EditorGUIUtility.TextContent("Halo Strength|Controls the visibility of the halo effect around lights in the Scene.");
            public static readonly GUIContent HaloTexture = EditorGUIUtility.TextContent("Halo Texture|Specifies the Texture used when drawing the halo effect around lights in the Scene");
            public static readonly GUIContent FlareStrength = EditorGUIUtility.TextContent("Flare Strength|Controls the visibility of lens flares from lights in the Scene.");
            public static readonly GUIContent FlareFadeSpeed = EditorGUIUtility.TextContent("Flare Fade Speed|Controls the time over which lens flares fade from view after initially appearing.");
            public static readonly GUIContent SpotCookie = EditorGUIUtility.TextContent("Spot Cookie|Specifies the Texture mask used to cast shadows, create silhouettes, or patterned illumination when using spot lights.");
        }

        protected SerializedProperty m_HaloStrength;
        protected SerializedProperty m_FlareStrength;
        protected SerializedProperty m_FlareFadeSpeed;
        protected SerializedProperty m_HaloTexture;
        protected SerializedProperty m_SpotCookie;

        public virtual void OnEnable()
        {
            m_HaloStrength = serializedObject.FindProperty("m_HaloStrength");
            m_FlareStrength = serializedObject.FindProperty("m_FlareStrength");
            m_FlareFadeSpeed = serializedObject.FindProperty("m_FlareFadeSpeed");
            m_HaloTexture = serializedObject.FindProperty("m_HaloTexture");
            m_SpotCookie = serializedObject.FindProperty("m_SpotCookie");
        }

        public virtual void OnDisable() {}

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_HaloTexture, Styles.HaloTexture);
            EditorGUILayout.Slider(m_HaloStrength, 0.0f, 1.0f, Styles.HaloStrength);

            EditorGUILayout.PropertyField(m_FlareFadeSpeed, Styles.FlareFadeSpeed);
            EditorGUILayout.Slider(m_FlareStrength, 0.0f, 1.0f, Styles.FlareStrength);

            EditorGUILayout.PropertyField(m_SpotCookie, Styles.SpotCookie);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
