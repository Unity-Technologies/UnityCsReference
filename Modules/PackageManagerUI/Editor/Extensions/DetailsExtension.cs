// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class DetailsExtension : VisualElement, IDetailsExtension
    {
        private Toggle m_HeaderToggle;

        private VisualElement m_ContentContainer;
        public override VisualElement contentContainer => m_ContentContainer;

        private PackageManagerPrefs m_PackageManagerPrefs;

        public bool expanded { get => m_HeaderToggle.value; set => SetExpanded(value); }
        public string title { get => m_HeaderToggle.text; set => m_HeaderToggle.text = value; }

        private int m_Priority;
        public int priority
        {
            get => m_Priority;
            set
            {
                if (m_Priority == value)
                    return;
                m_Priority = value;
                onPriorityChanged?.Invoke();
            }
        }

        public bool enabled { get => enabledSelf; set => SetEnabled(value); }
        public new bool visible { get => UIUtils.IsElementVisible(this); set => UIUtils.SetElementDisplay(this, value); }

        public event Action onPriorityChanged;

        public DetailsExtension(PackageManagerPrefs packageManagerPrefs)
        {
            m_PackageManagerPrefs = packageManagerPrefs;

            m_HeaderToggle = new Toggle();
            m_HeaderToggle.text = string.Empty;
            m_HeaderToggle.AddToClassList("containerTitle");
            m_HeaderToggle.AddToClassList("expander");
            hierarchy.Add(m_HeaderToggle);

            m_ContentContainer = new VisualElement();
            m_ContentContainer.AddToClassList("detailsExtensionContainer");
            hierarchy.Add(m_ContentContainer);

            SetExpanded(m_PackageManagerPrefs.IsDetailsExtensionExpanded(title));
            m_HeaderToggle.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));
        }

        private void SetExpanded(bool expanded)
        {
            if (m_HeaderToggle.value != expanded)
                m_HeaderToggle.value = expanded;
            m_PackageManagerPrefs.SetDetailsExtensionExpanded(title, expanded);
            UIUtils.SetElementDisplay(m_ContentContainer, expanded);
        }
    }
}
