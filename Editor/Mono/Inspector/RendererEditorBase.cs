// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using Object = UnityEngine.Object;
using System.Globalization;

// RayTracingMode enum will be moved into UnityEngine.Rendering in the future.
using RayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode;

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

            private GUIContent m_LightProbeUsageStyle = EditorGUIUtility.TrTextContent("Light Probes", "Specifies how Light Probes will handle the interpolation of lighting and occlusion. Disabled if the object is set to receive Global Illumination from lightmaps.");
            private GUIContent m_LightProbeVolumeOverrideStyle = EditorGUIUtility.TrTextContent("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component from another GameObject.");
            private GUIContent m_ReflectionProbeUsageStyle = EditorGUIUtility.TrTextContent("Reflection Probes", "Specifies if or how the object is affected by reflections in the Scene.  This property cannot be disabled in deferred rendering modes.");
            private GUIContent m_ProbeAnchorStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes and reflection probes.");
            private GUIContent m_ProbeAnchorNoReflectionProbesStyle = EditorGUIUtility.TrTextContent("Anchor Override", "Specifies the Transform position that will be used for sampling the light probes.");
            private GUIContent m_DeferredNote = EditorGUIUtility.TrTextContent("In Deferred Shading, all objects receive shadows and get per-pixel reflection probes.");
            private GUIContent m_LightProbeVolumeNote = EditorGUIUtility.TrTextContent("A valid Light Probe Proxy Volume component could not be found.");
            private GUIContent m_LightProbeVolumeUnsupportedNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is unsupported by the current graphics hardware or API configuration. Simple 'Blend Probes' mode will be used instead.");
            private GUIContent m_LightProbeVolumeUnsupportedOnTreesNote = EditorGUIUtility.TrTextContent("The Light Probe Proxy Volume feature is not supported on tree rendering. Simple 'Blend Probes' mode will be used instead.");
            private GUIContent m_LightProbeCustomNote = EditorGUIUtility.TrTextContent("The Custom Provided mode requires SH properties to be sent via MaterialPropertyBlock.");
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            private GUIContent[] m_ReflectionProbeUsageOptions = (Enum.GetNames(typeof(ReflectionProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
#pragma warning restore RS0030

            private GUIContent probeAnchorStyle
            {
                get
                {
                    if (!SupportedRenderingFeatures.active.reflectionProbes)
                        return m_ProbeAnchorNoReflectionProbesStyle;
                    return m_ProbeAnchorStyle;
                }
            }

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

            internal void RenderLightProbeProxyVolumeWarningNote(Renderer renderer, int selectionCount)
            {
                if (IsUsingLightProbeProxyVolume(selectionCount))
                {
                    if (LightProbeProxyVolume.isFeatureSupported && SupportedRenderingFeatures.active.lightProbeProxyVolumes)
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
                if (!SupportedRenderingFeatures.active.reflectionProbes)
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
                                && SupportedRenderingFeatures.active.lightProbeProxyVolumes)
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

                Tree tree;
                renderer.TryGetComponent(out tree);
                if ((tree != null) && (m_LightProbeUsage.intValue == (int)LightProbeUsage.UseProxyVolume))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(m_LightProbeVolumeUnsupportedOnTreesNote.text, MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
            }

            internal bool RenderProbeAnchor(bool useMiniStyle)
            {
                bool useReflectionProbes = !m_ReflectionProbeUsage.hasMultipleDifferentValues && (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue != ReflectionProbeUsage.Off && SupportedRenderingFeatures.active.reflectionProbes;
                bool lightProbesEnabled = !m_LightProbeUsage.hasMultipleDifferentValues && (LightProbeUsage)m_LightProbeUsage.intValue != LightProbeUsage.Off;
                bool needsRendering = useReflectionProbes || lightProbesEnabled;

                if (needsRendering)
                {
                    // anchor field
                    if (!useMiniStyle)
                        EditorGUILayout.PropertyField(m_ProbeAnchor, probeAnchorStyle);
                    else
                        ModuleUI.GUIObject(probeAnchorStyle, m_ProbeAnchor);
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
                    bool useReflectionProbes = !m_ReflectionProbeUsage.hasMultipleDifferentValues && (ReflectionProbeUsage)m_ReflectionProbeUsage.intValue != ReflectionProbeUsage.Off && SupportedRenderingFeatures.active.reflectionProbes;

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
                        GUI.Label(rect, "Weight " + blendInfos[i].weight.ToString("f2", CultureInfo.InvariantCulture.NumberFormat), EditorStyles.miniLabel);
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

        private SerializedProperty m_MaskInteraction;
        private SerializedProperty m_SortingOrder;
        private SerializedProperty m_SortingLayerID;
        private SerializedProperty m_DynamicOccludee;
        private SerializedProperty m_RenderingLayerMask;
        private SerializedProperty m_RendererPriority;
        private SerializedProperty m_SkinnedMotionVectors;
        private SerializedProperty m_MotionVectors;
        private SerializedProperty m_RayTracingMode;
        private SerializedProperty m_RayTraceProcedural;
        private SerializedProperty m_RayTracingAccelStructBuildFlags;
        private SerializedProperty m_RayTracingAccelStructBuildFlagsOverride;
        protected SerializedProperty m_Materials;
        private SerializedProperty m_ForceMeshLod;
        private SerializedProperty m_MeshLodSelectionBias;

        class Styles
        {
            public static readonly GUIContent materials = EditorGUIUtility.TrTextContent("Materials");
            public static readonly GUIContent probeSettings = EditorGUIUtility.TrTextContent("Probes");
            public static readonly GUIContent otherSettings = EditorGUIUtility.TrTextContent("Additional Settings");
            public static readonly GUIContent meshLodSettings = EditorGUIUtility.TrTextContent("Mesh LOD");

            public static readonly GUIContent maskInteractionLabel = EditorGUIUtility.TrTextContent("Mask Interaction", "Renderer's interaction with a Sprite Mask");
            public static readonly GUIContent dynamicOcclusion = EditorGUIUtility.TrTextContent("Dynamic Occlusion", "Controls if dynamic occlusion culling should be performed for this renderer.");
            public static readonly GUIContent motionVectors = EditorGUIUtility.TrTextContent("Motion Vectors", "Specifies whether the Mesh Renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture.");
            public static readonly GUIContent skinnedMotionVectors = EditorGUIUtility.TrTextContent("Skinned Motion Vectors", "Enabling Skinned Motion Vectors will allow generation of high precision motion vectors for the Skinned Mesh. This is achieved by keeping the skinning results of the previous frame in memory thus increasing the memory usage.");
            public static readonly GUIContent renderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");
            public static readonly GUIContent rendererPriority = EditorGUIUtility.TrTextContent("Priority", "Sets the priority value that the render pipeline uses to calculate the rendering order.");
            public static readonly GUIContent rayTracingModeStyle = EditorGUIUtility.TrTextContent("Ray Tracing Mode", "Describes how the acceleration structure associated with a renderer will update for ray tracing.");
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            public static readonly GUIContent[] rayTracingModeOptions = (Enum.GetNames(typeof(RayTracingMode)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
#pragma warning restore RS0030
            public static readonly GUIContent rayTracingGeomStyle = EditorGUIUtility.TrTextContent("Procedural Geometry", "Specifies whether to treat geometry as procedurally defined by an intersection shader or as a Mesh.");
            public static readonly GUIContent rayTracingAccelStructBuildFlagsStyle = EditorGUIUtility.TrTextContent("Acceleration Structure Build Flags", "Specifies whether this renderer overrides the default build flags that you specified when you created a RayTracingAccelerationStructure.");
            public static readonly GUIContent forceMeshLodStyle = EditorGUIUtility.TrTextContent("LOD Override", "Disable automatic LOD selection and set the LOD index to the value in the Override Level property.");
            public static readonly GUIContent forcedMeshLodLevelStyle = EditorGUIUtility.TrTextContent("Override Level", "Set the LOD index to this value.");
            public static readonly GUIContent meshLodSelectionBiasStyle = EditorGUIUtility.TrTextContent("LOD Selection Bias", "The value that Unity adds to the calculated LOD index. Increasing this value results in Unity selecting less detailed LODs, reducing the value - in more detailed LODs.");
        }

        private bool m_IsPrefab;

        protected Probes m_Probes;
        protected RendererLightingSettings m_Lighting;

        protected SavedBool m_ShowMaterials;
        protected SavedBool m_ShowProbeSettings;
        protected SavedBool m_ShowOtherSettings;
        protected SavedBool m_ShowMeshLodSettings;
        protected SavedBool m_ShowRayTracingSettings;

        public virtual void OnEnable()
        {
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
            m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
            m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
            m_DynamicOccludee = serializedObject.FindProperty("m_DynamicOccludee");
            m_RenderingLayerMask = serializedObject.FindProperty("m_RenderingLayerMask");
            m_RendererPriority = serializedObject.FindProperty("m_RendererPriority");
            m_RayTracingMode = serializedObject.FindProperty("m_RayTracingMode");
            m_RayTraceProcedural = serializedObject.FindProperty("m_RayTraceProcedural");
            m_RayTracingAccelStructBuildFlags = serializedObject.FindProperty("m_RayTracingAccelStructBuildFlags");
            m_RayTracingAccelStructBuildFlagsOverride = serializedObject.FindProperty("m_RayTracingAccelStructBuildFlagsOverride");
            m_MotionVectors = serializedObject.FindProperty("m_MotionVectors");
            m_SkinnedMotionVectors = serializedObject.FindProperty("m_SkinnedMotionVectors");
            m_Materials = serializedObject.FindProperty("m_Materials");
            m_ForceMeshLod = serializedObject.FindProperty("m_ForceMeshLod");
            m_MeshLodSelectionBias = serializedObject.FindProperty("m_MeshLodSelectionBias");

            m_ShowMaterials = new SavedBool($"{target.GetType()}.ShowMaterials", true);
            m_ShowProbeSettings = new SavedBool($"{target.GetType()}.ShowProbeSettings", true);
            m_ShowOtherSettings = new SavedBool($"{target.GetType()}.ShowOtherSettings", true);
            m_ShowMeshLodSettings = new SavedBool($"{target.GetType()}.ShowLodSettings", true);
            m_ShowRayTracingSettings = new SavedBool($"{target.GetType()}.ShowRayTracingSettings", true);

            m_Lighting = new RendererLightingSettings(serializedObject);
            m_Lighting.showLightingSettings = new SavedBool($"{target.GetType()}.ShowLightingSettings", true);
            m_Lighting.showLightmapSettings = new SavedBool($"{target.GetType()}.ShowLightmapSettings", true);
            m_Lighting.showBakedLightmap = new SavedBool($"{target.GetType()}.ShowBakedLightmapSettings", false);
            m_Lighting.showPreviewLightmap = new SavedBool($"{target.GetType()}.ShowPreviewLightmapSettings", false);
            m_Lighting.showRealtimeLightmap = new SavedBool($"{target.GetType()}.ShowRealtimeLightmapSettings", false);

            m_Probes = new Probes();
            m_Probes.Initialize(serializedObject);

            // Calculate if the newly selected LOD group is a prefab... they require special handling
            m_IsPrefab = PrefabUtility.IsPartOfPrefabAsset(((Renderer)target).gameObject);
        }

        public void DrawMeshLODLabel(Renderer renderer, int lodCount)
        {
            if (Selection.transforms.Length > 1)
                return;

            if (SceneView.lastActiveSceneView == null)
                return;

            Vector3 position = renderer.bounds.center;
            float size = renderer.bounds.size.magnitude;

            Camera camera = SceneView.lastActiveSceneView.camera;

            ushort meshLODLevel = LODUtility.CalculateMeshLOD(camera, renderer);
            LODGUI.DrawLODLabel(camera, position, size, meshLODLevel, lodCount, LODGUI.kMeshLODColors, "Mesh LOD ", LODGUI.kMeshLODGradientStart, LODGUI.kMeshLODColorGradient);
        }

        protected void LightingSettingsGUI(bool showLightmappSettings)
        {
            LightingSettingsGUI(showLightmappSettings, false, 1);
        }

        protected void LightingSettingsGUI(bool showLightmappSettings, bool showMeshLODSettings, int lodCount)
        {
            m_Lighting.RenderSettings(showLightmappSettings, showMeshLODSettings, lodCount);

            if (SupportedRenderingFeatures.active.rendererProbes)
            {
                m_ShowProbeSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowProbeSettings.value, Styles.probeSettings);

                if (m_ShowProbeSettings.value)
                {
                    EditorGUI.indentLevel += 1;
                    m_Probes.OnGUI(targets, (Renderer)target, false);
                    EditorGUI.indentLevel -= 1;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        protected void Other2DSettingsGUI()
        {
            m_ShowOtherSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowOtherSettings.value, Styles.otherSettings);

            if (m_ShowOtherSettings.value)
            {
                EditorGUI.indentLevel++;

                SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);

                DrawRenderingLayer();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private Vector3 m_LastCameraPos = Vector3.zero;
        private Rect m_LastSceneViewCameraViewport = Rect.zero;
        private float m_LastMeshLODThreshold = 1.0f;

        protected void MeshLODUpdate()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
            {
                return;
            }

            // Update the last camera positon and repaint if the camera has moved
            if (SceneView.lastActiveSceneView.camera.transform.position != m_LastCameraPos || SceneView.lastActiveSceneView.cameraViewport != m_LastSceneViewCameraViewport
                || m_LastMeshLODThreshold != QualitySettings.meshLodThreshold)
            {
                m_LastCameraPos = SceneView.lastActiveSceneView.camera.transform.position;
                m_LastSceneViewCameraViewport = SceneView.lastActiveSceneView.cameraViewport;
                m_LastMeshLODThreshold = QualitySettings.meshLodThreshold;
                Repaint();
            }
        }

        private static void UpdateCamera(float desiredPercentage, Renderer renderer)
        {
            var sceneView = SceneView.lastActiveSceneView;
            var sceneCamera = sceneView.camera;

            // Figure out a distance based on the lod level
            var distance = LODUtility.CalculateMeshLODDistance(sceneCamera, desiredPercentage, renderer);

            LODGUI.UpdateCameraFromLODSlider(renderer.bounds.center, sceneCamera, distance);
        }

        internal static Rect s_ToolTipRect;

        private static readonly int m_LODSliderId = "LODSliderIDHash".GetHashCode();
        private static readonly int m_CameraSliderId = "LODCameraIDHash".GetHashCode();
        private void DrawMeshLODLevelSlider(Rect sliderPosition, List<LODGUI.LODInfo> lods, int startLOD, int lodCount)
        {
            int cameraId = GUIUtility.GetControlID(m_CameraSliderId, FocusType.Passive);
            int sliderId = GUIUtility.GetControlID(m_LODSliderId, FocusType.Passive);
            Event evt = Event.current;
            var renderer = (Renderer)target;

            if (evt.GetTypeForControl(sliderId) == EventType.Repaint)
            {
                for (int i = 0; i < lods.Count; i++)
                {
                    LODGUI.LODInfo lod = lods[i];

                    if (lod.m_RangePosition.Contains(evt.mousePosition))
                    {
                        if (!GUIStyle.IsTooltipActive(lod.LODName))
                            s_ToolTipRect = new Rect(evt.mousePosition, Vector2.zero);
                        GUIStyle.SetMouseTooltip(String.Format("LOD {0} - {1:0.#}%", lod.LODIndex, i == 0 ? 100.0f : lods[i - 1].RawScreenPercent * 100.0f), s_ToolTipRect);
                    }
                }

                LODGUI.DrawMeshLODSlider(sliderPosition, lods, startLOD, lodCount);
            }

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null && !m_IsPrefab)
            {
                var camera = SceneView.lastActiveSceneView.camera;

                var linearHeight = LODUtility.GetMeshRelativeHeight(camera, renderer);
                var relativeHeight = LODGUI.DelinearizeScreenPercentage(linearHeight);

                var cameraRect = LODGUI.CalcLODButton(sliderPosition, Mathf.Clamp01(relativeHeight));
                var cameraIconRect = LODGUI.CalcLODCameraIconRect(cameraRect);
                var cameraLineRect = LODGUI.CalcLODCameraLineRect(cameraRect);
                var cameraPercentRect = LODGUI.CalcLODCameraPctRect(cameraIconRect, cameraLineRect);

                switch (evt.GetTypeForControl(cameraId))
                {
                    case EventType.Repaint:
                    {
                        // Draw a marker to indicate the current scene camera distance
                        var colorCache = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(colorCache.r, colorCache.g, colorCache.b, 0.8f);
                        LODGUI.Styles.m_LODCameraLine.Draw(cameraLineRect, false, false, false, false);
                        GUI.backgroundColor = colorCache;
                        GUI.Label(cameraIconRect, LODGUI.Styles.m_CameraIcon, GUIStyle.none);
                        LODGUI.Styles.m_LODSliderText.Draw(cameraPercentRect, String.Format("{0:0}%", Mathf.Clamp01(linearHeight) * 100.0f), false, false, false, false);
                        break;
                    }
                    case EventType.MouseDown:
                    {
                        if (cameraIconRect.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            var cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);

                            GUIUtility.hotControl = cameraId;
                            BeginLODDrag(cameraPercent, renderer, lods);
                        }
                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == cameraId)
                        {
                            evt.Use();
                            var cameraPercent = LODGUI.GetCameraPercent(evt.mousePosition, sliderPosition);

                            // Change the active LOD level if the camera moves into a new LOD level
                            UpdateLODDrag(cameraPercent, renderer, lods);
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == cameraId)
                        {
                            EndLODDrag();
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                }
            }
        }

        private void BeginLODDrag(float desiredCameraPercentage, Renderer renderer, IEnumerable<LODGUI.LODInfo> lods)
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            UpdateCamera(desiredCameraPercentage, renderer);
            SceneView.lastActiveSceneView.ClearSearchFilter();
            SceneView.lastActiveSceneView.SetSceneViewFilteringForLODGroups(true);
            HierarchyIterator.FilterSingleSceneObject(renderer.gameObject.GetEntityId(), false);
            SceneView.RepaintAll();
        }

        private void EndLODDrag()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            SceneView.lastActiveSceneView.SetSceneViewFilteringForLODGroups(false);
            SceneView.lastActiveSceneView.ClearSearchFilter();
            // Clearing the search filter of a SceneView will not actually reset the visibility values
            // of the GameObjects in the scene so we have to explicitly do that  (case 770915).
            HierarchyIterator.ClearSceneObjectsFilter();
        }

        private void UpdateLODDrag(float desiredCameraPercentage, Renderer renderer, IEnumerable<LODGUI.LODInfo> lods)
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            UpdateCamera(desiredCameraPercentage, renderer);
            SceneView.RepaintAll();
        }


        protected void MeshLodSettingsGUI(int lodCount)
        {
            m_ShowMeshLodSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowMeshLodSettings.value, Styles.meshLodSettings);

            if (m_ShowMeshLodSettings.value)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var renderer = ((Renderer)target);

                    if (Selection.gameObjects.Length == 1)
                    {
                        // Add some space at the top..
                        GUILayout.Space(LODGUI.kSliderBarTopMargin);

                        // Precalculate and cache the slider bar position for this update
                        var sliderBarPosition = GUILayoutUtility.GetRect(0, LODGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));

                        List<float> lodValue = new List<float>();
                        List<int> lodIndex = new List<int>();

                        if (renderer.forceMeshLod >= 0)
                        {
                            lodValue.Add(0.0f);
                            lodIndex.Add(renderer.forceMeshLod);
                        }
                        else
                        {
                            for (int i = 1 + Mathf.Max(0, (int)renderer.meshLodSelectionBias); i < lodCount; i++)
                            {
                                var value = LODUtility.CalculateMeshLODBoundsPercentage(SceneView.lastActiveSceneView.camera, (ushort)i, renderer);
                                if (value < 1.0f)
                                {
                                    lodValue.Add(value);
                                    lodIndex.Add(i - 1);
                                }
                            }
                            lodValue.Add(0.0f);
                            lodIndex.Add(lodCount - 1);
                        }

                        // Precalculate the lod info (button locations / ranges ect)
                        var lods = LODGUI.CreateLODInfos(lodValue.Count, sliderBarPosition,
                            i => String.Format($"LOD {(ushort)lodIndex[i]}"),
                            i => lodValue[i]);

                        DrawMeshLODLevelSlider(sliderBarPosition, lods, lodIndex[0], lodCount);
                        EditorApplication.update += MeshLODUpdate;

                        GUILayout.Space(LODGUI.kSliderBarBottomMargin);
                    }

                    EditorGUI.BeginChangeCheck();

                    var forceMeshLODEnabled = EditorGUILayout.Toggle(Styles.forceMeshLodStyle, m_ForceMeshLod.intValue != -1);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (forceMeshLODEnabled)
                            m_ForceMeshLod.intValue = 0;
                        else
                            m_ForceMeshLod.intValue = -1;
                    }

                    if (forceMeshLODEnabled)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.IntSlider(m_ForceMeshLod, 0, lodCount - 1, Styles.forcedMeshLodLevelStyle);
                        }
                    }

                    using (new EditorGUI.DisabledScope(forceMeshLODEnabled))
                    {
                        int maxLevel = lodCount - 1;
                        EditorGUILayout.Slider(m_MeshLodSelectionBias, -maxLevel, maxLevel, Styles.meshLodSelectionBiasStyle);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void OtherSettingsGUI(bool showMotionVectors, bool showSkinnedMotionVectors = false, bool showSortingLayerFields = false)
        {
            m_ShowOtherSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowOtherSettings.value, Styles.otherSettings);

            if (m_ShowOtherSettings.value)
            {
                EditorGUI.indentLevel++;

                if (SupportedRenderingFeatures.active.motionVectors)
                {
                    if (showMotionVectors)
                        EditorGUILayout.PropertyField(m_MotionVectors, Styles.motionVectors, true);
                    else if (showSkinnedMotionVectors)
                        EditorGUILayout.PropertyField(m_SkinnedMotionVectors, Styles.skinnedMotionVectors, true);
                }

                EditorGUILayout.PropertyField(m_DynamicOccludee, Styles.dynamicOcclusion);


                if (showSortingLayerFields)
                    SortingLayerEditorUtility.RenderSortingLayerFields(m_SortingOrder, m_SortingLayerID);

                DrawRenderingLayer();
                DrawRendererPriority(m_RendererPriority);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void DrawMaterials()
        {
            EditorGUILayout.PropertyField(m_Materials);
        }

        protected void DrawRenderingLayer()
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            DrawRenderingLayer(m_RenderingLayerMask, target as Renderer, targets.ToArray());
#pragma warning restore RS0030
        }

        internal static void DrawRenderingLayer(SerializedProperty layerMask, Renderer target, Object[] targets, bool useMiniStyle = false)
        {
            if (!GraphicsSettings.isScriptableRenderPipelineEnabled || target == null)
                return;

            using var mixedScope = new EditorGUI.MixedValueScope(layerMask.hasMultipleDifferentValues);
            using var changeScope = new EditorGUI.ChangeCheckScope();

            var mask = target.renderingLayerMask;
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.renderingLayerMask, layerMask);
            if (useMiniStyle)
            {
                rect = ModuleUI.PrefixLabel(rect, Styles.renderingLayerMask);
                mask = EditorGUI.RenderingLayerMaskField(rect, GUIContent.none, mask, ParticleSystemStyles.Get().popup);
            }
            else
                mask = EditorGUI.RenderingLayerMaskField(rect,Styles.renderingLayerMask, mask);
            EditorGUI.EndProperty();

            if (changeScope.changed)
            {
                Undo.RecordObjects(targets, "Set rendering layer mask");
                for (var i = 0; i < targets.Length; i++)
                {
                    var t = targets[i];
                    var r = t as Renderer;
                    if (r == null)
                        continue;
                    r.renderingLayerMask = mask;
                    EditorUtility.SetDirty(t);
                }
            }
        }

        internal static void DrawRendererPriority(SerializedProperty rendererPrority, bool useMiniStyle = false)
        {
            if (!SupportedRenderingFeatures.active.rendererPriority)
                return;

            if (!useMiniStyle)
            {
                EditorGUILayout.PropertyField(rendererPrority, Styles.rendererPriority);
            }
            else
            {
                ModuleUI.GUIInt(Styles.rendererPriority, rendererPrority);
            }
        }

        protected void RayTracingSettingsGUI()
        {
            if (SystemInfo.supportsRayTracingShaders || SystemInfo.supportsInlineRayTracing)
            {
                m_ShowRayTracingSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowRayTracingSettings.value, "Ray Tracing");
                if (m_ShowRayTracingSettings.value)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.Popup(m_RayTracingMode, Styles.rayTracingModeOptions, Styles.rayTracingModeStyle);

                    EditorGUILayout.PropertyField(m_RayTraceProcedural, Styles.rayTracingGeomStyle);

                    // Ray Tracing Acceleration Structure Build Flags
                    {
                        Rect rect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(rect, Styles.rayTracingAccelStructBuildFlagsStyle, m_RayTracingAccelStructBuildFlagsOverride);

                        rect = EditorGUI.PrefixLabel(rect, Styles.rayTracingAccelStructBuildFlagsStyle);

                        EditorGUI.BeginChangeCheck();

                        Rect toggleRect = rect;

                        toggleRect.x -= EditorGUI.indent;
                        toggleRect.width = 30;

                        bool overrideFlags = EditorGUI.Toggle(toggleRect, m_RayTracingAccelStructBuildFlagsOverride.boolValue);
                        int buildFlags = m_RayTracingAccelStructBuildFlags.intValue;

                        EditorGUI.EndProperty();

                        bool disableFlagsField = m_RayTracingAccelStructBuildFlags.hasMultipleDifferentValues;

                        using (new EditorGUI.DisabledScope(!m_RayTracingAccelStructBuildFlagsOverride.boolValue || m_RayTracingAccelStructBuildFlagsOverride.hasMultipleDifferentValues || disableFlagsField))
                        {
                            rect.xMin += EditorGUI.kDefaultSpacing;

                            if (m_RayTracingAccelStructBuildFlags.hasMultipleDifferentValues)
                                EditorGUI.showMixedValue = true;

                            buildFlags = (int)(RayTracingAccelerationStructureBuildFlags)EditorGUI.EnumFlagsField(rect, (RayTracingAccelerationStructureBuildFlags)m_RayTracingAccelStructBuildFlags.intValue);

                            EditorGUI.showMixedValue = false;

                            if (buildFlags == -1 && !disableFlagsField)
                            {
                                buildFlags = 0;

                                foreach (RayTracingAccelerationStructureBuildFlags type in Enum.GetValues(typeof(RayTracingAccelerationStructureBuildFlags)))
                                {
                                    buildFlags = buildFlags | (int)type;
                                }
                            }

                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            m_RayTracingAccelStructBuildFlagsOverride.boolValue = overrideFlags;

                            if (!disableFlagsField)
                                m_RayTracingAccelStructBuildFlags.intValue = buildFlags;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
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
