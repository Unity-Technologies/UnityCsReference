// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Settings;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEditorInternal;
using UnityEditor.Rendering;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using HelpBox = UnityEngine.UIElements.HelpBox;

namespace UnityEditor
{
    [CustomEditor(typeof(GraphicsSettings))]
    internal class GraphicsSettingsInspector : ProjectSettingsBaseEditor
    {
        internal static class GraphicsSettingsData
        {
            internal const string builtIn = "Built-In";
            internal const string bodyTemplateBuiltInOnly = "UXML/ProjectSettings/GraphicsSettingsEditor-Builtin.uxml";
            internal const string bodyTemplateSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-SRP.uxml";
            internal const string helpBoxesTemplateForSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-HelpBoxes.uxml";
            internal const string builtInTabContent = "UXML/ProjectSettings/GraphicsSettingsEditor-BuiltInTab.uxml";

            internal static GUIContent builtInWarningText =
                EditorGUIUtility.TrTextContent("A Scriptable Render Pipeline is in use. Settings in the Built-In Render Pipeline are not currently in use.");
            internal static readonly string k_BuildProfileGraphicsSettingsOverrideWarning =
                L10n.Tr("The current active build profile has overridden certain Graphics settings. To ensure that the correct settings are included in your build, see the Build Profiles...");
        }
        internal IEnumerable<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> globalSettings => m_GlobalSettings;

        readonly VisibilityControllerBasedOnRenderPipeline m_VisibilityController = new();
        TabbedView m_TabbedView;
        VisualElement m_CurrentRoot;
        ScrollView m_ScrollView;
        List<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> m_GlobalSettings;
        HelpBox m_BuildProfileGraphicsSettingsOverrideWarning;
        internal static Action OnActiveProfileGraphicsSettingsChanged;

        readonly Dictionary<VisualElement, List<Label>> m_Labels = new();
        string m_CurrentText;

        bool m_FinishedInitialization;
        int m_LastListsHash;
        int m_GeometryChangedEventCounter;

        // As we use multiple IMGUI container while porting everything to UITK we will call serializedObject.Update in first separate IMGUI container.
        // This way we don't need to do it in each following containers.
        VisualElement ObjectUpdater()
        {
            return new IMGUIContainer(() => serializedObject.Update());
        }

        internal void Reload(bool globalSettingsExist, List<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> globalSettingsContainers)
        {
            var newHash = GraphicsSettingsInspectorUtility.ComputeRenderPipelineGlobalSettingsListHash(globalSettingsContainers);
            if (!m_FinishedInitialization || m_LastListsHash == newHash)
                return;

            Dispose();
            Create(m_CurrentRoot, globalSettingsExist, globalSettingsContainers);

            m_LastListsHash = newHash;
        }

        internal void Create(VisualElement root, bool globalSettingsExist, List<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> globalSettingsContainers)
        {
            m_VisibilityController.Initialize();
            Undo.undoRedoEvent += OnUndoRedoPerformed;

            m_CurrentRoot = root;
            m_CurrentRoot.Add(ObjectUpdater());
            m_GlobalSettings = globalSettingsContainers;

            var visualTreeAsset = EditorGUIUtility.Load(globalSettingsExist ? GraphicsSettingsData.bodyTemplateSRP : GraphicsSettingsData.bodyTemplateBuiltInOnly) as VisualTreeAsset;
            var content = visualTreeAsset.Instantiate();
            root.Add(content);

            Setup(globalSettingsExist);

            m_LastListsHash = GraphicsSettingsInspectorUtility.ComputeRenderPipelineGlobalSettingsListHash(globalSettingsContainers);
        }

        internal void Dispose()
        {
            UserSettings.ToConfig(m_CurrentRoot);

            m_Labels.Clear();
            m_CurrentRoot.Clear();

            m_VisibilityController.Clear();
            m_VisibilityController.Dispose();

            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            OnActiveProfileGraphicsSettingsChanged -= UpdateBuildProfileGraphicsSettingsOverrideWarning;
            m_FinishedInitialization = false;
        }

