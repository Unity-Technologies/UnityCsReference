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
using UnityEditor.BuildReporting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Networking;
using UnityEditor.Utils;

namespace UnityEditor.BuildReporting
{
    internal class StrippingInfo : ScriptableObject, ISerializationCallbackReceiver
    {
        public const string RequiredByScripts = "Required by Scripts";

        [System.Serializable]
        public struct SerializedDependency
        {
            [SerializeField]
            public string key;
            [SerializeField]
            public List<string> value;
            [SerializeField]
            public string icon;
        }

        public List<SerializedDependency> serializedDependencies;

        public List<string> modules = new List<string>();

        public List<int> serializedSizes = new List<int>();

        public Dictionary<string, HashSet<string>> dependencies = new Dictionary<string, HashSet<string>>();

        public Dictionary<string, int> sizes = new Dictionary<string, int>();

        public Dictionary<string, string> icons = new Dictionary<string, string>();

        public int totalSize = 0;

        void OnEnable()
        {
            SetIcon(RequiredByScripts, "class/MonoScript");

            // We don't have real icons specifically for all the modules right now.
            // While it would be nice to have some, for now, we set up the common modules to use some
            // of their class icons.
            SetIcon(ModuleName("AI"), "class/NavMeshAgent");
            SetIcon(ModuleName("Animation"), "class/Animation");
            SetIcon(ModuleName("Audio"), "class/AudioSource");
            SetIcon(ModuleName("Core"), "class/GameManager");
            SetIcon(ModuleName("IMGUI"), "class/GUILayer");
            SetIcon(ModuleName("ParticleSystem"), "class/ParticleSystem");
            SetIcon(ModuleName("ParticlesLegacy"), "class/EllipsoidParticleEmitter");
            SetIcon(ModuleName("Physics"), "class/PhysicMaterial");
            SetIcon(ModuleName("Physics2D"), "class/PhysicsMaterial2D");
            SetIcon(ModuleName("TextRendering"), "class/Font");
            SetIcon(ModuleName("UI"), "class/CanvasGroup");
            SetIcon(ModuleName("Umbra"), "class/OcclusionCullingSettings");
            SetIcon(ModuleName("UNET"), "class/NetworkTransform");
            SetIcon(ModuleName("Vehicles"), "class/WheelCollider");
            SetIcon(ModuleName("Cloth"), "class/Cloth");
            SetIcon(ModuleName("ImageConversion"), "class/Texture");
            SetIcon(ModuleName("ScreenCapture"), "class/RenderTexture");
        }

        public void OnBeforeSerialize()
        {
            serializedDependencies = new List<SerializedDependency>();
            foreach (var dep in dependencies)
            {
                var list = new List<string>();
                foreach (var nc in dep.Value)
                    list.Add(nc);
                SerializedDependency sd;
                sd.key = dep.Key;
                sd.value = list;
                sd.icon = icons.ContainsKey(dep.Key) ? icons[dep.Key] : "class/DefaultAsset";
                serializedDependencies.Add(sd);
            }
            serializedSizes = new List<int>();
            foreach (var module in modules)
            {
                serializedSizes.Add(sizes.ContainsKey(module) ? sizes[module] : 0);
            }
        }

        public void OnAfterDeserialize()
        {
            dependencies = new Dictionary<string, HashSet<string>>();
            icons = new Dictionary<string, string>();
            for (int i = 0; i < serializedDependencies.Count; i++)
            {
                HashSet<string> depends = new HashSet<string>();
                foreach (var s in serializedDependencies[i].value)
                    depends.Add(s);
                dependencies.Add(serializedDependencies[i].key, depends);
                icons[serializedDependencies[i].key] = serializedDependencies[i].icon;
            }
            sizes = new Dictionary<string, int>();
            for (int i = 0; i < serializedSizes.Count; i++)
                sizes[modules[i]] = serializedSizes[i];
        }

        public void RegisterDependency(string obj, string depends)
        {
            if (!dependencies.ContainsKey(obj))
                dependencies[obj] = new HashSet<string>();
            dependencies[obj].Add(depends);
            if (!icons.ContainsKey(depends))
                SetIcon(depends, "class/" + depends);
        }

