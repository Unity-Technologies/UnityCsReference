// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class OverlayGroupMenuItem : Foldout
    {
        Toggle m_Toggle;
        List<Overlay> m_Overlays;

        public OverlayGroupMenuItem(string name, List<Overlay> overlays)
        {
            m_Overlays = overlays;
            text = name;

            var label = this.Q<Label>();
            label.parent.Insert(label.parent.IndexOf(label), m_Toggle = new Toggle());

            m_Toggle.RegisterValueChangedCallback(ValueChanged);
            var foldoutToggle = this.Q<Toggle>(classes: toggleUssClassName);
            foldoutToggle.RegisterCallback<MouseOverEvent>(OnMouseEnter);
            foldoutToggle.RegisterCallback<MouseOutEvent>(OnMouseLeave);
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            foreach (var overlay in m_Overlays)
                overlay.displayedChanged += OnDisplayChanged;

            UpdateToggleState();
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            foreach (var overlay in m_Overlays)
                overlay.displayedChanged -= OnDisplayChanged;
        }

        void OnDisplayChanged(bool displayed)
        {
            UpdateToggleState();
        }

        void UpdateToggleState()
        {
            bool allDisplayed = true;
            bool allHidden = true;

            foreach (var overlay in m_Overlays)
            {
                var display = overlay.displayed;
                allDisplayed &= display;
                allHidden &= !display;
            }

            m_Toggle.showMixedValue = !allDisplayed && !allHidden;
            m_Toggle.SetValueWithoutNotify(allDisplayed);
        }

        void ValueChanged(ChangeEvent<bool> evt)
        {
            foreach (var overlay in m_Overlays)
                overlay.displayed = evt.newValue;
        }

        void OnMouseLeave(MouseOutEvent evt)
        {
            foreach (var overlay in m_Overlays)
                overlay.SetHighlightEnabled(false);
        }

        void OnMouseEnter(MouseOverEvent evt)
        {
            foreach (var overlay in m_Overlays)
                overlay.SetHighlightEnabled(enabledSelf); // We don't highlight if the item is disabled
        }
    }

    sealed class OverlayMenuItem : Toggle
    {
        const string k_MenuItemClass = "overlay-menu-item";

        readonly Label m_Title;
        VisualElement m_Toggle;
        Overlay m_Overlay;

        public OverlayMenuItem(Overlay overlay)
        {
            m_Overlay = overlay;

            AddToClassList(k_MenuItemClass);

            var container = new VisualElement();
            container.AddToClassList(k_MenuItemClass + "__container");

            m_Title = new Label(string.IsNullOrEmpty(overlay.displayName) ? overlay.GetType().Name : overlay.displayName) { name = "DisplayName" };
            m_Title.AddToClassList(k_MenuItemClass + "__display-name");
            container.Add(m_Title);

            tooltip = m_Title.text;

            Add(container);

            m_Toggle = this.Q<VisualElement>(className: "unity-toggle__input");
            if (m_Toggle != null)
                m_Toggle.AddToClassList(k_MenuItemClass + "__toggle");

            Insert(0, m_Toggle); // Move toggle before label

            RegisterCallback<MouseOverEvent>(OnMouseEnter);
            RegisterCallback<MouseOutEvent>(OnMouseLeave);

            this.RegisterValueChangedCallback((evt) => overlay.displayed = evt.newValue);
            SetValueWithoutNotify(overlay.displayed);
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            m_Overlay.displayedChanged += OnDisplayChanged;
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Overlay.displayedChanged -= OnDisplayChanged;
        }

        void OnDisplayChanged(bool displayed)
        {
            UpdateToggleState();
        }

        void UpdateToggleState()
        {
            SetValueWithoutNotify(m_Overlay.displayed);
        }

        void OnMouseLeave(MouseOutEvent evt)
        {
            m_Overlay.SetHighlightEnabled(false);
        }

        void OnMouseEnter(MouseOverEvent evt)
        {
            m_Overlay.SetHighlightEnabled(enabledSelf); // We don't highlight if the item is disabled
        }
    }
}
