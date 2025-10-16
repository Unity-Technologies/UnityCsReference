// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// A delegate type for a function to determine whether the Editor supports a given BuildTargetGroup and BuildTarget.
    /// </summary>
    internal delegate bool IsASupportedBuildTarget(BuildTargetGroup buildTargetGroup, BuildTarget target);

    /// <summary>
    /// Represents an object which can be passed to an instance of <see cref="ProjectAuditor"/> to specify how analysis should be performed and to provide delegates to be called when analysis steps have completed.
    /// AnalysisParams defaults to values which instruct ProjectAuditor to analyze everything in the project for the current build target, but instances can be populated with custom data in an object initializer to provide additional constraints.
    /// </summary>
    [Serializable]
    public class AnalysisParams
    {
        /// <summary>
        /// Issue Categories to include in the audit. If null, the analysis will include all categories.
        /// </summary>
        [SerializeField]
        public SerializableEnum<IssueCategory>[] Categories;

        [SerializeField]
        SerializableEnum<BuildTarget> m_Platform;

        string m_PlatformAsString;

        /// <summary>
        /// Analysis platform. The default platform is the currently active build target.
        /// </summary>
        public BuildTarget Platform
        {
            get => m_Platform;
            set
            {
                m_Platform = value;
                m_PlatformAsString = m_Platform.ToString();
                DiagnosticParams?.SetAnalysisPlatform(Platform);
            }
        }

        /// <summary>
        /// Assemblies to analyze. If null, all compiled assemblies will be analyzed.
        /// </summary>
        public string[] AssemblyNames;

        /// <summary>
        /// Code optimization mode. The default is <see cref="CodeOptimization.Release"/>.
        /// </summary>
        public SerializableEnum<CodeOptimization> CodeOptimization;

        /// <summary>
        /// The Compilation mode to use during code analysis. The default is <see cref="CompilationMode.Player"/>.
        /// </summary>
        public SerializableEnum<CompilationMode> CompilationMode;

        /// <summary>
        /// Reports a batch of new issues. Note that this be called multiple times per analysis.
        /// </summary>
        public Action<IEnumerable<ReportItem>> OnIncomingIssues;

        /// <summary>
        /// Notifies that all Modules completed their analysis.
        /// </summary>
        public Action<Report> OnCompleted;

        /// <summary>
        /// Notifies that a Module completed its analysis.
        /// </summary>
        public Action<AnalysisResult> OnModuleCompleted;

        /// <summary>
        /// The DiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        /// By default, this makes a copy of <see cref="ProjectAuditorSettings"/>.
        /// </summary>
        public DiagnosticParams DiagnosticParams;

        /// <summary>
        /// This delegate is called at the start of an analysis to determine whether a given BuildTargetGroup and BuildTarget are supported by this Editor version.
        /// </summary>
        [NonSerialized]
        internal IsASupportedBuildTarget SupportedBuildTarget;

        // AnalysisParams copy of the global rules. Can be added to with WithAdditionalDiagnosticRules but doesn't need
        // to be exposed to the API.
        [SerializeField]
        internal SeverityRules Rules;

        [NonSerialized]
        internal Report ExistingReport;

        [NonSerialized]
        internal Predicate<string> AssetPathFilter;

        internal string PlatformAsString => m_PlatformAsString;

        /// <summary>
        /// AnalysisParams constructor.
        /// </summary>
        /// <param name="copyParamsFromGlobal">If true, the global ProjectSettings will register DiagnosticParams defaults, save any changes and copy the data into this object. This is usually the desired behaviour, but is not allowed during serialization. </param>
        public AnalysisParams(bool copyParamsFromGlobal = true)
        {
            if (copyParamsFromGlobal)
            {
                // Check for any new defaults (newly-installed package, new user modules, or an updated version of the package since last analysis)
                ProjectAuditorSettings.instance.DiagnosticParams.RegisterParameters();
                ProjectAuditorSettings.instance.Save();

                Rules = new SeverityRules(ProjectAuditorSettings.instance.Rules);
                DiagnosticParams = new DiagnosticParams(ProjectAuditorSettings.instance.DiagnosticParams);
            }

            Platform = BuildTarget.NoTarget;
            CodeOptimization = Editor.CodeOptimization.Release;
            CompilationMode = Editor.CompilationMode.Player;
            SupportedBuildTarget = BuildPipeline.IsBuildTargetSupported;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="original">The AnalysisParams object to copy from.</param>
        public AnalysisParams(AnalysisParams original)
        {
            Rules = original.Rules;
            DiagnosticParams = original.DiagnosticParams;

            Categories = original.Categories;
            Platform = original.Platform;
            AssemblyNames = original.AssemblyNames;
            CodeOptimization = original.CodeOptimization;
            CompilationMode = original.CompilationMode;

            OnIncomingIssues = original.OnIncomingIssues;
            OnCompleted = original.OnCompleted;
            OnModuleCompleted = original.OnModuleCompleted;

            ExistingReport = original.ExistingReport;

            AssetPathFilter = original.AssetPathFilter;
        }

        /// <summary>
        /// Adds a list of additional Rules which will be applied during analysis.
        /// </summary>
        /// <param name="rules">Additional Rules to impose.</param>
        /// <returns>This AnalysisParams object, after adding the additional Rules.</returns>
        public AnalysisParams WithAdditionalDiagnosticRules(List<Rule> rules)
        {
            foreach (var rule in rules)
            {
                Rules.AddRule(rule);
            }

            return this;
        }
    }
}
