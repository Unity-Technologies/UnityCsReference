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
        private const string k_AllSteps = "All Steps";
        // Tooltip shown when no specific step is selected; otherwise the tooltip is the full step name.
        private const string k_StepFilterTooltip = "Filter messages by build step";
        private const string k_UxmlPath = "BuildAnalysis/UXML/MessagesConsole.uxml";
        private const int k_NoStepFilter = -1;

        // Severity int IDs are stable so sort/filter can compare ints rather than parsing strings each time.
        private const int k_SeverityError = 0;
        private const int k_SeverityWarning = 1;
        private const int k_SeverityInfo = 2;
        private const int k_SeverityOther = 3;

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
        private readonly ZebraEmptyBody m_EmptyBody;
        private readonly Label m_DetailText;
        private readonly Label m_FooterCountLabel;

        private BuildAnalysisMessage[] m_Messages;
        private int[] m_SeverityIds = Array.Empty<int>();
        private int[] m_StepNameIds = Array.Empty<int>();

        // Index into a pool of unique (severityId, text) keys; used as the collapse-grouping key.
        // Built lazily on first transition to collapsed mode — avoids a multi-MB scratch dict on every
        // Bind when the user never collapses.
        private int[] m_CollapseKeyIds = Array.Empty<int>();
        private bool m_CollapseKeysBuilt;
        // Step name pool, sorted alphabetically; used directly as dropdown choices (after the "All Steps" prefix).
        private string[] m_StepNamePool = Array.Empty<string>();

        private readonly List<int> m_FilteredIndices = new List<int>();
        // collapseKeyId → total count of matching messages in the current filter (only populated when collapsed).
        private readonly Dictionary<int, int> m_CollapseCountByKey = new Dictionary<int, int>();

        private IVisualElementScheduledItem m_SearchDebounce;
        private bool m_ShowErrors = true;
        private bool m_ShowWarnings = true;
        private bool m_ShowInfo = true;
        private bool m_Collapsed;
        // k_NoStepFilter means "All Steps" (no filter); otherwise an index into m_StepNamePool.
        private int m_StepFilterId = k_NoStepFilter;
        private string m_SearchText = string.Empty;

        public MessagesConsole()
        {
            AddToClassList("section");

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
            SetStepDropdownValue(k_AllSteps);

            m_CollapseToggle = this.Q<ToolbarToggle>("collapse-toggle");

            m_ListView = this.Q<MultiColumnListView>("messages-list-view");
            m_ListView.itemsSource = m_FilteredIndices;

            // Host inside content-area (the TwoPaneSplitView's parent) rather than the splitter
            // itself, so the absolute overlay never competes with the splitter's pane children.
            m_EmptyBody = new ZebraEmptyBody(m_ListView);
            this.Q<VisualElement>("content-area").Add(m_EmptyBody);
            m_ListView.makeNoneElement = () => new VisualElement();
            m_ListView.columns["column-type"].makeCell = MakeTypeCell;
            m_ListView.columns["column-type"].bindCell = BindTypeCell;
            m_ListView.columns["column-step"].makeCell = MakeStepCell;
            m_ListView.columns["column-step"].bindCell = BindStepCell;
            m_ListView.columns["column-log"].makeCell = MakeLogCell;
            m_ListView.columns["column-log"].bindCell = BindLogCell;

            m_StepDropdown.RegisterValueChangedCallback(evt => SetStepFilter(evt.newValue));
            m_CollapseToggle.RegisterValueChangedCallback(evt =>
            {
                m_Collapsed = evt.newValue;
                if (!m_Collapsed)
                    m_CollapseCountByKey.Clear();
                ApplyFilters();
            });
            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                var newText = evt.newValue ?? string.Empty;
                m_SearchDebounce?.Pause();
                m_SearchDebounce = schedule.Execute(() => SetSearchText(newText)).StartingIn(200);
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
            m_Messages = analysis.Messages;
            var n = m_Messages.Length;

            // Reuse parallel arrays across binds when possible; only grow when capacity is exceeded.
            // m_CollapseKeyIds is grown lazily by EnsureCollapseKeysBuilt.
            if (m_SeverityIds.Length < n)
                m_SeverityIds = new int[n];
            if (m_StepNameIds.Length < n)
                m_StepNameIds = new int[n];
            m_CollapseKeysBuilt = false;

            BuildSeverityIds(n);
            BuildStepNamePoolAndIds(analysis.Tables.Steps, n);
            BuildStepDropdownChoices();

            var counts = analysis.Computed.Counts;
            m_ErrorToggleCount.text = FormatUtility.FormatCappedCount(counts.ErrorMessageCount);
            m_WarnToggleCount.text = FormatUtility.FormatCappedCount(counts.WarningMessageCount);
            m_InfoToggleCount.text = FormatUtility.FormatCappedCount(counts.InfoMessageCount);

            ResetFilterState();
            ShowNoDetail();
            ApplyFilters();
        }

        private void BuildSeverityIds(int n)
        {
            for (int i = 0; i < n; i++)
                m_SeverityIds[i] = SeverityToId(m_Messages[i].Severity);
        }

        private void BuildStepNamePoolAndIds(BuildAnalysisStep[] steps, int n)
        {
            var stepIdToName = new Dictionary<int, string>(steps.Length);
            foreach (var step in steps)
                stepIdToName[step.Id] = step.Name ?? "Unknown";

            var insertionMap = new Dictionary<string, int>(StringComparer.Ordinal);
            var insertionPool = new List<string>();

            for (int i = 0; i < n; i++)
            {
                var stepName = stepIdToName.TryGetValue(m_Messages[i].StepId, out var resolved) ? resolved : "Unknown";
                if (!insertionMap.TryGetValue(stepName, out var id))
                {
                    id = insertionPool.Count;
                    insertionPool.Add(stepName);
                    insertionMap[stepName] = id;
                }
                m_StepNameIds[i] = id;
            }

            // Sort the pool alphabetically and remap insertion-id → sorted-id, so id order == display order
            var sortedPool = new List<string>(insertionPool);
            sortedPool.Sort(StringComparer.OrdinalIgnoreCase);
            var remap = new int[insertionPool.Count];
            for (int sortedId = 0; sortedId < sortedPool.Count; sortedId++)
                remap[insertionMap[sortedPool[sortedId]]] = sortedId;

            for (int i = 0; i < n; i++)
                m_StepNameIds[i] = remap[m_StepNameIds[i]];

            m_StepNamePool = sortedPool.ToArray();
        }

        private void EnsureCollapseKeysBuilt()
        {
            if (m_CollapseKeysBuilt)
                return;

            var n = m_Messages.Length;
            if (m_CollapseKeyIds.Length < n)
                m_CollapseKeyIds = new int[n];

            var keyMap = new Dictionary<(int sevId, string text), int>();
            for (int i = 0; i < n; i++)
            {
                var key = (m_SeverityIds[i], m_Messages[i].Text ?? string.Empty);
                if (!keyMap.TryGetValue(key, out var id))
                {
                    id = keyMap.Count;
                    keyMap[key] = id;
                }
                m_CollapseKeyIds[i] = id;
            }
            m_CollapseKeysBuilt = true;
        }

        private void BuildStepDropdownChoices()
        {
            var choices = new List<string>(m_StepNamePool.Length + 1) { k_AllSteps };
            choices.AddRange(m_StepNamePool);
            m_StepDropdown.choices = choices;
            SetStepDropdownValue(k_AllSteps);
        }

        private void SetStepFilter(string value)
        {
            var newId = string.IsNullOrEmpty(value) || string.Equals(value, k_AllSteps, StringComparison.Ordinal)
                ? k_NoStepFilter
                : Array.IndexOf(m_StepNamePool, value);

            if (newId == m_StepFilterId)
                return;

            m_StepFilterId = newId;
            SetStepDropdownValue(newId == k_NoStepFilter ? k_AllSteps : m_StepNamePool[newId]);
            ApplyFilters();
        }

        // Sets the dropdown value without firing its callback, and keeps the tooltip in sync: the
        // full step name on hover (it ellipsizes in the field when long), or the filter hint for "All Steps".
        private void SetStepDropdownValue(string value)
        {
            m_StepDropdown.SetValueWithoutNotify(value);
            m_StepDropdown.tooltip = string.Equals(value, k_AllSteps, StringComparison.Ordinal)
                ? k_StepFilterTooltip
                : value;
        }

        private void SetSearchText(string value)
        {
            var newText = value ?? string.Empty;
            if (string.Equals(newText, m_SearchText, StringComparison.Ordinal))
                return;
            m_SearchText = newText;
            ApplyFilters();
        }

        private void ResetFilterState()
        {
            m_SearchDebounce?.Pause();
            m_ShowErrors = true;
            m_ShowWarnings = true;
            m_ShowInfo = true;
            m_Collapsed = false;
            m_CollapseCountByKey.Clear();
            m_StepFilterId = k_NoStepFilter;
            m_SearchText = string.Empty;
            m_ErrorToggle.SetValueWithoutNotify(true);
            m_WarnToggle.SetValueWithoutNotify(true);
            m_InfoToggle.SetValueWithoutNotify(true);
            m_CollapseToggle.SetValueWithoutNotify(false);
            m_SearchField.SetValueWithoutNotify(string.Empty);
            SetStepDropdownValue(k_AllSteps);
        }

        private void ApplyFilters()
        {
            m_FilteredIndices.Clear();
            if (m_FilteredIndices.Capacity < m_Messages.Length)
                m_FilteredIndices.Capacity = m_Messages.Length;

            bool hasStepFilter = m_StepFilterId != k_NoStepFilter;
            bool hasSearch = !string.IsNullOrEmpty(m_SearchText);

            if (m_Collapsed)
            {
                EnsureCollapseKeysBuilt();

                // Filter first, then collapse, so a message that occurs in multiple steps is still
                // counted under whichever step the user filtered to. Collapsing on (Severity, Text)
                // up-front would discard step context for all but the first occurrence.
                m_CollapseCountByKey.Clear();
                for (int i = 0; i < m_Messages.Length; i++)
                {
                    if (!IsSeverityVisible(m_SeverityIds[i])) continue;
                    if (hasStepFilter && m_StepNameIds[i] != m_StepFilterId) continue;
                    if (hasSearch)
                    {
                        var text = m_Messages[i].Text;
                        if (text == null || text.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }

                    var key = m_CollapseKeyIds[i];
                    if (m_CollapseCountByKey.TryGetValue(key, out var c))
                    {
                        m_CollapseCountByKey[key] = c + 1;
                    }
                    else
                    {
                        m_CollapseCountByKey[key] = 1;
                        m_FilteredIndices.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_Messages.Length; i++)
                {
                    if (!IsSeverityVisible(m_SeverityIds[i])) continue;
                    if (hasStepFilter && m_StepNameIds[i] != m_StepFilterId) continue;
                    if (hasSearch)
                    {
                        var text = m_Messages[i].Text;
                        if (text == null || text.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }
                    m_FilteredIndices.Add(i);
                }
            }

            ApplySort();

            // Clear selection when filters change so a stale highlight doesn't survive.
            m_ListView.selectedIndex = -1;
            ShowNoDetail();

            m_ListView.RefreshItems();
            UpdateFooter();
            m_EmptyBody.Refresh();
        }

        private void UpdateFooter()
        {
            m_FooterCountLabel.text = $"Showing {m_FilteredIndices.Count}/{m_Messages.Length}";
        }

        private bool IsSeverityVisible(int severityId) => severityId switch
        {
            k_SeverityError   => m_ShowErrors,
            k_SeverityWarning => m_ShowWarnings,
            k_SeverityInfo    => m_ShowInfo,
            _                 => true
        };

        private static int SeverityToId(string severity) => severity switch
        {
            BuildMessageSeverity.Error   => k_SeverityError,
            BuildMessageSeverity.Warning => k_SeverityWarning,
            BuildMessageSeverity.Info    => k_SeverityInfo,
            _                            => k_SeverityOther
        };

        private void ApplySort()
        {
            using var enumerator = m_ListView.sortedColumns.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            var sort = enumerator.Current;
            var ascending = sort.direction == SortDirection.Ascending;
            SortFiltered(sort.columnName, ascending);
        }

        private void SortFiltered(string columnName, bool ascending)
        {
            Comparison<int> cmp = columnName switch
            {
                "column-type" => CompareByType,
                "column-step" => CompareByStep,
                "column-log"  => CompareByLog,
                _             => null
            };
            if (cmp == null) return;
            m_FilteredIndices.Sort(ascending ? cmp : (a, b) => -cmp(a, b));
        }

        private int CompareByType(int a, int b) => m_SeverityIds[a].CompareTo(m_SeverityIds[b]);

        // Pool is alphabetically sorted, so id order == display order.
        private int CompareByStep(int a, int b) => m_StepNameIds[a].CompareTo(m_StepNameIds[b]);

        private int CompareByLog(int a, int b) =>
            string.Compare(m_Messages[a].Text, m_Messages[b].Text, StringComparison.OrdinalIgnoreCase);

        private void OnListSelectionChanged()
        {
            var idx = m_ListView.selectedIndex;
            if (idx < 0 || idx >= m_FilteredIndices.Count)
            {
                ShowNoDetail();
                return;
            }
            m_DetailText.text = m_Messages[m_FilteredIndices[idx]].Text;
        }

        private void ShowNoDetail()
        {
            m_DetailText.text = string.Empty;
        }

        private static VisualElement MakeTypeCell()
        {
            var container = new VisualElement();
            container.AddToClassList("messages-cell__type-container");
            var icon = new VisualElement();
            icon.AddToClassList("messages-cell__type-icon");
            container.Add(icon);
            return container;
        }

        private void BindTypeCell(VisualElement ve, int row)
        {
            var sevId = m_SeverityIds[m_FilteredIndices[row]];
            var ussClass = sevId switch
            {
                k_SeverityError   => "error-icon-small",
                k_SeverityWarning => "warn-icon-small",
                _                 => "info-icon-small"
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

        private void BindStepCell(VisualElement ve, int row)
        {
            ((Label)ve).text = m_StepNamePool[m_StepNameIds[m_FilteredIndices[row]]];
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

        private void BindLogCell(VisualElement ve, int row)
        {
            var msgIdx = m_FilteredIndices[row];
            var label = (Label)ve.ElementAt(0);
            var countLabel = (Label)ve.ElementAt(1);

            label.text = m_Messages[msgIdx].Text;

            countLabel.style.display = m_Collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_Collapsed)
                countLabel.text = FormatUtility.FormatCappedCount(m_CollapseCountByKey[m_CollapseKeyIds[msgIdx]]);
        }
    }
}
