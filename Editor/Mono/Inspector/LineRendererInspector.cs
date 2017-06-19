// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(LineRenderer))]
    [CanEditMultipleObjects]
    internal class LineRendererInspector : RendererEditorBase
    {
        private class Styles
        {
            public static GUIContent colorGradient = EditorGUIUtility.TextContent("Color|The gradient describing the color along the line.");
            public static GUIContent numCornerVertices = EditorGUIUtility.TextContent("Corner Vertices|How many vertices to add for each corner.");
            public static GUIContent numCapVertices = EditorGUIUtility.TextContent("End Cap Vertices|How many vertices to add at each end.");
            public static GUIContent alignment = EditorGUIUtility.TextContent("Alignment|Lines can rotate to face their transform component or the camera. Note that when using Local mode, lines will face the XY plane of the Transform.");
            public static GUIContent textureMode = EditorGUIUtility.TextContent("Texture Mode|Should the U coordinate be stretched or tiled?");
            public static GUIContent generateLightingData = EditorGUIUtility.TextContent("Generate Lighting Data|Toggle generation of normal and tangent data, for use in lit shaders.");
        }

        private string[] m_ExcludedProperties;

        private LineRendererCurveEditor m_CurveEditor = new LineRendererCurveEditor();
        private SerializedProperty m_ColorGradient;
        private SerializedProperty m_NumCornerVertices;
        private SerializedProperty m_NumCapVertices;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_TextureMode;
        private SerializedProperty m_GenerateLightingData;

        public override void OnEnable()
        {
            base.OnEnable();

            List<string> excludedProperties = new List<string>();
            excludedProperties.Add("m_Parameters");
            excludedProperties.AddRange(Probes.GetFieldsStringArray());
            m_ExcludedProperties = excludedProperties.ToArray();

            m_CurveEditor.OnEnable(serializedObject);
            m_ColorGradient = serializedObject.FindProperty("m_Parameters.colorGradient");
            m_NumCornerVertices = serializedObject.FindProperty("m_Parameters.numCornerVertices");
            m_NumCapVertices = serializedObject.FindProperty("m_Parameters.numCapVertices");
            m_Alignment = serializedObject.FindProperty("m_Parameters.alignment");
            m_TextureMode = serializedObject.FindProperty("m_Parameters.textureMode");
            m_GenerateLightingData = serializedObject.FindProperty("m_Parameters.generateLightingData");

            InitializeProbeFields();
        }

        public void OnDisable()
        {
            m_CurveEditor.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(m_SerializedObject, m_ExcludedProperties);

            m_CurveEditor.CheckCurveChangedExternally();
            m_CurveEditor.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_ColorGradient, Styles.colorGradient);
            EditorGUILayout.PropertyField(m_NumCornerVertices, Styles.numCornerVertices);
            EditorGUILayout.PropertyField(m_NumCapVertices, Styles.numCapVertices);
            EditorGUILayout.PropertyField(m_Alignment, Styles.alignment);
            EditorGUILayout.PropertyField(m_TextureMode, Styles.textureMode);
            EditorGUILayout.PropertyField(m_GenerateLightingData, Styles.generateLightingData);

            EditorGUILayout.Space();

            RenderSortingLayerFields();

            m_Probes.OnGUI(targets, (Renderer)target, false);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
