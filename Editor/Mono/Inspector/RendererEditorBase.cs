// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

namespace UnityEditor
{
    internal class RendererEditorBase : Editor
    {
        internal class Probes
        {
            private SerializedProperty m_LightProbeUsage;
            private SerializedProperty m_LightProbeVolumeOverride;
            private SerializedProperty m_ReflectionProbeUsage;
            private SerializedProperty m_ProbeAnchor;
            private SerializedProperty m_ReceiveShadows;

            private GUIContent m_LightProbeUsageStyle = EditorGUIUtility.TextContent("Light Probes|Specifies how Light Probes will handle the interpolation of lighting and occlusion. Disabled if the object is set to Lightmap Static.");
            private GUIContent m_LightProbeVolumeOverrideStyle = EditorGUIUtility.TextContent("Proxy Volume Override|If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");
            private GUIContent m_ReflectionProbeUsageStyle = EditorGUIUtility.TextContent("Reflection Probes|Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");
            private GUIContent m_ProbeAnchorStyle = EditorGUIUtility.TextContent("Anchor Override|Specifies the Transform position that will be used for sampling the light probes and reflection probes.");
            private GUIContent m_DeferredNote = EditorGUIUtility.TextContent("In Deferred Shading, all objects receive shadows and get per-pixel reflection probes.");
            private GUIContent m_LightProbeVolumeNote = EditorGUIUtility.TextContent("A valid Light Probe Proxy Volume component could not be found.");
            private GUIContent m_LightProbeVolumeUnsupportedNote = EditorGUIUtility.TextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");
            private GUIContent m_LightProbeVolumeUnsupportedOnTreesNote = EditorGUIUtility.TextContent("The Light Probe Proxy Volume feature is not supported on tree rendering. Simple 'Blend Probes' mode will be used instead.");
            private string[] m_ReflectionProbeUsageNames = Enum.GetNames(typeof(ReflectionProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray();
            private string[] m_LightProbeUsageNames = Enum.GetNames(typeof(LightProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray();
            private GUIContent[] m_ReflectionProbeUsageOptions = (Enum.GetNames(typeof(ReflectionProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            private GUIContent[] m_LightProbeBlendModeOptions = (Enum.GetNames(typeof(LightProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();

            private List<ReflectionProbeBlendInfo> m_BlendInfo = new List<ReflectionProbeBlendInfo>();

            internal void Initialize(SerializedObject serializedObject)
            {
                m_LightProbeUsage = serializedObject.FindProperty("m_LightProbeUsage");
                m_LightProbeVolumeOverride = serializedObject.FindProperty("m_LightProbeVolumeOverride");
                m_ReflectionProbeUsage = serializedObject.FindProperty("m_ReflectionProbeUsage");
                m_ProbeAnchor = serializedObject.FindProperty("m_ProbeAnchor");
                m_ReceiveShadows = serializedObject.FindProperty("m_ReceiveShadows");
            }

            internal bool IsUsingLightProbeProxyVolume(int selectionCount)
            {
                bool isUsingLightProbeVolumes =
                    ((selectionCount == 1) && (m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume)) ||
                    ((selectionCount > 1) && !m_LightProbeUsage.hasMultipleDifferentValues && (m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume));

                return isUsingLightProbeVolumes;
            }

            internal bool HasValidLightProbeProxyVolumeOverride(Renderer renderer, int selectionCount)
            {
                LightProbeProxyVolume proxyVolumeOverride = (renderer.lightProbeProxyVolumeOverride != null) ?
                    renderer.lightProbeProxyVolumeOverride.GetComponent<LightProbeProxyVolume>() :
                    null;

                return IsUsingLightProbeProxyVolume(selectionCount) && ((proxyVolumeOverride == null) || (proxyVolumeOverride.boundingBoxMode != LightProbeProxyVolume.BoundingBoxMode.AutomaticLocal));
            }

            internal void RenderLightProbeProxyVolumeWarningNote(Renderer renderer, int selectionCount)
            {
                if (IsUsingLightProbeProxyVolume(selectionCount))
                {
                    if (LightProbeProxyVolume.isFeatureSupported)
                    {
                        LightProbeProxyVolume lightProbeProxyVol = renderer.GetComponent<LightProbeProxyVolume>();
                        bool invalidProxyVolumeOverride = (renderer.lightProbeProxyVolumeOverride == null) ||
                            (renderer.lightProbeProxyVolumeOverride.GetComponent<LightProbeProxyVolume>() == null);
                        if (lightProbeProxyVol == null && invalidProxyVolumeOverride)
                        {
                            EditorGUILayout.HelpBox(m_LightProbeVolumeNote.text, MessageType.Warning);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(m_LightProbeVolumeUnsupportedNote.text, MessageType.Warning);
                    }
                }
            }

            internal void RenderReflectionProbeUsage(bool useMiniStyle, bool isDeferredRenderingPath, bool isDeferredReflections)
            {
                using (new EditorGUI.DisabledScope(isDeferredRenderingPath))
                {
                    // reflection probe usage field; UI disabled when using deferred reflections
                    if (!useMiniStyle)
                    {
                        if (isDeferredReflections)
                        {
                            EditorGUILayout.EnumPopup(m_ReflectionProbeUsageStyle, (m_ReflectionProbeUsage.intValue != (int)ReflectionProbeUsage.Off) ? ReflectionProbeUsage.Simple : ReflectionProbeUsage.Off);
                        }
                        else
                        {
                            EditorGUILayout.Popup(m_ReflectionProbeUsage, m_ReflectionProbeUsageOptions, m_ReflectionProbeUsageStyle);
                        }
                    }
                    else
                    {
                        if (isDeferredReflections)
                        {
                            ModuleUI.GUIPopup(m_ReflectionProbeUsageStyle, (int)ReflectionProbeUsage.Simple, m_ReflectionProbeUsageNames);
                        }
                        else
                        {
                            ModuleUI.GUIPopup(m_ReflectionProbeUsageStyle, m_ReflectionProbeUsage, m_ReflectionProbeUsageNames);
                        }
                    }
                }
            }

            internal void RenderLightProbeUsage(int selectionCount, Renderer renderer, bool useMiniStyle, bool usesLightMaps)
            {
                using (new EditorGUI.DisabledScope(usesLightMaps))
                {
                    if (!useMiniStyle)
                    {
                        if (usesLightMaps)
                            EditorGUILayout.EnumPopup(m_LightProbeUsageStyle, LightProbeUsage.Off);
                        else
                        {
                            EditorGUILayout.Popup(m_LightProbeUsage, m_LightProbeBlendModeOptions, m_LightProbeUsageStyle);

                            if (!m_LightProbeUsage.hasMultipleDifferentValues && m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(m_LightProbeVolumeOverride, m_LightProbeVolumeOverrideStyle);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    else
                    {
                        if (usesLightMaps)
                            ModuleUI.GUIPopup(m_LightProbeUsageStyle, (int)LightProbeUsage.Off, m_LightProbeUsageNames);
                        else
                        {
                            ModuleUI.GUIPopup(m_LightProbeUsageStyle, m_LightProbeUsage, m_LightProbeUsageNames);

                            if (!m_LightProbeUsage.hasMultipleDifferentValues && m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume)
                            {
                                EditorGUI.indentLevel++;
                                ModuleUI.GUIObject(m_LightProbeVolumeOverrideStyle, m_LightProbeVolumeOverride);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }

                var tree = renderer.GetComponent<Tree>();
                if ((tree != null) && (m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(m_LightProbeVolumeUnsupportedOnTreesNote.text, MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
            }

            internal bool RenderProbeAnchor(bool useMiniStyle)
            {
                bool useReflectionProbes = !m_ReflectionProbeUsage.hasMultipleDifferentValues && (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue != ReflectionProbeUsage.Off;
                bool lightProbesEnabled = !m_LightProbeUsage.hasMultipleDifferentValues && (LightProbeUsage)m_LightProbeUsage.intValue != LightProbeUsage.Off;
                bool needsRendering = useReflectionProbes || lightProbesEnabled;

                if (needsRendering)
                {
                    // anchor field
                    if (!useMiniStyle)
                        EditorGUILayout.PropertyField(m_ProbeAnchor, m_ProbeAnchorStyle);
                    else
                        ModuleUI.GUIObject(m_ProbeAnchorStyle, m_ProbeAnchor);
                }

                return needsRendering;
            }

            // Set useMiniStyle to true to use smaller UI styles,
            // like in particle system modules UI.
            //
            // The code does branches right now on that instead of just
            // picking one or another style, since there are no UI
            // functions, like EditorGUILayout.ObjectField that would take
            // a style parameter :(
            internal void OnGUI(UnityEngine.Object[] selection, Renderer renderer, bool useMiniStyle)
            {
                int selectionCount = 1;
                bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();
                bool isDeferredReflections = isDeferredRenderingPath && (UnityEngine.Rendering.GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled);
                bool isLightmappedOrDynamicLightmappedForRendering = false;

                if (selection != null)
                {
                    foreach (UnityEngine.Object obj in selection)
                    {
                        if (LightmapEditorSettings.IsLightmappedOrDynamicLightmappedForRendering((Renderer)obj))
                        {
                            isLightmappedOrDynamicLightmappedForRendering = true;
                            break;
                        }
                    }

                    selectionCount = selection.Length;
                }

                RenderLightProbeUsage(selectionCount, renderer, useMiniStyle, isLightmappedOrDynamicLightmappedForRendering);

                RenderReflectionProbeUsage(useMiniStyle, isDeferredRenderingPath, isDeferredReflections);

                // anchor field - light probes and reflection probes share the same anchor
                bool needsProbeAnchorField = RenderProbeAnchor(useMiniStyle);

                if (needsProbeAnchorField)
                {
                    bool useReflectionProbes = !m_ReflectionProbeUsage.hasMultipleDifferentValues && (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue != ReflectionProbeUsage.Off;

                    if (useReflectionProbes)
                    {
                        if (!isDeferredReflections)
                        {
                            renderer.GetClosestReflectionProbes(m_BlendInfo);
                            ShowClosestReflectionProbes(m_BlendInfo);
                        }
                    }
                }

                bool receivesShadow = !m_ReceiveShadows.hasMultipleDifferentValues && m_ReceiveShadows.boolValue;

                if ((isDeferredRenderingPath && receivesShadow) || (isDeferredReflections && needsProbeAnchorField))
                {
                    EditorGUILayout.HelpBox(m_DeferredNote.text, MessageType.Info);
                }

                RenderLightProbeProxyVolumeWarningNote(renderer, selectionCount);
            }

            // Show an info list of probes affecting this object, and their weights.
            internal static void ShowClosestReflectionProbes(List<ReflectionProbeBlendInfo> blendInfos)
            {
                float kProbeLabelWidth = 20;
                float kWeightWidth = 70;

                // No UI interaction, so disable all controls.
                using (new EditorGUI.DisabledScope(true))
                {
                    for (int i = 0; i < blendInfos.Count; i++)
                    {
                        var rowRect = GUILayoutUtility.GetRect(0, EditorGUI.kSingleLineHeight);
                        rowRect = EditorGUI.IndentedRect(rowRect);
                        float probeFieldWidth = rowRect.width - kProbeLabelWidth - kWeightWidth;
                        var rect = rowRect;

                        rect.width = kProbeLabelWidth;
                        GUI.Label(rect, "#" + i, EditorStyles.miniLabel);

                        rect.x += rect.width;
                        rect.width = probeFieldWidth;
                        EditorGUI.ObjectField(rect, blendInfos[i].probe, typeof(ReflectionProbe), true);

                        rect.x += rect.width;
                        rect.width = kWeightWidth;
                        GUI.Label(rect, "Weight " + blendInfos[i].weight.ToString("f2"), EditorStyles.miniLabel);
                    }
                }
            }

            internal static string[] GetFieldsStringArray()
            {
                return new[]
                {
                    "m_LightProbeUsage",
                    "m_LightProbeVolumeOverride",
                    "m_ReflectionProbeUsage",
                    "m_ProbeAnchor",
                };
            }
        }

        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;


        protected Probes m_Probes;

        public virtual void OnEnable()
        {
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
        }

        protected void RenderSortingLayerFields()
        {
            EditorGUILayout.Space();
            SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);
        }

        protected void InitializeProbeFields()
        {
            m_Probes = new Probes();
            m_Probes.Initialize(serializedObject);
        }

        protected void RenderProbeFields()
        {
            m_Probes.OnGUI(targets, (Renderer)target, false);
        }

        protected void RenderCommonProbeFields(bool useMiniStyle)
        {
            bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();
            bool isDeferredReflections = isDeferredRenderingPath && (UnityEngine.Rendering.GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled);
            m_Probes.RenderReflectionProbeUsage(useMiniStyle, isDeferredRenderingPath, isDeferredReflections);
            m_Probes.RenderProbeAnchor(useMiniStyle);
        }
    }
}
