// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using UnityEditorInternal;
using Mono.Cecil;
using UnityEditor.Build.Reporting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Networking;
using UnityEditor.Utils;

namespace UnityEditor
{
    internal class CodeStrippingUtils
    {
        private static UnityType s_GameManagerTypeInfo = null;
        static UnityType GameManagerTypeInfo
        {
            get
            {
                if (s_GameManagerTypeInfo == null)
                    s_GameManagerTypeInfo = FindTypeByNameChecked("GameManager", "initializing code stripping utils");
                return s_GameManagerTypeInfo;
            }
        }

        private static UnityType FindTypeByNameChecked(string name, string msg)
        {
            UnityType result = UnityType.FindTypeByName(name);
            if (result == null)
                throw new ArgumentException(string.Format("Could not map typename '{0}' to type info ({1})", name, msg ?? "no context"));
            return result;
        }

        public static HashSet<string> GetModulesFromICalls(string icallsListFile)
        {
            var icalls = File.ReadAllLines(icallsListFile);
            var result = new HashSet<string>();
            foreach (var icall in icalls)
            {
                var module = ModuleMetadata.GetICallModule(icall);
                if (!string.IsNullOrEmpty(module))
                    result.Add(module);
            }
            return result;
        }

        private static void AddNativeModuleInStrippingInfo(string moduleName, string requiredMessage, StrippingInfo strippingInfo, HashSet<string> nativeModules, string icon)
        {
            nativeModules.Add(moduleName);
            strippingInfo.AddModule(moduleName);
            strippingInfo.RegisterDependency(StrippingInfo.ModuleName(moduleName), requiredMessage);
            strippingInfo.SetIcon(requiredMessage, icon);
        }

        public static void InjectCustomDependencies(BuildTarget target, StrippingInfo strippingInfo, HashSet<UnityType> nativeClasses,
            HashSet<string> nativeModules)
        {
            // This function can be used to inject user-readable dependency information for specific classes which would not be obvious otherwise.
            // Can also be used to set up dependencies to modules which cannot be derived by the build pipeline without custom rules
            if (UnityEngine.Connect.UnityConnectSettings.enabled || UnityEngine.Analytics.PerformanceReporting.enabled || UnityEngine.Analytics.Analytics.enabled)
            {
                string requiredMessage = "Required by HW Statistics (See Player Settings)";
                const string icon = "class/PlayerSettings";
                if (UnityEngine.Analytics.Analytics.enabled || UnityEngine.Analytics.PerformanceReporting.enabled)
                {
                    requiredMessage = "Required by Analytics Performance Reporting (See Analytics Services Window)";
                    AddNativeModuleInStrippingInfo("PerformanceReporting", requiredMessage, strippingInfo, nativeModules, icon);

                    requiredMessage = "Required by UnityAnalytics (See Services Window)";
                    AddNativeModuleInStrippingInfo("UnityAnalytics", requiredMessage, strippingInfo, nativeModules, icon);
                }
                AddNativeModuleInStrippingInfo("UnityConnect", requiredMessage, strippingInfo, nativeModules, icon);
                strippingInfo.RegisterDependency("UnityConnectSettings", "Required by UnityAnalytics");
            }

            if (CrashReporting.CrashReportingSettings.enabled)
                AddNativeModuleInStrippingInfo("CrashReporting", "Required by Crash Reporting Service (See Services Window)", strippingInfo, nativeModules, "class/PlayerSettings");

            if (UnityEditorInternal.VR.VRModule.ShouldInjectVRDependenciesForBuildTarget(target))
            {
                const string moduleName = "VR";
                const string requiredMessage = "Required because VR is enabled in PlayerSettings";
                nativeModules.Add(moduleName);
                strippingInfo.RegisterDependency(StrippingInfo.ModuleName(moduleName), requiredMessage);
                strippingInfo.SetIcon(requiredMessage, "class/PlayerSettings");
            }

            foreach (string module in ModuleMetadata.GetModuleNames())
            {
                if (!ModuleMetadata.IsStrippableModule(module))
                {
                    string requiredMessage = module + " is always required";
                    AddNativeModuleInStrippingInfo(module, requiredMessage, strippingInfo, nativeModules, "class/DefaultAsset");
                }
            }
        }

