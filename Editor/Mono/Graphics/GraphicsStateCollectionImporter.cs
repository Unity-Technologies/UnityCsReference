// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [ScriptedImporter(version: 1, ext: "graphicsstate")]
    [ExcludeFromPreset]
    class GraphicsStateCollectionImporter : ScriptedImporter
    {
        public RuntimePlatform runtimePlatform;
        public GraphicsDeviceType graphicsDeviceType;
        public int version;
        public string qualityLevelName;
        public int shaderVariantCount;
        public int graphicsStateCount;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            GraphicsStateCollection asset = new GraphicsStateCollection(ctx.assetPath);
            Texture2D icon = EditorGUIUtility.FindTexture(typeof(DefaultAsset));
            ctx.AddObjectToAsset("graphicsstatecollection", asset, icon);
            ctx.SetMainObject(asset);

            // Gather information about collection to display in inspector
            runtimePlatform = asset.runtimePlatform;
            graphicsDeviceType = asset.graphicsDeviceType;
            version = asset.version;
            qualityLevelName = asset.qualityLevelName;
            shaderVariantCount = asset.variantCount;
            graphicsStateCount = asset.totalGraphicsStateCount;
        }
    }

    [CustomEditor(typeof(GraphicsStateCollectionImporter))]
    class GraphicsStateCollectionImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty m_RuntimePlatform;
        SerializedProperty m_GraphicsDeviceType;
        SerializedProperty m_Version;
        SerializedProperty m_QualityLevelName;
        SerializedProperty m_ShaderVariantCount;
        SerializedProperty m_GraphicsStateCount;

        public override bool showImportedObject => false;
        protected override bool needsApplyRevert => false;

        public override void OnEnable()
        {
            base.OnEnable();

            m_RuntimePlatform = serializedObject.FindProperty("runtimePlatform");
            m_GraphicsDeviceType = serializedObject.FindProperty("graphicsDeviceType");
            m_Version = serializedObject.FindProperty("version");
            m_QualityLevelName = serializedObject.FindProperty("qualityLevelName");
            m_ShaderVariantCount = serializedObject.FindProperty("shaderVariantCount");
            m_GraphicsStateCount = serializedObject.FindProperty("graphicsStateCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_RuntimePlatform);
                EditorGUILayout.PropertyField(m_GraphicsDeviceType);
                EditorGUILayout.PropertyField(m_Version);
                EditorGUILayout.PropertyField(m_QualityLevelName, new GUIContent("Quality Level"));
                EditorGUILayout.PropertyField(m_ShaderVariantCount);
                EditorGUILayout.PropertyField(m_GraphicsStateCount);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
