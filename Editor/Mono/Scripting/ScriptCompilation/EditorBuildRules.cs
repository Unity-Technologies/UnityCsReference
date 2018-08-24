// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using File = System.IO.File;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // This class is intentionally immutable, avoid adding mutable state to it.
    static class EditorBuildRules
    {
        [Flags]
        internal enum TargetAssemblyType
        {
            Undefined = 0,
            Predefined = 1,
            Custom = 2
        }

        internal enum EditorCompatibility
        {
            NotCompatibleWithEditor = 0,
            CompatibleWithEditor = 1
        }

        internal class TargetAssembly
        {
            public string Filename { get; private set; }
            public SupportedLanguage Language { get; set; }
            public AssemblyFlags Flags { get; private set; }
            public string PathPrefix { get; private set; }
            public Func<string, int> PathFilter { get; private set; }
            public Func<BuildTarget, EditorScriptCompilationOptions, bool> IsCompatibleFunc { get; private set; }
            public List<TargetAssembly> References { get; private set; }
            public TargetAssemblyType Type { get; private set; }

            public TargetAssembly()
            {
                References = new List<TargetAssembly>();
            }

            public TargetAssembly(string name,
                                  SupportedLanguage language,
                                  AssemblyFlags flags,
                                  TargetAssemblyType type,
                                  string pathPrefix,
                                  Func<string, int> pathFilter,
                                  Func<BuildTarget, EditorScriptCompilationOptions, bool> compatFunc) : this()
            {
                Language = language;
                Filename = name;
                Flags = flags;
                PathPrefix = pathPrefix;
                PathFilter = pathFilter;
                IsCompatibleFunc = compatFunc;
                Type = type;
            }

            public string FilenameWithSuffix(string filenameSuffix)
            {
                if (!string.IsNullOrEmpty(filenameSuffix))
                    return Filename.Replace(".dll", filenameSuffix + ".dll");

                return Filename;
            }

            public string FullPath(string outputDirectory, string filenameSuffix)
            {
                return AssetPath.Combine(outputDirectory, FilenameWithSuffix(filenameSuffix));
            }

            public EditorCompatibility editorCompatibility
            {
                get
                {
                    bool isCompatibleWithEditor = IsCompatibleFunc == null ||
                        IsCompatibleFunc(BuildTarget.NoTarget, EditorScriptCompilationOptions.BuildingForEditor);

                    return isCompatibleWithEditor
                        ? EditorCompatibility.CompatibleWithEditor
                        : EditorCompatibility.NotCompatibleWithEditor;
                }
            }
        }

        public class CompilationAssemblies
        {
            public PrecompiledAssembly[] UnityAssemblies { get; set; }
            public PrecompiledAssembly[] PrecompiledAssemblies { get; set; }
            public TargetAssembly[] CustomTargetAssemblies { get; set; }
            public string[] EditorAssemblyReferences { get; set; }
        }

        public class GenerateChangedScriptAssembliesArgs
        {
            public IEnumerable<string> AllSourceFiles { get; set; }
            public IEnumerable<string> DirtySourceFiles { get; set; }
            public string ProjectDirectory { get; set; }
            public ScriptAssemblySettings Settings { get; set; }
            public CompilationAssemblies Assemblies { get; set; }
            public HashSet<string> RunUpdaterAssemblies { get; set; }
            public HashSet<TargetAssembly> NotCompiledTargetAssemblies { get; set; }

            public GenerateChangedScriptAssembliesArgs()
            {
                NotCompiledTargetAssemblies = new HashSet<TargetAssembly>();
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

            var lowerPath = path.ToLower();
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

        internal static bool FastStartsWith(string str, string prefix, string prefixLowercase)
        {
            int strLength = str.Length;
            int prefixLength = prefix.Length;

            if (prefixLength > strLength)
                return false;

            int lastPrefixCharIndex = prefixLength - 1;

            // Check last char in prefix is equal. Since we are comparing
            // file paths against directory paths, the last char will be '/'.
            if (str[lastPrefixCharIndex] != prefix[lastPrefixCharIndex])
                return false;

            for (int i = 0; i < prefixLength; ++i)
            {
                if (str[i] == prefix[i])
                    continue;

                char strC = char.ToLower(str[i], CultureInfo.InvariantCulture);

                if (strC != prefixLowercase[i])
                    return false;
            }

            return true;
        }

        public static TargetAssembly[] CreateTargetAssemblies(IEnumerable<CustomScriptAssembly> customScriptAssemblies)
        {
            if (customScriptAssemblies == null)
                return null;


            foreach (var customAssembly in customScriptAssemblies)
            {
                if (predefinedTargetAssemblies.Any(p => AssetPath.GetAssemblyNameWithoutExtension(p.Filename) == customAssembly.Name))
                {
                    throw new Exception(string.Format("Assembly cannot be have reserved name '{0}'. Defined in '{1}'", customAssembly.Name, customAssembly.FilePath));
                }
            }

            var targetAssemblies = new List<TargetAssembly>();
            var nameToTargetAssembly = new Dictionary<string, TargetAssembly>();


            // Create TargetAssemblies
            foreach (var customAssembly in customScriptAssemblies)
            {
                var lowerPathPrefix = customAssembly.PathPrefix.ToLower(CultureInfo.InvariantCulture);

                var targetAssembly = new TargetAssembly(customAssembly.Name + ".dll",
                        null,
                        customAssembly.AssemblyFlags,
                        TargetAssemblyType.Custom,
                        customAssembly.PathPrefix,
                        path => FastStartsWith(path, customAssembly.PathPrefix, lowerPathPrefix) ? customAssembly.PathPrefix.Length : -1,
                        (BuildTarget target, EditorScriptCompilationOptions options) => customAssembly.IsCompatibleWith(target, options));

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
                        UnityEngine.Debug.LogWarning(string.Format("Could not find reference '{0}' for assembly '{1}'", reference, customAssembly.Name));
                        continue;
                    }

                    targetAssembly.References.Add(referenceAssembly);
                }
            }

            return targetAssemblies.ToArray();
        }

        public static ScriptAssembly[] GetAllScriptAssemblies(IEnumerable<string> allSourceFiles, string projectDirectory, ScriptAssemblySettings settings, CompilationAssemblies assemblies)
        {
            if (allSourceFiles == null || allSourceFiles.Count() == 0)
                return new ScriptAssembly[0];

            var targetAssemblyFiles = new Dictionary<TargetAssembly, HashSet<string>>();

            foreach (var scriptFile in allSourceFiles)
            {
                var targetAssembly = GetTargetAssembly(scriptFile, projectDirectory, assemblies.CustomTargetAssemblies);

                if (!IsCompatibleWithPlatform(targetAssembly, settings))
                    continue;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(scriptFile);
                var scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);

                if (targetAssembly.Language == null && targetAssembly.Type == TargetAssemblyType.Custom)
                    targetAssembly.Language = scriptLanguage;

                HashSet<string> assemblySourceFiles;

                if (!targetAssemblyFiles.TryGetValue(targetAssembly, out assemblySourceFiles))
                {
                    assemblySourceFiles = new HashSet<string>();
                    targetAssemblyFiles[targetAssembly] = assemblySourceFiles;
                }

                assemblySourceFiles.Add(AssetPath.Combine(projectDirectory, scriptFile));
            }

            return ToScriptAssemblies(targetAssemblyFiles, settings, assemblies, null);
        }

        public static ScriptAssembly[] GenerateChangedScriptAssemblies(GenerateChangedScriptAssembliesArgs args)
        {
            var dirtyTargetAssemblies = new Dictionary<TargetAssembly, HashSet<string>>();

            var allTargetAssemblies = args.Assemblies.CustomTargetAssemblies == null ?
                predefinedTargetAssemblies :
                predefinedTargetAssemblies.Concat(args.Assemblies.CustomTargetAssemblies).ToArray();

            // Mark all assemblies that the script updater must be run on as dirty.
            if (args.RunUpdaterAssemblies != null)
                foreach (var assemblyFilename in args.RunUpdaterAssemblies)
                {
                    var targetAssembly = allTargetAssemblies.First(a => a.FilenameWithSuffix(args.Settings.FilenameSuffix) == assemblyFilename);
                    dirtyTargetAssemblies[targetAssembly] = new HashSet<string>();
                }

            // Collect all dirty TargetAssemblies
            foreach (var dirtySourceFile in args.DirtySourceFiles)
            {
                var targetAssembly = GetTargetAssembly(dirtySourceFile, args.ProjectDirectory, args.Assemblies.CustomTargetAssemblies);

                if (!IsCompatibleWithPlatform(targetAssembly, args.Settings))
                    continue;

                HashSet<string> assemblySourceFiles;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(dirtySourceFile);
                var scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);

                if (!dirtyTargetAssemblies.TryGetValue(targetAssembly, out assemblySourceFiles))
                {
                    assemblySourceFiles = new HashSet<string>();
                    dirtyTargetAssemblies[targetAssembly] = assemblySourceFiles;

                    if (targetAssembly.Type == TargetAssemblyType.Custom)
                        targetAssembly.Language = scriptLanguage;
                }

                assemblySourceFiles.Add(AssetPath.Combine(args.ProjectDirectory, dirtySourceFile));

                // If there are mixed languages in a custom script folder, mark the assembly to not be compiled.
                if (scriptLanguage != targetAssembly.Language)
                    args.NotCompiledTargetAssemblies.Add(targetAssembly);
            }

            bool isAnyCustomScriptAssemblyDirty = dirtyTargetAssemblies.Any(entry => entry.Key.Type == TargetAssemblyType.Custom);

            // If we have any dirty custom target assemblies, then the predefined target assemblies are marked as dirty,
            // as the predefined assemblies always reference the custom script assemblies.
            if (isAnyCustomScriptAssemblyDirty)
            {
                foreach (var assembly in predefinedTargetAssemblies)
                {
                    if (!IsCompatibleWithPlatform(assembly, args.Settings))
                        continue;

                    if (!dirtyTargetAssemblies.ContainsKey(assembly))
                        dirtyTargetAssemblies[assembly] = new HashSet<string>();
                }
            }

            // Collect any TargetAssemblies that reference the dirty TargetAssemblies, as they will also be dirty.
            int dirtyAssemblyCount;

            do
            {
                dirtyAssemblyCount = 0;

                foreach (var assembly in allTargetAssemblies)
                {
                    if (!IsCompatibleWithPlatform(assembly, args.Settings))
                        continue;

                    // If already dirty, skip.
                    if (dirtyTargetAssemblies.ContainsKey(assembly))
                        continue;

                    foreach (var reference in assembly.References)
                        if (dirtyTargetAssemblies.ContainsKey(reference))
                        {
                            dirtyTargetAssemblies[assembly] = new HashSet<string>();
                            dirtyAssemblyCount++;
                            break;
                        }
                }
            }
            while (dirtyAssemblyCount > 0);

            // Add any non-dirty source files that belong to dirty TargetAssemblies
            foreach (var sourceFile in args.AllSourceFiles)
            {
                var targetAssembly = GetTargetAssembly(sourceFile, args.ProjectDirectory, args.Assemblies.CustomTargetAssemblies);

                if (!IsCompatibleWithPlatform(targetAssembly, args.Settings))
                    continue;

                HashSet<string> assemblySourceFiles;

                var scriptExtension = ScriptCompilers.GetExtensionOfSourceFile(sourceFile);
                var scriptLanguage = ScriptCompilers.GetLanguageFromExtension(scriptExtension);

                if (targetAssembly.Language == null && targetAssembly.Type == TargetAssemblyType.Custom)
                    targetAssembly.Language = scriptLanguage;

                // If there are mixed languages in a custom script folder, mark the assembly to not be compiled.
                if (scriptLanguage != targetAssembly.Language)
                    args.NotCompiledTargetAssemblies.Add(targetAssembly);

                if (dirtyTargetAssemblies.TryGetValue(targetAssembly, out assemblySourceFiles))
                    assemblySourceFiles.Add(AssetPath.Combine(args.ProjectDirectory, sourceFile));
            }

            // Remove any target assemblies which have no source files associated with them.
            dirtyTargetAssemblies = dirtyTargetAssemblies.Where(e => e.Value.Count > 0).ToDictionary(e => e.Key, e => e.Value);

            // Remove any target assemblies which have been marked as do not compile.
            foreach (var removeAssembly in args.NotCompiledTargetAssemblies)
                dirtyTargetAssemblies.Remove(removeAssembly);


            // Convert TargetAssemblies to ScriptAssembiles
            var scriptAssemblies = ToScriptAssemblies(dirtyTargetAssemblies, args.Settings, args.Assemblies, args.RunUpdaterAssemblies);
            return scriptAssemblies;
        }

        internal static ScriptAssembly[] ToScriptAssemblies(IDictionary<TargetAssembly, HashSet<string>> targetAssemblies, ScriptAssemblySettings settings,
            CompilationAssemblies assemblies, HashSet<string> runUpdaterAssemblies)
        {
            var scriptAssemblies = new ScriptAssembly[targetAssemblies.Count];

            var targetToScriptAssembly = new Dictionary<TargetAssembly, ScriptAssembly>();
            int index = 0;

            bool buildingForEditor = settings.BuildingForEditor;
            foreach (var entry in targetAssemblies)
            {
                var targetAssembly = entry.Key;
                var sourceFiles = entry.Value;
                var scriptAssembly = new ScriptAssembly();

                // Setup TargetAssembly -> ScriptAssembly mapping for converting references
                scriptAssemblies[index] = scriptAssembly;
                targetToScriptAssembly[targetAssembly] = scriptAssemblies[index++];

                // Setup ScriptAssembly
                scriptAssembly.Flags = targetAssembly.Flags;
                scriptAssembly.BuildTarget = settings.BuildTarget;
                scriptAssembly.Language = targetAssembly.Language;

                var editorOnlyTargetAssembly = (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                if (editorOnlyTargetAssembly || (buildingForEditor && settings.ApiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6))
                    scriptAssembly.ApiCompatibilityLevel = (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest) ? ApiCompatibilityLevel.NET_4_6 : ApiCompatibilityLevel.NET_2_0;
                else
                    scriptAssembly.ApiCompatibilityLevel = settings.ApiCompatibilityLevel;

                if (!string.IsNullOrEmpty(settings.FilenameSuffix))
                {
                    var basename = AssetPath.GetAssemblyNameWithoutExtension(targetAssembly.Filename);
                    scriptAssembly.Filename = string.Concat(basename, settings.FilenameSuffix, ".dll");
                }
                else
                    scriptAssembly.Filename = targetAssembly.Filename;

                if (runUpdaterAssemblies != null && runUpdaterAssemblies.Contains(scriptAssembly.Filename))
                    scriptAssembly.RunUpdater = true;

                scriptAssembly.OutputDirectory = settings.OutputDirectory;
                scriptAssembly.Defines = settings.Defines;
                scriptAssembly.Files = sourceFiles.ToArray();

                // Script files must always be passed in the same order to the compiler.
                // Otherwise player builds might fail for partial classes.
                Array.Sort(scriptAssembly.Files);
            }

            // Setup ScriptAssembly references
            index = 0;
            foreach (var entry in targetAssemblies)
                AddScriptAssemblyReferences(ref scriptAssemblies[index++], entry.Key, settings,
                    assemblies, targetToScriptAssembly, settings.FilenameSuffix);

            return scriptAssemblies;
        }

        static bool IsPrecompiledAssemblyCompatibleWithScriptAssembly(PrecompiledAssembly compiledAssembly, ScriptAssembly scriptAssembly)
        {
            bool useDotNet = WSAHelpers.UseDotNetCore(scriptAssembly);

            if (useDotNet)
            {
                bool compiledAssemblyCompatibleWithDotNet = (compiledAssembly.Flags & AssemblyFlags.UseForDotNet) == AssemblyFlags.UseForDotNet;
                return compiledAssemblyCompatibleWithDotNet;
            }

            bool compiledAssemblyCompatibleWithMono = (compiledAssembly.Flags & AssemblyFlags.UseForMono) == AssemblyFlags.UseForMono;
            return compiledAssemblyCompatibleWithMono;
        }

        internal static void AddScriptAssemblyReferences(ref ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, ScriptAssemblySettings settings,
            CompilationAssemblies assemblies,
            IDictionary<TargetAssembly, ScriptAssembly> targetToScriptAssembly, string filenameSuffix)
        {
            var scriptAssemblyReferences = new List<ScriptAssembly>();
            var references = new List<string>();

            bool buildingForEditor = settings.BuildingForEditor;

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) referencees.
            var unityReferences = GetUnityReferences(scriptAssembly, assemblies.UnityAssemblies, settings.CompilationOptions);
            references.AddRange(unityReferences);

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
                    var assemblyPath = reference.FullPath(settings.OutputDirectory, filenameSuffix);

                    if (File.Exists(assemblyPath))
                        references.Add(assemblyPath);
                }
            }

            // For predefined target assembly add references to custom target assemblies
            if (assemblies.CustomTargetAssemblies != null && (targetAssembly.Type & TargetAssemblyType.Predefined) == TargetAssemblyType.Predefined)
            {
                foreach (var customTargetAssembly in assemblies.CustomTargetAssemblies)
                {
                    ScriptAssembly scriptAssemblyReference;

                    // Only add ScriptAssembly reference if the custom target assembly is dirty, e.g. is in targetToScriptAssembly dictionary
                    // Otherwise just add already compiled custom target assembly as precompiled reference.
                    if (targetToScriptAssembly.TryGetValue(customTargetAssembly, out scriptAssemblyReference))
                        scriptAssemblyReferences.Add(scriptAssemblyReference);
                    else
                    {
                        var customTargetAssemblyPath = customTargetAssembly.FullPath(settings.OutputDirectory, filenameSuffix);

                        // File might not exist if there are no scripts in the custom target assembly folder.
                        if (File.Exists(customTargetAssemblyPath))
                            references.Add(customTargetAssemblyPath);
                    }
                }
            }

            // Add pre-compiled assemblies as references

            var precompiledReferences = GetPrecompiledReferences(scriptAssembly, targetAssembly.Type, settings.CompilationOptions, targetAssembly.editorCompatibility, assemblies.PrecompiledAssemblies);
            references.AddRange(precompiledReferences);

            if (buildingForEditor && assemblies.EditorAssemblyReferences != null)
                references.AddRange(assemblies.EditorAssemblyReferences);

            scriptAssembly.ScriptAssemblyReferences = scriptAssemblyReferences.ToArray();
            scriptAssembly.References = references.ToArray();
        }

        public static List<string> GetUnityReferences(ScriptAssembly scriptAssembly, PrecompiledAssembly[] unityAssemblies, EditorScriptCompilationOptions options)
        {
            var references = new List<string>();

            bool assemblyEditorOnly = (scriptAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor)  == EditorScriptCompilationOptions.BuildingForEditor;

            // Add Unity assemblies (UnityEngine.dll, UnityEditor.dll) referencees.
            if (unityAssemblies != null)
                foreach (var unityAssembly in unityAssemblies)
                {
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
                            if (IsPrecompiledAssemblyCompatibleWithScriptAssembly(unityAssembly, scriptAssembly))
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
                        if (IsPrecompiledAssemblyCompatibleWithScriptAssembly(precompiledAssembly, scriptAssembly))
                            references.Add(precompiledAssembly.Path);
                    }
                }

            return references;
        }

        public static List<string> GetCompiledCustomAssembliesReferences(ScriptAssembly scriptAssembly, TargetAssembly[] customTargetAssemblies, string outputDirectory, string filenameSuffix)
        {
            var references = new List<string>();

            if (customTargetAssemblies != null)
            {
                foreach (var customTargetAssembly in customTargetAssemblies)
                {
                    var customTargetAssemblyPath = customTargetAssembly.FullPath(outputDirectory, filenameSuffix);

                    // File might not exist if there are no scripts in the custom target assembly folder.
                    if (File.Exists(customTargetAssemblyPath))
                        references.Add(customTargetAssemblyPath);
                }
            }

            return references;
        }

        static bool IsCompatibleWithEditor(BuildTarget buildTarget, EditorScriptCompilationOptions options)
        {
            return (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
        }

        static bool IsCompatibleWithPlatform(TargetAssembly assembly, ScriptAssemblySettings settings)
        {
            return assembly.IsCompatibleFunc == null || assembly.IsCompatibleFunc(settings.BuildTarget, settings.CompilationOptions);
        }

        internal static TargetAssembly[] CreatePredefinedTargetAssemblies()
        {
            var runtimeFirstPassAssemblies = new List<TargetAssembly>();
            var runtimeAssemblies = new List<TargetAssembly>();
            var editorFirstPassAssemblies = new List<TargetAssembly>();
            var editorAssemblies = new List<TargetAssembly>();

            var supportedLanguages = ScriptCompilers.SupportedLanguages;
            var assemblies = new List<TargetAssembly>();

            // Initialize predefined assembly targets
            foreach (var language in supportedLanguages)
            {
                var languageName = language.GetLanguageName();

                var runtimeFirstPass = new TargetAssembly("Assembly-" + languageName + "-firstpass" + ".dll",
                        language,
                        AssemblyFlags.FirstPass,
                        TargetAssemblyType.Predefined,
                        null,
                        FilterAssemblyInFirstpassFolder,
                        null);

                var runtime = new TargetAssembly("Assembly-" + languageName + ".dll",
                        language,
                        AssemblyFlags.None,
                        TargetAssemblyType.Predefined,
                        null,
                        null,
                        null);

                var editorFirstPass = new TargetAssembly("Assembly-" + languageName + "-Editor-firstpass" + ".dll",
                        language,
                        AssemblyFlags.EditorOnly | AssemblyFlags.FirstPass,
                        TargetAssemblyType.Predefined,
                        null,
                        FilterAssemblyInFirstpassEditorFolder,
                        IsCompatibleWithEditor);

                var editor = new TargetAssembly("Assembly-" + languageName + "-Editor" + ".dll",
                        language,
                        AssemblyFlags.EditorOnly,
                        TargetAssemblyType.Predefined,
                        null,
                        FilterAssemblyInEditorFolder,
                        IsCompatibleWithEditor);

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

        internal static TargetAssembly GetTargetAssembly(string scriptPath, string projectDirectory, TargetAssembly[] customTargetAssemblies)
        {
            TargetAssembly resultAssembly = GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies);

            if (resultAssembly != null)
                return resultAssembly;

            return GetPredefinedTargetAssembly(scriptPath);
        }

        internal static TargetAssembly GetPredefinedTargetAssembly(string scriptPath)
        {
            TargetAssembly resultAssembly = null;

            var extension = AssetPath.GetExtension(scriptPath).Substring(1).ToLower();
            var lowerPath = "/" + scriptPath.ToLower();
            int highestPathDepth = -1;

            foreach (var assembly in predefinedTargetAssemblies)
            {
                if (extension != assembly.Language.GetExtensionICanCompile())
                    continue;

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

        internal static TargetAssembly GetCustomTargetAssembly(string scriptPath, string projectDirectory, TargetAssembly[] customTargetAssemblies)
        {
            if (customTargetAssemblies == null)
                return null;

            int highestPathDepth = -1;
            TargetAssembly resultAssembly = null;

            // CustomScriptAssembly paths are absolute, so we convert the scriptPath to an absolute path, if necessary.
            bool isPathAbsolute = AssetPath.IsPathRooted(scriptPath);
            var fullPath = isPathAbsolute ? AssetPath.GetFullPath(scriptPath) : AssetPath.Combine(projectDirectory, scriptPath);

            foreach (var assembly in customTargetAssemblies)
            {
                int maxPathDepth = assembly.PathPrefix.Length;

                if (maxPathDepth <= highestPathDepth)
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
            return FastStartsWith(pathName, lowerPrefix, lowerPrefix) ? lowerPrefix.Length : -1;
        }
    }
}
