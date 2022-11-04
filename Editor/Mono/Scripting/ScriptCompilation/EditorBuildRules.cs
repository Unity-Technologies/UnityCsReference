// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Do not add "using System.IO", use AssetPath methods instead
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // This class is intentionally immutable, avoid adding mutable state to it.
    static class EditorBuildRules
    {
        internal enum UnityReferencesOptions
        {
            None = 0,
            ExcludeModules = 1,
        }

        public class CompilationAssemblies
        {
            public PrecompiledAssembly[] UnityAssemblies { get; set; }
            public Dictionary<string, PrecompiledAssembly> PrecompiledAssemblies { get; set; }
            public Dictionary<string, TargetAssembly> CustomTargetAssemblies { get; set; }
            public TargetAssembly[] PredefinedAssembliesCustomTargetReferences { get; set; }
            public string[] EditorAssemblyReferences { get; set; }
            public string[] RoslynAnalyzerDllPaths { get; set; }
        }

        public static Dictionary<string, TargetAssembly> predefinedTargetAssemblies { get; private set; }

        private static readonly string[] s_CSharpVersionDefines =
        {
            "CSHARP_7_OR_LATER", // Incremental Compiler adds this.
            "CSHARP_7_3_OR_NEWER",
        };

        static EditorBuildRules()
        {
            predefinedTargetAssemblies = CreatePredefinedTargetAssemblies();
        }

        public static TargetAssembly[] GetPredefinedTargetAssemblies()
        {
            return predefinedTargetAssemblies.Values.ToArray();
        }

        public static string[] PredefinedTargetAssemblyNames
        {
            get { return predefinedTargetAssemblies.Select(a => AssetPath.GetAssemblyNameWithoutExtension(a.Key)).ToArray(); }
        }

        /// <summary>
        /// Checks the scriptPath against the pathPrefix and additionalPathPrefixes.
        /// Returns the depth from the scriptPath where -1 indicates no match.
        /// </summary>
        internal static int PathFilter(string scriptPath, string pathPrefix, string lowerPathPrefix, string[] additionalPathPrefixes, string[] lowerAdditionalPathPrefixes)
        {
            // Find the path that is closest to the script path
            int depth = Utility.FastStartsWith(scriptPath, pathPrefix, lowerPathPrefix) ? pathPrefix.Length : -1;

            if (additionalPathPrefixes != null)
            {
                for (int i = 0; i < additionalPathPrefixes.Length; ++i)
                {
                    if (Utility.FastStartsWith(scriptPath, additionalPathPrefixes[i], lowerAdditionalPathPrefixes[i]) && additionalPathPrefixes[i].Length > depth)
                    {
                        depth = additionalPathPrefixes[i].Length;
                    }
                }
            }

            return depth;
        }

        public static Dictionary<string, TargetAssembly> CreateTargetAssemblies(IEnumerable<CustomScriptAssembly> customScriptAssemblies)
        {
            if (customScriptAssemblies == null)
                return null;

            var targetAssemblies = new List<TargetAssembly>();
            var nameToTargetAssembly = new Dictionary<string, TargetAssembly>();

            // Create TargetAssemblies
            foreach (var customAssembly in customScriptAssemblies)
            {
                var lowerPathPrefix = Utility.FastToLower(customAssembly.PathPrefix);
                var lowerAdditionalPathPrefixes = customAssembly.AdditionalPrefixes?.Select(Utility.FastToLower).ToArray();

                var targetAssembly = new TargetAssembly(customAssembly.Name + ".dll",
                    customAssembly.AssemblyFlags,
                    TargetAssemblyType.Custom,
                    customAssembly.PathPrefix,
                    customAssembly.AdditionalPrefixes,
                    path => PathFilter(path, customAssembly.PathPrefix, lowerPathPrefix, customAssembly.AdditionalPrefixes, lowerAdditionalPathPrefixes),
                    (settings, defines) => customAssembly.IsCompatibleWith(settings.BuildTarget, settings.Subtarget, settings.CompilationOptions, defines),
                    customAssembly.CompilerOptions)
                {
                    ExplicitPrecompiledReferences = customAssembly.PrecompiledReferences?.ToList() ?? new List<string>(),
                    VersionDefines = customAssembly.VersionDefines != null
                        ? customAssembly.VersionDefines.ToList() : new List<VersionDefine>(),
                    RootNamespace = customAssembly.RootNamespace,
                    ResponseFileDefines = customAssembly.ResponseFileDefines,
                    AsmDefPath = customAssembly.FilePath
                };

                targetAssemblies.Add(targetAssembly);
                nameToTargetAssembly[customAssembly.Name] = targetAssembly;
            }

            var targetAssembliesEnumerator = targetAssemblies.GetEnumerator();

            // Setup references for TargetAssemblies
            foreach (var customAssembly in customScriptAssemblies)
            {
                targetAssembliesEnumerator.MoveNext();
                var targetAssembly = targetAssembliesEnumerator.Current;

                if (customAssembly.References == null)
                    continue;

                foreach (var reference in customAssembly.References)
                {
                    TargetAssembly referenceAssembly = null;

                    if (!nameToTargetAssembly.TryGetValue(reference, out referenceAssembly))
                    {
                        continue;
                    }

                    targetAssembly.References.Add(referenceAssembly);
                }
            }

            var customTargetAssembliesDict = new Dictionary<string, TargetAssembly>();

            foreach (var targetAssembly in targetAssemblies)
            {
                customTargetAssembliesDict[targetAssembly.Filename] = targetAssembly;
            }

            return customTargetAssembliesDict;
        }

        public static ScriptAssembly[] GetAllScriptAssemblies(
            Dictionary<string, string> allSourceFiles,
            String projectDirectory,
            ScriptAssemblySettings settings,
            CompilationAssemblies assemblies,
            ISafeModeInfo safeModeInfo,
            TargetAssemblyType onlyIncludeType = TargetAssemblyType.Undefined,
            Func<TargetAssembly, bool> targetAssemblyCondition = null,
            ICompilationSetupWarningTracker warningSink = null)
        {
            if (allSourceFiles == null || allSourceFiles.Count == 0)
                return new ScriptAssembly[0];

            var targetAssemblyFiles = new Dictionary<TargetAssembly, DirtyTargetAssembly>();

            foreach (var entry in allSourceFiles)
            {
                var scriptFile = entry.Key;
                var assemblyName = entry.Value;
                var targetAssembly = GetTargetAssembly(scriptFile, assemblyName, projectDirectory, assemblies.CustomTargetAssemblies);

                if (targetAssembly == null)
                    continue;

                if (!IsCompatibleWithPlatformAndDefines(targetAssembly, settings))
                    continue;

                if (targetAssemblyCondition != null && !targetAssemblyCondition(targetAssembly))
                    continue;

                // Optionally only include specific TargetAssemblyType assemblies.
                if (onlyIncludeType != TargetAssemblyType.Undefined && targetAssembly.Type != onlyIncludeType)
                    continue;

                DirtyTargetAssembly dirtyTargetAssembly;

                if (!targetAssemblyFiles.TryGetValue(targetAssembly, out dirtyTargetAssembly))
                {
                    dirtyTargetAssembly = new DirtyTargetAssembly();
                    targetAssemblyFiles[targetAssembly] = dirtyTargetAssembly;
                }

                dirtyTargetAssembly.SourceFiles.Add(AssetPath.Combine(projectDirectory, scriptFile));
            }

            return ToScriptAssemblies(targetAssemblyFiles, settings, assemblies, warningSink, safeModeInfo);
        }

        internal class DirtyTargetAssembly
        {
            public DirtyTargetAssembly()
            {
                SourceFiles = new HashSet<string>();
            }

            public HashSet<string> SourceFiles { get; set; }
        }

        internal static ScriptAssembly[] ToScriptAssemblies(
            IDictionary<TargetAssembly, DirtyTargetAssembly> targetAssemblies,
            ScriptAssemblySettings settings,
            CompilationAssemblies assemblies, ICompilationSetupWarningTracker warningSink, ISafeModeInfo safeModeInfo)
        {
            var scriptAssemblies = new ScriptAssembly[targetAssemblies.Count];

            var targetToScriptAssembly = new Dictionary<TargetAssembly, ScriptAssembly>();
            int index = 0;

            bool buildingForEditor = settings.BuildingForEditor;
            var safeModeWhiteList = new HashSet<string>(safeModeInfo.GetWhiteListAssemblyNames());
            foreach (var entry in targetAssemblies)
            {
                var targetAssembly = entry.Key;
                var dirtyTargetAssembly = entry.Value;
                var scriptAssembly = new ScriptAssembly();

                // Setup TargetAssembly -> ScriptAssembly mapping for converting references
                scriptAssemblies[index] = scriptAssembly;
                targetToScriptAssembly[targetAssembly] = scriptAssemblies[index++];

                // Setup ScriptAssembly
                scriptAssembly.Flags = targetAssembly.Flags;
                scriptAssembly.BuildTarget = settings.BuildTarget;
                scriptAssembly.OriginPath = targetAssembly.PathPrefix;
                scriptAssembly.Filename = targetAssembly.Filename;
                scriptAssembly.SkipCodeGen = safeModeWhiteList.Contains(targetAssembly.Filename);
                scriptAssembly.RootNamespace = targetAssembly.Type == TargetAssemblyType.Predefined ? settings.ProjectRootNamespace : targetAssembly.RootNamespace;

                scriptAssembly.OutputDirectory = settings.OutputDirectory;

                var cSharpVersionDefines = targetAssembly.Defines == null ? s_CSharpVersionDefines.ToList() : targetAssembly.Defines.Concat(s_CSharpVersionDefines).ToList();

                //This is used for Source Generation
                if ((targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly)
                {
                    cSharpVersionDefines.Add("UNITY_EDITOR_ONLY_COMPILATION");
                }

                scriptAssembly.Defines = cSharpVersionDefines.ToArray();
                scriptAssembly.Files = dirtyTargetAssembly.SourceFiles.ToArray();
                scriptAssembly.TargetAssemblyType = targetAssembly.Type;
                scriptAssembly.AsmDefPath = targetAssembly.AsmDefPath;

                if (scriptAssembly.TargetAssemblyType == TargetAssemblyType.Predefined)
                    scriptAssembly.CompilerOptions = new ScriptCompilerOptions(settings.PredefinedAssembliesCompilerOptions);
                else
                    scriptAssembly.CompilerOptions = targetAssembly.CompilerOptions;

                scriptAssembly.CompilerOptions.AdditionalCompilerArguments = settings.AdditionalCompilerArguments;

                var editorOnlyTargetAssembly = (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                if (editorOnlyTargetAssembly)
                    scriptAssembly.CompilerOptions.ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Unity_4_8;
                else
                    scriptAssembly.CompilerOptions.ApiCompatibilityLevel = settings.PredefinedAssembliesCompilerOptions.ApiCompatibilityLevel;

                if ((settings.CompilationOptions & EditorScriptCompilationOptions.BuildingUseDeterministicCompilation) == EditorScriptCompilationOptions.BuildingUseDeterministicCompilation)
                    scriptAssembly.CompilerOptions.UseDeterministicCompilation = true;
                else
                    scriptAssembly.CompilerOptions.UseDeterministicCompilation = false;

                scriptAssembly.CompilerOptions.CodeOptimization = settings.CodeOptimization;
            }

            // Don't add the auto-referenced engine assemblies if the assembly either has the flag set, or
            // is a codegen assembly
            AutoReferencedPackageAssemblies.AddReferences(assemblies.CustomTargetAssemblies, settings.CompilationOptions,
                t =>
                {
                    var hasNoEngineReferencesFlag = (t.Flags & AssemblyFlags.NoEngineReferences) == AssemblyFlags.NoEngineReferences;
                    if (hasNoEngineReferencesFlag)
                        return false;

                    return !UnityCodeGenHelpers.IsCodeGen(t.Filename);
                });

            // Setup ScriptAssembly references
            index = 0;
            foreach (var entry in targetAssemblies)
            {
                var scriptAssembly = scriptAssemblies[index++];
                AddScriptAssemblyReferences(ref scriptAssembly, entry.Key, settings, assemblies, targetToScriptAssembly, warningSink);

                if (UnityCodeGenHelpers.IsCodeGen(entry.Key.Filename)
                    ||  UnityCodeGenHelpers.IsCodeGenTest(entry.Key.Filename)
                    || CompilationPipelineCommonHelper.ShouldAdd(entry.Key.Filename))
                {
                    CompilationPipelineCommonHelper.UpdateScriptAssemblyReference(ref scriptAssembly);
                }

                if (!buildingForEditor)
                {
                    PlatformSupportModuleHelpers.AddAdditionalPlatformSupportData(settings.CompilationExtension, ref scriptAssembly);
                }
            }

            if (assemblies.RoslynAnalyzerDllPaths != null)
                RoslynAnalyzers.SetAnalyzers(scriptAssemblies, assemblies.CustomTargetAssemblies.Values.ToArray(), assemblies.RoslynAnalyzerDllPaths, false);

            return scriptAssemblies;
        }

        static bool IsPrecompiledAssemblyCompatibleWithBuildTarget(PrecompiledAssembly compiledAssembly, BuildTarget buildTarget)
        {
            if (buildTarget == BuildTarget.WSAPlayer)
            {
                // Apparently this is used for IL2CPP too. TO DO: figure out why we need this (.winmd files end up not being referenced when this gets removed)
                bool compiledAssemblyCompatibleWithDotNet = (compiledAssembly.Flags & AssemblyFlags.UseForDotNet) == AssemblyFlags.UseForDotNet;
                return compiledAssemblyCompatibleWithDotNet;
            }

            bool compiledAssemblyCompatibleWithMono = (compiledAssembly.Flags & AssemblyFlags.UseForMono) == AssemblyFlags.UseForMono;
            return compiledAssemblyCompatibleWithMono;
        }

        internal static void AddScriptAssemblyReferences(ref ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, ScriptAssemblySettings settings,
            CompilationAssemblies assemblies,
            IDictionary<TargetAssembly, ScriptAssembly> targetToScriptAssembly, ICompilationSetupWarningTracker warningSink)
        {
            var scriptAssemblyReferences = new List<ScriptAssembly>(targetAssembly.References.Count);
            var references = new List<string>();
            bool buildingForEditor = settings.BuildingForEditor;
            bool noEngineReferences = (targetAssembly.Flags & AssemblyFlags.NoEngineReferences) == AssemblyFlags.NoEngineReferences;

            bool shouldProcessPredefinedCustomTargets = assemblies.CustomTargetAssemblies != null && (targetAssembly.Type & TargetAssemblyType.Predefined) == TargetAssemblyType.Predefined;
            var predefinedCustomTargetReferences = Enumerable.Empty<TargetAssembly>();
            if (shouldProcessPredefinedCustomTargets && assemblies.PredefinedAssembliesCustomTargetReferences != null)
                predefinedCustomTargetReferences = assemblies.PredefinedAssembliesCustomTargetReferences;

            var unityReferences = Array.Empty<PrecompiledAssembly>();

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) references, as long as the target
            // doesn't specify that it doesn't want them.
            if (!noEngineReferences)
            {
                // Add predefined custom target references in a hash-set for fast lookup
                var predefinedCustomTargetRefs = new HashSet<string>(predefinedCustomTargetReferences.Select(x => x.Filename));
                unityReferences = GetUnityReferences(scriptAssembly, targetAssembly, assemblies.UnityAssemblies, predefinedCustomTargetRefs, settings.CompilationOptions, UnityReferencesOptions.None);
                references.AddRange(unityReferences
                    .Where(r => !r.Flags.HasFlag(AssemblyFlags.UserOverride))
                    .Select(r => r.Path)
                );
            }

            AddTestRunnerCustomReferences(ref targetAssembly, assemblies.CustomTargetAssemblies);

            // Setup target assembly references
            foreach (var reference in targetAssembly.References)
            {
                ScriptAssembly scriptAssemblyReference;

                // If the assembly already showed up in the unity references, don't reference it here.
                // This can happen when an assembly is configured as a unity assembly override, but
                // overrides are disabled. The Unity assembly should take precedence in that case.
                if (unityReferences.Any(r => Path.GetFileName(r.Path) == reference.Filename))
                    continue;

                // Add ScriptAssembly references to other dirty script assemblies that also need to be rebuilt.
                if (targetToScriptAssembly.TryGetValue(reference, out scriptAssemblyReference))
                {
                    System.Diagnostics.Debug.Assert(scriptAssemblyReference != null);
                    scriptAssemblyReferences.Add(scriptAssemblyReference);
                }
            }

            // For predefined target assembly add references to custom target assemblies
            if (shouldProcessPredefinedCustomTargets)
            {
                foreach (var customTargetAssembly in predefinedCustomTargetReferences)
                {
                    ScriptAssembly scriptAssemblyReference;

                    // Only add ScriptAssembly reference if the custom target assembly is dirty, e.g. is in targetToScriptAssembly dictionary
                    // Otherwise just add already compiled custom target assembly as precompiled reference.
                    if (targetToScriptAssembly.TryGetValue(customTargetAssembly, out scriptAssemblyReference))
                        scriptAssemblyReferences.Add(scriptAssemblyReference);
                }
            }

            var unityReferencesGenerated = unityReferences
                .Where(r => r.Flags.HasFlag(AssemblyFlags.UserOverride))
                .Select(r => Path.GetFileName(r.Path));
            foreach (var assembly in unityReferencesGenerated)
            {
                var scriptAssemblyReference = targetToScriptAssembly.FirstOrDefault(kvp => kvp.Key.Filename == assembly);
                if (scriptAssemblyReference.Value != null)
                    scriptAssemblyReferences.Add(scriptAssemblyReference.Value);
            }

            // Add pre-compiled assemblies as references
            var allPrecompiledAssemblies = assemblies.PrecompiledAssemblies ?? new Dictionary<string, PrecompiledAssembly>(0);
            List<PrecompiledAssembly> precompiledReferences = new List<PrecompiledAssembly>(allPrecompiledAssemblies.Count);
            var explicitPrecompiledReferences = new List<PrecompiledAssembly>(targetAssembly.ExplicitPrecompiledReferences.Count);

            if ((targetAssembly.Flags & AssemblyFlags.ExplicitReferences) == AssemblyFlags.ExplicitReferences)
            {
                if (!noEngineReferences)
                {
                    precompiledReferences.AddRange(allPrecompiledAssemblies
                        .Where(x => (x.Value.Flags & AssemblyFlags.UserAssembly) != AssemblyFlags.UserAssembly)
                        .Select(x => x.Value));
                }

                foreach (var explicitPrecompiledReference in targetAssembly.ExplicitPrecompiledReferences)
                {
                    PrecompiledAssembly assembly;
                    if (allPrecompiledAssemblies.TryGetValue(explicitPrecompiledReference, out assembly))
                    {
                        explicitPrecompiledReferences.Add(assembly);
                    }
                }
            }
            else
            {
                var precompiledAssemblies = allPrecompiledAssemblies.Values.Where(x => (x.Flags & AssemblyFlags.ExplicitlyReferenced) != AssemblyFlags.ExplicitlyReferenced).ToList();

                // if noEngineReferences, add just the non-explicitly-referenced user assemblies
                if (noEngineReferences)
                    precompiledReferences.AddRange(precompiledAssemblies.Where(x => (x.Flags & AssemblyFlags.UserAssembly) == AssemblyFlags.UserAssembly));
                else
                    precompiledReferences.AddRange(precompiledAssemblies);
            }

            AddTestRunnerPrecompiledReferences(targetAssembly, allPrecompiledAssemblies, ref precompiledReferences);

            var precompiledReferenceNames = GetPrecompiledReferences(scriptAssembly, targetAssembly.Type, settings.CompilationOptions, targetAssembly.editorCompatibility, precompiledReferences, explicitPrecompiledReferences, warningSink);
            references.AddRange(precompiledReferenceNames);

            if (buildingForEditor && assemblies.EditorAssemblyReferences != null)
                references.AddRange(assemblies.EditorAssemblyReferences);

            references.AddRange(MonoLibraryHelpers.GetSystemLibraryReferences(scriptAssembly.CompilerOptions.ApiCompatibilityLevel));

            scriptAssembly.ScriptAssemblyReferences = scriptAssemblyReferences.ToArray();
            scriptAssembly.References = references.ToArray();
        }

        internal static void AddTestRunnerCustomReferences(ref TargetAssembly targetAssembly, Dictionary<string, TargetAssembly> assembliesCustomTargetAssemblies)
        {
            if (TestRunnerHelpers.ShouldAddTestRunnerReferences(targetAssembly) && assembliesCustomTargetAssemblies != null)
            {
                targetAssembly.References.AddRange(TestRunnerHelpers.GetReferences(assembliesCustomTargetAssemblies.Values));
            }
        }

        internal static void AddTestRunnerPrecompiledReferences(TargetAssembly targetAssembly, Dictionary<string, PrecompiledAssembly> nameToPrecompiledAssemblies, ref List<PrecompiledAssembly> precompiledReferences)
        {
            if (TestRunnerHelpers.ShouldAddNunitReferences(targetAssembly))
            {
                TestRunnerHelpers.AddNunitReferences(nameToPrecompiledAssemblies, ref precompiledReferences);
            }
        }

        public static PrecompiledAssembly[] GetUnityReferences(ScriptAssembly scriptAssembly, PrecompiledAssembly[] unityAssemblies, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            return GetUnityReferences(scriptAssembly, null, unityAssemblies, options, unityReferencesOptions);
        }

        public static PrecompiledAssembly[] GetUnityReferences(ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, PrecompiledAssembly[] unityAssemblies, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            return GetUnityReferences(scriptAssembly, targetAssembly, unityAssemblies, null, options, unityReferencesOptions);
        }

        public static PrecompiledAssembly[] GetUnityReferences(ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, PrecompiledAssembly[] unityAssemblies, HashSet<string> predefinedCustomTargetReferences, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            var references = new List<PrecompiledAssembly>();

            bool assemblyEditorOnly = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool excludeUnityModules = unityReferencesOptions == UnityReferencesOptions.ExcludeModules;
            bool isOverridingUnityAssembly = false;

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) referencees.
            if (unityAssemblies == null)
                return references.ToArray();

            foreach (var unityAssembly in unityAssemblies)
            {
                if ((unityAssembly.Flags & (AssemblyFlags.UserOverride | AssemblyFlags.UserOverrideCandidate)) != AssemblyFlags.None)
                {
                    var unityAssemblyFileName = AssetPath.GetFileName(unityAssembly.Path);

                    // This scriptAssembly is overriding this unityAssembly so it should probably not depend on itself.
                    if (unityAssemblyFileName == scriptAssembly.Filename)
                    {
                        isOverridingUnityAssembly = true;
                        continue;
                    }

                    // Custom targets may override Unity references, do not add them to avoid duplicated references.
                    if (predefinedCustomTargetReferences != null && predefinedCustomTargetReferences.Contains(unityAssemblyFileName))
                        continue;

                    // If this scriptAssembly/targetAssembly explicitly references another
                    // scriptAssembly that has actually overridden this unityAssembly, we should
                    // not add the unityAssembly to the references as well. It's possible
                    // that this scriptAssembly is using new APIs that don't exist in the shipped
                    // copy of the unityAssembly.
                    if (targetAssembly != null && targetAssembly.References.Any(ta => ta.Filename == unityAssemblyFileName))
                        continue;
                }

                var isUnityModule = (unityAssembly.Flags & AssemblyFlags.UnityModule) == AssemblyFlags.UnityModule;

                if (isUnityModule && excludeUnityModules)
                    continue;

                var moduleExcludedForRuntimeCode = (unityAssembly.Flags & AssemblyFlags.ExcludedForRuntimeCode) == AssemblyFlags.ExcludedForRuntimeCode;

                // Add Unity editor assemblies (UnityEditor.dll) to all assemblies when building inside the editor
                if ((buildingForEditor && !moduleExcludedForRuntimeCode) || assemblyEditorOnly)
                {
                    if ((unityAssembly.Flags & AssemblyFlags.UseForMono) != 0)
                        references.Add(unityAssembly);
                }
                else
                {
                    bool unityAssemblyEditorOnly = (unityAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                    // Add Unity runtime assemblies (UnityEngine.dll) to all assemblies
                    if (!unityAssemblyEditorOnly && !moduleExcludedForRuntimeCode)
                    {
                        if (IsPrecompiledAssemblyCompatibleWithBuildTarget(unityAssembly, scriptAssembly.BuildTarget))
                            references.Add(unityAssembly);
                    }
                }
            }

            // UserOverride assemblies should not have a dependency on Editor assemblies.
            if (isOverridingUnityAssembly && !assemblyEditorOnly)
                return references.Where(assembly => !Path.GetFileName(assembly.Path).Contains("UnityEditor")).ToArray();

            return references.ToArray();
        }

        public static List<string> GetPrecompiledReferences(ScriptAssembly scriptAssembly, TargetAssemblyType targetAssemblyType, EditorScriptCompilationOptions options, EditorCompatibility editorCompatibility, IEnumerable<PrecompiledAssembly> implicitPrecompiledAssemblies, IEnumerable<PrecompiledAssembly> explicitPrecompiledAssemblies, ICompilationSetupWarningTracker warningSink)
        {
            var references = new List<string>();

            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool assemblyEditorOnly = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            bool isCustomAssembly = (targetAssemblyType & TargetAssemblyType.Custom) == TargetAssemblyType.Custom;

            void AddReferenceIfMatchBuildTargetAndEditorFlag(PrecompiledAssembly precompiledAssembly, bool explicitReference)
            {
                bool compiledAssemblyEditorOnly = (precompiledAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                // Add all pre-compiled runtime assemblies as references to all script assemblies. Don't add pre-compiled editor assemblies as dependencies to runtime assemblies.
                if (!compiledAssemblyEditorOnly || assemblyEditorOnly || (isCustomAssembly && buildingForEditor && editorCompatibility == EditorCompatibility.CompatibleWithEditor))
                {
                    if (IsPrecompiledAssemblyCompatibleWithBuildTarget(precompiledAssembly, scriptAssembly.BuildTarget))
                    {
                        references.Add(precompiledAssembly.Path);
                    }
                    // we don't warn on build target mismatch, as this is actually a common pattern (an asmdef with multiple references to different "target-specific" assemblies with the same symbols - e.g. foo.XboxOne.dll, foo.PS5.dll, foo.WebGL.dll)
                }
                else if (explicitReference && !string.IsNullOrEmpty(scriptAssembly.AsmDefPath))
                {
                    warningSink?.AddAssetWarning(scriptAssembly.AsmDefPath, $"{scriptAssembly.Filename}: can't add reference to {precompiledAssembly.Path} as it is an editor-only assembly");
                }
            }

            if (implicitPrecompiledAssemblies != null)
            {
                foreach (var precompiledAssembly in implicitPrecompiledAssemblies)
                {
                    AddReferenceIfMatchBuildTargetAndEditorFlag(precompiledAssembly, false);
                }
            }
            if (explicitPrecompiledAssemblies != null)
            {
                foreach (var precompiledAssembly in explicitPrecompiledAssemblies)
                {
                    AddReferenceIfMatchBuildTargetAndEditorFlag(precompiledAssembly, true);
                }
            }

            return references;
        }

        public static List<string> GetCompiledCustomAssembliesReferences(ScriptAssembly scriptAssembly, IDictionary<string, TargetAssembly> customTargetAssemblies, string outputDirectory)
        {
            var references = new List<string>();

            if (customTargetAssemblies != null)
            {
                foreach (var entry in customTargetAssemblies)
                {
                    var customTargetAssembly = entry.Value;
                    var customTargetAssemblyPath = customTargetAssembly.FullPath(outputDirectory);

                    // File might not exist if there are no scripts in the custom target assembly folder.
                    if (AssetPath.Exists(customTargetAssemblyPath))
                        references.Add(customTargetAssemblyPath);
                }
            }

            return references;
        }

        static bool IsCompatibleWithEditor(ScriptAssemblySettings settings)
        {
            return (settings.CompilationOptions & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
        }

        public static bool IsCompatibleWithPlatformAndDefines(TargetAssembly assembly, ScriptAssemblySettings settings)
        {
            return assembly.IsCompatibleFunc == null || assembly.IsCompatibleFunc(settings, assembly.Defines);
        }

        public static bool IsCompatibleWithPlatformAndDefines(TargetAssembly assembly, BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            var settings = new ScriptAssemblySettings
            {
                BuildTarget = buildTarget,
                CompilationOptions = options
            };

            return IsCompatibleWithPlatformAndDefines(assembly, settings);
        }

        internal static Dictionary<string, TargetAssembly> CreatePredefinedTargetAssemblies()
        {
            var runtimeFirstPassAssemblies = new List<TargetAssembly>();
            var runtimeAssemblies = new List<TargetAssembly>();
            var editorFirstPassAssemblies = new List<TargetAssembly>();
            var editorAssemblies = new List<TargetAssembly>();

            var assemblies = new List<TargetAssembly>();

            var scriptCompilerOptions = new ScriptCompilerOptions();

            // Initialize predefined assembly targets
            {
                const string languageName = "CSharp";

                var runtimeFirstPass = new TargetAssembly("Assembly-" + languageName + "-firstpass" + ".dll",
                    AssemblyFlags.FirstPass | AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    FilterAssemblyInFirstpassFolder,
                    null,
                    scriptCompilerOptions);

                var runtime = new TargetAssembly("Assembly-" + languageName + ".dll",
                    AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    null,
                    null,
                    scriptCompilerOptions);

                var editorFirstPass = new TargetAssembly("Assembly-" + languageName + "-Editor-firstpass" + ".dll",
                    AssemblyFlags.EditorOnly | AssemblyFlags.FirstPass | AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    FilterAssemblyInFirstpassEditorFolder,
                    (settings, defines) => IsCompatibleWithEditor(settings),
                    scriptCompilerOptions);

                var editor = new TargetAssembly("Assembly-" + languageName + "-Editor" + ".dll",
                    AssemblyFlags.EditorOnly | AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    FilterAssemblyInEditorFolder,
                    (settings, defines) => IsCompatibleWithEditor(settings),
                    scriptCompilerOptions);

                runtimeFirstPassAssemblies.Add(runtimeFirstPass);
                runtimeAssemblies.Add(runtime);
                editorFirstPassAssemblies.Add(editorFirstPass);
                editorAssemblies.Add(editor);

                assemblies.Add(runtimeFirstPass);
                assemblies.Add(runtime);
                assemblies.Add(editorFirstPass);
                assemblies.Add(editor);
            }

            // Setup dependencies

            // Runtime assemblies depend all first pass runtime assemblies
            foreach (var assembly in runtimeAssemblies)
                assembly.References.AddRange(runtimeFirstPassAssemblies);

            // First pass editor assemblies depend on all first pass runtime assemblies
            foreach (var assembly in editorFirstPassAssemblies)
                assembly.References.AddRange(runtimeFirstPassAssemblies);

            // Editor assemblies depend on all previous runtime and editor assemblies
            foreach (var assembly in editorAssemblies)
            {
                assembly.References.AddRange(runtimeFirstPassAssemblies);
                assembly.References.AddRange(runtimeAssemblies);
                assembly.References.AddRange(editorFirstPassAssemblies);
            }

            return assemblies.ToDictionary(x => x.Filename);
        }

        internal static TargetAssembly[] GetTargetAssembliesWithScripts(
            Dictionary<string, string> allScripts,
            string projectDirectory,
            IDictionary<string, TargetAssembly> customTargetAssemblies,
            ScriptAssemblySettings settings)
        {
            return GetTargetAssembliesWithScriptsHashSet(allScripts, projectDirectory, customTargetAssemblies, settings).ToArray();
        }

        internal static HashSet<TargetAssembly> GetTargetAssembliesWithScriptsHashSet(
            Dictionary<string, string> allScripts,
            string projectDirectory,
            IDictionary<string, TargetAssembly> customTargetAssemblies,
            ScriptAssemblySettings settings)
        {
            var uniqueTargetAssemblies = new HashSet<TargetAssembly>();

            foreach (var entry in allScripts)
            {
                var script = entry.Key;
                var assemblyName = entry.Value;
                var targetAssembly = GetTargetAssembly(script, assemblyName, projectDirectory, customTargetAssemblies);

                // This can happen for scripts in packages that are not included in an .asmdef assembly
                // and they will therefore not be compiled.
                if (targetAssembly == null)
                    continue;

                if (!IsCompatibleWithPlatformAndDefines(targetAssembly, settings))
                    continue;

                uniqueTargetAssemblies.Add(targetAssembly);
            }

            return uniqueTargetAssemblies;
        }

        internal static TargetAssembly GetTargetAssembly(string scriptPath, string assemblyName, string projectDirectory, IDictionary<string, TargetAssembly> customTargetAssemblies)
        {
            TargetAssembly resultAssembly;

            if (assemblyName != null &&
                customTargetAssemblies != null &&
                customTargetAssemblies.Count > 0 &&
                customTargetAssemblies.TryGetValue(assemblyName, out resultAssembly))
            {
                return resultAssembly;
            }

            return GetPredefinedTargetAssembly(scriptPath, assemblyName);
        }

        static string ScriptPathToLowerCase(string scriptPath)
        {
            int length = scriptPath.Length;

            var chars = new char[length + 1];

            chars[0] = '/';

            for (int i = 0; i < length; ++i)
            {
                char stringChar = scriptPath[i];

                if (stringChar == '\\')
                    chars[i + 1] = AssetPath.Separator;
                else
                    chars[i + 1] = Utility.FastToLower(stringChar);
            }

            return new string(chars);
        }

        internal static TargetAssembly GetPredefinedTargetAssembly(string scriptPath, string assemblyName = null)
        {
            if (assemblyName == null)
            {
                TargetAssembly resultAssembly = null;
                var lowerPath = ScriptPathToLowerCase(scriptPath);
                int highestPathDepth = -1;
                foreach (var assembly in predefinedTargetAssemblies.Values)
                {
                    var pathDepth = assembly.PathFilter?.Invoke(lowerPath) ?? 0;
                     if (pathDepth <= highestPathDepth)

                    {
                     continue;

                    }
                    resultAssembly = assembly;
                    highestPathDepth = pathDepth;
                }
                return resultAssembly;
            }

            if (predefinedTargetAssemblies.TryGetValue(assemblyName, out var predefined))
            {
                return predefined;
            }
            return null;
        }

        static int FilterAssemblyInFirstpassFolder(string pathName)
        {
            int result;

            result = FilterAssemblyPathBeginsWith(pathName, "/assets/plugins/");
            if (result >= 0) return result;

            result = FilterAssemblyPathBeginsWith(pathName, "/assets/standard assets/");
            if (result >= 0) return result;

            result = FilterAssemblyPathBeginsWith(pathName, "/assets/pro standard assets/");
            if (result >= 0) return result;

            result = FilterAssemblyPathBeginsWith(pathName, "/assets/iphone standard assets/");
            if (result >= 0) return result;

            return -1;
        }

        static int FilterAssemblyInFirstpassEditorFolder(string pathName)
        {
            int baseIndex = FilterAssemblyInFirstpassFolder(pathName);
            if (baseIndex == -1) return -1;

            return FilterAssemblyInEditorFolder(pathName);
        }

        static int FilterAssemblyInEditorFolder(string pathName)
        {
            const string editorSegment = "/editor/";
            int editorSegmentIndex = pathName.IndexOf(editorSegment, StringComparison.InvariantCulture);
            if (editorSegmentIndex == -1) return -1;

            return editorSegmentIndex + editorSegment.Length;
        }

        static int FilterAssemblyPathBeginsWith(string pathName, string lowerPrefix)
        {
            return Utility.FastStartsWith(pathName, lowerPrefix, lowerPrefix) ? lowerPrefix.Length : -1;
        }
    }
}