        public void AddModule(string module)
        {
            if (!modules.Contains(module))
                modules.Add(module);
            if (!sizes.ContainsKey(module))
                sizes[module] = 0;

            // Fall back to default icon for unknown modules
            if (!icons.ContainsKey(module))
                SetIcon(module, "class/DefaultAsset");
        }

        public void SetIcon(string dependency, string icon)
        {
            icons[dependency] = icon;
            if (!dependencies.ContainsKey(dependency))
                dependencies[dependency] = new HashSet<string>();
        }

        public void AddModuleSize(string module, int size)
        {
            if (modules.Contains(module))
                sizes[module] = size;
        }

        public static StrippingInfo GetBuildReportData(BuildReport report)
        {
            if (report == null)
                return null;
            var allStrippingData = (StrippingInfo[])report.GetAppendices(typeof(StrippingInfo));
            if (allStrippingData.Length > 0)
                return allStrippingData[0];

            var newData = ScriptableObject.CreateInstance<StrippingInfo>();
            report.AddAppendix(newData);
            return newData;
        }

        public static string ModuleName(string module)
        {
            return module + " Module";
        }
    }
}

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

        static string[] s_blackListNativeClassNames = new string[]
        {
            "Behaviour", // GetFixedBehaviourManager is directly used by fixed update in the player loop
            "PreloadData",
            // will otherwise be stripped if scene only uses default materials not explicitly referenced
            // (ie some components will get a default material if a material reference is null)
            "Material",
            // those are used to create builtin textures and availability is checked at runtime
            "Cubemap",
            "Texture3D",
            "Texture2DArray",
            "RenderTexture",
            "Mesh", // Used by IMGUI (even on empty projects, it draws development console & watermarks)
            "Sprite", // Used by Unity splash screen.
            "LowerResBlitTexture",
        };

        static UnityType[] s_blackListNativeClasses;

        public static UnityType[] BlackListNativeClasses
        {
            get
            {
                if (s_blackListNativeClasses == null)
                {
                    s_blackListNativeClasses = s_blackListNativeClassNames.Select(
                            typeName => FindTypeByNameChecked(typeName, "code stripping blacklist native class")).ToArray();
                }

                return s_blackListNativeClasses;
            }
        }

        static readonly Dictionary<string, string> s_blackListNativeClassesDependencyNames = new Dictionary<string, string>
        {
            {"ParticleSystemRenderer", "ParticleSystem"}
        };

        static Dictionary<UnityType, UnityType> s_blackListNativeClassesDependency;

        public static Dictionary<UnityType, UnityType> BlackListNativeClassesDependency
        {
            get
            {
                if (s_blackListNativeClassesDependency == null)
                {
                    s_blackListNativeClassesDependency = new Dictionary<UnityType, UnityType>();
                    foreach (var kv in s_blackListNativeClassesDependencyNames)
                        BlackListNativeClassesDependency.Add(
                            FindTypeByNameChecked(kv.Key, "code stripping blacklist native class dependency key"),
                            FindTypeByNameChecked(kv.Value, "code stripping blacklist native class dependency value"));
                }

                return s_blackListNativeClassesDependency;
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

        private static bool IsVRModuleUsed(BuildTarget target)
        {
            var targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);

            if (targetGroup != BuildTargetGroup.iOS)
                return false;
            if (!PlayerSettings.virtualRealitySupported)
                return false;

            string[] vrDevices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(targetGroup);
            if (Array.IndexOf(vrDevices, "cardboard") < 0)
                return false;
            return true;
        }

        public static void InjectCustomDependencies(BuildTarget target, StrippingInfo strippingInfo, HashSet<UnityType> nativeClasses,
            HashSet<string> nativeModules)
        {
            // This function can be used to inject user-readable dependency information for specific classes which would not be obvious otherwise.
            // Can also be used to set up dependencies to modules which cannot be derived by the build pipeline without custom rules
            const string analyticsManagerName = "UnityAnalyticsManager";
            var analyticsManager = UnityType.FindTypeByName(analyticsManagerName);
            if (nativeClasses.Contains(analyticsManager))
            {
                if (PlayerSettings.submitAnalytics)
                {
                    const string requiredMessage = "Required by HW Statistics (See Player Settings)";
                    strippingInfo.RegisterDependency(analyticsManagerName, requiredMessage);
                    strippingInfo.SetIcon(requiredMessage, "class/PlayerSettings");
                }
                if (UnityEditor.Analytics.AnalyticsSettings.enabled)
                {
                    const string requiredMessage = "Required by Unity Analytics (See Services Window)";
                    strippingInfo.RegisterDependency(analyticsManagerName, requiredMessage);
                    strippingInfo.SetIcon(requiredMessage, "class/PlayerSettings");
                }
            }

            if (IsVRModuleUsed(target))
            {
                const string moduleName = "VR";
                const string requiredMessage = "Required because VR is enabled in PlayerSettings";
                nativeModules.Add(moduleName);
                strippingInfo.RegisterDependency(moduleName, StrippingInfo.RequiredByScripts);
                strippingInfo.SetIcon(requiredMessage, "class/PlayerSettings");
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

            bool didAdd = true;
            if (platformProvider != null)
            {
                while (didAdd)
                {
                    didAdd = false;
                    foreach (var module in nativeModules.ToList())
                    {
                        var whitelist = GetModuleWhitelist(module, platformProvider.moduleStrippingInformationFolder);
                        if (File.Exists(whitelist))
                        {
                            foreach (var dependentModule in GetDependentModules(whitelist))
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
                                    if (strippingInfo.icons.ContainsKey(moduleName))
                                        strippingInfo.SetIcon("Required by " + moduleName, strippingInfo.icons[moduleName]);
                                }
                            }
                        }
                    }
                }
            }

            AssemblyReferenceChecker checker = new AssemblyReferenceChecker();
            checker.CollectReferencesFromRoots(strippedAssemblyDir, userAssemblies, true, 0.0f, true);

            if (strippingInfo != null)
            {
                foreach (var module in nativeModules)
                    strippingInfo.AddModule(StrippingInfo.ModuleName(module));
                strippingInfo.AddModule(StrippingInfo.ModuleName("Core"));
            }

            if (nativeClasses != null && strippingInfo != null)
                InjectCustomDependencies(platformProvider.target, strippingInfo, nativeClasses, nativeModules);
        }

        public static string GetModuleWhitelist(string module, string moduleStrippingInformationFolder)
        {
            return Paths.Combine(moduleStrippingInformationFolder, module + ".xml");
        }

        public static List<string> GetDependentModules(string moduleXml)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(moduleXml);
            List<string> result = new List<string>();

            XmlNodeList dependencyNodes = doc.DocumentElement.SelectNodes("/linker/dependencies/module");
            foreach (XmlNode dependencyNode in dependencyNodes)
                result.Add(dependencyNode.Attributes["name"].Value);

            return result;
        }

        public static void WriteModuleAndClassRegistrationFile(string strippedAssemblyDir, string icallsListFile, string outputDir, RuntimeClassRegistry rcr, IEnumerable<UnityType> classesToSkip, IIl2CppPlatformProvider platformProvider)
        {
            HashSet<UnityType> nativeClasses;
            HashSet<string> nativeModules;
            // by default, we only care about il2cpp
            bool doStripping = PlayerSettings.stripEngineCode;
            GenerateDependencies(strippedAssemblyDir, icallsListFile, rcr, doStripping, out nativeClasses, out nativeModules, platformProvider);

            var outputClassRegistration = Path.Combine(outputDir, "UnityClassRegistration.cpp");
            WriteModuleAndClassRegistrationFile(outputClassRegistration, nativeModules, nativeClasses, new HashSet<UnityType>(classesToSkip));
        }

        public static HashSet<string> GetNativeModulesToRegister(HashSet<UnityType> nativeClasses, StrippingInfo strippingInfo)
        {
            return (nativeClasses == null) ? GetAllStrippableModules() : GetRequiredStrippableModules(nativeClasses, strippingInfo);
        }

        private static HashSet<string> GetAllStrippableModules()
        {
            HashSet<string> nativeModules = new HashSet<string>();
            foreach (string module in ModuleMetadata.GetModuleNames())
                if (ModuleMetadata.GetModuleStrippable(module)) // Only handle strippable modules. Others are listed in RegisterStaticallyLinkedModules()
                    nativeModules.Add(module);
            return nativeModules;
        }

        private static HashSet<string> GetRequiredStrippableModules(HashSet<UnityType> nativeClasses, StrippingInfo strippingInfo)
        {
            HashSet<UnityType> nativeClassesUsedInModules = new HashSet<UnityType>();
            HashSet<string> nativeModules = new HashSet<string>();
            foreach (string module in ModuleMetadata.GetModuleNames())
            {
                if (ModuleMetadata.GetModuleStrippable(module)) // Only handle strippable modules. Others are listed in RegisterStaticallyLinkedModules()
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
                if (!ModuleMetadata.GetModuleStrippable(module)) // Only handle strippable modules
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

            // Inject blacklisted native types
            foreach (var klass in BlackListNativeClasses)
                nativeClasses.Add(klass);

            foreach (var dependent in BlackListNativeClassesDependency.Keys)
            {
                if (nativeClasses.Contains(dependent))
                {
                    var provider = BlackListNativeClassesDependency[dependent];
                    nativeClasses.Add(provider);
                }
            }

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
                                    var name = definition.Name.Name;
                                    if (!AssemblyReferenceChecker.IsIgnoredSystemDll(name))
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
                                    var name = definition.Name.Name;
                                    if (!AssemblyReferenceChecker.IsIgnoredSystemDll(name))
                                        strippingInfo.RegisterDependency(className, StrippingInfo.RequiredByScripts);
                                }
                            }
                        }
                    }
                }
            }

            return foundTypes;
        }

        private static void WriteStaticallyLinkedModuleRegistration(TextWriter w, HashSet<string> nativeModules, HashSet<UnityType> nativeClasses)
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
            w.WriteLine("void RegisterStaticallyLinkedModulesGranular()");
            w.WriteLine("{");
            foreach (string module in nativeModules)
            {
                w.WriteLine("\tvoid RegisterModule_" + module + "();");
                w.WriteLine("\tRegisterModule_" + module + "();");
                w.WriteLine();
            }
            w.WriteLine("}");
        }

        private static void WriteModuleAndClassRegistrationFile(string file, HashSet<string> nativeModules, HashSet<UnityType> nativeClasses, HashSet<UnityType> classesToSkip)
        {
            using (TextWriter w = new StreamWriter(file))
            {
                w.WriteLine("template <typename T> void RegisterClass();");
                w.WriteLine("template <typename T> void RegisterStrippedTypeInfo(int, const char*, const char*);");

                w.WriteLine();
                WriteStaticallyLinkedModuleRegistration(w, nativeModules, nativeClasses);
                w.WriteLine();

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
                            w.WriteLine("template <> void RegisterClass<{0}>();", type.qualifiedName);
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
                            w.WriteLine("\tRegisterClass<{0}>();", klass.qualifiedName);
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

                    //  w.WriteLine("\tRegisterStrippedTypeInfo<{0}>({1}, \"{2}\", \"{3}\");", type.qualifiedName, type.persistentTypeID, type.name, type.nativeNamespace);
                    //}
                }
                w.WriteLine("}");
                w.Close();
            }
        }

        private static readonly string[] s_TreatedAsUserAssemblies =
        {
            // Treat analytics as we user assembly. If it is not used, it won't be in the directory,
            // so this should not add to the build size unless it is really used.
            "UnityEngine.Analytics.dll",
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
                arguments.AddRange(GetAssembliesInDirectory(strippedAssemblyDir, assembly));

            return arguments.ToArray();
        }

        private static IEnumerable<string> GetAssembliesInDirectory(string strippedAssemblyDir, string assemblyName)
        {
            return Directory.GetFiles(strippedAssemblyDir, assemblyName, SearchOption.TopDirectoryOnly);
        }
    } //CodeStrippingUtils
} //UnityEditor
