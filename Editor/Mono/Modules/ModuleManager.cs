// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Hardware;
using UnityEditorInternal;
using UnityEngine;
using Unity.Profiling;
using UnityEditor.Utils;
using UnityEditor.DeploymentTargets;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Modules
{
    internal static class ModuleManager
    {
        [NonSerialized]
        static Dictionary<string, IPlatformSupportModule> s_PlatformModules;

        [NonSerialized]
        static bool s_PlatformModulesInitialized;

        [NonSerialized]
        static IPlatformSupportModule s_ActivePlatformModule;

        private const string k_IsCacheBuildKey = "ModuleManagerIsExtensionsRegistered";

        internal static bool EnableLogging
        {
            get
            {
                return (bool)(Debug.GetDiagnosticSwitch("ModuleManagerLogging") ?? false);
            }
        }

        internal static Dictionary<string, IPlatformSupportModule> platformSupportModules
        {
            get
            {
                InitializeModuleManager();
                if (s_PlatformModules == null)
                    RegisterPlatformSupportModules();
                return s_PlatformModules;
            }
        }


        // Accesses the list of currently known platformSupportModules without forcing platform
        // support module registration. Use this API from all code which may run during the initial
        // domain reloading phase instead of accessing platformSupportModules. Platform support
        // modules will be registered automatically when it is safe to do so.
        internal static Dictionary<string, IPlatformSupportModule> platformSupportModulesDontRegister
        {
            get
            {
                if (s_PlatformModules == null)
                    return new Dictionary<string, IPlatformSupportModule>();
                return s_PlatformModules;
            }
        }

        class BuildTargetChangedHandler : Build.IActiveBuildTargetChanged
        {
            public int callbackOrder { get { return 0; } }

            public void OnActiveBuildTargetChanged(BuildTarget oldTarget, BuildTarget newTarget)
            {
                ModuleManager.OnActiveBuildTargetChanged(oldTarget, newTarget);
            }
        }

        static void OnActiveBuildTargetChanged(BuildTarget oldTarget, BuildTarget newTarget)
        {
            string target = GetTargetStringFromBuildTarget(newTarget);
            ChangeActivePlatformModuleTo(target);
        }

        static void DeactivateActivePlatformModule()
        {
            if (s_ActivePlatformModule != null)
            {
                s_ActivePlatformModule.OnDeactivate();
                s_ActivePlatformModule = null;
            }
        }

        static void ChangeActivePlatformModuleTo(string target)
        {
            DeactivateActivePlatformModule();

            IPlatformSupportModule selected;
            if (platformSupportModules.TryGetValue(target, out selected))
            {
                s_ActivePlatformModule = selected;
                s_ActivePlatformModule.OnActivate();
            }
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static bool IsPlatformSupportLoaded(string target)
        {
            return platformSupportModules.ContainsKey(target);
        }

        // Native binding doesn't support overloaded functions
        internal static bool IsPlatformSupportLoadedByBuildTarget(BuildTarget target)
        {
            return IsPlatformSupportLoaded(GetTargetStringFromBuildTarget(target));
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static void RegisterAdditionalUnityExtensions()
        {
            foreach (var module in platformSupportModules)
            {
                module.Value.RegisterAdditionalUnityExtensions();
            }
        }

        [NonSerialized]
        static ProfilerMarker s_InitializeModuleManagerMarker = new ProfilerMarker("ModuleManager.InitializeModuleManager");

        // entry point from native
        [RequiredByNativeCode]
        internal static void InitializeModuleManager()
        {
            using (s_InitializeModuleManagerMarker.Auto())
            {
                RegisterPackageManager();
            }
        }

        // entry point from native
        // Note that in order for this function to work properly, it must be called between two domain
        // reloads. The first domain reload is needed because RegisterPlatformSupportModules()
        // investigates the currently loaded set of assemblies. The second reload is needed so that
        // assemblies returned by module.AssemblyReferencesForUserScripts are actually loaded in the
        // current domain and user code may use it.
        [RequiredByNativeCode]
        internal static void InitializePlatformSupportModules()
        {
            if (s_PlatformModulesInitialized)
            {
                Console.WriteLine("Platform modules already initialized, skipping");
                return;
            }

            InitializeModuleManager();
            foreach (var module in platformSupportModules.Values)
            {
                foreach (var library in module.NativeLibraries)
                    EditorUtility.LoadPlatformSupportNativeLibrary(library);
                foreach (var fullPath in module.AssemblyReferencesForUserScripts)
                    InternalEditorUtility.RegisterPlatformModuleAssembly(Path.GetFileName(fullPath), fullPath);

                EditorUtility.LoadPlatformSupportModuleNativeDllInternal(module.TargetName);

                module.OnLoad();
            }

            // Setup active build target and call OnActivate() for current platform module
            OnActiveBuildTargetChanged(BuildTarget.NoTarget, EditorUserBuildSettings.activeBuildTarget);

            s_PlatformModulesInitialized = true;
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static void ShutdownPlatformSupportModules()
        {
            DeactivateActivePlatformModule();

            if (s_PlatformModules != null)
            {
                foreach (var module in s_PlatformModules.Values)
                    module.OnUnload();
            }
        }

        // entry point from native
        [RequiredByNativeCode(true)]
        internal static void ShutdownModuleManager()
        {
            s_PlatformModules = null;
        }

        private static void RegisterPackageManager()
        {
            try
            {
                if (!SessionState.GetBool(k_IsCacheBuildKey, false))
                {
                    SessionState.SetBool(k_IsCacheBuildKey, true);
                    LoadLegacyExtensionsFromIvyFiles();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing extension manager. {0}", ex);
            }
        }

        class IvyPackageFileData
        {
            public string filename;
            public string type;
            public string guid;

            public bool IsDll => type.Equals("dll", StringComparison.InvariantCultureIgnoreCase);

            public static IvyPackageFileData CreateFromRegexMatch(Match match)
            {
                var filename = match.Groups["name"].Value;
                if (match.Groups["ext"].Success)
                    filename += "." + match.Groups["ext"].Value;

                return new IvyPackageFileData()
                {
                    filename = filename,
                    type = match.Groups["type"].Value,
                    guid = match.Groups["guid"].Value
                };
            }
        }

        static void LoadLegacyExtensionsFromIvyFiles()
        {
            //We can't use the cached native type scanner here since this is called to early for that to be built up.

            HashSet<string> ivyFiles;
            try
            {
                ivyFiles = new HashSet<string>();

                string unityExtensionsFolder = FileUtil.CombinePaths(Directory.GetParent(EditorApplication.applicationPath).ToString(), "Data", "UnityExtensions");
                string playbackEngineFolders = FileUtil.CombinePaths(Directory.GetParent(EditorApplication.applicationPath).ToString(), "PlaybackEngines");

                foreach (var searchPath in new[]
                     {
                         FileUtil.NiceWinPath(EditorApplication.applicationContentsPath),
                         FileUtil.NiceWinPath(unityExtensionsFolder),
                         FileUtil.NiceWinPath(playbackEngineFolders)
                     })
                {
                    if (!Directory.Exists(searchPath))
                        continue;

                    ivyFiles.UnionWith(Directory.GetFiles(searchPath, "ivy.xml", SearchOption.AllDirectories));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scanning for extension ivy.xml files: {0}", ex);
                return;
            }

            var packages = new Dictionary<string, List<IvyPackageFileData>>();

            var artifactRegex = new Regex(@"<artifact(\s+(name=""(?<name>[^""]*)""|type=""(?<type>[^""]*)""|ext=""(?<ext>[^""]*)""|e:guid=""(?<guid>[^""]*)""))+\s*/>",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            foreach (var ivyFile in ivyFiles)
            {
                try
                {
                    var ivyFileContent = File.ReadAllText(ivyFile);
                    var artifacts = artifactRegex.Matches(ivyFileContent).Cast<Match>()
                        .Select(IvyPackageFileData.CreateFromRegexMatch).ToList();

                    var packageDir = Path.GetDirectoryName(ivyFile);
                    packages.Add(packageDir, artifacts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading extensions from ivy.xml file at {0}: {1}", ivyFile, ex);
                }
            }

            try
            {
                foreach (var packageInfo in packages)
                {
                    var files = packageInfo.Value;
                    foreach (var packageInfoFile in files)
                    {
                        string fullPath = Paths.NormalizePath(Path.Combine(packageInfo.Key, packageInfoFile.filename));

                        if (!File.Exists(fullPath))
                            Debug.LogWarningFormat(
                                "Missing assembly \t{0} listed in ivy file {1}. Extension support may be incomplete.", fullPath,
                                packageInfo.Key);

                        if (!packageInfoFile.IsDll)
                            continue;

                        if (!string.IsNullOrEmpty(packageInfoFile.guid))
                        {
                            InternalEditorUtility.RegisterExtensionDll(fullPath.Replace('\\', '/'),
                                packageInfoFile.guid);
                        }
                        else
                        {
                            InternalEditorUtility.RegisterPrecompiledAssembly(fullPath, fullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scanning for extensions. {0}", ex);
            }
        }

        static bool TryParseBuildTarget(string targetString, out BuildTargetGroup buildTargetGroup, out BuildTarget target)
        {
            buildTargetGroup = BuildTargetGroup.Standalone;
            target = BuildTarget.StandaloneWindows;
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetString);
                buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
                return true;
            }
            catch
            {
                Debug.LogWarning(string.Format("Couldn't find build target for {0}", targetString));
            }
            return false;
        }

        private static void RegisterPlatformSupportModules()
        {
            var allTypesWithInterface = TypeCache.GetTypesDerivedFrom<IPlatformSupportModule>();
            s_PlatformModules = new Dictionary<string, IPlatformSupportModule>(allTypesWithInterface.Count);

            foreach (var type in allTypesWithInterface)
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                var platformSupportModule = Activator.CreateInstance(type) as IPlatformSupportModule;
                s_PlatformModules.Add(platformSupportModule.TargetName, platformSupportModule);
            }
        }

        internal static List<string> GetJamTargets()
        {
            List<string> jamTargets = new List<string>();

            foreach (var module in platformSupportModules.Values)
            {
                jamTargets.Add(module.JamTarget);
            }

            return jamTargets;
        }

        internal static IPlatformSupportModule FindPlatformSupportModule(string moduleName)
        {
            foreach (var module in platformSupportModules.Values)
                if (module.TargetName == moduleName)
                    return module;

            return null;
        }

        internal static IDevice GetDevice(string deviceId)
        {
            DevDevice device;
            if (DevDeviceList.FindDevice(deviceId, out device))
            {
                IPlatformSupportModule module = FindPlatformSupportModule(device.module);
                if (module != null)
                    return module.CreateDevice(deviceId);
                else
                    throw new ApplicationException("Couldn't find module for target: " + device.module);
            }

            throw new ApplicationException("Couldn't create device API for device: " + deviceId);
        }

        internal static IUserAssembliesValidator GetUserAssembliesValidator(string target)
        {
            if (target == null)
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateUserAssembliesValidatorExtension();
            }
            return null;
        }

        internal static IBuildPostprocessor GetBuildPostProcessor(string target)
        {
            if (target == null)
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateBuildPostprocessor();
            }

            return null;
        }

        internal static IBuildPostprocessor GetBuildPostProcessor(BuildTargetGroup targetGroup, BuildTarget target)
        {
            return GetBuildPostProcessor(GetTargetStringFrom(targetGroup, target));
        }

        internal static IDeploymentTargetsExtension GetDeploymentTargetsExtension(string target)
        {
            if (target == null)
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateDeploymentTargetsExtension();
            }

            return null;
        }

        internal static IDeploymentTargetsExtension GetDeploymentTargetsExtension(BuildTargetGroup targetGroup, BuildTarget target)
        {
            return GetDeploymentTargetsExtension(GetTargetStringFrom(targetGroup, target));
        }

        internal static IBuildAnalyzer GetBuildAnalyzer(string target)
        {
            if (target == null) return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateBuildAnalyzer();
            }

            return null;
        }

        internal static IBuildAnalyzer GetBuildAnalyzer(BuildTarget target)
        {
            return GetBuildAnalyzer(GetTargetStringFromBuildTarget(target));
        }

        internal static ISettingEditorExtension GetEditorSettingsExtension(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateSettingsEditorExtension();
            }

            return null;
        }

        internal static ITextureImportSettingsExtension GetTextureImportSettingsExtension(BuildTarget target)
        {
            return GetTextureImportSettingsExtension(GetTargetStringFromBuildTarget(target));
        }

        internal static ITextureImportSettingsExtension GetTextureImportSettingsExtension(string targetName)
        {
            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(targetName, out module))
            {
                return platformSupportModules[targetName].CreateTextureImportSettingsExtension();
            }

            return new DefaultTextureImportSettingsExtension();
        }

        internal static List<IPreferenceWindowExtension> GetPreferenceWindowExtensions()
        {
            List<IPreferenceWindowExtension> prefWindExtensions = new List<IPreferenceWindowExtension>();

            foreach (var module in platformSupportModules.Values)
            {
                IPreferenceWindowExtension prefWindowExtension = module.CreatePreferenceWindowExtension();

                if (prefWindowExtension != null)
                    prefWindExtensions.Add(prefWindowExtension);
            }
            return prefWindExtensions;
        }

        internal static IBuildWindowExtension GetBuildWindowExtension(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateBuildWindowExtension();
            }

            return null;
        }

        internal static ICompilationExtension GetCompilationExtension(string target)
        {
            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateCompilationExtension();
            }

            return new DefaultCompilationExtension();
        }

        private static IScriptingImplementations GetScriptingImplementations(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreateScriptingImplementations();
            }

            return null;
        }

        internal static IScriptingImplementations GetScriptingImplementations(BuildTargetGroup target)
        {
            // Standalone Windows, Linux and OS X share player settings between each other, so they share scripting implementations too
            // However, since we can't pin BuildTargetGroup to any single platform support module, we have to explicitly check for this case
            if (target == BuildTargetGroup.Standalone)
                return new DesktopStandalonePostProcessor.ScriptingImplementations();

            return GetScriptingImplementations(GetTargetStringFromBuildTargetGroup(target));
        }

        internal static IPluginImporterExtension GetPluginImporterExtension(string target)
        {
            if (target == null)
                return null;


            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].CreatePluginImporterExtension();
            }

            return null;
        }

        internal static IPluginImporterExtension GetPluginImporterExtension(BuildTarget target)
        {
            return GetPluginImporterExtension(GetTargetStringFromBuildTarget(target));
        }

        internal static IPluginImporterExtension GetPluginImporterExtension(BuildTargetGroup target)
        {
            return GetPluginImporterExtension(GetTargetStringFromBuildTargetGroup(target));
        }

        internal static string GetTargetStringFromBuildTarget(BuildTarget target)
        {
            return BuildTargetDiscovery.GetModuleNameForBuildTarget(target);
        }

        internal static string GetTargetStringFromBuildTargetGroup(BuildTargetGroup target)
        {
            return BuildTargetDiscovery.GetModuleNameForBuildTargetGroup(target);
        }

        // This function returns module name depending on the combination of targetGroup x target
        internal static string GetTargetStringFrom(BuildTargetGroup targetGroup, BuildTarget target)
        {
            if (targetGroup == BuildTargetGroup.Unknown)
                throw new ArgumentException("targetGroup must be valid");

            if (targetGroup == BuildTargetGroup.Standalone)
                return GetTargetStringFromBuildTarget(target);

            return GetTargetStringFromBuildTargetGroup(targetGroup);
        }

        internal static bool IsPlatformSupported(BuildTarget target)
        {
            return GetTargetStringFromBuildTarget(target) != null;
        }

        internal static string GetExtensionVersion(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].ExtensionVersion;
            }

            return null;
        }

        internal static bool ShouldShowMultiDisplayOption()
        {
            GUIContent[] platformDisplayNames = Modules.ModuleManager.GetDisplayNames(EditorUserBuildSettings.activeBuildTarget.ToString());
            BuildTargetGroup curPlatform = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return curPlatform == BuildTargetGroup.Standalone || curPlatform == BuildTargetGroup.WSA || curPlatform == BuildTargetGroup.iOS || curPlatform == BuildTargetGroup.Android
                || platformDisplayNames != null;
        }

        internal static GUIContent[] GetDisplayNames(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return platformSupportModules[target].GetDisplayNames();
            }

            return null;
        }
    }

    internal static class ModuleUtils
    {
        // entry point from native
        internal static string[] GetAdditionalReferencesForUserScripts()
        {
            var references = new List<string>();

            foreach (var module in ModuleManager.platformSupportModules.Values)
                references.AddRange(module.AssemblyReferencesForUserScripts);

            return references.ToArray();
        }

        internal static string[] GetAdditionalReferencesForEditorCsharpProject()
        {
            var references = new List<string>();

            foreach (var module in ModuleManager.platformSupportModules.Values)
                references.AddRange(module.AssemblyReferencesForEditorCsharpProject);

            return references.ToArray();
        }
    }
}
