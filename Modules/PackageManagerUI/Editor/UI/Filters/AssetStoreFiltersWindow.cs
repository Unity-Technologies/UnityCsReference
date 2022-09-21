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
    internal class AssetStoreFiltersWindow : PackageManagerFiltersWindow
    {
        internal static readonly string[] k_Statuses =
        {
            PageFilters.k_DownloadedStatus,
            PageFilters.k_UpdateAvailableStatus,
            PageFilters.k_UnlabeledStatus,
            "Hidden",
            "Deprecated"
        };

        [NonSerialized]
        private List<string> m_Categories;

        [NonSerialized]
        private List<string> m_Labels;

        private AssetStoreRestAPI m_AssetStoreRestAPI;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        protected override void ResolveDependencies()
        {
            base.ResolveDependencies();
            var container = ServicesContainer.instance;
            m_AssetStoreRestAPI = container.Resolve<AssetStoreRestAPI>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
        }

        protected override Vector2 GetSize(IPage page)
        {
            var height = k_FoldOutHeight + k_Statuses.Length * k_ToggleHeight;

            var categories = m_CategoriesFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>();
            height += (categories.Any() ? k_FoldOutHeight : 0) + categories.Count() * k_ToggleHeight;

            return new Vector2(k_Width, Math.Min(height, k_MaxHeight));
        }

        private float GetLabelsHeight()
        {
            var labels = m_LabelsFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>();
            var firstLabels = labels.Take(k_MaxDisplayLabels);
            var height = (labels.Any() ? k_FoldOutHeight : 0) + firstLabels.Count() * k_ToggleHeight;

            var selectedLabels = labels.Skip(k_MaxDisplayLabels).Where(t => t.value);
            height += selectedLabels.Count() * k_ToggleHeight;
            if (labels.Count() > firstLabels.Count() + selectedLabels.Count())
                height += k_ToggleHeight; // Show all button height

            return height;
        }

        protected override void Init(Rect rect, IPage page)
        {
            // We want to show categories right way since the information is always available
            m_Categories = m_AssetStoreRestAPI.GetCategories().ToList();
            base.Init(rect, page);

            // Defer display of labels
            m_AssetStoreRestAPI.ListLabels(labels =>
            {
                // If window was closed during fetch of labels, ignore display
                if (instance == null)
                    return;

                m_Labels = labels ?? new List<string>();
                if (m_Labels.Any())
                {
                    DoLabelsDisplay();
                    ApplyFilters();

                    // Update window height if necessary
                    var labelsHeight = GetLabelsHeight();
                    var newHeight = Math.Min(position.height + labelsHeight, k_MaxHeight);
                    if (newHeight != position.height)
                    {
                        position = new Rect(position) { height = newHeight };
                        RepaintImmediately();
                    }
                }
            },
            error => Debug.LogWarning(string.Format(L10n.Tr("[Package Manager Window] Error while fetching labels: {0}"), error.message)));
        }

        protected override void ApplyFilters()
        {
            foreach (var toggle in m_StatusFoldOut.Children().OfType<Toggle>())
                toggle.SetValueWithoutNotify(toggle.name == m_Filters.status);

            foreach (var toggle in m_CategoriesFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>())
                toggle.SetValueWithoutNotify(m_Filters.categories?.Contains(toggle.name.ToLower()) ?? false);

            var selectedLabels = 0;
            var labels = m_LabelsFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>();
            foreach (var toggle in labels)
            {
                if (m_Filters?.labels?.Contains(toggle.name) ?? false)
                {
                    toggle.SetValueWithoutNotify(true);
                    UIUtils.SetElementDisplay(toggle, true);
                    selectedLabels++;
                }
            }

            if (labels.Count() > Math.Max(k_MaxDisplayLabels, selectedLabels))
            {
                m_ShowAllButton = new Button {text = L10n.Tr("Show all"), name = k_ShowAllButtonName};
                m_ShowAllButton.clickable.clicked += () =>
                {
                    m_LabelsFoldOut.Remove(m_ShowAllButton);
                    foreach (var toggle in m_LabelsFoldOut.Children())
                        UIUtils.SetElementDisplay(toggle, true);
                };
                m_LabelsFoldOut.Add(m_ShowAllButton);
            }
        }

        protected override void DoDisplay(IPage page)
        {
            m_StatusFoldOut = new Foldout {text = L10n.Tr("Status"), name = k_StatusFoldOutName, classList = {k_FoldoutClass}};
            foreach (var status in k_Statuses)
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

                        if (status == PageFilters.k_UnlabeledStatus && m_LabelsFoldOut != null)
                            foreach (var t in m_LabelsFoldOut.Children().OfType<Toggle>())
                                t.value = false;

                        if (status == PageFilters.k_UpdateAvailableStatus)
                        {
                            m_AssetStoreCallQueue.CheckUpdateForUncheckedLocalInfos();
                        }
                    }

                    UpdateFiltersIfNeeded();
                });

                m_StatusFoldOut.Add(toggle);
            }
            m_Container.Add(m_StatusFoldOut);

            if (m_Categories.Any())
            {
                m_CategoriesFoldOut = new Foldout {text = L10n.Tr("Categories"), name = k_CategoriesFoldOutName, classList = {k_FoldoutClass}};
                foreach (var category in m_Categories)
                {
                    var toggle = new Toggle(L10n.Tr(category)) {name = category.ToLower(), classList = {k_ToggleClass}};
                    toggle.RegisterValueChangedCallback(evt => UpdateFiltersIfNeeded());
                    m_CategoriesFoldOut.Add(toggle);
                }

                m_CategoriesFoldOut.Query<Label>().ForEach(UIUtils.TextTooltipOnSizeChange);
                m_Container.Add(m_CategoriesFoldOut);
            }
        }

        private void DoLabelsDisplay()
        {
            m_LabelsFoldOut = new Foldout {text = L10n.Tr("Labels"), name = k_LabelsFoldOutName, classList = {k_FoldoutClass}};
            var i = 0;
            foreach (var label in m_Labels)
            {
                var toggle = new Toggle(L10n.Tr(label)) {name = label, classList = {k_ToggleClass}};
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        // Uncheck Unlabeled if checked
                        m_StatusFoldOut.Q<Toggle>(PageFilters.k_UnlabeledStatus.ToLower()).value = false;
                    }
                    UpdateFiltersIfNeeded();
                });
                m_LabelsFoldOut.Add(toggle);

                if (++i > k_MaxDisplayLabels)
                    UIUtils.SetElementDisplay(toggle, false);
            }

            m_LabelsFoldOut.Query<Label>().ForEach(UIUtils.TextTooltipOnSizeChange);
            m_Container.Add(m_LabelsFoldOut);
        }

        private void UpdateFiltersIfNeeded()
        {
            var filters = m_Filters.Clone();
            var selectedStatuses = m_StatusFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name.ToLower());
            filters.status = selectedStatuses.FirstOrDefault();

            if (m_CategoriesFoldOut != null)
            {
                var selectedCategories = m_CategoriesFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name.ToLower());
                filters.categories = selectedCategories.ToList();
            }

            if (m_LabelsFoldOut != null)
            {
                var selectedLabels = m_LabelsFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name);
                filters.labels = selectedLabels.ToList();
            }

            if (!filters.Equals(m_Filters))
            {
                m_Filters = filters;
                UpdatePageFilters();
            }
        }

        internal Foldout m_StatusFoldOut;
        internal Foldout m_CategoriesFoldOut;
        internal Foldout m_LabelsFoldOut;
        internal Button m_ShowAllButton;
    }
}
