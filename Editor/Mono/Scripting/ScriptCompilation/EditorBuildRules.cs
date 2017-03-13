// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // This class is intentionally immutable, avoid adding mutable state to it.
    static class EditorBuildRules
    {
        internal enum TargetAssemblyType
        {
            Undefined = 0,
            Predefined = 1,
            Custom = 2
        }

        internal class TargetAssembly
        {
            public string Filename { get; private set; }
            public SupportedLanguage Language { get; private set; }
            public AssemblyFlags Flags { get; private set; }
            public Func<string, int> PathFilter { get; private set; }
            public List<TargetAssembly> References { get; private set; }
            public TargetAssemblyType Type { get; private set; }

            public TargetAssembly()
            {
                References = new List<TargetAssembly>();
            }

            public TargetAssembly(string name, SupportedLanguage language, AssemblyFlags flags, TargetAssemblyType type)
                : this(name, language, flags, type, null)
            {
            }

            public TargetAssembly(string name, SupportedLanguage language, AssemblyFlags flags, TargetAssemblyType type,
                                  Func<string, int> pathFilter) : this()
            {
                Language = language;
                Filename = name;
                Flags = flags;
                PathFilter = pathFilter;
                Type = type;
            }

            public bool EditorOnly
            {
                get { return (Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly; }
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
            public BuildFlags BuildFlags { get; set; }
            public ScriptAssemblySettings Settings { get; set; }
            public CompilationAssemblies Assemblies { get; set; }
            public HashSet<string> RunUpdaterAssemblies { get; set; }
            public HashSet<string> NotCompiledSourceFiles { get; set; }

            public GenerateChangedScriptAssembliesArgs()
            {
                NotCompiledSourceFiles = new HashSet<string>();
            }
        }

        static readonly TargetAssembly[] predefinedTargetAssemblies;

        static EditorBuildRules()
        {
            predefinedTargetAssemblies = CreatePredefinedTargetAssemblies();
        }

        public static IEnumerable<TargetAssembly> GetTargetAssemblies(SupportedLanguage language, TargetAssembly[] customTargetAssemblies)
        {
            var predefined = predefinedTargetAssemblies.Where(a => a.Language.GetLanguageName() == language.GetLanguageName());

            if (customTargetAssemblies == null)
                return predefined;

            var custom = customTargetAssemblies.Where(a => a.Language.GetLanguageName() == language.GetLanguageName());

            return predefined.Concat(custom);
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

        public static TargetAssembly[] CreateTargetAssemblies(IEnumerable<CustomScriptAssembly> customScriptAssemblies)
        {
            if (customScriptAssemblies == null)
                return null;

            var targetAssemblies = new List<TargetAssembly>();
            var nameToTargetAssembly = new Dictionary<string, TargetAssembly>();

            // Create TargetAssemblies
            foreach (var customAssembly in customScriptAssemblies)
            {
                var pathPrefixLowerCase = customAssembly.PathPrefix.ToLower();

                var targetAssembly = new TargetAssembly(customAssembly.Name + ".dll", customAssembly.Language, customAssembly.AssemblyFlags,
                        TargetAssemblyType.Custom, path => path.StartsWith(pathPrefixLowerCase) ? pathPrefixLowerCase.Length : -1);

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

        public static ScriptAssembly[] GetAllScriptAssemblies(IEnumerable<string> allSourceFiles, string projectDirectory, BuildFlags buildFlags, ScriptAssemblySettings settings, CompilationAssemblies assemblies)
        {
            bool buildingForEditor = (buildFlags & BuildFlags.BuildingForEditor) == BuildFlags.BuildingForEditor;
            var targetAssemblyFiles = new Dictionary<TargetAssembly, HashSet<string>>();

            foreach (var scriptFile in allSourceFiles)
            {
                var targetAssembly = GetTargetAssembly(scriptFile, projectDirectory, assemblies.CustomTargetAssemblies);

                // Script does not belong to any assembly. See comment in GetTargetAssembly.
                if (targetAssembly == null)
                    continue;

                // Do not collect editor assembly if we are not building for editor.
                if (!buildingForEditor && targetAssembly.EditorOnly)
                    continue;

                HashSet<string> assemblySourceFiles;

                if (!targetAssemblyFiles.TryGetValue(targetAssembly, out assemblySourceFiles))
                {
                    assemblySourceFiles = new HashSet<string>();
                    targetAssemblyFiles[targetAssembly] = assemblySourceFiles;
                }

                assemblySourceFiles.Add(Path.Combine(projectDirectory, scriptFile));
            }

            return ToScriptAssemblies(targetAssemblyFiles, settings, buildFlags, assemblies, null);
        }

        public static ScriptAssembly[] GenerateChangedScriptAssemblies(GenerateChangedScriptAssembliesArgs args)
        {
            bool buildingForEditor = (args.BuildFlags & BuildFlags.BuildingForEditor) == BuildFlags.BuildingForEditor;

            var dirtyTargetAssemblies = new Dictionary<TargetAssembly, HashSet<string>>();

            var allTargetAssemblies = args.Assemblies.CustomTargetAssemblies == null ?
                predefinedTargetAssemblies :
                predefinedTargetAssemblies.Concat(args.Assemblies.CustomTargetAssemblies).ToArray();

            // Mark all assemblies that the script updater must be run on as dirty.
            if (args.RunUpdaterAssemblies != null)
                foreach (var assemblyFilename in args.RunUpdaterAssemblies)
                {
                    var targetAssembly = allTargetAssemblies.First(a => a.Filename == assemblyFilename);
                    dirtyTargetAssemblies[targetAssembly] = new HashSet<string>();
                }

            // Collect all dirty TargetAssemblies
            foreach (var dirtySourceFile in args.DirtySourceFiles)
            {
                var targetAssembly = GetTargetAssembly(dirtySourceFile, args.ProjectDirectory, args.Assemblies.CustomTargetAssemblies);

                if (targetAssembly == null)
                {
                    args.NotCompiledSourceFiles.Add(dirtySourceFile);
                    continue;
                }

                // Do not mark editor assembly as dirty if we are not building for editor.
                if (!buildingForEditor && targetAssembly.EditorOnly)
                    continue;

                HashSet<string> assemblySourceFiles;

                if (!dirtyTargetAssemblies.TryGetValue(targetAssembly, out assemblySourceFiles))
                {
                    assemblySourceFiles = new HashSet<string>();
                    dirtyTargetAssemblies[targetAssembly] = assemblySourceFiles;
                }

                assemblySourceFiles.Add(Path.Combine(args.ProjectDirectory, dirtySourceFile));
            }

            bool isAnyCustomScriptAssemblyDirty = dirtyTargetAssemblies.Any(entry => entry.Key.Type == TargetAssemblyType.Custom);

            // If we have any dirty custom target assemblies, then the predefined target assemblies are marked as dirty,
            // as the predefined assemblies always reference the custom script assemblies.
            if (isAnyCustomScriptAssemblyDirty)
            {
                foreach (var assembly in predefinedTargetAssemblies)
                {
                    // Do not mark editor assembly as dirty if we are not building for editor.
                    if (!buildingForEditor && assembly.EditorOnly)
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
                    // Do not mark editor assembly as dirty if we are not building for editor.
                    if (!buildingForEditor && assembly.EditorOnly)
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

                if (targetAssembly == null)
                {
                    args.NotCompiledSourceFiles.Add(sourceFile);
                    continue;
                }

                // Do not mark editor assembly as dirty if we are not building for editor.
                if (!buildingForEditor && targetAssembly.EditorOnly)
                    continue;

                HashSet<string> assemblySourceFiles;

                if (dirtyTargetAssemblies.TryGetValue(targetAssembly, out assemblySourceFiles))
                    assemblySourceFiles.Add(Path.Combine(args.ProjectDirectory, sourceFile));
            }

            // Remove any target assemblies which have no source files associated with them.
            dirtyTargetAssemblies = dirtyTargetAssemblies.Where(e => e.Value.Count > 0).ToDictionary(e => e.Key, e => e.Value);

            // Convert TargetAssemblies to ScriptAssembiles
            var scriptAssemblies = ToScriptAssemblies(dirtyTargetAssemblies, args.Settings, args.BuildFlags, args.Assemblies, args.RunUpdaterAssemblies);
            return scriptAssemblies;
        }

        internal static ScriptAssembly[] ToScriptAssemblies(IDictionary<TargetAssembly, HashSet<string>> targetAssemblies, ScriptAssemblySettings settings,
            BuildFlags buildFlags, CompilationAssemblies assemblies, HashSet<string> runUpdaterAssemblies)
        {
            var scriptAssemblies = new ScriptAssembly[targetAssemblies.Count];

            var targetToScriptAssembly = new Dictionary<TargetAssembly, ScriptAssembly>();
            int index = 0;

            bool buildingForEditor = (buildFlags & BuildFlags.BuildingForEditor) == BuildFlags.BuildingForEditor;
            foreach (var entry in targetAssemblies)
            {
                var targetAssembly = entry.Key;
                var sourceFiles = entry.Value;
                var scriptAssembly = new ScriptAssembly();

                // Setup TargetAssembly -> ScriptAssembly mapping for converting references
                scriptAssemblies[index] = scriptAssembly;
                targetToScriptAssembly[targetAssembly] = scriptAssemblies[index++];

                // Setup ScriptAssembly
                scriptAssembly.BuildTarget = settings.BuildTarget;

                if (targetAssembly.EditorOnly || (buildingForEditor && settings.ApiCompatibilityLevel == ApiCompatibilityLevel.NET_4_6))
                    scriptAssembly.ApiCompatibilityLevel = ApiCompatibilityLevel.NET_2_0;
                else
                    scriptAssembly.ApiCompatibilityLevel = settings.ApiCompatibilityLevel;

                if (!string.IsNullOrEmpty(settings.FilenameSuffix))
                {
                    var basename = Path.GetFileNameWithoutExtension(targetAssembly.Filename);
                    var extension = Path.GetExtension(targetAssembly.Filename);
                    scriptAssembly.Filename = string.Concat(basename, settings.FilenameSuffix, extension);
                }
                else
                    scriptAssembly.Filename = targetAssembly.Filename;

                if (runUpdaterAssemblies != null && runUpdaterAssemblies.Contains(scriptAssembly.Filename))
                    scriptAssembly.RunUpdater = true;

                scriptAssembly.OutputDirectory = settings.OutputDirectory;
                scriptAssembly.Defines = settings.Defines;
                scriptAssembly.Files = sourceFiles.ToArray();
            }

            // Setup ScriptAssembly references
            index = 0;
            foreach (var entry in targetAssemblies)
                AddScriptAssemblyReferences(ref scriptAssemblies[index++], entry.Key, settings, buildFlags,
                    assemblies, targetToScriptAssembly, settings.FilenameSuffix);

            return scriptAssemblies;
        }

        static bool IsCompiledAssemblyCompatibleWithTargetAssembly(PrecompiledAssembly compiledAssembly, TargetAssembly targetAssembly, BuildTarget buildTarget, TargetAssembly[] customTargetAssemblies)
        {
            bool useDotNet = WSAHelpers.UseDotNetCore(targetAssembly.Filename, buildTarget, customTargetAssemblies);

            if (useDotNet)
            {
                bool compiledAssemblyCompatibleWithDotNet = (compiledAssembly.Flags & AssemblyFlags.UseForDotNet) == AssemblyFlags.UseForDotNet;
                return compiledAssemblyCompatibleWithDotNet;
            }

            bool compiledAssemblyCompatibleWithMono = (compiledAssembly.Flags & AssemblyFlags.UseForMono) == AssemblyFlags.UseForMono;
            return compiledAssemblyCompatibleWithMono;
        }

        internal static void AddScriptAssemblyReferences(ref ScriptAssembly scriptAssembly, TargetAssembly targetAssembly, ScriptAssemblySettings settings,
            BuildFlags buildFlags, CompilationAssemblies assemblies,
            IDictionary<TargetAssembly, ScriptAssembly> targetToScriptAssembly, string filenameSuffix)
        {
            var scriptAssemblyReferences = new List<ScriptAssembly>();
            var references = new List<string>();

            bool buildingForEditor = (buildFlags & BuildFlags.BuildingForEditor) == BuildFlags.BuildingForEditor;
            bool targetAssemblyEditorOnly = (targetAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

            // Add Unity assemblies (UnityEngine.dll, UnityEngine.dll) referencees.
            if (assemblies.UnityAssemblies != null)
                foreach (var unityAssembly in assemblies.UnityAssemblies)
                {
                    // Add Unity editor assemblies (UnityEditor.dll) to all assemblies when building inside the editor
                    if (buildingForEditor || targetAssemblyEditorOnly)
                    {
                        if ((unityAssembly.Flags & AssemblyFlags.UseForMono) != 0)
                            references.Add(unityAssembly.Path);
                    }
                    else
                    {
                        bool unityAssemblyEditorOnly = (unityAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                        // Add Unity runtime assemblies (UnityEngine.dll) to all assemblies
                        if (!unityAssemblyEditorOnly)
                        {
                            if (IsCompiledAssemblyCompatibleWithTargetAssembly(unityAssembly, targetAssembly, settings.BuildTarget, assemblies.CustomTargetAssemblies))
                                references.Add(unityAssembly.Path);
                        }
                    }
                }

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
                    var assemblyPath = Path.Combine(settings.OutputDirectory, reference.Filename);
                    if (!string.IsNullOrEmpty(filenameSuffix))
                        assemblyPath = assemblyPath.Replace(".dll", filenameSuffix + ".dll");
                    if (File.Exists(assemblyPath))
                        references.Add(assemblyPath);
                }
            }

            // For predefined target assembly add references to dirty custom target assemblies
            if (assemblies.CustomTargetAssemblies != null && targetAssembly.Type == TargetAssemblyType.Predefined)
            {
                foreach (var customTargetAssembly in assemblies.CustomTargetAssemblies)
                {
                    ScriptAssembly scriptAssemblyReference;

                    // Only add reference if the custom target assembly is dirty, e.g. is in targetToScriptAssembly dictionary
                    if (targetToScriptAssembly.TryGetValue(customTargetAssembly, out scriptAssemblyReference))
                        scriptAssemblyReferences.Add(scriptAssemblyReference);
                }
            }

            // Add pre-compiled assemblies as references
            if (assemblies.PrecompiledAssemblies != null)
                foreach (var precompiledAssembly in assemblies.PrecompiledAssemblies)
                {
                    bool compiledAssemblyEditorOnly = (precompiledAssembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly;

                    // Add all pre-compiled runtime assemblies as references to all script assemblies. Don't add pre-compiled editor assemblies as dependencies to runtime assemblies.
                    if (!compiledAssemblyEditorOnly || targetAssemblyEditorOnly)
                    {
                        if (IsCompiledAssemblyCompatibleWithTargetAssembly(precompiledAssembly, targetAssembly, settings.BuildTarget, assemblies.CustomTargetAssemblies))
                            references.Add(precompiledAssembly.Path);
                    }
                }

            if (buildingForEditor && assemblies.EditorAssemblyReferences != null)
                references.AddRange(assemblies.EditorAssemblyReferences);

            scriptAssembly.ScriptAssemblyReferences = scriptAssemblyReferences.ToArray();
            scriptAssembly.References = references.ToArray();
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

                var runtimeFirstPass = new TargetAssembly("Assembly-" + languageName + "-firstpass" + ".dll", language,
                        AssemblyFlags.None, TargetAssemblyType.Predefined, FilterAssemblyInFirstpassFolder);

                var runtime = new TargetAssembly("Assembly-" + languageName + ".dll", language, AssemblyFlags.None, TargetAssemblyType.Predefined);

                var editorFirstPass = new TargetAssembly("Assembly-" + languageName + "-Editor-firstpass" + ".dll", language,
                        AssemblyFlags.EditorOnly, TargetAssemblyType.Predefined, FilterAssemblyInFirstpassEditorFolder);

                var editor = new TargetAssembly("Assembly-" + languageName + "-Editor" + ".dll", language,
                        AssemblyFlags.EditorOnly, TargetAssemblyType.Predefined, FilterAssemblyInEditorFolder);

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
            TargetAssembly candidateAssembly = null;
            TargetAssembly resultAssembly = GetCustomTargetAssembly(scriptPath, projectDirectory, customTargetAssemblies, ref candidateAssembly);

            if (resultAssembly != null)
                return resultAssembly;

            // Script belongs to a CustomScriptAssembly folder but there is no
            // matching scripting language. Return null to tell the caller
            // that this script should not be be included in the compilation.
            if (candidateAssembly != null)
                return null;

            return GetPredefinedTargetAssembly(scriptPath);
        }

        internal static TargetAssembly GetPredefinedTargetAssembly(string scriptPath)
        {
            TargetAssembly resultAssembly = null;

            var extension = Path.GetExtension(scriptPath).Substring(1).ToLower();
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

        internal static TargetAssembly GetCustomTargetAssembly(string scriptPath, string projectDirectory, TargetAssembly[] customTargetAssemblies,
            ref TargetAssembly candidateAssembly)
        {
            if (customTargetAssemblies == null)
                return null;

            var extension = Path.GetExtension(scriptPath).Substring(1).ToLower();
            int highestPathDepth = -1;
            TargetAssembly resultAssembly = null;

            // CustomScriptAssembly paths are absolute, so we convert the scriptPath to an absolute path, if necessary.
            bool isPathAbsolute = Path.IsPathRooted(scriptPath);
            var lowerFullPath = isPathAbsolute ? scriptPath.ToLower() : Path.Combine(projectDirectory, scriptPath).ToLower();

            foreach (var assembly in customTargetAssemblies)
            {
                int pathDepth = assembly.PathFilter(lowerFullPath);

                if (pathDepth <= highestPathDepth)
                    continue;

                bool canCompile = extension == assembly.Language.GetExtensionICanCompile();

                if (canCompile)
                {
                    resultAssembly = assembly;
                    highestPathDepth = pathDepth;
                }
                else
                {
                    // We have a matching CustomScriptAssembly for the path, but
                    // not for the scripting language. Set candidate assembly.
                    candidateAssembly = assembly;
                }
            }

            // resultAssembly might be null if the script belongs to a
            // CustomScriptAssembly folder but there is no matching scripting language.
            // In that case the candidateAssembly will be set.

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
            int editorSegmentIndex = pathName.IndexOf(editorSegment);
            if (editorSegmentIndex == -1) return -1;

            return editorSegmentIndex + editorSegment.Length;
        }

        static int FilterAssemblyPathBeginsWith(string pathName, string prefix)
        {
            return pathName.StartsWith(prefix) ? prefix.Length : -1;
        }
    }
}
