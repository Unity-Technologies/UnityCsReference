// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditor.UnityLinker;
using Debug = UnityEngine.Debug;

namespace UnityEditorInternal
{
    internal class AssemblyStripper
    {
        /// <summary>
        /// Escapes XML special characters to prevent XML parsing errors.
        /// This is necessary for compiler-generated names (e.g., lambda methods like &lt;OnEnable&gt;b__1_1)
        /// and generic type names (e.g., List&lt;int&gt;).
        /// </summary>
        /// <param name="value">The string value to escape.</param>
        /// <returns>The escaped string safe for use in XML attributes and content.</returns>
        private static string EscapeXmlString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("&", "&amp;")   // Must be first to avoid double-escaping
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        static List<NPath> ProcessBuildPipelineGenerateAdditionalLinkXmlFiles(BuildPostProcessArgs args)
        {
            var results = new List<NPath>();
            var processors = BuildPipelineInterfaces.processors.unityLinkerProcessors;
            if (processors == null)
                return results;

            NPath stagingAreaManaged = $"{args.stagingAreaData}/Managed";
            var pipelineData = new UnityLinkerBuildPipelineData(args.target, stagingAreaManaged.MakeAbsolute().ToString());

            foreach (var processor in processors)
            {
                results.Add(processor.GenerateAdditionalLinkXmlFile(args.report, pipelineData));
                var processorType = processor.GetType();

                // The OnBeforeRun and OnAfterRun methods are no longer supported. We warn if the project uses them.
                // But since these were interface methods, any project using GenerateAdditionalLinkXmlFile also had to
                // implement these. So we only want to warn if the methods are not empty.
                //
                // To detect if we should consider a method as "empty" we check if the method body has more than 2 bytes.
                // An empty method is 1 byte (ret), or 2 bytes in debug mode (nop, ret). The assumption is that there is
                // no viable void method with side effects having only 2 bytes.

                var onBeforeRun = processorType.GetMethod("OnBeforeRun");
                if (onBeforeRun != null && onBeforeRun.GetMethodBody().GetILAsByteArray().Length > 2)
                    Debug.LogWarning($"{processorType} has a non-empty OnBeforeRun method, but IUnityLinkerProcessor.OnBeforeRun is no longer supported.");
                var onAfterRun = processorType.GetMethod("OnAfterRun");
                if (onAfterRun != null && onAfterRun.GetMethodBody().GetILAsByteArray().Length > 2)
                    Debug.LogWarning($"{processorType} has a non-empty OnAfterRun method, but IUnityLinkerProcessor.OnAfterRun is no longer supported.");
            }

            return results;
        }

        internal static IEnumerable<NPath> GetUserBlacklistFiles()
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return Directory.GetFiles("Assets", "link.xml", SearchOption.AllDirectories)
#pragma warning restore UA2001
                .Select(s => Path.Combine(Directory.GetCurrentDirectory(), s))
                .ToNPaths();
        }

