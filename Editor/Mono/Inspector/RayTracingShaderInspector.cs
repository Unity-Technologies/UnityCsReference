// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Globalization;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(RayTracingShader))]
    internal class RayTracingShaderInspector : Editor
    {
        SerializedProperty m_MaxRecursionDepth;

        Vector2 m_ScrollPosition = Vector2.zero;

        private static GUIContent s_MaxRecursionDepthText = EditorGUIUtility.TrTextContent("Max. Recursion Depth", "Limit on ray recursion for the Ray Tracing pipeline. This is defined in the shader by using max_recursion_depth pragma(e.g. \"#pragma max_recursion_depth 5\"). Applications should pick a limit that is as low as absolutely necessary. A value of 1 means that only primary rays can be cast.");
        private static GUIContent s_PlatformList = EditorGUIUtility.TrTextContent("Platforms:");
        private static GUIContent s_NotSupported = EditorGUIUtility.TrTextContent("Ray Tracing Shader not supported! No graphics APIs with Ray Tracing support found in the Graphics APIs list.");
        static List<string> GetPlatformList(RayTracingShader rs)
        {
            var platformList = new List<string>();
            var platformCount = ShaderUtil.GetRayTracingShaderPlatformCount(rs);
            for (var i = 0; i < platformCount; ++i)
            {
                var platform = ShaderUtil.GetRayTracingShaderPlatformType(rs, i);
                if (ShaderUtil.IsRayTracingShaderValidForPlatform(rs, platform))
                    platformList.Add(platform.ToString());
            }
            return platformList;
        }

        private bool ShowPlatformListSection(RayTracingShader rs)
        {
            var platformList = GetPlatformList(rs);
            if (platformList.Count != 0)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label(s_PlatformList);
                foreach (var p in platformList)
                {
                    EditorGUILayout.LabelField(p);
                }
                EditorGUI.indentLevel--;
                return true;
            }
            return false;
        }

        public void OnEnable()
        {
            m_MaxRecursionDepth = serializedObject.FindProperty("m_MaxRecursionDepth");
        }

        public override void OnInspectorGUI()
        {
            var rts = target as RayTracingShader;
            if (rts == null)
                return;

            serializedObject.Update();

            GUI.enabled = true;

            EditorGUI.indentLevel = 0;

            if (ShowPlatformListSection(rts))
            {
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_MaxRecursionDepth, s_MaxRecursionDepthText);
            }
            else
            {
                EditorGUILayout.HelpBox(s_NotSupported.text, MessageType.Error);
            }

            ShowShaderErrors(rts);
        }

        private void ShowShaderErrors(RayTracingShader s)
        {
            int n = ShaderUtil.GetRayTracingShaderMessageCount(s);
            if (n < 1)
                return;
            ShaderInspector.ShaderErrorListUI(s, ShaderUtil.GetRayTracingShaderMessages(s), ref m_ScrollPosition);
        }
    }
}
