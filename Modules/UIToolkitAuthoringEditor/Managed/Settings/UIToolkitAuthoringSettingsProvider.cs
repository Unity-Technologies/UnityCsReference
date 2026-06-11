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
    private const string k_EnableHierarchyIntegrationText = "Enable Hierarchy Integration";
    private const string k_EnableUIStagesText = "Enable UI Stages";
    private const string k_DisplayOptionsText = "Display Options";
    private const string k_DisplayTypenameOptionsText = "Always Display Typename";
    private const string k_DisplayUssClassOptionsText = "Display USS classes";
    private const string k_NewVisualTreeAssetLocationText = "New UI Document Location";
    private const string k_NewVisualTreeAssetLocationTooltip =
        "Where to place the new UXML when an Add Element action needs to create one. " +
        "Ask for location: prompt with a save dialog. Use current folder: place it in the active Project window folder without prompting.";
    private const string k_AutoOpenWindowsText = "Auto-Open Windows";
    private const string k_AutoOpenUIViewportWindowText = "UI Viewport";
    private const string k_AutoOpenStyleSheetsWindowText = "Style Sheets";
    private const string k_RectangleSelectionModeText = "Rectangle Selection Mode";

    int IUIToolkitSettingsProviderExtension.order => 100;

    bool IUIToolkitSettingsProviderExtension.HasSearchInterestHandler(string searchContext)
    {
        if (k_EnableHierarchyIntegrationText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_DisplayOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_DisplayTypenameOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
            return true;
        if (k_DisplayUssClassOptionsText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
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
        var header = new Label("UI Authoring");
        header.AddToClassList("uitoolkit-settings-header");
        header.style.paddingTop = 20;
        rootElement.Add(header);

        var hierarchyIntegration = new Toggle()
        {
            text = k_EnableHierarchyIntegrationText,
            value = UIToolkitAuthoringSettings.EnableHierarchyIntegration
        };

        hierarchyIntegration.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.EnableHierarchyIntegration = evt.newValue;
        });

        rootElement.Add(hierarchyIntegration);

        var uiStagesIntegration = new Toggle()
        {
            text = k_EnableUIStagesText,
            value = UIToolkitAuthoringSettings.EnableUIStages
        };

        uiStagesIntegration.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.EnableUIStages = evt.newValue;
        });

        if (Unsupported.IsSourceBuild())
            rootElement.Add(uiStagesIntegration);

        var displayOptions = new Label(k_DisplayOptionsText);
        displayOptions.style.paddingTop = 20;
        displayOptions.AddToClassList("uitoolkit-settings-advanced-header");
        rootElement.Add(displayOptions);

        var displayTypenameOptions = new Toggle()
        {
            label = k_DisplayTypenameOptionsText,
            value = UIToolkitAuthoringSettings.DisplayOptions.HasFlag(UIHierarchyDisplayOptions.Typename)
        };

        displayTypenameOptions.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
                UIToolkitAuthoringSettings.DisplayOptions |= UIHierarchyDisplayOptions.Typename;
            else
                UIToolkitAuthoringSettings.DisplayOptions &= ~UIHierarchyDisplayOptions.Typename;
        });

        rootElement.Add(displayTypenameOptions);
        displayTypenameOptions.AddToClassList(Toggle.alignedFieldUssClassName);

        var displayUssClassesOptions = new Toggle()
        {
            label = k_DisplayUssClassOptionsText,
            value = UIToolkitAuthoringSettings.DisplayOptions.HasFlag(UIHierarchyDisplayOptions.UssClasses)
        };

        displayUssClassesOptions.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue)
                UIToolkitAuthoringSettings.DisplayOptions |= UIHierarchyDisplayOptions.UssClasses;
            else
                UIToolkitAuthoringSettings.DisplayOptions &= ~UIHierarchyDisplayOptions.UssClasses;
        });

        rootElement.Add(displayUssClassesOptions);
        displayUssClassesOptions.AddToClassList(Toggle.alignedFieldUssClassName);

        var newVtaLocation = new EnumField(k_NewVisualTreeAssetLocationText, UIToolkitAuthoringSettings.NewVisualTreeAssetLocation)
        {
            tooltip = k_NewVisualTreeAssetLocationTooltip,
        };
        newVtaLocation.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.NewVisualTreeAssetLocation = (NewVisualTreeAssetLocation)evt.newValue;
        });
        rootElement.Add(newVtaLocation);
        newVtaLocation.AddToClassList(EnumField.alignedFieldUssClassName);

        var autoOpenHeader = new Label(k_AutoOpenWindowsText);
        autoOpenHeader.style.paddingTop = 20;
        autoOpenHeader.AddToClassList("uitoolkit-settings-advanced-header");
        rootElement.Add(autoOpenHeader);

        var autoOpenUIViewport = new EnumField(k_AutoOpenUIViewportWindowText, UIToolkitAuthoringSettings.AutoOpenUIViewportWindow)
        {
            style = { marginRight = 8 }
        };
        autoOpenUIViewport.AddToClassList(EnumField.alignedFieldUssClassName);
        autoOpenUIViewport.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.AutoOpenUIViewportWindow = (AutoOpenMode)evt.newValue;
        });
        if (Unsupported.IsSourceBuild())
            rootElement.Add(autoOpenUIViewport);

        var autoOpenStyleSheets = new EnumField(k_AutoOpenStyleSheetsWindowText, UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow)
        {
            style = { marginRight = 8 }
        };
        autoOpenStyleSheets.AddToClassList(EnumField.alignedFieldUssClassName);
        autoOpenStyleSheets.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.AutoOpenStyleSheetsWindow = (AutoOpenMode)evt.newValue;
        });
        if (Unsupported.IsSourceBuild())
            rootElement.Add(autoOpenStyleSheets);

        var rectangleSelectionMode = new EnumField(UIToolkitAuthoringSettings.RectangleSelectionMode)
        {
            label = k_RectangleSelectionModeText,
        };
        rectangleSelectionMode.style.paddingTop = 20;
        rectangleSelectionMode.RegisterValueChangedCallback(evt =>
        {
            UIToolkitAuthoringSettings.RectangleSelectionMode = (RectangleSelectionMode)evt.newValue;
        });
        rootElement.Add(rectangleSelectionMode);
        rectangleSelectionMode.AddToClassList(EnumField.alignedFieldUssClassName);
    }

    void IUIToolkitSettingsProviderExtension.OnDeactivate()
    {
    }
}
