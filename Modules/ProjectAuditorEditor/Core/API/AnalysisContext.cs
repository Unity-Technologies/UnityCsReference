// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Base class for a context object passed by a Module to an Analyzer's Analyze() method.
    /// </summary>
    /// <remarks>
    /// AnalysisContext provides information to the Analyze() method which is used to decide which issues to report.
    /// It also provides helper methods to build Issues or Insights.
    /// </remarks>
    public class AnalysisContext
    {
        /// <summary>
        /// The AnalysisParams object that was passed to (or created by) the ProjectAuditor's Audit() method.
        /// </summary>
        /// <remarks>
        /// This contains information that can be useful during analysis: for example, the analysis target platform, or
        /// information to determine whether a particular Descriptor is applicable in the current analysis.
        /// </remarks>
        public AnalysisParams Params;

        /// <summary>
        /// Create a ReportItemBuilder for an Issue: a potential problem in the project, with an actionable
        /// recommendation to resolve it.
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="id">Descriptor ID</param>
        /// <param name="messageArgs">Arguments to be used in the message formatting</param>
        /// <returns>
        /// The ReportItemBuilder, constructed with the specified category, descriptor ID and message arguments
        /// </returns>
        public ReportItemBuilder CreateIssue(IssueCategory category, string id, params object[] messageArgs)
        {
            return new ReportItemBuilder(category, id, messageArgs);
        }

        /// <summary>
        /// Create a ReportItemBuilder for an Insight: A ReportItem collected for informational purposes.
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="description">User-friendly description</param>
        /// <returns>The ReportItemBuilder, constructed with the specified category and description string</returns>
        public ReportItemBuilder CreateInsight(IssueCategory category, string description)
        {
            return new ReportItemBuilder(category, description);
        }

        /// <summary>
        /// Checks whether a given Descriptor is enabled for the current analysis.
        /// </summary>
        /// <param name="descriptor">The descriptor to check</param>
        /// <returns>True if the Descriptor is applicable to the current target platform and Unity version, and if it's
        /// either enabled by default or by a Rule specified in Params. Otherwise, returns false.</returns>
        /// <remarks>
        /// The analysis for some Issues can take a long time to run, particularly in a large project. The Descriptors
        /// for such issues may declare <see cref="Descriptor.IsEnabledByDefault"/> to be false to stop them running
        /// when running Project Auditor interactively in the Editor. When running Project Auditor in a CI/CD environment
        /// it may be desirable to re-enable analysis for these Descriptors.
        /// <see cref="AnalysisParams.WithAdditionalDiagnosticRules"/> can be used to add temporary Rules to increase
        /// the <see cref="Severity"/> of a Descriptor to anything other than Severity.None in order to re-enable
        /// analysis in this context.
        /// </remarks>
        public bool IsDescriptorEnabled(Descriptor descriptor)
        {
            if (!descriptor.IsApplicable(Params))
                return false;

            var rule = Params.Rules.GetRule(descriptor.Id);
            if (rule != null)
                return rule.Severity != Severity.None;

            return descriptor.IsEnabledByDefault;
        }
    }
}
