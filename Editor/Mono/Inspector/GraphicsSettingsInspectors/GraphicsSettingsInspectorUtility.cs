// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Settings;
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
            if (settingsWindow.GetCurrentProvider() is not GraphicsSettingsProvider provider)
                return;

            if(provider.inspector == null)
                return;

            provider.Reload();
        }

        internal static int ComputeRenderPipelineGlobalSettingsListHash(List<GlobalSettingsContainer> settingsContainers)
        {
            bool haveSettings = settingsContainers is { Count: > 0 };
            if (!haveSettings)
                return 0;

            var currentHash = new HashCode();
            currentHash.Add(GraphicsSettings.currentRenderPipelineAssetType?.ToString() ?? "");
            foreach (var globalSettings in settingsContainers)
            {
                TryGetSettingsListFromRenderPipelineGlobalSettings(
                    globalSettings.serializedObject.targetObject as RenderPipelineGlobalSettings,
                    out SerializedObject _,
                    out SerializedProperty _,
                    out SerializedProperty settingsListInContainer);
                currentHash.Add(settingsListInContainer.contentHash);
            }
            return currentHash.ToHashCode();
        }

        #endregion

        #region OpenAndScrollTo

        const string highlightableClass = "graphics-settings__highlightable";
        const string highlightableColorClass = "graphics-settings__highlightable--background-color";

        static int s_EventCounter;
        static VisualElement s_SearchedElement;
        static readonly List<Foldout> k_Foldouts = new();

        public static void OpenAndScrollTo(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                throw new ArgumentException(nameof(propertyPath), $"The {nameof(propertyPath)} argument can't be null or empty.");

            OpenAndScrollTo<PropertyField, PropertyField>(
                root => TryFindPropertyAndTabByBindingPath(propertyPath, root, out var tabbedView, out var tabButton, out var propertyField)
                    ? (true, tabbedView, tabButton, propertyField)
                    : (false, null, null, null),
                () => $"Couldn't find a property with bindingPath {propertyPath} in the settings container.",
                (scrollView, _, propertyField, _) => OpenFoldoutsThenScroll(propertyField, scrollView));
        }

        public static void OpenAndScrollTo(Type renderPipelineGraphicsSettingsType)
        {
            OpenAndScrollTo<PropertyField>(renderPipelineGraphicsSettingsType);
        }

        public static void OpenAndScrollTo<TGraphicsSettings>(string propertyPath = "")
            where TGraphicsSettings : IRenderPipelineGraphicsSettings
        {
            OpenAndScrollTo<TGraphicsSettings, PropertyField>(p => p.bindingPath.Contains(propertyPath));
        }

        public static void OpenAndScrollTo<TGraphicsSettings, TVisualElement>(Func<TVisualElement, bool> subElementFunc = null)
            where TGraphicsSettings : IRenderPipelineGraphicsSettings
            where TVisualElement : VisualElement
        {
            OpenAndScrollTo(typeof(TGraphicsSettings), subElementFunc);
        }

        /// <summary>
        /// Open the Graphics Settings window and scroll to the specified Render Pipeline Graphics Settings type.
        /// </summary>
        /// <param name="renderPipelineGraphicsSettingsType">Type of the IRenderPipelineGraphicsSettings to search for.</param>
        /// <param name="subElementFunc">Provide a Func to search for sub element in the IRenderPipelineGraphicsSettings drawer.</param>
        /// <typeparam name="TVisualElement">Type of the VisualElement to searcg for.</typeparam>
        /// <exception cref="ArgumentNullException">Throw if a renderPipelineGraphicsSettingsType is null.</exception>
        /// <exception cref="ArgumentException">Throw if a renderPipelineGraphicsSettingsType type is not derived from IRenderPipelineGraphicsSettings.</exception>
        internal static void OpenAndScrollTo<TVisualElement>(Type renderPipelineGraphicsSettingsType, Func<TVisualElement, bool> subElementFunc = null)
            where TVisualElement : VisualElement
        {
            if (renderPipelineGraphicsSettingsType == null)
                throw new ArgumentNullException(nameof(renderPipelineGraphicsSettingsType));
            if (!typeof(IRenderPipelineGraphicsSettings).IsAssignableFrom(renderPipelineGraphicsSettingsType))
                throw new ArgumentException($"{nameof(IRenderPipelineGraphicsSettings)} is not assignable from {nameof(renderPipelineGraphicsSettingsType)}");

            OpenAndScrollTo(
                root => TryFindTabByType(renderPipelineGraphicsSettingsType, root, out var tabbedView, out var tabButton, out var bindingPath)
                    ? (true, tabbedView, tabButton, bindingPath)
                    : (false, tabbedView, null, null),
                () => $"Couldn't find a tab for {renderPipelineGraphicsSettingsType.Name} type in the settings container.",
                SearchForVisualElementInTabsAndScroll,
                subElementFunc);
        }

        /// <summary>
        /// Open the Graphics Settings window and scroll to the field specified by the set of methods.
        /// </summary>
        /// <param name="searchForVisualElement">Search for a visual element that corresponds to the propertyPath or IRenderPipelineGraphicsSettings type.</param>
        /// <param name="message">Message to log if the visual element wasn't found. It will also return tabbed view if exists and tab button if it contains a visual element.</param>
        /// <param name="execute">Execute the method to scroll to the visual element.</param>
        /// <param name="subElementFunc">Provide a Func to search for sub element in the IRenderPipelineGraphicsSettings drawer.</param>
        /// <typeparam name="TVisualElement">Type of the VisualElement to search for.</typeparam>
        /// <typeparam name="TResult">Type of the result data. Either binding path of the property field or property field itself.</typeparam>
        static void OpenAndScrollTo<TVisualElement, TResult>(
            Func<VisualElement, (bool, TabbedView, TabButton, TResult)> searchForVisualElement,
            Func<string> message, Action<ScrollView, TabButton, TResult, Func<TVisualElement, bool>> execute,
            Func<TVisualElement, bool> subElementFunc = null)
            where TVisualElement : VisualElement
        {
            var settingsWindow = GetOrOpenGraphicsSettingsWindow(out var previousWindowState);
            var root = settingsWindow.rootVisualElement;

            //Wait for the window to layout if a window changed provider or open first time and then find the tabs.
            RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(previousWindowState is PreviousWindowState.Opened or PreviousWindowState.IncorrectProvider, settingsWindow.rootVisualElement, () =>
            {
                var (result, tabbedView, tabButton, resultedData) = searchForVisualElement.Invoke(root);
                if (!result)
                {
                    Debug.LogWarning(message.Invoke());
                    return;
                }

                var isTabbedViewNull = tabbedView == null;
                var isCorrectTabOpen = isTabbedViewNull || tabButton == null || tabbedView.ActiveTab == tabButton;
                var scrollView = root.Q<ScrollView>("MainScrollView");

                //Wait for the tab to layout if active tab was switched and then search for the VisualElement. Added a special check for the built-in settings because it will produce only one event when tab switch to it.
                RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(isTabbedViewNull || previousWindowState is PreviousWindowState.Opened, tabbedView,
                    () => RegisterCallbackOnceOrCallImmediately<GeometryChangedEvent>(isTabbedViewNull || isCorrectTabOpen || tabbedView.ActiveTab.userData as string == GraphicsSettingsInspector.GraphicsSettingsData.builtIn, tabbedView,
                        () => root.schedule.Execute(() => execute.Invoke(scrollView, tabButton, resultedData, subElementFunc)).StartingIn(500)));

                if (!isTabbedViewNull && !isCorrectTabOpen)
                    tabbedView.Activate(tabButton);
            });
        }

        /// <summary>
        /// Get or open the Graphics Settings window and focus on it.
        /// </summary>
        /// <param name="previousWindowState"> The state of the window before the method was called.</param>
        /// <returns>ProjectSettings window with selected Graphics Settings page.</returns>
        internal static ProjectSettingsWindow GetOrOpenGraphicsSettingsWindow(out PreviousWindowState previousWindowState)
        {
            previousWindowState = PreviousWindowState.Opened;
            if (!EditorWindow.HasOpenInstances<ProjectSettingsWindow>())
            {
                previousWindowState = PreviousWindowState.NotOpened;
                return SettingsService.OpenProjectSettings(GraphicsSettingsProvider.s_GraphicsSettingsProviderPath) as ProjectSettingsWindow;
            }

            var settingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>(null, true);
            if (settingsWindow.GetCurrentProvider() is GraphicsSettingsProvider && settingsWindow.rootVisualElement != null)
                return settingsWindow;

            previousWindowState = PreviousWindowState.IncorrectProvider;
            settingsWindow = SettingsService.OpenProjectSettings(GraphicsSettingsProvider.s_GraphicsSettingsProviderPath) as ProjectSettingsWindow;
            settingsWindow.Show();
            return settingsWindow;
        }

        /// <summary>
        /// Find the PropertyField and TabbedView by binding path.
        /// </summary>
        /// <param name="propertyPath">Property Path of the element.</param>
        /// <param name="root">Root VisualElement to search for Tabs.</param>
        /// <param name="tabbedView">Visual Element that contains all tabs and bodies. Null if there's none.</param>
        /// <param name="tabButton">Tab that contains property field. Null if there's none.</param>
        /// <param name="propertyField">Property Field the contains provided property path.</param>
        /// <returns>True if the search was successful.</returns>
        static bool TryFindPropertyAndTabByBindingPath(string propertyPath, VisualElement root, out TabbedView tabbedView, out TabButton tabButton, out PropertyField propertyField)
        {
            tabbedView = root.Q<TabbedView>();
            tabButton = null;

            propertyField = root.Query<PropertyField>().Where(p => propertyPath.Contains(p.bindingPath)).First();
            if (propertyField == null)
                return false;

            if (tabbedView != null)
                tabButton = propertyField.GetFirstAncestorOfType<TabButton>();
            return true;
        }

        /// <summary>
        /// Find the tab and binding path for the Render Pipeline Graphics Settings type.
        /// </summary>
        /// <param name="renderPipelineGraphicsSettingsType">Type of the IRenderPipelineGraphicsSettings</param>
        /// <param name="root">Root VisualElement to search for the TabbedView.</param>
        /// <param name="tabbedView">TabbedView that contains all tabs.</param>
        /// <param name="tabButton">Tab which contains IRenderPipelineGraphicsSettings.</param>
        /// <param name="bindingPath">Real bindingPath of the IRenderPipelineGraphicsSettings.</param>
        /// <returns>True if the search was successful.</returns>
        static bool TryFindTabByType(Type renderPipelineGraphicsSettingsType, VisualElement root, out TabbedView tabbedView, out TabButton tabButton, out string bindingPath)
        {
            tabButton = null;
            bindingPath = "";

            tabbedView = root.Q<TabbedView>();
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
                var tabButton = root.Query<TabButton>().Where(tb =>
                {
                    if (tb.userData is not Type renderPipelineAssetType)
                        return false;

                    return globalSettingsSupportedOn.renderPipelineTypes[0] == renderPipelineAssetType;
                }).First();

                if (tabButton == null)
                    return;

                var index = gs.IndexOf(renderPipelineGraphicsSettingsType);
                if (index < 0)
                    return;

                lambdaTabButton = tabButton;
                lambdaBindingPath = $"{RenderPipelineGraphicsSettingsManager.serializationPathToCollection}.Array.data[{index}]";
            });
            if (!string.IsNullOrEmpty(lambdaBindingPath))
                (tabButton, bindingPath) = (lambdaTabButton, lambdaBindingPath);
            return tabButton != null;
        }

        /// <summary>
        /// Search for the VisualElement in the tabs and scroll to it.
        /// </summary>
        /// <param name="subElementFunc">Method to search for sub element of the IRenderPipelineGraphicsSettings.</param>
        /// <param name="tabButton">Tab to search for the VisualElement.</param>
        /// <param name="bindingPath">Binding path of the IRenderPipelineGraphicsSettings.</param>
        /// <param name="root">Root VisualElement to search for the Main ScrollView.</param>
        /// <typeparam name="TVisualElement">Type of the VisualElement to search for.</typeparam>
        static void SearchForVisualElementInTabsAndScroll<TVisualElement>(ScrollView scrollView, TabButton tabButton, string bindingPath, Func<TVisualElement, bool> subElementFunc)
            where TVisualElement : VisualElement
        {
            var target = tabButton.Target;
            VisualElement field = target
                .Query<PropertyField>()
                .Where(p => string.CompareOrdinal(p.bindingPath, bindingPath) == 0)
                .First();

            if (field == null)
                return;

            // If the subElementFunc is null, we just want to scroll to the field.
            if (subElementFunc == null)
            {
                OpenFoldoutsThenScroll(field, scrollView);
                return;
            }

            var subElement = field.Query<TVisualElement>().Where(subElementFunc.Invoke).First();
            // If the subElement is found, we just want to scroll to it.
            if (subElement != null)
            {
                OpenFoldoutsThenScroll(subElement, scrollView);
                return;
            }

            // This could mean that the field hidden in the Foldout.
            // Let's try to find the Foldout and open it.
            s_SearchedElement = null;
            k_Foldouts.Clear();
            field.Query<Foldout>().Where(f => !f.value).ForEach(
                f => ExpandFoldoutAndChildren(f, subElementFunc, foundElement =>
                {
                    s_SearchedElement = null;
                    k_Foldouts.Clear();
                    OpenFoldoutsThenScroll(foundElement, scrollView);
                }));
        }

        /// <summary>
        /// Expand a foldout and all child's foldout until the searched element is found.
        /// </summary>
        /// <param name="foldout">Foldout to expand.</param>
        /// <param name="subElementFunc">Condition to determine correct Visual Element.</param>
        /// <param name="onFinished">When correct VisualElement found then call this method with it.</param>
        /// <typeparam name="TVisualElement">VisualElement type to search for.</typeparam>
        static void ExpandFoldoutAndChildren<TVisualElement>(Foldout foldout, Func<TVisualElement, bool> subElementFunc, Action<VisualElement> onFinished)
            where TVisualElement : VisualElement
        {
            if(s_SearchedElement != null)
                return;

            k_Foldouts.Add(foldout);
            foldout.value = true;

            //We use Execute and Delay with starting 1 to be sure that foldouts loaded and layouted.
            foldout.schedule.Execute(() =>
            {
                s_SearchedElement = foldout.Query<TVisualElement>().Where(subElementFunc.Invoke).First();
                if (s_SearchedElement != null)
                {
                    foreach (var openedFoldout in k_Foldouts)
                    {
                        if (!openedFoldout.Contains(s_SearchedElement))
                            openedFoldout.value = false;
                    }

                    onFinished.Invoke(s_SearchedElement);
                    return;
                }

                var childFoldouts = foldout.Query<Foldout>().ToList();
                foreach (var childFoldout in childFoldouts)
                {
                    ExpandFoldoutAndChildren(childFoldout, subElementFunc, onFinished);
                }
            }).StartingIn(1);
        }

        /// <summary>
        /// Open all Foldouts and scroll to the provided field.
        /// </summary>
        /// <param name="field">Field to check for open foldouts in parents.</param>
        /// <param name="scrollView">Main Scroll View of the Graphics Settings page.</param>
        /// <typeparam name="T">Type of the VisualElement.</typeparam>
        static void OpenFoldoutsThenScroll<T>(T field, ScrollView scrollView)
            where T : VisualElement
        {
            var current = field.parent;

            s_EventCounter = 0;
            while (current != null)
            {
                if (current is Foldout { value: false } foldout)
                {
                    s_EventCounter++;
                    foldout.contentContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                    {
                        s_EventCounter--;
                        if (s_EventCounter != 0)
                            return;

                        scrollView.UpdateScrollers(false, true);
                        ScrollTo(field, scrollView);
                    });
                    foldout.value = true;
                }

                current = current.parent;
            }

            if (s_EventCounter == 0)
                ScrollTo(field, scrollView);
        }

        /// <summary>
        /// Scroll to the provided field and highlight it.
        /// </summary>
        /// <param name="field">Provided VisualElement.</param>
        /// <param name="scrollView">Main Scroll View of the Graphics Settings page.</param>
        /// <typeparam name="T">Type of the VisualElement.</typeparam>
        static void ScrollTo<T>(T field, ScrollView scrollView)
            where T : VisualElement
        {
            scrollView.ScrollTo(field);
            field.AddToClassList(highlightableClass);
            field.AddToClassList(highlightableColorClass);
            field.RegisterCallbackOnce<TransitionEndEvent>(_ =>
            {
                field.RemoveFromClassList(highlightableColorClass);
                field.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                    field.RemoveFromClassList(highlightableClass));
            });
        }

        /// <summary>
        /// Utility method to register a callback once or call it immediately if the condition is true.
        /// </summary>
        /// <param name="immediateCondition">Condition to check.</param>
        /// <param name="element">Subscribe to the event on this VisualElement.</param>
        /// <param name="callback">Call this callback when finished.</param>
        /// <typeparam name="T">Type of the event to subscribe.</typeparam>
        static void RegisterCallbackOnceOrCallImmediately<T>(bool immediateCondition, VisualElement element, Action callback)
            where T : EventBase<T>, new()
        {
            if (immediateCondition)
                callback?.Invoke();
            else
                element.RegisterCallbackOnce<T>(_ => callback?.Invoke());
        }

        /// <summary>
        /// State of the window before GetOrOpenGraphicsSettingsWindow method was called. Used to determine how many layout events we expect.
        /// </summary>
        internal enum PreviousWindowState
        {
            /// <summary>
            /// Window was not opened before.
            /// </summary>
            NotOpened,
            /// <summary>
            /// Window had different page open. We need to wait for the Graphics Settings page to open and layout.
            /// </summary>
            IncorrectProvider,
            /// <summary>
            /// Window was already opened with the Graphics Settings page.
            /// </summary>
            Opened
        }
        #endregion
    }
}
