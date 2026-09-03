// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Searchable popup used by the components inspector to add a UI Toolkit component to the selected
/// element, in the style of the GameObject "Add Component" button. The caller supplies the already
/// filtered list of component types (compatible owner type, not already attached).
/// </summary>
sealed class AddComponentDropdown : AdvancedDropdown
{
    static readonly string k_TitleText = L10n.Tr("Add UI Component", null);
    static readonly string k_FallbackNameText = L10n.Tr("Component", null);

    readonly IReadOnlyList<UxmlSerializedDataDescription> m_Candidates;
    readonly Action<UxmlSerializedDataDescription> m_OnSelected;

    public AddComponentDropdown(IReadOnlyList<UxmlSerializedDataDescription> candidates,
        Action<UxmlSerializedDataDescription> onSelected)
        : base(new AdvancedDropdownState())
    {
        m_Candidates = candidates;
        m_OnSelected = onSelected;
        minimumSize = new Vector2(240, 320);
    }

    static string DisplayName(UxmlSerializedDataDescription description)
    {
        var componentType = description.serializedDataType?.DeclaringType;
        return componentType?.Name ?? description.serializedDataType?.Name ?? k_FallbackNameText;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem(k_TitleText);
        foreach (var candidate in m_Candidates)
            root.AddChild(new Item(candidate, DisplayName(candidate)));
        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        if (item is Item componentItem)
            m_OnSelected?.Invoke(componentItem.description);
    }

    sealed class Item : AdvancedDropdownItem
    {
        public readonly UxmlSerializedDataDescription description;

        public Item(UxmlSerializedDataDescription description, string name) : base(name)
        {
            this.description = description;
        }
    }
}
