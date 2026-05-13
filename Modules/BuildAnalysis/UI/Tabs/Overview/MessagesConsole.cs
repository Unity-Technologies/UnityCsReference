// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class MessagesConsole : VisualElement
    {
        private struct MessageEntry
        {
            public BuildAnalysisMessage Msg;
            public int Count; // 0 means uncollapsed (count not tracked)
            public string StepName;
        }

        private const string k_AllSteps = "All Steps";
        private const string k_UxmlPath = "BuildAnalysis/UXML/MessagesConsole.uxml";

        private readonly DropdownField m_StepDropdown;
        private readonly ToolbarSearchField m_SearchField;
        private readonly Toggle m_ErrorToggle;
        private readonly Label m_ErrorToggleCount;
        private readonly Toggle m_WarnToggle;
        private readonly Label m_WarnToggleCount;
        private readonly Toggle m_InfoToggle;
        private readonly Label m_InfoToggleCount;
        private readonly Toggle m_CollapseToggle;
        private readonly MultiColumnListView m_ListView;
        private readonly Label m_DetailText;
        private readonly Label m_FooterCountLabel;

        // Step name cache: StepId → display name, built once per Bind()
        private readonly Dictionary<int, string> m_StepNameCache = new Dictionary<int, string>();
        private readonly Dictionary<(string, string), int> m_CollapseSeen = new Dictionary<(string, string), int>();

        private readonly List<BuildAnalysisMessage> m_AllMessages = new List<BuildAnalysisMessage>();
        private readonly List<MessageEntry> m_FilteredMessages = new List<MessageEntry>();
        private IVisualElementScheduledItem m_SearchDebounce;
        private bool m_ShowErrors = true;
        private bool m_ShowWarnings = true;
        private bool m_ShowInfo = true;
        private bool m_Collapsed;
        private string m_StepFilter = k_AllSteps;
        private string m_SearchText = string.Empty;

        public MessagesConsole()
        {
            AddToClassList("overview-section");
            AddToClassList("messages-section");

            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_StepDropdown = this.Q<DropdownField>("step-dropdown");
            m_SearchField = this.Q<ToolbarSearchField>("search-field");
            m_ErrorToggle = this.Q<Toggle>("error-toggle");
            m_ErrorToggleCount = this.Q<Label>("error-toggle-count");
            m_WarnToggle = this.Q<Toggle>("warn-toggle");
            m_WarnToggleCount = this.Q<Label>("warn-toggle-count");
            m_InfoToggle = this.Q<Toggle>("info-toggle");
            m_InfoToggleCount = this.Q<Label>("info-toggle-count");
            m_DetailText = this.Q<Label>("detail-text");
            m_FooterCountLabel = this.Q<Label>("footer-count-label");

            m_StepDropdown.choices = new List<string> { k_AllSteps };
            m_StepDropdown.SetValueWithoutNotify(k_AllSteps);

            m_CollapseToggle = this.Q<ToolbarToggle>("collapse-toggle");

            m_ListView = this.Q<MultiColumnListView>("messages-list-view");
            m_ListView.itemsSource = m_FilteredMessages;
            m_ListView.makeNoneElement = () => new VisualElement();
            m_ListView.columns["type"].makeCell = MakeTypeCell;
            m_ListView.columns["type"].bindCell = BindTypeCell;
            m_ListView.columns["step"].makeCell = MakeStepCell;
            m_ListView.columns["step"].bindCell = BindStepCell;
            m_ListView.columns["log"].makeCell = MakeLogCell;
            m_ListView.columns["log"].bindCell = BindLogCell;

            m_StepDropdown.RegisterValueChangedCallback(evt =>
            {
                m_StepFilter = evt.newValue ?? k_AllSteps;
                ApplyFilters();
            });
            m_CollapseToggle.RegisterValueChangedCallback(evt =>
            {
                m_Collapsed = evt.newValue;
                ApplyFilters();
            });
            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                m_SearchText = evt.newValue ?? string.Empty;
                m_SearchDebounce?.Pause();
                m_SearchDebounce = schedule.Execute(ApplyFilters).StartingIn(200);
            });
            m_ErrorToggle.RegisterValueChangedCallback(evt =>
            {
                m_ShowErrors = evt.newValue;
                ApplyFilters();
            });
            m_WarnToggle.RegisterValueChangedCallback(evt =>
            {
                m_ShowWarnings = evt.newValue;
                ApplyFilters();
            });
            m_InfoToggle.RegisterValueChangedCallback(evt =>
            {
                m_ShowInfo = evt.newValue;
                ApplyFilters();
            });
            m_ListView.selectionChanged += _ => OnListSelectionChanged();
            m_ListView.columnSortingChanged += () =>
            {
                ApplySort();
                m_ListView.RefreshItems();
            };
        }

        public void Bind(BuildAnalysis analysis)
        {
            if (analysis == null)
            {
                m_StepNameCache.Clear();
                m_AllMessages.Clear();
                ResetFilterState();
                m_ErrorToggleCount.text = "0";
                m_WarnToggleCount.text = "0";
                m_InfoToggleCount.text = "0";
                m_StepDropdown.choices = new List<string> { k_AllSteps };
                m_StepDropdown.SetValueWithoutNotify(k_AllSteps);
                ShowNoDetail();
                ApplyFilters();
                return;
            }

            m_StepNameCache.Clear();
            foreach (var step in analysis.Tables.Steps)
                m_StepNameCache[step.Id] = step.Name ?? "Unknown";

            m_AllMessages.Clear();
            m_AllMessages.AddRange(analysis.Messages);

            ResetFilterState();

            var counts = analysis.Computed.Counts;
            m_ErrorToggleCount.text = FormatUtils.FormatCount(counts.ErrorMessageCount);
            m_WarnToggleCount.text = FormatUtils.FormatCount(counts.WarningMessageCount);
            m_InfoToggleCount.text = FormatUtils.FormatCount(counts.InfoMessageCount);

            var stepNames = new List<string> { k_AllSteps };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var msg in m_AllMessages)
            {
                var stepName = ResolveStepName(msg.StepId);
                if (seen.Add(stepName))
                    stepNames.Add(stepName);
            }
            m_StepDropdown.choices = stepNames;
            m_StepDropdown.SetValueWithoutNotify(k_AllSteps);

            ShowNoDetail();
            ApplyFilters();
        }

        private void ResetFilterState()
        {
            m_ShowErrors = true;
            m_ShowWarnings = true;
            m_ShowInfo = true;
            m_Collapsed = false;
            m_StepFilter = k_AllSteps;
            m_SearchText = string.Empty;
            m_ErrorToggle.SetValueWithoutNotify(true);
            m_WarnToggle.SetValueWithoutNotify(true);
            m_InfoToggle.SetValueWithoutNotify(true);
            m_CollapseToggle.SetValueWithoutNotify(false);
            m_SearchField.SetValueWithoutNotify(string.Empty);
        }

        private void ApplyFilters()
        {
            m_FilteredMessages.Clear();
            if (m_Collapsed)
            {
                // Filter first, then collapse, so a message that occurs in multiple
                // steps is still counted under whichever step the user filtered to.
                // Collapsing on (Severity, Text) up-front would discard step context
                // for all but the first occurrence.
                m_CollapseSeen.Clear();
                foreach (var msg in m_AllMessages)
                {
                    if (!PassesFilter(msg))
                        continue;
                    var key = (msg.Severity, msg.Text);
                    if (m_CollapseSeen.TryGetValue(key, out var idx))
                    {
                        var existing = m_FilteredMessages[idx];
                        existing.Count++;
                        m_FilteredMessages[idx] = existing;
                    }
                    else
                    {
                        m_CollapseSeen[key] = m_FilteredMessages.Count;
                        m_FilteredMessages.Add(new MessageEntry { Msg = msg, Count = 1, StepName = ResolveStepName(msg.StepId) });
                    }
                }
            }
            else
            {
                foreach (var msg in m_AllMessages)
                {
                    if (PassesFilter(msg))
                        m_FilteredMessages.Add(new MessageEntry { Msg = msg, Count = 0, StepName = ResolveStepName(msg.StepId) });
                }
            }

            ApplySort();

            // Clear selection when filters change
            m_ListView.selectedIndex = -1;
            ShowNoDetail();

            RefreshListView();
        }

        private void RefreshListView()
        {
            m_ListView.RefreshItems();
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            m_FooterCountLabel.text = $"Showing {m_FilteredMessages.Count}/{m_AllMessages.Count}";
        }

        private bool PassesFilter(BuildAnalysisMessage msg)
        {
            if (!IsSeverityVisible(msg.Severity))
                return false;
            if (!string.Equals(m_StepFilter, k_AllSteps, StringComparison.Ordinal))
            {
                if (!string.Equals(ResolveStepName(msg.StepId), m_StepFilter, StringComparison.Ordinal))
                    return false;
            }
            if (!string.IsNullOrEmpty(m_SearchText) &&
                msg.Text.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        private bool IsSeverityVisible(string severity) => severity switch
        {
            BuildMessageSeverity.Error   => m_ShowErrors,
            BuildMessageSeverity.Warning => m_ShowWarnings,
            BuildMessageSeverity.Info    => m_ShowInfo,
            _                            => true
        };

        private static VisualElement MakeTypeCell()
        {
            var container = new VisualElement();
            container.AddToClassList("messages-cell__type-container");
            var icon = new VisualElement();
            icon.AddToClassList("messages-cell__type-icon");
            container.Add(icon);
            return container;
        }

        private void BindTypeCell(VisualElement ve, int index)
        {
            var severity = m_FilteredMessages[index].Msg.Severity;
            var ussClass = severity switch
            {
                BuildMessageSeverity.Error   => "error-icon-small",
                BuildMessageSeverity.Warning => "warn-icon-small",
                _                            => "info-icon-small"
            };

            var icon = ve.ElementAt(0);
            if (icon.ClassListContains(ussClass))
                return;
            icon.ClearClassList();
            icon.AddToClassList("messages-cell__type-icon");
            icon.AddToClassList(ussClass);
        }

        private static VisualElement MakeStepCell()
        {
            var label = new Label();
            label.AddToClassList("messages-cell__step");
            return label;
        }

        private void BindStepCell(VisualElement ve, int index)
        {
            ((Label)ve).text = m_FilteredMessages[index].StepName;
        }

        private static VisualElement MakeLogCell()
        {
            var container = new VisualElement();
            container.AddToClassList("messages-cell__log-container");
            var label = new Label();
            label.AddToClassList("messages-cell__log");
            var count = new Label();
            count.AddToClassList("messages-cell__collapse-count");
            count.style.display = DisplayStyle.None;
            container.Add(label);
            container.Add(count);
            return container;
        }

        private void BindLogCell(VisualElement ve, int index)
        {
            var entry = m_FilteredMessages[index];
            var label = (Label)ve.ElementAt(0);
            var countLabel = (Label)ve.ElementAt(1);

            label.text = entry.Msg.Text;

            countLabel.style.display = m_Collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_Collapsed)
                countLabel.text = FormatUtils.FormatCount(entry.Count);
        }

        private void OnListSelectionChanged()
        {
            var idx = m_ListView.selectedIndex;
            if (idx < 0 || idx >= m_FilteredMessages.Count)
            {
                ShowNoDetail();
                return;
            }

            var entry = m_FilteredMessages[idx];
            m_DetailText.text = entry.Msg.Text;
        }

        private void ShowNoDetail()
        {
            m_DetailText.text = string.Empty;
        }

        private void ApplySort()
        {
            using var enumerator = m_ListView.sortedColumns.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            var sort = enumerator.Current;
            var ascending = sort.direction == SortDirection.Ascending;

            m_FilteredMessages.Sort((a, b) =>
            {
                int cmp = sort.columnName switch
                {
                    "type" => SeverityOrder(a.Msg.Severity).CompareTo(SeverityOrder(b.Msg.Severity)),
                    "step" => string.Compare(a.StepName, b.StepName, StringComparison.OrdinalIgnoreCase),
                    "log"  => string.Compare(a.Msg.Text, b.Msg.Text, StringComparison.OrdinalIgnoreCase),
                    _      => 0
                };
                return ascending ? cmp : -cmp;
            });
        }

        private static int SeverityOrder(string severity) => severity switch
        {
            BuildMessageSeverity.Error   => 0,
            BuildMessageSeverity.Warning => 1,
            _                            => 2
        };

        private string ResolveStepName(int stepId) =>
            m_StepNameCache.TryGetValue(stepId, out var name) ? name : "Unknown";

    }
}
