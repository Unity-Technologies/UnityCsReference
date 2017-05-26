// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;
using UnityEngineInternal;
using UnityEditor.AnimatedValues;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshRenderer))]
    [CanEditMultipleObjects]
    internal class MeshRendererEditor : RendererEditorBase
    {
        class Styles
        {
            public static readonly string MaterialWarning = "This renderer has more materials than the Mesh has submeshes. Multiple materials will be applied to the same submesh, which costs performance. Consider using multiple shader passes.";
            public static readonly string StaticBatchingWarning = "This renderer is statically batched and uses an instanced shader at the same time. Instancing will be disabled in such a case. Consider disabling static batching if you want it to be instanced.";
        }

        private SerializedProperty m_Materials;
        private LightingSettingsInspector m_Lighting;

        private const string kDisplayLightingKey = "MeshRendererEditor.Lighting.ShowSettings";
        private const string kDisplayLightmapKey = "MeshRendererEditor.Lighting.ShowLightmapSettings";
        private const string kDisplayChartingKey = "MeshRendererEditor.Lighting.ShowChartingSettings";

        private SerializedObject m_GameObjectsSerializedObject;
        private SerializedProperty m_GameObjectStaticFlags;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Materials = serializedObject.FindProperty("m_Materials");

            m_GameObjectsSerializedObject = new SerializedObject(targets.Select(t => ((MeshRenderer)t).gameObject).ToArray());
            m_GameObjectStaticFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");

            InitializeProbeFields();
            InitializeLightingFields();
        }

        private void InitializeLightingFields()
        {
            m_Lighting = new LightingSettingsInspector(serializedObject);

            m_Lighting.showSettings = EditorPrefs.GetBool(kDisplayLightingKey, false);
            m_Lighting.showChartingSettings = SessionState.GetBool(kDisplayChartingKey, true);
            m_Lighting.showLightmapSettings = SessionState.GetBool(kDisplayLightmapKey, true);
        }

        private void LightingFieldsGUI()
        {
            bool oldShowLighting = m_Lighting.showSettings;
            bool oldShowCharting = m_Lighting.showChartingSettings;
            bool oldShowLightmap = m_Lighting.showLightmapSettings;

            if (m_Lighting.Begin())
            {
                RenderProbeFields();
                m_Lighting.RenderMeshSettings(true);
            }
            m_Lighting.End();

            if (m_Lighting.showSettings != oldShowLighting)
                EditorPrefs.SetBool(kDisplayLightingKey, m_Lighting.showSettings);
            if (m_Lighting.showChartingSettings != oldShowCharting)
                SessionState.SetBool(kDisplayChartingKey, m_Lighting.showChartingSettings);
            if (m_Lighting.showLightmapSettings != oldShowLightmap)
                SessionState.SetBool(kDisplayLightmapKey, m_Lighting.showLightmapSettings);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            LightingFieldsGUI();

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
                EditorGUILayout.HelpBox(Styles.MaterialWarning, MessageType.Warning, true);
            }

            if (ShaderUtil.MaterialsUseInstancingShader(m_Materials))
            {
                m_GameObjectsSerializedObject.Update();

                if (!m_GameObjectStaticFlags.hasMultipleDifferentValues && ((StaticEditorFlags)m_GameObjectStaticFlags.intValue & StaticEditorFlags.BatchingStatic) != 0)
                {
                    EditorGUILayout.HelpBox(Styles.StaticBatchingWarning, MessageType.Warning, true);
                }
            }

            CullDynamicFieldGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
