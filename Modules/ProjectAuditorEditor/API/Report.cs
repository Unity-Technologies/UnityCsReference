// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Contains information about the session in which a <see cref="Report"/> was created.
    /// </summary>
    [Serializable]
    internal class SessionInfo : AnalysisParams
    {
        /// <summary>
        /// Default Constructor. For serialization purposes only.
        /// </summary>
        public SessionInfo() : base(false) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serializedParams">AnalysisParams object which was passed to ProjectAuditor to create the Report</param>
        public SessionInfo(AnalysisParams serializedParams)
            : base(serializedParams)
        { }

        /// <summary>
        /// The version number of the Project Auditor rules package which was used.
        /// </summary>
        public string ProjectAuditorRulesVersion;

        /// <summary>
        /// The version of Unity which was used.
        /// </summary>
        public string UnityVersion;

        /// <summary>
        /// The Company Name string in the project's Project Settings.
        /// </summary>
        public string CompanyName;

        /// <summary>
        /// The `Application.cloudProjectId` identifier for the project.
        /// </summary>
        public string ProjectId;

        /// <summary>
        /// The Product Name string in the project's Project Settings.
        /// </summary>
        public string ProjectName;

        /// <summary>
        /// The Product Version string in the project's Project Settings.
        /// </summary>
        public string ProjectRevision;

        /// <summary>
        /// The date and time at which the Report was created.
        /// </summary>
        public string DateTime;

        /// <summary>
        /// The `SystemInfo.deviceName` identifier for the device on which the Unity Editor was running.
        /// </summary>
        public string HostName;

        /// <summary>
        /// The `SystemInfo.operatingSystem` identifier for the operating system on which the Unity Editor was running.
        /// </summary>
        public string HostPlatform;

        /// <summary>
        /// True if the "Use Roslyn Analyzers" checkbox was ticked in Preferences > Project Auditor.
        /// </summary>
        public bool UseRoslynAnalyzers;

        /// <summary>
        /// The analyzed areas from the preferences.
        /// </summary>
        public SerializableEnum<ProjectAreaFlags> ProjectAreas;
    }

    /// <summary>
    /// Report contains a list of all issues found by ProjectAuditor.
    /// </summary>
    [Serializable]
    public sealed class Report
    {
        internal const string k_CurrentVersion = "1.2";
        internal const string k_SaveFileHeader = "PROJECT_AUDITOR_REPORT";

        [SerializeField]
        string version = k_CurrentVersion;

        /// <summary>
        /// File format version of the Report (read-only).
        /// </summary>
        public string ReportVersion
        {
            get => version;
            internal set => version = value;
        }

        // stephenm TODO: ModuleInfo serializes to JSON but isn't accessible in any meaningful way if a script just has a Report object it wants to query. Figure out some API for this?
        // Keeping this internal for now. Exposing this means exposing IssueLayout, which means exposing PropertyDefinition, which to be useful means exposing every enum that can
        // be passed to PropertyTypeUtil.FromCustom() (basically one per view). I'd love to find a more elegant way to do this.
        [Serializable]
        internal class ModuleInfo
        {
            // stephenm TODO: Comment (for all these fields... Assuming we do what the above comment says and expose this via an API of some sort)
            public string name;

            // this is used by HasCategory
            public SerializableEnum<IssueCategory>[] categories;
            [NonSerialized]  // See comment for PostSerializeLayoutUpdate
            public IssueLayout[] layouts;

            public long durationMs;

            public AnalysisResult result;
        }

        /// <summary>
        /// Contains information about the session in which this Report was created.
        /// </summary>
        [SerializeField]
        private SessionInfo sessionInfo;
        internal SessionInfo SessionInfo { get => sessionInfo; set => sessionInfo = value; }

        /// <summary>
        /// A name to display along with the Report, configurable by the user.
        /// </summary>
        public string DisplayName;

        [SerializeField]
        private bool needsSaving;
        internal bool NeedsSaving { get => needsSaving; set => needsSaving = value; }

        [SerializeField]
        List<ModuleInfo> moduleMetadata = new List<ModuleInfo>();

        [SerializeField]
        DescriptorLibrary m_DescriptorLibrary = new DescriptorLibrary();

        [SerializeField]
        List<ReportItem> m_Issues = new List<ReportItem>();

        static Mutex s_Mutex = new Mutex();

        internal ReportItem[] Insights
        {
            get
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_Issues.Where(i => !i.IsIssue()).ToArray();
#pragma warning restore UA2001
            }
        }

        internal ReportItem[] UnfixedIssues
        {
            get
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_Issues.Where(i => i.IsIssue() && !i.WasFixed).ToArray();
#pragma warning restore UA2001
            }
        }

        internal List<Descriptor> Descriptors => m_DescriptorLibrary.m_SerializedDescriptors;

        /// <summary>
        /// The total number of ProjectIssues included in this report.
        /// </summary>
        public int NumTotalIssues => m_Issues.Count;

        // for serialization purposes only
        internal Report()
        {}

        // for internal use only
        internal Report(AnalysisParams analysisParams)
        {
            SessionInfo = new SessionInfo(analysisParams)
            {
                ProjectAuditorRulesVersion = ProjectAuditorRulesPackage.Version,
                ProjectId = PlayerSettings.productGUID.ToString(),
                ProjectName = Application.productName,
                ProjectRevision = Application.version,
                CompanyName = Application.companyName,
                UnityVersion = Application.unityVersion,

                DateTime = Utils.Json.SerializeDateTime(DateTime.Now),
                // Windows' SystemInfo.deviceName implementation is currently the NetBIOS name (limited to 15 characters), so use this instead.
                HostName = System.Net.Dns.GetHostName(),
                // It's not 2016 any more, but too many systems depend on operatingSystem thinking it is, so update mac's naming here
                HostPlatform = SystemInfo.operatingSystem.Replace("Mac OS X", "macOS"),

                UseRoslynAnalyzers = UserPreferences.UseRoslynAnalyzers,
                ProjectAreas = (ProjectAreaFlags)UserPreferences.ProjectAreasToAnalyze
            };
        }

        /// <summary>
        /// Checks whether the Report includes analysis for a given IssueCategory.
        /// </summary>
        /// <param name="category">The IssuesCategory to check</param>
        /// <returns>True if ProjectAuditor ran one or more Modules that reports issues of the specified IssueCategory. Otherwise, returns false.</returns>
        public bool HasCategory(IssueCategory category)
        {
            if (category == IssueCategory.Metadata)
                return true;

            foreach (ModuleInfo moduleInfo in moduleMetadata)
                foreach (IssueCategory moduleCategory in moduleInfo.categories)
                    if (moduleCategory == category)
                        return true;

            return false;
        }

        /// <summary>
        /// Gets a read-only collection of all of the ProjectIssues included in the report.
        /// </summary>
        /// <returns>All the issues in the report</returns>
        public IReadOnlyCollection<ReportItem> GetAllIssues()
        {
            s_Mutex.WaitOne();
            var result = m_Issues.ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Get total number of issues for a specific IssueCategory.
        /// </summary>
        /// <param name="category"> Desired IssueCategory</param>
        /// <returns> Number of project issues</returns>
        public int GetNumIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var result = m_Issues.Count(i => i.Category == category);
#pragma warning restore UA2001
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// After we load from json the layouts in the ModuleInfo are invalid because they aren't serialized out.  This
        /// function runs through them and re-initializes them as they were on an initial scan.
        /// </summary>
        /// <param name="modules">An array of all the Module classes that are used to get the SupportedLayouts.</param>
        internal void PostSerializeLayoutUpdate(Module[] modules)
        {
            foreach (ModuleInfo moduleInfo in moduleMetadata)
            {
                if (moduleInfo.layouts == null)
                {
                    foreach (Module module in modules)
                    {
                        if (module.Name == moduleInfo.name || (moduleInfo.name == "AudioClips" && module.Name == "Audio Clips"))
                        {
                            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            moduleInfo.layouts = module.SupportedLayouts.ToArray();
#pragma warning restore UA2001
                        }
                    }
                }
            }
        }

        internal IssueLayout GetLayout(IssueCategory category)
        {
            foreach (ModuleInfo m in moduleMetadata)
            {
                if (m.layouts == null)
                    continue;

                foreach (IssueLayout l in m.layouts)
                {
                    if (l.Category == category)
                        return l;
                }
            }

            return null;
        }

        /// <summary>
        /// find all issues for a specific IssueCategory.
        /// </summary>
        /// <param name="category"> Desired IssueCategory</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ReportItem> FindByCategory(IssueCategory category)
        {
            s_Mutex.WaitOne();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var result = m_Issues.Where(i => i.Category == category).ToArray();
#pragma warning restore UA2001
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Find all Issues that match a specific ID.
        /// </summary>
        /// <param name="id"> Desired Descriptor ID</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ReportItem> FindByDescriptorId(string id)
        {
            s_Mutex.WaitOne();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var result = m_Issues.Where(i => i.Id.IsValid() && i.Id.Equals(id)).ToArray();
#pragma warning restore UA2001
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Clears all issues that match the specified IssueCategory from the report.
        /// </summary>
        /// <param name="Category">The IssueCategory of the issues to remove.</param>
        internal void ClearIssues(IssueCategory Category)
        {
            s_Mutex.WaitOne();
            m_Issues.RemoveAll(issue => issue.Category == Category);
            foreach (var info in moduleMetadata)
            {
                var categories = new List<SerializableEnum<IssueCategory>>(info.categories);
                categories.RemoveAll(c => c == Category);
                info.categories = categories.ToArray();
            }
            moduleMetadata.RemoveAll(info => info.categories.Length == 0);
            s_Mutex.ReleaseMutex();
        }

        /// <summary>
        /// Check whether all issues in the report are valid.
        /// </summary>
        /// <returns>True is none of the issues in the report have a null description string. Otherwise returns false.</returns>
        public bool IsValid()
        {
            if (moduleMetadata.Count == 0)
                return false;

            foreach (IssueCategory ic in Enum.GetValues(typeof(IssueCategory)))
            {
                if (ic == IssueCategory.Metadata || ic == IssueCategory.SpriteAtlas)
                    continue;

                if (ic >= IssueCategory.FirstCustomCategory)
                    break;

                if (SessionInfo != null && SessionInfo.Categories != null && Array.IndexOf(SessionInfo.Categories, ic) == -1)
                    continue;

                IssueLayout il = GetLayout(ic);
                if (il == null)
                    return false;
            }

            return m_Issues.TrueForAll(i => i.IsValid()) && moduleMetadata.TrueForAll(m => m.result != AnalysisResult.Cancelled);
        }

        /// <summary>
        /// Is this Report for the currently loaded Project?
        /// </summary>
        /// <returns>True if this report is for the currently loaded Unity Project.</returns>
        internal bool IsForCurrentProject()
        {
            return (SessionInfo.ProjectId == PlayerSettings.productGUID.ToString());
        }

        /// <summary>
        /// Save the Report as a JSON file.
        /// </summary>
        /// <param name="path">The file path at which to save the file</param>
        public void Save(string path)
        {
            StringWriter sw = new StringWriter();
            sw.WriteLine(k_SaveFileHeader);
            sw.WriteLine(k_CurrentVersion);

            string contents = JsonUtility.ToJson(this, UserPreferences.PrettifyJsonOutput);
            File.WriteAllText(path, sw.ToString() + contents);
        }

        /// <summary>
        /// Load a Report from a JSON file at the specified path.
        /// </summary>
        /// <param name="path">File path of the report to load</param>
        /// <param name="errorMessage">Error message if load fails</param>
        /// <returns>A loaded Report object</returns>
        public static Report Load(string path, out string errorMessage)
        {
            string whole_file = File.ReadAllText(path);

            StringReader sr = new StringReader(whole_file);

            string header = sr.ReadLine();

            if (!header.Equals(k_SaveFileHeader))
            {
                errorMessage = "Invalid file format";
                return null;
            }

            string version = sr.ReadLine();
            if (!version.Equals(k_CurrentVersion))
            {
                errorMessage = $"Report file version is {version}, but this version of Project Auditor requires file version {k_CurrentVersion}";
                return null;
            }

            errorMessage = "";
            return JsonUtility.FromJson<Report>(sr.ReadToEnd());
        }

        // Internal only: Data written by ProjectAuditor during analysis
        internal void RecordModuleInfo(Module module, long moduleAnalysisTimeMs, AnalysisResult analysisResult)
        {
            var name = module.Name;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var info = moduleMetadata.FirstOrDefault(m => m.name.Equals(name));
#pragma warning restore UA2001
            if (info == null)
            {
                info = new ModuleInfo
                {
                    name = module.Name,
                    categories = module.Categories.ToSerializableArray(),
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    layouts = module.SupportedLayouts.ToArray(),
#pragma warning restore UA2001
                };
                moduleMetadata.Add(info);
            }

            info.durationMs = moduleAnalysisTimeMs;
            info.result = analysisResult;
        }

        internal long CalculateIssueCategoryAnalysisDuration(IssueCategory category)
        {
            long result = 0;

            foreach (ModuleInfo moduleInfo in moduleMetadata)
            {
                if (Array.Exists(moduleInfo.categories, c => c == category))
                    result += moduleInfo.durationMs;
            }

            return result;
        }

        // Internal only: Data written by ProjectAuditor during analysis
        internal void AddIssues(IEnumerable<ReportItem> issues)
        {
            s_Mutex.WaitOne();
            m_Issues.AddRange(issues);
            s_Mutex.ReleaseMutex();
        }
    }
}
