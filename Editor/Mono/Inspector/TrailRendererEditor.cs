// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(TrailRenderer))]
    [CanEditMultipleObjects]
    internal class TrailRendererInspector : RendererEditorBase
    {
        private class Styles
        {
            public static GUIContent colorGradient = EditorGUIUtility.TrTextContent("Color", "The gradient describing the color along the trail.");
            public static GUIContent numCornerVertices = EditorGUIUtility.TrTextContent("Corner Vertices", "How many vertices to add for each corner.");
            public static GUIContent numCapVertices = EditorGUIUtility.TrTextContent("End Cap Vertices", "How many vertices to add at each end.");
            public static GUIContent alignment = EditorGUIUtility.TrTextContent("Alignment", "Trails can rotate to face their transform component or the camera. When using TransformZ mode, lines extrude along the XY plane of the Transform.");
            public static GUIContent textureMode = EditorGUIUtility.TrTextContent("Texture Mode", "Should the U coordinate be stretched or tiled?");
            public static GUIContent shadowBias = EditorGUIUtility.TrTextContent("Shadow Bias", "Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the trail width at each segment.");
            public static GUIContent generateLightingData = EditorGUIUtility.TrTextContent("Generate Lighting Data", "Toggle generation of normal and tangent data, for use in lit shaders.");
        }

        private LineRendererCurveEditor m_CurveEditor = new LineRendererCurveEditor();
        private SerializedProperty m_Time;
        private SerializedProperty m_MinVertexDistance;
        private SerializedProperty m_Autodestruct;
        private SerializedProperty m_Emitting;
        private SerializedProperty m_ColorGradient;
        private SerializedProperty m_NumCornerVertices;
        private SerializedProperty m_NumCapVertices;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_TextureMode;
        private SerializedProperty m_ShadowBias;
        private SerializedProperty m_GenerateLightingData;

        public override void OnEnable()
        {
            base.OnEnable();

            m_CurveEditor.OnEnable(serializedObject);
            m_Time = serializedObject.FindProperty("m_Time");
            m_MinVertexDistance = serializedObject.FindProperty("m_MinVertexDistance");
            m_Autodestruct = serializedObject.FindProperty("m_Autodestruct");
            m_Emitting = serializedObject.FindProperty("m_Emitting");
            m_ColorGradient = serializedObject.FindProperty("m_Parameters.colorGradient");
            m_NumCornerVertices = serializedObject.FindProperty("m_Parameters.numCornerVertices");
            m_NumCapVertices = serializedObject.FindProperty("m_Parameters.numCapVertices");
            m_Alignment = serializedObject.FindProperty("m_Parameters.alignment");
            m_TextureMode = serializedObject.FindProperty("m_Parameters.textureMode");
            m_ShadowBias = serializedObject.FindProperty("m_Parameters.shadowBias");
            m_GenerateLightingData = serializedObject.FindProperty("m_Parameters.generateLightingData");
        }

        public void OnDisable()
        {
            m_CurveEditor.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_CurveEditor.CheckCurveChangedExternally();
            m_CurveEditor.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Time);
            EditorGUILayout.PropertyField(m_MinVertexDistance);
            EditorGUILayout.PropertyField(m_Autodestruct);
            EditorGUILayout.PropertyField(m_Emitting);
            EditorGUILayout.PropertyField(m_ColorGradient, Styles.colorGradient);
            EditorGUILayout.PropertyField(m_NumCornerVertices, Styles.numCornerVertices);
            EditorGUILayout.PropertyField(m_NumCapVertices, Styles.numCapVertices);
            EditorGUILayout.PropertyField(m_Alignment, Styles.alignment);
            EditorGUILayout.PropertyField(m_TextureMode, Styles.textureMode);
            EditorGUILayout.PropertyField(m_GenerateLightingData, Styles.generateLightingData);
            EditorGUILayout.PropertyField(m_ShadowBias, Styles.shadowBias);

            DrawMaterials();
            LightingSettingsGUI(false);
            OtherSettingsGUI(true, false, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
