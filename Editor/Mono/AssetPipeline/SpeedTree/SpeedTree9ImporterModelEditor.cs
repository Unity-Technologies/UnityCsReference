// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using static UnityEditor.SpeedTree.Importer.SpeedTree9Importer;

using Styles = UnityEditor.SpeedTree.Importer.SpeedTreeImporterCommonEditor.Styles;

namespace UnityEditor.SpeedTree.Importer
{
    class SpeedTree9ImporterModelEditor : BaseSpeedTree9ImporterTabUI
    {
        // Mesh properties
        private SerializedProperty m_UnitConversion;
        private SerializedProperty m_ScaleFactor;

        // Material properties
        private SerializedProperty m_MainColor;
        private SerializedProperty m_EnableHueVariation;
        private SerializedProperty m_HueVariation;
        private SerializedProperty m_AlphaClipThreshold;
        private SerializedProperty m_TransmissionScale;
        private SerializedProperty m_EnableBumpMapping;
        private SerializedProperty m_EnableSubsurfaceScattering;
        private SerializedProperty m_DiffusionProfileAssetID;
        private SerializedProperty m_DiffusionProfileID;

        // Lighting properties
        private SerializedProperty m_EnableShadowCasting;
        private SerializedProperty m_EnableShadowReceiving;
        private SerializedProperty m_EnableLightProbes;
        private SerializedProperty m_ReflectionProbeEnumValue;

        // Additional Settings properties
        private SerializedProperty m_MotionVectorModeEnumValue;
        private SerializedProperty m_GenerateColliders;

        // LOD properties
        private SerializedProperty m_EnableSmoothLOD;
        private SerializedProperty m_AnimateCrossFading;
        private SerializedProperty m_BillboardTransitionCrossFadeWidth;
        private SerializedProperty m_FadeOutWidth;
        private SerializedProperty m_PerLODSettings;

        // LODGroup GUI
        private int m_SelectedLODSlider = -1;
        private int m_SelectedLODRange = 0;
        private SavedBool[] m_LODGroupFoldoutHeaderValues = null;
        private Texture2D[] m_LODColorTextures;

        private AnimBool m_ShowSmoothLODOptions = new AnimBool();
        private AnimBool m_ShowCrossFadeWidthOptions = new AnimBool();

        private SpeedTree9Importer m_StEditor;

        public SpeedTree9ImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        { }

