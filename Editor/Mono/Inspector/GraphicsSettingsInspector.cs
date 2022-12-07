// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [CustomEditor(typeof(GraphicsSettings))]
    internal class GraphicsSettingsInspector : ProjectSettingsBaseEditor
    {
        internal class ResourcesPaths
        {
            internal const string graphicsSettings = "StyleSheets/ProjectSettings/GraphicsSettings.uss";
            internal const string bodyTemplate = "UXML/ProjectSettings/GraphicsSettingsEditor.uxml";
        }

        readonly VisibilityControllerBasedOnRenderPipeline m_VisibilityController = new();

        internal void CreateInspectorUI(VisualElement root)
        {
            root.Add(ObjectUpdater());
            root.Add(CreateGUI());
        }

        // As we use multiple IMGUI container while porting everything to UITK we will call serializedObject.Update in first separate IMGUI container.
        // This way we don't need to do it in each following containers.
        VisualElement ObjectUpdater()
        {
            return new IMGUIContainer(() =>
            {
                m_VisibilityController.Update();
                serializedObject.Update();
            });
        }

        VisualElement CreateGUI()
        {
            var visualTreeAsset = EditorGUIUtility.Load(ResourcesPaths.bodyTemplate) as VisualTreeAsset;
            var template = visualTreeAsset.Instantiate();
            template
                .Query<GraphicsSettingsElement>()
                .ForEach(d => d.Initialize(serializedObject));

            m_VisibilityController.RegisterVisualElementTree(template);
            return template;
        }

        void OnEnable()
        {
            m_VisibilityController.Initialize();
        }

        void OnDisable()
        {
            m_VisibilityController.Clear();
            m_VisibilityController.Dispose();
        }
    }

    /// <summary>
    /// Control visibility of UI elements depends on active Render Pipeline.
    /// For one it stays specific for GraphicsSettings as it requires a way to determine BuiltinOnly elements and there is no generic way to do it.
    /// </summary>
    internal class VisibilityControllerBasedOnRenderPipeline : IDisposable
    {
        readonly List<ValueTuple<GraphicsSettingsElement, SupportedOnRenderPipelineAttribute>> m_TrackedElements = new();

        RenderPipelineAsset m_PreviousAsset;

        public void Initialize()
        {
            RenderPipelineManager.activeRenderPipelineAssetChanged += RenderPipelineAssetChanged;
        }

        public bool RegisterVisualElement(GraphicsSettingsElement element)
        {
            if (m_TrackedElements.Any(t => t.Item1 == element))
                return false;

            var type = element.GetType();
            var attribute = type.GetCustomAttribute<SupportedOnRenderPipelineAttribute>(false);

            UpdateElementVisibility (element, attribute);
            m_TrackedElements.Add(new ValueTuple<GraphicsSettingsElement, SupportedOnRenderPipelineAttribute>(element, attribute));

            return true;
        }

        public void RegisterVisualElementTree(VisualElement element)
        {
            element.Query<GraphicsSettingsElement>().ForEach(RegisterVisualElement);
        }

        public bool UnregisterVisualElement(GraphicsSettingsElement element)
        {
            var type = element.GetType();
            var attribute = type.GetCustomAttribute<SupportedOnRenderPipelineAttribute>(false);
            if (attribute == null)
                return false;

            var index = m_TrackedElements.FindIndex(t => element == t.Item1);
            if (index < 0)
                return false;

            m_TrackedElements.RemoveAt(index);
            return true;
        }

        public void Update()
        {
            if (GraphicsSettings.currentRenderPipeline == m_PreviousAsset)
                return;

            for (var i = 0; i < m_TrackedElements.Count; i++)
            {
                var trackedElement = m_TrackedElements[i];
                UpdateElementVisibility(trackedElement.Item1, trackedElement.Item2);
            }

            m_PreviousAsset = GraphicsSettings.currentRenderPipeline;
        }

        void RenderPipelineAssetChanged(RenderPipelineAsset previous, RenderPipelineAsset next)
        {
            Update();
        }

        bool ShouldDisplayElement(GraphicsSettingsElement element, SupportedOnRenderPipelineAttribute attribute)
        {
            if (attribute is { isSupportedOnCurrentPipeline: true })
                return true;
            return element.BuiltinOnly && !GraphicsSettings.isScriptableRenderPipelineEnabled || !element.BuiltinOnly;
        }

        void UpdateElementVisibility(GraphicsSettingsElement element, SupportedOnRenderPipelineAttribute attribute)
        {
            if (ShouldDisplayElement(element, attribute))
                Show(element);
            else
                Hide(element);
        }

        void Show(GraphicsSettingsElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        void Hide(GraphicsSettingsElement element)
        {
            element.style.display = DisplayStyle.None;
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
        [SettingsProvider]
        public static SettingsProvider CreateUserSettingsProvider()
        {
            var graphicsSettingsProvider = new GraphicsSettingsProvider("Project/Graphics", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorTitleBar.Styles>()
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorBuiltinShaders.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorCameraSettings.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorLightProbe.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorRenderPipelineAsset.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorShaderLog.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorCullingSettings.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorTierSettings.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorBuiltinShaders.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorShaderStripping.Styles>())
                    .Concat(GetSearchKeywordsFromGUIContentProperties<GraphicsSettingsInspectorVideoShader.Styles>())
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

                var editor = Editor.CreateEditor(settingsObj) as GraphicsSettingsInspector;
                element.styleSheets.Add(EditorGUIUtility.Load(GraphicsSettingsInspector.ResourcesPaths.graphicsSettings) as StyleSheet);
                editor.CreateInspectorUI(element);
            };
        }
    }
}
