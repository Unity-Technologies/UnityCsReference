// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering.Settings;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    public static class GraphicsSettingsInspectorUtility
    {
        #region Localization

        internal static void Localize(VisualElement visualElement, Func<VisualElement, string> get, Action<VisualElement, string> set)
        {
            if (get == null)
                throw new InvalidOperationException("get function cannot be null");
            if (set == null)
                throw new InvalidOperationException("set function cannot be null");

            var extractedText = get.Invoke(visualElement);
            if (string.IsNullOrWhiteSpace(extractedText))
                return;

            var localizedString = L10n.Tr(extractedText);
            set.Invoke(visualElement, localizedString);
        }

        internal static void LocalizeTooltip(VisualElement visualElement)
        {
            Localize(visualElement, e => e.tooltip, (e, s) => e.tooltip = s);
        }

        internal static void LocalizeText(Label visualElement)
        {
            Localize(visualElement, e => ((Label)e).text, (e, s) => ((Label)e).text = s);
        }

        internal static void LocalizeVisualTree(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(LocalizeTooltip);
            root.Query<Label>().ForEach(label =>
            {
                //Ignore text inside ObjectField because it's an asset name
                if (label.ClassListContains("unity-object-field-display__label"))
                    return;
                LocalizeText(label);
            });
        }

        #endregion

        #region Render Pipeline Assets extraction

        internal class GlobalSettingsContainer
        {
            public readonly string name;
            public readonly string path;
            public readonly Type renderPipelineAssetType;
            public readonly SerializedProperty property;
            public readonly SerializedObject serializedObject;

            public GlobalSettingsContainer(string name, string path, Type renderPipelineAssetType, SerializedProperty property, SerializedObject serializedObject)
            {
                this.name = name;
                this.path = path;
                this.renderPipelineAssetType = renderPipelineAssetType;
                this.property = property;
                this.serializedObject = serializedObject;
            }
        }

        internal static bool GatherGlobalSettingsFromSerializedObject(SerializedObject serializedObject, out List<GlobalSettingsContainer> globalSettings)
        {
            var renderPipelineGlobalSettingsMap = serializedObject.FindProperty("m_RenderPipelineGlobalSettingsMap");
            globalSettings = CollectRenderPipelineAssetsByGlobalSettings(renderPipelineGlobalSettingsMap);
            return globalSettings.Count > 0;
        }

        internal static List<GlobalSettingsContainer> CollectRenderPipelineAssetsByGlobalSettings(SerializedProperty renderPipelineGlobalSettingsMap)
        {
            var existedGlobalSettings = new List<GlobalSettingsContainer>();
            for (int i = 0; i < renderPipelineGlobalSettingsMap.arraySize; ++i)
            {
                var globalSettings = GetRenderPipelineGlobalSettingsByIndex(renderPipelineGlobalSettingsMap, i);
                if (TryCreateNewGlobalSettingsContainer(globalSettings, out var globalSettingsContainer))
                    existedGlobalSettings.Add(globalSettingsContainer);
            }

            return existedGlobalSettings;
        }

        internal static bool TryCreateNewGlobalSettingsContainer(RenderPipelineGlobalSettings globalSettings, out GlobalSettingsContainer globalSettingsContainer)
        {
            globalSettingsContainer = null;

            if (globalSettings == null)
                return false;

            var result = TryGetSettingsListFromRenderPipelineGlobalSettings(globalSettings,
                out var globalSettingsSO,
                out var settingsContainer,
                out var settingsListInContainer);
            if (!result || settingsListInContainer.arraySize == 0)
                return false;

            if (!IsAnyRenderPipelineGraphicsSettingsValid(settingsListInContainer))
                return false;

            var globalSettingsType = globalSettings.GetType();
            if (!TryExtractSupportedOnRenderPipelineAttribute(globalSettingsType, out var supportedOnRenderPipelineAttribute, out var message))
            {
                Debug.LogWarning(message);
                return false;
            }

            var tabName = CreateNewTabName(globalSettingsType, supportedOnRenderPipelineAttribute);
            var path = AssetDatabase.GetAssetPath(globalSettings);
            globalSettingsContainer = new GlobalSettingsContainer(tabName, path, supportedOnRenderPipelineAttribute.renderPipelineTypes[0], settingsContainer, globalSettingsSO);
            return true;
        }

        static bool TryGetSettingsListFromRenderPipelineGlobalSettings(RenderPipelineGlobalSettings globalSettings, out SerializedObject globalSettingsSO, out SerializedProperty settingsContainer,
            out SerializedProperty settingsListInContainer)
        {
            globalSettingsSO = new SerializedObject(globalSettings);
            settingsContainer = globalSettingsSO.FindProperty(RenderPipelineGraphicsSettingsManager.serializationPathToContainer);
            if (settingsContainer == null)
            {
                settingsListInContainer = null;
                return false;
            }

            settingsListInContainer = globalSettingsSO.FindProperty(RenderPipelineGraphicsSettingsManager.serializationPathToCollection);
            return settingsListInContainer != null;
        }

        static bool IsAnyRenderPipelineGraphicsSettingsValid(SerializedProperty settingsListInContainer)
        {
            for (int i = 0; i < settingsListInContainer.arraySize; i++)
            {
                var serializedSettings = settingsListInContainer.GetArrayElementAtIndex(i);
                if (serializedSettings.managedReferenceValue is not IRenderPipelineGraphicsSettings settings)
                    continue;

                if (Unsupported.IsDeveloperMode())
                    return true; // TODO: Remove when all HDRP and URP settings have been fully migrated

                if (settings.GetType().GetCustomAttribute<HideInInspector>() == null)
                    return true;
            }

            return false;
        }

        internal static RenderPipelineGlobalSettings GetRenderPipelineGlobalSettingsByIndex(SerializedProperty srpDefaultSettings, int i)
        {
            var property = srpDefaultSettings.GetArrayElementAtIndex(i);
            var second = property.FindPropertyRelative("second");
            var globalSettings = second.objectReferenceValue as RenderPipelineGlobalSettings;
            return globalSettings;
        }

        internal static string CreateNewTabName(Type globalSettingsType, SupportedOnRenderPipelineAttribute supportedOnRenderPipelineAttribute)
        {
            string tabName;
            var inspectorName = globalSettingsType.GetCustomAttribute<DisplayNameAttribute>();
            if (inspectorName != null)
                tabName = inspectorName.DisplayName;
            else
            {
                var pipelineAssetName = supportedOnRenderPipelineAttribute.renderPipelineTypes[0].Name;
                if (pipelineAssetName.EndsWith("Asset", StringComparison.Ordinal))
                    pipelineAssetName = pipelineAssetName[..^"Asset".Length];

                tabName = GetAbbreviation(pipelineAssetName);
            }

            return tabName;
        }

        internal static bool TryExtractSupportedOnRenderPipelineAttribute(Type globalSettingsType, out SupportedOnRenderPipelineAttribute supportedOnRenderPipelineAttribute, out string message)
        {
            supportedOnRenderPipelineAttribute = globalSettingsType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
            if (supportedOnRenderPipelineAttribute == null)
            {
                message =
                    $"Cannot associate {globalSettingsType.FullName} settings with appropriate {nameof(RenderPipelineAsset)} without {nameof(SupportedOnRenderPipelineAttribute)}. Settings will be skipped and not displayed.";
                return false;
            }

            if (supportedOnRenderPipelineAttribute.renderPipelineTypes.Length != 1)
            {
                message =
                    $"{nameof(SupportedOnRenderPipelineAttribute)} for {globalSettingsType.FullName} settings must have exactly one parameter. {nameof(RenderPipelineGlobalSettings)} can only be for 1 {nameof(RenderPipeline)}. Settings will be skipped and not displayed.";
                return false;
            }

            if (supportedOnRenderPipelineAttribute.renderPipelineTypes.Length == 1 && supportedOnRenderPipelineAttribute.renderPipelineTypes[0] == typeof(RenderPipelineAsset))
            {
                message =
                    $"{nameof(SupportedOnRenderPipelineAttribute)} for {globalSettingsType.FullName} settings must have specific non-abstract {nameof(RenderPipelineAsset)} type";
                return false;
            }

            message = string.Empty;
            return true;
        }

        internal static string GetAbbreviation(string text)
        {
            var nameArray = text.ToCharArray();
            var builder = new StringBuilder();
            for (int i = 0; i < nameArray.Length; i++)
            {
                if (char.IsUpper(nameArray[i]))
                    builder.Append(nameArray[i]);
            }

            var abbreviation = builder.ToString();
            return abbreviation.Length == 0 ? text : abbreviation;
        }

        #endregion

        #region UI-relative methods

        //Temp solution until we introduce custom editor support and title support for pipeline assets
        internal static TabButton CreateNewTab(TabbedView tabView, string tabName, VisualElement tabTarget, bool active = false)
        {
            tabTarget.name = $"{tabName}SettingsContainer";
            LocalizeVisualTree(tabTarget);

            var tab = new TabButton(tabName, tabTarget)
            {
                name = $"{tabName}TabButton"
            };
            LocalizeVisualTree(tab);
            tabView.AddTab(tab, active);
            return tab;
        }

        internal static VisualElement CreateRPHelpBox(VisibilityControllerBasedOnRenderPipeline visibilityController, Type currentAssetType)
        {
            var helpBoxTemplate = EditorGUIUtility.Load(GraphicsSettingsInspector.GraphicsSettingsData.helpBoxesTemplateForSRP) as VisualTreeAsset;
            var helpBoxContainer = helpBoxTemplate.Instantiate();
            LocalizeVisualTree(helpBoxContainer);

            var allRenderPipelineAssetTypes = TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>();
            var allAssetsExceptCurrent = new Type[allRenderPipelineAssetTypes.Count];
            for (int j = 0, index = 0; j < allRenderPipelineAssetTypes.Count; j++, index++)
            {
                if (currentAssetType != null && allRenderPipelineAssetTypes[j] == currentAssetType)
                {
                    index--;
                    continue;
                }

                allAssetsExceptCurrent[index] = allRenderPipelineAssetTypes[j] == null ? null : allRenderPipelineAssetTypes[j];
            }

            var infoHelpBox = helpBoxContainer.MandatoryQ<HelpBox>("CurrentPipelineInfoHelpBox");
            var warningHelpBox = helpBoxContainer.MandatoryQ<HelpBox>("CurrentPipelineWarningHelpBox");
            visibilityController.RegisterVisualElement(infoHelpBox, currentAssetType);
            visibilityController.RegisterVisualElement(warningHelpBox, allAssetsExceptCurrent);

            if (Unsupported.IsDeveloperMode())
            {
                helpBoxContainer.Add(new HelpBox($"Developer Mode is enabled. HideInInspector attribute, for {nameof(IRenderPipelineGraphicsSettings)}, will be ignored.", HelpBoxMessageType.Info));
            }

            return helpBoxContainer;
        }

        internal static TabButton GetTabButtonForRenderPipelineAssetType(VisualElement root, Type renderPipelineAssetType)
        {
            return root.Query<TabButton>().Where(t => t.userData as Type == renderPipelineAssetType).First();
        }

        internal static Type GetRenderPipelineAssetTypeForSelectedTab(VisualElement root)
        {
            var tabbedView = root.Q<TabbedView>();
            var currentActiveTab = tabbedView?.ActiveTab;
            return currentActiveTab?.userData as Type;
        }

        internal static void ReloadGraphicsSettingsEditorIfNeeded()
        {
            if (!EditorWindow.HasOpenInstances<ProjectSettingsWindow>())
                return;

            var settingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>(null, false);
            if (settingsWindow.GetCurrentProvider() is GraphicsSettingsProvider provider)
                provider.Reload();
        }

        public static void OpenAndScrollTo(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                throw new ArgumentException(nameof(propertyPath), $"The {nameof(propertyPath)} argument can't be null or empty.");

            var settingsWindow = GetOrOpenGraphicsSettingsWindow(out var wasOpened);
            var root = settingsWindow.rootVisualElement;

            RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(!wasOpened, root, () =>
            {
                if (!TryFindPropertyAndTabByBindingPath(propertyPath, root, out var propertyField, out var tabbedView, out var tabButton))
                {
                    Debug.LogWarning($"Couldn't find a property with bindingPath {propertyPath} in the settings container.");
                    return;
                }

                var scrollView = root.Q<ScrollView>("MainScrollView");
                var isCorrectTabOpenOrNotExist = tabbedView?.ActiveTab == tabButton;
                if (!isCorrectTabOpenOrNotExist)
                    tabbedView?.Activate(tabButton);
                RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(isCorrectTabOpenOrNotExist, tabButton?.Target,
                    () => root.schedule.Execute(() => OpenFoldoutsThenScroll(propertyField, scrollView)).StartingIn(50));
            });
        }

        public static void OpenAndScrollTo(Type renderPipelineGraphicsSettingsType)
        {
            if (renderPipelineGraphicsSettingsType == null)
                throw new ArgumentNullException(nameof(renderPipelineGraphicsSettingsType));
            if (!typeof(IRenderPipelineGraphicsSettings).IsAssignableFrom(renderPipelineGraphicsSettingsType))
                throw new ArgumentException($"{nameof(IRenderPipelineGraphicsSettings)} is not assignable from {nameof(renderPipelineGraphicsSettingsType)}");

            var settingsWindow = GetOrOpenGraphicsSettingsWindow(out var wasOpened);
            var root = settingsWindow.rootVisualElement;

            RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(!wasOpened, settingsWindow.rootVisualElement, () =>
            {
                if (!TryFindTabByType(renderPipelineGraphicsSettingsType, root, out var tabButton, out var bindingPath))
                {
                    Debug.LogWarning($"Couldn't find a tab for {renderPipelineGraphicsSettingsType.Name} type in the settings container.");
                    return;
                }

                var tabbedView = root.Q<TabbedView>();
                var isCorrectTabOpen = tabbedView.ActiveTab == tabButton;
                if (!isCorrectTabOpen)
                    tabbedView.Activate(tabButton);

                RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(isCorrectTabOpen, tabbedView, () =>
                {
                    tabbedView.schedule.Execute(() =>
                    {
                        var target = tabButton.Target;
                        var foundPropertyField = target
                            .Query<PropertyField>()
                            .Where(p => string.CompareOrdinal(p.bindingPath, bindingPath) == 0)
                            .First();
                        if (foundPropertyField == null)
                            return;

                        var scrollView = root.Q<ScrollView>("MainScrollView");
                        OpenFoldoutsThenScroll(foundPropertyField, scrollView);
                    }).StartingIn(50);
                });
            });
        }

        internal static ProjectSettingsWindow GetOrOpenGraphicsSettingsWindow(out bool wasOpened)
        {
            wasOpened = false;
            if (!EditorWindow.HasOpenInstances<ProjectSettingsWindow>())
            {
                wasOpened = true;
                return SettingsService.OpenProjectSettings(GraphicsSettingsProvider.s_GraphicsSettingsProviderPath) as ProjectSettingsWindow;
            }

            var settingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>(null, true);
            if (settingsWindow.GetCurrentProvider() is GraphicsSettingsProvider)
                return settingsWindow;

            wasOpened = true;
            return SettingsService.OpenProjectSettings(GraphicsSettingsProvider.s_GraphicsSettingsProviderPath) as ProjectSettingsWindow;
        }

        static bool TryFindTabByType(Type renderPipelineGraphicsSettingsType, VisualElement root, out TabButton tabButton, out string bindingPath)
        {
            tabButton = null;
            bindingPath = "";

            var tabbedView = root.Q<TabbedView>();
            if (tabbedView == null)
                return false;

            TabButton lambdaTabButton = null;
            string lambdaBindingPath = "";
            EditorGraphicsSettings.ForEachPipelineSettings(gs =>
            {
                if (gs == null)
                    return;

                if (!TryExtractSupportedOnRenderPipelineAttribute(gs.GetType(), out var globalSettingsSupportedOn, out var message))
                    return;
                lambdaTabButton = root.Query<TabButton>().Where(tb =>
                {
                    if (tb.userData is not Type renderPipelineAssetType)
                        return false;

                    return globalSettingsSupportedOn.renderPipelineTypes[0] == renderPipelineAssetType;
                }).First();

                if (lambdaTabButton == null)
                    return;
                var index = gs.IndexOf(renderPipelineGraphicsSettingsType);
                if (index >= 0)
                    lambdaBindingPath = $"{RenderPipelineGraphicsSettingsManager.serializationPathToCollection}.Array.data[{index}]";
            });
            if (!string.IsNullOrEmpty(lambdaBindingPath))
                (tabButton, bindingPath) = (lambdaTabButton, lambdaBindingPath);
            return tabButton != null;
        }

        static bool TryFindPropertyAndTabByBindingPath(string propertyPath, VisualElement root, out PropertyField propertyField, out TabbedView tabbedView, out TabButton tabButton)
        {
            tabButton = null;
            propertyField = null;

            tabbedView = root.Q<TabbedView>();
            if (tabbedView == null)
                propertyField = root.Query<PropertyField>().Where(p => propertyPath.Contains(p.bindingPath)).First();
            else
            {
                TabButton lambdaTabButton = null;
                PropertyField lambdaPropertyField = null;
                root.Query<TabButton>().ForEach(tb =>
                {
                    var target = tb.Target;
                    if (target == null)
                        return;
                    var foundPropertyField = target.Query<PropertyField>().Where(p => propertyPath.Contains(p.bindingPath)).First();
                    if (foundPropertyField != null)
                        (lambdaTabButton, lambdaPropertyField) = (tb, foundPropertyField);
                });

                (tabButton, propertyField) = (lambdaTabButton, lambdaPropertyField);
            }

            return propertyField != null;
        }

        static int s_EventCounter;

        static void OpenFoldoutsThenScroll(PropertyField propertyField, ScrollView scrollView)
        {
            var current = propertyField.parent;

            s_EventCounter = 0;
            while (current != null)
            {
                if (current is Foldout { value: false } foldout)
                {
                    s_EventCounter++;
                    foldout.contentContainer.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
                    {
                        s_EventCounter--;
                        if (s_EventCounter == 0)
                        {
                            scrollView.UpdateScrollers(false, true);
                            ScrollTo(propertyField, scrollView);
                        }
                    });
                    foldout.value = true;
                }

                current = current.parent;
            }

            if (s_EventCounter == 0)
                ScrollTo(propertyField, scrollView);
        }

        static void ScrollTo(PropertyField propertyField, ScrollView scrollView)
        {
            scrollView.ScrollTo(propertyField);
            propertyField.AddToClassList("graphics-settings__highlightable--selection-animation");
            propertyField.RegisterCallbackOnce<TransitionEndEvent>(evt => propertyField.RemoveFromClassList("graphics-settings__highlightable--selection-animation"));
        }

        static void RegisterCallbackOnceOrCallImmediately<T>(bool immediateCondition, VisualElement element, Action callback)
            where T : EventBase<T>, new()
        {
            if (immediateCondition)
                callback?.Invoke();
            else
                element.RegisterCallbackOnce<T>(e => callback?.Invoke());
        }

        #endregion
    }
}
