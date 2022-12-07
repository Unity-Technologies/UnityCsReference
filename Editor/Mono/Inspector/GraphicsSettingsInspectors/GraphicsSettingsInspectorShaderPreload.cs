// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorShaderPreload : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorShaderPreload, UxmlTraits> { }
        internal class Styles
        {
            public static readonly GUIContent shaderPreloadSave = EditorGUIUtility.TrTextContent("Save to asset...", "Save currently tracked shaders into a Shader Variant Manifest asset.");
            public static readonly GUIContent shaderPreloadClear = EditorGUIUtility.TrTextContent("Clear", "Clear currently tracked shader variant information.");
            public static readonly GUIContent delayShaderPreload = EditorGUIUtility.TrTextContent("Preload shaders after showing first scene");
            public static readonly GUIContent preloadShadersTimeLimit = EditorGUIUtility.TrTextContent("Preload time limit per frame (ms)");
            public static readonly GUIContent saveShaderVariantCollectionMessage = EditorGUIUtility.TrTextContent("Save shader variant collection");
            public static readonly GUIContent saveShaderVariantCollectionTitle = EditorGUIUtility.TrTextContent("Save Shader Variant Collection");
        }

        SerializedProperty m_PreloadedShaders;
        SerializedProperty m_PreloadShadersBatchTimeLimit;
        bool m_DelayShaderPreload;
        int m_PreloadShadersTimeLimit;

        protected override void Initialize()
        {
            m_PreloadedShaders = m_SerializedObject.FindProperty("m_PreloadedShaders");
            m_PreloadedShaders.isExpanded = true;
            m_PreloadShadersBatchTimeLimit = m_SerializedObject.FindProperty("m_PreloadShadersBatchTimeLimit");
            LoadShaderPreloadingDelay();

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            using var check = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_PreloadedShaders, true);

            m_DelayShaderPreload = EditorGUILayout.Toggle(Styles.delayShaderPreload, m_DelayShaderPreload);
            if (m_DelayShaderPreload)
                m_PreloadShadersTimeLimit = EditorGUILayout.IntField(Styles.preloadShadersTimeLimit, m_PreloadShadersTimeLimit);
            SaveShaderPreloadingDelay();

            EditorGUILayout.Space();
            GUILayout.Label($"Currently tracked: {ShaderUtil.GetCurrentShaderVariantCollectionShaderCount()} shaders {ShaderUtil.GetCurrentShaderVariantCollectionVariantCount()} total variants");

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Styles.shaderPreloadSave, EditorStyles.miniButton))
            {
                var assetPath = EditorUtility.SaveFilePanelInProject(
                    Styles.saveShaderVariantCollectionTitle.text,
                    "NewShaderVariants",
                    "shadervariants",
                    Styles.saveShaderVariantCollectionMessage.text,
                    ProjectWindowUtil.GetActiveFolderPath());
                if (!string.IsNullOrEmpty(assetPath))
                    ShaderUtil.SaveCurrentShaderVariantCollection(assetPath);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(Styles.shaderPreloadClear, EditorStyles.miniButton))
                ShaderUtil.ClearCurrentShaderVariantCollection();
            EditorGUILayout.EndHorizontal();

            if (check.changed)
                m_SerializedObject.ApplyModifiedProperties();
        }

        void LoadShaderPreloadingDelay()
        {
            m_PreloadShadersTimeLimit = m_PreloadShadersBatchTimeLimit.intValue;
            m_DelayShaderPreload = m_PreloadShadersTimeLimit >= 0;
            if (!m_DelayShaderPreload)
                m_PreloadShadersTimeLimit = 0;
        }

        void SaveShaderPreloadingDelay()
        {
            var newVal = m_DelayShaderPreload ? m_PreloadShadersTimeLimit : -1;
            if (m_PreloadShadersBatchTimeLimit.intValue != newVal)
                m_PreloadShadersBatchTimeLimit.intValue = newVal;
        }
    }
}