        void Setup(bool globalSettingsExist)
        {
            m_VisibilityController.Clear();

            m_CurrentRoot
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            m_BuildProfileGraphicsSettingsOverrideWarning = m_CurrentRoot.Query<HelpBox>("build-profile-override-warning-help-box");
            m_BuildProfileGraphicsSettingsOverrideWarning.text = GraphicsSettingsData.k_BuildProfileGraphicsSettingsOverrideWarning;
            UpdateBuildProfileGraphicsSettingsOverrideWarning();
            OnActiveProfileGraphicsSettingsChanged += UpdateBuildProfileGraphicsSettingsOverrideWarning;

            BindEnumFieldWithFadeGroup(m_CurrentRoot, "Lightmap", ShaderUtil.CalculateLightmapStrippingFromCurrentScene);
            BindEnumFieldWithFadeGroup(m_CurrentRoot, "Fog", ShaderUtil.CalculateFogStrippingFromCurrentScene);
            BindEnumFieldToLightProbe(m_CurrentRoot);

            BindShaderPreload(m_CurrentRoot);

            if (globalSettingsExist)
            {
                m_TabbedView = m_CurrentRoot.MandatoryQ<TabbedView>("PipelineSpecificSettings");
                GenerateTabs();
            }
            else
            {
                m_CurrentRoot.Query<GraphicsSettingsElement>()
                    .ForEach(e =>
                    {
                        if (e.BuiltinOnly)
                            m_VisibilityController.RegisterVisualElement(e, null);
                    });
                m_CurrentRoot.Query<BuiltInShaderElement>().ForEach(e => m_VisibilityController.RegisterVisualElement(e, null));
                SetupTransparencySortMode(m_CurrentRoot);
            }

            GraphicsSettingsInspectorUtility.LocalizeVisualTree(m_CurrentRoot);

            // Register a callback on the Geometry Change event of the content container.It will be called when the size of all children will be known.
            m_ScrollView = m_CurrentRoot.Q<ScrollView>("MainScrollView");
            m_ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnMainScrollViewGeometryChanged);

            m_CurrentRoot.Bind(serializedObject);
        }

