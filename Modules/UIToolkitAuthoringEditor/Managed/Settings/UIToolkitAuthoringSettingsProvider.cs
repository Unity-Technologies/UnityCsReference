// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.UIToolkit.Editor;

[UsedImplicitly]
internal class UIToolkitAuthoringSettingsProvider : IUIToolkitSettingsProviderExtension
{
    private const string k_EnableHierarchyIntegrationText = "Enable Hierarchy Integration";
    private const string k_DisplayOptionsText = "Display Options";
    private const string k_DisplayTypenameOptionsText = "Always Display Typename";
    private const string k_DisplayUssClassOptionsText = "Display USS classes";

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
    }

    void IUIToolkitSettingsProviderExtension.OnDeactivate()
    {
    }
}
