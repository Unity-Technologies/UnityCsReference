// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal class BuildStepsElement : VisualElement
    {
        private const int k_FixedItemHeight = 26;
        private const int k_MaxVisibleRows = 10;
        private const string k_UssPath = "BuildAnalysis/StyleSheets/BuildSteps.uss";

        private struct StepItemData
        {
            public string Name;
            public long DurationMs;
            public float PercentOfTotal;
            public int WarningCount;
            public int ErrorCount;
        }

        private sealed class StepNode
        {
            public BuildAnalysisStep Step;
            public List<StepNode> Children = new List<StepNode>();
        }

        private readonly MultiColumnTreeView m_TreeView;

        public BuildStepsElement()
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);

            AddToClassList("overview-section");

            var foldout = new Foldout { text = "Build Steps", value = true };
            foldout.AddToClassList("overview-section-foldout");
            Add(foldout);

            m_TreeView = new MultiColumnTreeView
            {
                fixedItemHeight = k_FixedItemHeight,
                selectionType = SelectionType.None,
                horizontalScrollingEnabled = false,
            };
            m_TreeView.AddToClassList("build-steps-tree");
            m_TreeView.columns.primaryColumnName = "name";

            m_TreeView.columns.Add(new Column
            {
                name = "name",
                stretchable = true,
                resizable = false,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("build-step-name");
                    label.selection.isSelectable = true;
                    return label;
                },
                bindCell = BindNameCell,
            });

            m_TreeView.columns.Add(new Column
            {
                name = "badges",
                width = 70,
                stretchable = false,
                resizable = false,
                makeCell = MakeBadgeCell,
                bindCell = BindBadgeCell,
            });

            m_TreeView.columns.Add(new Column
            {
                name = "bar",
                width = 200,
                minWidth = 100,
                maxWidth = 350,
                stretchable = true,
                resizable = false,
                makeCell = () =>
                {
                    var bar = new VisualElement();
                    bar.AddToClassList("build-step-bar");
                    var fill = new VisualElement();
                    fill.AddToClassList("build-step-bar-fill");
                    bar.Add(fill);
                    return bar;
                },
                bindCell = BindBarCell,
            });

            m_TreeView.columns.Add(new Column
            {
                name = "meta",
                width = 90,
                stretchable = false,
                resizable = false,
                makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("build-step-meta");
                    label.selection.isSelectable = true;
                    return label;
                },
                bindCell = BindMetaCell,
            });

            // horizontalScrollingEnabled=false suppresses scroll interaction, but the
            // horizontal scroller can still claim layout space transiently during resize,
            // triggering a feedback loop (horizontal bar appears → steals height →
            // vertical bar appears → steals width → repeat). Setting Hidden on the
            // ScrollView directly ensures it never occupies space and breaks the loop.
            m_TreeView.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            foldout.contentContainer.Add(m_TreeView);

            // Keep the TreeView in the DOM (never display:none) so the layout engine
            // resolves its width before Rebuild() is called — required for the
            // stretchable name column to size correctly.
            m_TreeView.style.height = 0;
        }

        public void Bind(BuildAnalysis analysis)
        {
            var steps = analysis?.Tables.Steps;
            if (steps == null || steps.Length == 0)
            {
                SetEmpty();
                return;
            }

            var totalMs = analysis.Summary.TotalTimeMs;
            if (totalMs <= 0)
            {
                for (var i = 0; i < steps.Length; i++)
                    totalMs += steps[i].DurationMs;
            }

            if (totalMs <= 0)
                totalMs = 1;

            var badges = ComputeStepBadges(analysis.Messages);
            var items = BuildTreeViewItems(steps, totalMs, badges);

            m_TreeView.SetRootItems(items);
            m_TreeView.Rebuild();

            m_TreeView.style.height = Math.Min(items.Count, k_MaxVisibleRows) * k_FixedItemHeight;
        }

        private void SetEmpty()
        {
            m_TreeView.style.height = 0;
        }

        private void BindNameCell(VisualElement element, int index)
        {
            var data = m_TreeView.GetItemDataForIndex<StepItemData>(index);
            ((Label)element).text = data.Name;
        }

        private void BindBadgeCell(VisualElement element, int index)
        {
            var data = m_TreeView.GetItemDataForIndex<StepItemData>(index);

            var warnBadge = element.Q(className: "build-step-badge--warn");
            var errorBadge = element.Q(className: "build-step-badge--error");

            warnBadge.style.display = data.WarningCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            warnBadge.Q<Label>(className: "build-step-badge-count").text = FormatUtils.FormatCount(data.WarningCount);
            warnBadge.tooltip = FormatBadgeTooltip(data.WarningCount, "Warning");

            errorBadge.style.display = data.ErrorCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            errorBadge.Q<Label>(className: "build-step-badge-count").text = FormatUtils.FormatCount(data.ErrorCount);
            errorBadge.tooltip = FormatBadgeTooltip(data.ErrorCount, "Error");
        }

        private void BindBarCell(VisualElement element, int index)
        {
            var data = m_TreeView.GetItemDataForIndex<StepItemData>(index);
            element.Q(className: "build-step-bar-fill").style.width =
                Length.Percent(Mathf.Clamp(data.PercentOfTotal, 0f, 100f));
        }

        private void BindMetaCell(VisualElement element, int index)
        {
            var data = m_TreeView.GetItemDataForIndex<StepItemData>(index);
            ((Label)element).text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1:F1}%)",
                FormatUtils.FormatDuration(data.DurationMs),
                data.PercentOfTotal);
        }

        private static string FormatBadgeTooltip(int count, string severity)
        {
            var plural = count == 1 ? severity : severity + "s";
            return $"{count} {plural}. Filter by this step in the Message Console to view them.";
        }

        private static VisualElement MakeBadgeCell()
        {
            var container = new VisualElement();
            container.AddToClassList("build-step-badges");
            container.Add(MakeSingleBadge("warn", "warn-icon-small"));
            container.Add(MakeSingleBadge("error", "error-icon-small"));
            return container;
        }

        private static VisualElement MakeSingleBadge(string modifier, string iconClass)
        {
            var badge = new VisualElement();
            badge.AddToClassList("build-step-badge");
            badge.AddToClassList($"build-step-badge--{modifier}");
            var icon = new VisualElement();
            icon.AddToClassList("build-step-badge-icon");
            icon.AddToClassList(iconClass);
            badge.Add(icon);
            var count = new Label();
            count.AddToClassList("build-step-badge-count");
            badge.Add(count);
            return badge;
        }

        private static Dictionary<int, (int Warnings, int Errors)> ComputeStepBadges(BuildAnalysisMessage[] messages)
        {
            var result = new Dictionary<int, (int Warnings, int Errors)>();
            if (messages == null)
                return result;

            for (var i = 0; i < messages.Length; i++)
            {
                var msg = messages[i];
                result.TryGetValue(msg.StepId, out var counts);

                if (string.Equals(msg.Severity, BuildMessageSeverity.Warning, StringComparison.Ordinal))
                    counts.Warnings++;
                else if (string.Equals(msg.Severity, BuildMessageSeverity.Error, StringComparison.Ordinal))
                    counts.Errors++;
                else
                    continue;

                result[msg.StepId] = counts;
            }

            return result;
        }

        private static List<TreeViewItemData<StepItemData>> BuildTreeViewItems(
            BuildAnalysisStep[] steps, long totalMs, Dictionary<int, (int Warnings, int Errors)> badges)
        {
            var roots = BuildStepTree(steps);
            var result = new List<TreeViewItemData<StepItemData>>();

            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                // Depth-0 is the top-level container step (e.g. "Build player") which adds
                // no value to the user — promote its children directly as root items.
                if (root.Step.Depth == 0)
                {
                    for (var j = 0; j < root.Children.Count; j++)
                        result.Add(ConvertNode(root.Children[j], totalMs, badges));
                }
                else
                {
                    result.Add(ConvertNode(root, totalMs, badges));
                }
            }

            return result;
        }

        private static TreeViewItemData<StepItemData> ConvertNode(
            StepNode node, long totalMs, Dictionary<int, (int Warnings, int Errors)> badges)
        {
            var percent = (float)node.Step.DurationMs * 100f / totalMs;
            badges.TryGetValue(node.Step.Id, out var counts);

            var data = new StepItemData
            {
                Name = node.Step.Name ?? "Unknown",
                DurationMs = node.Step.DurationMs,
                PercentOfTotal = percent,
                WarningCount = counts.Warnings,
                ErrorCount = counts.Errors,
            };

            if (node.Children.Count == 0)
                return new TreeViewItemData<StepItemData>(node.Step.Id, data);

            var children = new List<TreeViewItemData<StepItemData>>(node.Children.Count);
            for (var i = 0; i < node.Children.Count; i++)
                children.Add(ConvertNode(node.Children[i], totalMs, badges));

            return new TreeViewItemData<StepItemData>(node.Step.Id, data, children);
        }

        private static List<StepNode> BuildStepTree(BuildAnalysisStep[] steps)
        {
            var roots = new List<StepNode>();
            var stack = new Stack<StepNode>();

            for (var i = 0; i < steps.Length; i++)
            {
                var node = new StepNode { Step = steps[i] };

                while (stack.Count > 0 && stack.Peek().Step.Depth >= node.Step.Depth)
                    stack.Pop();

                if (stack.Count == 0)
                    roots.Add(node);
                else
                    stack.Peek().Children.Add(node);

                stack.Push(node);
            }

            return roots;
        }
    }
}