        void BindShaderPreload(VisualElement root)
        {
            var shaderPreloadProperty = serializedObject.FindProperty("m_PreloadedShaders");
            shaderPreloadProperty.isExpanded = false;

            var shaderPreloadPropertyField = root.MandatoryQ<IMGUIContainer>("PreloadedShaders");
            shaderPreloadPropertyField.onGUIHandler = () =>
            {
                //for some reason, converting the display of this native array to UITK make MacOS crash when domain reload after user add a new script
                EditorGUILayout.PropertyField(shaderPreloadProperty, EditorGUIUtility.TrTextContent("Preload Shaders"));
            };
            
            var delayedShaderTimeLimitProperty = serializedObject.FindProperty("m_PreloadShadersBatchTimeLimit");
            var shaderPreloadToggle = root.MandatoryQ<Toggle>("ShaderPreloadToggle");
            var delayedShaderTimeLimitGroup = root.MandatoryQ<VisualElement>("DelayedShaderTimeLimitGroup");
            var delayedShaderTimeLimit = root.MandatoryQ<IntegerField>("DelayedShaderTimeLimit");
            shaderPreloadToggle.RegisterValueChangedCallback(evt => {
                delayedShaderTimeLimitGroup.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                var newVal = evt.newValue ? delayedShaderTimeLimit.value : -1;
                if (delayedShaderTimeLimitProperty.intValue != newVal)
                {
                    delayedShaderTimeLimitProperty.intValue = newVal;
                    delayedShaderTimeLimitProperty.serializedObject.ApplyModifiedProperties();
                }
            });
            delayedShaderTimeLimit.RegisterValueChangedCallback(evt =>
            {
                if (delayedShaderTimeLimitProperty.intValue != evt.newValue)
                {
                    delayedShaderTimeLimitProperty.intValue = evt.newValue;
                    delayedShaderTimeLimitProperty.serializedObject.ApplyModifiedProperties();
                }
            });
            shaderPreloadToggle.SetValueWithoutNotify(delayedShaderTimeLimitProperty.intValue >= 0);
            delayedShaderTimeLimit.SetValueWithoutNotify(Mathf.Max(0, delayedShaderTimeLimitProperty.intValue));
            delayedShaderTimeLimitGroup.style.display = delayedShaderTimeLimitProperty.intValue >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            
            var shaderTracking = root.MandatoryQ<HelpBox>("ShaderTrackingInfoBox");
            shaderTracking.schedule.Execute(() =>
                shaderTracking.text =
                    $"Currently tracked: {ShaderUtil.GetCurrentShaderVariantCollectionShaderCount()} shaders {ShaderUtil.GetCurrentShaderVariantCollectionVariantCount()} total variants").Every(500);

            var saveButton = root.MandatoryQ<Button>("SaveShaderVariants");
            saveButton.clickable = new Clickable(() =>
            {
                var assetPath = EditorUtility.SaveFilePanelInProject(
                    L10n.Tr("Save Shader Variant Collection"),
                    "NewShaderVariants",
                    "shadervariants",
                    L10n.Tr("Save shader variant collection"),
                    ProjectWindowUtil.GetActiveFolderPath());
                if (!string.IsNullOrEmpty(assetPath))
                    ShaderUtil.SaveCurrentShaderVariantCollection(assetPath);
            });
            var clearButton = root.MandatoryQ<Button>("ClearCurrentShaderVariants");
            clearButton.clickable = new Clickable(ShaderUtil.ClearCurrentShaderVariantCollection);
        }

        void GenerateTabs()
        {
            //Add BuiltInTab
            var builtInAsset = EditorGUIUtility.Load(GraphicsSettingsData.builtInTabContent) as VisualTreeAsset;
            var builtInTemplate = builtInAsset.Instantiate();
            builtInTemplate
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            builtInTemplate.Query<PropertyField>().Where(p => !string.IsNullOrWhiteSpace(p.bindingPath)).ForEach(p =>
            {
                var serializedProperty = serializedObject.FindProperty(p.bindingPath);
                if (serializedProperty != null)
                    p.BindProperty(serializedProperty);
            });

            SetupTransparencySortMode(builtInTemplate);

            var builtInSettingsContainer = builtInTemplate.MandatoryQ<VisualElement>($"{GraphicsSettingsData.builtIn}SettingsContainer");
            var builtInHelpBoxes = GraphicsSettingsInspectorUtility.CreateRPHelpBox(m_VisibilityController, null);
            builtInSettingsContainer.Insert(0, builtInHelpBoxes);
            builtInHelpBoxes.MandatoryQ<HelpBox>("CurrentPipelineWarningHelpBox").text = GraphicsSettingsData.builtInWarningText.text;

            // If we open the settings page for the first time we check the current render pipeline.
            var settingsPipelineFullTypeName = UserSettings.FromConfig().pipelineFullTypeName;
            var selectedTab = string.IsNullOrEmpty(settingsPipelineFullTypeName) ? GraphicsSettings.currentRenderPipelineAssetType?.ToString() : settingsPipelineFullTypeName;

            var builtinActive = string.IsNullOrEmpty(selectedTab) || selectedTab.Equals(GraphicsSettingsData.builtIn);
            var tabButton = GraphicsSettingsInspectorUtility.CreateNewTab(m_TabbedView, GraphicsSettingsData.builtIn, builtInSettingsContainer, builtinActive);
            tabButton.userData = GraphicsSettingsData.builtIn;

            //Add SRP tabs
            foreach (var globalSettingsContainer in m_GlobalSettings)
            {
                var globalSettingsElement = new VisualElement();
                var rpHelpBoxes = GraphicsSettingsInspectorUtility.CreateRPHelpBox(m_VisibilityController, globalSettingsContainer.renderPipelineAssetType);
                globalSettingsElement.Add(rpHelpBoxes);

                globalSettingsElement.Bind(globalSettingsContainer.serializedObject);
                var propertyEditor = new PropertyField(globalSettingsContainer.property);
                propertyEditor.AddToClassList(InspectorElement.uIEInspectorVariantUssClassName);
                globalSettingsElement.Add(propertyEditor);

                var srpTabButton = GraphicsSettingsInspectorUtility.CreateNewTab(m_TabbedView, globalSettingsContainer.name, globalSettingsElement,
                    !builtinActive && selectedTab.Equals(globalSettingsContainer.renderPipelineAssetType.ToString()));
                srpTabButton.userData = globalSettingsContainer.renderPipelineAssetType;
            }
        }

