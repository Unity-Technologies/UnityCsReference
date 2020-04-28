// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine.Rendering;
using UnityEngineInternal;
using System.Globalization;

namespace UnityEditor
{
    internal class RendererLightingSettings
    {
        static class Styles
        {
            public static readonly GUIContent optimizeRealtimeUVs = EditorGUIUtility.TrTextContent("Optimize Realtime UVs", "Specifies whether the authored mesh UVs get optimized for Realtime Global Illumination or not. When enabled, the authored UVs can get merged, and are scaled and packed for optimization purposes. When disabled, the authored UVs are scaled and packed, but not merged.");
            public static readonly GUIContent ignoreNormalsForChartDetection = EditorGUIUtility.TrTextContent("Ignore Normals", "When enabled, prevents the UV charts from being split during the precompute process for Realtime Global Illumination lighting.");
            public static readonly int[] minimumChartSizeValues = { 2, 4 };
            public static readonly GUIContent[] minimumChartSizeStrings =
            {
                EditorGUIUtility.TrTextContent("2 (Minimum)"),
                EditorGUIUtility.TrTextContent("4 (Stitchable)"),
            };

            public static readonly int[] receiveGILightmapValues = { (int)ReceiveGI.Lightmaps, (int)ReceiveGI.LightProbes };
            public static readonly GUIContent[] receiveGILightmapStrings =
            {
                EditorGUIUtility.TrTextContent("Lightmaps"),
                EditorGUIUtility.TrTextContent("Light Probes")
            };

            public static readonly GUIContent lightingSettings = EditorGUIUtility.TrTextContent("Lighting");
            public static readonly GUIContent lighting = new GUIContent(EditorGUIUtility.TrTextContent("Lighting").text); // prevent the Lighting window icon from being added
            public static readonly GUIContent minimumChartSize = EditorGUIUtility.TrTextContent("Min Chart Size", "Specifies the minimum texel size used for a UV chart. If stitching is required, a value of 4 will create a chart of 4x4 texels to store lighting and directionality. If stitching is not required, a value of 2 will reduce the texel density and provide better lighting build times and run time performance.");
            public static readonly GUIContent importantGI = EditorGUIUtility.TrTextContent("Prioritize Illumination", "When enabled, the object will be marked as a priority object and always included in lighting calculations. Useful for objects that will be strongly emissive to make sure that other objects will be illuminated by this object.");
            public static readonly GUIContent stitchLightmapSeams = EditorGUIUtility.TrTextContent("Stitch Seams", "When enabled, seams in baked lightmaps will get smoothed.");
            public static readonly GUIContent autoUVMaxDistance = EditorGUIUtility.TrTextContent("Max Distance", "Specifies the maximum worldspace distance to be used for UV chart simplification. If charts are within this distance, they will be simplified for optimization purposes.");
            public static readonly GUIContent autoUVMaxAngle = EditorGUIUtility.TrTextContent("Max Angle", "Specifies the maximum angle in degrees between faces sharing a UV edge. If the angle between the faces is below this value, the UV charts will be simplified.");
            public static readonly GUIContent lightmapParameters = EditorGUIUtility.TrTextContent("Lightmap Parameters", "Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public static readonly GUIContent atlasTilingX = EditorGUIUtility.TrTextContent("Tiling X");
            public static readonly GUIContent atlasTilingY = EditorGUIUtility.TrTextContent("Tiling Y");
            public static readonly GUIContent atlasOffsetX = EditorGUIUtility.TrTextContent("Offset X");
            public static readonly GUIContent atlasOffsetY = EditorGUIUtility.TrTextContent("Offset Y");
            public static readonly GUIContent clampedSize = EditorGUIUtility.TrTextContent("Object's size in lightmap has reached the max atlas size.", "If you need higher resolution for this object, divide it into smaller meshes or set higher max atlas size via the LightingSettings class.");
            public static readonly GUIContent clampedPackingResolution = EditorGUIUtility.TrTextContent("Object's size in the realtime lightmap has reached the maximum size. If you need higher resolution for this object, divide it into smaller meshes.");
            public static readonly GUIContent zeroAreaPackingMesh = EditorGUIUtility.TrTextContent("Mesh used by the renderer has zero UV or surface area. Non zero area is required for lightmapping.");
            public static readonly GUIContent noNormalsNoLightmapping = EditorGUIUtility.TrTextContent("Mesh used by the renderer doesn't have normals. Normals are needed for lightmapping.");
            public static readonly GUIContent noVerticesNoLightmapping = EditorGUIUtility.TrTextContent("Mesh used by the renderer doesn't have vertices. Vertices are needed for lightmapping.");
            public static readonly GUIContent unsupportedTopology = EditorGUIUtility.TrTextContent("Mesh with point, strip or line topology is not supported by lightmapping.");
            public static readonly GUIContent uvOverlap = EditorGUIUtility.TrTextContent("This GameObject has overlapping UVs. Please adjust Mesh Importer settings or increase chart padding in your modeling package.");
            public static readonly GUIContent atlas = EditorGUIUtility.TrTextContent("Baked Lightmap");
            public static readonly GUIContent realtimeLM = EditorGUIUtility.TrTextContent("Realtime Lightmap");
            public static readonly GUIContent scaleInLightmap = EditorGUIUtility.TrTextContent("Scale In Lightmap", "Specifies the relative size of object's UVs within a lightmap. A value of 0 will result in the object not being lightmapped, but still contribute lighting to other objects in the Scene.");
            public static readonly GUIContent albedoScale = EditorGUIUtility.TrTextContent("Albedo Scale", "Specifies the relative size of object's UVs within its albedo texture that is used when calculating the influence on surrounding objects.");
            public static readonly GUIContent atlasIndex = EditorGUIUtility.TrTextContent("Lightmap Index");
            public static readonly GUIContent lightmapResolution = EditorGUIUtility.TrTextContent("Lightmap Resolution");
            public static readonly GUIContent lightmapObjectScale = EditorGUIUtility.TrTextContent("Lightmap Object Scale");
            public static readonly GUIContent pvrInstanceHash = EditorGUIUtility.TrTextContent("Instance Hash", "The hash of the baked GI instance.");
            public static readonly GUIContent pvrAtlasHash = EditorGUIUtility.TrTextContent("Atlas Hash", "The hash of the atlas this baked GI instance is a part of.");
            public static readonly GUIContent pvrAtlasInstanceOffset = EditorGUIUtility.TrTextContent("Atlas Instance Offset", "The offset into the transform array instances of this atlas start at.");
            public static readonly GUIContent realtimeLMResolution = EditorGUIUtility.TrTextContent("System Resolution", "The resolution in texels of the realtime lightmap that this renderer belongs to.");
            public static readonly GUIContent realtimeLMInstanceResolution = EditorGUIUtility.TrTextContent("Instance Resolution", "The resolution in texels of the realtime lightmap packed instance.");
            public static readonly GUIContent realtimeLMInputSystemHash = EditorGUIUtility.TrTextContent("System Hash", "The hash of the realtime system that the renderer belongs to.");
            public static readonly GUIContent realtimeLMInstanceHash = EditorGUIUtility.TrTextContent("Instance Hash", "The hash of the realtime GI instance.");
            public static readonly GUIContent realtimeLMGeometryHash = EditorGUIUtility.TrTextContent("Geometry Hash", "The hash of the realtime GI geometry that the renderer is using.");
            public static readonly GUIContent uvCharting = EditorGUIUtility.TrTextContent("Realtime UVs");
            public static readonly GUIContent lightmapSettings = EditorGUIUtility.TrTextContent("Lightmapping");
            public static readonly GUIContent castShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "Specifies whether a geometry creates shadows or not when a shadow-casting Light shines on it.");
            public static readonly GUIContent receiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "When enabled, any shadows cast from other objects are drawn on the geometry.");
            public static readonly GUIContent shadowBias = EditorGUIUtility.TrTextContent("Shadow Bias", "Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the trail width at each segment.");
            public static readonly GUIContent contributeGI = EditorGUIUtility.TrTextContent("Contribute Global Illumination", "When enabled, this GameObject influences lightmaps and Light Probes. If you want this object itself to be lightmapped, you must enable this property.");
            public static readonly GUIContent receiveGITitle = EditorGUIUtility.TrTextContent("Receive Global Illumination", "If enabled, this GameObject receives global illumination from lightmaps or Light Probes. To use lightmaps, Contribute Global Illumination must be enabled.");
            public static readonly GUIContent lightmapParametersDefault = EditorGUIUtility.TrTextContent("Scene Default Parameters");