        internal override void OnEnable()
        {
            base.OnEnable();

            m_StEditor = target as SpeedTree9Importer;

            // Mesh properties
            {
                MeshSettings meshSettings = m_StEditor.m_MeshSettings;
                string meshSettingsStr = nameof(m_StEditor.m_MeshSettings);

                m_UnitConversion = FindPropertyFromName(meshSettingsStr, nameof(meshSettings.unitConversion));
                m_ScaleFactor = FindPropertyFromName(meshSettingsStr, nameof(meshSettings.scaleFactor));
            }

            // Material properties
            {
                MaterialSettings matSettings = m_StEditor.m_MaterialSettings;
                string matSettingsStr = nameof(m_StEditor.m_MaterialSettings);

                m_MainColor = FindPropertyFromName(matSettingsStr, nameof(matSettings.mainColor));
                m_EnableHueVariation = FindPropertyFromName(matSettingsStr, nameof(matSettings.enableHueVariation));
                m_HueVariation = FindPropertyFromName(matSettingsStr, nameof(matSettings.hueVariation));
                m_AlphaClipThreshold = FindPropertyFromName(matSettingsStr, nameof(matSettings.alphaClipThreshold));
                m_TransmissionScale = FindPropertyFromName(matSettingsStr, nameof(matSettings.transmissionScale));
                m_EnableBumpMapping = FindPropertyFromName(matSettingsStr, nameof(matSettings.enableBumpMapping));
                m_EnableSubsurfaceScattering = FindPropertyFromName(matSettingsStr, nameof(matSettings.enableSubsurfaceScattering));

                m_DiffusionProfileAssetID = FindPropertyFromName(matSettingsStr, nameof(matSettings.diffusionProfileAssetID));
                m_DiffusionProfileID = FindPropertyFromName(matSettingsStr, nameof(matSettings.diffusionProfileID));
            }

            // Lighting properties
            {
                LightingSettings lightSettings = m_StEditor.m_LightingSettings;
                string lightSettingsStr = nameof(m_StEditor.m_LightingSettings);

                m_EnableShadowCasting = FindPropertyFromName(lightSettingsStr, nameof(lightSettings.enableShadowCasting));
                m_EnableShadowReceiving = FindPropertyFromName(lightSettingsStr, nameof(lightSettings.enableShadowReceiving));
                m_EnableLightProbes = FindPropertyFromName(lightSettingsStr, nameof(lightSettings.enableLightProbes));
                m_ReflectionProbeEnumValue = FindPropertyFromName(lightSettingsStr, nameof(lightSettings.reflectionProbeEnumValue));
            }

            // Additional Settings properties
            {
                AdditionalSettings addSettings = m_StEditor.m_AdditionalSettings;
                string addSettingsStr = nameof(m_StEditor.m_AdditionalSettings);

                m_MotionVectorModeEnumValue = FindPropertyFromName(addSettingsStr, nameof(addSettings.motionVectorModeEnumValue));
                m_GenerateColliders = FindPropertyFromName(addSettingsStr, nameof(addSettings.generateColliders));
            }

            // LOD properties
            {
                LODSettings lodSettings = m_StEditor.m_LODSettings;
                string lodSettingsStr = nameof(m_StEditor.m_LODSettings);

                m_EnableSmoothLOD = FindPropertyFromName(lodSettingsStr, nameof(lodSettings.enableSmoothLODTransition));
                m_AnimateCrossFading = FindPropertyFromName(lodSettingsStr, nameof(lodSettings.animateCrossFading));
                m_BillboardTransitionCrossFadeWidth = FindPropertyFromName(lodSettingsStr, nameof(lodSettings.billboardTransitionCrossFadeWidth));
                m_FadeOutWidth = FindPropertyFromName(lodSettingsStr, nameof(lodSettings.fadeOutWidth));
                m_PerLODSettings = serializedObject.FindProperty(nameof(m_StEditor.m_PerLODSettings));
            }

            // Other
            {
                m_ShowSmoothLODOptions.value = m_EnableSmoothLOD.hasMultipleDifferentValues || m_EnableSmoothLOD.boolValue;
                m_ShowSmoothLODOptions.valueChanged.AddListener(Repaint);

                m_ShowCrossFadeWidthOptions.value = m_AnimateCrossFading.hasMultipleDifferentValues || !m_AnimateCrossFading.boolValue;
                m_ShowCrossFadeWidthOptions.valueChanged.AddListener(Repaint);
            }

            ResetFoldoutLists();
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            m_ShowSmoothLODOptions.valueChanged.RemoveListener(Repaint);
            m_ShowCrossFadeWidthOptions.valueChanged.RemoveListener(Repaint);
        }