        void SearchChanged()
        {
            var settingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>(null, false);
            if (settingsWindow.GetCurrentProvider() is not GraphicsSettingsProvider provider)
                return;

            var currentText = provider.settingsWindow.GetSearchText();
            if (string.IsNullOrWhiteSpace(currentText))
                return;

            if (currentText.Equals(m_CurrentText, StringComparison.Ordinal))
                return;

            m_CurrentText = currentText;

            if (m_Labels.Count == 0)
            {
                foreach (var tab in m_TabbedView.tabs)
                {
                    m_Labels.Add(tab.Target, tab.Target.Query<Label>().ToList());
                }
            }

            var highlighted = AnyLabelMatchSearch(m_TabbedView.ActiveTab.Target);
            if (highlighted)
                return;

            foreach (var tab in m_TabbedView.tabs)
            {
                if(tab == m_TabbedView.ActiveTab)
                    continue;

                var highlightedInDifferentTab = AnyLabelMatchSearch(tab.Target);
                if (!highlightedInDifferentTab)
                    continue;

                m_TabbedView.Activate(tab);
                return;
            }

            bool AnyLabelMatchSearch(VisualElement element)
            {
                var list = m_Labels[element];
                foreach (var label in list)
                {
                    if (SettingsWindow.Styles.TagRegex.IsMatch(label.text))
                        return true;
                }
                return false;
            }
        }

        void OnMainScrollViewGeometryChanged(GeometryChangedEvent evt)
        {
            void Unregister()
            {
                m_ScrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnMainScrollViewGeometryChanged);
                m_GeometryChangedEventCounter = 0;
                m_FinishedInitialization = true;
                m_TabbedView?.schedule.Execute(SearchChanged).Every(100);
            }

            void UnregisterAfterLastGeometryChange()
            {
                if (m_GeometryChangedEventCounter > 1)
                {
                    m_GeometryChangedEventCounter--;
                    return;
                }

                Unregister();
            }

