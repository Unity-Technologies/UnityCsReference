// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.VisualStudioIntegration
{
    enum ScriptingLanguage
    {
        None,
        Boo,
        CSharp,
        UnityScript,
    }

    class SolutionSynchronizer
    {
        enum Mode
        {
            UnityScriptAsUnityProj,
            UnityScriptAsPrecompiledAssembly
        }

        public static readonly ISolutionSynchronizationSettings DefaultSynchronizationSettings =
            new DefaultSolutionSynchronizationSettings();

        static readonly string WindowsNewline = "\r\n";

        /// <summary>
        /// Map source extensions to ScriptingLanguages
        /// </summary>
        static internal readonly Dictionary<string, ScriptingLanguage> BuiltinSupportedExtensions = new Dictionary<string, ScriptingLanguage>
        {
            {"cs", ScriptingLanguage.CSharp},
            {"uxml", ScriptingLanguage.None},
            {"uss", ScriptingLanguage.None},
            {"shader", ScriptingLanguage.None},
            {"compute", ScriptingLanguage.None},
            {"cginc", ScriptingLanguage.None},
            {"hlsl", ScriptingLanguage.None},
            {"glslinc", ScriptingLanguage.None},
            {"template", ScriptingLanguage.None},
            {"raytrace", ScriptingLanguage.None},
        };

        private static readonly string[] reimportSyncExtensions = new[] { ".dll", ".asmdef" };

        string[] ProjectSupportedExtensions = new string[0];

        /// <summary>
        /// Map ScriptingLanguages to project extensions
        /// </summary>
        static readonly Dictionary<ScriptingLanguage, string> ProjectExtensions = new Dictionary<ScriptingLanguage, string>
        {
            { ScriptingLanguage.Boo, ".booproj" },
            { ScriptingLanguage.CSharp, ".csproj" },
            { ScriptingLanguage.UnityScript, ".unityproj" },
            { ScriptingLanguage.None, ".csproj" },
        };

        public static readonly string MSBuildNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        private readonly string _projectDirectory;
        private readonly ISolutionSynchronizationSettings _settings;
        private readonly string _projectName;
        readonly IAssemblyNameProvider m_assemblyNameProvider;
        bool m_ShouldGenerateAll;

        IFileIO m_fileIOProvider;
        IGUIDGenerator m_GUIDProvider;

        public SolutionSynchronizer(string projectDirectory, ISolutionSynchronizationSettings settings, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIO, IGUIDGenerator guidGenerator)
        {
            _projectDirectory = projectDirectory.ConvertSeparatorsToUnity();
            _settings = settings;
            _projectName = Path.GetFileName(_projectDirectory);
            m_assemblyNameProvider = assemblyNameProvider;
            m_fileIOProvider = fileIO;
            m_GUIDProvider = guidGenerator;
        }

        public SolutionSynchronizer(string projectDirectory, ISolutionSynchronizationSettings settings) : this(projectDirectory, settings, new AssemblyNameProvider(), new FileIOProvider(), new GUIDProvider())
        {
        }

        public SolutionSynchronizer(string projectDirectory) : this(projectDirectory, DefaultSynchronizationSettings)
        {
        }

        private void SetupProjectSupportedExtensions()
        {
            ProjectSupportedExtensions = m_assemblyNameProvider.ProjectSupportedExtensions;
        }

        bool ShouldFileBePartOfSolution(string file)
        {
            string extension = Path.GetExtension(file);

            // Exclude files coming from packages except if they are internalized.
            if (!m_ShouldGenerateAll && m_assemblyNameProvider.IsInternalizedPackagePath(file))
                return false;

            // Dll's are not scripts but still need to be included..
            if (extension == ".dll")
                return true;

            if (file.ToLower().EndsWith(".asmdef"))
                return true;

            return IsSupportedExtension(extension);
        }

        private bool IsSupportedExtension(string extension)
        {
            extension = extension.TrimStart('.');
            if (BuiltinSupportedExtensions.ContainsKey(extension))
                return true;
            if (ProjectSupportedExtensions.Contains(extension))
                return true;
            return false;
        }

        private static ScriptingLanguage ScriptingLanguageFor(Compilation.Assembly assembly)
        {
            return ScriptingLanguageFor(GetExtensionOfSourceFiles(assembly.sourceFiles));
        }

        private static ScriptingLanguage ScriptingLanguageFor(string extension)
        {
            ScriptingLanguage result;
            if (BuiltinSupportedExtensions.TryGetValue(extension.TrimStart('.'), out result))
                return result;

            return ScriptingLanguage.None;
        }

        static string GetExtensionOfSourceFiles(string[] files)
        {
            // Files are sorted by the compilation pipeline, ensuring cs files are the first in the list.
            return files.Length > 0 ? GetExtensionOfSourceFile(files[0]) : "NA";
        }

        static string GetExtensionOfSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            ext = ext.Substring(1); //strip dot
            return ext;
        }

        public bool ProjectExists(Compilation.Assembly assembly)
        {
            return m_fileIOProvider.Exists(ProjectFile(assembly));
        }

        public bool SolutionExists()
        {
            return m_fileIOProvider.Exists(SolutionFile());
        }

        private static void DumpAssembly(Compilation.Assembly assembly)
        {
            Console.WriteLine("{0} ({1})", assembly.outputPath, assembly.compilerOptions.ApiCompatibilityLevel);
            Console.WriteLine("Files: ");
            Console.WriteLine(string.Join("\n", assembly.sourceFiles));
            Console.WriteLine("References: ");
            Console.WriteLine(string.Join("\n", assembly.allReferences));
            Console.WriteLine("");
        }

        /// <summary>
        /// Syncs the scripting solution if any affected files are relevant.
        /// </summary>
        /// <returns>
        /// Whether the solution was synced.
        /// </returns>
        /// <param name='affectedFiles'>
        /// A set of files whose status has changed
        /// </param>
        /// <param name="reimportedFiles">
        /// A set of files that got reimported
        /// </param>
        public bool SyncIfNeeded(IEnumerable<string> affectedFiles, IEnumerable<string> reimportedFiles)
        {
            SetupProjectSupportedExtensions();

            // Don't sync if we haven't synced before
            if (SolutionExists() && (affectedFiles.Any(ShouldFileBePartOfSolution) || reimportedFiles.Any(ShouldSyncOnReimportedAsset)))
            {
                Sync();
                return true;
            }

            return false;
        }

        private bool ShouldSyncOnReimportedAsset(string asset)
        {
            return reimportSyncExtensions.Contains(new FileInfo(asset).Extension);
        }

        public void Sync()
        {
            Profiler.BeginSample("SolutionSynchronizerSync");
            // Do not sync solution until all Unity extensions are registered and initialized.
            // Otherwise Unity might emit errors when VSTU tries to generate the solution and
            // get all managed extensions, which not yet initialized.
            if (!InternalEditorUtility.IsUnityExtensionsInitialized())
            {
                Profiler.EndSample();
                return;
            }

            SetupProjectSupportedExtensions();

            bool externalCodeAlreadyGeneratedProjects = AssetPostprocessingInternal.OnPreGeneratingCSProjectFiles();

            if (!externalCodeAlreadyGeneratedProjects)
            {
                #pragma warning disable 618
                var scriptEditor = ScriptEditorUtility.GetScriptEditorFromPreferences();
                GenerateAndWriteSolutionAndProjects(scriptEditor);
            }

            AssetPostprocessingInternal.CallOnGeneratedCSProjectFiles();
            Profiler.EndSample();
        }

        internal void GenerateAndWriteSolutionAndProjects(ScriptEditorUtility.ScriptEditor scriptEditor)
        {
            Profiler.BeginSample("GenerateAndWriteSolutionAndProjects");

            Profiler.BeginSample("SolutionSynchronizer.GetIslands");
            // Only synchronize islands that have associated source files and ones that we actually want in the project.
            // This also filters out DLLs coming from .asmdef files in packages.
            List<Compilation.Assembly> assemblies = m_assemblyNameProvider.GetAssemblies(ShouldFileBePartOfSolution).ToList();

            Profiler.EndSample();

            Profiler.BeginSample("GenerateAllAssetProjectParts.GetIslands");
            var allAssetProjectParts = GenerateAllAssetProjectParts();
            Profiler.EndSample();

            Profiler.BeginSample("SyncSolution");
            SyncSolution(assemblies);
            Profiler.EndSample();

            var allProjectAssemblies = RelevantAssembliesForMode(assemblies, ModeForCurrentExternalEditor()).ToList();

            foreach (Compilation.Assembly assembly in allProjectAssemblies)
            {
                Profiler.BeginSample("SyncProject");
                SyncProject(assembly, allAssetProjectParts, ParseResponseFileData(assembly), allProjectAssemblies);
                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        IEnumerable<ResponseFileData> ParseResponseFileData(Compilation.Assembly assembly)
        {
            var systemReferenceDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(assembly.compilerOptions.ApiCompatibilityLevel);

            Dictionary<string, ResponseFileData> responseFilesData = assembly.compilerOptions.ResponseFiles.ToDictionary(x => x, x => m_assemblyNameProvider.ParseResponseFile(
                x,
                _projectDirectory,
                systemReferenceDirectories
            ));

            Dictionary<string, ResponseFileData> responseFilesWithErrors = responseFilesData.Where(x => x.Value.Errors.Any())
                .ToDictionary(x => x.Key, x => x.Value);

            if (responseFilesWithErrors.Any())
            {
                foreach (var error in responseFilesWithErrors)
                    foreach (var valueError in error.Value.Errors)
                    {
                        UnityEngine.Debug.LogErrorFormat("{0} Parse Error : {1}", error.Key, valueError);
                    }
            }

            return responseFilesData.Select(x => x.Value);
        }

        Dictionary<string, string> GenerateAllAssetProjectParts()
        {
            Dictionary<string, StringBuilder> stringBuilders = new Dictionary<string, StringBuilder>();

            foreach (string asset in m_assemblyNameProvider.GetAllAssetPaths())
            {
                // Exclude files coming from packages except if they are internalized.
                if (!m_ShouldGenerateAll && m_assemblyNameProvider.IsInternalizedPackagePath(asset))
                {
                    continue;
                }
                string extension = Path.GetExtension(asset);
                if (IsSupportedExtension(extension) && ScriptingLanguage.None == ScriptingLanguageFor(extension))
                {
                    // Find assembly the asset belongs to by adding script extension and using compilation pipeline.
                    var assemblyName = m_assemblyNameProvider.GetAssemblyNameFromScriptPath(asset + ".cs");
                    assemblyName = assemblyName ?? m_assemblyNameProvider.GetAssemblyNameFromScriptPath(asset + ".js");
                    assemblyName = assemblyName ?? m_assemblyNameProvider.GetAssemblyNameFromScriptPath(asset + ".boo");

                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        continue;
                    }

                    assemblyName = Utility.FileNameWithoutExtension(assemblyName);

                    StringBuilder projectBuilder = null;

                    if (!stringBuilders.TryGetValue(assemblyName, out projectBuilder))
                    {
                        projectBuilder = new StringBuilder();
                        stringBuilders[assemblyName] = projectBuilder;
                    }

                    projectBuilder.Append("     <None Include=\"").Append(EscapedRelativePathFor(asset)).Append("\" />").Append(WindowsNewline);
                }
            }

            var result = new Dictionary<string, string>();

            foreach (var entry in stringBuilders)
                result[entry.Key] = entry.Value.ToString();

            return result;
        }

        void SyncProject(Compilation.Assembly assembly,
            Dictionary<string, string> allAssetsProjectParts,
            IEnumerable<ResponseFileData> responseFilesData,
            List<Compilation.Assembly> allProjectAssemblies)
        {
            SyncProjectFileIfNotChanged(ProjectFile(assembly), ProjectText(assembly, ModeForCurrentExternalEditor(), allAssetsProjectParts, responseFilesData, allProjectAssemblies));
        }

        void SyncProjectFileIfNotChanged(string path, string newContents)
        {
            if (Path.GetExtension(path) == ".csproj")
            {
                newContents = AssetPostprocessingInternal.CallOnGeneratedCSProject(path, newContents);
            }

            SyncFileIfNotChanged(path, newContents);
        }

        void SyncSolutionFileIfNotChanged(string path, string newContents)
        {
            newContents = AssetPostprocessingInternal.CallOnGeneratedSlnSolution(path, newContents);

            SyncFileIfNotChanged(path, newContents);
        }

        static void LogDifference(string path, string currentContents, string newContents)
        {
            Console.WriteLine("[C# Project] Writing {0} because it has changed", path);

            var currentReader = new StringReader(currentContents);
            var newReader = new StringReader(newContents);

            string currentLine = null;
            string newLine = null;
            int lineNumber = 1;

            do
            {
                currentLine = currentReader.ReadLine();
                newLine = newReader.ReadLine();

                if (currentLine != null && newLine != null && currentLine != newLine)
                {
                    Console.WriteLine("[C# Project] First difference on line {0}", lineNumber);

                    Console.WriteLine("\n[C# Project] Current {0}:", path);

                    for (int i = 0;
                         i < 5 && currentLine != null;
                         i++, currentLine = currentReader.ReadLine())
                    {
                        Console.WriteLine("[C# Project]   {0:D3}: {1}", lineNumber + i, currentLine);
                    }

                    Console.WriteLine("\n[C# Project] New {0}:", path);

                    for (int i = 0;
                         i < 5 && newLine != null;
                         i++, newLine = newReader.ReadLine())
                    {
                        Console.WriteLine("[C# Project]   {0:D3}: {1}", lineNumber + i, newLine);
                    }

                    currentLine = null;
                    newLine = null;
                }

                lineNumber++;
            }
            while (currentLine != null && newLine != null);
        }

        private void SyncFileIfNotChanged(string filename, string newContents)
        {
            if (m_fileIOProvider.Exists(filename))
            {
                var currentContents = m_fileIOProvider.ReadAllText(filename);

                if (currentContents == newContents)
                {
                    return;
                }

                try
                {
                    LogDifference(filename, currentContents, newContents);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Failed to log difference of {0}\n{1}",
                        filename, exception);
                }
            }

            m_fileIOProvider.WriteAllText(filename, newContents);
        }

        public static readonly Regex scriptReferenceExpression = new Regex(
            @"^Library.ScriptAssemblies.(?<dllname>(?<project>.*)\.dll$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static bool IsAdditionalInternalAssemblyReference(bool isBuildingEditorProject, string reference)
        {
            if (isBuildingEditorProject)
                return Modules.ModuleUtils.GetAdditionalReferencesForEditorCsharpProject().Contains(reference);
            return false;
        }

        string ProjectText(Compilation.Assembly assembly,
            Mode mode,
            Dictionary<string, string> allAssetsProjectParts,
            IEnumerable<ResponseFileData> responseFilesData,
            List<Compilation.Assembly> allProjectAssemblies)
        {
            var projectBuilder = new StringBuilder(ProjectHeader(assembly, responseFilesData));
            var references = new List<string>();
            var projectReferences = new List<Match>();

            foreach (string file in assembly.sourceFiles)
            {
                if (!ShouldFileBePartOfSolution(file))
                    continue;

                var extension = Path.GetExtension(file).ToLower();
                var fullFile = EscapedRelativePathFor(file);
                if (".dll" != extension)
                {
                    var tagName = "Compile";
                    projectBuilder.Append("     <").Append(tagName).Append(" Include=\"").Append(fullFile).Append("\" />").Append(WindowsNewline);
                }
                else
                {
                    references.Add(fullFile);
                }
            }

            string additionalAssetsForProject;

            // Append additional non-script files that should be included in project generation.
            if (allAssetsProjectParts.TryGetValue(assembly.name, out additionalAssetsForProject))
                projectBuilder.Append(additionalAssetsForProject);

            var allAdditionalReferenceFilenames = new List<string>();
            var assemblyRefs = references.Union(assembly.allReferences);

            foreach (string reference in assemblyRefs)
            {
                if (reference.EndsWith("/UnityEditor.dll", StringComparison.Ordinal)
                    || reference.EndsWith("/UnityEngine.dll", StringComparison.Ordinal)
                    || reference.EndsWith("\\UnityEditor.dll", StringComparison.Ordinal)
                    || reference.EndsWith("\\UnityEngine.dll", StringComparison.Ordinal))
                    continue;

                var match = scriptReferenceExpression.Match(reference);
                if (match.Success)
                {
                    var language = ScriptCompilers.GetLanguageFromExtension(GetExtensionOfSourceFiles(assembly.sourceFiles));
                    var targetLanguage = (ScriptingLanguage)Enum.Parse(typeof(ScriptingLanguage), language.GetLanguageName(), true);
                    if (mode == Mode.UnityScriptAsUnityProj || ScriptingLanguage.CSharp == targetLanguage)
                    {
                        // Add a reference to a project except if it's a reference to a script assembly
                        // that we are not generating a project for. This will be the case for assemblies
                        // coming from .assembly.json files in non-internalized packages.
                        var dllName = match.Groups["dllname"].Value;
                        if (allProjectAssemblies.Any(i => Path.GetFileName(i.outputPath) == dllName))
                        {
                            projectReferences.Add(match);
                            continue;
                        }
                    }
                }

                string fullReference = Path.IsPathRooted(reference) ? reference : Path.Combine(_projectDirectory, reference);
                AppendReference(fullReference, projectBuilder);
            }

            var responseRefs = responseFilesData.SelectMany(x => x.FullPathReferences);
            foreach (var reference in responseRefs)
            {
                AppendReference(reference, projectBuilder);
            }

            if (0 < projectReferences.Count)
            {
                string referencedProject;
                projectBuilder.AppendLine("  </ItemGroup>");
                projectBuilder.AppendLine("  <ItemGroup>");
                foreach (Match reference in projectReferences)
                {
                    var targetAssembly = EditorCompilationInterface.Instance.GetTargetAssemblyDetails(reference.Groups["dllname"].Value);
                    ScriptingLanguage targetLanguage = ScriptingLanguage.None;
                    if (targetAssembly != null)
                        targetLanguage = (ScriptingLanguage)Enum.Parse(typeof(ScriptingLanguage), targetAssembly.Language.GetLanguageName(), true);
                    referencedProject = reference.Groups["project"].Value;
                    projectBuilder.Append("    <ProjectReference Include=\"").Append(referencedProject).Append(GetProjectExtension(targetLanguage)).Append("\">").Append(WindowsNewline);
                    projectBuilder.Append("      <Project>{").Append(ProjectGuid(Path.Combine("Temp", reference.Groups["project"].Value + ".dll"))).Append("}</Project>").Append(WindowsNewline);
                    projectBuilder.Append("      <Name>").Append(referencedProject).Append("</Name>").Append(WindowsNewline);
                    projectBuilder.AppendLine("    </ProjectReference>");
                }
            }

            projectBuilder.Append(ProjectFooter(assembly));
            return projectBuilder.ToString();
        }

        static void AppendReference(string fullReference, StringBuilder projectBuilder)
        {
            //replace \ with / and \\ with /
            var escapedFullPath = SecurityElement.Escape(fullReference);
            escapedFullPath = escapedFullPath.Replace("\\", "/");
            escapedFullPath = escapedFullPath.Replace("\\\\", "/");
            projectBuilder.Append(" <Reference Include=\"").Append(Utility.FileNameWithoutExtension(escapedFullPath)).Append("\">").Append(WindowsNewline);
            projectBuilder.Append(" <HintPath>").Append(escapedFullPath).Append("</HintPath>").Append(WindowsNewline);
            projectBuilder.Append(" </Reference>").Append(WindowsNewline);
        }

        public string ProjectFile(Compilation.Assembly assembly)
        {
            ScriptingLanguage language = ScriptingLanguageFor(assembly);
            return Path.Combine(_projectDirectory, string.Format("{0}{1}", assembly.name, ProjectExtensions[language]));
        }

        internal string SolutionFile()
        {
            return Path.Combine(_projectDirectory, string.Format("{0}.sln", _projectName));
        }

        private string ProjectHeader(
            Compilation.Assembly assembly,
            IEnumerable<ResponseFileData> responseFilesData)
        {
            string targetframeworkversion = "v4.7.1";
            string targetLanguageVersion = "latest";
            string toolsversion = "4.0";
            string productversion = "10.0.20506";
            string baseDirectory = ".";
            string cscToolPath = "$(CscToolPath)";
            cscToolPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts");
            cscToolPath = Paths.UnifyDirectorySeparator(cscToolPath);
            string cscToolExe = Application.platform == RuntimePlatform.WindowsEditor ? "unity_csc.bat" : "unity_csc.sh";

            var arguments = new object[]
            {
                toolsversion,
                productversion,
                ProjectGuid(assembly.name),
                _settings.EngineAssemblyPath,
                _settings.EditorAssemblyPath,
                string.Join(";", new[] { "DEBUG", "TRACE"}.Concat(assembly.defines).Concat(responseFilesData.SelectMany(x => x.Defines)).Distinct().ToArray()),
                MSBuildNamespaceUri,
                assembly.name,
                m_assemblyNameProvider.ProjectGenerationRootNamespace,
                targetframeworkversion,
                targetLanguageVersion,
                baseDirectory,
                assembly.compilerOptions.AllowUnsafeCode | responseFilesData.Any(x => x.Unsafe),
                cscToolPath,
                cscToolExe,
            };

            try
            {
                ScriptingLanguage language = ScriptingLanguageFor(assembly);
                return string.Format(_settings.GetProjectHeaderTemplate(language), arguments);
            }
            catch (Exception)
            {
                throw new System.NotSupportedException("Failed creating c# project because the c# project header did not have the correct amount of arguments, which is " + arguments.Length);
            }
        }

        private void SyncSolution(List<Compilation.Assembly> assemblies)
        {
            SyncSolutionFileIfNotChanged(SolutionFile(), SolutionText(assemblies, ModeForCurrentExternalEditor()));
        }

        private static Mode ModeForCurrentExternalEditor()
        {
            #pragma warning disable 618
            var scriptEditor = ScriptEditorUtility.GetScriptEditorFromPreferences();

            if (scriptEditor == ScriptEditorUtility.ScriptEditor.VisualStudio ||
                scriptEditor == ScriptEditorUtility.ScriptEditor.VisualStudioExpress)
                return Mode.UnityScriptAsPrecompiledAssembly;

            return EditorPrefs.GetBool("kExternalEditorSupportsUnityProj", false) ? Mode.UnityScriptAsUnityProj : Mode.UnityScriptAsPrecompiledAssembly;
        }

        private string SolutionText(List<Compilation.Assembly> assemblies, Mode mode)
        {
            var fileversion = "11.00";
            var vsversion = "2010";
            if (_settings.VisualStudioVersion == 9)
            {
                fileversion = "10.00";
                vsversion = "2008";
            }
            var relevantAssemblies = RelevantAssembliesForMode(assemblies, mode);
            string projectEntries = GetProjectEntries(relevantAssemblies);
            string projectConfigurations = string.Join(WindowsNewline, relevantAssemblies.Select(i => GetProjectActiveConfigurations(ProjectGuid(i.name))).ToArray());
            return string.Format(_settings.SolutionTemplate, fileversion, vsversion, projectEntries, projectConfigurations);
        }

        private static IEnumerable<Compilation.Assembly> RelevantAssembliesForMode(List<Compilation.Assembly> assemblies, Mode mode)
        {
            return assemblies.Where(i => ScriptingLanguage.CSharp == ScriptingLanguageFor(i));
        }

        /// <summary>
        /// Get a Project("{guid}") = "MyProject", "MyProject.unityproj", "{projectguid}"
        /// entry for each relevant language
        /// </summary>
        internal string GetProjectEntries(IEnumerable<Compilation.Assembly> assemblies)
        {
            var projectEntries = assemblies.Select(i => string.Format(
                DefaultSynchronizationSettings.SolutionProjectEntryTemplate,
                SolutionGuid(i),
                i.name,
                Path.GetFileName(ProjectFile(i)),
                ProjectGuid(i.name)
            ));

            return string.Join(WindowsNewline, projectEntries.ToArray());
        }

        /// <summary>
        /// Generate the active configuration string for a given project guid
        /// </summary>
        private string GetProjectActiveConfigurations(string projectGuid)
        {
            return string.Format(
                DefaultSynchronizationSettings.SolutionProjectConfigurationTemplate,
                projectGuid);
        }

        string EscapedRelativePathFor(string file)
        {
            var projectDir = _projectDirectory.ConvertSeparatorsToWindows();
            file = file.ConvertSeparatorsToWindows();
            var path = SkipPathPrefix(file, projectDir);

            var packageInfo = m_assemblyNameProvider.FindForAssetPath(path.ConvertSeparatorsToUnity());
            if (packageInfo != null)
            {
                // We have to normalize the path, because the PackageManagerRemapper assumes
                // dir seperators will be os specific.
                var absolutePath = Path.GetFullPath(NormalizePath(path)).ConvertSeparatorsToWindows();
                path = SkipPathPrefix(absolutePath, projectDir);
            }

            return SecurityElement.Escape(path);
        }

        static string SkipPathPrefix(string path, string prefix)
        {
            if (path.StartsWith($@"{prefix}\"))
                return path.Substring(prefix.Length + 1);
            return path;
        }

        static string NormalizePath(string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
                return path.Replace('/', Path.DirectorySeparatorChar);
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }

        string ProjectGuid(string assembly)
        {
            return m_GUIDProvider.ProjectGuid(_projectName, assembly);
        }

        string SolutionGuid(Assembly assembly)
        {
            return m_GUIDProvider.SolutionGuid(_projectName, GetExtensionOfSourceFiles(assembly.sourceFiles));
        }

        string ProjectFooter(Assembly assembly)
        {
            return _settings.GetProjectFooterTemplate(ScriptingLanguageFor(assembly));
        }

        [Obsolete("Use AssemblyHelper.IsManagedAssembly")]
        public static bool IsManagedAssembly(string file)
        {
            return AssemblyHelper.IsManagedAssembly(file);
        }

        public static string GetProjectExtension(ScriptingLanguage language)
        {
            if (!ProjectExtensions.ContainsKey(language))
                throw new ArgumentException("Unsupported language", "language");

            return ProjectExtensions[language];
        }

        public void GenerateAll(bool generateAll)
        {
            m_ShouldGenerateAll = generateAll;
        }
    }
}
