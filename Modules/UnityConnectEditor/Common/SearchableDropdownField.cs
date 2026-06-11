// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    [UxmlElement]
    internal partial class SearchableDropdownField : DropdownField
    {
        public SearchableDropdownField()
            : this(null) { }

        public SearchableDropdownField(string label)
            : base(label)
        {
            RegisterCallback<PointerDownEvent>(OnPointerDownTrickle, TrickleDown.TrickleDown);
            RegisterCallback<NavigationSubmitEvent>(OnSubmitTrickle, TrickleDown.TrickleDown);
        }

        void OnPointerDownTrickle(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !enabledInHierarchy)
                return;
            ShowPicker();
            evt.StopImmediatePropagation();
        }

        void OnSubmitTrickle(NavigationSubmitEvent evt)
        {
            if (!enabledInHierarchy)
                return;
            ShowPicker();
            evt.StopImmediatePropagation();
        }

        void ShowPicker()
        {
            var input = this.Q(className: BasePopupField<string, string>.inputUssClassName) ?? (VisualElement)this;
            var anchor = EditorMenuExtensions.GUIToScreenRect(this, input.worldBound);
            new SearchablePicker(this, input.worldBound.width).Show(anchor);
        }

        sealed class SearchablePicker : AdvancedDropdown
        {
            readonly SearchableDropdownField m_Field;
            string[] m_Items;

            public SearchablePicker(SearchableDropdownField field, float anchorWidth)
                : base(new AdvancedDropdownState())
            {
                m_Field = field;
                minimumSize = new Vector2(Mathf.Max(anchorWidth, 200f), 250f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                m_Items = m_Field.choices?.ToArray() ?? Array.Empty<string>();
                var root = new AdvancedDropdownItem(m_Field.label ?? string.Empty);
                for (int i = 0; i < m_Items.Length; i++)
                {
                    var item = new AdvancedDropdownItem(m_Items[i]) { elementIndex = i };
                    root.AddChild(item);
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item == null)
                    return;
                var index = item.elementIndex;
                if (m_Items == null || index < 0 || index >= m_Items.Length)
                    return;
                m_Field.value = m_Items[index];
            }
        }
    }
}
