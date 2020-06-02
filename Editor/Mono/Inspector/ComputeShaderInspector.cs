// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor
{
    [CustomEditor(typeof(ComputeShader))]
    internal class ComputeShaderInspector : Editor
    {
        private const float kSpace = 5f;
        Vector2 m_ScrollPosition = Vector2.zero;

        private static bool s_PreprocessOnly = false;

        // Compute kernel information is stored split by platform, then by kernels;
        // but for the inspector we want to show kernels, then platforms they are in.
        class KernelInfo
        {
            internal string name;
            internal string platforms;
        }

        internal class Styles
        {
            public static GUIContent togglePreprocess = EditorGUIUtility.TrTextContent("Preprocess only", "Show preprocessor output instead of compiled shader code");
            public static GUIContent showCompiled = EditorGUIUtility.TrTextContent("Show compiled code");
            public static GUIContent kernelsHeading = EditorGUIUtility.TrTextContent("Kernels:");
        }

        static List<KernelInfo> GetKernelDisplayInfo(ComputeShader cs)
        {
            var kernelInfo = new List<KernelInfo>();
            var platformCount = ShaderUtil.GetComputeShaderPlatformCount(cs);
            for (var i = 0; i < platformCount; ++i)
            {
                var platform = ShaderUtil.GetComputeShaderPlatformType(cs, i);
                var kernelCount = ShaderUtil.GetComputeShaderPlatformKernelCount(cs, i);
                for (var j = 0; j < kernelCount; ++j)
                {
                    var kernelName = ShaderUtil.GetComputeShaderPlatformKernelName(cs, i, j);
                    var found = false;
                    foreach (var ki in kernelInfo)
                    {
                        if (ki.name == kernelName)
                        {
                            ki.platforms += ' ';
                            ki.platforms += platform.ToString();
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        var ki = new KernelInfo();
                        ki.name = kernelName;
                        ki.platforms = platform.ToString();
                        kernelInfo.Add(ki);
                    }
                }
            }
            return kernelInfo;
        }

        public override void OnInspectorGUI()
        {
            var cs = target as ComputeShader;
            if (cs == null)
                return;

            GUI.enabled = true;

            EditorGUI.indentLevel = 0;

            ShowKernelInfoSection(cs);
            ShowCompiledCodeSection(cs);
            ShowShaderErrors(cs);
        }

        private void ShowKernelInfoSection(ComputeShader cs)
        {
            GUILayout.Label(Styles.kernelsHeading, EditorStyles.boldLabel);
            var kernelInfo = GetKernelDisplayInfo(cs);
            foreach (var ki in kernelInfo)
            {
                EditorGUILayout.LabelField(ki.name, ki.platforms);
            }
        }

        private void ShowCompiledCodeSection(ComputeShader cs)
        {
            ComputeShaderImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(cs.GetInstanceID())) as ComputeShaderImporter;
            bool enablePreprocessOnly = EditorSettings.cachingShaderPreprocessor;
            if (importer && importer.preprocessorOverride == PreprocessorOverride.ForceCachingPreprocessor)
                enablePreprocessOnly = true;
            using (new EditorGUI.DisabledScope(!enablePreprocessOnly))
                s_PreprocessOnly = EditorGUILayout.Toggle(Styles.togglePreprocess, s_PreprocessOnly);
            GUILayout.Space(kSpace);
            if (GUILayout.Button(Styles.showCompiled, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                ShaderUtil.OpenCompiledComputeShader(cs, true, enablePreprocessOnly && s_PreprocessOnly);
                GUIUtility.ExitGUI();
            }
        }

        ShaderMessage[] m_ShaderMessages;
        private void ShowShaderErrors(ComputeShader s)
        {
            if (Event.current.type == EventType.Layout)
            {
                int n = ShaderUtil.GetComputeShaderMessageCount(s);
                m_ShaderMessages = null;
                if (n >= 1)
                {
                    m_ShaderMessages = ShaderUtil.GetComputeShaderMessages(s);
                }
            }

            if (m_ShaderMessages == null)
                return;
            ShaderInspector.ShaderErrorListUI(s, m_ShaderMessages, ref m_ScrollPosition);
        }
    }
}