        public static void GenerateDependencies(string strippedAssemblyDir, string icallsListFile, RuntimeClassRegistry rcr, bool doStripping, out HashSet<UnityType> nativeClasses, out HashSet<string> nativeModules, IIl2CppPlatformProvider platformProvider)
        {
            var strippingInfo = platformProvider == null ? null : StrippingInfo.GetBuildReportData(platformProvider.buildReport);
            var userAssemblies = GetUserAssemblies(strippedAssemblyDir);
            // [1] Extract native classes from scene and scripts
            nativeClasses = doStripping ? GenerateNativeClassList(rcr, strippedAssemblyDir, userAssemblies, strippingInfo) : null;

            // Exclude module managers (GlobalGameManager) if no dependent classes are used.
            if (nativeClasses != null)
                ExcludeModuleManagers(ref nativeClasses);

            // [2] Prepare a list of modules to register
            nativeModules = GetNativeModulesToRegister(nativeClasses, strippingInfo);

            if (nativeClasses != null && icallsListFile != null)
            {
                // Get required modules from icall list file
                var icallModules = GetModulesFromICalls(icallsListFile);

                // Add GameManager classes for modules
                foreach (var module in icallModules)
                {
                    if (!nativeModules.Contains(module))
                    {
                        if (strippingInfo != null)
                        {
                            strippingInfo.RegisterDependency(StrippingInfo.ModuleName(module), StrippingInfo.RequiredByScripts);
                        }
                    }

                    var moduleClasses = ModuleMetadata.GetModuleTypes(module);
                    foreach (var klass in moduleClasses)
                    {
                        if (klass.IsDerivedFrom(GameManagerTypeInfo))
                        {
                            nativeClasses.Add(klass);
                        }
                    }
                }

                nativeModules.UnionWith(icallModules);
            }

            ApplyManualStrippingOverrides(nativeClasses, nativeModules, strippingInfo);

            bool didAdd = true;
            if (platformProvider != null)
            {
                while (didAdd)
                {
                    didAdd = false;
                    foreach (var module in nativeModules.ToList())
                    {
                        var dependecies = ModuleMetadata.GetModuleDependencies(module);
                        foreach (var dependentModule in dependecies)
                        {
                            if (!nativeModules.Contains(dependentModule))
                            {
                                nativeModules.Add(dependentModule);
                                didAdd = true;
                            }
                            if (strippingInfo != null)
                            {
                                var moduleName = StrippingInfo.ModuleName(module);
                                strippingInfo.RegisterDependency(StrippingInfo.ModuleName(dependentModule), "Required by " + moduleName);
                                strippingInfo.SetIcon("Required by " + moduleName, $"package/com.unity.modules.{module.ToLower()}");
                            }
                        }
                    }
                }
            }

            if (nativeClasses != null)
                RemoveClassesFromRemovedModules(nativeClasses, nativeModules);

            AssemblyReferenceChecker checker = new AssemblyReferenceChecker();
            checker.CollectReferencesFromRoots(strippedAssemblyDir, userAssemblies, true, 0.0f, true);

            if (strippingInfo != null)
            {
                foreach (var module in nativeModules)
                    strippingInfo.AddModule(module);
                strippingInfo.AddModule("Core");
            }

            if (nativeClasses != null && strippingInfo != null && platformProvider != null)
                InjectCustomDependencies(platformProvider.target, strippingInfo, nativeClasses, nativeModules);
        }

        public static void ApplyManualStrippingOverrides(HashSet<UnityType> nativeClasses, HashSet<string> nativeModules, StrippingInfo strippingInfo)
        {
            // Apply manual stripping overrides
            foreach (var module in ModuleMetadata.GetModuleNames())
            {
                var includeSetting = ModuleMetadata.GetModuleIncludeSettingForModule(module);
                if (includeSetting == ModuleIncludeSetting.ForceInclude)
                {
                    nativeModules.Add(module);
                    if (nativeClasses != null)
                    {
                        var moduleClasses = ModuleMetadata.GetModuleTypes(module);
                        foreach (var klass in moduleClasses)
                        {
                            nativeClasses.Add(klass);
                            if (strippingInfo != null)
                            {
                                strippingInfo.RegisterDependency(klass.name, "Force included module");
                                strippingInfo.RegisterDependency(StrippingInfo.ModuleName(module), klass.name);
                            }
                        }
                    }

                    if (strippingInfo != null)
                        strippingInfo.RegisterDependency(StrippingInfo.ModuleName(module), "Force included module");
                }
                else if (includeSetting == ModuleIncludeSetting.ForceExclude)
                {
                    if (nativeModules.Contains(module))
                    {
                        nativeModules.Remove(module);

                        if (strippingInfo != null)
                            strippingInfo.modules.Remove(StrippingInfo.ModuleName(module));
                    }
                }
            }
        }

