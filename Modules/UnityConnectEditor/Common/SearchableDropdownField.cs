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
            // AdvancedDropdown.Show applies GUIUtility.GUIToScreenRect internally, which is
            // unreliable from UI Toolkit pointer callbacks. Compute the correct screen rect
            // via the panel-aware helper, then pre-invert so the internal conversion cancels.
            var screenRect = EditorMenuExtensions.GUIToScreenRect(this, input.worldBound);
            var anchor = GUIUtility.ScreenToGUIRect(screenRect);
            new SearchablePicker(this, anchor.width).Show(anchor);
        }

        sealed class SearchablePicker : AdvancedDropdown
        {
            const float k_MaxHeight = 400f;
            // A very narrow anchor would ellipsify away most of every name; this also guards
            // against a zero width, which would otherwise collapse the popup entirely.
            const float k_MinWidth = 200f;

            readonly SearchableDropdownField m_Field;
            string[] m_Items;

            public SearchablePicker(SearchableDropdownField field, float anchorWidth)
                : base(new AdvancedDropdownState())
            {
                m_Field = field;
                // AdvancedDropdown grows to fit content and only caps at the screen edge; for a
                // field embedded in a settings tab, an org with many projects would flip the
                // popup upward against the top of the screen. Cap the height so it stays anchored.
                // Capping the width keeps long project names inside the popup, where
                // AdvancedDropdownGUI ellipsifies them, instead of running past its right edge.
                maximumSize = new Vector2(Mathf.Max(anchorWidth, k_MinWidth), k_MaxHeight);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                m_Items = m_Field.choices?.ToArray() ?? Array.Empty<string>();
                var root = new AdvancedDropdownItem(m_Field.label ?? string.Empty);
                for (int i = 0; i < m_Items.Length; i++)
                {
                    // The tooltip carries the full name for rows the width cap ellipsifies.
                    var item = new AdvancedDropdownItem(m_Items[i]) { elementIndex = i, tooltip = m_Items[i] };
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
