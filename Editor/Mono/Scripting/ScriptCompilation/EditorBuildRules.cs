// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Do not add "using System.IO", use AssetPath methods instead
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Profiling;
using UnityEditor.Compilation;
using UnityEditor.Scripting.Compilers;

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
        }

        public class GenerateChangedScriptAssembliesArgs
        {
            public Dictionary<string, string> AllSourceFiles { get; set; }
            public Dictionary<string, string> DirtySourceFiles { get; set; }
            public IEnumerable<TargetAssembly> DirtyTargetAssemblies { get; set; }
            public IEnumerable<string> DirtyPrecompiledAssemblies { get; set; }
            public string ProjectDirectory { get; set; }
            public ScriptAssemblySettings Settings { get; set; }
            public CompilationAssemblies Assemblies { get; set; }
            public HashSet<string> RunUpdaterAssemblies { get; set; }
            public HashSet<TargetAssembly> NotCompiledTargetAssemblies { get; set; }
            public TargetAssembly[] NoScriptsCustomTargetAssemblies { get; set; }
            public HashSet<string> NotCompiledScripts { get; set; }

            public GenerateChangedScriptAssembliesArgs()
            {
                NotCompiledTargetAssemblies = new HashSet<TargetAssembly>();
                NoScriptsCustomTargetAssemblies = new TargetAssembly[0];
                NotCompiledScripts = new HashSet<string>();
            }
        }

        static readonly TargetAssembly[] predefinedTargetAssemblies;

        static EditorBuildRules()
        {
            predefinedTargetAssemblies = CreatePredefinedTargetAssemblies();
        }

        public static TargetAssembly[] GetPredefinedTargetAssemblies()
        {
            return predefinedTargetAssemblies;
        }

        public static PrecompiledAssembly CreateUserCompiledAssembly(string path)
        {
            var flags = AssemblyFlags.None;

            var lowerPath = Utility.FastToLower(path);
            if (lowerPath.Contains("/editor/") || lowerPath.Contains(@"\editor\"))
                flags |= AssemblyFlags.EditorOnly;

            return new PrecompiledAssembly
            {
                Path = path,
                Flags = flags
            };
        }

        public static PrecompiledAssembly CreateEditorCompiledAssembly(string path)
        {
            return new PrecompiledAssembly
            {
                Path = path,
                Flags = AssemblyFlags.EditorOnly
            };
        }

        public static string[] PredefinedTargetAssemblyNames
        {
            get { return predefinedTargetAssemblies.Select(a => AssetPath.GetAssemblyNameWithoutExtension(a.Filename)).ToArray(); }
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
                    null,
                    customAssembly.AssemblyFlags,
                    TargetAssemblyType.Custom,
                    customAssembly.PathPrefix,
                    customAssembly.AdditionalPrefixes,
                    path => PathFilter(path, customAssembly.PathPrefix, lowerPathPrefix, customAssembly.AdditionalPrefixes, lowerAdditionalPathPrefixes),
                    (settings, defines) => customAssembly.IsCompatibleWith(settings.BuildTarget, settings.CompilationOptions, defines),
                    customAssembly.CompilerOptions)
                {
                    ExplicitPrecompiledReferences = customAssembly.PrecompiledReferences?.ToList() ?? new List<string>(),
                    VersionDefines = customAssembly.VersionDefines != null
                        ? customAssembly.VersionDefines.ToList() : new List<VersionDefine>(),
                    RootNamespace = customAssembly.RootNamespace
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

        public static ScriptAssembly[] GetAllScriptAssemblies(Dictionary<string, string> allSourceFiles,
            String projectDirectory, ScriptAssemblySettings settings, CompilationAssemblies assemblies,
            HashSet<String> runUpdaterAssemblies, TargetAssemblyType onlyIncludeType = TargetAssemblyType.Undefined)
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

                // Optionally only include specific TargetAssemblyType assemblies.
                if (onlyIncludeType != TargetAssemblyType.Undefined && targetAssembly.Type != onlyIncludeType)
                    continue;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(scriptFile);
                var scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);

                if (targetAssembly.Language == null && targetAssembly.Type == TargetAssemblyType.Custom)
                    targetAssembly.Language = scriptLanguage;

                DirtyTargetAssembly dirtyTargetAssembly;

                if (!targetAssemblyFiles.TryGetValue(targetAssembly, out dirtyTargetAssembly))
                {
                    dirtyTargetAssembly = new DirtyTargetAssembly(DirtySource.None);
                    targetAssemblyFiles[targetAssembly] = dirtyTargetAssembly;
                }

                dirtyTargetAssembly.SourceFiles.Add(AssetPath.Combine(projectDirectory, scriptFile));
            }

            return ToScriptAssemblies(targetAssemblyFiles, settings, assemblies, runUpdaterAssemblies);
        }

        internal class DirtyTargetAssembly
        {
            public DirtyTargetAssembly(DirtySource dirtySource)
            {
                SourceFiles = new HashSet<string>();
                DirtySource = dirtySource;
            }

            public HashSet<string> SourceFiles { get; set; }
            public DirtySource DirtySource { get; set; }
        }

        public static ScriptAssembly[] GenerateChangedScriptAssemblies(GenerateChangedScriptAssembliesArgs args)
        {
            var dirtyTargetAssemblies = new Dictionary<TargetAssembly, DirtyTargetAssembly>();

            // Add initial dirty target assemblies
            foreach (var dirtyTargetAssembly in args.DirtyTargetAssemblies)
            {
                if (!IsCompatibleWithPlatformAndDefines(dirtyTargetAssembly, args.Settings))
                    continue;

                dirtyTargetAssemblies[dirtyTargetAssembly] = new DirtyTargetAssembly(DirtySource.DirtyAssembly);
            }

            // Dirty custom script assemblies that have explicit references to
            // explicitly referenced dirty precompiled assemblies.
            if (args.Assemblies.CustomTargetAssemblies != null)
            {
                foreach (var dirtyPrecompiledAssembly in args.DirtyPrecompiledAssemblies)
                {
                    var customTargetAssembliesWithExplictReferences = args.Assemblies.CustomTargetAssemblies.Where(a => (a.Value.Flags & AssemblyFlags.ExplicitReferences) == AssemblyFlags.ExplicitReferences);

                    foreach (var entry in customTargetAssembliesWithExplictReferences)
                    {
                        var customTargetAssembly = entry.Value;
                        if (customTargetAssembly.ExplicitPrecompiledReferences.Contains(dirtyPrecompiledAssembly))
                        {
                            dirtyTargetAssemblies[customTargetAssembly] = new DirtyTargetAssembly(DirtySource.DirtyReference);
                            break;
                        }
                    }
                }
            }

            var allTargetAssemblies = args.Assemblies.CustomTargetAssemblies == null ?
                predefinedTargetAssemblies :
                predefinedTargetAssemblies.Concat(args.Assemblies.CustomTargetAssemblies.Values).ToArray();

            // Mark all assemblies that the script updater must be run on as dirty.
            if (args.RunUpdaterAssemblies != null)
                foreach (var assemblyFilename in args.RunUpdaterAssemblies)
                {
                    var targetAssembly = allTargetAssemblies.First(a => a.Filename == assemblyFilename);
                    dirtyTargetAssemblies[targetAssembly] = new DirtyTargetAssembly(DirtySource.DirtyAssembly);
                }

            // Collect all dirty TargetAssemblies
            foreach (var entry in args.DirtySourceFiles)
            {
                var dirtySourceFile = entry.Key;
                var assemblyName = entry.Value;
                var targetAssembly = GetTargetAssembly(dirtySourceFile, assemblyName, args.ProjectDirectory, args.Assemblies.CustomTargetAssemblies);

                if (targetAssembly == null)
                {
                    args.NotCompiledScripts.Add(dirtySourceFile);
                    continue;
                }

                if (!IsCompatibleWithPlatformAndDefines(targetAssembly, args.Settings))
                    continue;

                DirtyTargetAssembly dirtyTargetAssembly;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(dirtySourceFile);
                SupportedLanguage scriptLanguage = null;

                try
                {
                    scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);
                }
                catch (Exception e)
                {
                    // UnityScript/Boo support has been disabled but not removed,
                    // so we log the exception and skip the source file.
                    UnityEngine.Debug.Log(e);
                    continue;
                }

                if (!dirtyTargetAssemblies.TryGetValue(targetAssembly, out dirtyTargetAssembly))
                {
                    dirtyTargetAssembly = new DirtyTargetAssembly(DirtySource.DirtyScript);
                    dirtyTargetAssemblies[targetAssembly] = dirtyTargetAssembly;

                    if (targetAssembly.Type == TargetAssemblyType.Custom)
                        targetAssembly.Language = scriptLanguage;
                }

                dirtyTargetAssembly.SourceFiles.Add(AssetPath.Combine(args.ProjectDirectory, dirtySourceFile));

                if (targetAssembly.Language == null && targetAssembly.Type == TargetAssemblyType.Custom)
                    targetAssembly.Language = scriptLanguage;

                // If there are mixed languages in a custom script folder, mark the assembly to not be compiled.
                if (scriptLanguage != targetAssembly.Language)
                    args.NotCompiledTargetAssemblies.Add(targetAssembly);
            }

            bool isAnyCustomScriptAssemblyDirty = dirtyTargetAssemblies.Any(entry =>
            {
                var targetAssembly = entry.Key;
                bool isCustomScriptAssembly = targetAssembly.Type == TargetAssemblyType.Custom;
                bool isExplicitlyReferenced = (targetAssembly.Flags & AssemblyFlags.ExplicitlyReferenced) == AssemblyFlags.ExplicitlyReferenced;
                return isCustomScriptAssembly && !isExplicitlyReferenced;
            });

            // If we have any dirty custom target assemblies, then the predefined target assemblies are marked as dirty,
            // as the predefined assemblies always reference the custom script assemblies.
            if (isAnyCustomScriptAssemblyDirty)
            {
                foreach (var assembly in predefinedTargetAssemblies)
                {
                    if (!IsCompatibleWithPlatformAndDefines(assembly, args.Settings))
                        continue;

                    if (!dirtyTargetAssemblies.ContainsKey(assembly))
                        dirtyTargetAssemblies[assembly] = new DirtyTargetAssembly(DirtySource.DirtyReference);
                }
            }

            // Return empty array in case of no dirty target assemblies
            if (dirtyTargetAssemblies.Count == 0)
                return new ScriptAssembly[0];

            // Collect any TargetAssemblies that reference the dirty TargetAssemblies, as they will also be dirty.
            int dirtyAssemblyCount;

            do
            {
                dirtyAssemblyCount = 0;

                foreach (var assembly in allTargetAssemblies)
                {
                    if (!IsCompatibleWithPlatformAndDefines(assembly, args.Settings))
                        continue;

                    // If already dirty, skip.
                    if (dirtyTargetAssemblies.ContainsKey(assembly))
                        continue;

                    foreach (var reference in assembly.References)
                        if (dirtyTargetAssemblies.ContainsKey(reference))
                        {
                            dirtyTargetAssemblies[assembly] = new DirtyTargetAssembly(DirtySource.DirtyReference);
                            dirtyAssemblyCount++;
                            break;
                        }
                }
            }
            while (dirtyAssemblyCount > 0);

            // Add any non-dirty source files that belong to dirty TargetAssemblies
            foreach (var entry in args.AllSourceFiles)
            {
                var sourceFile = entry.Key;
                var assemblyName = entry.Value;
                var targetAssembly = GetTargetAssembly(sourceFile, assemblyName, args.ProjectDirectory, args.Assemblies.CustomTargetAssemblies);

                if (targetAssembly == null)
                {
                    args.NotCompiledScripts.Add(sourceFile);
                    continue;
                }

                if (!IsCompatibleWithPlatformAndDefines(targetAssembly, args.Settings))
                    continue;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(sourceFile);
                var scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);

                if (targetAssembly.Language == null && targetAssembly.Type == TargetAssemblyType.Custom)
                    targetAssembly.Language = scriptLanguage;

                // If there are mixed languages in a custom script folder, mark the assembly to not be compiled.
                if (scriptLanguage != targetAssembly.Language)
                    args.NotCompiledTargetAssemblies.Add(targetAssembly);

                DirtyTargetAssembly dirtyTargetAssembly;
                if (dirtyTargetAssemblies.TryGetValue(targetAssembly, out dirtyTargetAssembly))
                    dirtyTargetAssembly.SourceFiles.Add(AssetPath.Combine(args.ProjectDirectory, sourceFile));
            }

            // Remove any target assemblies which have no source files associated with them.
            var noScriptsCustomTargetAssemblies = new List<TargetAssembly>();

            foreach (var entry in dirtyTargetAssemblies)
            {
                if (entry.Value.SourceFiles.Count == 0 && entry.Key.Type == TargetAssemblyType.Custom)
                {
                    noScriptsCustomTargetAssemblies.Add(entry.Key);
                }
            }

            args.NoScriptsCustomTargetAssemblies = noScriptsCustomTargetAssemblies.ToArray();

            dirtyTargetAssemblies = dirtyTargetAssemblies.Where(e => e.Value.SourceFiles.Count > 0).ToDictionary(e => e.Key, e => e.Value);

            // Remove any target assemblies which have been marked as do not compile.
            foreach (var removeAssembly in args.NotCompiledTargetAssemblies)
                dirtyTargetAssemblies.Remove(removeAssembly);

            // Convert TargetAssemblies to ScriptAssembiles
            var scriptAssemblies = ToScriptAssemblies(dirtyTargetAssemblies, args.Settings, args.Assemblies, args.RunUpdaterAssemblies);
            return scriptAssemblies;
        }

        internal static ScriptAssembly[] ToScriptAssemblies(IDictionary<TargetAssembly, DirtyTargetAssembly> targetAssemblies, ScriptAssemblySettings settings,
            CompilationAssemblies assemblies, HashSet<string> runUpdaterAssemblies)
        {
            var scriptAssemblies = new ScriptAssembly[targetAssemblies.Count];

            var targetToScriptAssembly = new Dictionary<TargetAssembly, ScriptAssembly>();
            int index = 0;

            bool buildingForEditor = settings.BuildingForEditor;
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
                scriptAssembly.Language = targetAssembly.Language;
                scriptAssembly.OriginPath = targetAssembly.PathPrefix;
                scriptAssembly.Filename = targetAssembly.Filename;
                scriptAssembly.RootNamespace = targetAssembly.Type == TargetAssemblyType.Predefined ? settings.ProjectRootNamespace : targetAssembly.RootNamespace;
                scriptAssembly.DirtySource = dirtyTargetAssembly.DirtySource;

                if (runUpdaterAssemblies != null && runUpdaterAssemblies.Contains(scriptAssembly.Filename))
                    scriptAssembly.CallOnBeforeCompilationStarted = true;

                var compilerDefines = scriptAssembly.Language.GetCompilerDefines();

                scriptAssembly.OutputDirectory = settings.OutputDirectory;
                scriptAssembly.Defines = targetAssembly.Defines == null ? compilerDefines : targetAssembly.Defines.Concat(compilerDefines).ToArray();
                scriptAssembly.Files = dirtyTargetAssembly.SourceFiles.ToArray();

                if (targetAssembly.Type == TargetAssemblyType.Predefined)
                    scriptAssembly.CompilerOptions = settings.PredefinedAssembliesCompilerOptions;
                else
                    scriptAssembly.CompilerOptions = targetAssembly.CompilerOptions;

                var editorOnlyTargetAssembly = (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                if (editorOnlyTargetAssembly || buildingForEditor && settings.PredefinedAssembliesCompilerOptions.ApiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6)
                    scriptAssembly.CompilerOptions.ApiCompatibilityLevel = ApiCompatibilityLevel.NET_4_6;
                else
                    scriptAssembly.CompilerOptions.ApiCompatibilityLevel = settings.PredefinedAssembliesCompilerOptions.ApiCompatibilityLevel;

                if (buildingForEditor && (settings.CompilationOptions & EditorScriptCompilationOptions.BuildingUseReferenceAssemblies) > 0)
                    scriptAssembly.CompilerOptions.EmitReferenceAssembly = true;

                if ((settings.CompilationOptions &
                     EditorScriptCompilationOptions.BuildingUseDeterministicCompilation) ==
                    EditorScriptCompilationOptions.BuildingUseDeterministicCompilation)
                {
                    scriptAssembly.CompilerOptions.UseDeterministicCompilation = true;
                }

                scriptAssembly.CompilerOptions.CodeOptimization = (buildingForEditor
                    && settings.EditorCodeOptimization == CodeOptimization.Release
                    || !buildingForEditor && !settings.BuildingDevelopmentBuild)
                    ? CodeOptimization.Release : CodeOptimization.Debug;
            }

            // Don't add the auto-referenced engine assemblies if the assembly either has the flag set, or
            // is a codegen assembly
            AutoReferencedPackageAssemblies.AddReferences(assemblies.CustomTargetAssemblies, settings.CompilationOptions,
                t => !(((t.Flags & AssemblyFlags.NoEngineReferences) == AssemblyFlags.NoEngineReferences) ||
                    UnityCodeGenHelpers.IsCodeGen(t.Filename)));

            // Setup ScriptAssembly references
            index = 0;
            foreach (var entry in targetAssemblies)
            {
                var scriptAssembly = scriptAssemblies[index++];
                AddScriptAssemblyReferences(ref scriptAssembly, entry.Key, settings,
                    assemblies, targetToScriptAssembly);

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
            IDictionary<TargetAssembly, ScriptAssembly> targetToScriptAssembly)
        {
            var scriptAssemblyReferences = new List<ScriptAssembly>(targetAssembly.References.Count);
            var references = new List<string>();
            bool buildingForEditor = settings.BuildingForEditor;
            bool noEngineReferences = (targetAssembly.Flags & AssemblyFlags.NoEngineReferences) == AssemblyFlags.NoEngineReferences;

            bool shouldProcessPredefinedCustomTargets = assemblies.CustomTargetAssemblies != null && (targetAssembly.Type & TargetAssemblyType.Predefined) == TargetAssemblyType.Predefined;
            var predefinedCustomTargetReferences = Enumerable.Empty<TargetAssembly>();
            if (shouldProcessPredefinedCustomTargets && assemblies.PredefinedAssembliesCustomTargetReferences != null)
                predefinedCustomTargetReferences = assemblies.PredefinedAssembliesCustomTargetReferences;

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) references, as long as the target
            // doesn't specify that it doesn't want them.
            if (!noEngineReferences)
            {
                // Add predefined custom target references in a hash-set for fast lookup
                var predefinedCustomTargetRefs = new HashSet<string>(predefinedCustomTargetReferences.Select(x => x.Filename));
                var unityReferences = GetUnityReferences(scriptAssembly, targetAssembly, assemblies.UnityAssemblies, predefinedCustomTargetRefs, settings.CompilationOptions, UnityReferencesOptions.None);
                references.AddRange(unityReferences);
            }

            AddTestRunnerCustomReferences(ref targetAssembly, assemblies.CustomTargetAssemblies);

            // Setup target assembly references
            foreach (var reference in targetAssembly.References)
            {
                ScriptAssembly scriptAssemblyReference;

                // Add ScriptAssembly references to other dirty script assemblies that also need to be rebuilt.
                if (targetToScriptAssembly.TryGetValue(reference, out scriptAssemblyReference))
                {
                    System.Diagnostics.Debug.Assert(scriptAssemblyReference != null);
                    scriptAssemblyReferences.Add(scriptAssemblyReference);
                }
                else
                {
                    // Add string references to other assemblies that do not need to be rebuilt.
                    var assemblyPath = reference.FullPath(settings.OutputDirectory);

                    if (AssetPath.Exists(assemblyPath))
                        references.Add(assemblyPath);
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
                    else
                    {
                        var customTargetAssemblyPath = customTargetAssembly.FullPath(settings.OutputDirectory);

                        // File might not exist if there are no scripts in the custom target assembly folder.
                        if (AssetPath.Exists(customTargetAssemblyPath))
                            references.Add(customTargetAssemblyPath);
                    }
                }
            }

            // Add pre-compiled assemblies as references
            var allPrecompiledAssemblies = assemblies.PrecompiledAssemblies ?? new Dictionary<string, PrecompiledAssembly>(0);
            List<PrecompiledAssembly> precompiledReferences = new List<PrecompiledAssembly>(allPrecompiledAssemblies.Count);

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
                        precompiledReferences.Add(assembly);
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

            var precompiledReferenceNames = GetPrecompiledReferences(scriptAssembly, targetAssembly.Type, settings.CompilationOptions, targetAssembly.editorCompatibility, precompiledReferences.ToArray());
            references.AddRange(precompiledReferenceNames);

            if (buildingForEditor && assemblies.EditorAssemblyReferences != null)
                references.AddRange(assemblies.EditorAssemblyReferences);

            references.AddRange(MonoLibraryHelpers.GetSystemLibraryReferences(scriptAssembly.CompilerOptions.ApiCompatibilityLevel, scriptAssembly.Language));

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

        public static List<string> GetUnityReferences(ScriptAssembly scriptAssembly, PrecompiledAssembly[] unityAssemblies, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            return GetUnityReferences(scriptAssembly, null, unityAssemblies, options, unityReferencesOptions);
        }

        public static List<string> GetUnityReferences(ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, PrecompiledAssembly[] unityAssemblies, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            return GetUnityReferences(scriptAssembly, targetAssembly, unityAssemblies, null, options, unityReferencesOptions);
        }

        public static List<string> GetUnityReferences(ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, PrecompiledAssembly[] unityAssemblies, HashSet<string> predefinedCustomTargetReferences, EditorScriptCompilationOptions options, UnityReferencesOptions unityReferencesOptions)
        {
            var references = new List<string>();

            bool assemblyEditorOnly = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool excludeUnityModules = unityReferencesOptions == UnityReferencesOptions.ExcludeModules;

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) referencees.
            if (unityAssemblies == null)
                return references;

            foreach (var unityAssembly in unityAssemblies)
            {
                if ((unityAssembly.Flags & (AssemblyFlags.UserOverride | AssemblyFlags.UserOverrideCandidate)) != AssemblyFlags.None)
                {
                    var unityAssemblyFileName = AssetPath.GetFileName(unityAssembly.Path);

                    // This scriptAssembly is overriding this unityAssembly so it should probably not depend on itself.
                    if (unityAssemblyFileName == scriptAssembly.Filename)
                        continue;

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
                        references.Add(unityAssembly.Path);
                }
                else
                {
                    bool unityAssemblyEditorOnly = (unityAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                    // Add Unity runtime assemblies (UnityEngine.dll) to all assemblies
                    if (!unityAssemblyEditorOnly && !moduleExcludedForRuntimeCode)
                    {
                        if (IsPrecompiledAssemblyCompatibleWithBuildTarget(unityAssembly, scriptAssembly.BuildTarget))
                            references.Add(unityAssembly.Path);
                    }
                }
            }

            return references;
        }

        public static List<string> GetPrecompiledReferences(ScriptAssembly scriptAssembly, TargetAssemblyType targetAssemblyType, EditorScriptCompilationOptions options, EditorCompatibility editorCompatibility, PrecompiledAssembly[] precompiledAssemblies)
        {
            var references = new List<string>();

            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool assemblyEditorOnly = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            bool isCustomAssembly = (targetAssemblyType & TargetAssemblyType.Custom) == TargetAssemblyType.Custom;

            if (precompiledAssemblies != null)
                foreach (var precompiledAssembly in precompiledAssemblies)
                {
                    bool compiledAssemblyEditorOnly = (precompiledAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                    // Add all pre-compiled runtime assemblies as references to all script assemblies. Don't add pre-compiled editor assemblies as dependencies to runtime assemblies.
                    if (!compiledAssemblyEditorOnly || assemblyEditorOnly || (isCustomAssembly && buildingForEditor && editorCompatibility == EditorCompatibility.CompatibleWithEditor))
                    {
                        if (IsPrecompiledAssemblyCompatibleWithBuildTarget(precompiledAssembly, scriptAssembly.BuildTarget))
                            references.Add(precompiledAssembly.Path);
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

        static bool IsCompatibleWithPlatformAndDefines(TargetAssembly assembly, ScriptAssemblySettings settings)
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

        internal static TargetAssembly[] CreatePredefinedTargetAssemblies()
        {
            var runtimeFirstPassAssemblies = new List<TargetAssembly>();
            var runtimeAssemblies = new List<TargetAssembly>();
            var editorFirstPassAssemblies = new List<TargetAssembly>();
            var editorAssemblies = new List<TargetAssembly>();

            var supportedLanguages = ScriptCompilers.SupportedLanguages;
            var assemblies = new List<TargetAssembly>();

            var scriptCompilerOptions = new ScriptCompilerOptions();

            // Initialize predefined assembly targets
            foreach (var language in supportedLanguages)
            {
                var languageName = language.GetLanguageName();

                var runtimeFirstPass = new TargetAssembly("Assembly-" + languageName + "-firstpass" + ".dll",
                    language,
                    AssemblyFlags.FirstPass | AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    FilterAssemblyInFirstpassFolder,
                    null,
                    scriptCompilerOptions);

                var runtime = new TargetAssembly("Assembly-" + languageName + ".dll",
                    language,
                    AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    null,
                    null,
                    scriptCompilerOptions);

                var editorFirstPass = new TargetAssembly("Assembly-" + languageName + "-Editor-firstpass" + ".dll",
                    language,
                    AssemblyFlags.EditorOnly | AssemblyFlags.FirstPass | AssemblyFlags.UserAssembly,
                    TargetAssemblyType.Predefined,
                    null,
                    null,
                    FilterAssemblyInFirstpassEditorFolder,
                    (settings, defines) => IsCompatibleWithEditor(settings),
                    scriptCompilerOptions);

                var editor = new TargetAssembly("Assembly-" + languageName + "-Editor" + ".dll",
                    language,
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

            return assemblies.ToArray();
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

            if (customTargetAssemblies != null &&
                customTargetAssemblies.Count > 0 &&
                customTargetAssemblies.TryGetValue(assemblyName, out resultAssembly))
            {
                return resultAssembly;
            }

            // Do not compile scripts outside the Assets/ folder into predefined assemblies.
            if (!Utility.IsAssetsPath(scriptPath))
                return null;

            return GetPredefinedTargetAssembly(scriptPath);
        }

        internal static TargetAssembly GetTargetAssemblyLinearSearch(string scriptPath, string projectDirectory, IDictionary<string, TargetAssembly> customTargetAssemblies)
        {
            TargetAssembly resultAssembly = GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);

            if (resultAssembly != null)
                return resultAssembly;

            // Do not compile scripts outside the Assets/ folder into predefined assemblies.
            if (!Utility.IsAssetsPath(scriptPath))
                return null;

            return GetPredefinedTargetAssembly(scriptPath);
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

        internal static TargetAssembly GetPredefinedTargetAssembly(string scriptPath)
        {
            TargetAssembly resultAssembly = null;

            var lowerPath = ScriptPathToLowerCase(scriptPath);
            int highestPathDepth = -1;

            foreach (var assembly in predefinedTargetAssemblies)
            {
                var pathFilter = assembly.PathFilter;
                int pathDepth = -1;

                if (pathFilter == null)
                    pathDepth = 0;
                else
                    pathDepth = pathFilter(lowerPath);

                if (pathDepth > highestPathDepth)
                {
                    resultAssembly = assembly;
                    highestPathDepth = pathDepth;
                }
            }

            return resultAssembly;
        }

        internal static TargetAssembly GetCustomTargetAssembly(string scriptPath, string projectDirectory, IDictionary<string, TargetAssembly> customTargetAssemblies)
        {
            if (customTargetAssemblies == null)
                return null;

            int highestPathDepth = -1;
            TargetAssembly resultAssembly = null;

            // CustomScriptAssembly paths are absolute, so we convert the scriptPath to an absolute path, if necessary.
            bool isPathAbsolute = AssetPath.IsPathRooted(scriptPath);
            var fullPath = isPathAbsolute ? AssetPath.GetFullPath(scriptPath) : AssetPath.Combine(projectDirectory, scriptPath);

            foreach (var entry in customTargetAssemblies)
            {
                var assembly = entry.Value;

                if (assembly.MaxPathLength <= highestPathDepth)
                    continue;

                int pathDepth = assembly.PathFilter(fullPath);

                if (pathDepth <= highestPathDepth)
                    continue;

                resultAssembly = assembly;
                highestPathDepth = pathDepth;
            }

            return resultAssembly;
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