            public static readonly GUIContent resolutionTooHighWarning = EditorGUIUtility.TrTextContent("Precompute/indirect resolution for this terrain is probably too high. Use a lower realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a lower resolution setting. Otherwise it may take a very long time to bake and memory consumption during and after the bake may be very high.");
            public static readonly GUIContent resolutionTooLowWarning = EditorGUIUtility.TrTextContent("Precompute/indirect resolution for this terrain is probably too low. If the Clustering stage takes a long time, try using a higher realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a higher resolution setting.");
            public static readonly GUIContent giNotEnabledInfo = EditorGUIUtility.TrTextContent("Lightmapping settings are currently disabled. Enable Baked Global Illumination or Realtime Global Illumination to display these settings.");
            public static readonly GUIContent isPresetInfo = EditorGUIUtility.TrTextContent("The Contribute Global Illumination property cannot be stored in a preset.");

            public static readonly GUIContent openPreview = EditorGUIUtility.TrTextContent("Open Preview");
            public static readonly GUIStyle openPreviewStyle = EditorStyles.objectFieldThumb.name + "LightmapPreviewOverlay";
            public static readonly int previewPadding = 30;
            public static readonly int previewWidth = 104;
        }

        SerializedObject m_SerializedObject;
        SerializedObject m_GameObjectsSerializedObject;

        SerializedProperty m_StaticEditorFlags;
        SerializedProperty m_ImportantGI;
        SerializedProperty m_StitchLightmapSeams;
        SerializedProperty m_LightmapParameters;
        SerializedProperty m_LightmapIndex;
        SerializedProperty m_LightmapTilingOffsetX;
        SerializedProperty m_LightmapTilingOffsetY;
        SerializedProperty m_LightmapTilingOffsetZ;
        SerializedProperty m_LightmapTilingOffsetW;
        SerializedProperty m_PreserveUVs;
        SerializedProperty m_AutoUVMaxDistance;
        SerializedProperty m_IgnoreNormalsForChartDetection;
        SerializedProperty m_AutoUVMaxAngle;
        SerializedProperty m_MinimumChartSize;
        SerializedProperty m_LightmapScale;
        SerializedProperty m_ReceiveGI;
        SerializedProperty m_CastShadows;
        SerializedProperty m_ReceiveShadows;

