// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshRenderer))]
    [CanEditMultipleObjects]
    internal class MeshRendererEditor : RendererEditorBase
    {
        class Styles
        {
            public static readonly GUIContent MaterialWarning = EditorGUIUtility.TrTextContent("This renderer has more materials than the Mesh has submeshes. Multiple materials will be applied to the same submesh, which costs performance. Consider using multiple shader passes.");
            public static readonly GUIContent StaticBatchingWarning = EditorGUIUtility.TrTextContent("This renderer is statically batched and uses an instanced shader at the same time. Instancing will be disabled in such a case. Consider disabling static batching if you want it to be instanced.");

            public static readonly GUIContent ProbeSettings = EditorGUIUtility.TrTextContent("Probes");
            public static readonly GUIContent OtherSettings = EditorGUIUtility.TrTextContent("Additional Settings");

            public static readonly GUIContent MotionVectors = EditorGUIUtility.TrTextContent("Motion Vectors", "Specifies whether the Mesh Renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture.");
        }

        private SerializedProperty m_Materials;
        private SerializedProperty m_MotionVectors;
        private LightingSettingsInspector m_Lighting;

        private SavedBool m_ShowProbeSettings;
        private SavedBool m_ShowOtherSettings;

        private SerializedObject m_GameObjectsSerializedObject;
        private SerializedProperty m_GameObjectStaticFlags;

        public override void OnEnable()
        {
            // Since we are not doing anything if we are not displayed in the inspector, early out. This help keeps multi selection snappier.
            if (hideInspector)
                return;

            base.OnEnable();

            m_Materials = serializedObject.FindProperty("m_Materials");
            m_MotionVectors = serializedObject.FindProperty("m_MotionVectors");

            m_GameObjectsSerializedObject = new SerializedObject(targets.Select(t => ((MeshRenderer)t).gameObject).ToArray());
            m_GameObjectStaticFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");

            m_ShowProbeSettings = new SavedBool($"{target.GetType()}.ShowProbeSettings", true);
            m_ShowOtherSettings = new SavedBool($"{target.GetType()}.ShowOtherSettings", true);

            m_Lighting = new LightingSettingsInspector(serializedObject);
            m_Lighting.showLightingSettings = new SavedBool($"{target.GetType()}.ShowLightingSettings", true);
            m_Lighting.showLightmapSettings = new SavedBool($"{target.GetType()}.ShowLightmapSettings", true);
            m_Lighting.showBakedLightmap = new SavedBool($"{target.GetType()}.ShowBakedLightmapSettings", false);
            m_Lighting.showRealtimeLightmap = new SavedBool($"{target.GetType()}.ShowRealtimeLightmapSettings", false);

            InitializeProbeFields();

            Lightmapping.lightingDataUpdated += LightingDataUpdatedRepaint;
        }

        public void OnDisable()
        {
            Lightmapping.lightingDataUpdated -= LightingDataUpdatedRepaint;
        }

        private void LightingFieldsGUI()
        {
            m_Lighting.RenderMeshSettings(true);

            m_ShowProbeSettings.value = EditorGUILayout.Foldout(m_ShowProbeSettings.value, Styles.ProbeSettings, true);

            if (m_ShowProbeSettings.value)
            {
                EditorGUI.indentLevel += 1;
                RenderProbeFields();
                EditorGUI.indentLevel -= 1;
            }
        }

        private void LightingDataUpdatedRepaint()
        {
            if (m_Lighting.showLightmapSettings)
            {
                Repaint();
            }
        }

        private void OtherSettingsGUI()
        {
            m_ShowOtherSettings.value = EditorGUILayout.Foldout(m_ShowOtherSettings.value, Styles.OtherSettings, true);

            if (m_ShowOtherSettings.value)
            {
                EditorGUI.indentLevel++;

                if (SupportedRenderingFeatures.active.motionVectors)
                    EditorGUILayout.PropertyField(m_MotionVectors, Styles.MotionVectors, true);

                RenderRenderingLayer();

                RenderRendererPriority();

                CullDynamicFieldGUI();

                EditorGUI.indentLevel--;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Evaluate displayMaterialWarning before drawing properties to avoid mismatched layout group
            bool displayMaterialWarning = false;

            if (!m_Materials.hasMultipleDifferentValues)
            {
                MeshFilter mf = ((MeshRenderer)serializedObject.targetObject).GetComponent<MeshFilter>();
                displayMaterialWarning = mf != null && mf.sharedMesh != null && m_Materials.arraySize > mf.sharedMesh.subMeshCount;
            }

            EditorGUILayout.PropertyField(m_Materials, true);

            if (!m_Materials.hasMultipleDifferentValues && displayMaterialWarning)
            {
                EditorGUILayout.HelpBox(Styles.MaterialWarning.text, MessageType.Warning, true);
            }

            if (ShaderUtil.MaterialsUseInstancingShader(m_Materials))
            {
                m_GameObjectsSerializedObject.Update();

                if (!m_GameObjectStaticFlags.hasMultipleDifferentValues && ((StaticEditorFlags)m_GameObjectStaticFlags.intValue & StaticEditorFlags.BatchingStatic) != 0)
                {
                    EditorGUILayout.HelpBox(Styles.StaticBatchingWarning.text, MessageType.Warning, true);
                }
            }

            LightingFieldsGUI();

            OtherSettingsGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