        public static string[] GetLinkXmlFiles(BuildPostProcessArgs args, NPath linkerInputDirectory)
        {
            var linkXmlFiles = new List<NPath>();

            if (args.usedClassRegistry != null)
            {
                var buildFiles = args.report.GetFiles();
                linkXmlFiles.Add(WriteMethodsToPreserveBlackList(args.usedClassRegistry, linkerInputDirectory));
                linkXmlFiles.Add(WriteTypesInScenesBlacklist(args.usedClassRegistry, linkerInputDirectory, buildFiles));
                linkXmlFiles.Add(WriteSerializedTypesBlacklist(args.usedClassRegistry, linkerInputDirectory, buildFiles));
            }

            linkXmlFiles.AddRange(ProcessBuildPipelineGenerateAdditionalLinkXmlFiles(args));
            linkXmlFiles.AddRange(GetUserBlacklistFiles());

            var isMonoBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.FromActiveSettings(args.target)) == ScriptingImplementation.Mono2x;
            if (isMonoBackend)
            {
                // The old Mono assembly stripper uses per-platform link.xml files if available. Apply these here.
                var buildToolsDirectory = BuildPipeline.GetBuildToolsDirectory(args.target);
                if (!string.IsNullOrEmpty(buildToolsDirectory))
                {
                    var platformDescriptor = Path.Combine(buildToolsDirectory, "link.xml");
                    if (File.Exists(platformDescriptor))
                        linkXmlFiles.Add(platformDescriptor);
                }
            }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return linkXmlFiles
#pragma warning restore UA2001
                .Where(p => p?.FileExists() ?? false)
                .Select(p => p.MakeAbsolute().ToString())
                .ToArray();
        }

        static bool BuildFileMatchesAssembly(BuildFile file, string assemblyName)
        {
            return file.path.ToNPath().FileNameWithoutExtension == assemblyName &&
                (file.role == "ManagedLibrary" ||
                    file.role == "DependentManagedLibrary" ||
                    file.role == "ManagedEngineAPI");
        }

        private static NPath WriteTypesInScenesBlacklist(RuntimeClassRegistry rcr, NPath linkerInputDirectory, BuildFile[] buildFiles)
        {
            var items = rcr.GetAllManagedTypesInScenes();

            var sb = new StringBuilder();
            sb.AppendLine("<linker>");
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var assemblyTypePair in items.OrderBy(t => t.Key))
#pragma warning restore UA2001
            {
                // Some how stuff for assemblies that will not be in the build make it into UsedTypePerUserAssembly such as
                // ex: [UnityEditor.TestRunner.dll] UnityEditor.TestTools.TestRunner.TestListCacheData
                //
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about preservations in link xml files
                var filename = assemblyTypePair.Key.ToNPath().FileNameWithoutExtension;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, filename)))