        Renderer[] m_Renderers;
        Terrain[] m_Terrains;

        internal SavedBool showLightingSettings { get; set; }
        internal SavedBool showLightmapSettings { get; set; }
        internal SavedBool showBakedLightmap { get; set; }
        internal SavedBool showRealtimeLightmap { get; set; }

        VisualisationGITexture m_CachedRealtimeTexture;
        VisualisationGITexture m_CachedBakedTexture;

        private bool isPrefabAsset
        {
            get
            {
                if (m_SerializedObject == null || m_SerializedObject.targetObject == null)
                    return false;

                return PrefabUtility.IsPartOfPrefabAsset(m_SerializedObject.targetObject);
            }
        }

        private bool isPreset { get; }

        private float CalcLODScale(bool isMeshRenderer)
        {
            float lodScale = 1.0f;
            if (isMeshRenderer && (m_Renderers != null) && (m_Renderers.Length > 0))
            {
                lodScale = LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[0]);
                for (int i = 1; i < m_Renderers.Length; i++)
                {
                    if (!Mathf.Approximately(lodScale, LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[i])))
                        lodScale = 1.0F;
                }
            }
            return lodScale;
        }

        public RendererLightingSettings(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            m_GameObjectsSerializedObject = new SerializedObject(serializedObject.targetObjects.Select(t => ((Component)t).gameObject).ToArray());

            m_ImportantGI = m_SerializedObject.FindProperty("m_ImportantGI");
            m_StitchLightmapSeams = m_SerializedObject.FindProperty("m_StitchLightmapSeams");
            m_LightmapParameters = m_SerializedObject.FindProperty("m_LightmapParameters");
            m_LightmapIndex = m_SerializedObject.FindProperty("m_LightmapIndex");
            m_LightmapTilingOffsetX = m_SerializedObject.FindProperty("m_LightmapTilingOffset.x");
            m_LightmapTilingOffsetY = m_SerializedObject.FindProperty("m_LightmapTilingOffset.y");
            m_LightmapTilingOffsetZ = m_SerializedObject.FindProperty("m_LightmapTilingOffset.z");
            m_LightmapTilingOffsetW = m_SerializedObject.FindProperty("m_LightmapTilingOffset.w");
            m_PreserveUVs = m_SerializedObject.FindProperty("m_PreserveUVs");
            m_AutoUVMaxDistance = m_SerializedObject.FindProperty("m_AutoUVMaxDistance");
            m_IgnoreNormalsForChartDetection = m_SerializedObject.FindProperty("m_IgnoreNormalsForChartDetection");
            m_AutoUVMaxAngle = m_SerializedObject.FindProperty("m_AutoUVMaxAngle");
            m_MinimumChartSize = m_SerializedObject.FindProperty("m_MinimumChartSize");
            m_LightmapScale = m_SerializedObject.FindProperty("m_ScaleInLightmap");
            m_CastShadows = m_SerializedObject.FindProperty("m_CastShadows");
            m_ReceiveShadows = m_SerializedObject.FindProperty("m_ReceiveShadows");
            m_ReceiveGI = m_SerializedObject.FindProperty("m_ReceiveGI");

            m_Renderers = m_SerializedObject.targetObjects.OfType<Renderer>().ToArray();
            m_Terrains = m_SerializedObject.targetObjects.OfType<Terrain>().ToArray();

            m_StaticEditorFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");

            if (m_SerializedObject == null || m_SerializedObject.targetObject == null)
                isPreset = false;
            else
                isPreset = m_SerializedObject.targetObject is Component ? ((int)(m_SerializedObject.targetObject as Component).gameObject.hideFlags == 93) : !AssetDatabase.Contains(m_SerializedObject.targetObject);
        }

        public void RenderSettings(bool showLightmapSettings)
        {
            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjectsCount == 0)
                return;

            var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
            var lightmapper = settings.lightmapper;
            bool bakedGI = settings.bakedGI;
            bool realtimeGI = settings.realtimeGI;

            m_GameObjectsSerializedObject.Update();

            ReceiveGI receiveGI = (ReceiveGI)m_ReceiveGI.intValue;
            bool contributeGI = isPreset ? true : (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.ContributeGI) != 0;
            bool showEnlightenSettings = isPreset || isPrefabAsset || realtimeGI || (bakedGI && lightmapper == LightingSettings.Lightmapper.Enlighten);

            // m_ReceiveGI might still be set to Lightmaps, but LightProbes is shown in the inspector since the contributeGI if off.
            // In this case we still have to mark it as "multiple values" even though both have "Lightmaps" as the value, but one is showing a grayed out "Light Probes" in the UI
            bool showMixedGIValue = m_ReceiveGI.hasMultipleDifferentValues || (isPreset ? false : ((m_StaticEditorFlags.hasMultipleDifferentValuesBitwise & (int)StaticEditorFlags.ContributeGI) != 0));

            showLightingSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingSettings.value, Styles.lightingSettings);

            if (showLightingSettings.value)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.PropertyField(m_CastShadows, Styles.castShadows, true);
                bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();

                if (SupportedRenderingFeatures.active.receiveShadows)
                {
                    using (new EditorGUI.DisabledScope(isDeferredRenderingPath))
                        EditorGUILayout.PropertyField(m_ReceiveShadows, Styles.receiveShadows, true);
                }

                if (!showLightmapSettings)
                {
                    EditorGUI.indentLevel -= 1;

                    EditorGUILayout.EndFoldoutHeaderGroup();

                    return;
                }

                using (new EditorGUI.DisabledScope(isPreset))
                    contributeGI = ContributeGISettings();

                if (isPreset)
                    EditorGUILayout.HelpBox(Styles.isPresetInfo.text, MessageType.Info);

                if (!(bakedGI || realtimeGI) && contributeGI && !isPrefabAsset && !isPreset)
                {
                    EditorGUILayout.HelpBox(Styles.giNotEnabledInfo.text, MessageType.Info);
                    EditorGUI.indentLevel -= 1;

                    EditorGUILayout.EndFoldoutHeaderGroup();

                    return;
                }

                if (contributeGI)
                {
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.BeginProperty(rect, Styles.receiveGITitle, m_ReceiveGI);
                    EditorGUI.BeginChangeCheck();

                    receiveGI = (ReceiveGI)EditorGUI.IntPopup(rect, Styles.receiveGITitle, (int)receiveGI, Styles.receiveGILightmapStrings, Styles.receiveGILightmapValues);

                    if (EditorGUI.EndChangeCheck())
                        m_ReceiveGI.intValue = (int)receiveGI;

                    EditorGUI.EndProperty();

                    if (showEnlightenSettings)
                        EditorGUILayout.PropertyField(m_ImportantGI, Styles.importantGI);

                    if (receiveGI == ReceiveGI.LightProbes && !showMixedGIValue)
                    {
                        LightmapScaleGUI(true, Styles.albedoScale, true);
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.showMixedValue = showMixedGIValue;
                        receiveGI = (ReceiveGI)EditorGUILayout.IntPopup(Styles.receiveGITitle, (int)ReceiveGI.LightProbes, Styles.receiveGILightmapStrings, Styles.receiveGILightmapValues);
                        EditorGUI.showMixedValue = false;
                    }
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (showLightmapSettings && contributeGI && receiveGI == ReceiveGI.Lightmaps && !showMixedGIValue)
            {
                this.showLightmapSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(this.showLightmapSettings.value, Styles.lightmapSettings);

                if (this.showLightmapSettings.value)
                {
                    EditorGUI.indentLevel += 1;

                    bool showProgressiveSettings = isPreset || isPrefabAsset || (bakedGI && lightmapper != LightingSettings.Lightmapper.Enlighten);

                    LightmapScaleGUI(true, Styles.scaleInLightmap, false);

                    if (showProgressiveSettings)
                        EditorGUILayout.PropertyField(m_StitchLightmapSeams, Styles.stitchLightmapSeams);

                    LightmapParametersGUI(m_LightmapParameters, Styles.lightmapParameters);

                    if (showEnlightenSettings)
                    {
                        RendererUVSettings();
                    }

                    if ((m_Renderers != null) && (m_Renderers.Length > 0))
                    {
                        ShowAtlasGUI(m_Renderers[0].GetInstanceID(), true);
                        ShowRealtimeLMGUI(m_Renderers[0]);

                        if (Lightmapping.HasZeroAreaMesh(m_Renderers[0]))
                            EditorGUILayout.HelpBox(Styles.zeroAreaPackingMesh.text, MessageType.Warning);

                        DisplayMeshWarning();

                        if (showEnlightenSettings)
                        {
                            if (Lightmapping.HasClampedResolution(m_Renderers[0]))
                                EditorGUILayout.HelpBox(Styles.clampedPackingResolution.text, MessageType.Warning);
                        }

                        if (showProgressiveSettings)
                        {
                            if (Lightmapping.HasUVOverlaps(m_Renderers[0]))
                                EditorGUILayout.HelpBox(Styles.uvOverlap.text, MessageType.Warning);
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        public void RenderTerrainSettings()
        {
            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjectsCount == 0)
                return;

            m_GameObjectsSerializedObject.Update();

            var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
            bool bakedGI = settings.bakedGI;
            bool realtimeGI = settings.realtimeGI;

            bool contributeGI = isPreset ? true : (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.ContributeGI) != 0;

            showLightingSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingSettings.value, Styles.lightingSettings);

            if (showLightingSettings.value)
            {
                EditorGUI.indentLevel += 1;

                using (new EditorGUI.DisabledScope(isPreset))
                    contributeGI = ContributeGISettings();

                if (isPreset)
                    EditorGUILayout.HelpBox(Styles.isPresetInfo.text, MessageType.Info);

                if (!(bakedGI || realtimeGI) && contributeGI && !isPrefabAsset && !isPreset)
                {
                    EditorGUILayout.HelpBox(Styles.giNotEnabledInfo.text, MessageType.Info);
                    EditorGUI.indentLevel -= 1;
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntPopup(Styles.receiveGITitle, 1, Styles.receiveGILightmapStrings, Styles.receiveGILightmapValues);
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            if (contributeGI)
            {
                showLightmapSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(showLightmapSettings.value, Styles.lightmapSettings);

                if (showLightmapSettings.value)
                {
                    EditorGUI.indentLevel += 1;

                    if (GUI.enabled)
                        ShowTerrainChunks(m_Terrains);

                    LightmapScaleGUI(false, Styles.scaleInLightmap, false);

                    LightmapParametersGUI(m_LightmapParameters, Styles.lightmapParameters);

                    if ((m_Terrains != null) && (m_Terrains.Length > 0))
                    {
                        if (GUI.enabled && m_Terrains.Length == 1 && m_Terrains[0].terrainData != null)
                            ShowBakePerformanceWarning(m_Terrains[0]);

                        ShowAtlasGUI(m_Terrains[0].GetInstanceID(), false);
                        ShowRealtimeLMGUI(m_Terrains[0]);
                    }

                    EditorGUI.indentLevel -= 1;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();

                GUILayout.Space(10);
            }
        }

        bool ContributeGISettings()
        {
            if (isPreset)
            {
                EditorGUILayout.Toggle(Styles.contributeGI, true);
                return true;
            }

            bool contributeGI = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.ContributeGI) != 0;
            bool mixedValue = (m_StaticEditorFlags.hasMultipleDifferentValuesBitwise & (int)StaticEditorFlags.ContributeGI) != 0;
            EditorGUI.showMixedValue = mixedValue;

            EditorGUI.BeginChangeCheck();
            contributeGI = EditorGUILayout.Toggle(Styles.contributeGI, contributeGI);

            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(m_GameObjectsSerializedObject.targetObjects, (int)StaticEditorFlags.ContributeGI, contributeGI);
                m_GameObjectsSerializedObject.SetIsDifferentCacheDirty();
                m_GameObjectsSerializedObject.Update();
            }

            EditorGUI.showMixedValue = false;

            return contributeGI && !mixedValue;
        }

        void RendererUVSettings()
        {
            // TODO: This is very temporary and the flag needs to be changed.
            bool optimizeRealtimeUVs = !m_PreserveUVs.boolValue;

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.optimizeRealtimeUVs, m_PreserveUVs);
            EditorGUI.BeginChangeCheck();

            optimizeRealtimeUVs = EditorGUI.Toggle(rect, Styles.optimizeRealtimeUVs, optimizeRealtimeUVs);

            if (EditorGUI.EndChangeCheck())
            {
                m_PreserveUVs.boolValue = !optimizeRealtimeUVs;
            }
            EditorGUI.EndProperty();

            if (optimizeRealtimeUVs)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AutoUVMaxDistance, Styles.autoUVMaxDistance);
                if (m_AutoUVMaxDistance.floatValue < 0.0f)
                    m_AutoUVMaxDistance.floatValue = 0.0f;
                EditorGUILayout.Slider(m_AutoUVMaxAngle, 0, 180, Styles.autoUVMaxAngle);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_IgnoreNormalsForChartDetection, Styles.ignoreNormalsForChartDetection);

            EditorGUILayout.IntPopup(m_MinimumChartSize, Styles.minimumChartSizeStrings, Styles.minimumChartSizeValues, Styles.minimumChartSize);
        }

        void ShowClampedSizeInLightmapGUI(float lightmapScale, float cachedSurfaceArea, bool isSSD)
        {
            float sizeInLightmap = Mathf.Sqrt(cachedSurfaceArea) * Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapResolution * lightmapScale;

            if (sizeInLightmap > Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapMaxSize)
                EditorGUILayout.HelpBox(Styles.clampedSize.text, MessageType.Info);
        }

        void LightmapScaleGUI(bool isMeshRenderer, GUIContent title, bool isSSD)
        {
            // SSDs (with the exception of those being computed with Enlighten) do not end up in a lightmap,
            // therefore we do not show clamping information.
            if (isSSD && Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper != LightingSettings.Lightmapper.Enlighten)
                return;

            float lodScale = CalcLODScale(isMeshRenderer);
            float lightmapScale = lodScale * m_LightmapScale.floatValue;

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.scaleInLightmap, m_LightmapScale);
            EditorGUI.BeginChangeCheck();
            lightmapScale = EditorGUI.FloatField(rect, title, lightmapScale);
            if (EditorGUI.EndChangeCheck())
                m_LightmapScale.floatValue = Mathf.Max(lightmapScale / Mathf.Max(lodScale, float.Epsilon), 0.0f);
            EditorGUI.EndProperty();

            float cachedSurfaceArea = 0.0f;

            if (isMeshRenderer)
            {
                if ((m_Renderers != null) && (m_Renderers.Length > 0))
                {
                    lightmapScale = lightmapScale * LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[0]);

                    // tell the user if the object's size in lightmap has reached the max atlas size
                    cachedSurfaceArea = InternalMeshUtil.GetCachedMeshSurfaceArea((MeshRenderer)m_Renderers[0]);
                }
            }
            else //terrain
            {
                // tell the user if the object's size in lightmap has reached the max atlas size
                if ((m_Terrains != null) && (m_Terrains.Length > 0))
                {
                    var terrainData = m_Terrains[0].terrainData;
                    cachedSurfaceArea = terrainData != null ? terrainData.size.x * terrainData.size.z : 0.0f;
                }
            }

            ShowClampedSizeInLightmapGUI(lightmapScale, cachedSurfaceArea, isSSD);
        }

        void ShowAtlasGUI(int instanceID, bool isMeshRenderer)
        {
            if (m_LightmapIndex == null)
                return;

            Hash128 contentHash = LightmapVisualizationUtility.GetBakedGITextureHash(m_LightmapIndex.intValue, 0, GITextureType.Baked);

            // if we need to fetch a new texture
            if (m_CachedBakedTexture.texture == null || m_CachedBakedTexture.contentHash != contentHash)
                m_CachedBakedTexture = LightmapVisualizationUtility.GetBakedGITexture(m_LightmapIndex.intValue, 0, GITextureType.Baked);

            if (m_CachedBakedTexture.texture == null)
                return;

            showBakedLightmap.value = EditorGUILayout.Foldout(showBakedLightmap.value, Styles.atlas, true);

            if (!showBakedLightmap.value)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedBakedTexture.texture, false, instanceID);

            GUILayout.BeginVertical();

            GUILayout.Label(Styles.atlasIndex.text + ": " + m_LightmapIndex.intValue);
            GUILayout.Label(Styles.atlasTilingX.text + ": " + m_LightmapTilingOffsetX.floatValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
            GUILayout.Label(Styles.atlasTilingY.text + ": " + m_LightmapTilingOffsetY.floatValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
            GUILayout.Label(Styles.atlasOffsetX.text + ": " + m_LightmapTilingOffsetZ.floatValue.ToString(CultureInfo.InvariantCulture.NumberFormat));
            GUILayout.Label(Styles.atlasOffsetY.text + ": " + m_LightmapTilingOffsetW.floatValue.ToString(CultureInfo.InvariantCulture.NumberFormat));

            var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();

            float lightmapResolution = settings.lightmapResolution * CalcLODScale(isMeshRenderer) * m_LightmapScale.floatValue;

            if (isMeshRenderer && (m_Renderers != null) && (m_Renderers.Length > 0))
            {
                Transform transform = m_Renderers[0].GetComponent<Transform>();
                float lightmapObjectScale = System.Math.Min(System.Math.Min(transform.localScale.x, transform.localScale.y), transform.localScale.z);
                GUILayout.Label(Styles.lightmapResolution.text + ": " + lightmapResolution.ToString(CultureInfo.InvariantCulture.NumberFormat));
                GUILayout.Label(Styles.lightmapObjectScale.text + ": " + lightmapObjectScale.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            bool showProgressiveInfo = !isPreset && (settings.bakedGI && settings.lightmapper != LightingSettings.Lightmapper.Enlighten);

            if (showProgressiveInfo && Unsupported.IsDeveloperMode())
            {
                Hash128 instanceHash;
                Lightmapping.GetPVRInstanceHash(instanceID, out instanceHash);
                EditorGUILayout.LabelField(Styles.pvrInstanceHash, GUIContent.Temp(instanceHash.ToString()));

                Hash128 atlasHash;
                Lightmapping.GetPVRAtlasHash(instanceID, out atlasHash);
                EditorGUILayout.LabelField(Styles.pvrAtlasHash, GUIContent.Temp(atlasHash.ToString()));

                int atlasInstanceOffset;
                Lightmapping.GetPVRAtlasInstanceOffset(instanceID, out atlasInstanceOffset);
                EditorGUILayout.LabelField(Styles.pvrAtlasInstanceOffset, GUIContent.Temp(atlasInstanceOffset.ToString()));
            }
            EditorGUI.indentLevel -= 1;

            GUILayout.Space(5);
        }

        void ShowRealtimeLMGUI(Terrain terrain)
        {
            Hash128 inputSystemHash;
            if (terrain == null || !Lightmapping.GetInputSystemHash(terrain.GetInstanceID(), out inputSystemHash) || inputSystemHash == new Hash128())
                return; // early return since we don't have any lightmaps for it

            if (!UpdateRealtimeTexture(inputSystemHash, terrain.GetInstanceID()))
                return;

            showRealtimeLightmap.value = EditorGUILayout.Foldout(showRealtimeLightmap.value, Styles.realtimeLM, true);

            if (!showRealtimeLightmap.value)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedRealtimeTexture.texture, true, terrain.GetInstanceID());

            GUILayout.BeginVertical();

            // Resolution of the system.
            int width, height;
            int numChunksInX, numChunksInY;
            if (Lightmapping.GetTerrainSystemResolution(terrain, out width, out height, out numChunksInX, out numChunksInY))
            {
                var str = width + "x" + height;
                if (numChunksInX > 1 || numChunksInY > 1)
                    str += string.Format(" ({0}x{1} chunks)", numChunksInX, numChunksInY);
                GUILayout.Label(Styles.realtimeLMResolution.text + ": " + str);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel -= 1;

            GUILayout.Space(5);
        }

        void ShowRealtimeLMGUI(Renderer renderer)
        {
            Hash128 inputSystemHash;
            if (renderer == null || !Lightmapping.GetInputSystemHash(renderer.GetInstanceID(), out inputSystemHash) || inputSystemHash == new Hash128())
                return; // early return since we don't have any lightmaps for it

            if (!UpdateRealtimeTexture(inputSystemHash, renderer.GetInstanceID()))
                return;

            showRealtimeLightmap.value = EditorGUILayout.Foldout(showRealtimeLightmap.value, Styles.realtimeLM, true);

            if (!showRealtimeLightmap.value)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedRealtimeTexture.texture, true, renderer.GetInstanceID());

            GUILayout.BeginVertical();

            int instWidth, instHeight;
            if (Lightmapping.GetInstanceResolution(renderer, out instWidth, out instHeight))
            {
                GUILayout.Label(Styles.realtimeLMInstanceResolution.text + ": " + instWidth + "x" + instHeight);
            }

            int width, height;
            if (Lightmapping.GetSystemResolution(renderer, out width, out height))
            {
                GUILayout.Label(Styles.realtimeLMResolution.text + ": " + width + "x" + height);
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Unsupported.IsDeveloperMode())
            {
                Hash128 instanceHash;
                if (Lightmapping.GetInstanceHash(renderer, out instanceHash))
                {
                    EditorGUILayout.LabelField(Styles.realtimeLMInstanceHash, GUIContent.Temp(instanceHash.ToString()));
                }

                Hash128 geometryHash;
                if (Lightmapping.GetGeometryHash(renderer, out geometryHash))
                {
                    EditorGUILayout.LabelField(Styles.realtimeLMGeometryHash, GUIContent.Temp(geometryHash.ToString()));
                }

                EditorGUILayout.LabelField(Styles.realtimeLMInputSystemHash, GUIContent.Temp(inputSystemHash.ToString()));
            }

            EditorGUI.indentLevel -= 1;

            GUILayout.Space(5);
        }

        bool UpdateRealtimeTexture(Hash128 inputSystemHash, int instanceId)
        {
            if (inputSystemHash == new Hash128())
                return false;

            Hash128 contentHash = LightmapVisualizationUtility.GetRealtimeGITextureHash(inputSystemHash, GITextureType.Irradiance);

            // if we need to fetch a new texture
            if (m_CachedRealtimeTexture.texture == null || m_CachedRealtimeTexture.contentHash != contentHash)
                m_CachedRealtimeTexture = LightmapVisualizationUtility.GetRealtimeGITexture(inputSystemHash, GITextureType.Irradiance);

            if (m_CachedRealtimeTexture.texture == null)
                return false;

            return true;
        }

        private void DrawLightmapPreview(Texture2D texture, bool realtimeLightmap, int instanceId)
        {
            GUILayout.Space(Styles.previewPadding);

            int previewWidth = Styles.previewWidth - 4; // padding

            Rect rect = GUILayoutUtility.GetRect(previewWidth, previewWidth, EditorStyles.objectField);
            Rect buttonRect = new Rect(rect.xMax - 70, rect.yMax - 14, 70, 14);

            if (Event.current.type == EventType.MouseDown)
            {
                if ((buttonRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 1) ||
                    (rect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2))
                {
                    LightmapPreviewWindow.CreateLightmapPreviewWindow(instanceId, realtimeLightmap, false);
                }
                else if (rect.Contains(Event.current.mousePosition) && Event.current.clickCount == 1)
                {
                    Object actualTargetObject = texture;
                    Component com = actualTargetObject as Component;

                    if (com)
                        actualTargetObject = com.gameObject;

                    EditorGUI.PingObjectOrShowPreviewOnClick(actualTargetObject, rect);
                }
            }

            EditorGUI.Toggle(rect, false, EditorStyles.objectFieldThumb);

            if (Event.current.type == EventType.Repaint)
            {
                rect = EditorStyles.objectFieldThumb.padding.Remove(rect);
                EditorGUI.DrawPreviewTexture(rect, texture);

                Styles.openPreviewStyle.Draw(rect, Styles.openPreview, false, false, false, false);
            }

            float spacing = Mathf.Max(5.0f, EditorGUIUtility.labelWidth - Styles.previewPadding - Styles.previewWidth);
            GUILayout.Space(spacing);
        }

        private void DisplayMeshWarning()
        {
            if (isPreset)
                return;

            Mesh mesh = GetSharedMesh(m_Renderers[0]);

            var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
            bool showEnlightenSettings = isPrefabAsset || settings.realtimeGI || (settings.bakedGI && settings.lightmapper == LightingSettings.Lightmapper.Enlighten);

            if (!HasSupportedTopologyForGI(mesh))
            {
                EditorGUILayout.HelpBox(Styles.unsupportedTopology.text, MessageType.Warning);
                return;
            }

            if (!HasVertices(mesh))
            {
                EditorGUILayout.HelpBox(Styles.noVerticesNoLightmapping.text, MessageType.Warning);
                return;
            }

            if (!HasNormals(mesh))
            {
                EditorGUILayout.HelpBox(Styles.noNormalsNoLightmapping.text, MessageType.Warning);
                return;
            }

            if (showEnlightenSettings)
            {
                if (Lightmapping.HasZeroAreaMesh(m_Renderers[0]))
                {
                    EditorGUILayout.HelpBox(Styles.zeroAreaPackingMesh.text, MessageType.Warning);
                }
            }
        }

        static Mesh GetSharedMesh(Renderer renderer)
        {
            Mesh mesh = null;

            if (renderer is MeshRenderer)
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf != null)
                    mesh = mf.sharedMesh;
            }

            return mesh;
        }

        static bool HasNormals(Mesh mesh)
        {
            return mesh != null && InternalMeshUtil.HasNormals(mesh);
        }

        static bool HasVertices(Mesh mesh)
        {
            return mesh != null && InternalMeshUtil.HasVertices(mesh);
        }

        static bool HasSupportedTopologyForGI(Mesh mesh)
        {
            return mesh != null && InternalMeshUtil.HasSupportedTopologyForGI(mesh);
        }

        static private bool isBuiltIn(SerializedProperty prop)
        {
            if (prop.objectReferenceValue != null)
            {
                var parameters = prop.objectReferenceValue as LightmapParameters;
                return (parameters.hideFlags == HideFlags.NotEditable);
            }

            return false;
        }

        static public void LightmapParametersGUI(SerializedProperty prop, GUIContent content)
        {
            EditorGUILayout.BeginHorizontal();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, content, prop);

            rect = EditorGUI.PrefixLabel(rect, content);

            GUIContent buttonContent = prop.hasMultipleDifferentValues ? EditorGUI.mixedValueContent : (prop.objectReferenceValue != null ? GUIContent.Temp(prop.objectReferenceStringValue) : Styles.lightmapParametersDefault);

            if (EditorGUI.DropdownButton(rect, buttonContent, FocusType.Passive, EditorStyles.popup))
                AssetPopupBackend.ShowAssetsPopupMenu<LightmapParameters>(rect, prop.objectReferenceTypeString, prop, "giparams", Styles.lightmapParametersDefault.text);

            string label = isBuiltIn(prop) ? "View" : "Edit...";

            // If object is null, then get the scene parameter setting and view this instead.
            using (new EditorGUI.DisabledScope(prop.hasMultipleDifferentValues))
            {
                if (prop.objectReferenceValue == null)
                {
                    SerializedObject so = new SerializedObject(Lightmapping.GetLightingSettingsOrDefaultsFallback());
                    SerializedProperty lightmapParameters = so.FindProperty("m_LightmapParameters");

                    if (lightmapParameters.objectReferenceValue != null)
                    {
                        label = isBuiltIn(lightmapParameters) ? "View" : "Edit...";

                        if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            Selection.activeObject = lightmapParameters.objectReferenceValue;
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Selection.activeObject = prop.objectReferenceValue;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }

        void ShowTerrainChunks(Terrain[] terrains)
        {
            if (terrains == null)
                return;

            int terrainChunksX = 0, terrainChunksY = 0;
            foreach (var terrain in terrains)
            {
                int tmpChunksX = 0, tmpChunksY = 0;
                Lightmapping.GetTerrainGIChunks(terrain, ref tmpChunksX, ref tmpChunksY);
                if (terrainChunksX == 0 && terrainChunksY == 0)
                {
                    terrainChunksX = tmpChunksX;
                    terrainChunksY = tmpChunksY;
                }
                else if (terrainChunksX != tmpChunksX || terrainChunksY != tmpChunksY)
                {
                    terrainChunksX = terrainChunksY = 0;
                    break;
                }
            }
            if (terrainChunksX * terrainChunksY > 1)
                EditorGUILayout.HelpBox(string.Format(L10n.Tr("Terrain is chunked up into {0} instances for baking."), terrainChunksX * terrainChunksY), MessageType.None);
        }

        void ShowBakePerformanceWarning(Terrain terrain)
        {
            var terrainWidth = terrain.terrainData.size.x;
            var terrainHeight = terrain.terrainData.size.z;
            var lightmapParameters = (LightmapParameters)m_LightmapParameters.objectReferenceValue ?? new LightmapParameters();
            var indirectResolution = Lightmapping.GetLightingSettingsOrDefaultsFallback().indirectResolution;

            var terrainSystemTexelsInWidth = terrainWidth * lightmapParameters.resolution * indirectResolution;
            var terrainSystemTexelsInHeight = terrainHeight * lightmapParameters.resolution * indirectResolution;
            const int kTerrainTexelsThreshold = 64 * 8;
            if (terrainSystemTexelsInWidth > kTerrainTexelsThreshold || terrainSystemTexelsInHeight > kTerrainTexelsThreshold)
            {
                EditorGUILayout.HelpBox(Styles.resolutionTooHighWarning.text, MessageType.Warning);
            }

            var terrainClustersInWidth = terrainSystemTexelsInWidth * lightmapParameters.clusterResolution;
            var terrainClustersInHeight = terrainSystemTexelsInHeight * lightmapParameters.clusterResolution;
            var terrainTrisPerClusterInWidth = terrain.terrainData.heightmapResolution / terrainClustersInWidth;
            var terrainTrisPerClusterInHeight = terrain.terrainData.heightmapResolution / terrainClustersInHeight;
            const float kTerrainClusterTriDensityThreshold = 256.0f / 5.0f;
            if (terrainTrisPerClusterInWidth > kTerrainClusterTriDensityThreshold || terrainTrisPerClusterInHeight > kTerrainClusterTriDensityThreshold)
            {
                EditorGUILayout.HelpBox(Styles.resolutionTooLowWarning.text, MessageType.Warning);
            }
        }
    }
}
