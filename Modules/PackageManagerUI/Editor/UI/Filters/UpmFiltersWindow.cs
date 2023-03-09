// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmFiltersWindow : PackageManagerFiltersWindow
    {
        protected override Vector2 GetSize(IPage page)
        {
            var height = k_FoldOutHeight + page.supportedStatusFilters.Count() * k_ToggleHeight;
            return new Vector2(k_Width, Math.Min(height, k_MaxHeight));
        }

        protected override void ApplyFilters()
        {
            foreach (var toggle in m_StatusFoldOut.Children().OfType<Toggle>())
                toggle.SetValueWithoutNotify(toggle.name == m_Filters.status.ToString());
        }

        protected override void DoDisplay(IPage page)
        {
            m_StatusFoldOut = new Foldout {text = L10n.Tr("Status"), name = k_StatusFoldOutName, classList = {k_FoldoutClass}};
            foreach (var status in page.supportedStatusFilters)
            {
                var toggle = new Toggle(status.GetDisplayName()) {name = status.ToString(), classList = {k_ToggleClass}};
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        foreach (var t in m_StatusFoldOut.Children().OfType<Toggle>())
                        {
                            if (t == toggle)
                                continue;

                            t.SetValueWithoutNotify(false);
                        }
                    }
                    UpdateFiltersIfNeeded();
                });

                m_StatusFoldOut.Add(toggle);
            }
            m_Container.Add(m_StatusFoldOut);
        }

        private void UpdateFiltersIfNeeded()
        {
            var filters = m_Filters.Clone();
            var selectedStatus = m_StatusFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name).FirstOrDefault();
            filters.status = !string.IsNullOrEmpty(selectedStatus) && Enum.TryParse(selectedStatus, out PageFilters.Status status) ? status : PageFilters.Status.None;

            if (!filters.Equals(m_Filters))
            {
                m_Filters = filters;
                UpdatePageFilters();
            }
        }

        internal Foldout m_StatusFoldOut;
    }
}
