// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Mono.Cecil;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/PluginImporter.h")]
    public sealed partial class PluginImporter : AssetImporter
    {
        [NativeMethod("GetCompatibleWithPlatformOrAnyPlatform")]
        extern internal bool GetCompatibleWithPlatformOrAnyPlatformBuildTarget(string buildTarget);

        [NativeMethod("GetCompatibleWithPlatformOrAnyPlatform")]
        extern private bool GetCompatibleWithPlatformOrAnyPlatformBuildGroupAndTarget(string buildTargetGroup, string buildTarget);

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

        internal string HasDiscouragedReferences()
        {
            if (!isNativePlugin)
            {
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(assetPath, new ReaderParameters());
                foreach (var reference in assemblyDefinition.MainModule.AssemblyReferences)
                {
                    // We don't use AssemblyHelper.IsUnityEngineModule here, because that would require loading the assembly, which may not even be present.
                    if (reference.Name.StartsWith("UnityEngine.") && reference.Name.EndsWith("Module"))
                        return reference.Name;
                }
            }
            return null;
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
                AttributeHelper.CallMethodsWithAttribute<IEnumerable<PluginDesc>>(typeof(RegisterPluginsAttribute), target);
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

        // this is implemented as a static map so that it can survive a garbage collection on the PluginImporter and not get lost
        private static Dictionary<string, System.Predicate<string>> s_shouldOverridePredicateMap = new Dictionary<string, System.Predicate<string>>();
        internal void SetShouldOverridePredicate(System.Predicate<string> shouldOverridePredicate)
        {
            if (shouldOverridePredicate != null)
            {
                s_shouldOverridePredicateMap[assetPath] = shouldOverridePredicate;
            }
            else
            {
                if (s_shouldOverridePredicateMap.ContainsKey(assetPath))
                    s_shouldOverridePredicateMap.Remove(assetPath);
            }
        }

        [RequiredByNativeCode]
        private bool InvokeShouldOverridePredicate()
        {
            if (s_shouldOverridePredicateMap.ContainsKey(assetPath))
            {
                try
                {
                    return s_shouldOverridePredicateMap[assetPath](assetPath);
                }
                catch (System.Exception)
                {
                    UnityEngine.Debug.LogWarning("Exception occurred while invoking ShouldOverridePredicate for " + assetPath);
                }
            }

            return false;
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

        internal void SetCompatibleWithEditor(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, bool enable)
        {
            SetCompatibleWithEditorWithBuildTargetsInternal(buildTargetGroup, buildTarget, enable);
        }

        public bool GetCompatibleWithEditor()
        {
            return GetCompatibleWithEditor("", "");
        }

        extern public bool GetCompatibleWithEditor(string buildTargetGroup, string buildTarget);

        extern internal void SetIsPreloaded(bool isPreloaded);

        extern internal bool GetIsPreloaded();

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

        public string GetPlatformData(BuildTarget platform, string key)
        {
            return GetPlatformData(BuildPipeline.GetBuildTargetName(platform), key);
        }

        extern public void SetPlatformData(string platformName, string key, string value);

        extern public string GetPlatformData(string platformName, string key);

        extern public void SetEditorData(string key, string value);

        extern public string GetEditorData(string key);

        public extern  bool isNativePlugin
        {
            [NativeMethod("IsNativePlugin")]
            get;
        }

        internal extern  DllType dllType
        {
            get;
        }

        extern public static  PluginImporter[] GetAllImporters();
    }
}
