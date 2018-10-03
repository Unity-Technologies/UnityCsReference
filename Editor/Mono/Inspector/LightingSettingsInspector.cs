// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine.Experimental.Rendering;
using UnityEngineInternal;

namespace UnityEditor
{
    internal class LightingSettingsInspector
    {
        static class Styles
        {
            public static readonly GUIContent OptimizeRealtimeUVs = EditorGUIUtility.TrTextContent("Optimize", "Specifies whether the authored mesh UVs get optimized for Realtime Global Illumination or not. When enabled, the authored UVs can get merged, scaled, and packed for optimisation purposes. When disabled, the authored UVs will get scaled and packed, but not merged.");
            public static readonly GUIContent IgnoreNormalsForChartDetection = EditorGUIUtility.TrTextContent("Ignore Normals", "When enabled, prevents the UV charts from being split during the precompute process for Realtime Global Illumination lighting.");
            public static readonly int[] MinimumChartSizeValues = { 2, 4 };
            public static readonly GUIContent[] MinimumChartSizeStrings =
            {
                EditorGUIUtility.TrTextContent("2 (Minimum)"),
                EditorGUIUtility.TrTextContent("4 (Stitchable)"),
            };
            public static readonly GUIContent Lighting = new GUIContent(EditorGUIUtility.TrTextContent("Lighting").text); // prevent the Lighting window icon from being added
            public static readonly GUIContent MinimumChartSize = EditorGUIUtility.TrTextContent("Min Chart Size", "Specifies the minimum texel size used for a UV chart. If stitching is required, a value of 4 will create a chart of 4x4 texels to store lighting and directionality. If stitching is not required, a value of 2 will reduce the texel density and provide better lighting build times and run time performance.");
            public static readonly GUIContent ImportantGI = EditorGUIUtility.TrTextContent("Prioritize Illumination", "When enabled, the object will be marked as a priority object and always included in lighting calculations. Useful for objects that will be strongly emissive to make sure that other objects will be illuminated by this object.");
            public static readonly GUIContent StitchLightmapSeams = EditorGUIUtility.TrTextContent("Stitch Seams", "When enabled, seams in baked lightmaps will get smoothed.");
            public static readonly GUIContent AutoUVMaxDistance = EditorGUIUtility.TrTextContent("Max Distance", "Specifies the maximum worldspace distance to be used for UV chart simplification. If charts are within this distance they will be simplified for optimization purposes.");
            public static readonly GUIContent AutoUVMaxAngle = EditorGUIUtility.TrTextContent("Max Angle", "Specifies the maximum angle in degrees between faces sharing a UV edge. If the angle between the faces is below this value, the UV charts will be simplified.");
            public static readonly GUIContent LightmapParameters = EditorGUIUtility.TrTextContent("Lightmap Parameters", "Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public static readonly GUIContent AtlasTilingX = EditorGUIUtility.TrTextContent("Tiling X");
            public static readonly GUIContent AtlasTilingY = EditorGUIUtility.TrTextContent("Tiling Y");
            public static readonly GUIContent AtlasOffsetX = EditorGUIUtility.TrTextContent("Offset X");
            public static readonly GUIContent AtlasOffsetY = EditorGUIUtility.TrTextContent("Offset Y");
            public static readonly GUIContent ClampedSize = EditorGUIUtility.TrTextContent("Object's size in lightmap has reached the max atlas size.", "If you need higher resolution for this object, divide it into smaller meshes or set higher max atlas size via the LightmapEditorSettings class.");
            public static readonly GUIContent ClampedPackingResolution = EditorGUIUtility.TrTextContent("Object's size in the realtime lightmap has reached the maximum size. If you need higher resolution for this object, divide it into smaller meshes.");
            public static readonly GUIContent ZeroAreaPackingMesh = EditorGUIUtility.TrTextContent("Mesh used by the renderer has zero UV or surface area. Non zero area is required for lightmapping.");
            public static readonly GUIContent NoNormalsNoLightmapping = EditorGUIUtility.TrTextContent("Mesh used by the renderer doesn't have normals. Normals are needed for lightmapping.");
            public static readonly GUIContent UVOverlap = EditorGUIUtility.TrTextContent("This GameObject has overlapping UVs. Please enable 'Generate Lightmap UVs' on the Asset or fix in your modelling package.");
            public static readonly GUIContent Atlas = EditorGUIUtility.TrTextContent("Baked Lightmap");
            public static readonly GUIContent RealtimeLM = EditorGUIUtility.TrTextContent("Realtime Lightmap");
            public static readonly GUIContent ScaleInLightmap = EditorGUIUtility.TrTextContent("Scale In Lightmap", "Specifies the relative size of object's UVs within a lightmap. A value of 0 will result in the object not being lightmapped, but still contribute lighting to other objects in the Scene.");
            public static readonly GUIContent AtlasIndex = EditorGUIUtility.TrTextContent("Lightmap Index");
            public static readonly GUIContent PVRInstanceHash = EditorGUIUtility.TrTextContent("Instance Hash", "The hash of the baked GI instance.");
            public static readonly GUIContent PVRAtlasHash = EditorGUIUtility.TrTextContent("Atlas Hash", "The hash of the atlas this baked GI instance is a part of.");
            public static readonly GUIContent PVRAtlasInstanceOffset = EditorGUIUtility.TrTextContent("Atlas Instance Offset", "The offset into the transform array instances of this atlas start at.");
            public static readonly GUIContent RealtimeLMResolution = EditorGUIUtility.TrTextContent("System Resolution", "The resolution in texels of the realtime lightmap that this renderer belongs to.");
            public static readonly GUIContent RealtimeLMInstanceResolution = EditorGUIUtility.TrTextContent("Instance Resolution", "The resolution in texels of the realtime lightmap packed instance.");
            public static readonly GUIContent RealtimeLMInputSystemHash = EditorGUIUtility.TrTextContent("System Hash", "The hash of the realtime system that the renderer belongs to.");
            public static readonly GUIContent RealtimeLMInstanceHash = EditorGUIUtility.TrTextContent("Instance Hash", "The hash of the realtime GI instance.");
            public static readonly GUIContent RealtimeLMGeometryHash = EditorGUIUtility.TrTextContent("Geometry Hash", "The hash of the realtime GI geometry that the renderer is using.");
            public static readonly GUIContent UVCharting = EditorGUIUtility.TrTextContent("Realtime UVs");
            public static readonly GUIContent LightmapSettings = EditorGUIUtility.TrTextContent("Lightmaps");
            public static readonly GUIContent LightmapStatic = EditorGUIUtility.TrTextContent("Lightmap Static", "Controls whether the geometry will be marked as Static for lightmapping purposes. When enabled, this mesh will be present in lightmap calculations.");
            public static readonly GUIContent CastShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "Specifies whether a geometry creates shadows or not when a shadow-casting Light shines on it.");
            public static readonly GUIContent ReceiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "When enabled, any shadows cast from other objects are drawn on the geometry.");
            public static readonly GUIContent MotionVectors = EditorGUIUtility.TrTextContent("Motion Vectors", "Specifies whether the Mesh renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture.");
            public static readonly GUIContent LightmapInfoBox = EditorGUIUtility.TrTextContent("To enable generation of lightmaps for this Mesh Renderer, please enable the 'Lightmap Static' property.");
            public static readonly GUIContent TerrainLightmapInfoBox = EditorGUIUtility.TrTextContent("To enable generation of lightmaps for this Mesh Renderer, please enable the 'Lightmap Static' property.");
            public static readonly GUIContent ResolutionTooHighWarning = EditorGUIUtility.TrTextContent("Precompute/indirect resolution for this terrain is probably too high. Use a lower realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a lower resolution setting. Otherwise it may take a very long time to bake and memory consumption during and after the bake may be very high.");
            public static readonly GUIContent ResolutionTooLowWarning = EditorGUIUtility.TrTextContent("Precompute/indirect resolution for this terrain is probably too low. If the Clustering stage takes a long time, try using a higher realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a higher resolution setting.");
            public static readonly GUIContent GINotEnabledInfo = EditorGUIUtility.TrTextContent("Lightmapping settings are currently disabled. Enable Baked Global Illumination or Realtime Global Illumination to display these settings.");
            public static readonly GUIContent CastShadowsProgressiveGPUWarning = EditorGUIUtility.TrTextContent("Cast Shadows is forced to 'On' when using the GPU lightmapper (Preview), it will be supported in a later version. Use the CPU lightmapper instead if you need this functionality.");
            public static readonly GUIContent ReceiveShadowsProgressiveGPUWarning = EditorGUIUtility.TrTextContent("Receive Shadows is forced to 'On' when using the GPU lightmapper (Preview), it will be supported in a later version. Use the CPU lightmapper instead if you need this functionality.");
            public static readonly GUIContent OpenPreview = EditorGUIUtility.TrTextContent("Open Preview");

