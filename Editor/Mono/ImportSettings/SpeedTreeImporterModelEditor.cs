// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    internal class SpeedTreeImporterModelEditor : BaseSpeedTreeImporterTabUI
    {
        private class Styles
        {
            public static GUIContent MeshesHeader = EditorGUIUtility.TrTextContent("Meshes");
            public static GUIContent UnitConversion = EditorGUIUtility.TrTextContent("Unit Conversion", "Select the unit conversion to apply to the imported SpeedTree asset");
            public static GUIContent ScaleFactor = EditorGUIUtility.TrTextContent("Scale Factor", "How much to scale the tree model, interpreting the exported units as meters");

            public static GUIContent EnableColorVariation = EditorGUIUtility.TrTextContent("Color Variation", "Color is determined by linearly interpolating between the Main Color & Color Variation values based on the world position X, Y and Z values");
            public static GUIContent EnableBump = EditorGUIUtility.TrTextContent("Normal Map", "Enable normal mapping (aka Bump mapping).");
            public static GUIContent EnableSubsurface = EditorGUIUtility.TrTextContent("Subsurface Scattering", "Enable subsurface scattering effects.");
            public static GUIContent MainColor = EditorGUIUtility.TrTextContent("Main Color", "The color modulating the diffuse lighting component.");
            public static GUIContent HueVariation = EditorGUIUtility.TrTextContent("Variation Color (RGB), Intensity (A)", "Tint the tree with the Variation Color");
            public static GUIContent AlphaTestRef = EditorGUIUtility.TrTextContent("Alpha Cutoff", "The alpha-test reference value.");

            public static GUIContent LightingHeader = EditorGUIUtility.TrTextContent("Lighting");
            public static GUIContent CastShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "The tree casts shadow");
            public static GUIContent ReceiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "The tree receives shadow");
            public static GUIContent UseLightProbes = EditorGUIUtility.TrTextContent("Light Probes", "The tree uses light probe for lighting"); // TODO: update help text
            public static GUIContent UseReflectionProbes = EditorGUIUtility.TrTextContent("Reflection Probes", "The tree uses reflection probe for rendering"); // TODO: update help text

            public static GUIContent AdditionalSettingsHeader = EditorGUIUtility.TrTextContent("Additional Settings");
            // TODO: motion vector settings?

            public static GUIContent WindHeader = EditorGUIUtility.TrTextContent("Wind");
            public static GUIContent WindQuality = EditorGUIUtility.TrTextContent("Wind Quality", "Controls the wind effect's quality.");

            public static GUIContent LODHeader = EditorGUIUtility.TrTextContent("LOD");
            public static GUIContent ResetLOD = EditorGUIUtility.TrTextContent("Reset LOD to...", "Unify the LOD settings for all selected assets");
            public static GUIContent SmoothLOD = EditorGUIUtility.TrTextContent("Smooth Transitions", "Toggles smooth LOD transitions");
            public static GUIContent AnimateCrossFading = EditorGUIUtility.TrTextContent("Animate Cross-fading", "Cross-fading is animated instead of being calculated by distance");
            public static GUIContent CrossFadeWidth = EditorGUIUtility.TrTextContent("Crossfade Width", "Proportion of the last 3D mesh LOD region width which is used for cross-fading to billboard tree");
            public static GUIContent FadeOutWidth = EditorGUIUtility.TrTextContent("Fade Out Width", "Proportion of the billboard LOD region width which is used for fading out the billboard");

            public static GUIContent EnableLodCustomizationsWarn = EditorGUIUtility.TrTextContent("Customizing LOD options may help with tuning the GPU performance but will likely negatively impact the instanced draw batching, i.e. CPU performance.\nPlease use the per-LOD customizations with careful memory and performance profiling for both CPU and GPU and remember that these options are a trade-off rather than a free win.");
            public static GUIContent BillboardSettingsHelp = EditorGUIUtility.TrTextContent("Billboard options are separate from the 3D model options shown above.\nChange the options below for influencing billboard rendering.");
            
            public static GUIContent ApplyAndGenerate = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and generate asset materials with the new settings.");
            public static GUIContent Regenerate = EditorGUIUtility.TrTextContent("Regenerate Materials", "Regenerate materials using the current import settings.");

            public static GUIContent[] ReflectionProbeUsageNames = (Enum.GetNames(typeof(ReflectionProbeUsage)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            public static GUIContent[] WindQualityNames = SpeedTreeImporter.windQualityNames.Select(s => new GUIContent(s)).ToArray();
            public static GUIContent[] UnitConversionNames =
            {
                  new GUIContent("Leave As Is")
                , new GUIContent("ft to m")
                , new GUIContent("cm to m")
                , new GUIContent("inch to m")
                , new GUIContent("Custom")
            };
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // DATA
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // mesh 
        private SerializedProperty m_ScaleFactor;
        private SerializedProperty m_UnitConversionEnumValue;

        // material
        private SerializedProperty m_MainColor;
        private SerializedProperty m_EnableHueVariation;
        private SerializedProperty m_HueVariation;
        private SerializedProperty m_AlphaTestRef;
        private SerializedProperty m_EnableBumpMapping;
        private SerializedProperty m_EnableSubsurfaceScattering;

        // lighting
        private SerializedProperty m_EnableShadowCasting;
        private SerializedProperty m_EnableShadowReceiving;
        private SerializedProperty m_EnableLightProbeUsage;
        private SerializedProperty m_ReflectionProbeUsage;

        // wind
        private SerializedProperty m_HighestWindQuality;
        private SerializedProperty m_SelectedWindQuality;

        // lod
        private SerializedProperty m_LODSettings;
        private SerializedProperty m_EnableSmoothLOD;
        private SerializedProperty m_AnimateCrossFading;
        private SerializedProperty m_BillboardTransitionCrossFadeWidth;
        private SerializedProperty m_FadeOutWidth;

        private bool m_AllAreV8;
        private bool m_AllAreNotV8;

        // LODGroup GUI
        private int m_SelectedLODSlider = -1;
        private int m_SelectedLODRange = 0;
        private SavedBool[] m_LODGroupFoldoutHeaderValues = null;
        private Texture2D[] m_LODColorTextures;

        private readonly AnimBool m_ShowSmoothLODOptions = new AnimBool();
        private readonly AnimBool m_ShowCrossFadeWidthOptions = new AnimBool();

        public bool doMaterialsHaveDifferentShader = false;

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // INTERFACE
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public SpeedTreeImporterModelEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        internal override void OnEnable()
        {
            m_ScaleFactor = serializedObject.FindProperty("m_ScaleFactor");
            m_UnitConversionEnumValue = serializedObject.FindProperty("m_UnitConversionEnumValue");

            m_MainColor = serializedObject.FindProperty("m_MainColor");
            m_EnableHueVariation = serializedObject.FindProperty("m_EnableHueVariation");
            m_HueVariation = serializedObject.FindProperty("m_HueVariation");
            m_AlphaTestRef = serializedObject.FindProperty("m_AlphaTestRef");
            m_EnableBumpMapping = serializedObject.FindProperty("m_EnableBumpMapping");
            m_EnableSubsurfaceScattering = serializedObject.FindProperty("m_EnableSubsurfaceScattering");

            m_EnableShadowCasting = serializedObject.FindProperty("m_EnableShadowCasting");
            m_EnableShadowReceiving = serializedObject.FindProperty("m_EnableShadowReceiving");
            m_EnableLightProbeUsage = serializedObject.FindProperty("m_EnableLightProbes");
            m_ReflectionProbeUsage = serializedObject.FindProperty("m_ReflectionProbeEnumValue");

            m_HighestWindQuality = serializedObject.FindProperty("m_BestWindQuality");
            m_SelectedWindQuality = serializedObject.FindProperty("m_SelectedWindQuality");

            m_LODSettings = serializedObject.FindProperty("m_LODSettings");
            m_EnableSmoothLOD = serializedObject.FindProperty("m_EnableSmoothLODTransition");
            m_AnimateCrossFading = serializedObject.FindProperty("m_AnimateCrossFading");
            m_BillboardTransitionCrossFadeWidth = serializedObject.FindProperty("m_BillboardTransitionCrossFadeWidth");
            m_FadeOutWidth = serializedObject.FindProperty("m_FadeOutWidth");

            m_ShowSmoothLODOptions.value = m_EnableSmoothLOD.hasMultipleDifferentValues || m_EnableSmoothLOD.boolValue;
            m_ShowSmoothLODOptions.valueChanged.AddListener(Repaint);
            m_ShowCrossFadeWidthOptions.value = m_AnimateCrossFading.hasMultipleDifferentValues || !m_AnimateCrossFading.boolValue;
            m_ShowCrossFadeWidthOptions.valueChanged.AddListener(Repaint);

            m_AllAreV8 = importers.All(im => im.isV8);
            m_AllAreNotV8 = importers.All(im => !im.isV8);

            ResetFoldoutLists();
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            m_ShowSmoothLODOptions.valueChanged.RemoveListener(Repaint);
            m_ShowCrossFadeWidthOptions.valueChanged.RemoveListener(Repaint);
        }

        internal List<LODGroupGUI.LODInfo> GetLODInfoArray(Rect area)
        {
            int lodCount = m_LODSettings.arraySize;
            return LODGroupGUI.CreateLODInfos(
                lodCount, area,
                i => i == lodCount - 1 && (target as SpeedTreeImporter).hasBillboard ? "Billboard" : String.Format("LOD {0}", i),
                i => m_LODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue);
        }
        private void ExpandSelectedHeaderAndCloseRemaining(int index)
        {
            // need this to safeguard against drag & drop on Culled section
            // as that sets the LOD index to 8 which is outside of the total
            // allowed LOD range
            if (index >= m_LODSettings.arraySize)
                return;

            Array.ForEach(m_LODGroupFoldoutHeaderValues, el => el.value = false);
            m_LODGroupFoldoutHeaderValues[index].value = true;
        }
        void InitAndSetFoldoutLabelTextures()
        {
            m_LODColorTextures = new Texture2D[m_LODSettings.arraySize];
            for (int i = 0; i < m_LODColorTextures.Length; i++)
            {
                m_LODColorTextures[i] = new Texture2D(1, 1);
                m_LODColorTextures[i].SetPixel(0, 0, LODGroupGUI.kLODColors[i]);
            }
        }
        void ResetFoldoutLists()
        {
            int lodArraySize = m_LODSettings.arraySize;
            m_LODGroupFoldoutHeaderValues = new SavedBool[lodArraySize];
            for (int i = 0; i < lodArraySize; i++)
            {
                m_LODGroupFoldoutHeaderValues[i] = new SavedBool($"{target.GetType()}.lodFoldout{i}", false);
            }
            InitAndSetFoldoutLabelTextures();
        }

        public bool HasSameLODConfig()
        {
            if (serializedObject.FindProperty("m_HasBillboard").hasMultipleDifferentValues)
                return false;
            if (m_LODSettings.FindPropertyRelative("Array.size").hasMultipleDifferentValues)
                return false;
            for (int i = 0; i < m_LODSettings.arraySize; ++i)
            {
                if (m_LODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").hasMultipleDifferentValues)
                    return false;
            }
            return true;
        }

        public bool CanUnifyLODConfig()
        {
            // differs only in LOD heights
            return !serializedObject.FindProperty("m_HasBillboard").hasMultipleDifferentValues
                && !m_LODSettings.FindPropertyRelative("Array.size").hasMultipleDifferentValues;
        }

        private bool DoMaterialsHaveDifferentShader()
        {
            if(assetTargets is null || assetTargets.Length == 0)
            {
                return false;
            }

            // Check whether the imported asset is a valid one.
            // GameObject cast will fail for non-geometry SpeedTree files
            // such as collections on some v7 assets. If the object is not valid,
            // the IEnumerable<GameObject>::ToArray() function call below will throw
            // an InvalidType exception, hence we do an explicit source check here.
            GameObject obj = assetTargets[0] as GameObject;
            if (obj == null)
            {
                return false;
            }

            var prefabs = assetTargets?.Cast<GameObject>()?.ToArray();
            var importerArray = importers.ToArray();

            // In tests assetTargets can become null
            for (int i = 0; i < Math.Min(importerArray.Length, prefabs?.Length ?? 0); ++i)
            {
                var im = importerArray[i];
                var defaultShader = im.defaultShader;
                var defaultBillboardShader = im.defaultBillboardShader;

                foreach (var mr in prefabs[i].transform.GetComponentsInChildren<MeshRenderer>())
                {
                    foreach (var mat in mr.sharedMaterials)
                    {
                        if (mat.shader != defaultShader)
                            return true;
                    }
                }

                if (defaultBillboardShader != null)
                {
                    foreach (var br in prefabs[i].transform.GetComponentsInChildren<BillboardRenderer>())
                    {
                        if (br.billboard.material.shader != defaultBillboardShader)
                            return true;
                    }
                }
            }

            return false;
        }


        public override void OnInspectorGUI()
        {
            // settings GUIs
            ShowMeshGUI();
            ShowMaterialGUI();
            ShowLightingGUI();
            ShowWindGUI();
            ShowLODGUI();

            EditorGUILayout.Space();

            bool materialsNeedToBeUpgraded = upgradeMaterials;
            doMaterialsHaveDifferentShader = !materialsNeedToBeUpgraded && DoMaterialsHaveDifferentShader();

            if (materialsNeedToBeUpgraded)
            {
                EditorGUILayout.HelpBox(
                    String.Format("SpeedTree materials need to be upgraded. Please back them up (if modified manually) then hit the \"{0}\" button below.", Styles.ApplyAndGenerate.text)
                    , MessageType.Warning
                );
            }

            if (doMaterialsHaveDifferentShader)
            {
                EditorGUILayout.HelpBox(
                    String.Format("There is a different SpeedTree shader provided by the current render pipeline which probably is more suitable for rendering. Hit the \"{0}\" button to regenerate the materials."
                        , (panelContainer as SpeedTreeImporterInspector).GetGenButtonText(HasModified()
                        , materialsNeedToBeUpgraded).text
                    )
                    , MessageType.Warning
                );
            }
        }

        private void ShowMeshGUI()
        {
            GUILayout.Label(Styles.MeshesHeader, EditorStyles.boldLabel);

            EditorGUILayout.Popup(m_UnitConversionEnumValue, Styles.UnitConversionNames, Styles.UnitConversion);

            bool bShowCustomScaleFactor = m_UnitConversionEnumValue.intValue == Styles.UnitConversionNames.Length-1;
            if (bShowCustomScaleFactor)
            {
                EditorGUILayout.PropertyField(m_ScaleFactor, Styles.ScaleFactor);
            }

            EditorGUILayout.Space();
        }
        public void ShowMaterialGUI()
        {
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_MainColor, Styles.MainColor);
            EditorGUILayout.PropertyField(m_EnableHueVariation, Styles.EnableColorVariation);
            if (importers.First().enableHueByDefault)
            {
                EditorGUILayout.PropertyField(m_HueVariation, Styles.HueVariation);
            }

            if (m_AllAreNotV8)
                EditorGUILayout.Slider(m_AlphaTestRef, 0f, 1f, Styles.AlphaTestRef);

            EditorGUILayout.PropertyField(m_EnableBumpMapping, Styles.EnableBump);
            EditorGUILayout.PropertyField(m_EnableSubsurfaceScattering, Styles.EnableSubsurface);

            EditorGUILayout.Space();
        }

        private void ShowLightingGUI()
        {
            GUILayout.Label(Styles.LightingHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_EnableShadowCasting, Styles.CastShadows);

            // from the docs page: https://docs.unity3d.com/Manual/SpeedTree.html
            // Known issues: As with any other renderer, the Receive Shadows option has no effect while using deferred rendering.
            // TODO: test and conditionally expose this field
            using (new EditorGUI.DisabledScope(!UnityEngine.Rendering.SupportedRenderingFeatures.active.receiveShadows))
            {
                EditorGUILayout.PropertyField(m_EnableShadowReceiving, Styles.ReceiveShadows);
            }

            EditorGUILayout.PropertyField(m_EnableLightProbeUsage, Styles.UseLightProbes);


            EditorGUILayout.Space();
        }


        private void ShowWindGUI()
        {
            GUILayout.Label(Styles.WindHeader, EditorStyles.boldLabel);
            int NumAvailableWindQualityOptions = 1 + m_HighestWindQuality.intValue; // 0 is None, we want at least 1 value
            ArraySegment<GUIContent> availableWindQualityOptions = new ArraySegment<GUIContent>(Styles.WindQualityNames, 0, NumAvailableWindQualityOptions);
            EditorGUILayout.Popup(m_SelectedWindQuality, availableWindQualityOptions.ToArray(), Styles.WindQuality);
            EditorGUILayout.Space();
        }

        private void ShowLODGUI()
        {
            m_ShowSmoothLODOptions.target = m_EnableSmoothLOD.hasMultipleDifferentValues || m_EnableSmoothLOD.boolValue;
            m_ShowCrossFadeWidthOptions.target = m_AnimateCrossFading.hasMultipleDifferentValues || !m_AnimateCrossFading.boolValue;

            // label
            GUILayout.Label(Styles.LODHeader, EditorStyles.boldLabel);

            // LOD Transitions
            {
                EditorGUILayout.PropertyField(m_EnableSmoothLOD, Styles.SmoothLOD);
                EditorGUI.indentLevel++;
                if (EditorGUILayout.BeginFadeGroup(m_ShowSmoothLODOptions.faded))
                {
                    EditorGUILayout.PropertyField(m_AnimateCrossFading, Styles.AnimateCrossFading);
                    if (EditorGUILayout.BeginFadeGroup(m_ShowCrossFadeWidthOptions.faded))
                    {
                        EditorGUILayout.Slider(m_BillboardTransitionCrossFadeWidth, 0.0f, 1.0f, Styles.CrossFadeWidth);
                        EditorGUILayout.Slider(m_FadeOutWidth, 0.0f, 1.0f, Styles.FadeOutWidth);
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

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
                        foreach (var importer in targets.Cast<SpeedTreeImporter>())
                        {
                            var menuText = String.Format("{0}: {1}",
                                Path.GetFileNameWithoutExtension(importer.assetPath),
                                String.Join(" | ", importer.LODHeights.Select(height => UnityString.Format("{0:0}%", height * 100)).ToArray()));
                            dropDownMenu.AddItem(new GUIContent(menuText), false, OnResetLODMenuClick, importer);
                        }
                        dropDownMenu.DropDown(buttonRect);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                var area = GUILayoutUtility.GetRect(0, LODGroupGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    LODGroupGUI.DrawMixedValueLODSlider(area);
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
                        var lodsLeft = lods.Where(lod => lod.ScreenPercent > 0.5f).OrderByDescending(x => x.LODLevel);
                        var lodsRight = lods.Where(lod => lod.ScreenPercent <= 0.5f).OrderBy(x => x.LODLevel);

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
                        m_LODSettings.GetArrayElementAtIndex(m_SelectedLODSlider).FindPropertyRelative("height").floatValue = lods[m_SelectedLODSlider].RawScreenPercent;
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
            for (int i = 0; i < m_LODSettings.arraySize; i++)
            {
                DrawLODGroupFoldout(camera, i, ref m_LODGroupFoldoutHeaderValues[i], lods);
            }
        }

        static private string GetLODSubmeshAndTriCountLabel(int numLODs, int lodGroupIndex, SpeedTreeImporter im, LODGroup lodGroup)
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

            int totalTriCount = (primitiveCounts.Length > 0 && primitiveCounts[lodGroupIndex] != null)
                ? primitiveCounts[lodGroupIndex].Sum()
                : 0;
            int lod0TriCount = primitiveCounts[0].Sum();
            var triCountChange = lod0TriCount != 0 ? (float)totalTriCount / lod0TriCount * 100 : 0;
            string triangleChangeLabel = lodGroupIndex > 0 && lod0TriCount != 0 ? $"({triCountChange.ToString("f2")}% LOD0)" : "";

            bool wideInspector = Screen.width >= 480;
            triangleChangeLabel = wideInspector ? triangleChangeLabel : "";
            string submeshCountLabel = wideInspector ? $"- {submeshCounts[lodGroupIndex]} Sub Mesh(es)" : "";

            return $"{totalTriCount} {LODGroupGUI.GUIStyles.m_TriangleCountLabel.text} {triangleChangeLabel} {submeshCountLabel}";
        }
        private void DrawLODGroupFoldout(Camera camera, int lodGroupIndex, ref SavedBool foldoutState, List<LODGroupGUI.LODInfo> lodInfoList)
        {
            GameObject[] prefabs = assetTargets?.Cast<GameObject>().ToArray(); // In tests assetTargets can become null
            SpeedTreeImporter[] importerArray = importers.ToArray();
            int numSelectedAssets = Math.Min(importerArray.Length, prefabs?.Length ?? 0);
            bool isDrawingSelectedLODGroup = m_SelectedLODRange == lodGroupIndex;

            // even though multiple assets may be selected, this code path
            // ensures the numLODs match for all the selected assets (see HasSameLODConfig() calls)
            int numLODs = m_LODSettings.arraySize;

            string LODFoldoutHeaderLabel = (importerArray[0].hasBillboard && lodGroupIndex == m_LODSettings.arraySize - 1)
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
            bool isBillboard = (lodIndex == lods.Count - 1) && importers.First().hasBillboard;
            int windQuality = m_HighestWindQuality.intValue;
            if (isBillboard)
            {
                windQuality = 1; // billboard has only one level of wind quality
            }


            SerializedProperty selectedLODProp = m_LODSettings.GetArrayElementAtIndex(lodIndex);
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
                EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
                EditorGUILayout.Popup(
                    selectedLODProp.FindPropertyRelative("windQuality"),
                    SpeedTreeImporter.windQualityNames.Take(windQuality + 1).Select(s => new GUIContent(s)).ToArray(),
                    Styles.WindQuality);


                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableHue"), Styles.EnableColorVariation);
                EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableBump"), Styles.EnableBump);

                if (m_AllAreV8)
                    EditorGUILayout.PropertyField(selectedLODProp.FindPropertyRelative("enableSubsurface"), Styles.EnableSubsurface);
            }
        }


        private void OnResetLODMenuClick(object userData)
        {
            var toHeights = (userData as SpeedTreeImporter).LODHeights;
            for (int i = 0; i < toHeights.Length; ++i)
                m_LODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue = toHeights[i];
        }
    }
}
