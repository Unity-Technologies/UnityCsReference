// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Profiling;
using PropertyDefinition = Unity.ProjectAuditor.Editor.Core.PropertyDefinition;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AssemblyProperty
    {
        ReadOnly = 0,
        CompileTime,
        CodeLocation,
        Num
    }

    enum PrecompiledAssemblyProperty
    {
        RoslynAnalyzer = 0,
        Num
    }

    internal enum CodeProperty
    {
        Assembly = 0,
        CodeLocation,
        Num
    }

    enum CompilerMessageProperty
    {
        Code = 0,
        Assembly,
        CodeLocation,
        Num
    }

    class CodeModule : ModuleWithAnalyzers<CodeModuleInstructionAnalyzer>
    {
        static readonly IssueLayout k_AssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.Assembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.LogLevel, Name = "Log Level"},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name", MaxAutoWidth = 800},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime), Format = PropertyFormat.String, Name = "Compile Time"},
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

        static readonly IssueLayout k_DomainReloadIssueLayout = new IssueLayout
        {
            Category = IssueCategory.DomainReload,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Code), Format = PropertyFormat.String, Name = "Code", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Filename, Name = "Filename", LongName = "Filename and line number"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), Format = PropertyFormat.String, Name = "Assembly", LongName = "Managed Assembly name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.CodeLocation), Format = PropertyFormat.String, Name = "Location", LongName = "Code Location" },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor"},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        List<OpCode> m_OpCodes;
        List<int>[] m_OpCodeAnalyzers = new List<int>[ushort.MaxValue];
        CodeModuleInstructionAnalyzer[] m_CompatibleAnalyzers;

        Thread m_AssemblyAnalysisThread;

        public override string Name => "Code";

        // Match a whole "word", starting with UDR and ending with exactly 4 digits, e.g. UDR1234
        static readonly Regex s_RegEx = new Regex(@"\bUDR\d{4}\b");

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_IssueLayout,
            k_AssemblyLayout,
            k_PrecompiledAssemblyLayout,
            k_CompilerMessageLayout,
            k_DomainReloadIssueLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_OpCodes = new List<OpCode>(GetAnalyzers().Select(a => a.opCodes).SelectMany(c => c).Distinct());
#pragma warning restore RS0030

            ProjectIssueExtensions.AddCustomComparer(IssueCategory.Assembly, PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime),
                (a, b) =>
                {
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime));
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime));

                    // Cut off the ' ms' at the end, and ignore editor entries
                    var longA = strA.Contains("Editor") ? -1 : long.Parse(strA.Substring(0, strA.Length - 3));
                    var longB = strB.Contains("Editor") ? -1 : long.Parse(strB.Substring(0, strB.Length - 3));

                    return longA < longB ? -1 : longA > longB ? 1 : 0;
                });
        }

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            if (m_Ids == null)
                throw new Exception("Descriptors Database not initialized.");

            if (m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            m_CompatibleAnalyzers = GetCompatibleAnalyzers(analysisParams);
            for (var i = 0; i < m_OpCodeAnalyzers.Length; i++)
            {
                m_OpCodeAnalyzers[i] = null;
            }
            foreach (var opCode in m_OpCodes)
            {
                var opCodeAnalyzers = new List<int>();
                for (int analyzerIndex = 0; analyzerIndex < m_CompatibleAnalyzers.Length; analyzerIndex++)
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (m_CompatibleAnalyzers[analyzerIndex].opCodes.Contains(opCode))
#pragma warning restore RS0030
                        opCodeAnalyzers.Add(analyzerIndex);
                }
                m_OpCodeAnalyzers[(ushort)opCode.Value] = opCodeAnalyzers;
            }

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var precompiledAssemblies = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All)
                .Select(assemblyPath => (ReportItem)context.CreateInsight(IssueCategory.PrecompiledAssembly, Path.GetFileNameWithoutExtension(assemblyPath))
                    .WithCustomProperties([false])
                    .WithLocation(assemblyPath))
                .ToArray();
#pragma warning restore RS0030
            if (precompiledAssemblies.Length > 0)
                analysisParams.OnIncomingIssues(precompiledAssemblies);

            yield return null;

            // find all roslyn analyzer DLLs by label
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var roslynAnalyzerAssets = new List<string>(AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath));
#pragma warning restore RS0030

            // find all roslyn analyzers packaged with Project Auditor
            if (Directory.Exists(ProjectAuditor.s_RoslynAnalyzersDataPath))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var assetPaths = AssetDatabase.FindAssets("", [ProjectAuditor.s_RoslynAnalyzersDataPath]).Select(AssetDatabase.GUIDToAssetPath);
