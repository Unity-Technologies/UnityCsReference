// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Profiling not yet converted
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    partial class IssueSearchProvider : SearchProvider
    {
        public const string kProviderId = "project-auditor-issues";
        private const string kProviderFilterId = "auditor:";
        private const string k_ProviderDisplayName = "Project Auditor Issues";

        private static readonly Regex kStartUntilColon = new(@"^([^:]*)(?::(.*))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        [AutoStaticsCleanupOnCodeReload]
        private static Texture2D s_SearchIcon;

        private enum TypeOfReportItem
        {
            Issues,
            Insights,
        }

        private IssueSearchProvider() : base(kProviderId, k_ProviderDisplayName)
        {
            isExplicitProvider = true;
            filterId = kProviderFilterId;
            fetchItems = FetchItems;
            fetchPropositions = FetchPropositions;
            fetchThumbnail = FetchThumbnail;
            fetchColumns = FetchColumns;
            tableConfig = GetDefaultTableConfig;
        }

        private static Texture2D GetSearchIcon()
        {
            if (s_SearchIcon == null)
                s_SearchIcon = EditorGUIUtility.LoadIcon("QuickSearch/SearchWindow");
            return s_SearchIcon;
        }

        IEnumerable<SearchProposition> FetchPropositions(SearchContext arg1, SearchPropositionOptions arg2)
        {
            var sb = new StringBuilder();

            // Type
            {
                var types = Enum.GetNames(typeof(TypeOfReportItem));
                foreach (var type in types)
                    sb.Append($"\"{type}\", ");
                var allTypes = sb.ToString().TrimEnd(',', ' ');
                foreach (var type in types)
                    yield return new SearchProposition(category: "Type", label: type, replacement: $"type=<$list:\"{type}\", [{allTypes}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: GetSearchIcon(), color: QueryColors.filter);
                sb.Clear();
            }

            // Categories
            {
                var categoryArray = (IssueCategory[])Enum.GetValues(typeof(IssueCategory));
                var categories = new List<IssueCategory>(categoryArray.Length);
                foreach (var category in categoryArray)
                {
                    if (!category.IsSummary() && category < IssueCategoryExtensions.FirstCustomCategory)
                        categories.Add(category);
                }

                foreach (var category in categories)
                    sb.Append($"\"{category}\", ");
                var allCategories = sb.ToString().TrimEnd(',', ' ');
                foreach (var category in categories)
                    yield return new SearchProposition(category: "Category", label: ObjectNames.NicifyVariableName(category.ToString()), replacement: $"category=<$list:\"{category}\", [{allCategories}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: GetSearchIcon(), color: QueryColors.filter);
                sb.Clear();
            }

            // Severities
            {
                var severities = Array.ConvertAll([Severity.Critical, Severity.Major, Severity.Moderate, Severity.Minor], s => s.ToFrontendString());
                foreach (var severity in severities)
                    sb.Append($"\"{severity}\", ");
                var allSeverities = sb.ToString().TrimEnd(',', ' ');
                foreach (var severity in severities)
                    yield return new SearchProposition(category: "Severity", label: severity, replacement: $"severity=<$list:\"{severity}\", [{allSeverities}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: GetSearchIcon(), color: QueryColors.filter);
                sb.Clear();
            }
        }

        IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var report = GetCurrentReport();
            if (report != null)
            {
                var category = IssueCategory.OptimizationSummary;
                var severity = Severity.None;
                var textQuery = context.searchQuery ?? "";
                var includeIssues = true;
                var includeInsights = true;

                var typeMatch = Regex.Match(textQuery, @"type=([^;\s]+)", RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                {
                    Enum.TryParse(typeMatch.Groups[1].Value.Trim('\"'), true, out TypeOfReportItem type);
                    includeIssues = type == TypeOfReportItem.Issues;
                    includeInsights = type == TypeOfReportItem.Insights;
                    textQuery = textQuery.Replace(typeMatch.Value, "");
                }

                var categoryMatch = Regex.Match(textQuery, @"category=([^;\s]+)", RegexOptions.IgnoreCase);
                if (categoryMatch.Success)
                {
                    Enum.TryParse(categoryMatch.Groups[1].Value.Trim('\"'), true, out category);
                    textQuery = textQuery.Replace(categoryMatch.Value, "");
                }

                var severityMatch = Regex.Match(textQuery, @"severity=([^;\s]+)", RegexOptions.IgnoreCase);
                if (severityMatch.Success)
                {
                    Enum.TryParse(severityMatch.Groups[1].Value.Trim('\"'), true, out severity);
                    textQuery = textQuery.Replace(severityMatch.Value, "");
                }

                textQuery = textQuery.Trim();

                foreach (var issue in report.GetAllIssues())
                {
                    if (issue.WasFixed)
                        continue;

                    bool isIssue = issue.IsIssue();
                    if (isIssue && !includeIssues)
                        continue;
                    if (!isIssue && !includeInsights)
                        continue;

                    if (!category.IsSummary() && issue.Category != category)
                        continue;
                    if (severity != Severity.None && issue.Severity != severity)
                        continue;

                    var title = isIssue ? issue.Id.GetDescriptor().Title : issue.Category.ToString();

                    if (!string.IsNullOrEmpty(textQuery) &&
                        issue.Description.IndexOf(textQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        title.IndexOf(textQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var match = kStartUntilColon.Match(title);
                    var label = match.Groups[1].Value;
                    var item = CreateItem(
                        context,
                        issue.GetHashCode().ToString(),
                        label,
                        issue.Description,
                        null,
                        issue);

                    item.SetField("Category", issue.Category);
                    item.SetField("Severity", issue.Severity.ToFrontendString());
                    if (match.Groups[2].Success)
                        item.SetField("Reason", match.Groups[2].Value.Trim());
                    item.SetField("Descriptor ID", issue.DescriptorIdAsString);
                    item.SetField("Path", issue.RelativePath);
                    item.thumbnail = FetchThumbnail(item, null);

                    yield return item;
                }
            }
        }

        private static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            yield return new SearchColumn("Category", "", "Project Auditor/Category") { width = 150 };
            yield return new SearchColumn("Severity", "Field/Severity", "Project Auditor/Severity") { width = 90 };
            yield return new SearchColumn("Reason") { width = 200 };
            yield return new SearchColumn("Description") { width = 400 };
            yield return new SearchColumn("Descriptor ID") { width = 90 };
            yield return new SearchColumn("Path") { width = 350 };
        }

        private static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            var columns = new[]
            {
                new SearchColumn("Category", "", "Project Auditor/Category") { width = 150 },
                new SearchColumn("Severity", "Field/Severity", "Project Auditor/Severity") { width = 90 },
                new SearchColumn("Reason") { width = 200 },
                new SearchColumn("Description") { width = 400 },
                new SearchColumn("Path") { width = 350 },
            };

            return new SearchTable(k_ProviderDisplayName, columns);
        }

        // Creates a read-only "icon + label" cell for the search table.
        static VisualElement CreateIconLabelCell(float iconSize)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            container.Add(new Image { name = "icon", style = { width = iconSize, height = iconSize, marginRight = 2, flexShrink = 0 } });
            container.Add(new Label { name = "label" });
            return container;
        }

        [SearchColumnProvider("Project Auditor/Category")]
        static void CategoryColumnProvider(SearchColumn column)
        {
            column.getter = args => args.item?.label;
            column.cellCreator = _ => CreateIconLabelCell(18);
            column.binder = (args, element) =>
            {
                element.Q<Image>("icon").image = args.item?.thumbnail;
                element.Q<Label>("label").text = args.value as string ?? string.Empty;
            };
        }

        // Shared by IssueSearchProvider and DescriptorSearchProvider: shows the severity icon next to its text.
        [SearchColumnProvider("Project Auditor/Severity")]
        static void SeverityColumnProvider(SearchColumn column)
        {
            column.getter = args => args.item?.GetValue("Severity");
            column.cellCreator = _ => CreateIconLabelCell(16);
            column.comparer = CompareSeverity;
            column.binder = (args, element) =>
            {
                var text = args.value as string;
                var icon = element.Q<Image>("icon");
                var label = element.Q<Label>("label");

                if (TryParseSeverity(args.value as string, out var severity))
                {
                    icon.image = Utility.GetSeverityIcon(severity).image;
                    label.text = text;
                }
                else
                {
                    icon.image = null;
                    label.text = text ?? string.Empty;
                }
            };
        }

        static bool TryParseSeverity(string text, out Severity severity)
        {
            if (string.IsNullOrEmpty(text))
            {
                severity = Severity.None;
                return false;
            }

            return Enum.TryParse(text, true, out severity);
        }

        static int CompareSeverity(SearchColumnCompareArgs args)
        {
            var order = args.sortAscending ? 1 : -1;
            var lhsText = args.lhs.value as string;
            var rhsText = args.rhs.value as string;

            if (TryParseSeverity(lhsText, out var lhs) && TryParseSeverity(rhsText, out var rhs))
                return order * ((int)lhs).CompareTo((int)rhs);

            return order * string.Compare(lhsText, rhsText, StringComparison.OrdinalIgnoreCase);
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            switch (item.label)
            {
                case "Texture":
                    return EditorGUIUtility.LoadIcon("Texture Icon");
                case "Mesh":
                    return EditorGUIUtility.LoadIcon("Mesh Icon");
                case "Audio":
                    return EditorGUIUtility.LoadIcon("AudioClip Icon");
                case "Shader":
                    return EditorGUIUtility.LoadIcon("Shader Icon");
                case "Material":
                    return EditorGUIUtility.LoadIcon("Material Icon");
                default:
                    return GetSearchIcon();
            }
        }

        private Report GetCurrentReport()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(ProjectAuditorWindow));
            return (windows.Length > 0) ? ((ProjectAuditorWindow)windows[0]).m_Report : null;
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new IssueSearchProvider();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
