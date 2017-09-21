// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;
using UnityEngineInternal;
using UnityEditor.AnimatedValues;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LightingSettingsInspector
    {
        class Styles
        {
            public GUIContent OptimizeRealtimeUVs = EditorGUIUtility.TextContent("Optimize Realtime UVs|Specifies whether the authored mesh UVs get optimized for Realtime Global Illumination or not. When enabled, the authored UVs can get merged, scaled, and packed for optimisation purposes. When disabled, the authored UVs will get scaled and packed, but not merged.");
            public GUIContent IgnoreNormalsForChartDetection = EditorGUIUtility.TextContent("Ignore Normals|When enabled, prevents the UV charts from being split during the precompute process for Realtime Global Illumination lighting.");
            public int[] MinimumChartSizeValues = { 2, 4 };
            public GUIContent[] MinimumChartSizeStrings =
            {
                EditorGUIUtility.TextContent("2 (Minimum)"),
                EditorGUIUtility.TextContent("4 (Stitchable)"),
            };
            public GUIContent Lighting = new GUIContent(EditorGUIUtility.TextContent("Lighting").text); // prevent the Lighting window icon from being added
            public GUIContent MinimumChartSize = EditorGUIUtility.TextContent("Min Chart Size|Specifies the minimum texel size used for a UV chart. If stitching is required, a value of 4 will create a chart of 4x4 texels to store lighting and directionality. If stitching is not required, a value of 2 will reduce the texel density and provide better lighting build times and run time performance.");
            public GUIContent ImportantGI = EditorGUIUtility.TextContent("Prioritize Illumination|When enabled, the object will be marked as a priority object and always included in lighting calculations. Useful for objects that will be strongly emissive to make sure that other objects will be illuminated by this object.");
            public GUIContent StitchLightmapSeams = EditorGUIUtility.TextContent("Stitch Seams|When enabled, seams in baked lightmaps will get smoothed.");
            public GUIContent AutoUVMaxDistance = EditorGUIUtility.TextContent("Max Distance|Specifies the maximum worldspace distance to be used for UV chart simplification. If charts are within this distance they will be simplified for optimization purposes.");
            public GUIContent AutoUVMaxAngle = EditorGUIUtility.TextContent("Max Angle|Specifies the maximum angle in degrees between faces sharing a UV edge. If the angle between the faces is below this value, the UV charts will be simplified.");
            public GUIContent LightmapParameters = EditorGUIUtility.TextContent("Lightmap Parameters|Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public GUIContent AtlasTilingX = EditorGUIUtility.TextContent("Tiling X");
            public GUIContent AtlasTilingY = EditorGUIUtility.TextContent("Tiling Y");
            public GUIContent AtlasOffsetX = EditorGUIUtility.TextContent("Offset X");
            public GUIContent AtlasOffsetY = EditorGUIUtility.TextContent("Offset Y");
            public GUIContent ClampedSize = EditorGUIUtility.TextContent("Object's size in lightmap has reached the max atlas size.|If you need higher resolution for this object, divide it into smaller meshes or set higher max atlas size via the LightmapEditorSettings class.");
            public GUIContent ClampedPackingResolution = EditorGUIUtility.TextContent("Object's size in the realtime lightmap has reached the maximum size. If you need higher resolution for this object, divide it into smaller meshes.");
            public GUIContent ZeroAreaPackingMesh = EditorGUIUtility.TextContent("Mesh used by the renderer has zero UV or surface area. Non zero area is required for lightmapping.");
            public GUIContent NoNormalsNoLightmapping = EditorGUIUtility.TextContent("Mesh used by the renderer doesn't have normals. Normals are needed for lightmapping.");
            public GUIContent Atlas = EditorGUIUtility.TextContent("Baked Lightmap");
            public GUIContent RealtimeLM = EditorGUIUtility.TextContent("Realtime Lightmap");
            public GUIContent ScaleInLightmap = EditorGUIUtility.TextContent("Scale In Lightmap|Specifies the relative size of object's UVs within a lightmap. A value of 0 will result in the object not being lightmapped, but still contribute lighting to other objects in the Scene.");
            public GUIContent AtlasIndex = EditorGUIUtility.TextContent("Lightmap Index");
            public GUIContent PVRInstanceHash = EditorGUIUtility.TextContent("Instance Hash|The hash of the baked GI instance.");
            public GUIContent PVRAtlasHash = EditorGUIUtility.TextContent("Atlas Hash|The hash of the atlas this baked GI instance is a part of.");
            public GUIContent PVRAtlasInstanceOffset = EditorGUIUtility.TextContent("Atlas Instance Offset|The offset into the transform array instances of this atlas start at.");
            public GUIContent RealtimeLMResolution = EditorGUIUtility.TextContent("System Resolution|The resolution in texels of the realtime lightmap that this renderer belongs to.");
            public GUIContent RealtimeLMInstanceResolution = EditorGUIUtility.TextContent("Instance Resolution|The resolution in texels of the realtime lightmap packed instance.");
            public GUIContent RealtimeLMInputSystemHash = EditorGUIUtility.TextContent("System Hash|The hash of the realtime system that the renderer belongs to.");
            public GUIContent RealtimeLMInstanceHash = EditorGUIUtility.TextContent("Instance Hash|The hash of the realtime GI instance.");
            public GUIContent RealtimeLMGeometryHash = EditorGUIUtility.TextContent("Geometry Hash|The hash of the realtime GI geometry that the renderer is using.");
            public GUIContent UVCharting = EditorGUIUtility.TextContent("UV Charting Control");
            public GUIContent LightmapSettings = EditorGUIUtility.TextContent("Lightmap Settings");
            public GUIContent LightmapStatic = EditorGUIUtility.TextContent("Lightmap Static|Controls whether the geometry will be marked as Static for lightmapping purposes. When enabled, this mesh will be present in lightmap calculations.");
            public GUIContent CastShadows = EditorGUIUtility.TextContent("Cast Shadows|Specifies whether a geometry creates shadows or not when a shadow-casting Light shines on it.");
            public GUIContent ReceiveShadows = EditorGUIUtility.TextContent("Receive Shadows|When enabled, any shadows cast from other objects are drawn on the geometry.");
            public GUIContent MotionVectors = EditorGUIUtility.TextContent("Motion Vectors|Specifies whether the Mesh renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture.");

            public GUIContent LightmapInfoBox = EditorGUIUtility.TextContent("To enable generation of lightmaps for this Mesh Renderer, please enable the 'Lightmap Static' property.");
            public GUIContent TerrainLightmapInfoBox = EditorGUIUtility.TextContent("To enable generation of lightmaps for this Mesh Renderer, please enable the 'Lightmap Static' property.");
            public GUIContent ResolutionTooHighWarning = EditorGUIUtility.TextContent("Precompute/indirect resolution for this terrain is probably too high. Use a lower realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a lower resolution setting. Otherwise it may take a very long time to bake and memory consumption during and after the bake may be very high.");
            public GUIContent ResolutionTooLowWarning = EditorGUIUtility.TextContent("Precompute/indirect resolution for this terrain is probably too low. If the Clustering stage takes a long time, try using a higher realtime/indirect resolution setting in the Lighting window or assign LightmapParameters that use a higher resolution setting.");
            public GUIContent GINotEnabledInfo = EditorGUIUtility.TextContent("Lightmapping settings are currently disabled. Enable Baked Global Illumination or Realtime Global Illumination to display these settings.");
        }

        static Styles s_Styles;

        bool m_ShowSettings = false;
        bool m_ShowChartingSettings = true;
        bool m_ShowLightmapSettings = true;
        bool m_ShowBakedLM = false;
        bool m_ShowRealtimeLM = false;
        AnimBool m_ShowClampedSize = new AnimBool();

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
        SerializedProperty m_CastShadows;
        SerializedProperty m_ReceiveShadows;
        SerializedProperty m_MotionVectors;

        Renderer[] m_Renderers;
        Terrain[] m_Terrains;

        public bool showSettings { get { return m_ShowSettings; } set { m_ShowSettings = value; } }
        public bool showChartingSettings { get { return m_ShowChartingSettings; } set { m_ShowChartingSettings = value; } }
        public bool showLightmapSettings { get { return m_ShowLightmapSettings; } set { m_ShowLightmapSettings = value; } }

        private bool isPrefabAsset
        {
            get
            {
                if (m_SerializedObject == null || m_SerializedObject.targetObject == null)
                    return false;

                PrefabType type = PrefabUtility.GetPrefabType(m_SerializedObject.targetObject);
                return (type == PrefabType.Prefab || type == PrefabType.ModelPrefab);
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
            m_CastShadows = serializedObject.FindProperty("m_CastShadows");
            m_ReceiveShadows = serializedObject.FindProperty("m_ReceiveShadows");
            m_MotionVectors = serializedObject.FindProperty("m_MotionVectors");

            m_Renderers = m_SerializedObject.targetObjects.OfType<Renderer>().ToArray();
            m_Terrains = m_SerializedObject.targetObjects.OfType<Terrain>().ToArray();

            m_StaticEditorFlags = m_GameObjectsSerializedObject.FindProperty("m_StaticEditorFlags");
        }

        public bool Begin()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            m_ShowSettings = EditorGUILayout.Foldout(m_ShowSettings, s_Styles.Lighting);
            if (!m_ShowSettings)
                return false;

            EditorGUI.indentLevel += 1;
            return true;
        }

        public void End()
        {
            if (m_ShowSettings)
            {
                EditorGUI.indentLevel -= 1;
            }
        }

        public void RenderMeshSettings(bool showLightmapSettings)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjects.Length == 0)
                return;

            m_GameObjectsSerializedObject.Update();

            EditorGUILayout.PropertyField(m_CastShadows, s_Styles.CastShadows, true);
            bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();

            using (new EditorGUI.DisabledScope(isDeferredRenderingPath))
                EditorGUILayout.PropertyField(m_ReceiveShadows, s_Styles.ReceiveShadows, true);

            EditorGUILayout.PropertyField(m_MotionVectors, s_Styles.MotionVectors, true);

            if (!showLightmapSettings)
                return;

            LightmapStaticSettings();

            if (!LightModeUtil.Get().IsAnyGIEnabled() && !isPrefabAsset)
            {
                EditorGUILayout.HelpBox(s_Styles.GINotEnabledInfo.text, MessageType.Info);
                return;
            }

            bool enableSettings = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            // We want to show the lightmap settings if the lightmap static flag is set.
            // Most of the settings apply to both, realtime and baked GI.
            if (enableSettings)
            {
                m_ShowChartingSettings = EditorGUILayout.Foldout(m_ShowChartingSettings, s_Styles.UVCharting);
                if (m_ShowChartingSettings)
                    RendererUVSettings();

                m_ShowLightmapSettings = EditorGUILayout.Foldout(m_ShowLightmapSettings, s_Styles.LightmapSettings);

                if (m_ShowLightmapSettings)
                {
                    EditorGUI.indentLevel += 1;

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

                    EditorGUILayout.PropertyField(m_ImportantGI, s_Styles.ImportantGI);

                    bool pathTracerIsActive = (
                            LightModeUtil.Get().AreBakedLightmapsEnabled() &&
                            LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU);

                    if (isPrefabAsset || pathTracerIsActive)
                        EditorGUILayout.PropertyField(m_StitchLightmapSeams, s_Styles.StitchLightmapSeams);

                    LightmapParametersGUI(m_LightmapParameters, s_Styles.LightmapParameters);

                    m_ShowBakedLM = EditorGUILayout.Foldout(m_ShowBakedLM, s_Styles.Atlas);
                    if (m_ShowBakedLM)
                        ShowAtlasGUI(m_Renderers[0].GetInstanceID());

                    m_ShowRealtimeLM = EditorGUILayout.Foldout(m_ShowRealtimeLM, s_Styles.RealtimeLM);
                    if (m_ShowRealtimeLM)
                        ShowRealtimeLMGUI(m_Renderers[0]);

                    EditorGUI.indentLevel -= 1;
                }

                if (LightmapEditorSettings.HasZeroAreaMesh(m_Renderers[0]))
                    EditorGUILayout.HelpBox(s_Styles.ZeroAreaPackingMesh.text, MessageType.Warning);

                if (LightmapEditorSettings.HasClampedResolution(m_Renderers[0]))
                    EditorGUILayout.HelpBox(s_Styles.ClampedPackingResolution.text, MessageType.Warning);

                if (!HasNormals(m_Renderers[0]))
                    EditorGUILayout.HelpBox(s_Styles.NoNormalsNoLightmapping.text, MessageType.Warning);

                m_SerializedObject.ApplyModifiedProperties();
            }
            else
                EditorGUILayout.HelpBox(s_Styles.LightmapInfoBox.text, MessageType.Info);
        }

        public void RenderTerrainSettings()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_SerializedObject == null || m_GameObjectsSerializedObject == null || m_GameObjectsSerializedObject.targetObjects.Length == 0)
                return;

            m_GameObjectsSerializedObject.Update();

            LightmapStaticSettings();

            if (!LightModeUtil.Get().IsAnyGIEnabled() && !isPrefabAsset)
            {
                EditorGUILayout.HelpBox(s_Styles.GINotEnabledInfo.text, MessageType.Info);
                return;
            }

            bool enableSettings = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            if (enableSettings)
            {
                m_ShowLightmapSettings = EditorGUILayout.Foldout(m_ShowLightmapSettings, s_Styles.LightmapSettings);

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

                        LightmapParametersGUI(m_LightmapParameters, s_Styles.LightmapParameters);

                        if (GUI.enabled && m_Terrains.Length == 1 && m_Terrains[0].terrainData != null)
                            ShowBakePerformanceWarning(m_Terrains[0]);

                        m_ShowBakedLM = EditorGUILayout.Foldout(m_ShowBakedLM, s_Styles.Atlas);
                        if (m_ShowBakedLM)
                            ShowAtlasGUI(m_Terrains[0].GetInstanceID());

                        m_ShowRealtimeLM = EditorGUILayout.Foldout(m_ShowRealtimeLM, s_Styles.RealtimeLM);
                        if (m_ShowRealtimeLM)
                            ShowRealtimeLMGUI(m_Terrains[0]);

                        m_SerializedObject.ApplyModifiedProperties();
                    }
                    EditorGUI.indentLevel -= 1;
                }
                GUILayout.Space(10);
            }
            else
                EditorGUILayout.HelpBox(s_Styles.TerrainLightmapInfoBox.text, MessageType.Info);
        }

        void LightmapStaticSettings()
        {
            bool lightmapStatic = (m_StaticEditorFlags.intValue & (int)StaticEditorFlags.LightmapStatic) != 0;

            EditorGUI.BeginChangeCheck();
            lightmapStatic = EditorGUILayout.Toggle(s_Styles.LightmapStatic, lightmapStatic);

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
            optimizeRealtimeUVs = EditorGUILayout.Toggle(s_Styles.OptimizeRealtimeUVs, optimizeRealtimeUVs);

            if (EditorGUI.EndChangeCheck())
            {
                m_PreserveUVs.boolValue = !optimizeRealtimeUVs;
            }

            EditorGUI.indentLevel++;
            bool disabledAutoUVs = m_PreserveUVs.boolValue;
            using (new EditorGUI.DisabledScope(disabledAutoUVs))
            {
                EditorGUILayout.PropertyField(m_AutoUVMaxDistance, s_Styles.AutoUVMaxDistance);
                if (m_AutoUVMaxDistance.floatValue < 0.0f)
                    m_AutoUVMaxDistance.floatValue = 0.0f;
                EditorGUILayout.Slider(m_AutoUVMaxAngle, 0, 180, s_Styles.AutoUVMaxAngle);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(m_IgnoreNormalsForChartDetection, s_Styles.IgnoreNormalsForChartDetection);

            EditorGUILayout.IntPopup(m_MinimumChartSize, s_Styles.MinimumChartSizeStrings, s_Styles.MinimumChartSizeValues, s_Styles.MinimumChartSize);
            EditorGUI.indentLevel--;
        }

        void ShowClampedSizeInLightmapGUI(float lightmapScale, float cachedSurfaceArea)
        {
            float sizeInLightmap = Mathf.Sqrt(cachedSurfaceArea) * LightmapEditorSettings.bakeResolution * lightmapScale;
            float maxAtlasSize = Math.Min(LightmapEditorSettings.maxAtlasWidth, LightmapEditorSettings.maxAtlasHeight);
            m_ShowClampedSize.target = sizeInLightmap > maxAtlasSize;

            if (EditorGUILayout.BeginFadeGroup(m_ShowClampedSize.faded))
                EditorGUILayout.HelpBox(s_Styles.ClampedSize.text, MessageType.Info);

            EditorGUILayout.EndFadeGroup();
        }

        float LightmapScaleGUI(float lodScale)
        {
            float lightmapScale = lodScale * m_LightmapScale.floatValue;

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, s_Styles.ScaleInLightmap, m_LightmapScale);
            EditorGUI.BeginChangeCheck();
            lightmapScale = EditorGUI.FloatField(rect, s_Styles.ScaleInLightmap, lightmapScale);
            if (EditorGUI.EndChangeCheck())
                m_LightmapScale.floatValue = Mathf.Max(lightmapScale / Mathf.Max(lodScale, float.Epsilon), 0.0f);
            EditorGUI.EndProperty();

            return lightmapScale;
        }

        void ShowAtlasGUI(int instanceID)
        {
            EditorGUI.indentLevel += 1;

            Hash128 instanceHash;
            LightmapEditorSettings.GetPVRInstanceHash(instanceID, out instanceHash);
            EditorGUILayout.LabelField(s_Styles.PVRInstanceHash, GUIContent.Temp(instanceHash.ToString()));

            Hash128 atlasHash;
            LightmapEditorSettings.GetPVRAtlasHash(instanceID, out atlasHash);
            EditorGUILayout.LabelField(s_Styles.PVRAtlasHash, GUIContent.Temp(atlasHash.ToString()));

            int atlasInstanceOffset;
            LightmapEditorSettings.GetPVRAtlasInstanceOffset(instanceID, out atlasInstanceOffset);
            EditorGUILayout.LabelField(s_Styles.PVRAtlasInstanceOffset, GUIContent.Temp(atlasInstanceOffset.ToString()));

            EditorGUILayout.LabelField(s_Styles.AtlasIndex, GUIContent.Temp(m_LightmapIndex.intValue.ToString()));
            EditorGUILayout.LabelField(s_Styles.AtlasTilingX, GUIContent.Temp(m_LightmapTilingOffsetX.floatValue.ToString()));
            EditorGUILayout.LabelField(s_Styles.AtlasTilingY, GUIContent.Temp(m_LightmapTilingOffsetY.floatValue.ToString()));
            EditorGUILayout.LabelField(s_Styles.AtlasOffsetX, GUIContent.Temp(m_LightmapTilingOffsetZ.floatValue.ToString()));
            EditorGUILayout.LabelField(s_Styles.AtlasOffsetY, GUIContent.Temp(m_LightmapTilingOffsetW.floatValue.ToString()));
            EditorGUI.indentLevel -= 1;
        }

        void ShowRealtimeLMGUI(Terrain terrain)
        {
            EditorGUI.indentLevel += 1;

            // Resolution of the system.
            int width, height;
            int numChunksInX, numChunksInY;
            if (LightmapEditorSettings.GetTerrainSystemResolution(terrain, out width, out height, out numChunksInX, out numChunksInY))
            {
                var str = width.ToString() + "x" + height.ToString();
                if (numChunksInX > 1 || numChunksInY > 1)
                    str += string.Format(" ({0}x{1} chunks)", numChunksInX, numChunksInY);
                EditorGUILayout.LabelField(s_Styles.RealtimeLMResolution, GUIContent.Temp(str));
            }

            EditorGUI.indentLevel -= 1;
        }

        void ShowRealtimeLMGUI(Renderer renderer)
        {
            EditorGUI.indentLevel += 1;

            // The hash of the instance.
            Hash128 instanceHash;
            if (LightmapEditorSettings.GetInstanceHash(renderer, out instanceHash))
            {
                EditorGUILayout.LabelField(s_Styles.RealtimeLMInstanceHash, GUIContent.Temp(instanceHash.ToString()));
            }

            // The hash of the geometry.
            Hash128 geometryHash;
            if (LightmapEditorSettings.GetGeometryHash(renderer, out geometryHash))
            {
                EditorGUILayout.LabelField(s_Styles.RealtimeLMGeometryHash, GUIContent.Temp(geometryHash.ToString()));
            }

            // Resolution of the packed instance.
            int instWidth, instHeight;
            if (LightmapEditorSettings.GetInstanceResolution(renderer, out instWidth, out instHeight))
            {
                EditorGUILayout.LabelField(s_Styles.RealtimeLMInstanceResolution, GUIContent.Temp(instWidth.ToString() + "x" + instHeight.ToString()));
            }

            // The hash of the system.
            Hash128 inputSystemHash;
            if (LightmapEditorSettings.GetInputSystemHash(renderer, out inputSystemHash))
            {
                EditorGUILayout.LabelField(s_Styles.RealtimeLMInputSystemHash, GUIContent.Temp(inputSystemHash.ToString()));
            }

            // Resolution of the system.
            int width, height;
            if (LightmapEditorSettings.GetSystemResolution(renderer, out width, out height))
            {
                EditorGUILayout.LabelField(s_Styles.RealtimeLMResolution, GUIContent.Temp(width.ToString() + "x" + height.ToString()));
            }

            EditorGUI.indentLevel -= 1;
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
                EditorGUILayout.HelpBox(string.Format("Terrain is chunked up into {0} instances for baking.", terrainChunksX * terrainChunksY), MessageType.None);
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
                EditorGUILayout.HelpBox(s_Styles.ResolutionTooHighWarning.text, MessageType.Warning);
            }

            var terrainClustersInWidth = terrainSystemTexelsInWidth * lightmapParameters.clusterResolution;
            var terrainClustersInHeight = terrainSystemTexelsInHeight * lightmapParameters.clusterResolution;
            var terrainTrisPerClusterInWidth = terrain.terrainData.heightmapResolution / terrainClustersInWidth;
            var terrainTrisPerClusterInHeight = terrain.terrainData.heightmapResolution / terrainClustersInHeight;
            const float kTerrainClusterTriDensityThreshold = 256.0f / 5.0f;
            if (terrainTrisPerClusterInWidth > kTerrainClusterTriDensityThreshold || terrainTrisPerClusterInHeight > kTerrainClusterTriDensityThreshold)
            {
                EditorGUILayout.HelpBox(s_Styles.ResolutionTooLowWarning.text, MessageType.Warning);
            }
        }
    }
}