#pragma warning restore UA2001
                    continue;

                if (assemblyTypePair.Value.Length == 0)
                {
                    // There should always be items if an assembly is in the dictionary, but there is/was a bug that could lead to this happen https://jira.unity3d.com/browse/UUM-92357
                    // If there are no items, then we don't want to write out the assembly element.  An assembly element with no types
                    // will result in preserving the entire assembly which is not what we ever want to do.
                    continue;
                }

                sb.AppendLine($"\t<assembly fullname=\"{filename}\">");
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var type in assemblyTypePair.Value.OrderBy(s => s))
#pragma warning restore UA2001
                {
                    sb.AppendLine($"\t\t<type fullname=\"{EscapeXmlString(type)}\" preserve=\"nothing\"/>");
                }
                sb.AppendLine("\t</assembly>");
            }
            sb.AppendLine("</linker>");

            var path = linkerInputDirectory.Combine("TypesInScenes.xml");
            path.WriteAllText(sb.ToString());
            return path;
        }

        private static NPath WriteSerializedTypesBlacklist(RuntimeClassRegistry rcr, NPath linkerInputDirectory, BuildFile[] buildFiles)
        {
            var items = rcr.GetAllSerializedClassesAsString();
            var oneOrMoreItemsWritten = false;
            var sb = new StringBuilder();
            sb.AppendLine("<linker>");
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var assemblyTypePair in items.OrderBy(t => t.Key))
#pragma warning restore UA2001
            {
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about preservations in link xml files
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, assemblyTypePair.Key)))
#pragma warning restore UA2001
                    continue;

                sb.AppendLine($"\t<assembly fullname=\"{assemblyTypePair.Key}\">");
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var type in assemblyTypePair.Value.OrderBy(s => s))
#pragma warning restore UA2001
                {
                    oneOrMoreItemsWritten = true;
                    sb.AppendLine($"\t\t<type fullname=\"{EscapeXmlString(type)}\" preserve=\"nothing\" serialized=\"true\"/>");
                }
                sb.AppendLine("\t</assembly>");
            }
            sb.AppendLine("</linker>");

            // Avoid writing empty files
            if (!oneOrMoreItemsWritten)
                return null;

            var path = linkerInputDirectory.Combine("SerializedTypes.xml");
            path.WriteAllText(sb.ToString());
            return path;
        }

        private static UnityType s_GameManagerTypeInfo = null;
        internal static UnityType GameManagerTypeInfo
        {
            get
            {
                if (s_GameManagerTypeInfo == null)
                {
                    UnityType result = UnityType.FindTypeByName("GameManager");
                    if (result == null)
                        throw new ArgumentException(string.Format("Could not map typename '{0}' to type info ({1})", "GameManager", "initializing code stripping utils"));
                    s_GameManagerTypeInfo = result;
                }

                return s_GameManagerTypeInfo;
            }
        }

        internal static void UpdateBuildReport(LinkerToEditorData dataFromLinker, StrippingInfo strippingInfo)
        {
            foreach (var moduleInfo in dataFromLinker.report.modules)
            {
                strippingInfo.AddModule(moduleInfo.name);
                foreach (var moduleDependency in moduleInfo.dependencies)
                {
                    strippingInfo.RegisterDependency(StrippingInfo.ModuleName(moduleInfo.name), moduleDependency.name);

                    if (!string.IsNullOrEmpty(moduleDependency.icon))
                        strippingInfo.SetIcon(moduleDependency.name, moduleDependency.icon);

                    // Hacky way to match the existing behavior
                    if (moduleDependency.name == "UnityConnectSettings")
                        strippingInfo.RegisterDependency(moduleDependency.name, "Required by UnityAnalytics");

                    foreach (var scene in moduleDependency.scenes)
                    {
                        strippingInfo.RegisterDependency(moduleDependency.name, scene);

                        var klass = UnityType.FindTypeByName(moduleDependency.name);
                        if (klass != null && !klass.IsDerivedFrom(GameManagerTypeInfo))
                        {
                            if (scene.EndsWith(".unity"))
                                strippingInfo.SetIcon(scene, "class/SceneAsset");
                            else
                                strippingInfo.SetIcon(scene, "class/AssetBundle");
                        }
                    }
                }
            }
        }

        internal static LinkerToEditorData ReadLinkerToEditorData(string outputDirectory)
        {
            var dataPath = Path.Combine(outputDirectory, "UnityLinkerToEditorData.json");
            var contents = File.ReadAllText(dataPath);
            var data = JsonUtility.FromJson<LinkerToEditorData>(contents);
            return data;
        }

        public static string WriteEditorData(BuildPostProcessArgs args, NPath linkerInputDirectory)
        {
            CollectIncludedAndExcludedModules(out var forceIncludeModules, out var forceExcludeModules);

            var editorToLinkerData = new EditorToLinkerData
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                typesInScenes = GetTypesInScenesInformation(args.report, args.usedClassRegistry)
#pragma warning restore UA2001
                    .OrderBy(data => data.fullManagedTypeName ?? data.nativeClass)
                    .ToArray(),
                allNativeTypes = CollectNativeTypeData().ToArray(),
                forceIncludeModules = forceIncludeModules.ToArray(),
                forceExcludeModules = forceExcludeModules.ToArray()
            };

            var path = linkerInputDirectory.Combine("EditorToUnityLinkerData.json");
            File.WriteAllText(path.ToString(), JsonUtility.ToJson(editorToLinkerData, true));
            return path.MakeAbsolute().ToString();
        }

        static List<EditorToLinkerData.TypeInSceneData> GetTypesInScenesInformation(BuildReport report, RuntimeClassRegistry rcr)
        {
            var items = new List<EditorToLinkerData.TypeInSceneData>();
            foreach (var nativeClass in rcr.GetAllNativeClassesIncludingManagersAsString())
            {
                var unityType = UnityType.FindTypeByName(nativeClass);

                var managedName = RuntimeClassMetadataUtils.ScriptingWrapperTypeNameForNativeID(unityType.persistentTypeID);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var usedInScenes = rcr.GetScenesForClass(unityType.persistentTypeID)?.OrderBy(p => p);
#pragma warning restore UA2001

                bool noManagedType = unityType.persistentTypeID != 0 && managedName == "UnityEngine.Object";
                var information = new EditorToLinkerData.TypeInSceneData(
                    noManagedType ? null : "UnityEngine.dll",
                    noManagedType ? null : managedName,
                    nativeClass,
                    unityType.module,
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    usedInScenes != null ? usedInScenes.ToArray() : null);
#pragma warning restore UA2001

                items.Add(information);
            }

            var buildFiles = report.GetFiles();
            foreach (var userAssembly in rcr.UsedTypePerUserAssembly)
            {
                // Some how stuff for assemblies that will not be in the build make it into UsedTypePerUserAssembly such as
                // ex: [UnityEditor.TestRunner.dll] UnityEditor.TestTools.TestRunner.TestListCacheData
                //
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about being able to find
                // all of the types that are reported as being in the scene.
                var filename = userAssembly.Key.ToNPath().FileNameWithoutExtension;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, filename)))