        void TriggerCallbacks()
        {
            var allMethods = AttributeHelper.GetMethodsWithAttribute<DiffuseProfileCallbackAttribute>().methodsWithAttributes;
            foreach (var method in allMethods)
            {
                var callback = Delegate.CreateDelegate(typeof(OnCustomEditorSettings), method.info) as OnCustomEditorSettings;
                callback?.Invoke(ref m_DiffusionProfileAssetID, ref m_DiffusionProfileID);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Mesh properties
            SpeedTreeImporterCommonEditor.ShowMeshGUI(ref m_UnitConversion, ref m_ScaleFactor);

            EditorGUILayout.Space();

            // Material Properties
            SpeedTreeImporterCommonEditor.ShowMaterialGUI(
                ref m_MainColor,
                ref m_EnableHueVariation,
                ref m_HueVariation,
                ref m_AlphaClipThreshold,
                ref m_EnableBumpMapping,
                ref m_EnableSubsurfaceScattering,
                renderHueVariationDropdown: m_EnableHueVariation.boolValue,
                renderAlphaTestRef: m_OutputImporterData.hasAlphaClipThreshold
                );

            if (m_OutputImporterData.hasTransmissionScale)
            {
                EditorGUILayout.PropertyField(m_TransmissionScale, Styles.TransmissionScale);
            }

            // Update the Diffuse Profile is necessary
            TriggerCallbacks();

            EditorGUILayout.Space();

            // Lighting Properties
            SpeedTreeImporterCommonEditor.ShowLightingGUI(
                ref m_EnableShadowCasting,
                ref m_EnableShadowReceiving,
                ref m_EnableLightProbes,
                ref m_ReflectionProbeEnumValue);

            EditorGUILayout.Space();

            // Additional Settings
            SpeedTreeImporterCommonEditor.ShowAdditionalSettingsGUI(
                ref m_MotionVectorModeEnumValue,
                ref m_GenerateColliders);

            EditorGUILayout.Space();

            // LOD properties
            SpeedTreeImporterCommonEditor.ShowLODGUI(
                ref m_EnableSmoothLOD,
                ref m_AnimateCrossFading,
                ref m_BillboardTransitionCrossFadeWidth,
                ref m_FadeOutWidth,
                ref m_ShowSmoothLODOptions,
                ref m_ShowCrossFadeWidthOptions);

            EditorGUILayout.Space();

            ShowLODGUI();

            ShowMaterialWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowMaterialWarnings()
        {
            EditorGUILayout.Space();

            bool materialsNeedToBeUpgraded = upgradeMaterials;

            if (materialsNeedToBeUpgraded)
            {
                EditorGUILayout.HelpBox(
                    String.Format("SpeedTree materials need to be upgraded. " +
                    "Please back them up (if modified manually) then hit the \"{0}\" button below.", Styles.ApplyAndGenerate.text)
                    , MessageType.Warning
                );
            }
            else if (!materialsNeedToBeUpgraded && DoMaterialsHaveDifferentShader())
            {
                EditorGUILayout.HelpBox(
                    String.Format("There is a different SpeedTree shader provided by the current render pipeline " +
                    "which probably is more suitable for rendering. Hit the \"{0}\" button to regenerate the materials."
                        , (panelContainer as SpeedTree9ImporterEditor).GetGenerateButtonText(HasModified()
                        , materialsNeedToBeUpgraded).text
                    )
                    , MessageType.Warning
                );
            }
        }

        private SerializedProperty FindPropertyFromName(string parentProperty, string childProperty)
        {
            const string dotStr = ".";

            string finalName = String.Concat(parentProperty, dotStr, childProperty);

            return serializedObject.FindProperty(finalName);
        }

        internal bool DoMaterialsHaveDifferentShader()
        {
            if (assetTargets is null || assetTargets.Length == 0)
            {
                return false;
            }

            GameObject[] prefabs = new GameObject[assetTargets.Length];
            for (int i = 0; i < assetTargets.Length; ++i)
            {
                prefabs[i] = assetTargets[i] as GameObject;
            }
            
            List<SpeedTree9Importer> importerArray = new List<SpeedTree9Importer>();
            foreach (SpeedTree9Importer importer in importers)
            {
                importerArray.Add(importer);
            }

            var renderPipeline = SpeedTreeImporterCommon.GetCurrentRenderPipelineType();

            // In tests assetTargets can become null
            for (int i = 0; i < Math.Min(importerArray.Count, prefabs?.Length ?? 0); ++i)
            {
                var im = importerArray[i];
                var defaultShaderName = im.GetShaderNameFromPipeline(renderPipeline);

                foreach (var mr in prefabs[i].transform.GetComponentsInChildren<MeshRenderer>())
                {
                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat?.shader.name != defaultShaderName)
                            return true;
                    }
                }
            }

            return false;
        }

        // TODO: Abstract the following code, so it can be shared between ST8 and ST9.