            var savedScrollOffset = UserSettings.FromConfig().scrollOffset;
            if (m_ScrollView.scrollOffset != savedScrollOffset)
            {
                m_ScrollView.scrollOffset = savedScrollOffset;

                m_GeometryChangedEventCounter++;
                m_ScrollView.contentContainer.schedule.Execute(UnregisterAfterLastGeometryChange).ExecuteLater(500);
            }
            else
                Unregister();
        }

        void BindEnumFieldWithFadeGroup(VisualElement content, string id, Action buttonCallback)
        {
            var enumMode = content.MandatoryQ<EnumField>($"{id}Modes");
            var enumModeGroup = content.MandatoryQ<VisualElement>($"{id}ModesGroup");
            var enumModeProperty = serializedObject.FindProperty($"m_{id}Stripping");
            UIElementsEditorUtility.SetVisibility(enumModeGroup, (StrippingModes)enumModeProperty.enumValueFlag == StrippingModes.Custom);
            UIElementsEditorUtility.BindSerializedProperty<StrippingModes>(enumMode, enumModeProperty,
                mode => UIElementsEditorUtility.SetVisibility(enumModeGroup, mode == StrippingModes.Custom));
            content.MandatoryQ<Button>($"Import{id}FromCurrentScene").clicked += buttonCallback;
        }

        void BindEnumFieldToLightProbe(VisualElement content)
        {
            var enumMode = content.MandatoryQ<EnumField>("LightProbe");
            var enumModeProperty = serializedObject.FindProperty(enumMode.bindingPath);
            UIElementsEditorUtility.BindSerializedProperty<LightProbeOutsideHullStrategy>(enumMode, enumModeProperty);
        }

        void SetupTransparencySortMode(VisualElement root)
        {
            var transparencySortMode = root.MandatoryQ<PropertyField>("TransparencySortMode");
            var transparencySortAxis = root.MandatoryQ<PropertyField>("TransparencySortAxis");
            transparencySortMode.RegisterValueChangeCallback(evt =>
                UIElementsEditorUtility.SetVisibility(transparencySortAxis, (TransparencySortMode)evt.changedProperty.enumValueIndex == TransparencySortMode.CustomAxis));
        }

        void UpdateBuildProfileGraphicsSettingsOverrideWarning()
        {
            if (BuildProfileContext.ActiveProfileHasGraphicsSettings())
                m_BuildProfileGraphicsSettingsOverrideWarning.style.display = DisplayStyle.Flex;
            else
                m_BuildProfileGraphicsSettingsOverrideWarning.style.display = DisplayStyle.None;
        }

        [Serializable]
        class UserSettings
        {
            internal const string s_Key = $"{nameof(GraphicsSettingsInspector)}_{nameof(UserSettings)}";

            public Vector2 scrollOffset = Vector2.zero;
            public string pipelineFullTypeName = "";

            public static UserSettings FromConfig()
            {
                var serializedScrollValues = EditorUserSettings.GetConfigValue(s_Key);
                return string.IsNullOrEmpty(serializedScrollValues) ? new UserSettings() : JsonUtility.FromJson<UserSettings>(serializedScrollValues);
            }

            public static void ToConfig(VisualElement root)
            {
                var userSettings = new UserSettings();

                var mainScrollView = root.Q<ScrollView>("MainScrollView");
                var tabbedView = root.Q<TabbedView>("PipelineSpecificSettings");
                if (mainScrollView == null)
                    return;

                userSettings.scrollOffset = mainScrollView.scrollOffset;

                if (tabbedView != null && tabbedView.ActiveTab != null)
                {
                    userSettings.pipelineFullTypeName = tabbedView.ActiveTab.userData.ToString();
                }
                else
                {
                    userSettings.pipelineFullTypeName = GraphicsSettingsData.builtIn;
                }

                EditorUserSettings.SetConfigValue(s_Key, JsonUtility.ToJson(userSettings));
            }
        }

        //internal for tests
        internal static void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            if (!info.undoName.StartsWith(RenderPipelineGraphicsSettingsManager.undoResetName))
                return;

            EditorGraphicsSettings.ForEachPipelineSettings(globalSettings =>
            {
                if (globalSettings == null)
                    return;

                var serializedGlobalSettings = new SerializedObject(globalSettings);
                var settingsIterator = serializedGlobalSettings.FindProperty(RenderPipelineGraphicsSettingsManager.serializationPathToCollection);

                using (new Notifier.Scope(settingsIterator, updateStateNow: false))
                {
                    /* Nothing to do: changes already done before this callback */
                }
            });
        }
    }

    internal enum StrippingModes
    {
        Automatic = 0,
        Custom = 1
    }

    /// <summary>
    /// Control visibility of UI elements depends on active Render Pipeline.
    /// For one it stays specific for GraphicsSettings as it requires a way to determine BuiltinOnly elements and there is no generic way to do it.
    /// </summary>
    internal class VisibilityControllerBasedOnRenderPipeline : IDisposable
    {
        readonly List<ValueTuple<VisualElement, Type[]>> m_TrackedElements = new();

        public void Initialize()
        {
            RenderPipelineManager.activeRenderPipelineAssetChanged += RenderPipelineAssetChanged;
        }

        public bool RegisterVisualElement(VisualElement element, params Type[] renderPipelineAssetTypes)
        {
            if (element == null)
                return false;

            if (m_TrackedElements.Any(t => t.Item1 == element))
                return false;

            renderPipelineAssetTypes ??= new Type[] { null };
            var newPair = new ValueTuple<VisualElement, Type[]>(element, renderPipelineAssetTypes);
            m_TrackedElements.Add(newPair);
            UpdateElementVisibility(GraphicsSettings.currentRenderPipelineAssetType, newPair);
            return true;
        }

        public void RegisterVisualElementTree(VisualElement element, params Type[] renderPipelineAssetTypes)
        {
            element.Query<GraphicsSettingsElement>().ForEach(e => RegisterVisualElement(e, renderPipelineAssetTypes));
        }

        public bool UnregisterVisualElement(VisualElement element)
        {
            if (element == null || m_TrackedElements.Count == 0)
                return false;

            var index = m_TrackedElements.FindIndex(t => element == t.Item1);
            if (index < 0)
                return false;

            m_TrackedElements.RemoveAt(index);
            return true;
        }

        void RenderPipelineAssetChanged(RenderPipelineAsset previous, RenderPipelineAsset next)
        {
            var newAssetType = next != null ? next.GetType() : null;
            for (var i = 0; i < m_TrackedElements.Count; i++)
            {
                UpdateElementVisibility(newAssetType, m_TrackedElements[i]);
            }
        }

        bool ShouldDisplayElement(Type currentRenderPipelineAssetType, Type[] renderPipelineAssetTypes)
        {
            return renderPipelineAssetTypes.Contains(currentRenderPipelineAssetType)
                   || renderPipelineAssetTypes.Any(t => t != null && t.IsAssignableFrom(currentRenderPipelineAssetType));
        }

        void UpdateElementVisibility(Type currentRenderPipelineAssetType, (VisualElement, Type[]) pair)
        {
            if (ShouldDisplayElement(currentRenderPipelineAssetType, pair.Item2))
                Show(pair.Item1);
            else
                Hide(pair.Item1);
        }

        void Show(VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        void Hide(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        public void ShowAll()
        {
            for (int i = 0; i < m_TrackedElements.Count; i++)
            {
                Show(m_TrackedElements[i].Item1);
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < m_TrackedElements.Count; i++)
            {
                Hide(m_TrackedElements[i].Item1);
            }
        }

        public void Clear()
        {
            m_TrackedElements.Clear();
        }

        public void Dispose()
        {
            RenderPipelineManager.activeRenderPipelineAssetChanged -= RenderPipelineAssetChanged;
        }
    }

    internal class GraphicsSettingsProvider : SettingsProvider
    {
        internal static readonly string s_GraphicsSettingsProviderPath = "Project/Graphics";
        internal GraphicsSettingsInspector inspector;

        [SettingsProvider]
        public static SettingsProvider CreateUserSettingsProvider()
        {
            var graphicsSettingsProvider = new GraphicsSettingsProvider(s_GraphicsSettingsProviderPath, SettingsScope.Project)
            {
                icon = EditorGUIUtility.FindTexture("UnityEngine/UI/GraphicRaycaster Icon")
            };
            return graphicsSettingsProvider;
        }

        internal GraphicsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            UpdateKeywords();
            activateHandler = (_, root) =>
            {
                var (graphicsSettings, globalSettingsExist, globalSettingsContainers) = UpdateKeywords();
                inspector = Editor.CreateEditor(graphicsSettings) as GraphicsSettingsInspector;
                inspector.Create(root, globalSettingsExist, globalSettingsContainers);
            };
            deactivateHandler = (() =>
            {
                if (inspector != null)
                {
                    inspector.Dispose();
                    inspector = null;
                }
            });
        }

        public (UnityEngine.Object, bool, List<GraphicsSettingsInspectorUtility.GlobalSettingsContainer>) UpdateKeywords()
        {
            var keywordsList = new List<string>();
            keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorTierSettings.Styles>());

            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var graphicsSettingsSO = new SerializedObject(graphicsSettings);
            keywordsList.AddRange(GetSearchKeywordsFromSerializedObject(graphicsSettingsSO, ResolveName));

            var globalSettingsExist = GraphicsSettingsInspectorUtility.GatherGlobalSettingsFromSerializedObject(graphicsSettingsSO, out var globalSettingsContainers);
            if (globalSettingsExist)
            {
                foreach (var globalSetting in globalSettingsContainers)
                    keywordsList.AddRange(GetSearchKeywordsFromSerializedObject(globalSetting.serializedObject));
            }

            keywords = keywordsList;
            return (graphicsSettings, globalSettingsExist, globalSettingsContainers);
        }

        // Important: GraphicsSettings only exists on C++ side.
        // As there is no C# representation of the field, there is no way to decorate them with attribute.
        // C++ variable name are used to build the path of the SerializedProperty, but do not match the displayed name.
        // Introducing a C# bindings may be a good solution for long therm maintenance but for limited cost, let's just remap.
        static readonly Dictionary<string, string> k_CPPToLabels = new()
        {
            //below are labels set in the UXML GraphicsSettingsEditor-Common.uxml
            { "m_LightmapStripping" , "Lightmap Modes" },
            { "m_LightmapKeepPlain" , "Baked Non-Directional" },
            { "m_LightmapKeepDirCombined" , "Baked Directional" },
            { "m_LightmapKeepDynamicPlain" , "Realtime Non-Directional" },
            { "m_LightmapKeepDynamicDirCombined" , "Realtime Directional" },
            { "m_LightmapKeepShadowMask" , "Baked Shadowmask" },
            { "m_LightmapKeepSubtractive" , "Baked Subtractive" },
            { "m_FogStripping" , "Fog Modes" },
            { "m_FogKeepLinear" , "Linear" },
            { "m_FogKeepExp" , "Exponential" },
            { "m_FogKeepExp2" , "Exponential Squared" },
            { "m_InstancingStripping" , "Instancing Variants" },
            { "m_BrgStripping" , "Batch Renderer Group Variants" },
            { "m_LogWhenShaderIsCompiled" , "Log Shader Compilation" },
            { "m_CameraRelativeLightCulling" , "Lights" },
            { "m_CameraRelativeShadowCulling" , "Shadows" },
            { "m_VideoShadersIncludeMode" , "Video" },
            { "m_AlwaysIncludedShaders" , "Always Included Shaders" },
            { "m_LightProbeOutsideHullStrategy" , "Renderer Light Probe Selection" },
            { "m_PreloadedShaders" , "Preload Shaders" },
            // Below: 1 serialized property is for 2 field of the inspector. Adding all to the search keys
            { "m_PreloadShadersBatchTimeLimit" , $"Preload Shaders After Showing First Scene Preload Time Limit Per Frame (ms)" },
        };

        string ResolveName(SerializedProperty property)
        {
            if (k_CPPToLabels.ContainsKey(property.name))
                return k_CPPToLabels[property.name];
            return property.displayName;
        }

        internal void Reload()
        {
            if (inspector == null)
                return;

            //Ensure of the Global Settings can update keywords as new settings may have been added
            var (_, globalSettingsExist, globalSettingsContainers) = UpdateKeywords();
            inspector.Reload(globalSettingsExist, globalSettingsContainers);
        }
    }
}
