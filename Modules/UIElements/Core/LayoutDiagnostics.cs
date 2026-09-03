// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Severity of a layout diagnostic.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal enum LayoutDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Represents a layout authoring problem detected on a visual element.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal readonly struct LayoutDiagnostic
    {
        /// <summary>Stable identifier for the rule that produced this diagnostic.</summary>
        public string ruleId { get; }

        /// <summary>Severity of the diagnostic.</summary>
        public LayoutDiagnosticSeverity severity { get; }

        /// <summary>One-line title summarizing the problem.</summary>
        public string title { get; }

        /// <summary>Multi-line description of what is wrong and the visible effect on layout.</summary>
        public string description { get; }

        /// <summary>Suggested actionable fix the user can apply.</summary>
        public string action { get; }


        public string details { get; }

        /// <summary>The element where the problem was detected (typically the parent / container).</summary>
        public VisualElement element { get; }

        public LayoutDiagnostic(string ruleId, LayoutDiagnosticSeverity severity, string title, string description, string action, VisualElement element, string details = null)
        {
            this.ruleId = ruleId;
            this.severity = severity;
            this.title = title;
            this.description = description;
            this.action = action;
            this.details = details;
            this.element = element;
        }
    }

    /// <summary>
    /// A rule that inspects a single visual element and emits zero or more diagnostics.
    /// Implementations should be cheap and self-contained so that they can be run on every
    /// element of a panel without significant cost.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal interface ILayoutDiagnosticRule
    {
        string id { get; }
        LayoutDiagnosticSeverity severity { get; }
        string title { get; }

        void Analyze(VisualElement element, List<LayoutDiagnostic> output);
    }

    /// <summary>
    /// Static entry point for running layout authoring diagnostics on a visual element subtree.
    /// Diagnostics are intentionally kept structural (they look at flex / size styles); they do not
    /// depend on the rendered pixel output, which makes them safe to run during tests or in the
    /// debugger without a rendering pass.
    /// </summary>
    /// <remarks>
    /// New rules can be registered with <see cref="RegisterRule"/> and removed with
    /// <see cref="UnregisterRule"/>. The <see cref="SingleFlexChildOptimizationBugRule"/> built-in
    /// rule is always registered on first access.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal static partial class LayoutDiagnostics
    {
        [AutoStaticsCleanupOnCodeReload] // registered rules may be user code; re-created with the built-in rule
        static readonly List<ILayoutDiagnosticRule> s_Rules = new()
        {
            new SingleFlexChildOptimizationBugRule(),
        };

        public static IReadOnlyList<ILayoutDiagnosticRule> rules => s_Rules;

        public static void RegisterRule(ILayoutDiagnosticRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (!s_Rules.Contains(rule))
                s_Rules.Add(rule);
        }

        public static bool UnregisterRule(ILayoutDiagnosticRule rule)
        {
            return s_Rules.Remove(rule);
        }

        /// <summary>
        /// Analyzes <paramref name="root"/> and all of its descendants and returns the list of diagnostics.
        /// </summary>
        public static List<LayoutDiagnostic> Analyze(VisualElement root)
        {
            var output = new List<LayoutDiagnostic>();
            Analyze(root, output);
            return output;
        }

        /// <summary>
        /// Analyzes <paramref name="root"/> and all of its descendants and appends diagnostics to <paramref name="output"/>.
        /// </summary>
        public static void Analyze(VisualElement root, List<LayoutDiagnostic> output)
        {
            if (root == null) return;
            AnalyzeRecursive(root, output);
        }

        static void AnalyzeRecursive(VisualElement element, List<LayoutDiagnostic> output)
        {
            // Display:none elements are removed from layout entirely, so any flex/percent
            // anomaly on them is invisible to the user and not interesting to report.
            if (element.resolvedStyle.display == DisplayStyle.None)
                return;

            for (int i = 0, n = s_Rules.Count; i < n; i++)
                s_Rules[i].Analyze(element, output);

            var hierarchy = element.hierarchy;
            for (int i = 0, n = hierarchy.childCount; i < n; i++)
                AnalyzeRecursive(hierarchy[i], output);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Built-in rules
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Detects containers whose children would trigger the "single flex child" optimization that
    /// older Unity versions implemented incorrectly (UUM-110585). The optimization is meant to
    /// apply when there is a single fully flexible child (flex-grow &gt;= 1 AND flex-shrink &gt;= 1)
    /// and every other child is non-flex. Older shipping versions applied it more loosely than
    /// that and produced wrong layouts when:
    ///  * the only "fully flexible" child has flex-grow or flex-shrink in (0, 1) — always wrong, or
    ///  * a child with only flex-shrink (or only flex-grow) precedes the fully flexible child —
    ///    only wrong when the container is actually forced to shrink/grow its children past their
    ///    content size (typical editor UIs rarely hit this path, which is why the visual breakage
    ///    is often invisible until someone resizes the window or feeds in more content).
    /// The current Unity version computes this layout correctly. The rule exists so that authors
    /// upgrading from an older Unity version can see exactly which elements may render at a new
    /// size and visually verify them.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIBuilderModule")]
    internal sealed class SingleFlexChildOptimizationBugRule : ILayoutDiagnosticRule
    {
        // Distinct ids per variant so the UI groups them separately and each gets its own
        // help box and 3-part description.
        const string k_IdFlexFactor = "UITK.Layout.SingleFlexChildBug.FlexFactorBelowOne";
        const string k_IdPartialSibling = "UITK.Layout.SingleFlexChildBug.PartialFlexSibling";

        const string k_TitleFlexFactor = "Older Unity versions miscomputed this layout — flex factor below 1";
        const string k_TitlePartialSibling = "Older Unity versions miscomputed this layout — partial-flex sibling";

        // Public Unity Issue Tracker entry for this fix (internal ref: UUM-110585).
        const string k_IssueTrackerUrl = "https://issuetracker.unity3d.com/issues/flexshrink-fails-when-the-first-of-two-label-elements-is-set-to-flexgrow-equals-0-and-the-second-is-set-to-flexgrow-equals-1";

        public string id => "UITK.Layout.SingleFlexChildBug";
        public LayoutDiagnosticSeverity severity => LayoutDiagnosticSeverity.Warning;
        public string title => "Older Unity versions miscomputed this layout";

        public void Analyze(VisualElement element, List<LayoutDiagnostic> output)
        {
            var hierarchy = element.hierarchy;
            int childCount = hierarchy.childCount;
            if (childCount < 1)
                return;

            // Reproduce the buggy native logic to know whether the optimization would have fired
            // on this container, then check whether the result would actually have been wrong.
            VisualElement candidate = null;
            bool aborted = false;
            bool seenPartialFlexBeforeCandidate = false; // a child that's flex (NodeIsFlex) but not "fully flex"

            for (int i = 0; i < childCount; i++)
            {
                var child = hierarchy[i];
                var rs = child.resolvedStyle;
                if (rs.position == Position.Absolute)
                    continue;
                // Hidden children don't participate in layout (and so neither do they trigger
                // the native single-flex-child optimization). Skipping them keeps the rule
                // consistent with the rule that drives the recursive walk above.
                if (rs.display == DisplayStyle.None)
                    continue;

                float grow = rs.flexGrow;
                float shrink = rs.flexShrink;
                bool nodeIsFlex = grow != 0f || shrink != 0f;
                bool fullyFlex = grow > 0f && shrink > 0f;

                if (candidate != null)
                {
                    if (nodeIsFlex)
                    {
                        candidate = null;
                        aborted = true;
                        break;
                    }
                }
                else if (fullyFlex)
                {
                    candidate = child;
                }
                else if (nodeIsFlex)
                {
                    // Flex but only grow OR only shrink, before any fully-flex candidate is found.
                    // The buggy logic did NOT abort here, which was the source of the problem.
                    seenPartialFlexBeforeCandidate = true;
                }
            }

            if (aborted || candidate == null)
                return;

            // A candidate exists; the buggy optimization would have fired. Now check whether it
            // would actually have been wrong.
            var crs = candidate.resolvedStyle;
            float candidateGrow = crs.flexGrow;
            float candidateShrink = crs.flexShrink;

            bool partialFactor = candidateGrow < 1f || candidateShrink < 1f;
            if (!seenPartialFlexBeforeCandidate && !partialFactor)
                return; // matches the corrected logic, never affected

            // Both variants are surfaced as Warning — there is nothing wrong with the layout in
            // this Unity version, we are only informing the upgrading author that the size of
            // this element may differ from what they saw in an older Unity version.
            string variantId;
            string variantTitle;
            string description;
            string action;

            if (partialFactor)
            {
                variantId = k_IdFlexFactor;
                variantTitle = k_TitleFlexFactor;
                description =
                    $"This element may be sized differently than in older Unity versions, which " +
                    $"mis-sized a single flexible child whose <b>flex-grow={candidateGrow:0.##}</b> or " +
                    $"<b>flex-shrink={candidateShrink:0.##}</b> is below 1 (now fixed — see " +
                    $"<a href=\"{k_IssueTrackerUrl}\">Unity Issue Tracker</a>). " +
                    $"<i>Affected on every layout pass.</i>";

                action =
                    "Check this element still looks correct. If you used factors below 1 to work around " +
                    "the old behavior, you can switch to whole-number factors (>= 1) or pixel sizes.";
            }
            else
            {
                variantId = k_IdPartialSibling;
                variantTitle = k_TitlePartialSibling;
                description =
                    "This element may be sized differently than in older Unity versions, which " +
                    "mis-sized a flex container whose sibling had only <b>flex-grow</b> (or only " +
                    "<b>flex-shrink</b>) before a fully flexible child (now fixed — see " +
                    $"<a href=\"{k_IssueTrackerUrl}\">Unity Issue Tracker</a>). " +
                    "<i>Affected only when the container must shrink or grow past its content size.</i>";

                action =
                    "Check this element still looks correct when the parent is resized. If you worked " +
                    "around the old size (e.g. by adjusting a sibling), you can remove that workaround.";
            }

            // Per-occurrence detail: a compact resolvedStyle dump of each flex-relevant child.
            // Lives on the diagnostic's `details` field so the help-box description stays short
            // and the table of values can be folded away.
            var details = BuildDetails(hierarchy, childCount, candidate);

            output.Add(new LayoutDiagnostic(
                variantId, LayoutDiagnosticSeverity.Warning, variantTitle, description, action, element, details));
        }

        static string BuildDetails(VisualElement.Hierarchy hierarchy, int childCount, VisualElement candidate)
        {
            var sb = new StringBuilder();
            sb.Append("Participating children (resolved style):\n");
            for (int i = 0; i < childCount; i++)
            {
                var c = hierarchy[i];
                var rs = c.resolvedStyle;
                if (rs.position == Position.Absolute || rs.display == DisplayStyle.None)
                    continue;
                sb.Append("  [").Append(i).Append("] ").Append(c.GetType().Name);
                if (!string.IsNullOrEmpty(c.name))
                    sb.Append('#').Append(c.name);
                sb.Append(" grow=").Append(rs.flexGrow.ToString("0.##"))
                    .Append(" shrink=").Append(rs.flexShrink.ToString("0.##"));
                if (c == candidate)
                    sb.Append("  <-- single flex-child candidate");
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }

}