#pragma warning restore RS0030
                foreach (var assetPath in assetPaths)
                {
                    if (assetPath.EndsWith(".dll"))
                        roslynAnalyzerAssets.Add(assetPath);
                }
            }

            // report all roslyn analyzers as PrecompiledAssembly issues
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var roslynAnalyzerIssues = roslynAnalyzerAssets
#pragma warning restore RS0030
                .Distinct()
                .Select(roslynAnalyzerDllPath => (ReportItem)context.CreateInsight(
                IssueCategory.PrecompiledAssembly,
                Path.GetFileNameWithoutExtension(roslynAnalyzerDllPath))
                .WithCustomProperties([true])
                .WithLocation(roslynAnalyzerDllPath));

            analysisParams.OnIncomingIssues(roslynAnalyzerIssues);

            yield return null;

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
                RoslynAnalyzers = UserPreferences.UseRoslynAnalyzers ? roslynAnalyzerAssets.ToArray() : null,
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                compiledEditorAssemblyPaths = new List<AssemblyInfo>(compiledEditorAssemblyPaths.Where(a => Array.IndexOf(analysisParams.AssemblyNames, a.Name) != -1));
                compiledPlayerAssemblyPaths = new List<AssemblyInfo>(compiledPlayerAssemblyPaths.Where(a => Array.IndexOf(analysisParams.AssemblyNames, a.Name) != -1));
#pragma warning restore RS0030
            }

            if (compiledEditorAssemblyPaths.Count > 0)
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var issues = compiledEditorAssemblyPaths.Select(assemblyInfo => (ReportItem)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
#pragma warning restore RS0030
                    .WithCustomProperties(
                    [
                        assemblyInfo.IsReadOnly,
                        "0 ms (Compiled by Editor)",
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var issues = compiledPlayerAssemblyPaths
                    .Where(assemblyInfo => assemblyInfo.IsUnityInternalAssembly)
                    .Select(assemblyInfo => (ReportItem)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                    .WithCustomProperties(
                    [
                        assemblyInfo.IsReadOnly,
                        "0 ms (Compiled by Editor)",
                        assemblyInfo.GetTypeString()
                    ])
                    .WithLocation(assemblyInfo.AsmDefPath))
                    .ToArray();
#pragma warning restore RS0030
                if (issues.Length > 0)
                    analysisParams.OnIncomingIssues(issues);
            }

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var assemblyInfos = compiledEditorAssemblyPaths.Concat(compiledPlayerAssemblyPaths)
                .Where(a => AssemblyPackageFilter(a, analysisParams)).ToArray();
            #pragma warning restore RS0030

            if (progress?.IsCancelled ?? false)
            {
                analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Cancelled, 0);
                yield break;
            }
            
            AsyncProgressState progressState = progress?.Start("Analyzing Assemblies", assemblyInfos.Length);
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            // Process successfully compiled assemblies
            var localAssemblyInfos = assemblyInfos.Where(info => !info.IsReadOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.IsReadOnly).ToArray();
#pragma warning restore RS0030
            var foundIssues = new List<ReportItem>();
            var callCrawler = new CallCrawler();
            var onIssueFoundInternal = new Action<ReportItem>(foundIssues.Add);

            var onCompleteInternal = new Action<IProgress, long>((bar, threadExecutionTimeMs) =>
            {
                // remove issues if platform does not match
                foundIssues.RemoveAll(i => i.Id.IsValid() &&
                    !i.Id.GetDescriptor().IsApplicable(analysisParams));

                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(foundIssues, bar);

                foreach (var d in foundIssues)
                {
                    // bump severity if issue is found in a hot-path
                    if (!d.IsMajorOrCritical() && d.Dependencies != null && d.Dependencies.PerfCriticalContext)
                    {
                        switch (d.Severity)
                        {
                            case Severity.Minor:
                                d.Severity = Severity.Moderate;
                                break;
                            case Severity.Moderate:
                                d.Severity = Severity.Major;
                                break;
                        }
                    }
                }

                // workaround for empty 'relativePath' strings which are not all available when 'onIssueFoundInternal' is called
                if (foundIssues.Count > 0)
                    analysisParams.OnIncomingIssues(foundIssues);

                bar?.Clear(progressState);
                analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, threadExecutionTimeMs);
            });

            var assemblyDirectories = new List<string>();
            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine | PrecompiledAssemblyTypes.SystemAssembly));
            if ((analysisParams.CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            yield return null;

            long executionTimeMs = 0;

            // first phase: analyze assemblies generated from editable scripts
            // second phase: analyze all remaining assemblies
            m_AssemblyAnalysisThread = new Thread(() =>
            {
                // Run analysis on the background thread
                var startTime = DateTime.UtcNow;

                AnalyzeAssemblies(localAssemblyInfos, analysisParams, assemblyDirectories, callCrawler, onIssueFoundInternal, progress, progressState);
                AnalyzeAssemblies(readOnlyAssemblyInfos, analysisParams, assemblyDirectories, callCrawler, onIssueFoundInternal, progress, progressState);

                executionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            });
            m_AssemblyAnalysisThread.Name = "Assembly Analysis";
            m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
            m_AssemblyAnalysisThread.Start();

            while (m_AssemblyAnalysisThread.IsAlive)
                yield return new WaitForEndOfFrame();

            onCompleteInternal?.Invoke(progress, executionTimeMs);
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

        void AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, AnalysisParams analysisParams, IReadOnlyCollection<string> assemblyDirectories, CallCrawler callCrawler, Action<ReportItem> onIssueFound, IProgress progress, AsyncProgressState progressState)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.Path)).Distinct())
