// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmFiltersWindow : PackageManagerFiltersWindow
    {
        private PackageDatabase m_PackageDatabase;
        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();
            var container = ServicesContainer.instance;
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        private IEnumerable<string> GetStatuses(IPage page)
        {
            yield return PageFilters.k_UpdateAvailableStatus;
            if (page.visualStates.Any(v => m_PackageDatabase.GetPackage(v.packageUniqueId)?.hasEntitlements == true))
                yield return PageFilters.k_SubscriptionBasedStatus;
        }

        protected override Vector2 GetSize(IPage page)
        {
            var height = k_FoldOutHeight + GetStatuses(page).Count() * k_ToggleHeight;
            return new Vector2(k_Width, Math.Min(height, k_MaxHeight));
        }

        protected override void ApplyFilters()
        {
            foreach (var toggle in m_StatusFoldOut.Children().OfType<Toggle>())
                toggle.SetValueWithoutNotify(toggle.name == m_Filters.status);
        }

        protected override void DoDisplay(IPage page)
        {
            m_StatusFoldOut = new Foldout {text = L10n.Tr("Status"), name = k_StatusFoldOutName, classList = {k_FoldoutClass}};
            foreach (var status in GetStatuses(page))
            {
                var toggle = new Toggle(status) {name = status.ToLower(), classList = {k_ToggleClass}};
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
            var selectedStatuses = m_StatusFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name.ToLower());
            filters.status = selectedStatuses.FirstOrDefault();

            if (!filters.Equals(m_Filters))
            {
                m_Filters = filters;
                UpdatePageFilters();
            }
        }

        internal Foldout m_StatusFoldOut;
    }
}