        static void RemoveClassesFromRemovedModules(HashSet<UnityType> nativeClasses, HashSet<string> nativeModules)
        {
            HashSet<UnityType> allModuleClasses = new HashSet<UnityType>();
            foreach (var module in nativeModules)
            {
                foreach (var klass in ModuleMetadata.GetModuleTypes(module))
                    allModuleClasses.Add(klass);
            }
            nativeClasses.RemoveWhere(klass => !allModuleClasses.Contains(klass));
        }

        public static string GetModuleWhitelist(string module, string moduleStrippingInformationFolder)
        {
            return Paths.Combine(moduleStrippingInformationFolder, module + ".xml");
        }

        public static void WriteModuleAndClassRegistrationFile(string strippedAssemblyDir, string icallsListFile, string outputDir, RuntimeClassRegistry rcr, IEnumerable<UnityType> classesToSkip, IIl2CppPlatformProvider platformProvider, bool writeModuleRegistration = true, bool writeClassRegistration = true)
        {
            HashSet<UnityType> nativeClasses;
            HashSet<string> nativeModules;
            // by default, we only care about il2cpp
            bool doStripping = PlayerSettings.stripEngineCode && !EditorUserBuildSettings.buildScriptsOnly;
            GenerateDependencies(strippedAssemblyDir, icallsListFile, rcr, doStripping, out nativeClasses, out nativeModules, platformProvider);

            var outputClassRegistration = Path.Combine(outputDir, "UnityClassRegistration.cpp");
            using (TextWriter w = new StreamWriter(outputClassRegistration))
            {
                if (writeModuleRegistration)
                    WriteFunctionRegisterStaticallyLinkedModulesGranular(w, nativeModules);
                if (writeClassRegistration)
                    WriteStaticallyLinkedModuleClassRegistration(w, nativeClasses, new HashSet<UnityType>(classesToSkip));
                w.Close();
            }
        }

        public static HashSet<string> GetNativeModulesToRegister(HashSet<UnityType> nativeClasses, StrippingInfo strippingInfo)
        {
            return (nativeClasses == null) ? GetAllStrippableModules() : GetRequiredStrippableModules(nativeClasses, strippingInfo);
        }

        private static HashSet<string> GetAllStrippableModules()
        {
            HashSet<string> nativeModules = new HashSet<string>();
            foreach (string module in ModuleMetadata.GetModuleNames())
                if (ModuleMetadata.IsStrippableModule(module)) // Only handle strippable modules. Others are listed in RegisterStaticallyLinkedModulesGranular()
                    nativeModules.Add(module);
            return nativeModules;
        }

        private static HashSet<string> GetRequiredStrippableModules(HashSet<UnityType> nativeClasses, StrippingInfo strippingInfo)
        {
            HashSet<UnityType> nativeClassesUsedInModules = new HashSet<UnityType>();
            HashSet<string> nativeModules = new HashSet<string>();
            foreach (string module in ModuleMetadata.GetModuleNames())
            {
                if (ModuleMetadata.IsStrippableModule(module)) // Only handle strippable modules. Others are listed in RegisterStaticallyLinkedModulesGranular()
                {
                    var moduleClasses = new HashSet<UnityType>(ModuleMetadata.GetModuleTypes(module));
                    if (nativeClasses.Overlaps(moduleClasses)) // Include module if at least one of its classes is present
                    {
                        nativeModules.Add(module);
                        if (strippingInfo != null)
                        {
                            foreach (var klass in moduleClasses)
                            {
                                if (nativeClasses.Contains(klass))
                                {
                                    strippingInfo.RegisterDependency(StrippingInfo.ModuleName(module), klass.name);
                                    // Don't list GlobalGameManagers in a module will automatically be added if the module is added.
                                    nativeClassesUsedInModules.Add(klass);
                                }
                            }
                        }
                    }
                }
            }

            // Classes which have not been registered as belonging to a module belong to the Core module.
            if (strippingInfo != null)
            {
                foreach (var klass in nativeClasses)
                {
                    if (!nativeClassesUsedInModules.Contains(klass))
                        strippingInfo.RegisterDependency(StrippingInfo.ModuleName("Core"), klass.name);
                }
            }
            return nativeModules;
        }

