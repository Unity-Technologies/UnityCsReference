// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.BuildService;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Scripting.ScriptCompilation.MsBuild;
using UnityEngine;
using PropertyDefinition = Unity.ProjectAuditor.Editor.Core.PropertyDefinition;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AssemblyProperty
    {
        ReadOnly = 0,
        CodeLocation,
        Num
    }

    enum PrecompiledAssemblyProperty
    {
        RoslynAnalyzer = 0,
        TargetFramework,
        Num
    }

    internal enum CodeProperty
    {
        Assembly = 0,
        CodeLocation,
        PerformanceCritical,
        Num
    }

    enum CompilerMessageProperty
    {
        Code = 0,
        Assembly,
        CodeLocation,
        Num
    }

    internal enum ObsoleteApiProperty
    {
        Recommendation,
        AutoUpgradable,
        WarningSince,
        ErrorSince,
        RemovedIn,
        ObsoleteSince,
        Num
    };

    class CodeModule : ModuleWithAnalyzers<CodeModuleAnalyzer>
    {
        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.Assembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name", MaxAutoWidth = 800},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), Format = PropertyFormat.Bool, Name = "Read Only", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Path, Name = "Asmdef Path"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.CodeLocation), Format = PropertyFormat.String, Name = "Location", LongName = "Code Location" },
            }
        };

        static readonly IssueLayout k_PrecompiledAssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.PrecompiledAssembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name"},
                new PropertyDefinition { Type = PropertyType.Directory, Name = "Path", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PrecompiledAssemblyProperty.RoslynAnalyzer), Format = PropertyFormat.Bool, Name = "Roslyn Analyzer"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PrecompiledAssemblyProperty.TargetFramework), Format = PropertyFormat.String, Name = "Target Framework"},
            }
        };

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = IssueCategory.Code,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Name = "Areas", LongName = "The areas the issue might have an impact on"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CodeProperty.CodeLocation), Format = PropertyFormat.String, Name = "Location", LongName = "Code Location" },
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CodeProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        static readonly IssueLayout k_CompilerMessageLayout = new IssueLayout
        {
            Category = IssueCategory.CodeCompilerMessage,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), Format = PropertyFormat.String, Name = "Code", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Message", LongName = "Compiler Message"},
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.CodeLocation), Format = PropertyFormat.String, Name = "Location", LongName = "Code Location" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Full Path"},
            }
        };

        static readonly IssueLayout k_ObsoleteApiLayout = new IssueLayout
        {
            Category = IssueCategory.ObsoleteAPI,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ObsoleteApiProperty.AutoUpgradable), Format = PropertyFormat.Bool, Name = "Upgradable", LongName = "Automatically upgradable" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ObsoleteApiProperty.ObsoleteSince), Format = PropertyFormat.String, Name = "Obsolete", LongName = "Obsolete since version", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(ObsoleteApiProperty.RemovedIn), Format = PropertyFormat.String, Name = "Removed", LongName = "Removed in version" }
            ]
        };

        List<OpCode> m_OpCodes;
        List<int>[] m_OpCodeAnalyzers = new List<int>[ushort.MaxValue];
        List<CodeModuleInstructionAnalyzer> m_CodeAnalyzers;
        List<CodeModulePrecompiledAssemblyAnalyzer> m_PrecompiledAssemblyAnalyzers;
        List<CodeModuleCompilerMessageAnalyzer> m_CompilerMessageAnalyzers;

        Thread m_AssemblyAnalysisThread;

        public override string Name => "Code";

        // Match a whole "word", starting with UDR and ending with exactly 4 digits, e.g. UDR1234
        static readonly Regex s_RegEx = new Regex(@"\bUAL\d{4}\b");

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_IssueLayout,
            k_AssemblyLayout,
            k_PrecompiledAssemblyLayout,
            k_CompilerMessageLayout,
            k_ObsoleteApiLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            RoslynAnalyzerAssetWatcher.Initialize();
            RoslynDiagnosticsLibrary.EnsureLoaded();

#pragma warning disable UAC2001 // Avoid Linq
            m_OpCodes = new List<OpCode>(GetAnalyzers().OfType<CodeModuleInstructionAnalyzer>().Select(a => a.opCodes).SelectMany(c => c).Distinct());
