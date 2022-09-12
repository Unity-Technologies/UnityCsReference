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
            return Directory.GetFiles("Assets", "link.xml", SearchOption.AllDirectories)
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

            return linkXmlFiles
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
            foreach (var assemblyTypePair in items.OrderBy(t => t.Key))
            {
                // Some how stuff for assemblies that will not be in the build make it into UsedTypePerUserAssembly such as
                // ex: [UnityEditor.TestRunner.dll] UnityEditor.TestTools.TestRunner.TestListCacheData
                //
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about preservations in link xml files
                var filename = assemblyTypePair.Key.ToNPath().FileNameWithoutExtension;
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, filename)))
                    continue;

                sb.AppendLine($"\t<assembly fullname=\"{filename}\">");
                foreach (var type in assemblyTypePair.Value.OrderBy(s => s))
                {
                    sb.AppendLine($"\t\t<type fullname=\"{type}\" preserve=\"nothing\"/>");
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
            foreach (var assemblyTypePair in items.OrderBy(t => t.Key))
            {
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about preservations in link xml files
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, assemblyTypePair.Key)))
                    continue;

                sb.AppendLine($"\t<assembly fullname=\"{assemblyTypePair.Key}\">");
                foreach (var type in assemblyTypePair.Value.OrderBy(s => s))
                {
                    oneOrMoreItemsWritten = true;
                    sb.AppendLine($"\t\t<type fullname=\"{type}\" preserve=\"nothing\" serialized=\"true\"/>");
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
                typesInScenes = GetTypesInScenesInformation(args.report, args.usedClassRegistry)
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
                var usedInScenes = rcr.GetScenesForClass(unityType.persistentTypeID)?.OrderBy(p => p);

                bool noManagedType = unityType.persistentTypeID != 0 && managedName == "UnityEngine.Object";
                var information = new EditorToLinkerData.TypeInSceneData(
                    noManagedType ? null : "UnityEngine.dll",
                    noManagedType ? null : managedName,
                    nativeClass,
                    unityType.module,
                    usedInScenes != null ? usedInScenes.ToArray() : null);

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
                if (buildFiles.All(file => !BuildFileMatchesAssembly(file, filename)))
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

            var groupedByAssembly = rcr.GetMethodsToPreserve().GroupBy(m => m.assembly);
            foreach (var assembly in groupedByAssembly.OrderBy(a => a.Key))
            {
                var assemblyName = assembly.Key;
                sb.AppendLine(string.Format("\t<assembly fullname=\"{0}\" ignoreIfMissing=\"1\">", assemblyName));
                var groupedByType = assembly.GroupBy(m => m.fullTypeName);
                foreach (var type in groupedByType.OrderBy(t => t.Key))
                {
                    sb.AppendLine(string.Format("\t\t<type fullname=\"{0}\">", type.Key));
                    foreach (var method in type.OrderBy(m => m.methodName))
                        sb.AppendLine(string.Format("\t\t\t<method name=\"{0}\"/>", method.methodName));
                    sb.AppendLine("\t\t</type>");
                }
                sb.AppendLine("\t</assembly>");
            }

            sb.AppendLine("</linker>");
            return sb.ToString();
        }
    }
}
