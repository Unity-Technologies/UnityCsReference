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

        private class Styles
        {
            public GUIContent s_MaxRecursionDepthText = EditorGUIUtility.TrTextContent("Max. Recursion Depth", "Limit on ray recursion for the Ray Tracing pipeline. This is defined in the shader by using max_recursion_depth pragma(e.g. \"#pragma max_recursion_depth 5\"). Applications should pick a limit that is as low as absolutely necessary. A value of 1 means that only primary rays can be cast.");
            public GUIContent s_PlatformList = EditorGUIUtility.TrTextContent("Platforms:");
            public GUIContent s_NotSupported = EditorGUIUtility.TrTextContent("Ray Tracing Shader not supported! No graphics APIs with Ray Tracing support found in the Graphics APIs list.");
            public GUIContent s_Index = EditorGUIUtility.TrTextContent("Index");
            public GUIContent s_Name = EditorGUIUtility.TrTextContent("Name");
            public GUIContent s_PayloadSize = EditorGUIUtility.TrTextContent("Payload Size (Bytes)");
            public GUIContent s_ParamSize = EditorGUIUtility.TrTextContent("Param. Size (Bytes)");
            public GUIContent s_RayGenShaderNames = EditorGUIUtility.TrTextContent("Ray Generation Shaders", "The list of all ray generation shaders in the shader file. Only one ray generation shader can be executed at a time.");
            public GUIContent s_MissShaderNames = EditorGUIUtility.TrTextContent("Miss Shaders", "The list of all miss shaders in the shader file. The index of the miss shader to execute is specified when calling TraceRay HLSL function.");
            public GUIContent s_CallableShaderNames = EditorGUIUtility.TrTextContent("Callable Shaders", "The list of all callable shaders in the shader file. The index of the callable shader to execute is specified when calling CallShader HLSL function.");
            public GUIStyle s_LabelStyle = new GUIStyle(EditorStyles.boldLabel);
            public Styles()
            {
                s_LabelStyle.richText = true;
            }
        }

        static Styles styles;

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
                GUILayout.Label(styles.s_PlatformList);
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

        void ShowRayGenerationShaderList(string[] shaderNames)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            for (int i = 0; i < shaderNames.Length; ++i)
            {
                GUILayout.Label(shaderNames[i], EditorStyles.textArea);
            }

            GUILayout.EndVertical();
        }

        void ShowMissShaderList(string[] missShaderNames, int[] rayPayloadSize)
        {
            GUIStyle messageStyle = "CN StatusInfo";

            float lineHeight = messageStyle.CalcHeight(EditorGUIUtility.TempContent("ShaderName"), 100);

            Rect rHeader = EditorGUILayout.GetControlRect(false, lineHeight);

            Vector2 indexColumnSize = EditorStyles.boldLabel.CalcSize(styles.s_Index);
            indexColumnSize.x += 15;
            GUI.Label(rHeader, styles.s_Index, new GUIStyle(EditorStyles.boldLabel));

            rHeader.xMin += indexColumnSize.x;
            GUI.Label(rHeader, styles.s_Name, EditorStyles.boldLabel);

            Vector2 payloadColumnSize = EditorStyles.boldLabel.CalcSize(styles.s_PayloadSize);

            rHeader.xMin = rHeader.xMax - payloadColumnSize.x - 15;
            GUI.Label(rHeader, styles.s_PayloadSize, EditorStyles.boldLabel);

            GUILayout.BeginVertical(GUI.skin.box);

            for (int i = 0; i < missShaderNames.Length; ++i)
            {
                Rect r = EditorGUILayout.GetControlRect(false, lineHeight);

                GUI.Label(r, i.ToString(), EditorStyles.textArea);

                r.xMin += indexColumnSize.x;
                GUI.Label(r, missShaderNames[i], EditorStyles.textArea);

                r.xMin = r.xMax - payloadColumnSize.x - 10;
                GUI.Label(r, rayPayloadSize[i].ToString(), EditorStyles.textArea);
            }

            GUILayout.EndVertical();
        }

        void ShowCallableShaderList(string[] callableShaderNames, int[] paramSize)
        {
            GUIStyle messageStyle = "CN StatusInfo";

            float lineHeight = messageStyle.CalcHeight(EditorGUIUtility.TempContent("ShaderName"), 100);

            Rect rHeader = EditorGUILayout.GetControlRect(false, lineHeight);

            Vector2 indexColumnSize = EditorStyles.boldLabel.CalcSize(styles.s_Index);
            indexColumnSize.x += 15;
            GUI.Label(rHeader, styles.s_Index, EditorStyles.boldLabel);

            rHeader.xMin += indexColumnSize.x;
            GUI.Label(rHeader, styles.s_Name, EditorStyles.boldLabel);

            Vector2 paramColumnSize = EditorStyles.boldLabel.CalcSize(styles.s_ParamSize);

            rHeader.xMin = rHeader.xMax - paramColumnSize.x - 15;
            GUI.Label(rHeader, styles.s_ParamSize, EditorStyles.boldLabel);

            GUILayout.BeginVertical(GUI.skin.box);

            for (int i = 0; i < callableShaderNames.Length; ++i)
            {
                Rect r = EditorGUILayout.GetControlRect(false, lineHeight);

                GUI.Label(r, i.ToString(), EditorStyles.textArea);

                r.xMin += indexColumnSize.x;
                GUI.Label(r, callableShaderNames[i], EditorStyles.textArea);

                r.xMin = r.xMax - paramColumnSize.x - 10;
                GUI.Label(r, paramSize[i].ToString(), EditorStyles.textArea);
            }

            GUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            if (styles == null)
                styles = new Styles();

            var rts = target as RayTracingShader;
            if (rts == null)
                return;

            serializedObject.Update();

            GUI.enabled = true;

            EditorGUI.indentLevel = 0;

            if (ShowPlatformListSection(rts))
            {
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_MaxRecursionDepth, styles.s_MaxRecursionDepthText);

                int rayGenShaderCount = ShaderUtil.GetRayGenerationShaderCount(rts);
                if (rayGenShaderCount > 0)
                {
                    GUILayout.Space(15.0f);
                    GUILayout.Label(styles.s_RayGenShaderNames, styles.s_LabelStyle);

                    string[] rayGenShaderNames = new string[rayGenShaderCount];
                    for (int i = 0; i < rayGenShaderCount; i++)
                        rayGenShaderNames[i] = ShaderUtil.GetRayGenerationShaderName(rts, i);

                    ShowRayGenerationShaderList(rayGenShaderNames);
                }

                int missShaderCount = ShaderUtil.GetMissShaderCount(rts);
                if (missShaderCount > 0)
                {
                    GUILayout.Space(15.0f);

                    GUILayout.Label(styles.s_MissShaderNames, styles.s_LabelStyle);

                    string[] missShaderNames = new string[missShaderCount];
                    int[] missShaderPayloadSize = new int[missShaderCount];
                    for (int i = 0; i < missShaderCount; i++)
                    {
                        missShaderNames[i] = ShaderUtil.GetMissShaderName(rts, i);
                        missShaderPayloadSize[i] = ShaderUtil.GetMissShaderRayPayloadSize(rts, i);
                    }

                    ShowMissShaderList(missShaderNames, missShaderPayloadSize);
                }

                int callableShaderCount = ShaderUtil.GetCallableShaderCount(rts);
                if (callableShaderCount > 0)
                {
                    GUILayout.Space(15.0f);

                    GUILayout.Label(styles.s_CallableShaderNames, styles.s_LabelStyle);

                    string[] callableShaderNames = new string[callableShaderCount];
                    int[] callableShaderParamsSize = new int[callableShaderCount];
                    for (int i = 0; i < callableShaderCount; i++)
                    {
                        callableShaderNames[i] = ShaderUtil.GetCallableShaderName(rts, i);
                        callableShaderParamsSize[i] = ShaderUtil.GetCallableShaderParamSize(rts, i);
                    }

                    ShowCallableShaderList(callableShaderNames, callableShaderParamsSize);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(styles.s_NotSupported.text, MessageType.Error);
            }

            ShowShaderErrors(rts);
        }

        ShaderMessage[] m_ShaderMessages;
        private void ShowShaderErrors(RayTracingShader s)
        {
            if (Event.current.type == EventType.Layout)
            {
                int n = ShaderUtil.GetRayTracingShaderMessageCount(s);
                m_ShaderMessages = null;
                if (n >= 1)
                {
                    m_ShaderMessages = ShaderUtil.GetRayTracingShaderMessages(s);
                }
            }

            if (m_ShaderMessages == null)
                return;
            ShaderInspector.ShaderErrorListUI(s, m_ShaderMessages, ref m_ScrollPosition);
        }
    }
}
