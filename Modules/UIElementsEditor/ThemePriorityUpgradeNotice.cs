// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // One-time console notice after upgrading to the tiered theme priority model (UUM-108227):
    // lists the themes that extend the built-in theme and also target Unity-styled elements, since
    // those see the biggest behavior change. Runs as a startup scan gated by a project-wide
    // marker rather than hooking imports: the upgrade reimport spans several refresh passes and
    // domain reloads, so there is no reliable "last import batch" to observe.
    [InitializeOnLoad]
    static class ThemePriorityUpgradeNotice
    {
        internal const int currentNoticeVersion = 1;

        static readonly string k_NoticeHeader = L10n.Tr(
            "UI Toolkit: theme style sheets now override Unity's built-in styles regardless of selector " +
            "specificity.\n" +
            "The following themes extend the built-in theme and style Unity controls; review their " +
            "contents to ensure selectors work as intended:\n", null);

        static readonly string k_NoticeFooter = L10n.Tr(
            "The previous behavior can be restored with the 'Legacy theme priority' setting in " +
            "Project Settings > UI Toolkit.", null);

        static readonly string k_NoticeShownOnce = L10n.Tr("This notice is shown once per project.", null);

        struct AffectedSelector
        {
            public StyleSheet sheet;
            public StyleComplexSelector complexSelector;
        }

        [NoAutoStaticsCleanup] // type names are stable for the session; rebuilt after domain reload
        static HashSet<string> s_UnityElementTypeNames;

        static HashSet<string> unityElementTypeNames
        {
            get
            {
                if (s_UnityElementTypeNames == null)
                {
                    s_UnityElementTypeNames = new HashSet<string> { nameof(VisualElement) };
                    foreach (var type in TypeCache.GetTypesDerivedFrom<VisualElement>("UnityEngine.UIElementsModule"))
                        s_UnityElementTypeNames.Add(type.Name);
                }
                return s_UnityElementTypeNames;
            }
        }

        static ThemePriorityUpgradeNotice()
        {
            // CI never commits the marker, so it would log on every run
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += ShowOnce;
        }

        static void ShowOnce()
        {
            if (UIToolkitProjectSettings.themePriorityNoticeVersion >= currentNoticeVersion
                || UIToolkitProjectSettings.enableLegacyThemePriority)
                return;

            LogAffectedThemes(onDemand: false);
            UIToolkitProjectSettings.themePriorityNoticeVersion = currentNoticeVersion;
        }

        // Also invoked on demand from the UI Toolkit project settings pane; on-demand scans report
        // clean results and skip the once-per-project note. Emits one consolidated warning listing
        // each offending selector with its owning sheet and line. A markdown report with the same
        // content is written to Logs/ (ephemeral, not version controlled) so the results are
        // readable outside the Editor and consumable by tooling.
        internal static void LogAffectedThemes(bool onDemand)
        {
            var themePaths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ThemeStyleSheet"))
                themePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            themePaths.Sort();

            // Warn only about selectors the current user can edit, classified per owning sheet:
            // a user theme can import USS from a read-only package (and a package theme owns none
            // of its content). Read-only hits are the package author's responsibility - the
            // author sees the warning in their own project, where the package is editable - so
            // they go to the report only.
            var affected = new List<(string path, List<AffectedSelector> editableHits, List<AffectedSelector> readOnlyHits)>();
            foreach (var path in themePaths)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                if (theme == null || !ImportsBuiltinTheme(theme))
                    continue;

                var hits = new List<AffectedSelector>();
                CollectAffectedSelectors(theme, hits);
                if (hits.Count == 0)
                    continue;

                var editableHits = new List<AffectedSelector>();
                var readOnlyHits = new List<AffectedSelector>();
                foreach (var hit in hits)
                {
                    var ownerPath = hit.sheet == theme ? path : AssetDatabase.GetAssetPath(hit.sheet);
                    (IsEditable(ownerPath) ? editableHits : readOnlyHits).Add(hit);
                }
                affected.Add((path, editableHits, readOnlyHits));
            }

            if (affected.Count == 0)
            {
                if (onDemand)
                    Debug.Log(L10n.Tr("UI Toolkit: no theme extending the built-in theme and styling Unity controls was found.", null));
                return;
            }

            var report = new StringBuilder();
            foreach (var (path, editableHits, readOnlyHits) in affected)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                report.Append("## ").AppendLine(path);
                foreach (var hit in editableHits)
                    AppendHitToReport(report, hit, theme);
                foreach (var hit in readOnlyHits)
                    AppendHitToReport(report, hit, theme, " — read-only package content");
                report.AppendLine();
            }
            var reportPath = WriteReport(report);

            var anyEditable = false;
            var message = new StringBuilder(k_NoticeHeader);
            if (reportPath != null)
                message.Append(L10n.Tr("Full report: ", null)).AppendLine(reportPath);
            foreach (var (path, editableHits, readOnlyHits) in affected)
            {
                if (editableHits.Count == 0)
                    continue;

                anyEditable = true;
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                message.Append("Theme: ").AppendLine(path);
                foreach (var hit in editableHits)
                {
                    message.Append("  - ").Append(SelectorText(hit))
                        .Append("  (").Append(OwnerText(hit, theme)).Append(", line ")
                        .Append(hit.complexSelector.rule.line).AppendLine(")");
                }
                if (readOnlyHits.Count > 0)
                    message.Append("  ").AppendLine(string.Format(
                        L10n.Tr("(+{0} selectors in read-only package content; see the report)", null), readOnlyHits.Count));
            }
            message.Append(k_NoticeFooter);
            if (!onDemand)
                message.AppendLine().Append(k_NoticeShownOnce);

            if (anyEditable)
                Debug.LogWarning(message.ToString());
            else if (onDemand && reportPath != null)
                Debug.Log(string.Format(
                    L10n.Tr("UI Toolkit: only read-only package content is affected; see the report at {0}", null), reportPath));
        }

        static void AppendHitToReport(StringBuilder report, in AffectedSelector hit, ThemeStyleSheet theme, string annotation = null)
        {
            report.Append("- `").Append(SelectorText(hit))
                .Append("` — ").Append(OwnerText(hit, theme)).Append(':')
                .Append(hit.complexSelector.rule.line);
            if (annotation != null)
                report.Append(annotation);
            report.AppendLine();
        }

        static bool IsEditable(string assetPath)
        {
            if (assetPath.StartsWith("Assets/"))
                return true;

            var packageInfo = PackageManager.PackageInfo.FindForAssetPath(assetPath);
            return packageInfo != null
                && (packageInfo.source == PackageManager.PackageSource.Embedded
                    || packageInfo.source == PackageManager.PackageSource.Local);
        }

        static string SelectorText(in AffectedSelector hit) =>
            StyleSheetExporter.Default.ToUssString(hit.sheet, hit.complexSelector);

        static string OwnerText(in AffectedSelector hit, ThemeStyleSheet theme) =>
            hit.sheet == theme ? "theme" : AssetDatabase.GetAssetPath(hit.sheet);

        static string WriteReport(StringBuilder body)
        {
            try
            {
                Directory.CreateDirectory("Logs");
                var reportPath = $"Logs/UIToolkitThemePriorityReport-{DateTime.Now:yyyyMMdd-HHmmss}.md";
                var content = new StringBuilder()
                    .AppendLine("# UI Toolkit theme priority scan")
                    .AppendLine()
                    .AppendLine("This report lists the themes that extend the built-in theme and style Unity")
                    .AppendLine("controls. Their selectors now override Unity's built-in styles regardless of")
                    .AppendLine("specificity; review them to ensure they work as intended. Regenerate this")
                    .AppendLine("report with 'Scan affected themes' in Project Settings > UI Toolkit.")
                    .AppendLine()
                    .Append(body);
                File.WriteAllText(reportPath, content.ToString());
                return reportPath;
            }
            catch (IOException)
            {
                // Console warnings above already carry the same information
                return null;
            }
        }

        internal static bool IsAffectedTheme(ThemeStyleSheet theme)
        {
            if (!ImportsBuiltinTheme(theme))
                return false;

            var hits = new List<AffectedSelector>();
            CollectAffectedSelectors(theme, hits);
            return hits.Count > 0;
        }

        static bool ImportsBuiltinTheme(StyleSheet sheet)
        {
            // The embedded unity-theme:// copy is the only import carrying an explicit Builtin
            // tier — stamped as part of the Editor Resources build (UIElementsStyleSheetGenerator)
            var flattenedImports = sheet.flattenedRecursiveImports;
            if (flattenedImports == null)
                return false;

            foreach (var imported in flattenedImports)
            {
                if (imported != null && imported.priority == StyleSheetPriority.Builtin)
                    return true;
            }
            return false;
        }

        // Scans the theme's own rules and, recursively, every non-builtin USS it imports — user
        // themes typically keep their rules in imported sheets rather than the .tss body. The
        // flattened import list preserves duplicates (diamond imports), so track visited sheets.
        static void CollectAffectedSelectors(ThemeStyleSheet theme, List<AffectedSelector> hits)
        {
            ScanSheet(theme, hits);

            var flattenedImports = theme.flattenedRecursiveImports;
            if (flattenedImports == null)
                return;

            var visited = new HashSet<StyleSheet>();
            foreach (var imported in flattenedImports)
            {
                if (imported != null && imported.priority != StyleSheetPriority.Builtin && visited.Add(imported))
                    ScanSheet(imported, hits);
            }
        }

        // Heuristic, and deliberately so (it only gates a one-time informational warning): selector
        // tokens do not need to match builtin tokens textually to collide at runtime, so match
        // unity-* classes, wildcards, and type selectors naming Unity control types.
        static void ScanSheet(StyleSheet sheet, List<AffectedSelector> hits)
        {
            if (sheet.rules == null)
                return;

            foreach (var rule in sheet.rules)
            {
                // A selector on an empty rule cannot change anything
                if (rule.complexSelectors == null || rule.properties == null || rule.properties.Length == 0)
                    continue;

                foreach (var complexSelector in rule.complexSelectors)
                {
                    if (complexSelector?.selectors == null)
                        continue;

                    if (TargetsUnityStyledElements(complexSelector))
                        hits.Add(new AffectedSelector { sheet = sheet, complexSelector = complexSelector });
                }
            }
        }

        static bool TargetsUnityStyledElements(StyleComplexSelector complexSelector)
        {
            foreach (var selector in complexSelector.selectors)
            {
                foreach (var part in selector.parts)
                {
                    switch (part.type)
                    {
                        case StyleSelectorType.Wildcard:
                            return true;
                        case StyleSelectorType.Class when part.value.StartsWith("unity-"):
                            return true;
                        case StyleSelectorType.Type when unityElementTypeNames.Contains(part.value):
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
