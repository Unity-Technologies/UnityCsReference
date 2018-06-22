// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Hardware;
using UnityEditorInternal;
using UnityEngine;
using Unity.DataContract;
using UnityEditor.Callbacks;
using UnityEditor.Utils;
using UnityEditor.DeploymentTargets;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Modules
{
    internal static class ModuleManager
    {
        [NonSerialized]
        static List<IPlatformSupportModule> s_PlatformModules;

        [NonSerialized]
        static bool s_PlatformModulesInitialized;

        [NonSerialized]
        static List<IEditorModule> s_EditorModules;

        [NonSerialized]
        static IPackageManagerModule s_PackageManager;

        [NonSerialized]
        static IPlatformSupportModule s_ActivePlatformModule;

        internal static bool EnableLogging
        {
            get
            {
                return (bool)(Debug.GetDiagnosticSwitch("ModuleManagerLogging") ?? false);
            }
        }

        internal static IPackageManagerModule packageManager
        {
            get
            {
                InitializeModuleManager();
                return s_PackageManager;
            }
        }

        internal static IEnumerable<IPlatformSupportModule> platformSupportModules
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
        internal static IEnumerable<IPlatformSupportModule> platformSupportModulesDontRegister
        {
            get
            {
                if (s_PlatformModules == null)
                    return new List<IPlatformSupportModule>();
                return s_PlatformModules;
            }
        }

        static List<IEditorModule> editorModules
        {
            get
            {
                if (s_EditorModules == null)
                    return new List<IEditorModule>();
                return s_EditorModules;
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                {
                    s_ActivePlatformModule = module;
                    module.OnActivate();
                    return;
                }
            }

            // There are still some unmodularized platforms, so we can't throw yet
            //throw new ApplicationException("Couldn't find platform module for target: " + target);
        }

        internal static bool IsRegisteredModule(string file)
        {
            return (s_PackageManager != null && s_PackageManager.GetType().Assembly.Location.NormalizePath() == file.NormalizePath());
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static bool IsPlatformSupportLoaded(string target)
        {
            foreach (var module in platformSupportModules)
                if (module.TargetName == target)
                    return true;

            return false;
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static void RegisterAdditionalUnityExtensions()
        {
            foreach (var module in platformSupportModules)
            {
                module.RegisterAdditionalUnityExtensions();
            }
        }

        // entry point from native
        [RequiredByNativeCode]
        internal static void InitializeModuleManager()
        {
            if (s_PackageManager == null)
            {
                RegisterPackageManager();
                if (s_PackageManager != null)
                    LoadUnityExtensions();
                else
                    Debug.LogError("Failed to load package manager");
            }
        }

        static string CombinePaths(params string[] paths)
        {
            if (null == paths)
                throw new ArgumentNullException("paths");
            if (1 == paths.Length)
                return paths[0];

            StringBuilder builder = new StringBuilder(paths[0]);
            for (int i = 1; i < paths.Length; ++i)
                builder.AppendFormat("{0}{1}", "/", paths[i]);
            return builder.ToString();
        }

        private static void LoadUnityExtensions()
        {
            foreach (Unity.DataContract.PackageInfo extension in s_PackageManager.unityExtensions)
            {
                if (EnableLogging)
                    Console.WriteLine("Setting {0} v{1} for Unity v{2} to {3}", extension.name, extension.version, extension.unityVersion, extension.basePath);
                foreach (var file in extension.files.Where(f => f.Value.type == PackageFileType.Dll))
                {
                    string fullPath = Paths.NormalizePath(Path.Combine(extension.basePath, file.Key));
                    if (!File.Exists(fullPath))
                        Debug.LogWarningFormat("Missing assembly \t{0} for {1}. Extension support may be incomplete.", file.Key, extension.name);
                    else
                    {
                        bool isExtension = !String.IsNullOrEmpty(file.Value.guid);
                        if (EnableLogging)
                            Console.WriteLine("  {0} ({1}) GUID: {2}",
                                file.Key,
                                isExtension ? "Extension" : "Custom",
                                file.Value.guid);
                        if (isExtension)
                            InternalEditorUtility.RegisterExtensionDll(fullPath.Replace('\\', '/'), file.Value.guid);
                        else
                            InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileName(fullPath), fullPath);
                    }
                }
                s_PackageManager.LoadPackage(extension);
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
            RegisterPlatformSupportModules();

            foreach (var module in platformSupportModules)
            {
                foreach (var library in module.NativeLibraries)
                    EditorUtility.LoadPlatformSupportNativeLibrary(library);
                foreach (var fullPath in module.AssemblyReferencesForUserScripts)
                    InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileName(fullPath), fullPath);

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
                foreach (var module in s_PlatformModules)
                    module.OnUnload();
            }
        }

        // entry point from native
        [RequiredByNativeCode(true)]
        internal static void ShutdownModuleManager()
        {
            if (s_PackageManager != null)
                s_PackageManager.Shutdown(true);

            s_PackageManager = null;
            s_PlatformModules = null;
            s_EditorModules = null;
        }

        static void RegisterPackageManager()
        {
            s_EditorModules = new List<IEditorModule>();  // TODO: no editor modules support for now, so just cache this

            // if this is a domain reload after unity is already running, then package manager should already be loaded from
            // the native land, so we don't go looking for it again and just use the assembly already loaded
            try
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => null != a.GetType("Unity.PackageManager.PackageManager"));
                if (assembly != null)
                {
                    if (InitializePackageManager(assembly, null))
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enumerating assemblies looking for package manager. {0}", ex);
            }


            // check if locator assembly is in the domain (it should be loaded along with UnityEditor/UnityEngine at this point)
            Type locatorType = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name ==  "Unity.Locator").Select(a => a.GetType("Unity.PackageManager.Locator")).FirstOrDefault();
            try
            {
                string playbackEngineFolders = FileUtil.CombinePaths(Directory.GetParent(EditorApplication.applicationPath).ToString(), "PlaybackEngines");
                locatorType.InvokeMember("Scan", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null,
                    new object[] {
                    new string[] {
                        FileUtil.NiceWinPath(EditorApplication.applicationContentsPath),
                        FileUtil.NiceWinPath(playbackEngineFolders)
                    },
                    Application.unityVersion
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scanning for packages. {0}", ex);
                return;
            }

            Unity.DataContract.PackageInfo package;
            // get the package manager package
            try
            {
                package = locatorType.InvokeMember("GetPackageManager", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new[] { Application.unityVersion }) as Unity.DataContract.PackageInfo;
                if (package == null)
                {
                    Console.WriteLine("No package manager found!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error scanning for packages. {0}", ex);
                return;
            }

            try
            {
                InitializePackageManager(package);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing package manager. {0}", ex);
            }


            // this will only happen when unity first starts up
            if (s_PackageManager != null)
                s_PackageManager.CheckForUpdates();
        }

        // instantiate package manager and add it to the native assembly list for automatic loading at domain reloads (play mode start, etc)
        static bool InitializePackageManager(Unity.DataContract.PackageInfo package)
        {
            string dll = package.files.Where(x => x.Value.type == PackageFileType.Dll).Select(x => x.Key).FirstOrDefault();
            if (dll == null || !File.Exists(Path.Combine(package.basePath, dll)))
                return false;
            InternalEditorUtility.SetPlatformPath(package.basePath);
            Assembly assembly = InternalEditorUtility.LoadAssemblyWrapper(Path.GetFileName(dll), Path.Combine(package.basePath, dll));
            return InitializePackageManager(assembly, package);
        }

        static bool InitializePackageManager(Assembly assembly, Unity.DataContract.PackageInfo package)
        {
            s_PackageManager = AssemblyHelper.FindImplementors<IPackageManagerModule>(assembly).FirstOrDefault();

            if (s_PackageManager == null)
                return false;

            string dllpath = assembly.Location;

            // if we have a package, it's because it came from the locator, which means we need to setup the dll
            // for loading on the next domain reloads
            if (package != null)
                InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileName(dllpath), dllpath);

            else // just set the package with the path to the loaded assembly so package manager can get its information from there
                package = new Unity.DataContract.PackageInfo() { basePath = Path.GetDirectoryName(dllpath) };

            s_PackageManager.moduleInfo = package;
            s_PackageManager.editorInstallPath = EditorApplication.applicationContentsPath;
            s_PackageManager.unityVersion = new PackageVersion(Application.unityVersion);

            s_PackageManager.Initialize();
            foreach (Unity.DataContract.PackageInfo engine in s_PackageManager.playbackEngines)
            {
                BuildTargetGroup buildTargetGroup;
                BuildTarget target;
                if (!TryParseBuildTarget(engine.name, out buildTargetGroup, out target))
                    continue;

                if (EnableLogging)
                    Console.WriteLine("Setting {4}:{0} v{1} for Unity v{2} to {3}", target, engine.version, engine.unityVersion, engine.basePath, buildTargetGroup);
                foreach (var file in engine.files.Where(f => f.Value.type == PackageFileType.Dll))
                {
                    string fullPath = Paths.NormalizePath(Path.Combine(engine.basePath, file.Key));
                    if (!File.Exists(fullPath))
                        Debug.LogWarningFormat("Missing assembly \t{0} for {1}. Player support may be incomplete.", engine.basePath, engine.name);
                    else
                        InternalEditorUtility.RegisterPrecompiledAssembly(Path.GetFileName(dllpath), dllpath);
                }
                BuildPipeline.SetPlaybackEngineDirectory(buildTargetGroup, target, BuildOptions.None /* TODO */, engine.basePath);
                InternalEditorUtility.SetPlatformPath(engine.basePath);
                s_PackageManager.LoadPackage(engine);
            }
            return true;
        }

        static bool TryParseBuildTarget(string targetString, out BuildTargetGroup buildTargetGroup, out BuildTarget target)
        {
            buildTargetGroup = BuildTargetGroup.Standalone;
            target = BuildTarget.StandaloneWindows;
            try
            {
                if (targetString == BuildTargetGroup.Facebook.ToString())
                {
                    buildTargetGroup = BuildTargetGroup.Facebook;
                    target = BuildTarget.StandaloneWindows;
                }
                else
                {
                    target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetString);
                    buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
                }
                return true;
            }
            catch
            {
                Debug.LogWarning(string.Format("Couldn't find build target for {0}", targetString));
            }
            return false;
        }

        static void RegisterPlatformSupportModules()
        {
            if (s_PlatformModules != null)
            {
                Console.WriteLine("Modules already registered, not loading");
                return;
            }
            Console.WriteLine("Registering platform support modules:");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            s_PlatformModules = RegisterModulesFromLoadedAssemblies<IPlatformSupportModule>(RegisterPlatformSupportModulesFromAssembly).ToList();

            stopwatch.Stop();
            Console.WriteLine("Registered platform support modules in: " +  stopwatch.Elapsed.TotalSeconds + "s.");
        }

        static IEnumerable<T> RegisterModulesFromLoadedAssemblies<T>(Func<Assembly, IEnumerable<T>> processAssembly)
        {
            if (processAssembly == null)
                throw new ArgumentNullException("processAssembly");

            return AppDomain.CurrentDomain.GetAssemblies().Aggregate(new List<T>(),
                delegate(List<T> list, Assembly assembly) {
                    try
                    {
                        var modules = processAssembly(assembly);
                        if (modules != null && modules.Any())
                            list.AddRange(modules);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while registering modules from " + assembly.FullName + ": " + ex.Message);
                    }
                    return list;
                });
        }

        internal static IEnumerable<IPlatformSupportModule> RegisterPlatformSupportModulesFromAssembly(Assembly assembly)
        {
            return AssemblyHelper.FindImplementors<IPlatformSupportModule>(assembly);
        }

        static IEnumerable<IEditorModule> RegisterEditorModulesFromAssembly(Assembly assembly)
        {
            return AssemblyHelper.FindImplementors<IEditorModule>(assembly);
        }

        internal static List<string> GetJamTargets()
        {
            List<string> jamTargets = new List<string>();

            foreach (var module in platformSupportModules)
            {
                jamTargets.Add(module.JamTarget);
            }

            return jamTargets;
        }

        internal static IPlatformSupportModule FindPlatformSupportModule(string moduleName)
        {
            foreach (var module in platformSupportModules)
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateUserAssembliesValidatorExtension();
            }

            return null;
        }

        internal static IBuildPostprocessor GetBuildPostProcessor(string target)
        {
            if (target == null)
                return null;

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateBuildPostprocessor();
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateDeploymentTargetsExtension();
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
            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateBuildAnalyzer();
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateSettingsEditorExtension();
            }

            return null;
        }

        internal static ITextureImportSettingsExtension GetTextureImportSettingsExtension(BuildTarget target)
        {
            return GetTextureImportSettingsExtension(GetTargetStringFromBuildTarget(target));
        }

        internal static ITextureImportSettingsExtension GetTextureImportSettingsExtension(string targetName)
        {
            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == targetName)
                    return module.CreateTextureImportSettingsExtension();
            }

            return new DefaultTextureImportSettingsExtension();
        }

        internal static List<IPreferenceWindowExtension> GetPreferenceWindowExtensions()
        {
            List<IPreferenceWindowExtension> prefWindExtensions = new List<IPreferenceWindowExtension>();

            foreach (var module in platformSupportModules)
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateBuildWindowExtension();
            }

            return null;
        }

        internal static ICompilationExtension GetCompilationExtension(string target)
        {
            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateCompilationExtension();
            }

            return new DefaultCompilationExtension();
        }

        private static IScriptingImplementations GetScriptingImplementations(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreateScriptingImplementations();
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

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.CreatePluginImporterExtension();
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

        // This is for the smooth transition to future generic target names without subtargets
        // This has to match IPlatformSupportModule.TargetName - not sure how this improves modularity...
        // ADD_NEW_PLATFORM_HERE
        internal static string GetTargetStringFromBuildTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.iOS: return "iOS";
                case BuildTarget.tvOS: return "tvOS";
                case BuildTarget.XboxOne: return "XboxOne";
                case BuildTarget.WSAPlayer: return "Metro";
                case BuildTarget.Tizen: return "Tizen";
                case BuildTarget.PSP2: return "PSP2";
                case BuildTarget.PSM: return "PSM";
                case BuildTarget.PS4: return "PS4";
                case BuildTarget.WiiU: return "WiiU";
                case BuildTarget.WebGL: return "WebGL";
                case BuildTarget.Android: return "Android";
                case BuildTarget.N3DS: return "N3DS";
                case BuildTarget.Switch: return "Switch";
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return "LinuxStandalone";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "WindowsStandalone";
                case BuildTarget.StandaloneOSX:
                    // Deprecated
#pragma warning disable 612, 618
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
#pragma warning restore 612, 618
                    return "OSXStandalone";
                default: return null;
            }
        }

        // This is for the smooth transition to future generic target names without subtargets
        internal static string GetTargetStringFromBuildTargetGroup(BuildTargetGroup target)
        {
            // ADD_NEW_PLATFORM_HERE
            switch (target)
            {
                case BuildTargetGroup.iOS: return "iOS";
                case BuildTargetGroup.tvOS: return "tvOS";
                case BuildTargetGroup.XboxOne: return "XboxOne";
                case BuildTargetGroup.WSA: return "Metro";
                case BuildTargetGroup.Tizen: return "Tizen";
                case BuildTargetGroup.PSP2: return "PSP2";
                case BuildTargetGroup.PSM: return "PSM";
                case BuildTargetGroup.PS4: return "PS4";
                case BuildTargetGroup.WiiU: return "WiiU";
                case BuildTargetGroup.WebGL: return "WebGL";
                case BuildTargetGroup.Android: return "Android";
                case BuildTargetGroup.N3DS: return "N3DS";
                case BuildTargetGroup.Facebook: return "Facebook";
                case BuildTargetGroup.Switch: return "Switch";
                default: return null;
            }
        }

        // This function returns module name depending on the combination of targetGroup x target
        internal static string GetTargetStringFrom(BuildTargetGroup targetGroup, BuildTarget target)
        {
            if (targetGroup == BuildTargetGroup.Unknown)
                throw new ArgumentException("targetGroup must be valid");
            switch (targetGroup)
            {
                case BuildTargetGroup.Facebook:
                    return "Facebook";
                case BuildTargetGroup.Standalone:
                    return GetTargetStringFromBuildTarget(target);
                default:
                    return GetTargetStringFromBuildTargetGroup(targetGroup);
            }
        }

        internal static bool IsPlatformSupported(BuildTarget target)
        {
            return GetTargetStringFromBuildTarget(target) != null;
        }

        internal static bool HaveLicenseForBuildTarget(string targetString)
        {
            BuildTargetGroup buildTargetGroup;
            BuildTarget target;
            if (!TryParseBuildTarget(targetString, out buildTargetGroup, out target))
                return false;
            return BuildPipeline.LicenseCheck(target);
        }

        internal static string GetExtensionVersion(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.ExtensionVersion;
            }

            return null;
        }

        internal static bool ShouldShowMultiDisplayOption()
        {
            GUIContent[] platformDisplayNames = Modules.ModuleManager.GetDisplayNames(EditorUserBuildSettings.activeBuildTarget.ToString());
            return (BuildTargetGroup.Standalone == BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)) ||
                (BuildTargetGroup.WSA == BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)) || (platformDisplayNames != null);
        }

        internal static GUIContent[] GetDisplayNames(string target)
        {
            if (string.IsNullOrEmpty(target))
                return null;

            foreach (var module in platformSupportModules)
            {
                if (module.TargetName == target)
                    return module.GetDisplayNames();
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

            foreach (var module in ModuleManager.platformSupportModulesDontRegister)
                references.AddRange(module.AssemblyReferencesForUserScripts);

            return references.ToArray();
        }

        internal static string[] GetAdditionalReferencesForEditorCsharpProject()
        {
            var references = new List<string>();

            foreach (var module in ModuleManager.platformSupportModulesDontRegister)
                references.AddRange(module.AssemblyReferencesForEditorCsharpProject);

            return references.ToArray();
        }
    }
}
