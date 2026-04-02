// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class MultiSelectFoldoutBase<SingleType, BulkType> : VisualElement, IMultiSelectFoldoutElement<BulkType>
{
    public string headerTextTemplate { get; set; }

    public bool isExpanded => m_Toggle.value;

    private List<BulkType> m_Items = new();
    public List<BulkType> items => m_Items;

    public ActionBase<SingleType, BulkType> action;

    private readonly Toggle m_Toggle;
    private readonly  VisualElement m_Container;
    private readonly  ToolbarButtonBase<SingleType, BulkType> m_Button;

    public MultiSelectFoldoutBase(ActionBase<SingleType, BulkType> action = null)
    {
        m_Toggle = new Toggle { name = "multiSelectFoldoutToggle" }.WithClassList("containerTitle", "expander");
        m_Container = new VisualElement { name = "multiSelectFoldoutContainer" };
        Add(m_Toggle);
        Add(m_Container);

        SetExpanded(false);
        m_Toggle.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));

        this.action = action;
         m_Button = action?.CreateToolbarButton();

        if (m_Button != null)
            m_Toggle.Add(m_Button.element);
    }

    public void Refresh()
    {
        var isVisible = m_Items.Count > 0;
        UIUtils.SetElementDisplay(this, isVisible);
        if (!isVisible)
            return;

        RefreshHeader();
        RefreshContent();
        m_Button?.Refresh(m_Items);
    }

    private void RefreshHeader()
    {
        if (string.IsNullOrEmpty(headerTextTemplate))
            return;
        var numItemsText = string.Format(m_Items.Count > 1 ? L10n.Tr("{0} items") : L10n.Tr("{0} item"), m_Items.Count);
        m_Toggle.text = string.Format(headerTextTemplate, numItemsText);
    }

    private void RefreshContent()
    {
        var expanded = isExpanded;
        UIUtils.SetElementDisplay(m_Container, expanded);
        if (!expanded)
            return;

        m_Container.Clear();
        foreach (var item in m_Items)
            m_Container.Add(CreateMultiSelectItem(item));
    }

    protected abstract MultiSelectItemBase<BulkType> CreateMultiSelectItem(BulkType item);

    // Most foldouts are controlled by the group it belongs to, hence AddItem always return true.
    // For special standalone foldouts, the function should be overridden to provide their own logic.
    public virtual bool AddItem(BulkType item)
    {
        m_Items.Add(item);
        return true;
    }

    public void ClearItems()
    {
        m_Items.Clear();
    }

    public void SetExpanded(bool expanded)
    {
        if (m_Toggle.value != expanded)
            m_Toggle.SetValueWithoutNotify(expanded);

        RefreshContent();
    }
}
