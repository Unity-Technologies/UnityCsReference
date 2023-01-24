// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshRenderer))]
    [CanEditMultipleObjects]
    internal class MeshRendererEditor : RendererEditorBase
    {
        class Styles
        {
            public static readonly GUIContent materialWarning = EditorGUIUtility.TrTextContent("This renderer has more materials than the Mesh has submeshes. Multiple materials will be applied to the same submesh, which costs performance. Consider using multiple shader passes.");
            public static readonly GUIContent staticBatchingWarning = EditorGUIUtility.TrTextContent("This Renderer uses static batching and instanced Shaders. When the Player is active, instancing is disabled. If you want instanced Shaders at run time, disable static batching.");
        }

        private SerializedObject m_GameObjectsSerializedObject;
        private SerializedProperty m_GameObjectStaticFlags;

        public override void OnEnable()
        {
            // Since we are not doing anything if we are not displayed in the inspector, early out. This help keeps multi selection snappier.
            if (hideInspector)
                return;

            base.OnEnable();

            m_GameObjectsSerializedObject = new SerializedObject(targets.Select(t => ((MeshRenderer)t).gameObject).ToArray());
            m_GameObjectStaticFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");

            Lightmapping.lightingDataUpdated += LightingDataUpdatedRepaint;
        }

        public void OnDisable()
        {
            Lightmapping.lightingDataUpdated -= LightingDataUpdatedRepaint;
        }

        private void LightingDataUpdatedRepaint()
        {
            if (m_Lighting.showLightmapSettings)
            {
                Repaint();
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

            // Disable Materials menu for the legacy Tree objects, see Fogbugz case: 1283092
            Tree treeComponent = ((MeshRenderer)serializedObject.targetObject).GetComponent<Tree>();
            bool hasTreeComponent = treeComponent != null;
            bool isSpeedTree = hasTreeComponent && treeComponent.data == null; // SpeedTrees always have the Tree component, but have null 'data' property
            using (new EditorGUI.DisabledScope(hasTreeComponent && !isSpeedTree))
            {
                DrawMaterials();
            }

            if (!m_Materials.hasMultipleDifferentValues && displayMaterialWarning)
            {
                EditorGUILayout.HelpBox(Styles.materialWarning.text, MessageType.Warning, true);
            }

            if (ShaderUtil.MaterialsUseInstancingShader(m_Materials))
            {
                m_GameObjectsSerializedObject.Update();

                int staticBatching, dynamicBatching;
                PlayerSettings.GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out staticBatching, out dynamicBatching);

                if (!m_GameObjectStaticFlags.hasMultipleDifferentValues && ((StaticEditorFlags)m_GameObjectStaticFlags.intValue & StaticEditorFlags.BatchingStatic) != 0 && staticBatching != 0)
                {
                    EditorGUILayout.HelpBox(Styles.staticBatchingWarning.text, MessageType.Warning, true);
                }
            }

            LightingSettingsGUI(true);
            RayTracingSettingsGUI();
            OtherSettingsGUI(true, false, false);

            if (targets.Length == 1)
                SpeedTreeMaterialFixer.DoFixerUI((target as MeshRenderer).gameObject);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