            public static readonly GUIStyle OpenPreviewStyle = EditorStyles.objectFieldThumb.name + "LightmapPreviewOverlay";

            public static readonly int PreviewPadding = 30;
            public static readonly int PreviewWidth = 104;
        }

        bool m_ShowChartingSettings = true;
        bool m_ShowLightmapSettings = true;
        bool m_ShowBakedLM = true;
        bool m_ShowRealtimeLM = true;

        SerializedObject m_SerializedObject;
        SerializedObject m_GameObjectsSerializedObject;
        SerializedObject m_LightmapSettings;

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
        SerializedProperty m_CastShadows;
        SerializedProperty m_ReceiveShadows;
        SerializedProperty m_MotionVectors;

        SerializedProperty m_EnabledBakedGI;
        SerializedProperty m_EnabledRealtimeGI;

        Renderer[] m_Renderers;
        Terrain[] m_Terrains;

        public bool showChartingSettings { get { return m_ShowChartingSettings; } set { m_ShowChartingSettings = value; } }
        public bool showLightmapSettings { get { return m_ShowLightmapSettings; } set { m_ShowLightmapSettings = value; } }

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

        public LightingSettingsInspector(SerializedObject serializedObject)
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
            m_MotionVectors = m_SerializedObject.FindProperty("m_MotionVectors");

