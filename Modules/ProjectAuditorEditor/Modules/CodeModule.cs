// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
using UnityEditor;
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
        Num
    }

    enum CompilerMessageProperty
    {
        Code = 0,
        Assembly,
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
                new PropertyDefinition { Type = PropertyType.Description, Name = "Assembly Name", MaxAutoWidth = 800},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime), Format = PropertyFormat.String, Name = "Compile Time"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AssemblyProperty.ReadOnly), Format = PropertyFormat.Bool, Name = "Read Only", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.Path, Name = "Asmdef Path"},
            }
        };

        static readonly IssueLayout k_PrecompiledAssemblyLayout = new IssueLayout
        {
            Category = IssueCategory.PrecompiledAssembly,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Assembly Name"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PrecompiledAssemblyProperty.RoslynAnalyzer), Format = PropertyFormat.Bool, Name = "Roslyn Analyzer"},
                new PropertyDefinition { Type = PropertyType.Directory, Name = "Path", IsDefaultGroup = true},
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
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(CompilerMessageProperty.Assembly), Format = PropertyFormat.String, Name = "Target Assembly", LongName = "Managed Assembly name" },
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
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor"},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            }
        };

        List<OpCode> m_OpCodes;
        List<CodeModuleInstructionAnalyzer>[] m_OpCodeAnalyzers = new List<CodeModuleInstructionAnalyzer>[ushort.MaxValue];

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

            m_OpCodes = GetAnalyzers().Select(a => a.opCodes).SelectMany(c => c).Distinct().ToList();

            ProjectIssueExtensions.AddCustomComparer(IssueCategory.Assembly, PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime),
                (a, b) =>
                {
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime));
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(AssemblyProperty.CompileTime));

                    // If one result is N/A, they all are (the analysis was done with Editor compilation - see below in this file)
                    if (strA.StartsWith("N/A"))
                        return 0;

                    // Cut off the ' ms' at the end
                    var longA = long.Parse(strA.Substring(0, strA.Length - 3));
                    var longB = long.Parse(strB.Substring(0, strB.Length - 3));

                    return longA < longB ? -1 : longA > longB ? 1 : 0;
                });
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            if (m_Ids == null)
                throw new Exception("Descriptors Database not initialized.");

            if (m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            var compatibleAnalyzers = GetCompatibleAnalyzers(analysisParams);
            for (var i = 0; i < m_OpCodeAnalyzers.Length; i++)
            {
                m_OpCodeAnalyzers[i] = null;
            }
            foreach (var opCode in m_OpCodes)
            {
                var opCodeAnalyzers = new List<CodeModuleInstructionAnalyzer>();
                foreach (var analyzer in compatibleAnalyzers)
                {
                    if (analyzer.opCodes.Contains(opCode))
                    {
                        opCodeAnalyzers.Add(analyzer);
                    }
                }
                m_OpCodeAnalyzers[(ushort)opCode.Value] = opCodeAnalyzers;
            }

            var precompiledAssemblies = AssemblyInfoProvider.GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.All)
                .Select(assemblyPath => (ReportItem)context.CreateInsight(IssueCategory.PrecompiledAssembly, Path.GetFileNameWithoutExtension(assemblyPath))
                    .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                    {
                        false
                    })
                    .WithLocation(assemblyPath))
                .ToArray();
            if (precompiledAssemblies.Any())
                analysisParams.OnIncomingIssues(precompiledAssemblies);

            // find all roslyn analyzer DLLs by label
            var roslynAnalyzerAssets = AssetDatabase.FindAssets("l:RoslynAnalyzer").Select(AssetDatabase.GUIDToAssetPath).ToList();

            // find all roslyn analyzers packaged with Project Auditor
            if (Directory.Exists(ProjectAuditor.s_RoslynAnalyzersDataPath))
            {
                var assetPaths = AssetDatabase.FindAssets("", new[] { ProjectAuditor.s_RoslynAnalyzersDataPath }).Select(AssetDatabase.GUIDToAssetPath);
                foreach (var assetPath in assetPaths)
                {
                    if (assetPath.EndsWith(".dll"))
                        roslynAnalyzerAssets.Add(assetPath);
                }
            }

            // report all roslyn analyzers as PrecompiledAssembly issues
            var roslynAnalyzerIssues = roslynAnalyzerAssets
                .Distinct()
                .Select(roslynAnalyzerDllPath => (ReportItem)context.CreateInsight(
                IssueCategory.PrecompiledAssembly,
                Path.GetFileNameWithoutExtension(roslynAnalyzerDllPath))
                .WithCustomProperties(new object[(int)PrecompiledAssemblyProperty.Num]
                {
                    true
                })
                .WithLocation(roslynAnalyzerDllPath));

            analysisParams.OnIncomingIssues(roslynAnalyzerIssues);

            var assemblyDirectories = new List<string>();
            var compilationPipeline = new AssemblyCompilation
            {
                OnAssemblyCompilationFinished = (compilationResult) =>
                {
                    analysisParams.OnIncomingIssues(ProcessCompilerMessages(context, compilationResult));
                },
                CodeOptimization = analysisParams.CodeOptimization,
                CompilationMode = analysisParams.CompilationMode,
                Platform = analysisParams.Platform,
                // TODO: reminder to add list of analyzers to metadata
                RoslynAnalyzers = UserPreferences.UseRoslynAnalyzers ? roslynAnalyzerAssets.ToArray() : null,
                AssemblyNames = analysisParams.AssemblyNames
            };

            Profiler.BeginSample("CodeModule.Audit.Compilation");
            var assemblyInfos = compilationPipeline.Compile(progress);
            Profiler.EndSample();

            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            if (analysisParams.AssemblyNames != null)
            {
                assemblyInfos = assemblyInfos.Where(a => analysisParams.AssemblyNames.Contains(a.Name)).ToArray();
            }

            if (analysisParams.CompilationMode == CompilationMode.Editor ||
                analysisParams.CompilationMode == CompilationMode.EditorPlayMode)
            {
                var issues = assemblyInfos.Select(assemblyInfo => (ReportItem)context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                    .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                    {
                        assemblyInfo.IsPackageReadOnly,
                        "N/A when Compilation Mode is Editor"
                    })
                    .WithLocation(assemblyInfo.AsmDefPath))
                    .ToArray();
                if (issues.Length > 0)
                    analysisParams.OnIncomingIssues(issues);
            }

            // process successfully compiled assemblies
            var localAssemblyInfos = assemblyInfos.Where(info => !info.IsPackageReadOnly).ToArray();
            var readOnlyAssemblyInfos = assemblyInfos.Where(info => info.IsPackageReadOnly).ToArray();
            var foundIssues = new List<ReportItem>();
            var callCrawler = new CallCrawler();
            var onCallFound = new Action<CallInfo>(pair =>
            {
                callCrawler.Add(pair);
            });
            var onIssueFoundInternal = new Action<ReportItem>(foundIssues.Add);
            var onCompleteInternal = new Action<IProgress>(bar =>
            {
                // remove issues if platform does not match
                foundIssues.RemoveAll(i => i.Id.IsValid() &&
                    !i.Id.GetDescriptor().IsApplicable(analysisParams));

                Profiler.BeginSample("CodeModule.Audit.BuildCallHierarchies");
                compilationPipeline.Dispose();
                callCrawler.BuildCallHierarchies(foundIssues, bar);
                Profiler.EndSample();

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
                if (foundIssues.Any())
                    analysisParams.OnIncomingIssues(foundIssues);
                analysisParams.OnModuleCompleted?.Invoke(AnalysisResult.Success);
            });

            assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UserAssembly | PrecompiledAssemblyTypes.UnityEngine | PrecompiledAssemblyTypes.SystemAssembly));
            if (analysisParams.CompilationMode == CompilationMode.Editor)
                assemblyDirectories.AddRange(AssemblyInfoProvider.GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes.UnityEditor));

            Profiler.BeginSample("CodeModule.Audit.Analysis");

            // first phase: analyze assemblies generated from editable scripts
            AnalyzeAssemblies(localAssemblyInfos, analysisParams.AssemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, null, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            // second phase: analyze all remaining assemblies, in a separate thread
            m_AssemblyAnalysisThread = new Thread(() =>
                AnalyzeAssemblies(readOnlyAssemblyInfos, analysisParams.AssemblyNames, assemblyDirectories, onCallFound, onIssueFoundInternal, onCompleteInternal));
            m_AssemblyAnalysisThread.Name = "Assembly Analysis";
            m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
            m_AssemblyAnalysisThread.Start();

            Profiler.EndSample();

            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;
            return AnalysisResult.InProgress;
        }

        void AnalyzeAssemblies(IReadOnlyCollection<AssemblyInfo> assemblyInfos, IReadOnlyCollection<string> assemblyFilters, IReadOnlyCollection<string> assemblyDirectories, Action<CallInfo> onCallFound, Action<ReportItem> onIssueFound, Action<IProgress> onComplete, IProgress progress = null)
        {
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var path in assemblyDirectories)
                    assemblyResolver.AddSearchDirectory(path);

                foreach (var dir in assemblyInfos.Select(info => Path.GetDirectoryName(info.Path)).Distinct())
                    assemblyResolver.AddSearchDirectory(dir);

                if (progress != null)
                    progress.Start("Analyzing Assemblies", string.Empty, assemblyInfos.Count());

                // Analyze all Player assemblies
                foreach (var assemblyInfo in assemblyInfos)
                {
                    if (progress?.IsCancelled ?? false)
                        return;

                    if (progress != null)
                        progress.Advance(assemblyInfo.Name);

                    if (!File.Exists(assemblyInfo.Path))
                    {
                        Debug.LogError(assemblyInfo.Path + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyInfo, assemblyResolver, onCallFound, assemblyFilters == null || assemblyFilters.Contains(assemblyInfo.Name) ? onIssueFound : null);
                }
            }

            progress?.Clear();
            onComplete?.Invoke(progress);
        }

        void AnalyzeAssembly(AssemblyInfo assemblyInfo, IAssemblyResolver assemblyResolver, Action<CallInfo> onCallFound, Action<ReportItem> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.Analyze " + assemblyInfo.Name);

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyInfo.Path,
                new ReaderParameters {ReadSymbols = true, AssemblyResolver = assemblyResolver, MetadataResolver = new MetadataResolverWithCache(assemblyResolver)}))
            {
                foreach (var typeDefinition in CodeAnalysis.MonoCecilHelper.AggregateAllTypeDefinitions(assembly.MainModule.Types))
                {
                    Profiler.BeginSample(typeDefinition.Name);
                    Profiler.BeginSample("CodeModule.IsPerformanceCriticalType");
                    var isPerformanceCriticalType = IsPerformanceCriticalType(typeDefinition);
                    Profiler.EndSample();
                    foreach (var methodDefinition in typeDefinition.Methods)
                    {
                        if (!methodDefinition.HasBody)
                            continue;

                        // workaround for long analysis times when Burst is installed
                        if (methodDefinition.DeclaringType.FullName.StartsWith("Unity.Burst.Editor.BurstDisassembler"))
                            continue;

                        if (!methodDefinition.DebugInformation.HasSequencePoints)
                            continue;

                        var isPerformanceCriticalContext = isPerformanceCriticalType && IsPerformanceCriticalMethod(methodDefinition);

                        AnalyzeMethodBody(assemblyInfo, methodDefinition, isPerformanceCriticalContext, onCallFound, onIssueFound);
                    }
                    Profiler.EndSample();
                }
            }

            Profiler.EndSample();
        }

        void AnalyzeMethodBody(AssemblyInfo assemblyInfo, MethodDefinition caller, bool perfCriticalContext, Action<CallInfo> onCallFound, Action<ReportItem> onIssueFound)
        {
            Profiler.BeginSample("CodeModule.AnalyzeMethodBody");

            var callerNode = new CallTreeNode(caller)
            {
                PerfCriticalContext = perfCriticalContext
            };

            var sequencePoints = caller.DebugInformation.SequencePoints;
            var lastSequencePointIndex = 0;
            var instructions = caller.Body.Instructions;
            for (var i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];

                // if issues wont be reported and the call crawler doesnt care about this instruction, immediately skip to the next one
                if (onIssueFound == null && !(inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt))
                {
                    continue;
                }

                // early out if we have no analyzers and the call crawler doesnt care
                var analyzers = m_OpCodeAnalyzers[(ushort)inst.OpCode.Value];
                if (analyzers == null && !(inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt))
                {
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
                    Profiler.BeginSample("CodeModule.OnCallFound");
                    onCallFound(new CallInfo(
                        (MethodReference)inst.Operand,
                        caller,
                        location,
                        perfCriticalContext
                    ));
                    Profiler.EndSample();
                }

                // skip analyzers if we are not interested in reporting issues, or have no analyzers
                if (onIssueFound == null || analyzers == null)
                    continue;

                var context = new InstructionAnalysisContext
                {
                    Instruction = inst,
                    MethodDefinition = caller
                };

                Profiler.BeginSample(inst.OpCode.Name);

                foreach (var analyzer in analyzers)
                {
                    Profiler.BeginSample(analyzer.GetType().Name);
                    var reportItemBuilder = analyzer.Analyze(context);
                    if (reportItemBuilder != null)
                    {
                        reportItemBuilder.WithDependencies(callerNode); // set root
                        reportItemBuilder.WithLocation(location);
                        reportItemBuilder.WithCustomProperties(new object[(int)CodeProperty.Num] {assemblyInfo.Name});

                        onIssueFound(reportItemBuilder);
                    }
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        IEnumerable<ReportItem> ProcessCompilerMessages(AnalysisContext context, AssemblyCompilationResult compilationResult)
        {
            Profiler.BeginSample("CodeModule.ProcessCompilerMessages");
            var compilerMessages = compilationResult.Messages;
            var severity = Severity.None;
            if (compilationResult.Status == CompilationStatus.MissingDependency)
                severity = Severity.Warning;
            else if (compilerMessages.Any(m => m.Type == CompilerMessageType.Error))
                severity = Severity.Error;

            var assemblyInfo = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(compilationResult.AssemblyPath);
            yield return context.CreateInsight(IssueCategory.Assembly, assemblyInfo.Name)
                .WithCustomProperties(new object[(int)AssemblyProperty.Num]
                {
                    assemblyInfo.IsPackageReadOnly,
                    compilationResult.DurationInMs + " ms"
                })
                .WithDependencies(new AssemblyDependencyNode(assemblyInfo.Name, compilationResult.DependentAssemblyNames))
                .WithLocation(assemblyInfo.AsmDefPath)
                .WithSeverity(severity);

            foreach (var message in compilerMessages)
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

                    yield return context.CreateIssue(IssueCategory.DomainReload, descriptor.Id)
                        .WithLocation(relativePath, message.Line)
                        .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type))
                        .WithCustomProperties(new object[(int)CompilerMessageProperty.Num]
                        {
                            message.Code,
                            assemblyInfo.Name
                        });
                }
                else
                {
                    yield return context.CreateInsight(IssueCategory.CodeCompilerMessage, message.Message)
                        .WithCustomProperties(new object[(int)CompilerMessageProperty.Num]
                        {
                            message.Code,
                            assemblyInfo.Name
                        })
                        .WithLocation(relativePath, message.Line)
                        .WithLogLevel(CompilerMessageTypeToLogLevel(message.Type));
                }
            }

            Profiler.EndSample();
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