        private void ShowLODGUI()
        {
            // LOD slider + Customizations
            if (HasSameLODConfig())
            {
                var area = GUILayoutUtility.GetRect(0, LODGroupGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));
                var lods = GetLODInfoArray(area);
                bool bDrawLODCustomizationGUI = m_SelectedLODRange != -1 && lods.Count > 0;

                EditorGUILayout.Space();

                DrawLODLevelSlider(area, lods);

                if (bDrawLODCustomizationGUI)
                {
                    GUILayout.Space(5);
                    DrawLODGroupFoldouts(lods);
                }
            }

            //  Mixed Value LOD Slider
            else
            {
                if (CanUnifyLODConfig())
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    Rect buttonRect = GUILayoutUtility.GetRect(Styles.ResetLOD, EditorStyles.miniButton);
                    if (GUI.Button(buttonRect, Styles.ResetLOD, EditorStyles.miniButton))
                    {
                        var dropDownMenu = new GenericMenu();
                        foreach (SpeedTree9Importer importer in importers)
                        {
                            float[] heights = importer.GetPerLODSettingsHeights();
                            string[] heightsFormated = new string[heights.Length];
                            for (int i = 0; i < heights.Length; ++i)
                            {
                                heightsFormated[i] = UnityString.Format("{0:0}%", heights[i] * 100);
                            }

                            var menuText = String.Format("{0}: {1}",
                                Path.GetFileNameWithoutExtension(importer.assetPath),
                                String.Join(" | ", heightsFormated));
                            dropDownMenu.AddItem(new GUIContent(menuText), false, OnResetLODMenuClick, importer);
                        }
                        dropDownMenu.DropDown(buttonRect);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox(Styles.MultiSelectionLODNotSupported.text, MessageType.Info);
                }
            }

            EditorGUILayout.Space();
        }

        private readonly int m_LODSliderId = "LODSliderIDHash".GetHashCode();