        private static void ExcludeModuleManagers(ref HashSet<UnityType> nativeClasses)
        {
            string[] moduleNames = ModuleMetadata.GetModuleNames();

            foreach (string module in moduleNames)
            {
                if (!ModuleMetadata.IsStrippableModule(module)) // Only handle strippable modules
                    continue;

                var moduleClasses = ModuleMetadata.GetModuleTypes(module);

                // Find (non-)manager classes
                HashSet<UnityType> managerClasses = new HashSet<UnityType>();
                HashSet<UnityType> nonManagerClasses = new HashSet<UnityType>();
                foreach (var klass in moduleClasses)
                {
                    if (klass.IsDerivedFrom(GameManagerTypeInfo))
                        managerClasses.Add(klass);
                    else
                        nonManagerClasses.Add(klass);
                }

                // At least one non-manager class is required for the removal attempt
                if (nonManagerClasses.Count == 0)
                    continue;

                // Exclude manager classes if no non-manager classes are present
                if (!nativeClasses.Overlaps(nonManagerClasses))
                {
                    foreach (var klass in managerClasses)
                        nativeClasses.Remove(klass);
                }
                else
                {
                    foreach (var klass in managerClasses)
                        nativeClasses.Add(klass);
                }
            }
        }

        private static HashSet<UnityType> GenerateNativeClassList(RuntimeClassRegistry rcr, string directory, string[] rootAssemblies, StrippingInfo strippingInfo)
        {
            HashSet<UnityType> nativeClasses = CollectNativeClassListFromRoots(directory, rootAssemblies, strippingInfo);

            // List native classes found in scenes
            foreach (string klassName in rcr.GetAllNativeClassesIncludingManagersAsString())
            {
                UnityType klass = UnityType.FindTypeByName(klassName);
                if (klass != null && klass.baseClass != null)
                {
                    nativeClasses.Add(klass);
                    if (strippingInfo != null)
                    {
                        if (!klass.IsDerivedFrom(GameManagerTypeInfo))
                        {
                            var scenes = rcr.GetScenesForClass(klass.persistentTypeID);
                            if (scenes != null)
                            {
                                foreach (var scene in scenes)
                                {
                                    strippingInfo.RegisterDependency(klassName, scene);
                                    if (scene.EndsWith(".unity"))
                                        strippingInfo.SetIcon(scene, "class/SceneAsset");
                                    else
                                        strippingInfo.SetIcon(scene, "class/AssetBundle");
                                }
                            }
                        }
                    }
                }
            }

            // Always include base classes of derived native classes.
            HashSet<UnityType> nativeClassesAndBaseClasses = new HashSet<UnityType>();
            foreach (var klass in nativeClasses)
            {
                var current = klass;
                while (current.baseClass != null)
                {
                    nativeClassesAndBaseClasses.Add(current);
                    current = current.baseClass;
                }
            }

            return nativeClassesAndBaseClasses;
        }

        private static HashSet<UnityType> CollectNativeClassListFromRoots(string directory, string[] rootAssemblies, StrippingInfo strippingInfo)
        {
            // Collect managed types
            HashSet<string> managedTypeNames = CollectManagedTypeReferencesFromRoots(directory, rootAssemblies, strippingInfo);

            // Extract native types from managed types
            var infos = managedTypeNames.Select(name => UnityType.FindTypeByName(name)).Where(klass => klass != null && klass.baseClass != null);
            return new HashSet<UnityType>(infos);
        }

