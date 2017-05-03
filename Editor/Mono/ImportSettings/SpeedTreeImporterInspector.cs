// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    [CustomEditor(typeof(SpeedTreeImporter))]
    [CanEditMultipleObjects]
    internal class SpeedTreeImporterInspector : AssetImporterEditor
    {
        private class Styles
        {
            public static GUIContent LODHeader          = EditorGUIUtility.TextContent("LODs");
            public static GUIContent ResetLOD           = EditorGUIUtility.TextContent("Reset LOD to...|Unify the LOD settings for all selected assets.");
            public static GUIContent SmoothLOD          = EditorGUIUtility.TextContent("Smooth LOD|Toggles smooth LOD transitions.");
            public static GUIContent AnimateCrossFading = EditorGUIUtility.TextContent("Animate Cross-fading|Cross-fading is animated instead of being calculated by distance.");
            public static GUIContent CrossFadeWidth     = EditorGUIUtility.TextContent("Crossfade Width|Proportion of the last 3D mesh LOD region width which is used for cross-fading to billboard tree.");
            public static GUIContent FadeOutWidth       = EditorGUIUtility.TextContent("Fade Out Width|Proportion of the billboard LOD region width which is used for fading out the billboard.");

            public static GUIContent MeshesHeader       = EditorGUIUtility.TextContent("Meshes");
            public static GUIContent ScaleFactor        = EditorGUIUtility.TextContent("Scale Factor|How much to scale the tree model compared to what is in the .spm file.");
            public static GUIContent ScaleFactorHelp    = EditorGUIUtility.TextContent("The default value of Scale Factor is 0.3048, the conversion ratio from feet to meters, as these are the most conventional measurements used in SpeedTree and Unity, respectively.");

            public static GUIContent MaterialsHeader    = EditorGUIUtility.TextContent("Materials");
            public static GUIContent MainColor          = EditorGUIUtility.TextContent("Main Color|The color modulating the diffuse lighting component.");
            public static GUIContent HueVariation       = EditorGUIUtility.TextContent("Hue Color|Apply to LODs that have Hue Variation effect enabled.");
            public static GUIContent AlphaTestRef       = EditorGUIUtility.TextContent("Alpha Cutoff|The alpha-test reference value.");
            public static GUIContent CastShadows        = EditorGUIUtility.TextContent("Cast Shadows|The tree casts shadow.");
            public static GUIContent ReceiveShadows     = EditorGUIUtility.TextContent("Receive Shadows|The tree receives shadow.");
            public static GUIContent UseLightProbes     = EditorGUIUtility.TextContent("Use Light Probes|The tree uses light probe for lighting.");
            public static GUIContent UseReflectionProbes = EditorGUIUtility.TextContent("Use Reflection Probes|The tree uses reflection probe for rendering.");
            public static GUIContent EnableBump         = EditorGUIUtility.TextContent("Normal Map|Enable normal mapping (aka Bump mapping).");
            public static GUIContent EnableHue          = EditorGUIUtility.TextContent("Enable Hue Variation|Enable Hue variation color (color is adjusted between Main Color and Hue Color).");
            public static GUIContent WindQuality        = EditorGUIUtility.TextContent("Wind Quality|Controls the wind quality.");

            public static GUIContent ApplyAndGenerate   = EditorGUIUtility.TextContent("Apply & Generate Materials|Apply current importer settings and generate materials with new settings.");
            public static GUIContent Regenerate         = EditorGUIUtility.TextContent("Regenerate Materials|Regenerate materials from the current importer settings.");
        };

        private SerializedProperty m_LODSettings;
        private SerializedProperty m_EnableSmoothLOD;
        private SerializedProperty m_AnimateCrossFading;
        private SerializedProperty m_BillboardTransitionCrossFadeWidth;
        private SerializedProperty m_FadeOutWidth;
        private SerializedProperty m_MainColor;
        private SerializedProperty m_HueVariation;
        private SerializedProperty m_AlphaTestRef;
        private SerializedProperty m_ScaleFactor;

        private const float kFeetToMetersRatio = 0.3048f;

        // LODGroup GUI
        private int m_SelectedLODSlider = -1;
        private int m_SelectedLODRange = 0;

        private readonly AnimBool m_ShowSmoothLODOptions = new AnimBool();
        private readonly AnimBool m_ShowCrossFadeWidthOptions = new AnimBool();

        public override void OnEnable()
        {
            m_LODSettings = serializedObject.FindProperty("m_LODSettings");
            m_EnableSmoothLOD = serializedObject.FindProperty("m_EnableSmoothLODTransition");
            m_AnimateCrossFading = serializedObject.FindProperty("m_AnimateCrossFading");
            m_BillboardTransitionCrossFadeWidth = serializedObject.FindProperty("m_BillboardTransitionCrossFadeWidth");
            m_FadeOutWidth = serializedObject.FindProperty("m_FadeOutWidth");
            m_MainColor = serializedObject.FindProperty("m_MainColor");
            m_HueVariation = serializedObject.FindProperty("m_HueVariation");
            m_AlphaTestRef = serializedObject.FindProperty("m_AlphaTestRef");
            m_ScaleFactor = serializedObject.FindProperty("m_ScaleFactor");

            m_ShowSmoothLODOptions.value = m_EnableSmoothLOD.hasMultipleDifferentValues || m_EnableSmoothLOD.boolValue;
            m_ShowSmoothLODOptions.valueChanged.AddListener(Repaint);
            m_ShowCrossFadeWidthOptions.value = m_AnimateCrossFading.hasMultipleDifferentValues || !m_AnimateCrossFading.boolValue;
            m_ShowCrossFadeWidthOptions.valueChanged.AddListener(Repaint);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowSmoothLODOptions.valueChanged.RemoveListener(Repaint);
            m_ShowCrossFadeWidthOptions.valueChanged.RemoveListener(Repaint);
        }

        private SpeedTreeImporter[] importers
        {
            get { return targets.Cast<SpeedTreeImporter>().ToArray(); }
        }

        private bool upgradeMaterials
        {
            get { return importers.Any(i => i.materialsShouldBeRegenerated); }
        }

        protected override bool OnApplyRevertGUI()
        {
            bool applied;
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                RevertButton();
                applied = ApplyButton("Apply Prefab");
            }

            bool upgrade = upgradeMaterials;
            GUIContent genButtonText = HasModified() || upgrade ? Styles.ApplyAndGenerate : Styles.Regenerate;

            if (GUILayout.Button(genButtonText))
            {
                bool hasModified = HasModified();

                // Apply the changes and generate the materials before importing so that asset previews are up-to-date.
                if (hasModified)
                    Apply();

                if (upgrade)
                {
                    foreach (var importer in importers)
                        importer.SetMaterialVersionToCurrent();
                }

                GenerateMaterials();

                if (hasModified || upgrade)
                {
                    ApplyAndImport();
                    applied = true;
                }
            }

            return applied;
        }

        private void GenerateMaterials()
        {
            string[] matFolders = importers.Select(im => im.materialFolderPath).ToArray();
            string[] guids = AssetDatabase.FindAssets("t:Material", matFolders);
            string[] paths = guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

            bool doGenerate = true;
            if (paths.Length > 0)
                doGenerate = Provider.PromptAndCheckoutIfNeeded(paths, String.Format("Materials will be checked out in:\n{0}", String.Join("\n", matFolders)));

            if (doGenerate)
            {
                foreach (var importer in importers)
                    importer.GenerateMaterials();
            }
        }

        internal List<LODGroupGUI.LODInfo> GetLODInfoArray(Rect area)
        {
            int lodCount = m_LODSettings.arraySize;
            return LODGroupGUI.CreateLODInfos(
                lodCount, area,
                i => i == lodCount - 1 && (target as SpeedTreeImporter).hasBillboard ? "Billboard" : String.Format("LOD {0}", i),
                i => m_LODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue);
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

        public override void OnInspectorGUI()
        {
            ShowMeshGUI();
            ShowMaterialGUI();
            ShowLODGUI();

            EditorGUILayout.Space();

            if (upgradeMaterials)
                EditorGUILayout.HelpBox(String.Format("SpeedTree materials need to be upgraded. Please back them up (if modified manually) then hit the \"{0}\" button below.", Styles.ApplyAndGenerate.text), MessageType.Warning);

            ApplyRevertGUI();
        }

        private void ShowMeshGUI()
        {
            GUILayout.Label(Styles.MeshesHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_ScaleFactor, Styles.ScaleFactor);

            // Display a help box to explain the rationale for the default value (feet to meters conversion ratio).
            if (!m_ScaleFactor.hasMultipleDifferentValues && Mathf.Approximately(m_ScaleFactor.floatValue, kFeetToMetersRatio))
            {
                EditorGUILayout.HelpBox(Styles.ScaleFactorHelp.text, MessageType.Info);
            }
        }

        private void ShowMaterialGUI()
        {
            GUILayout.Label(Styles.MaterialsHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_MainColor, Styles.MainColor);
            EditorGUILayout.PropertyField(m_HueVariation, Styles.HueVariation);
            EditorGUILayout.Slider(m_AlphaTestRef, 0f, 1f, Styles.AlphaTestRef);
        }

        private void ShowLODGUI()
        {
            m_ShowSmoothLODOptions.target = m_EnableSmoothLOD.hasMultipleDifferentValues || m_EnableSmoothLOD.boolValue;
            m_ShowCrossFadeWidthOptions.target = m_AnimateCrossFading.hasMultipleDifferentValues || !m_AnimateCrossFading.boolValue;

            GUILayout.Label(Styles.LODHeader, EditorStyles.boldLabel);

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

            EditorGUILayout.Space();
            if (HasSameLODConfig())
            {
                EditorGUILayout.Space();

                var area = GUILayoutUtility.GetRect(0, LODGroupGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));
                var lods = GetLODInfoArray(area);
                DrawLODLevelSlider(area, lods);

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (m_SelectedLODRange != -1 && lods.Count > 0)
                {
                    EditorGUILayout.LabelField(lods[m_SelectedLODRange].LODName + " Options", EditorStyles.boldLabel);
                    bool isBillboard = (m_SelectedLODRange == lods.Count - 1) && importers[0].hasBillboard;

                    EditorGUILayout.PropertyField(m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("castShadows"), Styles.CastShadows);
                    EditorGUILayout.PropertyField(m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("receiveShadows"), Styles.ReceiveShadows);

                    var useLightProbes = m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("useLightProbes");
                    EditorGUILayout.PropertyField(useLightProbes, Styles.UseLightProbes);
                    if (!useLightProbes.hasMultipleDifferentValues && useLightProbes.boolValue && isBillboard)
                        EditorGUILayout.HelpBox("Enabling Light Probe for billboards breaks batched rendering and may cause performance problem.", MessageType.Warning);

                    // TODO: reflection probe support when PBS is implemented
                    //EditorGUILayout.PropertyField(m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("useReflectionProbes"), Styles.UseReflectionProbes);

                    EditorGUILayout.PropertyField(m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("enableBump"), Styles.EnableBump);
                    EditorGUILayout.PropertyField(m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("enableHue"), Styles.EnableHue);

                    int bestWindQuality = importers.Min(im => im.bestWindQuality);
                    if (bestWindQuality > 0)
                    {
                        if (isBillboard)
                            bestWindQuality = bestWindQuality >= 1 ? 1 : 0; // billboard has only one level of wind quality
                        EditorGUILayout.Popup(
                            m_LODSettings.GetArrayElementAtIndex(m_SelectedLODRange).FindPropertyRelative("windQuality"),
                            SpeedTreeImporter.windQualityNames.Take(bestWindQuality + 1).Select(s => new GUIContent(s)).ToArray(),
                            Styles.WindQuality);
                    }
                }
            }
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
                                    String.Join(" | ", importer.LODHeights.Select(height => String.Format("{0:0}%", height * 100)).ToArray()));
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

        private void OnResetLODMenuClick(object userData)
        {
            var toHeights = (userData as SpeedTreeImporter).LODHeights;
            for (int i = 0; i < toHeights.Length; ++i)
                m_LODSettings.GetArrayElementAtIndex(i).FindPropertyRelative("height").floatValue = toHeights[i];
        }
    }
}