        private void DrawLODLevelSlider(Rect sliderPosition, List<LODGroupGUI.LODInfo> lods)
        {
            int sliderId = GUIUtility.GetControlID(m_LODSliderId, FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(sliderId))
            {
                case EventType.Repaint:
                    {
                        LODGroupGUI.DrawLODSlider(sliderPosition, lods, m_SelectedLODRange);
                        break;
                    }
                case EventType.MouseDown:
                    {
                        // Slightly grow position on the x because edge buttons overflow by 5 pixels
                        var barPosition = sliderPosition;
                        barPosition.x -= 5;
                        barPosition.width += 10;

                        if (barPosition.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            GUIUtility.hotControl = sliderId;

                            // Check for button click
                            var clickedButton = false;

                            // case:464019 have to re-sort the LOD array for these buttons to get the overlaps in the right order...
                            List<LODGroupGUI.LODInfo> lodsLeft = new List<LODGroupGUI.LODInfo>();
                            List<LODGroupGUI.LODInfo> lodsRight = new List<LODGroupGUI.LODInfo>();

                            foreach (LODGroupGUI.LODInfo lodInfo in lods)
                            {
                                if (lodInfo.ScreenPercent > 0.5f)
                                {
                                    lodsLeft.Add(lodInfo);
                                }
                                else
                                {
                                    lodsRight.Add(lodInfo);
                                }
                            }

                            // Descending order.
                            lodsLeft.Sort(new Comparison<LODGroupGUI.LODInfo>((i1, i2) => i2.LODLevel.CompareTo(i1.LODLevel)));

                            // Ascending order.
                            lodsRight.Sort(new Comparison<LODGroupGUI.LODInfo>((i1, i2) => i1.LODLevel.CompareTo(i2.LODLevel)));

                            var lodButtonOrder = new List<LODGroupGUI.LODInfo>();
                            lodButtonOrder.AddRange(lodsLeft);
                            lodButtonOrder.AddRange(lodsRight);

                            foreach (var lod in lodButtonOrder)
                            {
                                if (lod.m_ButtonPosition.Contains(evt.mousePosition))
                                {
                                    m_SelectedLODSlider = lod.LODLevel;
                                    m_SelectedLODRange = lod.LODLevel;
                                    clickedButton = true;
                                    break;
                                }
                            }

                            if (!clickedButton)
                            {
                                // Check for range click
                                foreach (var lod in lodButtonOrder)
                                {
                                    if (lod.m_RangePosition.Contains(evt.mousePosition))
                                    {
                                        m_SelectedLODSlider = -1;
                                        m_SelectedLODRange = lod.LODLevel;
                                        ExpandSelectedHeaderAndCloseRemaining(m_SelectedLODRange);
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == sliderId)
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == sliderId && m_SelectedLODSlider >= 0 && lods[m_SelectedLODSlider] != null)
                        {
                            evt.Use();

                            var cameraPercent = LODGroupGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                            // Bias by 0.1% so that there is no skipping when sliding
                            LODGroupGUI.SetSelectedLODLevelPercentage(cameraPercent - 0.001f, m_SelectedLODSlider, lods);
                            m_PerLODSettings.GetArrayElementAtIndex(m_SelectedLODSlider).FindPropertyRelative("height").floatValue = lods[m_SelectedLODSlider].RawScreenPercent;
                        }
                        break;
                    }
            }
        }

        private void DrawLODGroupFoldouts(List<LODGroupGUI.LODInfo> lods)
        {
            // check camera and bail if null
            Camera camera = null;
            if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.camera)
                camera = SceneView.lastActiveSceneView.camera;
            if (camera == null)
                return;

            // draw lod foldouts
            for (int i = 0; i < m_PerLODSettings.arraySize; i++)
            {
                DrawLODGroupFoldout(camera, i, ref m_LODGroupFoldoutHeaderValues[i], lods);
            }
        }

        private string GetLODSubmeshAndTriCountLabel(int numLODs, int lodGroupIndex, SpeedTree9Importer im, LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            Debug.Assert(lods.Length == numLODs);

            int[][] primitiveCounts = new int[numLODs][];
            int[] submeshCounts = new int[numLODs];
            for (int i = 0; i < lods.Length; i++)
            {
                Renderer[] renderers = lods[i].renderers;
                primitiveCounts[i] = new int[renderers.Length];

                for (int j = 0; j < renderers.Length; j++)
                {
                    bool hasMismatchingSubMeshTopologyTypes = LODGroupEditor.CheckIfMeshesHaveMatchingTopologyTypes(renderers);

                    Mesh rendererMesh = LODGroupEditor.GetMeshFromRendererIfAvailable(renderers[j]);
                    if (rendererMesh == null)
                        continue;

                    submeshCounts[i] += rendererMesh.subMeshCount;

                    if (hasMismatchingSubMeshTopologyTypes)
                    {
                        primitiveCounts[i][j] = rendererMesh.vertexCount;
                    }
                    else
                    {
                        for (int subMeshIndex = 0; subMeshIndex < rendererMesh.subMeshCount; subMeshIndex++)
                        {
                            primitiveCounts[i][j] += (int)rendererMesh.GetIndexCount(subMeshIndex) / 3;
                        }
                    }
                }
            }

            int totalTriCount = 0;
            if (primitiveCounts.Length > 0 && primitiveCounts[lodGroupIndex] != null)
            {
                Array.ForEach(primitiveCounts[lodGroupIndex], delegate (int i) { totalTriCount += i; });
            }

            int sumPrimitiveCounts = 0;
            Array.ForEach(primitiveCounts[0], delegate (int i) { sumPrimitiveCounts += i; });

            int lod0TriCount = sumPrimitiveCounts;

            var triCountChange = lod0TriCount != 0 ? (float)totalTriCount / lod0TriCount * 100 : 0;
            string triangleChangeLabel = lodGroupIndex > 0 && lod0TriCount != 0 ? $"({triCountChange.ToString("f2")}% LOD0)" : "";

            bool wideInspector = Screen.width >= 480;
            triangleChangeLabel = wideInspector ? triangleChangeLabel : "";
            string submeshCountLabel = wideInspector ? $"- {submeshCounts[lodGroupIndex]} Sub Mesh(es)" : "";

            return $"{totalTriCount} {LODGroupGUI.GUIStyles.m_TriangleCountLabel.text} {triangleChangeLabel} {submeshCountLabel}";
        }

        private void DrawLODGroupFoldout(Camera camera, int lodGroupIndex, ref SavedBool foldoutState, List<LODGroupGUI.LODInfo> lodInfoList)
        {
            GameObject[] ObjectArrayToGameObjectArray(UnityEngine.Object[] objects)
            {
                if (objects == null)
                    return null;

                GameObject[] gameObjects = new GameObject[objects.Length];

                for (int i = 0; i < objects.Length; ++i)
                {
                    gameObjects[i] = objects[i] as GameObject;
                }

                return gameObjects;
            }

            List<SpeedTree9Importer> importersList = new List<SpeedTree9Importer>(importers);

            GameObject[] prefabs = ObjectArrayToGameObjectArray(assetTargets); // In tests assetTargets can become null
            SpeedTree9Importer[] importerArray = importersList.ToArray();

            int numSelectedAssets = Math.Min(importerArray.Length, prefabs?.Length ?? 0);
            bool isDrawingSelectedLODGroup = m_SelectedLODRange == lodGroupIndex;

            // even though multiple assets may be selected, this code path
            // ensures the numLODs match for all the selected assets (see HasSameLODConfig() calls)
            int numLODs = m_PerLODSettings.arraySize;
            bool hasBillboard = m_OutputImporterData.hasBillboard;

            string LODFoldoutHeaderLabel = (hasBillboard && lodGroupIndex == m_PerLODSettings.arraySize - 1)
                ? "Billboard"
                : $"LOD {lodGroupIndex}";

            // primitive and submesh counts are displayed only when a single asset is selected
            string LODFoldoutHeaderGroupAdditionalText = numSelectedAssets == 1
                ? GetLODSubmeshAndTriCountLabel(numLODs, lodGroupIndex, importerArray[0], prefabs[0].GetComponentInChildren<LODGroup>())
                : "";

            // ------------------------------------------------------------------------------------------------------------------------------

            if (isDrawingSelectedLODGroup)
                LODGroupGUI.DrawRoundedBoxAroundLODDFoldout(lodGroupIndex, m_SelectedLODRange);
            else
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foldoutState.value = LODGroupGUI.FoldoutHeaderGroupInternal(
                GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebarFlat)
                , foldoutState.value
                , LODFoldoutHeaderLabel
                , m_LODColorTextures[lodGroupIndex]
                , LODGroupGUI.kLODColors[lodGroupIndex] * 0.6f // 0.5f magic number is copied from LODGroupsGUI.cs
                , LODFoldoutHeaderGroupAdditionalText
            );

            if (foldoutState.value) // expanded LOD-specific options panel
            {
                DrawLODSettingCustomizationGUI(lodInfoList, lodGroupIndex);
            }

            if (isDrawingSelectedLODGroup)
                GUILayoutUtility.EndLayoutGroup();
            else
                EditorGUILayout.EndVertical();
        }

