// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor
{
    [CustomEditor(typeof(GraphicsSettings))]
    internal class GraphicsSettingsInspector : ProjectSettingsBaseEditor
    {
        internal static class GraphicsSettingsData
        {
            internal const string bodyTemplateBuiltInOnly = "UXML/ProjectSettings/GraphicsSettingsEditor-Builtin.uxml";
            internal const string bodyTemplateSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-SRP.uxml";
            internal const string helpBoxesTemplateForSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-HelpBoxes.uxml";
            internal const string builtInTabContent = "UXML/ProjectSettings/GraphicsSettingsEditor-BuiltInTab.uxml";

            internal const string persistentViewKey = $"{nameof(GraphicsSettingsInspector)}_ScrollPosition";

            internal static GUIContent builtInWarningText =
                EditorGUIUtility.TrTextContent("A Scriptable Render Pipeline is in use. Settings in the Built-In Render Pipeline are not currently in use.");
        }

        internal static GraphicsSettingsInspector s_Instance;
        internal IEnumerable<GraphicsSettingsUtils.GlobalSettingsContainer> globalSettings => m_GlobalSettings;

        readonly VisibilityControllerBasedOnRenderPipeline m_VisibilityController = new();
        TabbedView m_TabbedView;
        VisualElement m_CurrentRoot;
        List<GraphicsSettingsUtils.GlobalSettingsContainer> m_GlobalSettings;

        void OnEnable()
        {
            s_Instance = this;
            m_VisibilityController.Initialize();
        }

        public void OnDisable()
        {
            s_Instance = null;
            m_VisibilityController.Clear();
            m_VisibilityController.Dispose();

            if (m_CurrentRoot != null && Unsupported.IsDeveloperMode())
            {
                var mainScrollView = m_CurrentRoot.Q<ScrollView>("MainScrollView");
                var tabbedView = m_CurrentRoot.Q<TabbedView>("PipelineSpecificSettings");
                if(mainScrollView == null || tabbedView == null)
                    return;
                var persistentViewValues = new PersistentViewValues
                {
                    vertical = mainScrollView.verticalScroller.value,
                    horizontal = mainScrollView.horizontalScroller.value,
                    tabIndex = tabbedView?.ActiveTabIndex ?? -1
                };
                EditorUserSettings.SetConfigValue(GraphicsSettingsData.persistentViewKey, JsonUtility.ToJson(persistentViewValues));
            }
        }

        internal void CreateInspectorUI(VisualElement root, bool globalSettingsExist, List<GraphicsSettingsUtils.GlobalSettingsContainer> globalSettings)
        {
            m_CurrentRoot = root;
            m_GlobalSettings = globalSettings;

            m_CurrentRoot.Add(ObjectUpdater());
            Setup(globalSettingsExist);
        }

        // As we use multiple IMGUI container while porting everything to UITK we will call serializedObject.Update in first separate IMGUI container.
        // This way we don't need to do it in each following containers.
        VisualElement ObjectUpdater()
        {
            return new IMGUIContainer(() => serializedObject.Update());
        }

        void Setup(bool globalSettingsExist)
        {
            m_VisibilityController.Clear();

            m_CurrentRoot
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            BindEnumFieldWithFadeGroup(m_CurrentRoot,
                "LightmapModes",
                "LightmapModesGroup",
                "m_LightmapStripping",
                "ImportLightmapFromCurrentScene",
                ShaderUtil.CalculateLightmapStrippingFromCurrentScene);
            BindEnumFieldWithFadeGroup(m_CurrentRoot,
                "FogModes",
                "FogModesGroup",
                "m_FogStripping",
                "ImportFogFromCurrentScene",
                ShaderUtil.CalculateFogStrippingFromCurrentScene);

            PersistentViewValues persistantViewValues = null;
            if (Unsupported.IsDeveloperMode())
                persistantViewValues = LoadPersistantViewValues();

            if (globalSettingsExist)
            {
                m_TabbedView = m_CurrentRoot.MandatoryQ<TabbedView>("PipelineSpecificSettings");
                GenerateTabs(persistantViewValues);
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

            ApplyPersistentView(m_CurrentRoot, persistantViewValues);

            GraphicsSettingsUtils.LocalizeVisualTree(m_CurrentRoot);
            m_CurrentRoot.Bind(serializedObject);
        }

        static PersistentViewValues LoadPersistantViewValues()
        {
            var serializedScrollValues = EditorUserSettings.GetConfigValue(GraphicsSettingsData.persistentViewKey);
            return string.IsNullOrEmpty(serializedScrollValues) ? null : JsonUtility.FromJson<PersistentViewValues>(serializedScrollValues);
        }

        void GenerateTabs(PersistentViewValues persistantViewValues)
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

            var builtInSettingsContainer = builtInTemplate.MandatoryQ<VisualElement>("Built-InSettingsContainer");
            var builtInHelpBoxes = GraphicsSettingsUtils.CreateRPHelpBox(m_VisibilityController, null);
            builtInSettingsContainer.Insert(0, builtInHelpBoxes);
            builtInHelpBoxes.MandatoryQ<HelpBox>("CurrentPipelineWarningHelpBox").text = GraphicsSettingsData.builtInWarningText.text;

            var builtinActive = persistantViewValues == null || persistantViewValues.tabIndex <= 0;
            GraphicsSettingsUtils.CreateNewTab(m_TabbedView, "Built-In", builtInSettingsContainer, builtinActive);

            //Add SRP tabs
            for (var i = 0; i < m_GlobalSettings.Count; i++)
            {
                var globalSettingsContainer = m_GlobalSettings[i];
                var globalSettingsElement = new VisualElement();
                var rpHelpBoxes = GraphicsSettingsUtils.CreateRPHelpBox(m_VisibilityController, globalSettingsContainer.renderPipelineAssetType);
                globalSettingsElement.Add(rpHelpBoxes);

                globalSettingsElement.Bind(globalSettingsContainer.serializedObject);
                var propertyEditor = new PropertyField(globalSettingsContainer.property);
                propertyEditor.AddToClassList(InspectorElement.uIEInspectorVariantUssClassName);
                globalSettingsElement.Add(propertyEditor);

                var rpActive = persistantViewValues != null && persistantViewValues.tabIndex != -1
                    ? persistantViewValues.tabIndex == i + 1
                    : GraphicsSettings.currentRenderPipelineAssetType == globalSettingsContainer.renderPipelineAssetType;

                GraphicsSettingsUtils.CreateNewTab(m_TabbedView, globalSettingsContainer.name, globalSettingsElement, rpActive);
            }
        }

        static void ApplyPersistentView(VisualElement content, PersistentViewValues persistantViewValues)
        {
            if (persistantViewValues == null)
                return;

            var mainScrollView = content.Q<ScrollView>("MainScrollView");
            mainScrollView.verticalScroller.value = persistantViewValues.vertical;
            mainScrollView.horizontalScroller.value = persistantViewValues.horizontal;
            mainScrollView.UpdateContentViewTransform();
        }

        void BindEnumFieldWithFadeGroup(VisualElement content, string enumFieldName, string fadeGroupName, string propertyName, string buttonName, Action buttonCallback)
        {
            var enumMode = content.MandatoryQ<EnumField>(enumFieldName);
            var enumModeGroup = content.MandatoryQ<VisualElement>(fadeGroupName);
            var enumModeProperty = serializedObject.FindProperty(propertyName);
            var lightmapModesUpdate = UIElementsEditorUtility.BindSerializedProperty<StrippingModes>(enumMode, enumModeProperty,
                mode => UIElementsEditorUtility.SetVisibility(enumModeGroup, mode == StrippingModes.Custom));
            lightmapModesUpdate?.Invoke();
            content.MandatoryQ<Button>(buttonName).clicked += buttonCallback;
        }

        void SetupTransparencySortMode(VisualElement root)
        {
            var transparencySortMode = root.MandatoryQ<PropertyField>("TransparencySortMode");
            var transparencySortAxis = root.MandatoryQ<PropertyField>("TransparencySortAxis");
            transparencySortMode.RegisterValueChangeCallback(evt =>
                UIElementsEditorUtility.SetVisibility(transparencySortAxis, (TransparencySortMode)evt.changedProperty.enumValueIndex == TransparencySortMode.CustomAxis));
        }

        [Serializable]
        class PersistentViewValues
        {
            public float vertical;
            public float horizontal;
            public int tabIndex;
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
        GraphicsSettingsInspector m_Inspector;

        [SettingsProvider]
        public static SettingsProvider CreateUserSettingsProvider()
        {
            var graphicsSettingsProvider = new GraphicsSettingsProvider("Project/Graphics", SettingsScope.Project)
            {
                icon = EditorGUIUtility.FindTexture("UnityEngine/UI/GraphicRaycaster Icon")
            };
            return graphicsSettingsProvider;
        }

        internal GraphicsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            activateHandler = (text, root) =>
            {
                var settingsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (settingsObj == null)
                    return;

                m_Inspector = Editor.CreateEditor(settingsObj) as GraphicsSettingsInspector;

                var globalSettingsExist = GraphicsSettingsUtils.GatherGlobalSettingsFromSerializedObject(m_Inspector.serializedObject, out var globalSettings);
                var content = CreateGraphicsSettingsUI(globalSettingsExist, m_Inspector, root, globalSettings);
                this.keywords = CreateKeywordsList(content);
            };
            deactivateHandler = (() =>
            {
                if (m_Inspector != null)
                    m_Inspector.OnDisable();
            });
        }


        internal static TemplateContainer CreateGraphicsSettingsUI(bool globalSettingsExist, GraphicsSettingsInspector inspector,  VisualElement root, List<GraphicsSettingsUtils.GlobalSettingsContainer> globalSettings)
        {
            var visualTreeAsset =
                EditorGUIUtility.Load(globalSettingsExist ? GraphicsSettingsInspector.GraphicsSettingsData.bodyTemplateSRP : GraphicsSettingsInspector.GraphicsSettingsData.bodyTemplateBuiltInOnly) as
                    VisualTreeAsset;
            var content = visualTreeAsset.Instantiate();
            root.Add(content);
            inspector.CreateInspectorUI(root, globalSettingsExist, globalSettings);
            return content;
        }

        List<string> CreateKeywordsList(TemplateContainer content)
        {
            var keywordsList = new List<string>();
            keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorTierSettings.Styles>());
            keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorShaderPreload.Styles>());
            keywordsList.AddRange(GetSearchKeywordsFromPath("ProjectSettings/GraphicsSettings.asset"));
            keywordsList.AddRange(GetSearchKeywordsFromVisualElementTree(content));
            return keywordsList;
        }
    }
}