#pragma warning restore UAC2001
        }

        // Compiles all scripts using an MSBuild analysis configuration ("Analysis" for the editor target, or
        // "<Target>+Analysis" for a player target) and returns a report of the compiler messages produced.
        static Task<MsBuildCompilation.CompilationMessages> RequestCompilationAsync(string analysisConfiguration, CancellationToken cancellationToken)
        {
            if (!MsBuildCompilationInterface.IsEnabled())
                throw new InvalidOperationException($"{nameof(RequestCompilationAsync)} is only supported when the MSBuild compilation pipeline is enabled.");

            static bool IsAnalysisConfiguration(string configuration) =>
                configuration is "Analysis" || (configuration is not null && configuration.EndsWith("+Analysis", StringComparison.Ordinal));

            if (!IsAnalysisConfiguration(analysisConfiguration))
            {
                throw new ArgumentException(
                    $"'{analysisConfiguration}' is not an analysis configuration. Expected \"Analysis\" or \"<Target>+Analysis\"; " +
                    "use RequestScriptCompilation() for a normal Editor compilation.", nameof(analysisConfiguration));
            }

            return MsBuildCompilationInterface.RequestAnalysisCompilationAsync(analysisConfiguration, cancellationToken);
        }

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            if (m_Ids == null)
                throw new Exception("Descriptors Database not initialized.");

            // Kick off async Roslyn analyzer diagnostic loading
            // (overlaps the precompiled assembly analysis; blocks at RoslynDiagnosticsLibrary.IsLoaded()).
            RoslynDiagnosticsLibrary.EnsureLoaded();

            if (m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            var compatibleAnalyzers = GetCompatibleAnalyzers(analysisParams);
            m_CodeAnalyzers = new List<CodeModuleInstructionAnalyzer>();
            m_PrecompiledAssemblyAnalyzers = new List<CodeModulePrecompiledAssemblyAnalyzer>();
            m_CompilerMessageAnalyzers = new List<CodeModuleCompilerMessageAnalyzer>();
            foreach (var analyzer in compatibleAnalyzers)
            {
                if (analyzer is CodeModuleInstructionAnalyzer codeAnalyzer)
                    m_CodeAnalyzers.Add(codeAnalyzer);
                else if (analyzer is CodeModulePrecompiledAssemblyAnalyzer precompiledAssemblyAnalyzer)
                    m_PrecompiledAssemblyAnalyzers.Add(precompiledAssemblyAnalyzer);
                else if (analyzer is CodeModuleCompilerMessageAnalyzer compilerMessageAnalyzer)
                    m_CompilerMessageAnalyzers.Add(compilerMessageAnalyzer);
            }

            for (var i = 0; i < m_OpCodeAnalyzers.Length; i++)
                m_OpCodeAnalyzers[i] = null;
            foreach (var opCode in m_OpCodes)
            {
                var opCodeAnalyzers = new List<int>();
                for (int analyzerIndex = 0; analyzerIndex < m_CodeAnalyzers.Count; analyzerIndex++)
                {
                    if (m_CodeAnalyzers[analyzerIndex].opCodes.Contains(opCode))
                        opCodeAnalyzers.Add(analyzerIndex);
                }
                m_OpCodeAnalyzers[(ushort)opCode.Value] = opCodeAnalyzers;
            }

            var precompiledAssemblyPaths = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All);
            var precompiledAssemblyIssues = new List<ReportItem>(precompiledAssemblyPaths.Count);
            var onPrecompiledAssemblyIssueFoundInternal = new Action<ReportItem>(precompiledAssemblyIssues.Add);

            AsyncProgressState progressState = progress?.Start("Analyzing Precompiled Assemblies", precompiledAssemblyPaths.Count);

            long threadExecutionTimeMs = 0;

            // Analyze precompiled assemblies
            m_AssemblyAnalysisThread = new Thread(() =>
            {
                var startTime = DateTime.UtcNow;
                AnalyzePrecompiledAssemblies(analysisParams, precompiledAssemblyPaths, onPrecompiledAssemblyIssueFoundInternal, progress, progressState);
                threadExecutionTimeMs += (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            });
            m_AssemblyAnalysisThread.Name = "Precompiled Assembly Analysis";
            m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
            m_AssemblyAnalysisThread.Start();

            // wait for thread
            while (m_AssemblyAnalysisThread.IsAlive)
                yield return new WaitForEndOfFrame();

            progress?.Clear(progressState);

            if (precompiledAssemblyIssues.Count > 0)
                analysisParams.OnIncomingIssues(precompiledAssemblyIssues);

            yield return null;

            // Ensure the roslyn info is loaded before we start analysis.
            while (!RoslynDiagnosticsLibrary.PollLoading())
                yield return new WaitForEndOfFrame();

            // find all roslyn analyzer DLLs by label
#pragma warning disable UAC2001 // Avoid Linq
            var roslynAnalyzerAssets = new List<string>(AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath));
#pragma warning restore UAC2001

            // report all roslyn analyzers as PrecompiledAssembly issues
#pragma warning disable UAC2001 // Avoid Linq
            var roslynAnalyzerIssues = roslynAnalyzerAssets