        private void DrawLODSettingCustomizationGUI(List<LODGroupGUI.LODInfo> lods, int lodIndex)
        {
            bool isBillboard = (lodIndex == lods.Count - 1) && m_OutputImporterData.hasBillboard;

            SerializedProperty selectedLODProp = m_PerLODSettings.GetArrayElementAtIndex(lodIndex);
            SerializedProperty lodSettingOverride = selectedLODProp.FindPropertyRelative("enableSettingOverride");

            // We don't want to clutter the GUI with same options but for billboards, instead
            // we treat the Billboard LOD level as always 'overrideSettings' and display the
            // billboard options below without the 'enableSettingOverride' warning text.
            if (isBillboard)
            {
                EditorGUILayout.LabelField("Billboard Options", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(Styles.BillboardSettingsHelp.text, MessageType.Info);
            }
            else
            {
                // Toggle 
                GUIContent customizationLabel = EditorGUIUtility.TrTextContent(String.Format("Customize {0} options", lods[lodIndex].LODName), "To override options for a certain LOD, check this box and select the LOD from the LOD slider above");
                EditorGUILayout.PropertyField(lodSettingOverride, customizationLabel);

                // Warning
                if (lodSettingOverride.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.EnableLodCustomizationsWarn.text, MessageType.Warning);
                }
            }
            EditorGUILayout.Space();

            // settings
            using (new EditorGUI.DisabledScope(!lodSettingOverride.boolValue))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("castShadows"), Styles.CastShadows);

