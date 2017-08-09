// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(QualitySettings))]
    internal class QualitySettingsEditor : ProjectSettingsBaseEditor
    {
        private static class Styles
        {
            public static readonly GUIStyle kToggle = "OL Toggle";
            public static readonly GUIStyle kDefaultToggle = "OL ToggleWhite";

            public static readonly GUIContent kPlatformTooltip = new GUIContent("", "Allow quality setting on platform");
            public static readonly GUIContent kAddQualityLevel = new GUIContent("Add Quality Level");

            public static readonly GUIContent kIconTrash = EditorGUIUtility.IconContent("TreeEditor.Trash", "|Delete Level");
            public static readonly GUIContent kSoftParticlesHint = EditorGUIUtility.TextContent("Soft Particles require using Deferred Lighting or making camera render the depth texture.");
            public static readonly GUIContent kBillboardsFaceCameraPos = EditorGUIUtility.TextContent("Billboards Face Camera Position|Make billboards face towards camera position. Otherwise they face towards camera plane. This makes billboards look nicer when camera rotates but is more expensive to render.");

            public static readonly GUIStyle kListEvenBg = "ObjectPickerResultsOdd";
            public static readonly GUIStyle kListOddBg = "ObjectPickerResultsEven";
            public static readonly GUIStyle kDefaultDropdown = "QualitySettingsDefault";

            public const int kMinToggleWidth = 15;
            public const int kMaxToggleWidth = 20;
            public const int kHeaderRowHeight = 20;
            public const int kLabelWidth = 80;
        }

        public const int kMinAsyncRingBufferSize = 2;
        public const int kMaxAsyncRingBufferSize = 512;
        public const int kMinAsyncUploadTimeSlice = 1;
        public const int kMaxAsyncUploadTimeSlice = 33;

        private SerializedObject m_QualitySettings;
        private SerializedProperty m_QualitySettingsProperty;
        private SerializedProperty m_PerPlatformDefaultQualityProperty;
        private List<BuildPlatform> m_ValidPlatforms;

        public void OnEnable()
        {
            m_QualitySettings = new SerializedObject(target);
            m_QualitySettingsProperty = m_QualitySettings.FindProperty("m_QualitySettings");
            m_PerPlatformDefaultQualityProperty = m_QualitySettings.FindProperty("m_PerPlatformDefaultQuality");
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();
        }

        private struct QualitySetting
        {
            public string m_Name;
            public string m_PropertyPath;
            public List<string> m_ExcludedPlatforms;
        }

        private readonly int m_QualityElementHash = "QualityElementHash".GetHashCode();
        private class Dragging
        {
            public int m_StartPosition;
            public int m_Position;
        }

        private Dragging m_Dragging;
        private bool m_ShouldAddNewLevel;
        private int m_DeleteLevel = -1;
        private int DoQualityLevelSelection(int currentQualitylevel, IList<QualitySetting> qualitySettings, Dictionary<string, int> platformDefaultQualitySettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            var selectedLevel = currentQualitylevel;

            //Header row
            GUILayout.BeginHorizontal();

            Rect header = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            header.x += EditorGUI.indent;
            header.width -= EditorGUI.indent;
            GUI.Label(header, "Levels", EditorStyles.boldLabel);

            //Header row icons
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));
                var temp = EditorGUIUtility.TempContent(platform.smallIcon);
                temp.tooltip = platform.title.text;
                GUI.Label(iconRect, temp);
                temp.tooltip = "";
            }

            //Extra column for deleting setting button
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));

            GUILayout.EndHorizontal();

            //Draw the row for each quality setting
            var currentEvent = Event.current;
            for (var i = 0; i < qualitySettings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                var bgStyle = i % 2 == 0 ? Styles.kListEvenBg : Styles.kListOddBg;
                bool selected = (selectedLevel == i);

                //Draw the selected icon if required
                Rect r = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth));

                switch (currentEvent.type)
                {
                    case EventType.Repaint:
                        bgStyle.Draw(r, GUIContent.none, false, false, selected, false);
                        GUI.Label(r, EditorGUIUtility.TempContent(qualitySettings[i].m_Name));
                        break;
                    case EventType.MouseDown:
                        if (r.Contains(currentEvent.mousePosition))
                        {
                            selectedLevel = i;
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = m_QualityElementHash;
                            GUI.changed = true;
                            m_Dragging = new Dragging {m_StartPosition = i, m_Position = i};
                            currentEvent.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            if (r.Contains(currentEvent.mousePosition))
                            {
                                m_Dragging.m_Position = i;
                                currentEvent.Use();
                            }
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                    case EventType.KeyDown:
                        if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.DownArrow)
                        {
                            selectedLevel += currentEvent.keyCode == KeyCode.UpArrow ? -1 : 1;
                            selectedLevel = Mathf.Clamp(selectedLevel, 0, qualitySettings.Count - 1);
                            GUIUtility.keyboardControl = 0;
                            GUI.changed = true;
                            currentEvent.Use();
                        }
                        break;
                }

                //Build a list of the current platform selection and draw it.
                foreach (var platform in m_ValidPlatforms)
                {
                    bool isDefaultQuality = false;
                    if (platformDefaultQualitySettings.ContainsKey(platform.name) &&  platformDefaultQualitySettings[platform.name] == i)
                        isDefaultQuality = true;

                    var toggleRect = GUILayoutUtility.GetRect(Styles.kPlatformTooltip, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                    if (Event.current.type == EventType.Repaint)
                    {
                        bgStyle.Draw(toggleRect, GUIContent.none, false, false, selected, false);
                    }

                    var color = GUI.backgroundColor;
                    if (isDefaultQuality && !EditorApplication.isPlayingOrWillChangePlaymode)
                        GUI.backgroundColor = Color.green;

                    var supported = !qualitySettings[i].m_ExcludedPlatforms.Contains(platform.name);
                    var newSupported = GUI.Toggle(toggleRect, supported, Styles.kPlatformTooltip, isDefaultQuality ? Styles.kDefaultToggle : Styles.kToggle);
                    if (supported != newSupported)
                    {
                        if (newSupported)
                            qualitySettings[i].m_ExcludedPlatforms.Remove(platform.name);
                        else
                            qualitySettings[i].m_ExcludedPlatforms.Add(platform.name);
                    }

                    GUI.backgroundColor = color;
                }

                //Extra column for deleting quality button
                var deleteButton = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                if (Event.current.type == EventType.Repaint)
                {
                    bgStyle.Draw(deleteButton, GUIContent.none, false, false, selected, false);
                }
                if (GUI.Button(deleteButton, Styles.kIconTrash, GUIStyle.none))
                    m_DeleteLevel = i;
                GUILayout.EndHorizontal();
            }

            //Add a spacer line to separate the levels from the defaults
            GUILayout.BeginHorizontal();
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle,
                GUILayout.MinWidth(Styles.kMinToggleWidth),
                GUILayout.MaxWidth(Styles.kMaxToggleWidth),
                GUILayout.Height(1));

            DrawHorizontalDivider();
            GUILayout.EndHorizontal();

            //Default platform selection dropdowns
            GUILayout.BeginHorizontal();

            var defaultQualityTitle = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            defaultQualityTitle.x += EditorGUI.indent;
            defaultQualityTitle.width -= EditorGUI.indent;
            GUI.Label(defaultQualityTitle, "Default", EditorStyles.boldLabel);

            // Draw default dropdown arrows
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle,
                        GUILayout.MinWidth(Styles.kMinToggleWidth),
                        GUILayout.MaxWidth(Styles.kMaxToggleWidth),
                        GUILayout.Height(Styles.kHeaderRowHeight));

                int position;
                if (!platformDefaultQualitySettings.TryGetValue(platform.name, out position))
                    platformDefaultQualitySettings.Add(platform.name, 0);

                position = EditorGUI.Popup(iconRect, position, qualitySettings.Select(x => x.m_Name).ToArray(), Styles.kDefaultDropdown);
                platformDefaultQualitySettings[platform.name] = position;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //Add an extra row for 'Add' button
            GUILayout.BeginHorizontal();

            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle,
                GUILayout.MinWidth(Styles.kMinToggleWidth),
                GUILayout.MaxWidth(Styles.kMaxToggleWidth),
                GUILayout.Height(Styles.kHeaderRowHeight));

            var addButtonRect = GUILayoutUtility.GetRect(Styles.kAddQualityLevel, Styles.kToggle, GUILayout.ExpandWidth(true));

            if (GUI.Button(addButtonRect, Styles.kAddQualityLevel))
                m_ShouldAddNewLevel = true;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return selectedLevel;
        }

        private List<QualitySetting> GetQualitySettings()
        {
            // Pull the quality settings from the runtime.
            var qualitySettings = new List<QualitySetting>();

            foreach (SerializedProperty prop in m_QualitySettingsProperty)
            {
                var qs = new QualitySetting
                {
                    m_Name = prop.FindPropertyRelative("name").stringValue,
                    m_PropertyPath = prop.propertyPath
                };

                qs.m_PropertyPath = prop.propertyPath;

                var platforms = new List<string>();
                var platformsProp = prop.FindPropertyRelative("excludedTargetPlatforms");
                foreach (SerializedProperty platformProp in platformsProp)
                    platforms.Add(platformProp.stringValue);

                qs.m_ExcludedPlatforms = platforms;
                qualitySettings.Add(qs);
            }
            return qualitySettings;
        }

        private void SetQualitySettings(IEnumerable<QualitySetting> settings)
        {
            foreach (var setting in settings)
            {
                var property = m_QualitySettings.FindProperty(setting.m_PropertyPath);
                if (property == null)
                    continue;

                var platformsProp = property.FindPropertyRelative("excludedTargetPlatforms");
                if (platformsProp.arraySize != setting.m_ExcludedPlatforms.Count)
                    platformsProp.arraySize = setting.m_ExcludedPlatforms.Count;

                var count = 0;
                foreach (SerializedProperty platform in platformsProp)
                {
                    if (platform.stringValue != setting.m_ExcludedPlatforms[count])
                        platform.stringValue = setting.m_ExcludedPlatforms[count];
                    count++;
                }
            }
        }

        private void HandleAddRemoveQualitySetting(ref int selectedLevel, Dictionary<string, int> platformDefaults)
        {
            if (m_DeleteLevel >= 0)
            {
                if (m_DeleteLevel < selectedLevel || m_DeleteLevel == m_QualitySettingsProperty.arraySize - 1)
                    selectedLevel = Mathf.Max(0, selectedLevel - 1);

                //Always ensure there is one quality setting
                if (m_QualitySettingsProperty.arraySize > 1 && m_DeleteLevel >= 0 && m_DeleteLevel < m_QualitySettingsProperty.arraySize)
                {
                    m_QualitySettingsProperty.DeleteArrayElementAtIndex(m_DeleteLevel);

                    // Fix defaults offset
                    List<string> keys = new List<string>(platformDefaults.Keys);
                    foreach (var key in keys)
                    {
                        int value = platformDefaults[key];
                        if (value != 0 && value >= m_DeleteLevel)
                            platformDefaults[key]--;
                    }
                }

                m_DeleteLevel = -1;
            }

            if (m_ShouldAddNewLevel)
            {
                m_QualitySettingsProperty.arraySize++;
                var addedSetting = m_QualitySettingsProperty.GetArrayElementAtIndex(m_QualitySettingsProperty.arraySize - 1);
                var nameProperty = addedSetting.FindPropertyRelative("name");
                nameProperty.stringValue = "Level " + (m_QualitySettingsProperty.arraySize - 1);

                m_ShouldAddNewLevel = false;
            }
        }

        private Dictionary<string, int> GetDefaultQualityForPlatforms()
        {
            var defaultPlatformQualities = new Dictionary<string, int>();

            foreach (SerializedProperty prop in m_PerPlatformDefaultQualityProperty)
            {
                defaultPlatformQualities.Add(prop.FindPropertyRelative("first").stringValue, prop.FindPropertyRelative("second").intValue);
            }
            return defaultPlatformQualities;
        }

        private void SetDefaultQualityForPlatforms(Dictionary<string, int> platformDefaults)
        {
            if (m_PerPlatformDefaultQualityProperty.arraySize != platformDefaults.Count)
                m_PerPlatformDefaultQualityProperty.arraySize = platformDefaults.Count;

            var count = 0;
            foreach (var def in platformDefaults)
            {
                var element = m_PerPlatformDefaultQualityProperty.GetArrayElementAtIndex(count);
                var firstProperty = element.FindPropertyRelative("first");
                var secondProperty = element.FindPropertyRelative("second");

                if (firstProperty.stringValue != def.Key || secondProperty.intValue != def.Value)
                {
                    firstProperty.stringValue = def.Key;
                    secondProperty.intValue = def.Value;
                }
                count++;
            }
        }

        private static void DrawHorizontalDivider()
        {
            var spacerLine = GUILayoutUtility.GetRect(GUIContent.none,
                    GUIStyle.none,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(1));
            var oldBgColor = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
                GUI.backgroundColor = oldBgColor * 0.7058f;
            else
                GUI.backgroundColor = Color.black;

            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.whiteTextureStyle.Draw(spacerLine, GUIContent.none, false, false, false, false);

            GUI.backgroundColor = oldBgColor;
        }

        void SoftParticlesHintGUI()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RenderingPath renderPath = mainCamera.actualRenderingPath;
            if (renderPath == RenderingPath.DeferredLighting || renderPath == RenderingPath.DeferredShading)
                return; // using deferred, all is good

            if ((mainCamera.depthTextureMode & DepthTextureMode.Depth) != 0)
                return; // already produces depth texture, all is good

            EditorGUILayout.HelpBox(Styles.kSoftParticlesHint.text, MessageType.Warning, false);
        }

        /**
         * Internal function that takes the shadow cascade splits property field, and dispatches a call to render the GUI.
         * It also transfers the result back
         */
        private void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;

            System.Type type = typeof(T);
            if (type == typeof(float))
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[] {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Changes made in play mode will not be saved.", MessageType.Warning, true);
            }

            m_QualitySettings.Update();

            var settings = GetQualitySettings();
            var defaults = GetDefaultQualityForPlatforms();
            var selectedLevel = QualitySettings.GetQualityLevel();

            EditorGUI.BeginChangeCheck();
            selectedLevel = DoQualityLevelSelection(selectedLevel, settings, defaults);
            if (EditorGUI.EndChangeCheck())
                QualitySettings.SetQualityLevel(selectedLevel);

            SetQualitySettings(settings);
            HandleAddRemoveQualitySetting(ref selectedLevel, defaults);
            SetDefaultQualityForPlatforms(defaults);
            GUILayout.Space(10.0f);
            DrawHorizontalDivider();
            GUILayout.Space(10.0f);

            var currentSettings = m_QualitySettingsProperty.GetArrayElementAtIndex(selectedLevel);
            var nameProperty = currentSettings.FindPropertyRelative("name");
            var pixelLightCountProperty = currentSettings.FindPropertyRelative("pixelLightCount");
            var shadowsProperty = currentSettings.FindPropertyRelative("shadows");
            var shadowResolutionProperty = currentSettings.FindPropertyRelative("shadowResolution");
            var shadowProjectionProperty = currentSettings.FindPropertyRelative("shadowProjection");
            var shadowCascadesProperty = currentSettings.FindPropertyRelative("shadowCascades");
            var shadowDistanceProperty = currentSettings.FindPropertyRelative("shadowDistance");
            var shadowNearPlaneOffsetProperty = currentSettings.FindPropertyRelative("shadowNearPlaneOffset");
            var shadowCascade2SplitProperty = currentSettings.FindPropertyRelative("shadowCascade2Split");
            var shadowCascade4SplitProperty = currentSettings.FindPropertyRelative("shadowCascade4Split");
            var shadowMaskUsageProperty = currentSettings.FindPropertyRelative("shadowmaskMode");
            var blendWeightsProperty = currentSettings.FindPropertyRelative("blendWeights");
            var textureQualityProperty = currentSettings.FindPropertyRelative("textureQuality");
            var anisotropicTexturesProperty = currentSettings.FindPropertyRelative("anisotropicTextures");
            var antiAliasingProperty = currentSettings.FindPropertyRelative("antiAliasing");
            var softParticlesProperty = currentSettings.FindPropertyRelative("softParticles");
            var realtimeReflectionProbes = currentSettings.FindPropertyRelative("realtimeReflectionProbes");
            var billboardsFaceCameraPosition = currentSettings.FindPropertyRelative("billboardsFaceCameraPosition");
            var vSyncCountProperty = currentSettings.FindPropertyRelative("vSyncCount");
            var lodBiasProperty = currentSettings.FindPropertyRelative("lodBias");
            var maximumLODLevelProperty = currentSettings.FindPropertyRelative("maximumLODLevel");
            var particleRaycastBudgetProperty = currentSettings.FindPropertyRelative("particleRaycastBudget");
            var asyncUploadTimeSliceProperty = currentSettings.FindPropertyRelative("asyncUploadTimeSlice");
            var asyncUploadBufferSizeProperty = currentSettings.FindPropertyRelative("asyncUploadBufferSize");
            var resolutionScalingFixedDPIFactorProperty = currentSettings.FindPropertyRelative("resolutionScalingFixedDPIFactor");

            bool usingSRP = GraphicsSettings.renderPipelineAsset != null;

            if (string.IsNullOrEmpty(nameProperty.stringValue))
                nameProperty.stringValue = "Level " + selectedLevel;

            EditorGUILayout.PropertyField(nameProperty);

            if (usingSRP)
                EditorGUILayout.HelpBox("A Scriptable Render Pipeline is in use, some settings will not be used and are hidden", MessageType.Info);

            GUILayout.Space(10);

            GUILayout.Label(EditorGUIUtility.TempContent("Rendering"), EditorStyles.boldLabel);
            if (!usingSRP)
                EditorGUILayout.PropertyField(pixelLightCountProperty);

            // still valid with SRP
            EditorGUILayout.PropertyField(textureQualityProperty);
            EditorGUILayout.PropertyField(anisotropicTexturesProperty);

            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(antiAliasingProperty);
                EditorGUILayout.PropertyField(softParticlesProperty);
                if (softParticlesProperty.boolValue)
                    SoftParticlesHintGUI();
            }

            EditorGUILayout.PropertyField(realtimeReflectionProbes);
            EditorGUILayout.PropertyField(billboardsFaceCameraPosition, Styles.kBillboardsFaceCameraPos);
            EditorGUILayout.PropertyField(resolutionScalingFixedDPIFactorProperty);
            GUILayout.Space(10);

            if (!usingSRP)
            {
                GUILayout.Label(EditorGUIUtility.TempContent("Shadows"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(shadowsProperty);
                EditorGUILayout.PropertyField(shadowResolutionProperty);
                EditorGUILayout.PropertyField(shadowProjectionProperty);
                EditorGUILayout.PropertyField(shadowDistanceProperty);
                EditorGUILayout.PropertyField(shadowMaskUsageProperty);
                EditorGUILayout.PropertyField(shadowNearPlaneOffsetProperty);
                EditorGUILayout.PropertyField(shadowCascadesProperty);

                if (shadowCascadesProperty.intValue == 2)
                    DrawCascadeSplitGUI<float>(ref shadowCascade2SplitProperty);
                else if (shadowCascadesProperty.intValue == 4)
                    DrawCascadeSplitGUI<Vector3>(ref shadowCascade4SplitProperty);

                GUILayout.Space(10);
            }

            GUILayout.Label(EditorGUIUtility.TempContent("Other"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blendWeightsProperty);
            EditorGUILayout.PropertyField(vSyncCountProperty);
            EditorGUILayout.PropertyField(lodBiasProperty);
            EditorGUILayout.PropertyField(maximumLODLevelProperty);
            EditorGUILayout.PropertyField(particleRaycastBudgetProperty);
            EditorGUILayout.PropertyField(asyncUploadTimeSliceProperty);
            EditorGUILayout.PropertyField(asyncUploadBufferSizeProperty);

            asyncUploadTimeSliceProperty.intValue = Mathf.Clamp(asyncUploadTimeSliceProperty.intValue, kMinAsyncUploadTimeSlice, kMaxAsyncUploadTimeSlice);
            asyncUploadBufferSizeProperty.intValue = Mathf.Clamp(asyncUploadBufferSizeProperty.intValue, kMinAsyncRingBufferSize, kMaxAsyncRingBufferSize);


            if (m_Dragging != null && m_Dragging.m_Position != m_Dragging.m_StartPosition)
            {
                m_QualitySettingsProperty.MoveArrayElement(m_Dragging.m_StartPosition, m_Dragging.m_Position);
                m_Dragging.m_StartPosition = m_Dragging.m_Position;
                selectedLevel = m_Dragging.m_Position;

                m_QualitySettings.ApplyModifiedProperties();
                QualitySettings.SetQualityLevel(Mathf.Clamp(selectedLevel, 0, m_QualitySettingsProperty.arraySize - 1));
            }

            m_QualitySettings.ApplyModifiedProperties();
        }
    }
}
