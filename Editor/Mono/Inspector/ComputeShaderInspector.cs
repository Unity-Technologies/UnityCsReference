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

        // Compute kernel information is stored split by platform, then by kernels;
        // but for the inspector we want to show kernels, then platforms they are in.
        class KernelInfo
        {
            internal string name;
            internal string platforms;
        };

        internal class Styles
        {
            public static GUIContent showCompiled = EditorGUIUtility.TextContent("Show compiled code");
            public static GUIContent kernelsHeading = EditorGUIUtility.TextContent("Kernels:");
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
            GUILayout.Space(kSpace);
            if (GUILayout.Button(Styles.showCompiled, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                ShaderUtil.OpenCompiledComputeShader(cs, true);
                GUIUtility.ExitGUI();
            }
        }

        private void ShowShaderErrors(ComputeShader s)
        {
            int n = ShaderUtil.GetComputeShaderErrorCount(s);
            if (n < 1)
                return;
            ShaderInspector.ShaderErrorListUI(s, ShaderUtil.GetComputeShaderErrors(s), ref m_ScrollPosition);
        }
    }
}
