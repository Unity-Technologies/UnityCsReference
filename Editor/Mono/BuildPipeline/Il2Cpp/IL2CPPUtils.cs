// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor;
using UnityEditor.BuildReporting;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;
using PackageInfo = Unity.DataContract.PackageInfo;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UnityEditorInternal
{
    internal class IL2CPPUtils
    {
        public const string BinaryMetadataSuffix = "-metadata.dat";

        internal static IIl2CppPlatformProvider PlatformProviderForNotModularPlatform(BuildTarget target, bool developmentBuild)
        {
            throw new Exception("Platform unsupported, or already modular.");
        }

        internal static IL2CPPBuilder RunIl2Cpp(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry, bool debugBuild)
        {
            var builder = new IL2CPPBuilder(tempFolder, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, debugBuild, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.Run();
            return builder;
        }

        internal static IL2CPPBuilder RunIl2Cpp(string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry, bool debugBuild)
        {
            var builder = new IL2CPPBuilder(stagingAreaData, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, debugBuild, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.Run();
            return builder;
        }

        internal static IL2CPPBuilder RunCompileAndLink(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry, bool debugBuild)
        {
            var builder = new IL2CPPBuilder(tempFolder, stagingAreaData, platformProvider, modifyOutputBeforeCompile, runtimeClassRegistry, debugBuild, IL2CPPUtils.UseIl2CppCodegenWithMonoBackend(BuildPipeline.GetBuildTargetGroup(platformProvider.target)));
            builder.RunCompileAndLink();
            return builder;
        }

        internal static void CopyEmbeddedResourceFiles(string tempFolder, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "Resources")).Where(f => f.EndsWith("-resources.dat")))
                File.Copy(file, Paths.Combine(destinationFolder, Path.GetFileName(file)), true);
        }

        internal static void CopySymmapFile(string tempFolder, string destinationFolder)
        {
            CopySymmapFile(tempFolder, destinationFolder, string.Empty);
        }

        internal static void CopySymmapFile(string tempFolder, string destinationFolder, string destinationFileNameSuffix)
        {
            const string fileName = "SymbolMap";
            var file = Path.Combine(tempFolder, fileName);
            if (File.Exists(file))
                File.Copy(file, Path.Combine(destinationFolder, fileName + destinationFileNameSuffix), true);
        }

        internal static void CopyMetadataFiles(string tempFolder, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "Metadata")).Where(f => f.EndsWith(BinaryMetadataSuffix)))
                File.Copy(file, Paths.Combine(destinationFolder, Path.GetFileName(file)), true);
        }

        internal static void CopyConfigFiles(string tempFolder, string destinationFolder)
        {
            var sourceFolder = Paths.Combine(IL2CPPBuilder.GetCppOutputPath(tempFolder), "Data", "etc");
            FileUtil.CopyDirectoryRecursive(sourceFolder, destinationFolder);
        }

        internal static string editorIl2cppFolder
        {
            get
            {
                var dir = @"il2cpp";
                return Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, dir));
            }
        }

        internal static string ApiCompatibilityLevelToDotNetProfileArgument(ApiCompatibilityLevel compatibilityLevel)
        {
            switch (compatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_2_0_Subset:
                    return "legacyunity";

                case ApiCompatibilityLevel.NET_2_0:
                    return "net20";

                case ApiCompatibilityLevel.NET_4_6:
                    return "net45";

                default:
                    throw new NotSupportedException(string.Format("ApiCompatibilityLevel.{0} is not supported by IL2CPP!", compatibilityLevel));
            }
        }

        internal static bool UseIl2CppCodegenWithMonoBackend(BuildTargetGroup targetGroup)
        {
            return EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest &&
                EditorApplication.useLibmonoBackendForIl2cpp &&
                PlayerSettings.GetScriptingBackend(targetGroup) == ScriptingImplementation.IL2CPP;
        }
    }

    internal class IL2CPPBuilder
    {
        private readonly string m_TempFolder;
        private readonly string m_StagingAreaData;
        private readonly IIl2CppPlatformProvider m_PlatformProvider;
        private readonly Action<string> m_ModifyOutputBeforeCompile;
        private readonly RuntimeClassRegistry m_RuntimeClassRegistry;
        private readonly bool m_DebugBuild;
        private readonly LinkXmlReader m_linkXmlReader = new LinkXmlReader();
        private readonly bool m_BuildForMonoRuntime;

        public IL2CPPBuilder(string tempFolder, string stagingAreaData, IIl2CppPlatformProvider platformProvider, Action<string> modifyOutputBeforeCompile, RuntimeClassRegistry runtimeClassRegistry, bool debugBuild, bool buildForMonoRuntime)
        {
            m_TempFolder = tempFolder;
            m_StagingAreaData = stagingAreaData;
            m_PlatformProvider = platformProvider;
            m_ModifyOutputBeforeCompile = modifyOutputBeforeCompile;
            m_RuntimeClassRegistry = runtimeClassRegistry;
            m_DebugBuild = debugBuild;
            m_BuildForMonoRuntime = buildForMonoRuntime;
        }

        public void Run()
        {
            var outputDirectory = GetCppOutputDirectoryInStagingArea();
            var managedDir = Path.GetFullPath(Path.Combine(m_StagingAreaData, "Managed"));

            // Make all assemblies in Staging/Managed writable for stripping.
            foreach (var file in Directory.GetFiles(managedDir))
            {
                var fileInfo = new FileInfo(file);
                fileInfo.IsReadOnly = false;
            }

            AssemblyStripper.StripAssemblies(m_StagingAreaData, m_PlatformProvider, m_RuntimeClassRegistry);

            // The IL2CPP editor integration here is responsible to give il2cpp.exe an empty directory to use.
            FileUtil.CreateOrCleanDirectory(outputDirectory);

            if (m_ModifyOutputBeforeCompile != null)
                m_ModifyOutputBeforeCompile(outputDirectory);

            ConvertPlayerDlltoCpp(GetUserAssembliesToConvert(managedDir), outputDirectory, managedDir);

            var compiler = m_PlatformProvider.CreateNativeCompiler();
            if (compiler != null && m_PlatformProvider.CreateIl2CppNativeCodeBuilder() == null)
            {
                var nativeLibPath = OutputFileRelativePath();

                var includePaths = new List<string>(m_PlatformProvider.includePaths);
                includePaths.Add(outputDirectory);

                m_PlatformProvider.CreateNativeCompiler().CompileDynamicLibrary(
                    nativeLibPath,
                    NativeCompiler.AllSourceFilesIn(outputDirectory),
                    includePaths,
                    m_PlatformProvider.libraryPaths,
                    new string[0]);
            }
        }

        public void RunCompileAndLink()
        {
            var il2CppNativeCodeBuilder = m_PlatformProvider.CreateIl2CppNativeCodeBuilder();
            if (il2CppNativeCodeBuilder != null)
            {
                Il2CppNativeCodeBuilderUtils.ClearAndPrepareCacheDirectory(il2CppNativeCodeBuilder);

                var arguments = Il2CppNativeCodeBuilderUtils.AddBuilderArguments(il2CppNativeCodeBuilder, OutputFileRelativePath(), m_PlatformProvider.includePaths, m_DebugBuild).ToList();

                arguments.Add(string.Format("--map-file-parser=\"{0}\"", GetMapFileParserPath()));
                arguments.Add(string.Format("--generatedcppdir=\"{0}\"", Path.GetFullPath(GetCppOutputDirectoryInStagingArea())));
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_PlatformProvider.target);
                if (PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
                    arguments.Add("--dotnetprofile=\"net45\"");
                Action<ProcessStartInfo> setupStartInfo = il2CppNativeCodeBuilder.SetupStartInfo;
                var managedDir = Path.GetFullPath(Path.Combine(m_StagingAreaData, "Managed"));

                RunIl2CppWithArguments(arguments, setupStartInfo, managedDir);
            }
        }

        private string OutputFileRelativePath()
        {
            var nativeLibPath = Path.Combine(m_StagingAreaData, "Native");
            Directory.CreateDirectory(nativeLibPath);
            nativeLibPath = Path.Combine(nativeLibPath, m_PlatformProvider.nativeLibraryFileName);
            return nativeLibPath;
        }

        internal List<string> GetUserAssembliesToConvert(string managedDir)
        {
            var userAssemblies = GetUserAssemblies(managedDir);
            userAssemblies.Add(Directory.GetFiles(managedDir, "UnityEngine.dll", SearchOption.TopDirectoryOnly).Single());
            userAssemblies.UnionWith(FilterUserAssemblies(Directory.GetFiles(managedDir, "*.dll", SearchOption.TopDirectoryOnly), m_linkXmlReader.IsDLLUsed, managedDir));

            return userAssemblies.ToList();
        }

        private HashSet<string> GetUserAssemblies(string managedDir)
        {
            HashSet<string> userAssemblies = new HashSet<string>();
            userAssemblies.UnionWith(FilterUserAssemblies(m_RuntimeClassRegistry.GetUserAssemblies(), m_RuntimeClassRegistry.IsDLLUsed, managedDir));
            userAssemblies.UnionWith(FilterUserAssemblies(Directory.GetFiles(managedDir, "I18N*.dll", SearchOption.TopDirectoryOnly), (assembly) => true, managedDir));

            return userAssemblies;
        }

        private IEnumerable<string> FilterUserAssemblies(IEnumerable<string> assemblies, Predicate<string> isUsed, string managedDir)
        {
            return assemblies.Where(assembly => isUsed(assembly)).Select(usedAssembly => Path.Combine(managedDir, usedAssembly));
        }

        public string GetCppOutputDirectoryInStagingArea()
        {
            return GetCppOutputPath(m_TempFolder);
        }

        public static string GetCppOutputPath(string tempFolder)
        {
            return Path.Combine(tempFolder, "il2cppOutput");
        }

        public static string GetMapFileParserPath()
        {
            return Path.GetFullPath(
                Path.Combine(
                    EditorApplication.applicationContentsPath,
                    Application.platform == RuntimePlatform.WindowsEditor ? @"Tools\MapFileParser\MapFileParser.exe" : @"Tools/MapFileParser/MapFileParser"));
        }

        private void ConvertPlayerDlltoCpp(ICollection<string> userAssemblies, string outputDirectory, string workingDirectory)
        {
            if (userAssemblies.Count == 0)
                return;

            var arguments = new List<string>();

            arguments.Add("--convert-to-cpp");

            if (m_PlatformProvider.emitNullChecks)
                arguments.Add("--emit-null-checks");

            if (m_PlatformProvider.enableStackTraces)
                arguments.Add("--enable-stacktrace");

            if (m_PlatformProvider.enableArrayBoundsCheck)
                arguments.Add("--enable-array-bounds-check");

            if (m_PlatformProvider.enableDivideByZeroCheck)
                arguments.Add("--enable-divide-by-zero-check");

            if (m_PlatformProvider.developmentMode)
                arguments.Add("--development-mode");

            if (m_BuildForMonoRuntime)
                arguments.Add("--mono-runtime");

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_PlatformProvider.target);
            if (PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
                arguments.Add("--dotnetprofile=\"net45\"");

            var il2CppNativeCodeBuilder = m_PlatformProvider.CreateIl2CppNativeCodeBuilder();
            if (il2CppNativeCodeBuilder != null)
            {
                Il2CppNativeCodeBuilderUtils.ClearAndPrepareCacheDirectory(il2CppNativeCodeBuilder);
                arguments.AddRange(Il2CppNativeCodeBuilderUtils.AddBuilderArguments(il2CppNativeCodeBuilder, OutputFileRelativePath(), m_PlatformProvider.includePaths, m_DebugBuild));
            }

            arguments.Add(string.Format("--map-file-parser=\"{0}\"", GetMapFileParserPath()));

            var additionalArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            if (!string.IsNullOrEmpty(additionalArgs))
                arguments.Add(additionalArgs);

            additionalArgs = System.Environment.GetEnvironmentVariable("IL2CPP_ADDITIONAL_ARGS");
            if (!string.IsNullOrEmpty(additionalArgs))
            {
                arguments.Add(additionalArgs);
            }

            var pathArguments = new List<string>(userAssemblies);
            arguments.AddRange(pathArguments.Select(arg => "--assembly=\"" + Path.GetFullPath(arg) + "\""));

            arguments.Add(string.Format("--generatedcppdir=\"{0}\"", Path.GetFullPath(outputDirectory)));

            string progressMessage = "Converting managed assemblies to C++";
            if (il2CppNativeCodeBuilder != null)
            {
                progressMessage = "Building native binary with IL2CPP...";
            }

            if (EditorUtility.DisplayCancelableProgressBar("Building Player", progressMessage, 0.3f))
                throw new OperationCanceledException();

            Action<ProcessStartInfo> setupStartInfo = null;
            if (il2CppNativeCodeBuilder != null)
                setupStartInfo = il2CppNativeCodeBuilder.SetupStartInfo;

            RunIl2CppWithArguments(arguments, setupStartInfo, workingDirectory);
        }

        private void RunIl2CppWithArguments(List<string> arguments, Action<ProcessStartInfo> setupStartInfo, string workingDirectory)
        {
            var args = arguments.Aggregate(String.Empty, (current, arg) => current + arg + " ");

            var useNetCore = ShouldUseIl2CppCore();
            string il2CppPath = useNetCore ? GetIl2CppCoreExe() : GetIl2CppExe();

            Console.WriteLine("Invoking il2cpp with arguments: " + args);

            CompilerOutputParserBase il2cppOutputParser = m_PlatformProvider.CreateIl2CppOutputParser();
            if (il2cppOutputParser == null)
                il2cppOutputParser = new Il2CppOutputParser();

            if (useNetCore)
                Runner.RunNetCoreProgram(il2CppPath, args, workingDirectory, il2cppOutputParser, setupStartInfo);
            else
                Runner.RunManagedProgram(il2CppPath, args, workingDirectory, il2cppOutputParser, setupStartInfo);
        }

        private string GetIl2CppExe()
        {
            return m_PlatformProvider.il2CppFolder + "/build/il2cpp.exe";
        }

        private string GetIl2CppCoreExe()
        {
            return m_PlatformProvider.il2CppFolder + "/build/il2cppcore/il2cppcore.dll";
        }

        private bool ShouldUseIl2CppCore()
        {
            bool shouldUse = false;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // On OSX 10.8 (and mabybe older versions, not sure) running .NET Core will result in the following error :
                //          dyld: lazy symbol binding failed: Symbol not found: __sincos_stret
                //
                // I'm not sure exactly what the issue is, but based on some google searching it's an issue not unique to .NET Core
                // and it does not happen in 10.9 and later.
                //
                // Some of our graphics tests run on OSX 10.8 and some users may have 10.8, in order to keep 10.8 working
                // we will fallback to running il2cpp on mono.
                // And as a precaution, let's use il2cpp on mono for anything older than 10.8 as well
                if (SystemInfo.operatingSystem.StartsWith("Mac OS X 10."))
                {
                    var versionText = SystemInfo.operatingSystem.Substring(9);
                    var version = new Version(versionText);

                    // our version of .NET Core does not support high sierra.  We need to upgrade to .NET Core 2.0 before we can use il2cppcore
                    // on high sierra
                    if (version >= new Version(10, 9) && version < new Version(10, 13))
                        shouldUse = true;
                }
                else
                {
                    shouldUse = true;
                }
            }

            return shouldUse && NetCoreProgram.IsNetCoreAvailable();
        }
    }

    internal interface IIl2CppPlatformProvider
    {
        BuildTarget target { get; }
        bool emitNullChecks { get; }
        bool enableStackTraces { get; }
        bool enableArrayBoundsCheck { get; }
        bool enableDivideByZeroCheck { get; }
        string nativeLibraryFileName { get; }
        string il2CppFolder { get; }
        bool developmentMode { get; }
        string moduleStrippingInformationFolder { get; }
        bool supportsEngineStripping { get; }

        BuildReport buildReport { get; }
        string[] includePaths { get; }
        string[] libraryPaths { get; }

        INativeCompiler CreateNativeCompiler();
        Il2CppNativeCodeBuilder CreateIl2CppNativeCodeBuilder();
        CompilerOutputParserBase CreateIl2CppOutputParser();
    }

    internal class BaseIl2CppPlatformProvider : IIl2CppPlatformProvider
    {
        public BaseIl2CppPlatformProvider(BuildTarget target, string libraryFolder)
        {
            this.target = target;
            this.libraryFolder = libraryFolder;
        }

        public virtual BuildTarget target { get; private set; }

        public virtual string libraryFolder { get; private set; }

        public virtual bool developmentMode
        {
            get { return false; }
        }

        public virtual bool emitNullChecks
        {
            get { return true; }
        }

        public virtual bool enableStackTraces
        {
            get { return true; }
        }

        public virtual bool enableArrayBoundsCheck
        {
            get { return true; }
        }

        public virtual bool enableDivideByZeroCheck
        {
            get { return false; }
        }

        public virtual bool supportsEngineStripping
        {
            get { return false; }
        }

        public virtual BuildReport buildReport
        {
            get { return null; }
        }

        public virtual string[] includePaths
        {
            get
            {
                return new[] {
                    GetFolderInPackageOrDefault("bdwgc/include"),
                    GetFolderInPackageOrDefault("libil2cpp/include")
                };
            }
        }

        public virtual string[] libraryPaths
        {
            get
            {
                return new[] {
                    GetFileInPackageOrDefault("bdwgc/lib/bdwgc." + staticLibraryExtension),
                    GetFileInPackageOrDefault("libil2cpp/lib/libil2cpp." + staticLibraryExtension)
                };
            }
        }

        public virtual string nativeLibraryFileName
        {
            get { return null; }
        }

        public virtual string staticLibraryExtension
        {
            get { return "a"; }
        }

        public virtual string il2CppFolder
        {
            get
            {
                var il2CppPackage = FindIl2CppPackage();
                if (il2CppPackage == null)
                {
                    return Path.GetFullPath(Path.Combine(
                            EditorApplication.applicationContentsPath,
                            "il2cpp"));
                }

                return il2CppPackage.basePath;
            }
        }

        public virtual string moduleStrippingInformationFolder
        {
            get { return Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(EditorUserBuildSettings.activeBuildTarget, 0), "Whitelists"); }
        }

        public virtual INativeCompiler CreateNativeCompiler()
        {
            return null;
        }

        public virtual Il2CppNativeCodeBuilder CreateIl2CppNativeCodeBuilder()
        {
            return null;
        }

        public virtual CompilerOutputParserBase CreateIl2CppOutputParser()
        {
            return null;
        }

        protected string GetFolderInPackageOrDefault(string path)
        {
            var il2CppPackage = FindIl2CppPackage();
            if (il2CppPackage == null)
                return Path.Combine(libraryFolder, path);

            var folder = Path.Combine(il2CppPackage.basePath, path);
            return !Directory.Exists(folder) ? Path.Combine(libraryFolder, path) : folder;
        }

        protected string GetFileInPackageOrDefault(string path)
        {
            var il2CppPackage = FindIl2CppPackage();
            if (il2CppPackage == null)
                return Path.Combine(libraryFolder, path);

            var file = Path.Combine(il2CppPackage.basePath, path);
            return !File.Exists(file) ? Path.Combine(libraryFolder, path) : file;
        }

        private static PackageInfo FindIl2CppPackage()
        {
            return ModuleManager.packageManager.unityExtensions.FirstOrDefault(e => e.name == "IL2CPP");
        }
    }

    internal class LinkXmlReader
    {
        private readonly List<string> _assembliesInALinkXmlFile = new List<string>();

        public LinkXmlReader()
        {
            foreach (var linkXmlFile in AssemblyStripper.GetUserBlacklistFiles())
            {
                XPathDocument linkXml = new XPathDocument(linkXmlFile);
                var navigator = linkXml.CreateNavigator();
                navigator.MoveToFirstChild();
                var iterator = navigator.SelectChildren("assembly", string.Empty);

                while (iterator.MoveNext())
                    _assembliesInALinkXmlFile.Add(iterator.Current.GetAttribute("fullname", string.Empty));
            }
        }

        public bool IsDLLUsed(string assemblyFileName)
        {
            return _assembliesInALinkXmlFile.Contains(Path.GetFileNameWithoutExtension(assemblyFileName));
        }
    }
}
