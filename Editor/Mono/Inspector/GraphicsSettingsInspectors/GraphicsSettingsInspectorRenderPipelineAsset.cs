// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorRenderPipelineAsset : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorRenderPipelineAsset, UxmlTraits> { }

        internal class Styles
        {
            public static readonly GUIContent renderPipeSettings = EditorGUIUtility.TrTextContent("Scriptable Render Pipeline Settings",
                "This defines the default render pipeline, which Unity uses when there is no override for a given quality level.");

            public static readonly GUIContent renderPipelineMessage = EditorGUIUtility.TrTextContent("A Scriptable Render Pipeline is in use, some settings will not be used and are hidden");
        }

        SerializedProperty m_ScriptableRenderLoop;

        protected override void Initialize()
        {
            m_ScriptableRenderLoop = m_SerializedObject.FindProperty("m_CustomRenderPipeline");

            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            GUILayout.Label(Styles.renderPipeSettings, EditorStyles.boldLabel);
            EditorGUI.RenderPipelineAssetField(m_SerializedObject, m_ScriptableRenderLoop);

            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
                EditorGUILayout.HelpBox(Styles.renderPipelineMessage.text, MessageType.Info);
        }
    }
}
