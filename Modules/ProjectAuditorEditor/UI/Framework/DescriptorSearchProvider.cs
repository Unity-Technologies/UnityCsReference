// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Profiling not yet converted
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.Core;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    partial class DescriptorSearchProvider : SearchProvider
    {
        public const string kProviderId = "project-auditor-descriptors";
        private const string kProviderFilterId = "auditordescriptors:";
        private const string k_ProviderDisplayName = "Project Auditor Issue Types";

        // Display values for the "Suppressed" column and the suppressed=<value> filter.
        private const string k_SuppressedYes = "Yes";
        private const string k_SuppressedNo = "No";

        [AutoStaticsCleanupOnCodeReload]
        private static Texture2D s_SearchIcon;

        private DescriptorSearchProvider() : base(kProviderId, k_ProviderDisplayName)
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

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            var sb = new StringBuilder();

            // Areas
            {
                var areas = new List<Areas>(AreasExtensions.AlphabeticalAreas);

                foreach (var area in areas)
                    sb.Append($"\"{area}\", ");
                var allAreas = sb.ToString().TrimEnd(',', ' ');
                foreach (var area in areas)
                    yield return new SearchProposition(category: "Area", label: area.ToFrontendString(), replacement: $"area=<$list:\"{area}\", [{allAreas}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: GetSearchIcon(), color: QueryColors.filter);
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

            // Suppressed
            {
                var suppressedOptions = new[] { k_SuppressedYes, k_SuppressedNo };
                foreach (var option in suppressedOptions)
                    sb.Append($"\"{option}\", ");
                var allOptions = sb.ToString().TrimEnd(',', ' ');
                foreach (var option in suppressedOptions)
                    yield return new SearchProposition(category: "Suppressed", label: option, replacement: $"suppressed=<$list:\"{option}\", [{allOptions}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: GetSearchIcon(), color: QueryColors.filter);
                sb.Clear();
            }
        }

        IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var areaFilter = Areas.None;
            var severity = Severity.None;
            bool? suppressedFilter = null;
            var textQuery = context.searchQuery ?? "";

            var areaMatch = Regex.Match(textQuery, @"area=([^;\s]+)", RegexOptions.IgnoreCase);
            if (areaMatch.Success)
            {
                Enum.TryParse(areaMatch.Groups[1].Value.Trim('\"'), true, out areaFilter);
                textQuery = textQuery.Replace(areaMatch.Value, "");
            }

            var severityMatch = Regex.Match(textQuery, @"severity=([^;\s]+)", RegexOptions.IgnoreCase);
            if (severityMatch.Success)
            {
                Enum.TryParse(severityMatch.Groups[1].Value.Trim('\"'), true, out severity);
                textQuery = textQuery.Replace(severityMatch.Value, "");
            }

            var suppressedMatch = Regex.Match(textQuery, @"suppressed=([^;\s]+)", RegexOptions.IgnoreCase);
            if (suppressedMatch.Success)
            {
                suppressedFilter = ParseSuppressedFilter(suppressedMatch.Groups[1].Value.Trim('\"'));
                textQuery = textQuery.Replace(suppressedMatch.Value, "");
            }

            textQuery = textQuery.Trim();

            var suppressedDiagnostics = UserPreferences.BuildSuppressedDiagnosticsSet();

            foreach (var descriptor in DescriptorLibrary.GetAllDescriptors())
            {
                Areas areas = descriptor.Areas;
                var isSuppressed = suppressedDiagnostics.Contains(descriptor.Id);

                if (areaFilter != Areas.None && (areas & areaFilter) == 0)
                    continue;
                if (severity != Severity.None && descriptor.DefaultSeverity != severity)
                    continue;
                if (suppressedFilter.HasValue && isSuppressed != suppressedFilter.Value)
                    continue;

                if (!string.IsNullOrEmpty(textQuery) &&
                    descriptor.Id.IndexOf(textQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (descriptor.Title == null || descriptor.Title.IndexOf(textQuery, StringComparison.OrdinalIgnoreCase) < 0) &&
                    (descriptor.Description == null || descriptor.Description.IndexOf(textQuery, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                var label = string.IsNullOrEmpty(descriptor.Title) ? descriptor.Id : descriptor.Title;
                var item = CreateItem(
                    context,
                    descriptor.Id,
                    label,
                    descriptor.Description,
                    GetSearchIcon(),
                    descriptor);

                item.SetField("Suppressed", isSuppressed ? k_SuppressedYes : k_SuppressedNo);
                item.SetField("Id", descriptor.Id);
                item.SetField("Title", descriptor.Title);
                item.SetField("Area", areas.ToFrontendString());
                item.SetField("Severity", descriptor.DefaultSeverity.ToFrontendString());
                item.SetField("Description", descriptor.Description);

                yield return item;
            }
        }

        private static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> items)
        {
            foreach (var column in CreateColumns())
                yield return column;
        }

        private static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            return new SearchTable(k_ProviderDisplayName, CreateColumns());
        }

        private static SearchColumn[] CreateColumns()
        {
            return new[]
            {
                new SearchColumn("Suppressed", "Field/Suppressed", "Project Auditor/Suppressed") { width = 80 },
                new SearchColumn("Id", "Field/Id") { width = 70 },
                new SearchColumn("Title", "Field/Title") { width = 240 },
                new SearchColumn("Area", "Field/Area") { width = 120 },
                new SearchColumn("Severity", "Field/Severity", "Project Auditor/Severity") { width = 90 },
                new SearchColumn("Description", "Field/Description") { width = 400 },
            };
        }

        // Reads the descriptor ID for a row from the item field (item.data is not guaranteed to survive
        // into the table cell rendering path).
        private static string GetDescriptorId(SearchColumnEventArgs args)
        {
            return args.item?.GetValue("Id") as string;
        }

        // An editable checkbox indicating whether the issue type is suppressed.
        // Uses the UITK cell approach (getter/setter/cellCreator/binder) rather than an IMGUI drawer,
        // which does not render reliably inside the search table's cells.
        [SearchColumnProvider("Project Auditor/Suppressed")]
        internal static void SuppressedColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var id = GetDescriptorId(args);
                if (string.IsNullOrEmpty(id))
                    return false;
                return UserPreferences.BuildSuppressedDiagnosticsSet().Contains(id);
            };
            column.setter = args =>
            {
                if (args.value is not bool wantSuppressed)
                    return;

                var id = GetDescriptorId(args);
                if (string.IsNullOrEmpty(id))
                    return;

                var suppressedDiagnostics = UserPreferences.BuildSuppressedDiagnosticsSet();
                if (suppressedDiagnostics.Contains(id) == wantSuppressed)
                    return; // already in the desired state

                UserPreferences.ToggleSuppressedDiagnostic(id, suppressedDiagnostics);
                // Keep the backing field in sync so the suppressed=<value> filter reflects the change.
                args.item.SetField("Suppressed", wantSuppressed ? k_SuppressedYes : k_SuppressedNo);

                // Repaint the Preferences window (if open) so its Suppressed Issues field reflects the change.
                UserPreferences.RepaintPreferencesWindow();
            };
            column.cellCreator = _ => new Toggle { style = { alignSelf = Align.Center } };
            column.binder = (args, element) =>
            {
                if (element is Toggle toggle)
                    toggle.SetValueWithoutNotify(args.value is bool isSuppressed && isSuppressed);
            };
        }

        // Parses the value of a suppressed=<value> filter. Accepts the displayed "Yes"/"No" labels as well as
        // common boolean spellings. Returns null (no filtering) if the value isn't recognized.
        private static bool? ParseSuppressedFilter(string value)
        {
            if (value.Equals(k_SuppressedYes, StringComparison.OrdinalIgnoreCase) ||
                value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value == "1")
                return true;
            if (value.Equals(k_SuppressedNo, StringComparison.OrdinalIgnoreCase) ||
                value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                value == "0")
                return false;
            return null;
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            return GetSearchIcon();
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new DescriptorSearchProvider();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
