// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SelectionWindowFooter : VisualElement
{
    public event Action onAllButtonClicked = delegate {};
    public event Action onNoneButtonClicked = delegate {};
    public event Action onActionButtonClicked = delegate {};
    public event Action onCancelButtonClicked = delegate {};

    private Button m_ActionButton;

    public SelectionWindowFooter()
    {
        var separator = new VisualElement { name = "separator", classList = { "separator" } };
        Add(separator);

        var footer = new VisualElement { name = "footer", classList = { "footer" } };
        Add(footer);

        var leftSection = new VisualElement { name = "leftSection", classList = { "left-section" } };
        footer.Add(leftSection);

        var allButton = new Button { name = "allButton", text = "All", tabIndex = -1, displayTooltipWhenElided = true };
        allButton.clicked += () => onAllButtonClicked?.Invoke();
        leftSection.Add(allButton);

        var noneButton = new Button { name = "noneButton", text = "None", tabIndex = -1, displayTooltipWhenElided = true };
        noneButton.clicked += () => onNoneButtonClicked?.Invoke();
        leftSection.Add(noneButton);

        var rightSection = new VisualElement { name = "rightSection", classList = { "right-section" } };
        footer.Add(rightSection);

        m_ActionButton = new Button { name = "actionButton", text = "Action", tabIndex = -1, displayTooltipWhenElided = true };
        m_ActionButton.clicked += () => onActionButtonClicked?.Invoke();
        rightSection.Add(m_ActionButton);

        var cancelButton = new Button { name = "cancelButton", text = "Cancel", tabIndex = -1, displayTooltipWhenElided = true };
        cancelButton.clicked += () => onCancelButtonClicked?.Invoke();
        rightSection.Add(cancelButton);
    }

    public void SetData(string actionName)
    {
        m_ActionButton.text = actionName;
    }

    public void SetActionEnabled(bool isEnabled)
    {
        m_ActionButton.SetEnabled(isEnabled);
    }
}