            m_Renderers = m_SerializedObject.targetObjects.OfType<Renderer>().ToArray();
            m_Terrains = m_SerializedObject.targetObjects.OfType<Terrain>().ToArray();

            m_StaticEditorFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");

            m_LightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
            m_EnabledBakedGI = m_LightmapSettings.FindProperty("m_GISettings.m_EnableBakedLightmaps");
            m_EnabledRealtimeGI = m_LightmapSettings.FindProperty("m_GISettings.m_EnableRealtimeLightmaps");
        }

        public void RenderMeshSettings(bool showLightmapSettings)
        {
            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjectsCount == 0)
                return;

            m_GameObjectsSerializedObject.Update();
            m_LightmapSettings.Update();

            // TODO(RadeonRays): remove scope for GPU lightmapper once the feature has been implemented.
            using (new EditorGUI.DisabledScope(LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveGPU))
            {
                EditorGUILayout.PropertyField(m_CastShadows, Styles.CastShadows, true);
            }
            if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveGPU)
                EditorGUILayout.HelpBox(Styles.CastShadowsProgressiveGPUWarning.text, MessageType.Info);

            bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();

            if (SupportedRenderingFeatures.active.rendererSupportsReceiveShadows)
            {
                // TODO(RadeonRays): remove scope for GPU lightmapper once the feature has been implemented.
                using (new EditorGUI.DisabledScope(isDeferredRenderingPath || LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveGPU))
                {
                    EditorGUILayout.PropertyField(m_ReceiveShadows, Styles.ReceiveShadows, true);
                }
                if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveGPU)
                    EditorGUILayout.HelpBox(Styles.ReceiveShadowsProgressiveGPUWarning.text, MessageType.Info);
            }

            if (SupportedRenderingFeatures.active.rendererSupportsMotionVectors)
                EditorGUILayout.PropertyField(m_MotionVectors, Styles.MotionVectors, true);

            if (!showLightmapSettings)
                return;

            GUILayout.Space(10);

            LightmapStaticSettings();

            if (!(m_EnabledBakedGI.boolValue || m_EnabledRealtimeGI.boolValue) && !isPrefabAsset)
            {
                EditorGUILayout.HelpBox(Styles.GINotEnabledInfo.text, MessageType.Info);
                return;
            }

            bool enableSettings = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            // We want to show the lightmap settings if the lightmap static flag is set.
            // Most of the settings apply to both, realtime and baked GI.
            if (enableSettings)
            {
                bool showEnlightenSettings = isPrefabAsset || m_EnabledRealtimeGI.boolValue || (m_EnabledBakedGI.boolValue && LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.Enlighten);
                bool showProgressiveSettings = isPrefabAsset || (m_EnabledBakedGI.boolValue && LightmapEditorSettings.lightmapper != LightmapEditorSettings.Lightmapper.Enlighten);

                float lodScale = LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[0]);
                for (int i = 1; i < m_Renderers.Length; i++)
                {
                    if (!Mathf.Approximately(lodScale, LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[i])))
                        lodScale = 1.0F;
                }

                float lightmapScale = LightmapScaleGUI(lodScale) * LightmapVisualization.GetLightmapLODLevelScale(m_Renderers[0]);

                // tell the user if the object's size in lightmap has reached the max atlas size
                float cachedSurfaceArea = InternalMeshUtil.GetCachedMeshSurfaceArea((MeshRenderer)m_Renderers[0]);

                ShowClampedSizeInLightmapGUI(lightmapScale, cachedSurfaceArea);

                // Enlighten specific
                if (showEnlightenSettings)
                    EditorGUILayout.PropertyField(m_ImportantGI, Styles.ImportantGI);

                if (showProgressiveSettings)
                    EditorGUILayout.PropertyField(m_StitchLightmapSeams, Styles.StitchLightmapSeams);

                LightmapParametersGUI(m_LightmapParameters, Styles.LightmapParameters);

                if (showEnlightenSettings)
                {
                    m_ShowChartingSettings = EditorGUILayout.Foldout(m_ShowChartingSettings, Styles.UVCharting, true);
                    if (m_ShowChartingSettings)
                        RendererUVSettings();
                }

                m_ShowLightmapSettings = EditorGUILayout.Foldout(m_ShowLightmapSettings, Styles.LightmapSettings, true);

                if (m_ShowLightmapSettings)
                {
                    EditorGUI.indentLevel += 1;

                    ShowAtlasGUI(m_Renderers[0].GetInstanceID());
                    ShowRealtimeLMGUI(m_Renderers[0]);

                    EditorGUI.indentLevel -= 1;
                }

                if (LightmapEditorSettings.HasZeroAreaMesh(m_Renderers[0]))
                    EditorGUILayout.HelpBox(Styles.ZeroAreaPackingMesh.text, MessageType.Warning);

                if (LightmapEditorSettings.HasClampedResolution(m_Renderers[0]))
                    EditorGUILayout.HelpBox(Styles.ClampedPackingResolution.text, MessageType.Warning);

                if (!HasNormals(m_Renderers[0]))
                    EditorGUILayout.HelpBox(Styles.NoNormalsNoLightmapping.text, MessageType.Warning);

                if (LightmapEditorSettings.HasUVOverlaps(m_Renderers[0]))
                    EditorGUILayout.HelpBox(Styles.UVOverlap.text, MessageType.Warning);

                m_SerializedObject.ApplyModifiedProperties();
            }
            else
                EditorGUILayout.HelpBox(Styles.LightmapInfoBox.text, MessageType.Info);
        }

        public void RenderTerrainSettings()
        {
            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjectsCount == 0)
                return;

            m_GameObjectsSerializedObject.Update();
            m_LightmapSettings.Update();

            LightmapStaticSettings();

            if (!(m_EnabledBakedGI.boolValue || m_EnabledRealtimeGI.boolValue) && !isPrefabAsset)
            {
                EditorGUILayout.HelpBox(Styles.GINotEnabledInfo.text, MessageType.Info);
                return;
            }

            bool enableSettings = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            if (enableSettings)
            {
                m_ShowLightmapSettings = EditorGUILayout.Foldout(m_ShowLightmapSettings, Styles.LightmapSettings, true);

                if (m_ShowLightmapSettings)
                {
                    EditorGUI.indentLevel += 1;

                    using (new EditorGUI.DisabledScope(!enableSettings))
                    {
                        if (GUI.enabled)
                            ShowTerrainChunks(m_Terrains);

                        float lightmapScale = LightmapScaleGUI(1.0f);

                        // tell the user if the object's size in lightmap has reached the max atlas size
                        var terrainData = m_Terrains[0].terrainData;
                        float cachedSurfaceArea = terrainData != null ? terrainData.size.x * terrainData.size.z : 0;
                        ShowClampedSizeInLightmapGUI(lightmapScale, cachedSurfaceArea);

                        LightmapParametersGUI(m_LightmapParameters, Styles.LightmapParameters);

                        if (GUI.enabled && m_Terrains.Length == 1 && m_Terrains[0].terrainData != null)
                            ShowBakePerformanceWarning(m_Terrains[0]);

                        ShowAtlasGUI(m_Terrains[0].GetInstanceID());
                        ShowRealtimeLMGUI(m_Terrains[0]);

                        m_SerializedObject.ApplyModifiedProperties();
                    }
                    EditorGUI.indentLevel -= 1;
                }
                GUILayout.Space(10);
            }
            else
                EditorGUILayout.HelpBox(Styles.TerrainLightmapInfoBox.text, MessageType.Info);
        }

        void LightmapStaticSettings()
        {
            bool lightmapStatic = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            EditorGUI.BeginChangeCheck();
            lightmapStatic = EditorGUILayout.Toggle(Styles.LightmapStatic, lightmapStatic);

            if (EditorGUI.EndChangeCheck())
            {
                SceneModeUtility.SetStaticFlags(m_GameObjectsSerializedObject.targetObjects, (int)StaticEditorFlags.LightmapStatic, lightmapStatic);
                m_GameObjectsSerializedObject.Update();
            }
        }

        void RendererUVSettings()
        {
            EditorGUI.indentLevel++;

            // TODO: This is very temporary and the flag needs to be changed. Will fix / Jen
            bool optimizeRealtimeUVs = !m_PreserveUVs.boolValue;

            EditorGUI.BeginChangeCheck();
            optimizeRealtimeUVs = EditorGUILayout.Toggle(Styles.OptimizeRealtimeUVs, optimizeRealtimeUVs);

            if (EditorGUI.EndChangeCheck())
            {
                m_PreserveUVs.boolValue = !optimizeRealtimeUVs;
            }

            EditorGUI.indentLevel++;
            bool disabledAutoUVs = m_PreserveUVs.boolValue;
            using (new EditorGUI.DisabledScope(disabledAutoUVs))
            {
                EditorGUILayout.PropertyField(m_AutoUVMaxDistance, Styles.AutoUVMaxDistance);
                if (m_AutoUVMaxDistance.floatValue < 0.0f)
                    m_AutoUVMaxDistance.floatValue = 0.0f;
                EditorGUILayout.Slider(m_AutoUVMaxAngle, 0, 180, Styles.AutoUVMaxAngle);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(m_IgnoreNormalsForChartDetection, Styles.IgnoreNormalsForChartDetection);

            EditorGUILayout.IntPopup(m_MinimumChartSize, Styles.MinimumChartSizeStrings, Styles.MinimumChartSizeValues, Styles.MinimumChartSize);
            EditorGUI.indentLevel--;
        }

        void ShowClampedSizeInLightmapGUI(float lightmapScale, float cachedSurfaceArea)
        {
            float sizeInLightmap = Mathf.Sqrt(cachedSurfaceArea) * LightmapEditorSettings.bakeResolution * lightmapScale;

            if (sizeInLightmap > LightmapEditorSettings.maxAtlasSize)
                EditorGUILayout.HelpBox(Styles.ClampedSize.text, MessageType.Info);
        }

        float LightmapScaleGUI(float lodScale)
        {
            float lightmapScale = lodScale * m_LightmapScale.floatValue;

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.ScaleInLightmap, m_LightmapScale);
            EditorGUI.BeginChangeCheck();
            lightmapScale = EditorGUI.FloatField(rect, Styles.ScaleInLightmap, lightmapScale);
            if (EditorGUI.EndChangeCheck())
                m_LightmapScale.floatValue = Mathf.Max(lightmapScale / Mathf.Max(lodScale, float.Epsilon), 0.0f);
            EditorGUI.EndProperty();

            return lightmapScale;
        }

        void ShowAtlasGUI(int instanceID)
        {
            if (m_LightmapIndex == null)
                return;

            Hash128 contentHash = LightmapVisualizationUtility.GetBakedGITextureHash(m_LightmapIndex.intValue, 0, GITextureType.Baked);

            // if we need to fetch a new texture
            if (m_CachedBakedTexture.texture == null || m_CachedBakedTexture.contentHash != contentHash)
                m_CachedBakedTexture = LightmapVisualizationUtility.GetBakedGITexture(m_LightmapIndex.intValue, 0, GITextureType.Baked);

            if (m_CachedBakedTexture.texture == null)
                return;

            m_ShowBakedLM = EditorGUILayout.Foldout(m_ShowBakedLM, Styles.Atlas, true);

            if (!m_ShowBakedLM)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedBakedTexture.texture, false, instanceID);

            GUILayout.BeginVertical();

            GUILayout.Label(Styles.AtlasIndex.text + ": " + m_LightmapIndex.intValue.ToString());
            GUILayout.Label(Styles.AtlasTilingX.text + ": " + m_LightmapTilingOffsetX.floatValue.ToString());
            GUILayout.Label(Styles.AtlasTilingY.text + ": " + m_LightmapTilingOffsetY.floatValue.ToString());
            GUILayout.Label(Styles.AtlasOffsetX.text + ": " + m_LightmapTilingOffsetZ.floatValue.ToString());
            GUILayout.Label(Styles.AtlasOffsetY.text + ": " + m_LightmapTilingOffsetW.floatValue.ToString());

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            bool showProgressiveInfo = isPrefabAsset || (m_EnabledBakedGI.boolValue && LightmapEditorSettings.lightmapper != LightmapEditorSettings.Lightmapper.Enlighten);

            if (showProgressiveInfo && Unsupported.IsDeveloperMode())
            {
                Hash128 instanceHash;
                LightmapEditorSettings.GetPVRInstanceHash(instanceID, out instanceHash);
                EditorGUILayout.LabelField(Styles.PVRInstanceHash, GUIContent.Temp(instanceHash.ToString()));

                Hash128 atlasHash;
                LightmapEditorSettings.GetPVRAtlasHash(instanceID, out atlasHash);
                EditorGUILayout.LabelField(Styles.PVRAtlasHash, GUIContent.Temp(atlasHash.ToString()));

                int atlasInstanceOffset;
                LightmapEditorSettings.GetPVRAtlasInstanceOffset(instanceID, out atlasInstanceOffset);
                EditorGUILayout.LabelField(Styles.PVRAtlasInstanceOffset, GUIContent.Temp(atlasInstanceOffset.ToString()));
            }
            EditorGUI.indentLevel -= 1;

            GUILayout.Space(5);
        }

        void ShowRealtimeLMGUI(Terrain terrain)
        {
            Hash128 inputSystemHash;
            if (terrain == null || !LightmapEditorSettings.GetInputSystemHash(terrain.GetInstanceID(), out inputSystemHash) || inputSystemHash == new Hash128())
                return; // early return since we don't have any lightmaps for it

            if (!UpdateRealtimeTexture(inputSystemHash, terrain.GetInstanceID()))
                return;

            m_ShowRealtimeLM = EditorGUILayout.Foldout(m_ShowRealtimeLM, Styles.RealtimeLM, true);

            if (!m_ShowRealtimeLM)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedRealtimeTexture.texture, true, terrain.GetInstanceID());

            GUILayout.BeginVertical();

            // Resolution of the system.
            int width, height;
            int numChunksInX, numChunksInY;
            if (LightmapEditorSettings.GetTerrainSystemResolution(terrain, out width, out height, out numChunksInX, out numChunksInY))
            {
                var str = width.ToString() + "x" + height.ToString();
                if (numChunksInX > 1 || numChunksInY > 1)
                    str += string.Format(" ({0}x{1} chunks)", numChunksInX, numChunksInY);
                GUILayout.Label(Styles.RealtimeLMResolution.text + ": " + str);
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
            if (renderer == null || !LightmapEditorSettings.GetInputSystemHash(renderer.GetInstanceID(), out inputSystemHash) || inputSystemHash == new Hash128())
                return; // early return since we don't have any lightmaps for it

            if (!UpdateRealtimeTexture(inputSystemHash, renderer.GetInstanceID()))
                return;

            m_ShowRealtimeLM = EditorGUILayout.Foldout(m_ShowRealtimeLM, Styles.RealtimeLM, true);

            if (!m_ShowRealtimeLM)
                return;

            EditorGUI.indentLevel += 1;

            GUILayout.BeginHorizontal();

            DrawLightmapPreview(m_CachedRealtimeTexture.texture, true, renderer.GetInstanceID());

            GUILayout.BeginVertical();

            int instWidth, instHeight;
            if (LightmapEditorSettings.GetInstanceResolution(renderer, out instWidth, out instHeight))
            {
                GUILayout.Label(Styles.RealtimeLMInstanceResolution.text + ": " + instWidth.ToString() + "x" + instHeight.ToString());
            }

            int width, height;
            if (LightmapEditorSettings.GetSystemResolution(renderer, out width, out height))
            {
                GUILayout.Label(Styles.RealtimeLMResolution.text + ": " + width.ToString() + "x" + height.ToString());
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Unsupported.IsDeveloperMode())
            {
                Hash128 instanceHash;
                if (LightmapEditorSettings.GetInstanceHash(renderer, out instanceHash))
                {
                    EditorGUILayout.LabelField(Styles.RealtimeLMInstanceHash, GUIContent.Temp(instanceHash.ToString()));
                }

                Hash128 geometryHash;
                if (LightmapEditorSettings.GetGeometryHash(renderer, out geometryHash))
                {
                    EditorGUILayout.LabelField(Styles.RealtimeLMGeometryHash, GUIContent.Temp(geometryHash.ToString()));
                }

                EditorGUILayout.LabelField(Styles.RealtimeLMInputSystemHash, GUIContent.Temp(inputSystemHash.ToString()));
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
            GUILayout.Space(Styles.PreviewPadding);

            int previewWidth = Styles.PreviewWidth - 4; // padding

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

                Styles.OpenPreviewStyle.Draw(rect, Styles.OpenPreview, false, false, false, false);
            }

            float spacing = Mathf.Max(5.0f, EditorGUIUtility.labelWidth - Styles.PreviewPadding - Styles.PreviewWidth);
            GUILayout.Space(spacing);
        }

        static bool HasNormals(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is MeshRenderer)
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf != null)
                    mesh = mf.sharedMesh;
            }
            else if (renderer is SkinnedMeshRenderer)
            {
                mesh = (renderer as SkinnedMeshRenderer).sharedMesh;
            }
            return mesh != null && InternalMeshUtil.HasNormals(mesh);
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

        static public bool LightmapParametersGUI(SerializedProperty prop, GUIContent content)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUIInternal.AssetPopup<LightmapParameters>(prop, content, "giparams", "Scene Default Parameters");

            string label = "Edit...";

            if (isBuiltIn(prop))
                label = "View";

            bool editClicked = false;

            // If object is null, then get the scene parameter setting and view this instead.
            if (prop.objectReferenceValue == null)
            {
                SerializedObject so = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                SerializedProperty lightmapParameters = so.FindProperty("m_LightmapEditorSettings.m_LightmapParameters");

                using (new EditorGUI.DisabledScope(lightmapParameters == null))
                {
                    if (isBuiltIn(lightmapParameters))
                        label = "View";
                    else
                        label = "Edit...";

                    if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Selection.activeObject = lightmapParameters.objectReferenceValue;
                        editClicked = true;
                    }
                }
            }
            else
            {
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                {
                    Selection.activeObject = prop.objectReferenceValue;
                    editClicked = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            return editClicked;
        }

        void ShowTerrainChunks(Terrain[] terrains)
        {
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

            var terrainSystemTexelsInWidth = terrainWidth * lightmapParameters.resolution * LightmapEditorSettings.realtimeResolution;
            var terrainSystemTexelsInHeight = terrainHeight * lightmapParameters.resolution * LightmapEditorSettings.realtimeResolution;
            const int kTerrainTexelsThreshold = 64 * 8;
            if (terrainSystemTexelsInWidth > kTerrainTexelsThreshold || terrainSystemTexelsInHeight > kTerrainTexelsThreshold)
            {
                EditorGUILayout.HelpBox(Styles.ResolutionTooHighWarning.text, MessageType.Warning);
            }

            var terrainClustersInWidth = terrainSystemTexelsInWidth * lightmapParameters.clusterResolution;
            var terrainClustersInHeight = terrainSystemTexelsInHeight * lightmapParameters.clusterResolution;
            var terrainTrisPerClusterInWidth = terrain.terrainData.heightmapResolution / terrainClustersInWidth;
            var terrainTrisPerClusterInHeight = terrain.terrainData.heightmapResolution / terrainClustersInHeight;
            const float kTerrainClusterTriDensityThreshold = 256.0f / 5.0f;
            if (terrainTrisPerClusterInWidth > kTerrainClusterTriDensityThreshold || terrainTrisPerClusterInHeight > kTerrainClusterTriDensityThreshold)
            {
                EditorGUILayout.HelpBox(Styles.ResolutionTooLowWarning.text, MessageType.Warning);
            }
        }
    }
}
