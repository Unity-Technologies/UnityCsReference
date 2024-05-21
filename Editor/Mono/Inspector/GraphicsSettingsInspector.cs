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
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using UnityEditor.Rendering;

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
        }
        internal IEnumerable<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> globalSettings => m_GlobalSettings;

        readonly VisibilityControllerBasedOnRenderPipeline m_VisibilityController = new();
        TabbedView m_TabbedView;
        VisualElement m_CurrentRoot;
        ScrollView m_ScrollView;
        List<GraphicsSettingsInspectorUtility.GlobalSettingsContainer> m_GlobalSettings;

        readonly Dictionary<VisualElement, List<Label>> m_Labels = new();
        string m_CurrentText;

        bool m_FinishedInitialization;
        uint m_LastListsHash;
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
            m_FinishedInitialization = false;
        }

        void Setup(bool globalSettingsExist)
        {
            m_VisibilityController.Clear();

            m_CurrentRoot
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            BindEnumFieldWithFadeGroup(m_CurrentRoot, "Lightmap", ShaderUtil.CalculateLightmapStrippingFromCurrentScene);
            BindEnumFieldWithFadeGroup(m_CurrentRoot, "Fog", ShaderUtil.CalculateFogStrippingFromCurrentScene);

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
            var lightmapModesUpdate = UIElementsEditorUtility.BindSerializedProperty<StrippingModes>(enumMode, enumModeProperty,
                mode => UIElementsEditorUtility.SetVisibility(enumModeGroup, mode == StrippingModes.Custom));
            lightmapModesUpdate?.Invoke();
            content.MandatoryQ<Button>($"Import{id}FromCurrentScene").clicked += buttonCallback;
        }

        void SetupTransparencySortMode(VisualElement root)
        {
            var transparencySortMode = root.MandatoryQ<PropertyField>("TransparencySortMode");
            var transparencySortAxis = root.MandatoryQ<PropertyField>("TransparencySortAxis");
            transparencySortMode.RegisterValueChangeCallback(evt =>
                UIElementsEditorUtility.SetVisibility(transparencySortAxis, (TransparencySortMode)evt.changedProperty.enumValueIndex == TransparencySortMode.CustomAxis));
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
            keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorShaderPreload.Styles>());

            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var graphicsSettingsSO = new SerializedObject(graphicsSettings);
            keywordsList.AddRange(GetSearchKeywordsFromSerializedObject(graphicsSettingsSO));

            var globalSettingsExist = GraphicsSettingsInspectorUtility.GatherGlobalSettingsFromSerializedObject(graphicsSettingsSO, out var globalSettingsContainers);
            if (globalSettingsExist)
            {
                foreach (var globalSetting in globalSettingsContainers)
                    keywordsList.AddRange(GetSearchKeywordsFromSerializedObject(globalSetting.serializedObject));
            }

            keywords = keywordsList;
            return (graphicsSettings, globalSettingsExist, globalSettingsContainers);
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
