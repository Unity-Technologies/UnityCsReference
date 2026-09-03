// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.UIElements.Debugger.UIElementsDebuggerImpl;

namespace UnityEditor.UIElements.Debugger
{
    /// <summary>
    /// Low-level UI Debugger section that runs <see cref="LayoutDiagnostics"/> on the active panel
    /// and surfaces detected authoring problems as <see cref="HelpBox"/> entries with an action to
    /// jump to the offending element.
    ///
    /// This is a "low-level" foldout: it is gated by <c>UIToolkitProjectSettings.EnableLowLevelDebugger</c>
    /// and is collapsed by default. Analysis only runs when the foldout is expanded so the cost is
    /// zero when the user is not actively looking at it.
    /// </summary>
    internal class LayoutDiagnosticsTab : DebuggerFoldout
    {
        readonly DebuggerSelection m_Selection;
        readonly VisualElement m_DiagnosticsContainer;
        readonly HelpBox m_NoIssuesBox;
        readonly Label m_SummaryLabel;
        readonly Button m_ReanalyzeButton;

        readonly List<LayoutDiagnostic> m_DiagnosticsCache = new();

        // The panel root we last built the UI for. The base DebuggerFoldout calls Refresh() on every
        // selection change (hovering the hierarchy re-picks the selected element), but this tab
        // analyzes the whole panel, not the selected element. Rebuilding on every hover would discard
        // the user's expanded group/details foldouts. We only rebuild when the analyzed root actually
        // changes; the "Re-analyze" button forces a rebuild by clearing this first.
        VisualElement m_LastAnalyzedRoot;

        public LayoutDiagnosticsTab(DebuggerSelection debuggerSelection)
            : base("Layout Diagnostics (low-level)", debuggerSelection, isLowLevel: true)
        {
            m_Selection = debuggerSelection;

            // Header: a short summary and a manual re-run button. The button is useful because a
            // panel can change after the foldout was opened, and we don't want to re-run the analysis
            // on every layout change (which would be wasteful for a tool that is only consulted
            // occasionally).
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 4,
                }
            };
            m_SummaryLabel = new Label
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
            header.Add(m_SummaryLabel);

            m_ReanalyzeButton = new Button(ForceRefresh) { text = "Re-analyze" };
            m_ReanalyzeButton.style.flexGrow = 0;
            m_ReanalyzeButton.style.flexShrink = 0;
            header.Add(m_ReanalyzeButton);

            // The project-wide UXML scanner used to live behind a [MenuItem] but a [MenuItem]
            // can't be hidden at runtime — only greyed out. Hosting the entry point on this
            // foldout means the project scan is only reachable when the low-level debugger
            // setting is on (the foldout itself is already gated by DebuggerFoldout).
            var scanButton = new Button(ScanProjectUxml) { text = "Scan Project UXML" };
            scanButton.tooltip = "Run layout diagnostics on every VisualTreeAsset in the project; results are written to the console.";
            scanButton.style.flexGrow = 0;
            scanButton.style.flexShrink = 0;
            scanButton.style.marginLeft = 4;
            header.Add(scanButton);

            // The raw-UXML scan only sees styles authored inline or linked with <Style> in the
            // asset. Many editor UIs (e.g. the UI Builder panes) get their stylesheet applied
            // programmatically by the host window and assemble part of their hierarchy in C#, so
            // their flex layout never resolves when a UXML is cloned in isolation — which is why
            // such cases slip past both the UXML scan and the per-fixture test action. Scanning the
            // *live* panels of every open window analyzes fully-resolved styles and catches them.
            var scanPanelsButton = new Button(ScanOpenPanels) { text = "Scan Open Windows" };
            scanPanelsButton.tooltip = "Run layout diagnostics on every currently-open panel (fully resolved styles); results are written to the console.";
            scanPanelsButton.style.flexGrow = 0;
            scanPanelsButton.style.flexShrink = 0;
            scanPanelsButton.style.marginLeft = 4;
            header.Add(scanPanelsButton);
            Add(header);

            m_NoIssuesBox = new HelpBox(
                "No issues detected by the registered low-level layout rules.",
                HelpBoxMessageType.Info);
            Add(m_NoIssuesBox);

