// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Profiling;
using UnityEditor.Hardware;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.DeploymentTargets;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.Build;

namespace UnityEditor.Modules
{
    internal static class ModuleManager
    {
        private static readonly ProfilerMarkerWithStringData s_InitializePlatformSupportModule = ProfilerMarkerWithStringData.Create("InitializePlatformSupportModule", "Name");

        [NonSerialized]
        static Dictionary<string, IPlatformSupportModule> s_PlatformModules;

        [NonSerialized]
        static bool s_PlatformModulesInitialized;

        [NonSerialized]
        static IPlatformSupportModule s_ActivePlatformModule;

        internal static Dictionary<string, IPlatformSupportModule> platformSupportModules
        {
            get
            {
                if (s_PlatformModules == null)
                    RegisterPlatformSupportModules();
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

            foreach (var module in platformSupportModules.Values)
            {
                using (s_InitializePlatformSupportModule.Auto(module.TargetName))
                {
                    foreach (var library in module.NativeLibraries)
                        EditorUtility.LoadPlatformSupportNativeLibrary(library);
                    foreach (var fullPath in module.AssemblyReferencesForUserScripts)
                        InternalEditorUtility.RegisterPlatformModuleAssembly(Path.GetFileName(fullPath), fullPath);

                    EditorUtility.LoadPlatformSupportModuleNativeDllInternal(module.TargetName);

                    module.OnLoad();
                }
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

                try
                {
                    var platformSupportModule = Activator.CreateInstance(type) as IPlatformSupportModule;
                    s_PlatformModules.Add(platformSupportModule.TargetName, platformSupportModule);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Could not add platformSupportModule {type.FullName}, try rebuilding the module: {ex.Message}");
                }
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

        internal static IScriptingImplementations GetScriptingImplementations(NamedBuildTarget namedBuildTarget)
        {
            // Standalone Windows, Linux and OS X share player settings between each other, so they share scripting implementations too
            // However, since we can't pin BuildTargetGroup to any single platform support module, we have to explicitly check for this case
            if (namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
                return new DesktopStandalonePostProcessor.ScriptingImplementations();

            return GetScriptingImplementations(GetTargetStringFromBuildTargetGroup(namedBuildTarget.ToBuildTargetGroup()));
        }

        internal static IPluginImporterExtension GetPluginImporterExtension(string target)
        {
            if (target == null)
                return null;


            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                try
                {
                    return platformSupportModules[target].CreatePluginImporterExtension();
                }
                // Handle exception since otherwise creating extension from other platforms will also stop
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    return null;
                }
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
            return curPlatform == BuildTargetGroup.Standalone
                || curPlatform == BuildTargetGroup.WSA
                || curPlatform == BuildTargetGroup.iOS
                || curPlatform == BuildTargetGroup.Android
                || curPlatform == BuildTargetGroup.EmbeddedLinux
                || curPlatform == BuildTargetGroup.QNX
                || platformDisplayNames != null
                ;
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

        internal static IEditorAnalyticsExtension GetEditorAnalyticsExtension(string target)
        {
            IPlatformSupportModule module;
            if (platformSupportModules.TryGetValue(target, out module))
            {
                return module.GetEditorAnalyticsExtension();
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
