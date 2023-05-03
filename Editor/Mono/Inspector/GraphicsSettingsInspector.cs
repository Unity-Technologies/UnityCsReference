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
        ProjectSettingsSection m_SRPSettings;
        TabbedView m_TabbedView;

        VisualElement currentRoot;
        VisualElement currentSettings;
        List<RenderPipelineAsset> pipelineAssets = new();

        void OnEnable()
        {
            m_VisibilityController.Initialize();
            RenderPipelineManager.activeRenderPipelineAssetChanged += OnPipelineChanged;
        }

        void OnPipelineChanged(RenderPipelineAsset prevAsset, RenderPipelineAsset nextAsset)
        {
            var currentPipelines = GraphicsSettingsUtils.GetCurrentPipelines(out var srpInUse);
            if (currentPipelines.Count != pipelineAssets.Count)
            {
                AddOrUpdateSettingsUI(currentPipelines, srpInUse);
                return;
            }

            for (int i = 0; i < currentPipelines.Count; i++)
            {
                if (pipelineAssets.Contains(currentPipelines[i])) continue;
                AddOrUpdateSettingsUI(currentPipelines, srpInUse);
                break;
            }
        }

        public void OnDisable()
        {
            m_VisibilityController.Clear();
            m_VisibilityController.Dispose();
            RenderPipelineManager.activeRenderPipelineAssetChanged -= OnPipelineChanged;
        }

        internal void CreateInspectorUI(VisualElement root, bool srpInUse, List<RenderPipelineAsset> currentPipelines)
        {
            currentRoot = root;
            currentRoot.Add(ObjectUpdater());
            AddOrUpdateSettingsUI(currentPipelines, srpInUse);
        }

        // As we use multiple IMGUI container while porting everything to UITK we will call serializedObject.Update in first separate IMGUI container.
        // This way we don't need to do it in each following containers.
        VisualElement ObjectUpdater()
        {
            return new IMGUIContainer(() => serializedObject.Update());
        }

        void AddOrUpdateSettingsUI(List<RenderPipelineAsset> assets, bool srpInUse)
        {
            if (currentSettings != null)
                currentRoot.Remove(currentSettings);
            currentSettings = CreateSettingsUI(assets, srpInUse);
            currentRoot.Add(currentSettings);
            pipelineAssets = assets;
        }

        VisualElement CreateSettingsUI(List<RenderPipelineAsset> assets, bool srpInUse)
        {
            m_VisibilityController.Clear();

            var visualTreeAsset = EditorGUIUtility.Load(srpInUse ? ResourcesPaths.bodyTemplateSRP : ResourcesPaths.bodyTemplateBuiltInOnly) as VisualTreeAsset;
            var content = visualTreeAsset.Instantiate();
            content
                .Query<ProjectSettingsElementWithSO>()
                .ForEach(d => d.Initialize(serializedObject));

            var lightmapModes = content.MandatoryQ<EnumField>("LightmapModes");
            var lightmapModesGroup = content.MandatoryQ<FadeGroup>("LightmapModesGroup");
            var lightmapModesProperty = serializedObject.FindProperty("m_LightmapStripping");
            var lightmapModesUpdate =
                GraphicsSettingsUtils.BindSerializedProperty<StrippingModes>(lightmapModes, lightmapModesProperty, mode => { lightmapModesGroup.value = mode == StrippingModes.Custom; });
            lightmapModesUpdate?.Invoke();
            lightmapModesGroup.value = lightmapModesProperty.intValue == (int)StrippingModes.Custom;
            content.MandatoryQ<Button>("ImportLightmapFromCurrentScene").clicked += ShaderUtil.CalculateLightmapStrippingFromCurrentScene;

            var fogModes = content.MandatoryQ<EnumField>("FogModes");
            var fogModesGroup = content.MandatoryQ<FadeGroup>("FogModesGroup");
            var fogModesProperty = serializedObject.FindProperty("m_FogStripping");
            var fogModesUpdate = GraphicsSettingsUtils.BindSerializedProperty<StrippingModes>(fogModes, fogModesProperty, mode => { fogModesGroup.value = mode == StrippingModes.Custom; });
            fogModesUpdate?.Invoke();
            fogModesGroup.value = fogModesProperty.intValue == (int)StrippingModes.Custom;
            content.MandatoryQ<Button>("ImportFogFromCurrentScene").clicked += ShaderUtil.CalculateFogStrippingFromCurrentScene;

            if (srpInUse)
            {
                m_SRPSettings = content.MandatoryQ<ProjectSettingsSection>("SRPSettings");
                m_TabbedView = content.MandatoryQ<TabbedView>("PipelineSpecificSettings");
                m_TabbedView.RegisterCallback<AttachToPanelEvent>(evt => GenerateTabs(m_TabbedView, assets));
            }
            else
            {
                content.Query<GraphicsSettingsElement>()
                    .ForEach(e =>
                    {
                        if (e.BuiltinOnly)
                            m_VisibilityController.RegisterVisualElement(e, null);
                    });
                content.Query<BuiltInShaderElement>().ForEach(e =>
                {
                    m_VisibilityController.RegisterVisualElement(e, null);
                });
            }

            GraphicsSettingsUtils.LocalizeVisualTree(content);
            content.Bind(serializedObject);

            return content;
        }

        void GenerateTabs(TabbedView tabView, List<RenderPipelineAsset> allAssets)
        {
            //Future tab generation after first migration of global settings

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
            GraphicsSettingsUtils.CreateNewTab(tabView, "Built-In", builtInSettingsContainer, GraphicsSettings.currentRenderPipeline == null);

            //Add SRP tabs
            foreach (var srpAsset in allAssets)
            {
                if (srpAsset == null) continue;

                var srpSettingsContainer = new VisualElement();
                if (allAssets.Count > 1)
                {
                    var srpWarning = GraphicsSettingsUtils.CreateSRPWarning(ResourcesPaths.warningTemplateForSRP, allAssets, srpAsset);
                    m_VisibilityController.RegisterVisualElement(srpWarning.warning, srpWarning.activePipelines);
                    srpSettingsContainer.Add(srpWarning.warning);
                }

                var abbreviation = GraphicsSettingsUtils.GetPipelineAssetAbbreviation(srpAsset);
                GraphicsSettingsUtils.CreateNewTab(tabView, abbreviation, srpSettingsContainer, GraphicsSettings.currentRenderPipeline == srpAsset);
            }
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

                var currentPipelines = GraphicsSettingsUtils.GetCurrentPipelines(out var srpInUse);
                m_Inspector.CreateInspectorUI(element, srpInUse, currentPipelines);

                var uxmlKeywords = srpInUse
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