            m_DiagnosticsContainer = new VisualElement();
            Add(m_DiagnosticsContainer);
        }

        // Forces a full re-analysis even if the panel root has not changed. Wired to the
        // "Re-analyze" button, since the panel's content can change without the root changing.
        void ForceRefresh()
        {
            m_LastAnalyzedRoot = null;
            Refresh();
        }

        protected override void Refresh()
        {
            var panel = m_Selection.panel;
            var root = panel?.visualTree;

            // Skip the rebuild when nothing relevant changed (e.g. the user is just hovering the
            // hierarchy, which re-fires selection). Rebuilding would collapse any group/details
            // foldout the user expanded. ForceRefresh() clears m_LastAnalyzedRoot to bypass this.
            if (root == m_LastAnalyzedRoot)
                return;
            m_LastAnalyzedRoot = root;

            m_DiagnosticsContainer.Clear();
            m_DiagnosticsCache.Clear();

            if (root == null)
            {
                m_SummaryLabel.text = "No panel selected.";
                m_NoIssuesBox.style.display = DisplayStyle.None;
                return;
            }

            LayoutDiagnostics.Analyze(root, m_DiagnosticsCache);

            int errorCount = 0;
            int warningCount = 0;
            int infoCount = 0;
            for (int i = 0; i < m_DiagnosticsCache.Count; i++)
            {
                switch (m_DiagnosticsCache[i].severity)
                {
                    case LayoutDiagnosticSeverity.Error: errorCount++; break;
                    case LayoutDiagnosticSeverity.Warning: warningCount++; break;
                    default: infoCount++; break;
                }
            }

            if (m_DiagnosticsCache.Count == 0)
            {
                m_SummaryLabel.text = $"Analyzed panel '{root.name ?? "?"}': no issues found.";
                m_NoIssuesBox.style.display = DisplayStyle.Flex;
                return;
            }

            m_NoIssuesBox.style.display = DisplayStyle.None;
            m_SummaryLabel.text = $"{errorCount} error(s), {warningCount} warning(s), {infoCount} info " +
                                  $"across {m_DiagnosticsCache.Count} occurrence(s).";

            // Group diagnostics by rule id so users see all instances of the same problem together.
            // Within a group we list each occurrence with its own select button.
            var groups = new Dictionary<string, List<int>>();
            for (int i = 0; i < m_DiagnosticsCache.Count; i++)
            {
                var diag = m_DiagnosticsCache[i];
                if (!groups.TryGetValue(diag.ruleId, out var list))
                {
                    list = new List<int>();
                    groups[diag.ruleId] = list;
                }
                list.Add(i);
            }

            foreach (var group in groups)
            {
                var first = m_DiagnosticsCache[group.Value[0]];
                var groupFoldout = new Foldout
                {
                    text = $"[{group.Value.Count}] {first.title}",
                    value = first.severity == LayoutDiagnosticSeverity.Error,
                };
                groupFoldout.AddToClassList("unity-layout-diagnostics__group");

                var helpBox = new HelpBox(
                    $"{first.description}\n\n<b>Suggested action:</b> {first.action}",
                    SeverityToHelpBoxType(first.severity));
                groupFoldout.Add(helpBox);

                foreach (var index in group.Value)
                {
                    var diag = m_DiagnosticsCache[index];
                    groupFoldout.Add(BuildOccurrenceRow(diag));
                }

                m_DiagnosticsContainer.Add(groupFoldout);
            }
        }

        VisualElement BuildOccurrenceRow(LayoutDiagnostic diag)
        {

            var wrapper = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column },
            };

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 2,
                    marginBottom = 2,
                }
            };

            var label = new Label(BuildElementSummary(diag.element))
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginRight = 4,
                }
            };
            label.tooltip = $"{diag.ruleId}\n{label.text}";
            row.Add(label);

            var selectButton = new Button(() => SelectElement(diag.element))
            {
                text = "Select",
            };
            selectButton.style.flexGrow = 0;
            selectButton.style.flexShrink = 0;
            row.Add(selectButton);
            wrapper.Add(row);

            if (!string.IsNullOrEmpty(diag.details))
            {
                var detailsFoldout = new Foldout
                {
                    text = "Details",
                    value = false,
                };
                detailsFoldout.style.marginLeft = 12;
                detailsFoldout.AddToClassList("unity-layout-diagnostics__details");

                var detailsLabel = new Label(diag.details)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                    }
                };
                detailsFoldout.Add(detailsLabel);
                wrapper.Add(detailsFoldout);
            }

            return wrapper;
        }

        void SelectElement(VisualElement element)
        {
            if (element == null)
                return;
            m_Selection.element = element;
        }

        static HelpBoxMessageType SeverityToHelpBoxType(LayoutDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case LayoutDiagnosticSeverity.Error: return HelpBoxMessageType.Error;
                case LayoutDiagnosticSeverity.Warning: return HelpBoxMessageType.Warning;
                default: return HelpBoxMessageType.Info;
            }
        }

        static string BuildElementSummary(VisualElement element)
        {
            if (element == null)
                return "<null>";
            var typeName = element.GetType().Name;
            var name = string.IsNullOrEmpty(element.name) ? "(no name)" : element.name;
            var className = element.GetClasses() != null ? string.Join(".", element.GetClasses()) : "";
            return string.IsNullOrEmpty(className)
                ? $"{typeName} • {name}"
                : $"{typeName} • {name} • .{className}";
        }

        // Scans every VisualTreeAsset in the project. Each asset is instantiated into a single
        // off-screen scratch window so computedStyle resolves through the cascade; results go to
        // the console rather than the foldout because the volume can be large.
        static void ScanProjectUxml()
        {
            var guids = AssetDatabase.FindAssets("t:VisualTreeAsset");
            if (guids == null || guids.Length == 0)
            {
                Debug.Log("[LayoutDiagnostics] No UXML assets found in project.");
                return;
            }

            var totals = new Dictionary<string, int>();
            int filesWithIssues = 0;
            int totalIssues = 0;
            var report = new StringBuilder();
            report.Append("[LayoutDiagnostics] Scanning ").Append(guids.Length).Append(" UXML asset(s)...\n");

            var host = ScriptableObject.CreateInstance<UxmlScannerWindow>();
            try
            {
                host.position = new Rect(-10000, -10000, 800, 600);
                host.ShowWithMode(ShowMode.Utility);

                try
                {
                    EditorUtility.DisplayProgressBar("Layout Diagnostics", "Scanning UXML...", 0f);

                    // Refreshing the progress bar forces a repaint, so on large projects we only
                    // update it ~100 times total rather than on every asset. Tiny projects (< 100
                    // assets) still update every iteration.
                    int progressUpdateInterval = Math.Max(1, guids.Length / 100);

                    for (int i = 0; i < guids.Length; i++)
                    {
                        var guid = guids[i];
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path))
                            continue;

                        if (i % progressUpdateInterval == 0)
                            EditorUtility.DisplayProgressBar(
                                "Layout Diagnostics",
                                $"Scanning {path} ({i + 1}/{guids.Length})",
                                (float)i / guids.Length);

                        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                        if (asset == null)
                            continue;

                        try
                        {
                            host.LoadAsset(asset);
                            var diagnostics = LayoutDiagnostics.Analyze(host.rootVisualElement);
                            if (diagnostics.Count == 0)
                                continue;

                            filesWithIssues++;
                            totalIssues += diagnostics.Count;

                            report.Append("\n  ").Append(path).Append(":\n");
                            for (int j = 0; j < diagnostics.Count; j++)
                            {
                                var d = diagnostics[j];
                                if (totals.TryGetValue(d.ruleId, out var c))
                                    totals[d.ruleId] = c + 1;
                                else
                                    totals[d.ruleId] = 1;

                                report.Append("    [").Append(d.severity).Append("] ")
                                    .Append(d.ruleId).Append(": ").Append(d.title).Append('\n');
                            }
                        }
                        catch (Exception e)
                        {
                            // Custom controls / missing types can throw on instantiate; skip those
                            // assets rather than aborting the whole scan.
                            report.Append("    skipped: ").Append(e.GetType().Name).Append(": ")
                                .Append(e.Message).Append('\n');
                        }
                        finally
                        {
                            host.ClearAsset();
                        }
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            finally
            {
                host.Close();
            }

            if (totalIssues == 0)
            {
                Debug.Log("[LayoutDiagnostics] No issues found in any UXML asset.");
                return;
            }

            report.Append("\nSummary: ").Append(totalIssues).Append(" issue(s) across ")
                .Append(filesWithIssues).Append(" file(s).");
            foreach (var kvp in totals)
                report.Append("\n  ").Append(kvp.Key).Append(": ").Append(kvp.Value);
            Debug.LogWarning(report.ToString());
        }

        // Scans every currently-open panel. Unlike the UXML scan, this runs against live visual
        // trees whose styles are already resolved through the full cascade (including stylesheets
        // applied programmatically by the host window), so it reports the layouts the user is
        // actually looking at. Results go to the console.
        static void ScanOpenPanels()
        {
            var totals = new Dictionary<string, int>();
            int panelsWithIssues = 0;
            int totalIssues = 0;
            int panelsScanned = 0;
            var report = new StringBuilder();
            report.Append("[LayoutDiagnostics] Scanning open panels...\n");

            var it = UIElementsUtility.GetPanelsIterator();
            while (it.MoveNext())
            {
                var panel = it.Current.Value;
                // All live panels, editor and runtime (the UUM-110585 bug affects both). Skip
                // disposed panels only.
                if (panel == null || panel.disposed)
                    continue;
                var root = panel.visualTree;
                if (root == null)
                    continue;

                panelsScanned++;

                List<LayoutDiagnostic> diagnostics;
                try
                {
                    diagnostics = LayoutDiagnostics.Analyze(root);
                }
                catch (Exception e)
                {
                    // A single misbehaving panel/element shouldn't abort the whole scan.
                    report.Append("\n  Panel '").Append(SafePanelName(panel, root)).Append("': skipped (")
                        .Append(e.GetType().Name).Append(": ").Append(e.Message).Append(")\n");
                    continue;
                }

                if (diagnostics.Count == 0)
                    continue;

                panelsWithIssues++;
                totalIssues += diagnostics.Count;

                report.Append("\n  Panel '").Append(SafePanelName(panel, root)).Append("':\n");
                for (int j = 0; j < diagnostics.Count; j++)
                {
                    var d = diagnostics[j];
                    totals.TryGetValue(d.ruleId, out var c);
                    totals[d.ruleId] = c + 1;
                    report.Append("    [").Append(d.severity).Append("] ")
                        .Append(d.ruleId).Append(": ").Append(BuildElementSummary(d.element)).Append('\n');
                }
            }

            if (totalIssues == 0)
            {
                Debug.Log($"[LayoutDiagnostics] No issues found across {panelsScanned} open panel(s).");
                return;
            }

            report.Append("\nSummary: ").Append(totalIssues).Append(" issue(s) across ")
                .Append(panelsWithIssues).Append(" of ").Append(panelsScanned).Append(" open panel(s).");
            foreach (var kvp in totals)
                report.Append("\n  ").Append(kvp.Key).Append(": ").Append(kvp.Value);
            Debug.LogWarning(report.ToString());
        }

        // Builds a human-readable label for a panel. The root container's name is usually just
        // "unity-panel-container", which says nothing, so prefer the owning EditorWindow's title
        // (or the runtime PanelSettings' name) and tag it with the context type.
        static string SafePanelName(Panel panel, VisualElement root)
        {
            string name = null;

            var owner = panel?.ownerObject;
            // Editor window panels: the owner is a HostView whose actualView is the EditorWindow.
            if (owner is HostView hostView && hostView.actualView != null)
            {
                var title = hostView.actualView.titleContent?.text;
                name = string.IsNullOrEmpty(title) ? hostView.actualView.GetType().Name : title;
            }
            // Runtime (UIDocument) panels and others: the owner's own name is meaningful.
            else if (owner != null && !string.IsNullOrEmpty(owner.name))
            {
                name = owner.name;
            }

            // Fall back to the root container / panel name.
            if (string.IsNullOrEmpty(name))
                name = root != null && !string.IsNullOrEmpty(root.name) ? root.name : panel?.name;
            if (string.IsNullOrEmpty(name))
                name = "<unnamed>";

            return panel != null ? $"{name} [{panel.contextType}]" : name;
        }

        // Off-screen host used by the project scanner to give each UXML a panel and a style
        // cascade so computedStyle resolves correctly when the analyzer reads it.
        sealed class UxmlScannerWindow : EditorWindow
        {
            #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
            UxmlScannerWindow() { }
            #pragma warning restore UAL0015

            public void LoadAsset(VisualTreeAsset asset)
            {
                rootVisualElement.Clear();
                asset.CloneTree(rootVisualElement);
                rootVisualElement.MarkDirtyRepaint();
                Repaint();
            }

            public void ClearAsset()
            {
                rootVisualElement.Clear();
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