#pragma warning restore UAC2001
                .Distinct()
                .Select(roslynAnalyzerDllPath => (ReportItem)context.CreateInsight(
                IssueCategory.PrecompiledAssembly,
                Path.GetFileNameWithoutExtension(roslynAnalyzerDllPath))
                .WithCustomProperties([true, string.Empty])
                .WithLocation(roslynAnalyzerDllPath));

            analysisParams.OnIncomingIssues(roslynAnalyzerIssues);

            yield return null;

            // The MSBuild pipeline builds the whole project in a single out-of-band "Analysis" configuration
            // with the Roslyn analyzers already wired up, and reports their diagnostics directly. None of the
            // per-assembly AssemblyBuilder compilation below applies, so finish the module here.
            if (MsBuildCompilationInterface.IsEnabled())
            {
                yield return AuditWithMsBuild(context, analysisParams, progress, threadExecutionTimeMs, roslynAnalyzerAssets);
                yield break;
            }

            var compilationPipeline = new AssemblyCompilation
            {
                OnAssemblyCompilationFinished = (compilationResult) =>
                {
                    analysisParams.OnIncomingIssues(ProcessCompilerMessages(context, compilationResult));
                },
                CodeOptimization = analysisParams.CodeOptimization,
                CodeAnalysisFlags = analysisParams.CodeAnalysisFlags,
                CodeOwnerFlags = analysisParams.CodeOwnerFlags,
                Platform = analysisParams.Platform,
                // TODO: reminder to add list of analyzers to metadata
                RoslynAnalyzers = roslynAnalyzerAssets.ToArray(),
                AssemblyNames = analysisParams.AssemblyNames
            };

            // Assembly compilation
            List<AssemblyInfo> compiledEditorAssemblyPaths = null;
            List<AssemblyInfo> compiledPlayerAssemblyPaths = null;
            yield return compilationPipeline.Compile(
                (editorPaths, playerPaths) => { compiledEditorAssemblyPaths = editorPaths; compiledPlayerAssemblyPaths = playerPaths; },
                progress);

            if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
            {
                var editorCompilerIssues = ProcessEditorCompilerMessages(context);
                analysisParams.OnIncomingIssues(editorCompilerIssues);
            }

            if (analysisParams.AssemblyNames != null)
            {
#pragma warning disable UAC2001 // Avoid Linq
                compiledEditorAssemblyPaths = new List<AssemblyInfo>(compiledEditorAssemblyPaths.Where(a => Array.IndexOf(analysisParams.AssemblyNames, a.Name) != -1));
                compiledPlayerAssemblyPaths = new List<AssemblyInfo>(compiledPlayerAssemblyPaths.Where(a => Array.IndexOf(analysisParams.AssemblyNames, a.Name) != -1));
#pragma warning restore UAC2001
            }

            if (compiledEditorAssemblyPaths.Count > 0)
            {
#pragma warning disable UAC2001 // Avoid Linq
                var issues = compiledEditorAssemblyPaths.Select(assemblyInfo => (ReportItem)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
#pragma warning restore UAC2001
                    .WithCustomProperties(
                    [
                        assemblyInfo.IsReadOnly,
                        assemblyInfo.GetTypeString()
                    ])
                    .WithLocation(assemblyInfo.AsmDefPath))
                    .ToArray();
                if (issues.Length > 0)
                    analysisParams.OnIncomingIssues(issues);
            }

            // Add these manually because they aren't actually compiled, even though they are part of the player (they are pre-compiled)
            if (compiledPlayerAssemblyPaths.Count > 0)
            {
#pragma warning disable UAC2001 // Avoid Linq
                var issues = compiledPlayerAssemblyPaths
                    .Where(assemblyInfo => assemblyInfo.IsUnityInternalAssembly)
                    .Select(assemblyInfo => (ReportItem)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                    .WithCustomProperties(
                    [
                        assemblyInfo.IsReadOnly,
                        assemblyInfo.GetTypeString()
                    ])
                    .WithLocation(assemblyInfo.AsmDefPath))
                    .ToArray();
#pragma warning restore UAC2001
                if (issues.Length > 0)
                    analysisParams.OnIncomingIssues(issues);
            }

            #pragma warning disable UAC2001 // Avoid Linq
            var assemblyInfos = compiledEditorAssemblyPaths.Concat(compiledPlayerAssemblyPaths)
                .Where(a => AssemblyPackageFilter(a, analysisParams)).ToArray();
            #pragma warning restore UAC2001

            if (progress?.IsCancelled ?? false)
            {
                analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Cancelled, 0);
                yield break;
            }
            
            AsyncProgressState assemblyProgressState = progress?.Start("Analyzing Assemblies", assemblyInfos.Length);
#pragma warning disable UAC2001 // Avoid Linq
            // Process successfully compiled assemblies
            var localAssemblyInfos = assemblyInfos.Where(info => !info.IsReadOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.IsReadOnly).ToArray();