        // Collects all types from managed assemblies.
        // Follows assembly references from given root assemblies.
        // Assemblies should be already stripped.
        private static HashSet<string> CollectManagedTypeReferencesFromRoots(string directory, string[] rootAssemblies, StrippingInfo strippingInfo)
        {
            HashSet<string> foundTypes = new HashSet<string>();

            AssemblyReferenceChecker checker = new AssemblyReferenceChecker();
            bool collectMethods = false;
            bool ignoreSystemDlls = false;
            checker.CollectReferencesFromRoots(directory, rootAssemblies, collectMethods, 0.0f, ignoreSystemDlls);
            {
                string[] fileNames = checker.GetAssemblyFileNames();
                AssemblyDefinition[] assemblies = checker.GetAssemblyDefinitions();

                foreach (AssemblyDefinition definition in assemblies)
                {
                    foreach (TypeDefinition typeDefinition in definition.MainModule.Types)
                    {
                        if (typeDefinition.Namespace.StartsWith("UnityEngine"))
                        {
                            // Skip blank types
                            if (typeDefinition.Fields.Count > 0 || typeDefinition.Methods.Count > 0 || typeDefinition.Properties.Count > 0)
                            {
                                string className = typeDefinition.Name;
                                foundTypes.Add(className);
                                if (strippingInfo != null)
                                {
                                    if (!AssemblyReferenceChecker.IsIgnoredSystemDll(definition))
                                        strippingInfo.RegisterDependency(className, StrippingInfo.RequiredByScripts);
                                }
                            }
                        }
                    }
                }

                AssemblyDefinition unityEngineAssemblyDefinition = null;
                AssemblyDefinition unityEngineUIAssemblyDefinition = null;
                for (int i = 0; i < fileNames.Length; i++)
                {
                    if (fileNames[i] == "UnityEngine.dll")
                        unityEngineAssemblyDefinition = assemblies[i];

                    // UnityEngine.UI references UnityEngine.Collider, which causes the inclusion of Physics and Physics2D modules if
                    // UnityEngine.UI is referenced. UnityEngine.UI code is designed to only actually access Colliders if these modules
                    // are used, so don't include references from UnityEngine.UI here.
                    if (fileNames[i] == "UnityEngine.UI.dll")
                        unityEngineUIAssemblyDefinition = assemblies[i];
                }

                foreach (AssemblyDefinition definition in assemblies)
                {
                    if (definition != unityEngineAssemblyDefinition && definition != unityEngineUIAssemblyDefinition)
                    {
                        foreach (TypeReference typeReference in definition.MainModule.GetTypeReferences())
                        {
                            if (typeReference.Namespace.StartsWith("UnityEngine"))
                            {
                                string className = typeReference.Name;
                                foundTypes.Add(className);
                                if (strippingInfo != null)
                                {
                                    if (!AssemblyReferenceChecker.IsIgnoredSystemDll(definition))
                                        strippingInfo.RegisterDependency(className, StrippingInfo.RequiredByScripts);
                                }
                            }
                        }
                    }
                }
            }

            return foundTypes;
        }

        public class ModuleDependencyComparer : IComparer<string>
        {
            public int Compare(string stringA, string stringB)
            {
                return ModuleMetadata.GetModuleDependencies(stringA).Contains(stringB) ? 1 : ModuleMetadata.GetModuleDependencies(stringB).Contains(stringA) ? -1 : 0;
            }
        }

