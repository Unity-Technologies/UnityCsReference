// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class PageFiltersWindowFoldoutTypeExtension
    {
        public static string UssName(this PageFiltersWindow.FoldoutType foldoutType) => $"{foldoutType.ToString().ToLowerInvariant()}Foldout";

        public static string DisplayText(this PageFiltersWindow.FoldoutType foldoutType)
        {
            return foldoutType switch
            {
                PageFiltersWindow.FoldoutType.Status => L10n.Tr("Status"),
                PageFiltersWindow.FoldoutType.Category => L10n.Tr("Categories"),
                PageFiltersWindow.FoldoutType.Label => L10n.Tr("Labels"),
                PageFiltersWindow.FoldoutType.Package => L10n.Tr("Installed Packages with Samples"),
                _ => string.Empty
            };
        }
    }

    internal class PageFiltersWindow : EditorWindow
    {
        internal enum FoldoutType
        {
            Status = 0,
            Category,
            Label,
            Package,

            Total
        }

        internal sealed class FilterToggle : Toggle
        {
            public string filter { get; }
            public FilterToggle(string filter, bool value, Func<string, string> getFilterDisplayString = null) : base(getFilterDisplayString?.Invoke(filter) ?? filter)
            {
                this.filter = filter;
                SetValueWithoutNotify(value);
            }
        }

        internal sealed class Content : ScrollView
        {
            private const long k_DelayTicks = TimeSpan.TicksPerSecond / 2;

            private long m_FilterChangedEventTriggerTimestamp;

            public Action<PageFilters> onFiltersChanged = delegate {};
            public Action<Vector2> onSizeChanged = delegate {};
            public Action onClose = delegate {};

            private readonly Foldout[] m_Foldouts = new Foldout[(int)FoldoutType.Total];
            private GroupBox m_StatusGroupBox;

            private readonly Dictionary<PageFilterStatus, RadioButton> m_StatusRadioButtons = new ();
            private readonly List<FilterToggle> m_CategoryToggles = new ();
            private readonly List<FilterToggle> m_PackageToggles = new ();
            private readonly List<FilterToggle> m_LabelToggles = new ();

            private readonly IPackageDatabase m_PackageDatabase;
            private readonly IPage m_Page;
            private readonly PageFilters m_Filters;
            public Content(IResourceLoader resourceLoader, IPackageDatabase packageDatabase, IPage page)
            {
                m_PackageDatabase = packageDatabase;

                name = "mainContainer";
                m_Page = page;
                m_Filters = new PageFilters(page.filters);

                styleSheets.Add(resourceLoader.filtersDropdownStyleSheet);

                CreateOrUpdateStatusFoldout();
                CreateOrUpdateFoldoutByType(FoldoutType.Category);
                CreateOrUpdateFoldoutByType(FoldoutType.Label);
                CreateOrUpdateFoldoutByType(FoldoutType.Package);
                foreach (var foldout in m_Foldouts)
                    if (foldout is not null)
                        Add(foldout);

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            private void OnAttachToPanel(AttachToPanelEvent evt)
            {
                m_Page.onFiltersChanged += OnFiltersChanged;
                m_Page.UpdateSupportedFiltersAsync();
            }

            private void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                m_Page.onFiltersChanged -= OnFiltersChanged;
            }

            private void CreateOrUpdateStatusFoldout()
            {
                Foldout foldout = null;
                m_StatusRadioButtons.Clear();
                if (m_Filters.supportedStatuses.Count > 0)
                {
                    var augmentedSupportedStatuses = new List<PageFilterStatus>(m_Filters.supportedStatuses.Count + 1) { PageFilterStatus.None };
                    foreach (var status in m_Filters.supportedStatuses)
                        if (status != PageFilterStatus.None)
                            augmentedSupportedStatuses.Add(status);
                    m_StatusGroupBox ??= new GroupBox();
                    foldout = m_Foldouts[(int)FoldoutType.Status] ?? new Foldout { text = L10n.Tr("Status"), name = "statusFoldout"};
                    foldout.Add(m_StatusGroupBox);
                    m_StatusGroupBox.Clear();
                    foreach (var status in augmentedSupportedStatuses)
                    {
                        var radioButton = new RadioButton(status.GetDisplayName());
                        radioButton.SetValueWithoutNotify(status == m_Filters.status);
                        radioButton.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.newValue)
                                HandleFiltersChangeOriginatedFromUI(m_Filters.UpdateStatus(status));
                        });
                        m_StatusGroupBox.Add(radioButton);
                        m_StatusRadioButtons.Add(status, radioButton);
                    }
                }
                m_Foldouts[(int)FoldoutType.Status] = foldout;
            }

            private string GetPackageToggleText(string packageUniqueId)
            {
                return m_PackageDatabase.GetPackage(packageUniqueId)?.displayName ?? packageUniqueId;
            }

            private void CreateOrUpdateFoldoutByType(FoldoutType foldoutType)
            {
                switch (foldoutType)
                {
                    case FoldoutType.Category:
                        CreateOrUpdateFoldoutHelper(foldoutType, m_Filters.supportedCategories, m_CategoryToggles, m_Filters.IsCategorySelected, m_Filters.UpdateCategories, L10n.Tr);
                        return;
                    case FoldoutType.Label:
                        CreateOrUpdateFoldoutHelper(foldoutType, m_Filters.supportedLabels, m_LabelToggles, m_Filters.IsLabelSelected, m_Filters.UpdateLabels);
                        return;
                    case FoldoutType.Package:
                        CreateOrUpdateFoldoutHelper(foldoutType, m_Filters.supportedPackageUniqueIds, m_PackageToggles, m_Filters.IsPackageSelected, m_Filters.UpdatePackages, GetPackageToggleText);
                        return;
                }
            }

            private void CreateOrUpdateFoldoutHelper(FoldoutType foldoutType,IReadOnlyCollection<string> supportedFilters, List<FilterToggle> filterToggles,
                Func<string, bool> isFilterSelectedFunc, Func<IReadOnlyList<string>, PageFilters.ChangedTypes> updateFiltersFunc, Func<string, string> getFilterDisplayString = null)
            {
                var foldoutIndex = (int)foldoutType;
                Foldout foldout = null;
                filterToggles.Clear();
                if (supportedFilters.Count > 0)
                {
                    foldout = m_Foldouts[foldoutIndex] ?? new Foldout { text = foldoutType.DisplayText(), name = foldoutType.UssName()};
                    foldout.Clear();
                    foreach (var filterValue in supportedFilters)
                    {
                        var toggle = new FilterToggle(filterValue, isFilterSelectedFunc(filterValue), getFilterDisplayString);
                        toggle.RegisterValueChangedCallback(_ =>
                        {
                            var newValues = new List<string>(filterToggles.Filter(t => t.value).SelectAsEnumerable(t => t.filter));
                            HandleFiltersChangeOriginatedFromUI(updateFiltersFunc(newValues));
                        });
                        foldout.Add(toggle);
                        filterToggles.Add(toggle);
                    }
                }
                m_Foldouts[foldoutIndex] = foldout;
            }

            private void HandleFiltersChangeOriginatedFromUI(PageFilters.ChangedTypes changedFilterTypes)
            {
                // We use `value = true` rather than `SetValueWithoutNotify` here because it is needed to notify the group box to update other radio buttons in the group
                if (changedFilterTypes.HasFlag(PageFilters.ChangedTypes.Status) && m_StatusRadioButtons.TryGetValue(m_Filters.status, out var statusButton))
                    statusButton.value = true;
                if (changedFilterTypes.HasFlag(PageFilters.ChangedTypes.Labels))
                    foreach (var toggle in m_LabelToggles)
                        toggle.SetValueWithoutNotify(m_Filters.IsLabelSelected(toggle.filter));
                if (changedFilterTypes.HasFlag(PageFilters.ChangedTypes.Categories))
                    foreach (var toggle in m_CategoryToggles)
                        toggle.SetValueWithoutNotify(m_Filters.IsCategorySelected(toggle.filter));
                if (changedFilterTypes.HasFlag(PageFilters.ChangedTypes.Packages))
                    foreach (var toggle in m_PackageToggles)
                        toggle.SetValueWithoutNotify(m_Filters.IsPackageSelected(toggle.filter));

                EditorApplication.update -= DelayedUpdatePageFilters;
                if (m_Filters.Equals(m_Page.filters))
                    return;

                // We added a delay before we actually trigger the filter change event so we don't repeatedly send too many events
                // when the user quickly changes multiple filter properties in the dropdown
                m_FilterChangedEventTriggerTimestamp = DateTime.Now.Ticks + k_DelayTicks;
                EditorApplication.update += DelayedUpdatePageFilters;
            }

            private void OnFiltersChanged(PageFiltersChangeArgs args)
            {
                // While the filters window is open, the displayed filters value and the actual page filters value can be out of sync
                // due to the delay update logic we put in place. As a result, we don't react when there is an update of filters value
                // when we receive a page event,  we see the value from the filters window as the source of truth, and we only handle
                // supported filter value changes (since supported filter changes can only originate elsewhere)
                if (!args.filterTypesChanged.AnySupportedFiltersChanged())
                    return;

                var previousHeight = CalculateTotalHeight();

                var newFilters = m_Page.filters;
                if (args.filterTypesChanged.HasFlag(PageFilters.ChangedTypes.SupportedStatuses) && m_Filters.UpdateSupportedStatuses(newFilters.supportedStatuses) != PageFilters.ChangedTypes.None)
                    CreateOrUpdateStatusFoldout();
                if (args.filterTypesChanged.HasFlag(PageFilters.ChangedTypes.SupportedCategories) && m_Filters.UpdateSupportedCategories(newFilters.supportedCategories) != PageFilters.ChangedTypes.None)
                    CreateOrUpdateFoldoutByType(FoldoutType.Category);
                if (args.filterTypesChanged.HasFlag(PageFilters.ChangedTypes.SupportedLabels) && m_Filters.UpdateSupportedLabels(newFilters.supportedLabels) != PageFilters.ChangedTypes.None)
                    CreateOrUpdateFoldoutByType(FoldoutType.Label);
                if (args.filterTypesChanged.HasFlag(PageFilters.ChangedTypes.SupportedPackages) && m_Filters.UpdateSupportedPackages(newFilters.supportedPackageUniqueIds) != PageFilters.ChangedTypes.None)
                    CreateOrUpdateFoldoutByType(FoldoutType.Package);

                Clear();
                foreach (var foldout in m_Foldouts)
                    if (foldout is not null)
                        Add(foldout);

                var newSize = CalculateSize();
                if (!Mathf.Approximately(newSize.y, previousHeight))
                    onSizeChanged?.Invoke(CalculateSize());
            }

            public void OnDisable()
            {
                EditorApplication.update -= DelayedUpdatePageFilters;
                if (m_FilterChangedEventTriggerTimestamp > 0)
                    onFiltersChanged?.Invoke(m_Filters);
                onClose?.Invoke();
            }

            private void DelayedUpdatePageFilters()
            {
                if (DateTime.Now.Ticks <= m_FilterChangedEventTriggerTimestamp)
                    return;
                EditorApplication.update -= DelayedUpdatePageFilters;
                onFiltersChanged?.Invoke(m_Filters);
                m_FilterChangedEventTriggerTimestamp = 0;
            }

            private int CalculateTotalHeight()
            {
                const int foldOutHeight = 25;
                const int toggleHeight = 20;
                var numToggles = m_StatusRadioButtons.Count + m_CategoryToggles.Count + m_LabelToggles.Count + m_PackageToggles.Count;
                return numToggles * toggleHeight + foldOutHeight * childCount;
            }

            public Vector2 CalculateSize()
            {
                const int width = 250;
                const int maxHeight = 450;
                return new Vector2(width, Math.Min(CalculateTotalHeight(), maxHeight));
            }
        }

        private static PageFiltersWindow s_Window;
        public static PageFiltersWindow instance => s_Window;

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static long s_LastClosedTime;

        private Content m_Content;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            this.SetAntiAliasing(4);
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDisable()
        {
            if (s_Window is null)
                return;

            if (m_Content != null)
            {
                m_Content.onSizeChanged -= HandleContentSizeChange;
                m_Content.OnDisable();
            }
            s_LastClosedTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            s_Window = null;
        }

        private void HandleContentSizeChange(Vector2 newSize)
        {
            if (Mathf.Approximately(newSize.y, position.height) && Mathf.Approximately(newSize.x, position.width))
                return;
            position = new Rect(position) { width = newSize.x, height = newSize.y };
            RepaintImmediately();
        }

        private void ShowContentAsDropDown(Rect rect, Content content)
        {
            m_Content = content;
            m_Content.onSizeChanged += HandleContentSizeChange;
            rootVisualElement.Add(m_Content);
            ShowAsDropDown(rect, m_Content.CalculateSize(), new[] { PopupLocation.Below });
        }

        public static bool ShowAtPosition(Rect rect, Content content)
        {
            if (s_Window is not null || content == null)
                return false;

            var nowMilliSeconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var justClosed = nowMilliSeconds < s_LastClosedTime + 150;

            if (!justClosed)
            {
                Event.current?.Use();
                s_Window = CreateInstance<PageFiltersWindow>();
                s_Window.ShowContentAsDropDown(rect, content);
            }
            return !justClosed;
        }
    }
}
