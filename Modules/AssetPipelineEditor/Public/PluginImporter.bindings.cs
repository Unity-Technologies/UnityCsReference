// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Callbacks;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/PluginImporter.h")]
    [ExcludeFromPreset]
    public sealed partial class PluginImporter : AssetImporter
    {
        [NativeMethod("GetCompatibleWithPlatformOrAnyPlatform")]
        extern internal bool GetCompatibleWithPlatformOrAnyPlatformBuildTarget(string buildTarget);

        [NativeMethod("GetCompatibleWithPlatformOrAnyPlatform")]
        extern private bool GetCompatibleWithPlatformOrAnyPlatformBuildGroupAndTarget(string buildTargetGroup, string buildTarget);

        [NativeProperty("IsExplicitlyReferenced")]
        internal extern bool IsExplicitlyReferenced { get; set; }

        [NativeProperty("ValidateReferences")]
        internal extern bool ValidateReferences { get; set;}

        [NativeProperty("DefineConstraints")]
        public extern string[] DefineConstraints { get; set; }

        public static PluginImporter[] GetImporters(string platformName)
        {
            // The final list of importers that will be returned
            List<PluginImporter> finalImporters = new List<PluginImporter>();

            // Contains all unique finalPaths. Used to remove overridable plugins from the finalImporters list
            Dictionary<string, PluginImporter> uniqueFinalPathToImporterMap = new Dictionary<string, PluginImporter>();

            PluginImporter[] allImporters = GetAllImporters().Where(imp => imp.GetCompatibleWithPlatformOrAnyPlatformBuildTarget(platformName)).ToArray();
            IPluginImporterExtension pluginImporterExtension = ModuleManager.GetPluginImporterExtension(platformName);

            if (pluginImporterExtension == null)
                pluginImporterExtension = ModuleManager.GetPluginImporterExtension(BuildPipeline.GetBuildTargetByName(platformName));

            if (pluginImporterExtension == null)
                return allImporters;

            // Go through all of the Importers for the specified platform and determine if any of the natively included Unity Plugins should be removed.
            // Removal should only happen if the user has included a plugin of the same name and they are copying to the same finalPath
            for (int i = 0; i < allImporters.Length; ++i)
            {
                PluginImporter currentImporter = allImporters[i];
                string finalPluginPath = pluginImporterExtension.CalculateFinalPluginPath(platformName, currentImporter);

                // Only compare overridables if the plugin has a calculated finalPluginPath
                if (!string.IsNullOrEmpty(finalPluginPath))
                {
                    PluginImporter tempImporter;
                    if (!uniqueFinalPathToImporterMap.TryGetValue(finalPluginPath, out tempImporter))
                    {
                        // Unique Plugin found, add it to the list for comparing overridable plugins
                        // The first overridable plugin of it's kind should be added to the return list here.
                        uniqueFinalPathToImporterMap.Add(finalPluginPath, currentImporter);
                    }
                    else if (tempImporter.GetIsOverridable() && !currentImporter.GetIsOverridable())
                    {
                        // finalPluginPath isn't unique and the finalImporter already in the list is overriden by the new one,
                        // remove the overridable one.
                        uniqueFinalPathToImporterMap[finalPluginPath] = currentImporter;
                        finalImporters.Remove(tempImporter);
                    }
                    else if (currentImporter.GetIsOverridable())
                    {
                        // The current importer is going to the same final location as another, but
                        // this plugin is overridable, so don't include it.
                        continue;
                    }
                }

                finalImporters.Add(currentImporter);
            }

            return finalImporters.ToArray();
        }

        public static PluginImporter[] GetImporters(BuildTarget platform)
        {
            return GetImporters(BuildPipeline.GetBuildTargetName(platform));
        }

        public static PluginImporter[] GetImporters(string buildTargetGroup, string buildTarget)
        {
            return GetAllImporters().Where(imp => imp.GetCompatibleWithPlatformOrAnyPlatformBuildGroupAndTarget(buildTargetGroup, buildTarget)).ToArray();
        }

        public static PluginImporter[] GetImporters(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            return GetImporters(BuildPipeline.GetBuildTargetGroupName(buildTargetGroup), BuildPipeline.GetBuildTargetName(buildTarget));
        }

        // TODO: Move plugins that use this to GetImporters, and Remove this function
        internal static IEnumerable<PluginDesc> GetExtensionPlugins(BuildTarget target)
        {
            IEnumerable<IEnumerable<PluginDesc>> pluginDescriptions =
                AttributeHelper.CallMethodsWithAttribute<IEnumerable<PluginDesc>, RegisterPluginsAttribute>(target);
            foreach (IEnumerable<PluginDesc> extensionPlugins in pluginDescriptions)
            {
                foreach (PluginDesc pluginDesc in extensionPlugins)
                {
                    yield return pluginDesc;
                }
            }
        }

        [NativeMethod("ClearPlatformData")]
        extern public void ClearSettings();

        extern public void SetCompatibleWithAnyPlatform(bool enable);

        extern public bool GetCompatibleWithAnyPlatform();

        extern public void SetExcludeFromAnyPlatform(string platformName, bool excludedFromAny);

        extern public bool GetExcludeFromAnyPlatform(string platformName);

        public delegate bool IncludeInBuildDelegate(string path);

        // this is implemented as a static map so that it can survive a garbage collection on the PluginImporter and not get lost
        private static Dictionary<string, IncludeInBuildDelegate> s_includeInBuildDelegateMap = new Dictionary<string, IncludeInBuildDelegate>();
        public void SetIncludeInBuildDelegate(IncludeInBuildDelegate includeInBuildDelegate)
        {
            s_includeInBuildDelegateMap[assetPath] = includeInBuildDelegate;
        }

        [RequiredByNativeCode]
        private bool InvokeIncludeInBuildDelegate()
        {
            if (s_includeInBuildDelegateMap.ContainsKey(assetPath))
            {
                return s_includeInBuildDelegateMap[assetPath](assetPath);
            }

            return true;
        }

        public void SetExcludeFromAnyPlatform(BuildTarget platform, bool excludedFromAny)
        {
            SetExcludeFromAnyPlatform(BuildPipeline.GetBuildTargetName(platform), excludedFromAny);
        }

        public bool GetExcludeFromAnyPlatform(BuildTarget platform)
        {
            return GetExcludeFromAnyPlatform(BuildPipeline.GetBuildTargetName(platform));
        }

        extern public void SetExcludeEditorFromAnyPlatform(bool excludedFromAny);

        extern public bool GetExcludeEditorFromAnyPlatform();

        extern public void SetCompatibleWithEditor(bool enable);

        [NativeMethod("SetCompatibleWithEditor")]
        extern internal void SetCompatibleWithEditorWithBuildTargetsInternal(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, bool enable);

        [NativeMethod("IsCompatibleWithDefines")]
        extern internal bool IsCompatibleWithDefines(string[] defines);

        internal void SetCompatibleWithEditor(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, bool enable)
        {
            SetCompatibleWithEditorWithBuildTargetsInternal(buildTargetGroup, buildTarget, enable);
        }

        public bool GetCompatibleWithEditor()
        {
            return GetCompatibleWithEditor("", "");
        }

        extern public bool GetCompatibleWithEditor(string buildTargetGroup, string buildTarget);

        public extern bool isPreloaded
        {
            [NativeMethod("GetIsPreloaded")]
            get;
            [NativeMethod("SetIsPreloaded")]
            set;
        }

        extern public bool GetIsOverridable();

        extern public bool ShouldIncludeInBuild();

        public void SetCompatibleWithPlatform(BuildTarget platform, bool enable)
        {
            SetCompatibleWithPlatform(BuildPipeline.GetBuildTargetName(platform), enable);
        }

        internal void SetCompatibleWithPlatform(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, bool enable)
        {
            SetCompatibleWithPlatform(BuildPipeline.GetBuildTargetGroupName(buildTargetGroup), BuildPipeline.GetBuildTargetName(buildTarget), enable);
        }

        public bool GetCompatibleWithPlatform(BuildTarget platform)
        {
            return GetCompatibleWithPlatform(BuildPipeline.GetBuildTargetName(platform));
        }

        internal bool GetCompatibleWithPlatform(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            return GetCompatibleWithPlatform(BuildPipeline.GetBuildTargetGroupName(buildTargetGroup), BuildPipeline.GetBuildTargetName(buildTarget));
        }

        public void SetCompatibleWithPlatform(string platformName, bool enable)
        {
            SetCompatibleWithPlatform(BuildPipeline.GetBuildTargetGroupName(BuildPipeline.GetBuildTargetByName(platformName)), platformName, enable);
        }

        public bool GetCompatibleWithPlatform(string platformName)
        {
            return GetCompatibleWithPlatform(BuildPipeline.GetBuildTargetGroupName(BuildPipeline.GetBuildTargetByName(platformName)), platformName);
        }

        extern internal void SetCompatibleWithPlatform(string buildTargetGroup, string buildTarget, bool enable);

        extern internal bool GetCompatibleWithPlatform(string buildTargetGroup, string buildTarget);

        public void SetPlatformData(BuildTarget platform, string key, string value)
        {
            SetPlatformData(BuildPipeline.GetBuildTargetName(platform), key, value);
        }

        public extern string GetPlatformData(BuildTarget platform, string key);

        extern public void SetPlatformData(string platformName, string key, string value);

        public string GetPlatformData(string platformName, string key)
        {
            return GetPlatformData(BuildPipeline.GetBuildTargetByName(platformName), key);
        }

        extern public void SetEditorData(string key, string value);

        extern public string GetEditorData(string key);

        public extern bool isNativePlugin
        {
            [NativeMethod("IsNativePlugin")]
            get;
        }

        internal extern DllType dllType
        {
            get;
        }

        internal extern AssemblyFullName assemblyFullName
        {
            get;
        }

        extern public static PluginImporter[] GetAllImporters();

        public extern void SetIcon([NotNull] string className, Texture2D icon);
        public extern Texture2D GetIcon([NotNull] string className);

        private static void LogIgnoredDuplicateAssembly(PluginImporter usedPluginImporter, PluginImporter ignoredPluginImporter)
        {
            var assemblyName = Path.GetFileName(usedPluginImporter.assetPath);

            // keep this message here in sync with PrecompiledAssemblies.cpp LogIgnoredDuplicateAssembly
            var message = $"Duplicate assembly '{assemblyName}' with different versions detected, using '{usedPluginImporter.assetPath}, Version={usedPluginImporter.assemblyFullName.Version}' and ignoring '{ignoredPluginImporter.assetPath}, Version={ignoredPluginImporter.assemblyFullName.Version}'.";
            Console.WriteLine(message);
        }

        internal static IEnumerable<PluginImporter> FilterAssembliesByAssemblyVersion(IEnumerable<PluginImporter> plugins)
        {
            var managedPlugins = new Dictionary<string, List<(PluginImporter PluginImporter, AssemblyFullName AssemblyFullName)>>();
            foreach (var plugin in plugins)
            {
                if (plugin.dllType == DllType.Native || plugin.dllType == DllType.Unknown)
                {
                    yield return plugin;
                    continue;
                }

                var assemblyFullName = plugin.assemblyFullName;
                var assemblyKey = plugin.assemblyFullName.Name;
                if (!managedPlugins.TryGetValue(assemblyKey, out var list))
                {
                    list = new List<(PluginImporter PluginImporter, AssemblyFullName AssemblyFullName)>();
                    managedPlugins[assemblyKey] = list;
                }
                list.Add((plugin, assemblyFullName));
            }

            foreach (var managedPlugin in managedPlugins.Values)
            {
                var bestCandidate = managedPlugin[0];
                for (int i = 1; i < managedPlugin.Count; i++)
                {
                    var currentCandidate = managedPlugin[i];
                    if (currentCandidate.AssemblyFullName.Version > bestCandidate.AssemblyFullName.Version)
                    {
                        LogIgnoredDuplicateAssembly(currentCandidate.PluginImporter, bestCandidate.PluginImporter);
                        bestCandidate = currentCandidate;
                    }
                    else if (currentCandidate.AssemblyFullName.Version != bestCandidate.AssemblyFullName.Version)
                    {
                        LogIgnoredDuplicateAssembly(bestCandidate.PluginImporter, currentCandidate.PluginImporter);
                    }
                }
                yield return bestCandidate.PluginImporter;
            }
        }
    }
}
