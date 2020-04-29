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
using UnityEditor.PackageManager;
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

    interface IAssemblyNameProvider
    {
        string[] ProjectSupportedExtensions { get; }
        ProjectGenerationFlag ProjectGenerationFlag { get; }
        string GetAssemblyNameFromScriptPath(string path);
        IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution);
        IEnumerable<string> GetAllAssetPaths();
        UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath);
        ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
        bool IsInternalizedPackagePath(string path);
        bool HasFlag(ProjectGenerationFlag flag);
        void ToggleProjectGeneration(ProjectGenerationFlag preference);
    }

    class AssemblyNameProvider : IAssemblyNameProvider
    {
        ProjectGenerationFlag m_ProjectGenerationFlag = (ProjectGenerationFlag)EditorPrefs.GetInt("unity_project_generation_flag", 0);

        public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationUserExtensions;

        public ProjectGenerationFlag ProjectGenerationFlag
        {
            get { return m_ProjectGenerationFlag; }
            private set
            {
                EditorPrefs.SetInt("unity_project_generation_flag", (int)value);
                m_ProjectGenerationFlag = value;
            }
        }

        public string GetAssemblyNameFromScriptPath(string path)
        {
            return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
        }

        public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
        {
            return CompilationPipeline.GetAssemblies()
                .Where(i => 0 < i.sourceFiles.Length && i.sourceFiles.Any(shouldFileBePartOfSolution));
        }

        public IEnumerable<string> GetAllAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths();
        }

        public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return CompilationPipeline.ParseResponseFile(
                responseFilePath,
                projectDirectory,
                systemReferenceDirectories
            );
        }

        public bool HasFlag(ProjectGenerationFlag flag)
        {
            return (this.ProjectGenerationFlag & flag) == flag;
        }

        public void ToggleProjectGeneration(ProjectGenerationFlag preference)
        {
            if (HasFlag(preference))
            {
                ProjectGenerationFlag ^= preference;
            }
            else
            {
                ProjectGenerationFlag |= preference;
            }
        }

        public UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
        }

        public bool IsInternalizedPackagePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }
            var packageInfo = FindForAssetPath(path);
            if (packageInfo == null)
            {
                return false;
            }
            var packageSource = packageInfo.source;
            switch (packageSource)
            {
                case PackageSource.Embedded:
                    return !HasFlag(ProjectGenerationFlag.Embedded);
                case PackageSource.Registry:
                    return !HasFlag(ProjectGenerationFlag.Registry);
                case PackageSource.BuiltIn:
                    return !HasFlag(ProjectGenerationFlag.BuiltIn);
                case PackageSource.Unknown:
                    return !HasFlag(ProjectGenerationFlag.Unknown);
                case PackageSource.Local:
                    return !HasFlag(ProjectGenerationFlag.Local);
                case PackageSource.Git:
                    return !HasFlag(ProjectGenerationFlag.Git);
                case PackageSource.LocalTarball:
                    return !HasFlag(ProjectGenerationFlag.LocalTarBall);
            }

            return true;
        }

        public void ResetProjectGenerationFlag()
        {
            ProjectGenerationFlag = ProjectGenerationFlag.None;
        }
    }

    class SolutionSynchronizer
    {
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
        bool m_ShouldGenerateAll;
        readonly IFileIO m_FileIO;

        internal IAssemblyNameProvider AssemblyNameProvider { get; }

        public SolutionSynchronizer(string projectDirectory, ISolutionSynchronizationSettings settings, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIO)
        {
            _projectDirectory = projectDirectory.ConvertSeparatorsToUnity();
            _settings = settings;
            _projectName = Path.GetFileName(_projectDirectory);
            AssemblyNameProvider = assemblyNameProvider;
            m_FileIO = fileIO;
        }

        public SolutionSynchronizer(string projectDirectory, ISolutionSynchronizationSettings settings) : this(projectDirectory, settings, new AssemblyNameProvider(), new FileIOProvider())
        {
        }

        public SolutionSynchronizer(string projectDirectory) : this(projectDirectory, DefaultSynchronizationSettings)
        {
        }

        private void SetupProjectSupportedExtensions()
        {
            ProjectSupportedExtensions = AssemblyNameProvider.ProjectSupportedExtensions;
        }

        public bool ShouldFileBePartOfSolution(string file)
        {
            string extension = Path.GetExtension(file);

            // Exclude files coming from packages except if they are internalized.
            if (AssemblyNameProvider.IsInternalizedPackagePath(file))
            {
                return false;
            }

            // Dll's are not scripts but still need to be included..
            if (extension == ".dll")
                return true;

            if (file.ToLowerInvariant().EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
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

        private static ScriptingLanguage ScriptingLanguageFor(Assembly assembly)
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
            return files.Length > 0 ? GetExtensionOfSourceFile(files[0]) : "NA";
        }

        static string GetExtensionOfSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            ext = ext.Substring(1); //strip dot
            return ext;
        }

        public bool ProjectExists(Assembly assembly)
        {
            return m_FileIO.Exists(ProjectFile(assembly));
        }

        public bool SolutionExists()
        {
            return m_FileIO.Exists(SolutionFile());
        }

        private static void DumpAssembly(Assembly assembly)
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
            // Only synchronize assemblies that have associated source files and ones that we actually want in the project.
            // This also filters out DLLs coming from .asmdef files in packages.
            IEnumerable<Assembly> assemblies = AssemblyNameProvider.GetAssemblies(ShouldFileBePartOfSolution);

            Profiler.EndSample();

            Profiler.BeginSample("GenerateAllAssetProjectParts.GetIslands");
            var allAssetProjectParts = GenerateAllAssetProjectParts();
            Profiler.EndSample();

            var assemblyList = assemblies.ToList();

            Profiler.BeginSample("SyncSolution");
            SyncSolution(assemblyList);
            Profiler.EndSample();

            var allProjectAssemblies = RelevantAssemblies(assemblyList).ToList();
            var assemblyNames = new HashSet<string>(allProjectAssemblies.Select(assembly => Path.GetFileName(assembly.outputPath)));

            foreach (Assembly assembly in allProjectAssemblies)
            {
                Profiler.BeginSample("SyncProject");
                SyncProject(assembly, allAssetProjectParts, ParseResponseFileData(assembly), assemblyNames);
                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        List<ResponseFileData> ParseResponseFileData(Assembly assembly)
        {
            var systemReferenceDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(assembly.compilerOptions.ApiCompatibilityLevel);

            Dictionary<string, ResponseFileData> responseFilesData = assembly.compilerOptions.ResponseFiles.ToDictionary(x => x, x => AssemblyNameProvider.ParseResponseFile(
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

            return responseFilesData.Select(x => x.Value).ToList();
        }

        Dictionary<string, string> GenerateAllAssetProjectParts()
        {
            Dictionary<string, StringBuilder> stringBuilders = new Dictionary<string, StringBuilder>();

            foreach (string asset in AssemblyNameProvider.GetAllAssetPaths())
            {
                // Exclude files coming from packages except if they are internalized.
                if (AssemblyNameProvider.IsInternalizedPackagePath(asset))
                {
                    continue;
                }

                string extension = Path.GetExtension(asset);
                if (IsSupportedExtension(extension) && ScriptingLanguage.None == ScriptingLanguageFor(extension))
                {
                    // Find assembly the asset belongs to by adding script extension and using compilation pipeline.
                    var assemblyName = AssemblyNameProvider.GetAssemblyNameFromScriptPath(asset);

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

        void SyncProject(
            Assembly assembly,
            Dictionary<string, string> allAssetsProjectParts,
            List<ResponseFileData> responseFilesData,
            HashSet<string> assemblyNames)
        {
            SyncProjectFileIfNotChanged(ProjectFile(assembly), ProjectText(assembly, allAssetsProjectParts, responseFilesData, assemblyNames));
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
            if (m_FileIO.Exists(filename))
            {
                var currentContents = m_FileIO.ReadAllText(filename);

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

            m_FileIO.WriteAllText(filename, newContents);
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

        string ProjectText(
            Assembly assembly,
            Dictionary<string, string> allAssetsProjectParts,
            List<ResponseFileData> responseFilesData,
            HashSet<string> assemblyNames)
        {
            var projectBuilder = new StringBuilder();
            projectBuilder.Append(ProjectHeader(assembly, responseFilesData));
            var references = new List<string>();

            foreach (string file in assembly.sourceFiles)
            {
                if (!ShouldFileBePartOfSolution(file))
                    continue;

                var extension = Path.GetExtension(file).ToLower();
                var fullFile = EscapedRelativePathFor(file);
                if (".dll" != extension)
                {
                    projectBuilder.Append("     <Compile Include=\"").Append(fullFile).Append("\" />").Append(WindowsNewline);
                }
                else
                {
                    references.Add(fullFile);
                }
            }

            // Append additional non-script files that should be included in project generation.
            string additionalAssetsForProject;
            if (allAssetsProjectParts.TryGetValue(assembly.name, out additionalAssetsForProject))
                projectBuilder.Append(additionalAssetsForProject);

            var responseRefs = responseFilesData.SelectMany(x => x.FullPathReferences.Select(r => r));
            var internalAssemblyReferences = assembly.assemblyReferences
                .Where(i => !i.sourceFiles.Any(ShouldFileBePartOfSolution)).Select(i => i.outputPath);
            var allReferences =
                assembly.compiledAssemblyReferences
                    .Union(responseRefs)
                    .Union(references)
                    .Union(internalAssemblyReferences);

            foreach (var reference in allReferences)
            {
                string fullReference = Path.IsPathRooted(reference) ? reference : Path.Combine(_projectDirectory, reference);
                AppendReference(fullReference, projectBuilder);
            }

            if (0 < assembly.assemblyReferences.Length)
            {
                projectBuilder.Append("  </ItemGroup>").Append(WindowsNewline);
                projectBuilder.Append("  <ItemGroup>").Append(WindowsNewline);
                foreach (Assembly reference in assembly.assemblyReferences.Where(i => i.sourceFiles.Any(ShouldFileBePartOfSolution)))
                {
                    var referencedProject = reference.outputPath;

                    projectBuilder.Append("    <ProjectReference Include=\"").Append(reference.name).Append(".csproj").Append("\">").Append(WindowsNewline);
                    projectBuilder.Append("      <Project>{").Append(ProjectGuid(reference.name)).Append("}</Project>").Append(WindowsNewline);
                    projectBuilder.Append("      <Name>").Append(reference.name).Append("</Name>").Append(WindowsNewline);
                    projectBuilder.Append("      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>").Append(WindowsNewline);
                    projectBuilder.Append("    </ProjectReference>").Append(WindowsNewline);
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

        public string ProjectFile(Assembly assembly)
        {
            ScriptingLanguage language = ScriptingLanguageFor(assembly);
            return Path.Combine(_projectDirectory, string.Format("{0}{1}", assembly.name, ProjectExtensions[language]));
        }

        internal string SolutionFile()
        {
            return Path.Combine(_projectDirectory, string.Format("{0}.sln", _projectName));
        }

        private string ProjectHeader(Assembly assembly,
            IEnumerable<ResponseFileData> responseFilesData)
        {
            string targetframeworkversion = "v3.5";
            string targetLanguageVersion = "4";
            string toolsversion = "4.0";
            string productversion = "10.0.20506";
            string baseDirectory = ".";
            string cscToolPath = "$(CscToolPath)";
            string cscToolExe = "$(CscToolExe)";
            ScriptingLanguage language = ScriptingLanguageFor(assembly);

            if (PlayerSettingsEditor.IsLatestApiCompatibility(assembly.compilerOptions.ApiCompatibilityLevel))
            {
                targetframeworkversion = "v4.7.1";
                targetLanguageVersion = "latest";

                cscToolPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts");
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    cscToolExe = "unity_csc.bat";
                else
                    cscToolExe = "unity_csc.sh";

                cscToolPath = Paths.UnifyDirectorySeparator(cscToolPath);
            }
            else if (_settings.VisualStudioVersion == 9)
            {
                toolsversion = "3.5";
                productversion = "9.0.21022";
            }

            var arguments = new object[]
            {
                toolsversion, productversion, ProjectGuid(assembly.name),
                _settings.EngineAssemblyPath,
                _settings.EditorAssemblyPath,
                string.Join(";", new[] { "DEBUG", "TRACE"}.Concat(assembly.defines).Concat(responseFilesData.SelectMany(x => x.Defines)).Distinct().ToArray()),
                MSBuildNamespaceUri,
                assembly.name,
                EditorSettings.projectGenerationRootNamespace,
                targetframeworkversion,
                targetLanguageVersion,
                baseDirectory,
                assembly.compilerOptions.AllowUnsafeCode | responseFilesData.Any(x => x.Unsafe),
                cscToolPath,
                cscToolExe,
            };

            try
            {
                return string.Format(_settings.GetProjectHeaderTemplate(language), arguments);
            }
            catch (Exception)
            {
                throw new System.NotSupportedException("Failed creating c# project because the c# project header did not have the correct amount of arguments, which is " + arguments.Length);
            }
        }

        private void SyncSolution(IEnumerable<Assembly> assemblies)
        {
            SyncSolutionFileIfNotChanged(SolutionFile(), SolutionText(assemblies));
        }

        private string SolutionText(IEnumerable<Assembly> assemblies)
        {
            var fileversion = "11.00";
            var vsversion = "2010";
            if (_settings.VisualStudioVersion == 9)
            {
                fileversion = "10.00";
                vsversion = "2008";
            }
            var relevantAssemblies = RelevantAssemblies(assemblies);
            string projectEntries = GetProjectEntries(relevantAssemblies);
            string projectConfigurations = string.Join(WindowsNewline, relevantAssemblies.Select(i => GetProjectActiveConfigurations(ProjectGuid(i.name))).ToArray());
            return string.Format(_settings.SolutionTemplate, fileversion, vsversion, projectEntries, projectConfigurations);
        }

        static IEnumerable<Assembly> RelevantAssemblies(IEnumerable<Assembly> assemblies)
        {
            return assemblies.Where(i => ScriptingLanguage.CSharp == ScriptingLanguageFor(i));
        }

        /// <summary>
        /// Get a Project("{guid}") = "MyProject", "MyProject.unityproj", "{projectguid}"
        /// entry for each relevant language
        /// </summary>
        internal string GetProjectEntries(IEnumerable<Assembly> assemblies)
        {
            var projectEntries = assemblies.Select(i => string.Format(
                DefaultSynchronizationSettings.SolutionProjectEntryTemplate,
                SolutionGuid(i), i.name, Path.GetFileName(ProjectFile(i)), ProjectGuid(i.name)
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
            var projectDir = _projectDirectory.Replace('/', '\\');
            file = file.Replace('/', '\\');
            var path = SkipPathPrefix(file, projectDir);

            var packageInfo = AssemblyNameProvider.FindForAssetPath(path.Replace('\\', '/'));
            if (packageInfo != null)
            {
                // We have to normalize the path, because the PackageManagerRemapper assumes
                // dir seperators will be os specific.
                var absolutePath = Path.GetFullPath(NormalizePath(path)).Replace('/', '\\');
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

        string ProjectGuid(string assemblyName)
        {
            return SolutionGuidGenerator.GuidForProject(_projectName + assemblyName);
        }

        string SolutionGuid(Assembly assembly)
        {
            return SolutionGuidGenerator.GuidForSolution(_projectName, GetExtensionOfSourceFiles(assembly.sourceFiles));
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