                using (new EditorGUI.DisabledScope(!UnityEngine.Rendering.SupportedRenderingFeatures.active.receiveShadows))
                {
                    EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("receiveShadows"), Styles.ReceiveShadows);
                }

                var useLightProbes = selectedLODProp.FindPropertyRelative("useLightProbes");
                EditorGUILayout.PropertyField(useLightProbes, Styles.UseLightProbes);
                if (!useLightProbes.hasMultipleDifferentValues && useLightProbes.boolValue && isBillboard)
                    EditorGUILayout.HelpBox("Enabling Light Probe for billboards breaks batched rendering and may cause performance problem.", MessageType.Warning);

                // TODO: reflection probe support when PBS is implemented
                //EditorGUILayout.PropertyField(SelectedLODProp.FindPropertyRelative("useReflectionProbes"), Styles.UseReflectionProbes);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableHue"), Styles.EnableColorVariation);
                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableBump"), Styles.EnableBump);

                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableSubsurface"), Styles.EnableSubsurface);
            }
        }

        private void OnResetLODMenuClick(object userData)
        {
            //var toHeights = (userData as SpeedTreeImporter).LODHeights;
            for (int i = 0; i < m_StEditor.m_PerLODSettings.Count; ++i)
            {
                var toHeight = m_StEditor.m_PerLODSettings[i].height;
                m_PerLODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue = toHeight;
            }
        }

        private bool HasSameLODConfig()
        {
            if (m_PerLODSettings.FindPropertyRelative("Array.size").hasMultipleDifferentValues)
                return false;

            for (int i = 0; i < m_PerLODSettings.arraySize; ++i)
            {
                if (m_PerLODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").hasMultipleDifferentValues)
                    return false;
            }
            return true;
        }

        private void ExpandSelectedHeaderAndCloseRemaining(int index)
        {
            // need this to safeguard against drag & drop on Culled section
            // as that sets the LOD index to 8 which is outside of the total
            // allowed LOD range
            if (index >= m_PerLODSettings.arraySize)
                return;

            Array.ForEach(m_LODGroupFoldoutHeaderValues, el => el.value = false);
            m_LODGroupFoldoutHeaderValues[index].value = true;
        }

        private List<LODGroupGUI.LODInfo> GetLODInfoArray(Rect area)
        {
            int lodCount = m_PerLODSettings.arraySize;
            return LODGroupGUI.CreateLODInfos(
                lodCount, area,
                i => i == lodCount - 1 && m_OutputImporterData.hasBillboard ? "Billboard" : String.Format("LOD {0}", i),
                i => m_PerLODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue);
        }

        private bool CanUnifyLODConfig()
        {
            // TODO: Add multi-selection support for LOD UI.
            return false;
        }

        void InitAndSetFoldoutLabelTextures()
        {
            m_LODColorTextures = new Texture2D[m_PerLODSettings.arraySize];
            for (int i = 0; i < m_LODColorTextures.Length; i++)
            {
                m_LODColorTextures[i] = new Texture2D(1, 1);
                m_LODColorTextures[i].SetPixel(0, 0, LODGroupGUI.kLODColors[i]);
            }
        }

        void ResetFoldoutLists()
        {
            int lodArraySize = m_PerLODSettings.arraySize;
            m_LODGroupFoldoutHeaderValues = new SavedBool[lodArraySize];
            for (int i = 0; i < lodArraySize; i++)
            {
                m_LODGroupFoldoutHeaderValues[i] = new SavedBool($"{target.GetType()}.lodFoldout{i}", false);
            }
            InitAndSetFoldoutLabelTextures();
        }
    }
}
