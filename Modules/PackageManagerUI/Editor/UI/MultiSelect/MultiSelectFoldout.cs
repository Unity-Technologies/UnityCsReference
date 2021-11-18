// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MultiSelectFoldout : VisualElement, IMultiSelectFoldoutElement
    {
        public string headerTextTemplate { get; set; }

        private List<IPackageVersion> m_Versions = new List<IPackageVersion>();
        public List<IPackageVersion> versions => m_Versions;

        protected PackageToolBarRegularButton m_Button;
        public PackageToolBarRegularButton button => m_Button;

        protected Toggle m_Toggle;
        protected VisualElement m_Container;

        public MultiSelectFoldout(PackageToolBarRegularButton button = null)
        {
            m_Toggle = new Toggle { name = "multiSelectFoldoutToggle", classList = { "containerTitle", "expander" } };
            m_Container = new VisualElement { name = "multiSelectFoldoutContainer" };
            Add(m_Toggle);
            Add(m_Container);

            SetExpanded(false);
            m_Toggle.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));

            m_Button = button;
            if (button != null)
                m_Toggle.Add(button.element);
        }

        public virtual void Refresh()
        {
            var isVisible = m_Versions.Any();
            UIUtils.SetElementDisplay(this, isVisible);
            if (!isVisible)
                return;

            RefreshHeader();
            RefreshContent();
            m_Button?.Refresh(m_Versions);
        }

        private void RefreshHeader()
        {
            if (string.IsNullOrEmpty(headerTextTemplate))
                return;
            var numItemsText = string.Format(m_Versions.Count > 1 ? L10n.Tr("{0} items") : L10n.Tr("{0} item"), m_Versions.Count);
            m_Toggle.text = string.Format(headerTextTemplate, numItemsText);
        }

        private void RefreshContent()
        {
            if (!m_Toggle.value)
                return;

            m_Container.Clear();
            foreach (var version in m_Versions)
                m_Container.Add(CreateMultiSelectItem(version));
        }

        protected virtual MultiSelectItem CreateMultiSelectItem(IPackageVersion version) => new MultiSelectItem(version);

        // Most foldouts are controlled by the group it belongs to, hence AddPackageVersion always return true.
        // For special standalone foldouts, the function should be overridden to provide their own logic.
        public virtual bool AddPackageVersion(IPackageVersion version)
        {
            m_Versions.Add(version);
            return true;
        }

        public void ClearVersions()
        {
            m_Versions.Clear();
        }

        private void SetExpanded(bool expanded)
        {
            if (expanded)
                RefreshContent();

            if (m_Toggle.value != expanded)
                m_Toggle.value = expanded;

            UIUtils.SetElementDisplay(m_Container, expanded);
        }
    }
}
