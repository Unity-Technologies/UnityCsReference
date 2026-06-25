// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.UIToolkit.Editor;

[UsedImplicitly]
internal class UIToolkitAuthoringSettingsProvider : IUIToolkitSettingsProviderExtension
{
    private const string k_SettingsPath = "Project/UI Toolkit";

    private const string k_EnableUIInSceneAuthoring = "Enable in Scene UI Authoring";
    private const string k_HierarchyDisplayOptionsText = "Hierarchy Display Options";
    private const string k_DisplayTypenameOptionsText = "Always Display Typename";
    private const string k_DisplayUssClassOptionsText = "Display USS classes";
    private const string k_UIDocumentCreationOptionsText = "UI Document Creation Options";
    private const string k_SelectionOptionsText = "Selection Options";
    private const string k_NewVisualTreeAssetLocationText = "New UI Document Location";
    private const string k_AutoOpenWindowsText = "Auto-Open Windows Options";
    private const string k_AutoOpenUIViewportWindowText = "UI Viewport";
    private const string k_AutoOpenStyleSheetsWindowText = "Style Sheets";
    private const string k_RectangleSelectionModeText = "Rectangle Selection Mode";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Settings/UIToolkitAuthoringSettings.uxml";

    VisualElement UIAuthoringSection;

    int IUIToolkitSettingsProviderExtension.order => 100;

    internal static void OpenSettings()
    {
        SettingsService.OpenProjectSettings(k_SettingsPath);
    }

    bool IUIToolkitSettingsProviderExtension.HasSearchInterestHandler(string searchContext)
    {
        if (k_EnableUIInSceneAuthoring.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_HierarchyDisplayOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_DisplayTypenameOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_DisplayUssClassOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_UIDocumentCreationOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_SelectionOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_NewVisualTreeAssetLocationText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_RectangleSelectionModeText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_AutoOpenWindowsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_AutoOpenUIViewportWindowText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_AutoOpenStyleSheetsWindowText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        return false;
    }

    void IUIToolkitSettingsProviderExtension.OnActivate(string searchContext, VisualElement rootElement)
    {
        BuildSettings(rootElement);
    }

    internal void BuildSettings(VisualElement rootElement)
    {
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(rootElement);

        var newHierarchyHelpBox = rootElement.Q<HelpBox>("EnableHv2HelpBox");
        newHierarchyHelpBox.onButtonClicked += () =>
        {
            EditorSettings.useLegacyHierarchy = false;
            UpdateNewHierarchyHelpBox();
        };

        var enableInSceneAuthoring = rootElement.Q<Toggle>("uitoolkit-authoring-settings__enable-scene-authoring");
        enableInSceneAuthoring.value = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring;
        enableInSceneAuthoring.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.EnableInSceneUIAuthoring = evt.newValue;
            UpdateNewHierarchyHelpBox();
        });
        UpdateNewHierarchyHelpBox();

        UIAuthoringSection = rootElement.Q("uitoolkit-authoring-settings__settings-container");

        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += CheckUIAuthoringOptions;
        CheckUIAuthoringOptions(UIToolkitAuthoringSettings.EnableInSceneUIAuthoring);

        var displayTypenameOptions = rootElement.Q<Toggle>("uitoolkit-authoring-settings__always-display-typename");
        displayTypenameOptions.value = UIToolkitAuthoringSettings.DisplayOptions.HasFlag(UIHierarchyDisplayOptions.Typename);
        displayTypenameOptions.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
                UIToolkitAuthoringSettings.DisplayOptions |= UIHierarchyDisplayOptions.Typename;
            else
                UIToolkitAuthoringSettings.DisplayOptions &= ~UIHierarchyDisplayOptions.Typename;
        });

        var displayUssClassesOptions = rootElement.Q<Toggle>("uitoolkit-authoring-settings__display-uss-classes");
        displayUssClassesOptions.value = UIToolkitAuthoringSettings.DisplayOptions.HasFlag(UIHierarchyDisplayOptions.UssClasses);
        displayUssClassesOptions.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
                UIToolkitAuthoringSettings.DisplayOptions |= UIHierarchyDisplayOptions.UssClasses;
            else
                UIToolkitAuthoringSettings.DisplayOptions &= ~UIHierarchyDisplayOptions.UssClasses;
        });

        var newVtaLocation = rootElement.Q<EnumField>("uitoolkit-authoring-settings__new-document-location");
        newVtaLocation.Init(UIToolkitAuthoringSettings.NewVisualTreeAssetLocation);
        newVtaLocation.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.NewVisualTreeAssetLocation = (NewVisualTreeAssetLocation)evt.newValue;
        });

        var autoOpenUIViewport = rootElement.Q<EnumField>("uitoolkit-authoring-settings__auto-open-window--viewport");
        autoOpenUIViewport.Init(UIToolkitAuthoringSettings.AutoOpenUIViewportWindow);
        autoOpenUIViewport.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.AutoOpenUIViewportWindow = (AutoOpenMode)evt.newValue;
        });

        var autoOpenStyleSheets = rootElement.Q<EnumField>("uitoolkit-authoring-settings__auto-open-window--style-sheet");
        autoOpenStyleSheets.Init(UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow);
        autoOpenStyleSheets.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow = (AutoOpenMode)evt.newValue;
        });

        var rectangleSelectionMode = rootElement.Q<EnumField>("uitoolkit-authoring-settings__rectangle-selection-mode");
        rectangleSelectionMode.Init(UIToolkitAuthoringSettings.RectangleSelectionMode);
        rectangleSelectionMode.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.RectangleSelectionMode = (RectangleSelectionMode)evt.newValue;
        });

        void UpdateNewHierarchyHelpBox()
        {
            var show = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
                       && EditorSettings.useLegacyHierarchy;
            newHierarchyHelpBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    void IUIToolkitSettingsProviderExtension.OnDeactivate()
    {
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= CheckUIAuthoringOptions;
    }

    void CheckUIAuthoringOptions(bool enabled)
    {
        UIAuthoringSection.SetEnabled(enabled);
    }
}
