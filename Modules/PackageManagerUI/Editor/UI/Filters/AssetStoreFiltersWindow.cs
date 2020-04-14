// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class AssetStoreFiltersWindow : PackageManagerFiltersWindow
    {
        internal const int k_MaxDisplayLabels = 5;
        private const int k_FoldOutHeight = 24;
        private const int k_ToggleHeight = 19;
        private const int k_Width = 196;
        private const int k_MaxHeight = 400;

        private static readonly string k_FoldoutClass = "foldout";
        private static readonly string k_ToggleClass = "toggle";

        internal static readonly string k_StatusFoldOutName = "statusFoldOut";
        internal static readonly string k_CategoriesFoldOutName = "categoriesFoldOut";
        internal static readonly string k_LabelsFoldOutName = "labelsFoldOut";
        internal static readonly string k_ShowAllButtonName = "showAll";

        internal static readonly string[] k_Statuses =
        {
            "Unlabeled",
            "Hidden",
            "Deprecated"
        };

        [NonSerialized]
        private List<string> m_Categories;

        [NonSerialized]
        private List<string> m_Labels;

        private AssetStoreClient m_AssetStoreClient;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_AssetStoreClient = container.Resolve<AssetStoreClient>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveDependencies();
        }

        protected override Vector2 GetSize()
        {
            var height = k_FoldOutHeight + k_Statuses.Length * k_ToggleHeight;

            var categories = m_CategoriesFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>();
            height += (categories.Any() ? k_FoldOutHeight : 0) + categories.Count() * k_ToggleHeight;

            var labels = m_LabelsFoldOut?.Children().OfType<Toggle>() ?? Enumerable.Empty<Toggle>();
            var firstLabels = labels.Take(k_MaxDisplayLabels);
            height += (labels.Any() ? k_FoldOutHeight : 0) + firstLabels.Count() * k_ToggleHeight;

            var selectedLabels = labels.Skip(k_MaxDisplayLabels).Where(t => t.value);
            height += selectedLabels.Count() * k_ToggleHeight;
            if (labels.Count() > firstLabels.Count() + selectedLabels.Count())
                height += k_ToggleHeight; // Show all button height

            return new Vector2(k_Width, Math.Min(height, k_MaxHeight));
        }

        protected override void Init(Rect rect, PageFilters filters)
        {
            m_AssetStoreClient.ListCategories(categories =>
            {
                m_Categories = categories ?? new List<string>();

                m_AssetStoreClient.ListLabels(labels =>
                {
                    m_Labels = labels ?? new List<string>();

                    base.Init(rect, filters);
                });
            });
        }

        protected override void ApplyFilters()
        {
            foreach (var toggle in m_StatusFoldOut.Children().OfType<Toggle>())
                toggle.SetValueWithoutNotify(toggle.name == m_Filters.statuses?.FirstOrDefault());

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

        protected override void DoDisplay()
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

                        if (status == k_Statuses[0] && m_LabelsFoldOut != null)
                        {
                            // Uncheck labels if checked
                            foreach (var t in m_LabelsFoldOut.Children().OfType<Toggle>())
                                t.value = false;
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

            if (m_Labels.Any())
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
                            m_StatusFoldOut.Q<Toggle>(k_Statuses[0].ToLower()).value = false;
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
        }

        private void UpdateFiltersIfNeeded()
        {
            var filters = m_Filters.Clone();
            var selectedStatuses = m_StatusFoldOut.Children().OfType<Toggle>().Where(toggle => toggle.value).Select(toggle => toggle.name.ToLower());
            filters.statuses = selectedStatuses.ToList();

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