#pragma warning restore UA2001
                    continue;

                foreach (var type in userAssembly.Value)
                    items.Add(new EditorToLinkerData.TypeInSceneData(userAssembly.Key, type, null, null, null));
            }

            return items;
        }

        static List<EditorToLinkerData.NativeTypeData> CollectNativeTypeData()
        {
            var items = new List<EditorToLinkerData.NativeTypeData>();
            foreach (var unityType in UnityType.GetTypes())
            {
                items.Add(new EditorToLinkerData.NativeTypeData
                {
                    name = unityType.name,
                    qualifiedName = unityType.qualifiedName,
                    nativeNamespace = unityType.hasNativeNamespace ? unityType.nativeNamespace : null,
                    module = unityType.module,
                    baseName = unityType.baseClass != null ? unityType.baseClass.name : null,
                    baseModule = unityType.baseClass != null ? unityType.baseClass.module : null,
                });
            }

            return items;
        }

        private class AssemblyStripperModuleAdder : IPreStrippingModuleAdder
        {
            private List<string> m_ForceIncludes;

            public AssemblyStripperModuleAdder(List<string> forceIncludes)
            {
                m_ForceIncludes = forceIncludes;
            }

            public void AddModule(string moduleName)
            {
                if (IsModuleIncluded(moduleName))
                    return;

                m_ForceIncludes.Add(moduleName);
            }

            public bool IsModuleIncluded(string moduleName)
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                => m_ForceIncludes.Any(i => string.Equals(i, moduleName));
#pragma warning restore UA2001
        }

        public static event Action<IPreStrippingModuleAdder> onCollectIncludedModules;

        static void CollectIncludedAndExcludedModules(out List<string> forceInclude, out List<string> forceExclude)
        {
            forceInclude = new List<string>();
            forceExclude = new List<string>();
            // Apply manual stripping overrides
            foreach (var module in ModuleMetadata.GetModuleNames())
            {
                var includeSetting = ModuleMetadata.GetModuleIncludeSettingForModule(module);
                if (includeSetting == ModuleIncludeSetting.ForceInclude)
                    forceInclude.Add(module);
                else if (includeSetting == ModuleIncludeSetting.ForceExclude)
                    forceExclude.Add(module);
            }

            var adder = new AssemblyStripperModuleAdder(forceInclude);
            onCollectIncludedModules?.Invoke(adder);
        }

        private static NPath WriteMethodsToPreserveBlackList(RuntimeClassRegistry rcr, NPath linkerInputDirectory)
        {
            var contents = GetMethodPreserveBlacklistContents(rcr);
            if (contents == null)
                return null;
            var methodPreserveBlackList = linkerInputDirectory.Combine("MethodsToPreserve.xml");
            methodPreserveBlackList.WriteAllText(contents);
            return methodPreserveBlackList;
        }

        private static string GetMethodPreserveBlacklistContents(RuntimeClassRegistry rcr)
        {
            if (rcr.GetMethodsToPreserve().Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine("<linker>");

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var groupedByAssembly = rcr.GetMethodsToPreserve().GroupBy(m => m.assembly);
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var assembly in groupedByAssembly.OrderBy(a => a.Key))
#pragma warning restore UA2001
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var assemblyName = assembly.Key;
#pragma warning restore UA2001
                sb.AppendLine(string.Format("\t<assembly fullname=\"{0}\" ignoreIfMissing=\"1\">", assemblyName));
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var groupedByType = assembly.GroupBy(m => m.fullTypeName);
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var type in groupedByType.OrderBy(t => t.Key))
#pragma warning restore UA2001
                {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    sb.AppendLine(string.Format("\t\t<type fullname=\"{0}\">", EscapeXmlString(type.Key)));
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var method in type.OrderBy(m => m.methodName))
#pragma warning restore UA2001
                        sb.AppendLine(string.Format("\t\t\t<method name=\"{0}\"/>", EscapeXmlString(method.methodName)));
                    sb.AppendLine("\t\t</type>");
                }
                sb.AppendLine("\t</assembly>");
            }

            sb.AppendLine("</linker>");
            return sb.ToString();
        }
    }
}