        private static void WriteFunctionInvokeRegisterStaticallyLinkedModuleClasses(TextWriter w, HashSet<UnityType> nativeClasses)
        {
            w.WriteLine("void InvokeRegisterStaticallyLinkedModuleClasses()");
            w.WriteLine("{");
            if (nativeClasses == null)
            {
                w.WriteLine("\tvoid RegisterStaticallyLinkedModuleClasses();");
                w.WriteLine("\tRegisterStaticallyLinkedModuleClasses();");
            }
            else
            {
                w.WriteLine("\t// Do nothing (we're in stripping mode)");
            }
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteFunctionRegisterStaticallyLinkedModulesGranular(TextWriter w, HashSet<string> nativeModules)
        {
            w.WriteLine("extern \"C\" void RegisterStaticallyLinkedModulesGranular()");
            w.WriteLine("{");

            var nativeModulesSorted = nativeModules.OrderBy(x => x, new ModuleDependencyComparer());
            foreach (string module in nativeModulesSorted)
            {
                w.WriteLine("\tvoid RegisterModule_" + module + "();");
                w.WriteLine("\tRegisterModule_" + module + "();");
                w.WriteLine();
            }
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteStaticallyLinkedModuleClassRegistration(TextWriter w, HashSet<UnityType> nativeClasses, HashSet<UnityType> classesToSkip)
        {
            w.WriteLine("template <typename T> void RegisterUnityClass(const char*);");
            w.WriteLine("template <typename T> void RegisterStrippedType(int, const char*, const char*);");
            w.WriteLine();

            WriteFunctionInvokeRegisterStaticallyLinkedModuleClasses(w, nativeClasses);

            // Forward declare types
            if (nativeClasses != null)
            {
                foreach (var type in UnityType.GetTypes())
                {
                    if (type.baseClass == null || type.isEditorOnly || classesToSkip.Contains(type))
                        continue;

                    if (type.hasNativeNamespace)
                        w.Write("namespace {0} {{ class {1}; }} ", type.nativeNamespace, type.name);
                    else
                        w.Write("class {0}; ", type.name);

                    if (nativeClasses.Contains(type))
                        w.WriteLine("template <> void RegisterUnityClass<{0}>(const char*);", type.qualifiedName);
                    else
                        w.WriteLine();
                }
                w.WriteLine();
            }

            // Class registration function
            w.WriteLine("void RegisterAllClasses()");
            w.WriteLine("{");

            if (nativeClasses == null)
            {
                w.WriteLine("\tvoid RegisterAllClassesGranular();");
                w.WriteLine("\tRegisterAllClassesGranular();");
            }
            else
            {
                w.WriteLine("void RegisterBuiltinTypes();");
                w.WriteLine("RegisterBuiltinTypes();");
                w.WriteLine("\t//Total: {0} non stripped classes", nativeClasses.Count);

                int index = 0;
                foreach (var klass in nativeClasses)
                {
                    w.WriteLine("\t//{0}. {1}", index, klass.qualifiedName);
                    if (classesToSkip.Contains(klass))
                        w.WriteLine("\t//Skipping {0}", klass.qualifiedName);
                    else
                        w.WriteLine("\tRegisterUnityClass<{0}>(\"{1}\");", klass.qualifiedName, klass.module);
                    ++index;
                }
                w.WriteLine();

                // Register stripped classes

                // TODO (ulfj ) 2016-08-15 : Right now we cannot deal with types that are compiled into the editor
                // but not the player due to other defines than UNITY_EDITOR in them module definition file
                // (for example WorldAnchor only being there if ENABLE_HOLOLENS_MODULE_API). Doing this would
                // require either some non trivial changes to the module registration macros or a way for these
                // conditionals to be included in the RTTI so we can emit them when generating the code, so we
                // disabling the registration of stripped classes for now and will get back to this when we have
                // landed the remaining changes to the type system.

                //w.WriteLine("\t//Stripped classes");
                //foreach (var type in UnityType.GetTypes())
                //{
                //  if (type.baseClass == null || type.isEditorOnly || classesToSkip.Contains(type) || nativeClasses.Contains(type))
                //      continue;

                //  w.WriteLine("\tRegisterStrippedType<{0}>({1}, \"{2}\", \"{3}\");", type.qualifiedName, type.persistentTypeID, type.name, type.nativeNamespace);
                //}
            }
            w.WriteLine("}");
        }

        private static readonly string[] s_TreatedAsUserAssemblies =
        {
            // Treat analytics as we user assembly. If it is not used, it won't be in the directory,
            // so this should not add to the build size unless it is really used.
            "Unity.Analytics.dll",
        };

        public static string[] UserAssemblies
        {
            get
            {
                EditorCompilation.TargetAssemblyInfo[] allTargetAssemblies = EditorCompilationInterface.GetTargetAssemblies();

                string[] targetAssemblyNames = new string[allTargetAssemblies.Length + s_TreatedAsUserAssemblies.Length];

                for (int i = 0; i < allTargetAssemblies.Length; ++i)
                {
                    targetAssemblyNames[i] = allTargetAssemblies[i].Name;
                }
                for (int i = 0; i < s_TreatedAsUserAssemblies.Length; ++i)
                {
                    targetAssemblyNames[allTargetAssemblies.Length + i] = s_TreatedAsUserAssemblies[i];
                }
                return targetAssemblyNames;
            }
        }

        private static string[] GetUserAssemblies(string strippedAssemblyDir)
        {
            var arguments = new List<string>();

            foreach (var assembly in UserAssemblies)
            {
                var files = Directory.GetFiles(strippedAssemblyDir, assembly, SearchOption.TopDirectoryOnly);
                arguments.AddRange(files.Select(f => Path.GetFileName(f)));
            }

            // Workaround: if there are no user assemblies (because the project does not contain scripts), add
            // UnityEngine, to makes sure we pick up types required by core module. Need to remove this once
            // we want to be able to strip core module for ECS only players.
            if (arguments.Count == 0)
                arguments.Add("UnityEngine.dll");

            return arguments.ToArray();
        }
    } //CodeStrippingUtils
} //UnityEditor