#pragma warning restore UAC2001
            var foundIssues = new List<ReportItem>();
            var onIssueFoundInternal = new Action<ReportItem>(foundIssues.Add);

            var assemblyDirectories = new List<string>();
            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine | PrecompiledAssemblyTypes.SystemAssembly));
            if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            yield return null;

            // first phase: analyze assemblies generated from editable scripts
            // second phase: analyze all remaining assemblies
            m_AssemblyAnalysisThread = new Thread(() =>
            {
                // Run analysis on the background thread
                var startTime = DateTime.UtcNow;

                AnalyzeAssemblies(localAssemblyInfos, analysisParams, assemblyDirectories, onIssueFoundInternal, progress, assemblyProgressState);
                AnalyzeAssemblies(readOnlyAssemblyInfos, analysisParams, assemblyDirectories, onIssueFoundInternal, progress, assemblyProgressState);

                threadExecutionTimeMs += (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            });
            m_AssemblyAnalysisThread.Name = "Assembly Analysis";
            m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
            m_AssemblyAnalysisThread.Start();

            // wait for thread
            while (m_AssemblyAnalysisThread.IsAlive)
                yield return new WaitForEndOfFrame();

            // remove issues if platform does not match
            foundIssues.RemoveAll(i => i.Id.IsValid() &&
                !i.Id.GetDescriptor().IsSupported(analysisParams));

            compilationPipeline.Dispose();

            // workaround for empty 'relativePath' strings which are not all available when 'onIssueFoundInternal' is called
            if (foundIssues.Count > 0)
                analysisParams.OnIncomingIssues(foundIssues);

            progress?.Clear(assemblyProgressState);
            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, threadExecutionTimeMs);
        }

        // Runs the analysis compilation(s) through the MSBuild pipeline and reports the resulting diagnostics.
        // One build per requested target: "Analysis" for editor code, "<Platform>+Analysis" for player code.
        IEnumerator AuditWithMsBuild(AnalysisContext context, AnalysisParams analysisParams, IProgress progress, long threadExecutionTimeMs, IReadOnlyCollection<string> roslynAnalyzerAssets)
        {
            var configurations = new List<(string Configuration, bool EditorAssemblies)>(2);
            if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
                configurations.Add(("Analysis", true));
            if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Player) != 0)
                configurations.Add(($"{analysisParams.Platform}+Analysis", false));

            AsyncProgressState progressState = progress?.Start("Analyzing Code", configurations.Count);

            WriteGlobalConfig();
            WriteAnalyzerProps(roslynAnalyzerAssets);

            using (var cancellationSource = new CancellationTokenSource())
            {
                foreach (var (configuration, editorAssemblies) in configurations)
                {
                    if (AdvanceAsyncProgress(progress, progressState, configuration) == false)
                        break;

                    var compilationTask = RequestCompilationAsync(configuration, cancellationSource.Token);
                    while (!compilationTask.IsCompleted)
                    {
                        if ((progress?.IsCancelled ?? false) && !cancellationSource.IsCancellationRequested)
                            cancellationSource.Cancel();

                        yield return new WaitForEndOfFrame();
                    }

                    if (compilationTask.IsCanceled || (progress?.IsCancelled ?? false))
                    {
                        analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Cancelled, threadExecutionTimeMs);
                        yield break;
                    }

                    if (compilationTask.IsFaulted)
                    {
                        Debug.LogError($"Project Auditor: '{configuration}' compilation failed: {compilationTask.Exception?.GetBaseException().Message}");
                        analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Failure, threadExecutionTimeMs);
                        yield break;
                    }

                    var issues = new List<ReportItem>(compilationTask.Result.Assemblies.Length);
                    yield return AnalyzeCompilerMessages(context, compilationTask.Result, editorAssemblies, issues.Add);
                    if (issues.Count > 0)
                        analysisParams.OnIncomingIssues(issues);

                    yield return null;
                }
            }

            progress?.Clear(progressState);
            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, threadExecutionTimeMs);
        }

        static void WriteGlobalConfig()
        {
            var globalConfigPath = Path.Combine(ProjectAuditor.ProjectPath, "Library", "ScriptAnalysis.globalconfig");

            var globalConfig = new StringBuilder();
            globalConfig.Append(
                "is_global = true\n" +
                "build_property.UnityEnableAutoStaticsCleanupAnalysis = true\n" +
                // Report the statics-cleanup diagnostics as warnings rather than the analyzer's
                // default Error severity, so that referenced assemblies still compile and dependent
                // assemblies don't cascade into CS0006 (missing metadata) failures.
                "dotnet_diagnostic.UAL0010.severity = warning\n" +
                "dotnet_diagnostic.UAL0011.severity = warning\n" +
                "dotnet_diagnostic.UAL0012.severity = warning\n" +
                "dotnet_diagnostic.UAL0013.severity = warning\n" +
                "dotnet_diagnostic.UAL0014.severity = warning\n");

            // Enable all known code diagnostics, except those we have suppressed
            var diagnostics = RoslynDiagnosticsLibrary.Diagnostics;
            if (diagnostics != null)
            {
                var suppressedDiagnostics = UserPreferences.BuildSuppressedDiagnosticsSet();

                foreach (var diagnostic in diagnostics)
                {
                    if (!suppressedDiagnostics.Contains(diagnostic.Id))
                        globalConfig.Append($"dotnet_diagnostic.{diagnostic.Id}.severity = warning\n");
                }
            }

            File.WriteAllText(globalConfigPath, globalConfig.ToString());
        }

        // Feeds every analyzer found by label into the Analysis build.
        static void WriteAnalyzerProps(IReadOnlyCollection<string> analyzerAssetPaths)
        {
            var propsPath = Path.Combine(ProjectAuditor.ProjectPath, "Library", "ScriptAnalysis.props");

            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", OmitXmlDeclaration = true };
            using (var writer = XmlWriter.Create(propsPath, settings))
            {
                writer.WriteComment(" This file is auto-generated. Do not edit ");
                writer.WriteStartElement("Project");
                writer.WriteStartElement("ItemGroup");
                writer.WriteAttributeString("Condition", "'$(IsEntryPointProject)' != 'true'");

                foreach (var assetPath in analyzerAssetPaths)
                {
                    var analyzerPath = PathUtils.ReplaceSeparators(Path.GetFullPath(FileUtil.GetPhysicalPath(assetPath)));

                    WriteAnalyzerPropsItem(writer, "Analyzer", analyzerPath);

                    foreach (var additionalFile in FindAnalyzerAdditionalFiles(analyzerPath))
                        WriteAnalyzerPropsItem(writer, "AdditionalFiles", additionalFile);
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        // Removes before including, so an analyzer the converter already resolved isn't passed to csc twice.
        static void WriteAnalyzerPropsItem(XmlWriter writer, string itemType, string path)
        {
            var value = EscapeMsBuildItemValue(path);

            foreach (var attribute in new[] { "Remove", "Include" })
            {
                writer.WriteStartElement(itemType);
                writer.WriteAttributeString(attribute, value);
                writer.WriteEndElement();
            }
        }

        // MSBuild reads these characters as syntax inside an item value: '$'/'@'/'%' introduce property, item
        // and metadata expansion, ';' separates list entries, '*'/'?' glob, and the quotes delimit attributes
        // and conditions. Escaping each as %XX keeps a path containing one a single literal item; MSBuild
        // unescapes it when the item is created. '%' must be replaced first, as it introduces the escape.
        static string EscapeMsBuildItemValue(string value)
        {
            var escaped = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if ("%$@;?*'\"".IndexOf(character) >= 0)
                    escaped.Append('%').Append(((int)character).ToString("X2"));
                else
                    escaped.Append(character);
            }

            return escaped.ToString();
        }

        // Analyzer databases ship beside their analyzer as "<Name>.<AnalyzerAssemblyName>.additionalfile",
        // the convention AnalyzerResolver reads. Without them the database-driven rules report nothing.
        static string[] FindAnalyzerAdditionalFiles(string analyzerPath)
        {
            var directory = Path.GetDirectoryName(analyzerPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return Array.Empty<string>();

            var pattern = "*." + Path.GetFileNameWithoutExtension(analyzerPath) + ".additionalfile";
            return Array.ConvertAll(Directory.GetFiles(directory, pattern), PathUtils.ReplaceSeparators);
        }

        // Converts the compiler/analyzer diagnostics reported by an MSBuild analysis build into report items.
        IEnumerator AnalyzeCompilerMessages(AnalysisContext context, MsBuildCompilation.CompilationMessages compilationMessages, bool editorAssemblies, Action<ReportItem> onAddIssue)
        {
            int count = 0;
            var includeTests = (context.Params.CodeAnalysisFlags & CodeAnalysisFlags.Tests) != 0;

            // Assemblies
            var assemblyInfos = new Dictionary<string, AssemblyInfo>(compilationMessages.Assemblies.Length);
            foreach (var assembly in compilationMessages.Assemblies)
            {
                ProcessAssembly(context, assembly.Name, assembly.ReferencedAssemblyNames, editorAssemblies, assemblyInfos, onAddIssue, includeTests);
                if (count++ % 100 == 0)
                    yield return null;
            }

            // Compilation messages
            foreach (var compilationMessage in compilationMessages.Messages)
            {
                var logMessage = compilationMessage.Msg;
                var assemblyName = compilationMessage.AssemblyName ?? string.Empty;
                var messageType = logMessage.MessageType == LogMessageType.Error ? CompilerMessageType.Error : CompilerMessageType.Warning;

                if (count++ % 100 == 0)
                    yield return null;

                // Messages emitted by the build itself rather than by the compiler (e.g. a project failed to
                // load) have no source location, so there's nothing to resolve an assembly or a path against.
                if (string.IsNullOrEmpty(logMessage.File))
                {
                    onAddIssue(context.CreateInsight(IssueCategory.CodeCompilerMessage, logMessage.Message)
                        .WithCustomProperties([logMessage.Code ?? string.Empty, assemblyName, string.Empty])
                        .WithLogLevel(CompilerMessageTypeToLogLevel(messageType)));
                    continue;
                }

                // Package code is filtered by file rather than by assembly, because package types can be
                // forwarded into the default assemblies during compilation.
                if (!PathPackageFilter(logMessage.File, context.Params.CodeAnalysisFlags, context.Params.CodeOwnerFlags))
                    continue;

                // Get/register assembly info
                if (!assemblyInfos.TryGetValue(assemblyName, out var assemblyInfo))
                    assemblyInfo = ProcessAssembly(context, assemblyName, null, editorAssemblies, assemblyInfos, onAddIssue, includeTests);

                // Skip tests
                if (!includeTests && assemblyInfo.IsTestAssembly)
                    continue;

                var message = new AssemblyUtils.CompilerMessage
                {
                    Code = logMessage.Code ?? string.Empty,
                    Message = logMessage.Message,
                    File = logMessage.File,
                    Line = logMessage.LineNumber,
                    Type = messageType
                };

                var messageContext = new CompilerMessageAnalysisContext
                {
                    Message = message,
                    Params = context.Params
                };

                bool handled = false;
                var relativePath = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, message.File);

                foreach (var analyzer in m_CompilerMessageAnalyzers)
                {
                    foreach (var issue in analyzer.Analyze(messageContext))
                    {
                        handled = true;
                        onAddIssue.Invoke(issue
                            .WithLocation(relativePath, message.Line)
                            .WithCustomProperties([assemblyInfo.Name, assemblyInfo.GetTypeString(), false]));
                    }
                }

                // If no analyzer handled the message, add it to the list of code compiler messages
                if (!handled)
                {
                    onAddIssue.Invoke(context.CreateInsight(IssueCategory.CodeCompilerMessage, message.Message)
                        .WithLocation(relativePath, message.Line)
                        .WithCustomProperties([message.Code, assemblyInfo.Name, assemblyInfo.GetTypeString()])
                        .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type)));
                }
            }
        }

        static AssemblyInfo ProcessAssembly(AnalysisContext context, string assemblyName, List<string> referencedAssemblyNames, bool editorAssemblies, Dictionary<string, AssemblyInfo> assemblyInfos, Action<ReportItem> onAddIssue, bool includeTests)
        {
            var assemblyInfo = assemblyName.Length == 0
                ? new AssemblyInfo { Name = string.Empty, RelativePath = "Assets", IsEditorAssembly = editorAssemblies }
                : AssemblyInfoProvider.GetAssemblyInfoFromAssemblyName(assemblyName, editorAssemblies, reportErrors: false);

            assemblyInfos.Add(assemblyName, assemblyInfo);

            if (!includeTests && assemblyInfo.IsTestAssembly)
                return assemblyInfo;

            var severity = Severity.None;
            // TODO: MSBuild doesn't tell us this, but the old AssemblyBuilder path did. Can MSBuild tell us?
            //if (compilationResult.Status == CompilationStatus.MissingDependency)
            //    severity = Severity.Warning;
            //else if (!compilationMessages.Assembly[name].Success)
            //    severity = Severity.Error;

            onAddIssue(context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                .WithCustomProperties(
                [
                    assemblyInfo.IsReadOnly,
                    assemblyInfo.GetTypeString(),
                ])
                .WithDependencies(new AssemblyDependencyNode(assemblyInfo.Name, referencedAssemblyNames))
                .WithLocation(assemblyInfo.AsmDefPath)
                .WithSeverity(severity));

            return assemblyInfo;
        }

        bool AssemblyPackageFilter(AssemblyInfo assemblyInfo, AnalysisParams analysisParams)
        {
            if (!string.IsNullOrEmpty(assemblyInfo.PackageResolvedPath))
            {
                if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Packages) != 0)
                {
                    if ((analysisParams.CodeOwnerFlags & CodeOwnerFlags.Unity) == 0)
                    {
                        if (assemblyInfo.IsUnityOwned)
                            return false;
                    }
                    if ((analysisParams.CodeOwnerFlags & CodeOwnerFlags.User) == 0)
                    {
                        if (!assemblyInfo.IsUnityOwned)
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        // Code compilation can forward types to other assemblies, effectively meaning package code can get "baked" into the Assembly-CSharp dlls.
        // So we need this extra check to detect and filter those types
        internal static bool PathPackageFilter(string path, CodeAnalysisFlags codeAnalysisFlags, CodeOwnerFlags codeOwnerFlags)
        {
            if (PathUtils.ReplaceSeparators(path).Contains("Library/PackageCache/", StringComparison.OrdinalIgnoreCase))
            {
                if ((codeAnalysisFlags & CodeAnalysisFlags.Packages) != 0)
                {
                    bool isUnityOwned = path.Contains("com.unity.", StringComparison.Ordinal);
                    if ((codeOwnerFlags & CodeOwnerFlags.Unity) == 0)
                    {
                        if (isUnityOwned)
                            return false;
                    }
                    if ((codeOwnerFlags & CodeOwnerFlags.User) == 0)
                    {
                        if (!isUnityOwned)
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        void AnalyzePrecompiledAssemblies(AnalysisParams analysisParams, List<string> precompiledAssemblyPaths, Action<ReportItem> onIssueFound, IProgress progress, AsyncProgressState progressState)
        {
            foreach (var assemblyPath in precompiledAssemblyPaths)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (AdvanceAsyncProgress(progress, progressState, assemblyName) == false)
                    break;

                var targetFramework = ReadTargetFramework(assemblyPath);

                var context = new PrecompiledAssemblyAnalysisContext
                {
                    AssemblyPath = assemblyPath,
                    TargetFramework = targetFramework,
                    Params = analysisParams
                };

                onIssueFound.Invoke((ReportItem)context.CreateInsight(IssueCategory.PrecompiledAssembly, Path.GetFileNameWithoutExtension(assemblyPath))
                    .WithCustomProperties([false, targetFramework])
                    .WithLocation(assemblyPath));

                foreach (var analyzer in m_PrecompiledAssemblyAnalyzers)
                {
                    foreach (var issue in analyzer.Analyze(context))
                        onIssueFound.Invoke(issue.WithLocation(assemblyPath));
                }
            }
        }

        void AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, AnalysisParams analysisParams, IReadOnlyCollection<string> assemblyDirectories, Action<ReportItem> onIssueFound, IProgress progress, AsyncProgressState progressState)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                #pragma warning disable UAC2001 // Avoid Linq
                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.Path)).Distinct())
#pragma warning restore UAC2001
                    assemblyResolver.AddSearchDirectory(dir);

                // Analyze all assemblies
                foreach (var assemblyInfo in assemblyInfos)
                {
                    if (AdvanceAsyncProgress(progress, progressState, assemblyInfo.Name) == false)
                        break;

                    if (!File.Exists(assemblyInfo.Path))
                    {
                        Debug.LogError(assemblyInfo.Path + " not found.");
                        continue;
                    }

                    var onIssueFoundFiltered = (analysisParams.AssemblyNames == null) || (Array.IndexOf(analysisParams.AssemblyNames, assemblyInfo.Name) != -1) ? onIssueFound : null;
                    AnalyzeAssembly(assemblyInfo, analysisParams, assemblyResolver, onIssueFoundFiltered);
                }
            }
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, AnalysisParams analysisParams, IAssemblyResolver assemblyResolver, Action<ReportItem> onIssueFound)
        {
            try
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.Path,
                    new ReaderParameters { ReadSymbols = true, AssemblyResolver = assemblyResolver, MetadataResolver = new MetadataResolverWithCache(assemblyResolver) }))
                {
                    object[] assemblyUserData = new object[m_CodeAnalyzers.Count];
                    for (int analyzerIndex = 0; analyzerIndex < m_CodeAnalyzers.Count; analyzerIndex++)
                        assemblyUserData[analyzerIndex] = m_CodeAnalyzers[analyzerIndex].OnAnalyzeAssembly();

                    bool isDefaultAssembly = (assemblyInfo.Name == AssemblyInfo.DefaultAssemblyName || assemblyInfo.Name == AssemblyInfo.DefaultEditorAssemblyName);

                    foreach (var typeDefinition in CodeAnalysis.MonoCecilHelper.AggregateAllTypeDefinitions(assembly.MainModule.Types))
                    {
                        var isPerformanceCriticalType = IsPerformanceCriticalType(typeDefinition);
                        foreach (var methodDefinition in typeDefinition.Methods)
                        {
                            if (!methodDefinition.HasBody)
                                continue;

                            // workaround for long analysis times when Burst is installed
                            if (methodDefinition.DeclaringType.FullName.StartsWith("Unity.Burst.Editor.BurstDisassembler"))
                                continue;

                            if (!methodDefinition.DebugInformation.HasSequencePoints)
                                continue;

                            // skip generated code (we could add a CodeAnalysisFlag for these if we wanted to include them)
                            var path = methodDefinition.DebugInformation.SequencePoints[0].Document.Url;
                            if (path.IndexOf("Unity.SourceGenerator", StringComparison.OrdinalIgnoreCase) >= 0)
                                continue;

                            // Unity forwards some package types to the default assemblies during compilation. Filter those separately from AssemblyPackageFilter.
                            if (isDefaultAssembly)
                            {
                                if (!PathPackageFilter(path, analysisParams.CodeAnalysisFlags, analysisParams.CodeOwnerFlags))
                                    continue;
                            }

                            var isPerformanceCriticalContext = isPerformanceCriticalType && IsPerformanceCriticalMethod(methodDefinition);

                            AnalyzeMethodBody(assemblyInfo, methodDefinition, assemblyUserData, isPerformanceCriticalContext, analysisParams.DependencyCrawler, onIssueFound);
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                // Failed to find the PDB file, log it and move on
                if (!assemblyInfo.IsUnityInternalAssembly)
                    Debug.LogWarning(ex.Message);
            }
        }

        void AnalyzeMethodBody(AssemblyInfo assemblyInfo, MethodDefinition caller, object[] assemblyUserData, bool perfCriticalContext, DependencyCrawler callCrawler, Action<ReportItem> onIssueFound)
        {
            var callerNode = new CallTreeNode(caller)
            {
                PerfCriticalContext = perfCriticalContext
            };

            var sequencePoints = caller.DebugInformation.SequencePoints;

            for (int analyzerIndex = 0; analyzerIndex < m_CodeAnalyzers.Count; analyzerIndex++)
            {
                var methodContext = new MethodAnalysisContext
                {
                    MethodDefinition = caller,
                    AssemblyUserData = assemblyUserData[analyzerIndex]
                };

                var reportItemBuilder = m_CodeAnalyzers[analyzerIndex].OnAnalyzeMethodBody(methodContext);
                if (reportItemBuilder != null)
                {
                    var s = sequencePoints[0];

                    reportItemBuilder.WithDependencies(callerNode);
                    reportItemBuilder.WithLocation(new Location(() => AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, s.Document.Url), s.IsHidden ? 0 : s.StartLine));
                    reportItemBuilder.WithCustomProperties([assemblyInfo.Name, assemblyInfo.GetTypeString(), perfCriticalContext]);

                    onIssueFound(reportItemBuilder);
                }
            }

            var lastSequencePointIndex = 0;
            var instructions = caller.Body.Instructions;
            for (var i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                var analyzers = m_OpCodeAnalyzers[(ushort)inst.OpCode.Value];

                if (inst.OpCode != OpCodes.Call && inst.OpCode != OpCodes.Callvirt)
                {
                    // if issues wont be reported and the call crawler doesnt care about this instruction, immediately skip to the next one
                    if (onIssueFound == null)
                        continue;

                    // early out if we have no analyzers and the call crawler doesnt care
                    if (analyzers == null)
                        continue;
                }

                // instructions and sequence points are in offset order
                // any sequence points earlier than the last one used can be skipped
                SequencePoint s = null;
                for (var j = lastSequencePointIndex; j < sequencePoints.Count; j++)
                {
                    var potentialPoint = sequencePoints[j];
                    if (inst.Offset < potentialPoint.Offset)
                    {
                        break;
                    }
                    s = potentialPoint;
                    lastSequencePointIndex = j;
                }

                Location location = null;
                if (s != null)
                {
                    location = new Location(() => AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, s.Document.Url), s.IsHidden ? 0 : s.StartLine);
                    callerNode.Location = location;
                }
                else
                {
                    // sequence point not found. Assuming caller.IsHideBySig == true
                }

                if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                {
                    callCrawler.AddToCodeCache(
                        (MethodReference)inst.Operand,
                        caller,
                        location,
                        perfCriticalContext
                    );
                }

                // skip analyzers if we are not interested in reporting issues, or have no analyzers
                if (onIssueFound == null || analyzers == null)
                    continue;

                var context = new InstructionAnalysisContext
                {
                    Instruction = inst,
                    MethodDefinition = caller,
                    AssemblyInfo = assemblyInfo
                };

                foreach (var analyzer in analyzers)
                {
                    context.AssemblyUserData = assemblyUserData[analyzer];
                    foreach (var reportItemBuilder in m_CodeAnalyzers[analyzer].Analyze(context))
                    {
                        onIssueFound(reportItemBuilder
                            .WithDependencies(callerNode)
                            .WithLocation(location)
                            .WithCustomProperties([assemblyInfo.Name, assemblyInfo.GetTypeString(), perfCriticalContext]));
                    }
                }
            }
        }

        IEnumerable<ReportItem> ProcessCompilerMessages(AnalysisContext context, AssemblyCompilationResult compilationResult)
        {
            var compilerMessages = compilationResult.Messages;
            var severity = Severity.None;
            if (compilationResult.Status == CompilationStatus.MissingDependency)
                severity = Severity.Warning;
            else if (Array.Exists(compilerMessages, m => m.Type == CompilerMessageType.Error))
                severity = Severity.Error;

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compilationResult.AssemblyPath, compilationResult.EditorAssembly);
            yield return context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                .WithCustomProperties(
                [
                    assemblyInfo.IsReadOnly,
                    assemblyInfo.GetTypeString(),
                ])
                .WithDependencies(new AssemblyDependencyNode(assemblyInfo.Name, compilationResult.DependentAssemblyNames))
                .WithLocation(assemblyInfo.AsmDefPath)
                .WithSeverity(severity);

            foreach (var message in compilerMessages)
                yield return ProcessEditorCompilerMessage(context, assemblyInfo, message);
        }

        IEnumerable<ReportItem> ProcessEditorCompilerMessages(AnalysisContext context)
        {
            int logCount = LogEntries.GetCount();
            var compilerEntries = new List<UnityEditor.Compilation.CompilerMessage>();
            LogEntries.StartGettingEntries();

            for (var i = 0; i < logCount; i++)
            {
                var entry = new LogEntry();
                LogEntries.GetEntryInternal(i, entry);
                var mode = (LogMessageFlags)entry.mode;
                if ((mode & (LogMessageFlags.kScriptCompileError | LogMessageFlags.kScriptCompileWarning)) == 0)
                    continue;

                if (!PathPackageFilter(entry.file, context.Params.CodeAnalysisFlags, context.Params.CodeOwnerFlags))
                    continue;

                compilerEntries.Add(new UnityEditor.Compilation.CompilerMessage()
                {
                    message = entry.message.TrimEnd('\r', '\n'),
                    file = entry.file,
                    line = entry.line,
                    column = entry.column,
                    type = (mode & LogMessageFlags.kScriptCompileError) != 0 ? CompilerMessageType.Error : CompilerMessageType.Warning
                });
            }

            LogEntries.EndGettingEntries();

            var projectFolder = Path.Combine(Application.dataPath, "../");

            foreach (var unityMessage in compilerEntries)
            {
                AssemblyInfo assemblyInfo;
                var assemblyName = UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(unityMessage.file);

                // Skip compiler messages for files not associated with any project assembly
                // (e.g., warnings from package cache files)
                if (string.IsNullOrEmpty(assemblyName))
                    continue;

                if (assemblyName == AssemblyInfo.DefaultAssemblyFileName || assemblyName == AssemblyInfo.DefaultEditorAssemblyFileName)
                {
                    var assemblyPath = Path.GetFullPath(Path.Combine("Library/ScriptAssemblies", assemblyName), projectFolder);
                    assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromUnityAssemblyPath(assemblyPath, assemblyName == AssemblyInfo.DefaultEditorAssemblyFileName);
                }
                else
                {
                    var assemblyPath = Path.GetFullPath(assemblyName, projectFolder);
                    assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assemblyPath, null);
                }

                var message = AssemblyCompilationTask.UnityCompilerMessageToProjectAuditorCompilerMessage(unityMessage);
                yield return ProcessEditorCompilerMessage(context, assemblyInfo, message);
            }
        }

        ReportItem ProcessEditorCompilerMessage(AnalysisContext context, AssemblyInfo assemblyInfo, AssemblyUtils.CompilerMessage message)
        {
            var relativePath = AssemblyInfoProvider.ResolveAssetPath(assemblyInfo, message.File);

            // stephenm TODO - A more data-driven way to specify which view Roslyn messages should be sent to, depending on their code.
            if (s_RegEx.IsMatch(message.Code))
            {
                var description = message.Message;
                string title = "Lifecycle API issue"; // Temporary until we move to MSBuild path.

                var descriptor = new Descriptor(
                    message.Code,
                    title,
                    Areas.IterationTime | Areas.MigrationToCoreCLR,
                    description,
                    string.Empty);

                DescriptorLibrary.RegisterDescriptor(descriptor.Id, descriptor);

                return context.CreateIssue(IssueCategory.Code, descriptor.Id)
                    .WithLocation(relativePath, message.Line)
                    .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type))
                    .WithCustomProperties(
                    [
                        assemblyInfo.Name,
                        assemblyInfo.GetTypeString(),
                        false
                    ]);
            }
            else
            {
                return context.CreateInsight(IssueCategory.CodeCompilerMessage, message.Message)
                    .WithCustomProperties(
                    [
                        message.Code,
                        assemblyInfo.Name,
                        assemblyInfo.GetTypeString()
                    ])
                    .WithLocation(relativePath, message.Line)
                    .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type));
            }
        }

        // Reads the assembly-level TargetFrameworkAttribute (for example ".NETStandard,Version=v2.1") from a
        // precompiled managed assembly. Returns an empty string when the attribute is absent or the file
        // cannot be read as a managed assembly.
        static string ReadTargetFramework(string assemblyPath)
        {
            try
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
                {
                    foreach (var attribute in assembly.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute"
                            && attribute.ConstructorArguments.Count > 0)
                        {
                            return attribute.ConstructorArguments[0].Value as string ?? string.Empty;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable or non-managed assembly — treat the target framework as unknown.
            }

            return string.Empty;
        }

        static LogLevel CompilerMessageTypeToLogLevel(CompilerMessageType compilerMessageType)
        {
            switch (compilerMessageType)
            {
                case CompilerMessageType.Error:
                    return LogLevel.Error;
                case CompilerMessageType.Warning:
                    return LogLevel.Warning;
                case CompilerMessageType.Info:
                    return LogLevel.Info;
            }

            return LogLevel.Info;
        }

        static bool IsPerformanceCriticalType(TypeDefinition typeDef)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviour(typeDef))
                return true;
            return false;
        }

        static bool IsPerformanceCriticalMethod(MethodDefinition methodDefinition)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviourUpdateMethod(methodDefinition))
                return true;
            return false;
        }
    }
}