#pragma warning restore RS0030
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
                    AnalyzeAssembly(assemblyInfo, analysisParams, assemblyResolver, callCrawler, onIssueFoundFiltered);
                }
            }
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, AnalysisParams analysisParams, IAssemblyResolver assemblyResolver, CallCrawler callCrawler, Action<ReportItem> onIssueFound)
        {
            try
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.Path,
                    new ReaderParameters { ReadSymbols = true, AssemblyResolver = assemblyResolver, MetadataResolver = new MetadataResolverWithCache(assemblyResolver) }))
                {
                    object[] assemblyUserData = new object[m_CompatibleAnalyzers.Length];
                    for (int analyzerIndex = 0; analyzerIndex < m_CompatibleAnalyzers.Length; analyzerIndex++)
                        assemblyUserData[analyzerIndex] = m_CompatibleAnalyzers[analyzerIndex].OnAnalyzeAssembly();

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

                            AnalyzeMethodBody(assemblyInfo, methodDefinition, assemblyUserData, isPerformanceCriticalContext, callCrawler, onIssueFound);
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

        void AnalyzeMethodBody(AssemblyInfo assemblyInfo, MethodDefinition caller, object[] assemblyUserData, bool perfCriticalContext, CallCrawler callCrawler, Action<ReportItem> onIssueFound)
        {
            var callerNode = new CallTreeNode(caller)
            {
                PerfCriticalContext = perfCriticalContext
            };

            for (int analyzerIndex = 0; analyzerIndex < m_CompatibleAnalyzers.Length; analyzerIndex++)
                m_CompatibleAnalyzers[analyzerIndex].OnAnalyzeMethodBody(caller, assemblyUserData[analyzerIndex]);

            var sequencePoints = caller.DebugInformation.SequencePoints;
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
                    callCrawler.Add(
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
                    var reportItemBuilder = m_CompatibleAnalyzers[analyzer].Analyze(context);
                    if (reportItemBuilder != null)
                    {
                        reportItemBuilder.WithDependencies(callerNode); // set root
                        reportItemBuilder.WithLocation(location);
                        reportItemBuilder.WithCustomProperties([assemblyInfo.Name, assemblyInfo.GetTypeString()]);

                        onIssueFound(reportItemBuilder);
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
                    compilationResult.DurationInMs + " ms",
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
                var descriptor = new Descriptor(
                    message.Code,
                    message.Message,
                    Areas.IterationTime,
                    RoslynTextLookup.GetDescription(message.Code),
                    RoslynTextLookup.GetRecommendation(message.Code));

                DescriptorLibrary.RegisterDescriptor(descriptor.Id, descriptor);

                return context.CreateIssue(IssueCategory.DomainReload, descriptor.Id)
                    .WithLocation(relativePath, message.Line)
                    .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type))
                    .WithCustomProperties(
                    [
                        message.Code,
                        assemblyInfo.Name,
                        assemblyInfo.GetTypeString()
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
