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
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor
{
    [CustomEditor(typeof(GraphicsSettings))]
    internal class GraphicsSettingsInspector : ProjectSettingsBaseEditor
    {
        internal class ResourcesPaths
        {
            internal const string graphicsSettings = "StyleSheets/ProjectSettings/GraphicsSettings.uss";
            internal const string bodyTemplateBuiltInOnly = "UXML/ProjectSettings/GraphicsSettingsEditor-Builtin.uxml";
            internal const string bodyTemplateSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-SRP.uxml";
            internal const string warningTemplateForSRP = "UXML/ProjectSettings/GraphicsSettingsEditor-Warning.uxml";
            internal const string builtInTabContent = "UXML/ProjectSettings/GraphicsSettingsEditor-BuiltInTab.uxml";
        }

        readonly VisibilityControllerBasedOnRenderPipeline m_VisibilityController = new();
        TabbedView m_TabbedView;

        VisualElement m_CurrentRoot;
        VisualElement m_CurrentSettings;

        List<GraphicsSettingsUtils.GlobalSettingsContainer> m_GlobalSettings;

        void OnEnable()
        {
            m_VisibilityController.Initialize();
        }

        public void OnDisable()
        {
            m_VisibilityController.Clear();
            m_VisibilityController.Dispose();
        }

        internal void CreateInspectorUI(VisualElement root, List<GraphicsSettingsUtils.GlobalSettingsContainer> globalSettings)
        {
            m_CurrentRoot = root;
            m_GlobalSettings = globalSettings;

            m_CurrentRoot.Add(ObjectUpdater());
            m_CurrentSettings = CreateSettingsUI(globalSettings);
            m_CurrentRoot.Add(m_CurrentSettings);
        }

        // As we use multiple IMGUI container while porting everything to UITK we will call serializedObject.Update in first separate IMGUI container.
        // This way we don't need to do it in each following containers.
        VisualElement ObjectUpdater()
        {
            return new IMGUIContainer(() => serializedObject.Update());
        }

        VisualElement CreateSettingsUI(List<GraphicsSettingsUtils.GlobalSettingsContainer> globalSettings)
        {
            m_VisibilityController.Clear();

            var globalSettingsExists = globalSettings.Count > 0;
            var visualTreeAsset = EditorGUIUtility.Load(globalSettingsExists ? ResourcesPaths.bodyTemplateSRP : ResourcesPaths.bodyTemplateBuiltInOnly) as VisualTreeAsset;
            var content = visualTreeAsset.Instantiate();
            content
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            BindStrippingModesEnumWithFadeGroup(content,
                "LightmapModes",
                "LightmapModesGroup",
                "m_LightmapStripping",
                "ImportLightmapFromCurrentScene",
                ShaderUtil.CalculateLightmapStrippingFromCurrentScene);
            BindStrippingModesEnumWithFadeGroup(content,
                "FogModes",
                "FogModesGroup",
                "m_FogStripping",
                "ImportFogFromCurrentScene",
                ShaderUtil.CalculateFogStrippingFromCurrentScene);

            if (globalSettingsExists)
            {
                m_TabbedView = content.MandatoryQ<TabbedView>("PipelineSpecificSettings");
                m_TabbedView.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }
            else
            {
                content.Query<GraphicsSettingsElement>()
                    .ForEach(e =>
                    {
                        if (e.BuiltinOnly)
                            m_VisibilityController.RegisterVisualElement(e, null);
                    });
                content.Query<BuiltInShaderElement>().ForEach(e => { m_VisibilityController.RegisterVisualElement(e, null); });
            }

            GraphicsSettingsUtils.LocalizeVisualTree(content);
            content.Bind(serializedObject);

            return content;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            GenerateTabs();
            m_TabbedView.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        void GenerateTabs()
        {
            //Add BuiltInTab
            var builtInAsset = EditorGUIUtility.Load(ResourcesPaths.builtInTabContent) as VisualTreeAsset;
            var builtInTemplate = builtInAsset.Instantiate();
            builtInTemplate
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            //By some reason I have to manually bind properties
            builtInTemplate.Query<PropertyField>().Where(p => !string.IsNullOrWhiteSpace(p.bindingPath)).ForEach(p =>
            {
                var serializedProperty = serializedObject.FindProperty(p.bindingPath);
                if (serializedProperty != null)
                    p.BindProperty(serializedProperty);
            });

            var builtInSettingsContainer = builtInTemplate.MandatoryQ<VisualElement>("Built-InSettingsContainer");
            var builtInWarning = builtInSettingsContainer.Q<HelpBox>("Built-InWarning");
            m_VisibilityController.RegisterVisualElement(builtInWarning, typeof(RenderPipelineAsset));
            GraphicsSettingsUtils.CreateNewTab(m_TabbedView, "Built-In", builtInSettingsContainer, GraphicsSettings.currentRenderPipeline == null);

            //Add SRP tabs
            for (var i = 0; i < m_GlobalSettings.Count; i++)
            {
                var globalSettingsContainer = m_GlobalSettings[i];
                var globalSettingsElement = new VisualElement();
                var srpWarning = GraphicsSettingsUtils.CreateSRPWarning(ResourcesPaths.warningTemplateForSRP, m_GlobalSettings, globalSettingsContainer.renderPipelineAssetType);
                m_VisibilityController.RegisterVisualElement(srpWarning.warning, srpWarning.activePipelines);
                globalSettingsElement.Add(srpWarning.warning);

                globalSettingsElement.Bind(globalSettingsContainer.serializedObject);
                globalSettingsElement.Add(new PropertyField(globalSettingsContainer.property));

                GraphicsSettingsUtils.CreateNewTab(m_TabbedView, globalSettingsContainer.name, globalSettingsElement,
                    GraphicsSettings.currentRenderPipelineAssetType == globalSettingsContainer.renderPipelineAssetType);
            }
        }

        void BindStrippingModesEnumWithFadeGroup(VisualElement content, string enumFieldName, string fadeGroupName, string propertyName, string buttonName, Action buttonCallback)
        {
            var enumMode = content.MandatoryQ<EnumField>(enumFieldName);
            var enumModeGroup = content.MandatoryQ<FadeGroup>(fadeGroupName);
            var enumModeProperty = serializedObject.FindProperty(propertyName);
            var lightmapModesUpdate = GraphicsSettingsUtils.BindSerializedProperty<StrippingModes>(enumMode, enumModeProperty,
                mode => enumModeGroup.value = mode == StrippingModes.Custom);
            lightmapModesUpdate?.Invoke();
            enumModeGroup.value = enumModeProperty.intValue == (int)StrippingModes.Custom;
            content.MandatoryQ<Button>(buttonName).clicked += buttonCallback;
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
                keywords = GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorTierSettings.Styles>()
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorShaderPreload.Styles>())
                    .Concat(GetSearchKeywordsFromPath("ProjectSettings/GraphicsSettings.asset")),
                icon = EditorGUIUtility.FindTexture("UnityEngine/UI/GraphicRaycaster Icon")
            };
            return graphicsSettingsProvider;
        }

        internal GraphicsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            activateHandler = (text, element) =>
            {
                var settingsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (settingsObj == null)
                    return;

                m_Inspector = Editor.CreateEditor(settingsObj) as GraphicsSettingsInspector;
                element.styleSheets.Add(EditorGUIUtility.Load(GraphicsSettingsInspector.ResourcesPaths.graphicsSettings) as StyleSheet);

                var renderPipelineGlobalSettingsMap = m_Inspector.serializedObject.FindProperty("m_RenderPipelineGlobalSettingsMap");
                var globalSettings = GraphicsSettingsUtils.CollectRenderPipelineAssetsByGlobalSettings(renderPipelineGlobalSettingsMap);
                m_Inspector.CreateInspectorUI(element, globalSettings);

                var uxmlKeywords = globalSettings.Count > 0
                    ? GraphicsSettingsUtils.GetSearchKeywordsFromUXMLInEditorResources(GraphicsSettingsInspector.ResourcesPaths.bodyTemplateSRP)
                        .Concat(GraphicsSettingsUtils.GetSearchKeywordsFromUXMLInEditorResources(GraphicsSettingsInspector.ResourcesPaths.builtInTabContent))
                    : GraphicsSettingsUtils.GetSearchKeywordsFromUXMLInEditorResources(GraphicsSettingsInspector.ResourcesPaths.bodyTemplateBuiltInOnly);
                this.keywords = this.keywords != null ? this.keywords.Concat(uxmlKeywords) : uxmlKeywords;
            };
            deactivateHandler = (() =>
            {
                if (m_Inspector != null)
                    m_Inspector.OnDisable();
            });
        }
    }
}
