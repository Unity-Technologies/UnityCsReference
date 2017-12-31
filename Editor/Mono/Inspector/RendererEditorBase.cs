// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class RendererEditorBase : Editor
    {
        private GUIContent m_DynamicOccludeeLabel = EditorGUIUtility.TrTextContent("Dynamic Occluded", "Controls if dynamic occlusion culling should be performed for this renderer.");


        internal class Probes
        {
            private SerializedProperty m_LightProbeUsage;
            private SerializedProperty m_LightProbeVolumeOverride;
            private SerializedProperty m_ReflectionProbeUsage;
            private SerializedProperty m_ProbeAnchor;
            private SerializedProperty m_ReceiveShadows;

            private GUIContent m_LightProbeUsageStyle = EditorGUIUtility.TrTextContent("Light Probes", "Specifies how Light Probes will handle the interpolation of lighting and occlusion. Disabled if the object is set to Lightmap Static.");
            private GUIContent m_LightProbeVolumeOverrideStyle = EditorGUIUtility.TrTextContent("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");
            private GUIContent m_ReflectionProbeUsageStyle = EditorGUIUtility.TrTextContent("Reflection Probes", "Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");
            private GUIContent m_ProbeAnchorStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes and reflection probes.");
            private GUIContent m_DeferredNote = EditorGUIUtility.TrTextContent("In Deferred Shading, all objects receive shadows and get per-pixel reflection probes.");
            private GUIContent m_LightProbeVolumeNote = EditorGUIUtility.TrTextContent("A valid Light Probe Proxy Volume component could not be found.");
            private GUIContent m_LightProbeVolumeUnsupportedNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");
            private GUIContent m_LightProbeVolumeUnsupportedOnTreesNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is not supported on tree rendering. Simple 'Blend Probes' mode will be used instead.");
            private GUIContent m_LightProbeCustomNote = EditorGUIUtility.TrTextContent("The Custom Provided mode requires SH properties to be sent via MaterialPropertyBlock.");
            private GUIContent[] m_ReflectionProbeUsageOptions = (Enum.GetNames(typeof(ReflectionProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();

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
                    if (LightProbeProxyVolume.isFeatureSupported && SupportedRenderingFeatures.active.rendererSupportsLightProbeProxyVolumes)
                    {
                        LightProbeProxyVolume lightProbeProxyVol = renderer.GetComponent<LightProbeProxyVolume>();
                        bool invalidProxyVolumeOverride = (renderer.lightProbeProxyVolumeOverride == null) ||
                            (renderer.lightProbeProxyVolumeOverride.GetComponent<LightProbeProxyVolume>() == null);
                        if (lightProbeProxyVol == null && invalidProxyVolumeOverride && LightProbes.AreLightProbesAllowed(renderer))
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
                if (!SupportedRenderingFeatures.active.rendererSupportsReflectionProbes)
                    return;

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
                            ModuleUI.GUIPopup(m_ReflectionProbeUsageStyle, (int)ReflectionProbeUsage.Simple, m_ReflectionProbeUsageOptions);
                        }
                        else
                        {
                            ModuleUI.GUIPopup(m_ReflectionProbeUsageStyle, m_ReflectionProbeUsage, m_ReflectionProbeUsageOptions);
                        }
                    }
                }
            }

            internal void RenderLightProbeUsage(int selectionCount, Renderer renderer, bool useMiniStyle, bool lightProbeAllowed)
            {
                using (new EditorGUI.DisabledScope(!lightProbeAllowed))
                {
                    if (lightProbeAllowed)
                    {
                        // LightProbeUsage has non-sequential enum values. Extra care is to be taken.
                        if (useMiniStyle)
                        {
                            EditorGUI.BeginChangeCheck();
                            var newValue = ModuleUI.GUIEnumPopup(m_LightProbeUsageStyle, (LightProbeUsage)m_LightProbeUsage.intValue, m_LightProbeUsage);
                            if (EditorGUI.EndChangeCheck())
                                m_LightProbeUsage.intValue = (int)(LightProbeUsage)newValue;
                        }
                        else
                        {
                            Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup);
                            EditorGUI.BeginProperty(r, m_LightProbeUsageStyle, m_LightProbeUsage);
                            EditorGUI.BeginChangeCheck();
                            var newValue = EditorGUI.EnumPopup(r, m_LightProbeUsageStyle, (LightProbeUsage)m_LightProbeUsage.intValue);
                            if (EditorGUI.EndChangeCheck())
                                m_LightProbeUsage.intValue = (int)(LightProbeUsage)newValue;
                            EditorGUI.EndProperty();
                        }

                        if (!m_LightProbeUsage.hasMultipleDifferentValues)
                        {
                            if (m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume
                                && SupportedRenderingFeatures.active.rendererSupportsLightProbeProxyVolumes)
                            {
                                EditorGUI.indentLevel++;
                                if (useMiniStyle)
                                    ModuleUI.GUIObject(m_LightProbeVolumeOverrideStyle, m_LightProbeVolumeOverride);
                                else
                                    EditorGUILayout.PropertyField(m_LightProbeVolumeOverride, m_LightProbeVolumeOverrideStyle);
                                EditorGUI.indentLevel--;
                            }
                            else if (m_LightProbeUsage.intValue == (int)LightProbeUsage.CustomProvided)
                            {
                                EditorGUI.indentLevel++;
                                if (!Application.isPlaying)
                                    EditorGUILayout.HelpBox(m_LightProbeCustomNote.text, MessageType.Info);
                                else if (!renderer.HasPropertyBlock())
                                    EditorGUILayout.HelpBox(m_LightProbeCustomNote.text, MessageType.Error);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                    else
                    {
                        if (useMiniStyle)
                            ModuleUI.GUIEnumPopup(m_LightProbeUsageStyle, LightProbeUsage.Off, m_LightProbeUsage);
                        else
                            EditorGUILayout.EnumPopup(m_LightProbeUsageStyle, LightProbeUsage.Off);
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
                bool areLightProbesAllowed = true;

                if (selection != null)
                {
                    foreach (UnityEngine.Object obj in selection)
                    {
                        if (LightProbes.AreLightProbesAllowed((Renderer)obj) == false)
                        {
                            areLightProbesAllowed = false;
                            break;
                        }
                    }

                    selectionCount = selection.Length;
                }

                RenderLightProbeUsage(selectionCount, renderer, useMiniStyle, areLightProbesAllowed);

                RenderLightProbeProxyVolumeWarningNote(renderer, selectionCount);

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

        private static string[] m_LayerNames;
        private static string[] layerNames
        {
            get
            {
                if (m_LayerNames == null)
                {
                    m_LayerNames = new string[32];
                    for (int i = 0; i < m_LayerNames.Length; ++i)
                    {
                        m_LayerNames[i] = string.Format("Layer{0}", i + 1);
                    }
                }
                return m_LayerNames;
            }
        }

        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;
        private SerializedProperty m_DynamicOccludee;
        private SerializedProperty m_RenderingLayerMask;
        static GUIContent m_RenderingLayerMaskStyle = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");

        protected Probes m_Probes;

        public virtual void OnEnable()
        {
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_DynamicOccludee = serializedObject.FindProperty("m_DynamicOccludee");
            m_RenderingLayerMask = serializedObject.FindProperty("m_RenderingLayerMask");
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

        protected void CullDynamicFieldGUI()
        {
            EditorGUILayout.PropertyField(m_DynamicOccludee, m_DynamicOccludeeLabel);
        }

        protected void RenderRenderingLayer()
        {
            RenderRenderingLayer(m_RenderingLayerMask, target as Renderer, targets.ToArray());
        }

        internal static void RenderRenderingLayer(SerializedProperty layerMask, Renderer target, Object[] targets, bool useMiniStyle = false)
        {
            bool usingSRP = GraphicsSettings.renderPipelineAsset != null;
            if (!usingSRP || target == null)
                return;

            EditorGUI.showMixedValue = layerMask.hasMultipleDifferentValues;

            var renderer = target;
            var mask = (int)renderer.renderingLayerMask;

            EditorGUI.BeginChangeCheck();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, m_RenderingLayerMaskStyle, layerMask);
            if (useMiniStyle)
            {
                rect = ModuleUI.PrefixLabel(rect, m_RenderingLayerMaskStyle);
                mask = EditorGUI.MaskField(rect, GUIContent.none, mask, layerNames,
                        ParticleSystemStyles.Get().popup);
            }
            else
                mask = EditorGUI.MaskField(rect, m_RenderingLayerMaskStyle, mask, layerNames);
            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Set rendering layer mask");
                foreach (var t in targets)
                {
                    var r = t as Renderer;
                    if (r != null)
                    {
                        r.renderingLayerMask = (uint)mask;
                        EditorUtility.SetDirty(t);
                    }
                }
            }
            EditorGUI.showMixedValue = false;
        }

        protected void RenderCommonProbeFields(bool useMiniStyle)
        {
            bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();
            bool isDeferredReflections = isDeferredRenderingPath && (GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled);
            m_Probes.RenderReflectionProbeUsage(useMiniStyle, isDeferredRenderingPath, isDeferredReflections);
            m_Probes.RenderProbeAnchor(useMiniStyle);
        }
    }
}
