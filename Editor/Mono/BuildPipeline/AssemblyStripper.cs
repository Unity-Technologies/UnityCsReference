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
using NiceIO;
using UnityEditor.Build.Reporting;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.UnityLinker;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;

namespace UnityEditorInternal
{
    internal class AssemblyStripper
    {
        private static bool debugUnstripped
        {
            get
            {
                return false;
            }
        }

        private static string[] Il2CppBlacklistPaths
        {
            get
            {
                return new[]
                {
                    Path.Combine("..", "platform_native_link.xml")
                };
            }
        }

        public static string UnityLinkerPath
        {
            get
            {
                return IL2CPPUtils.GetExePath("UnityLinker");
            }
        }

        static IEnumerable<string> SanitizeLinkXmlFilePaths(List<string> linkXmlFilePaths, UnityLinkerRunInformation runInformation)
        {
            foreach (var linkXmlFilePath in linkXmlFilePaths)
            {
                // Generated link xml files that would have been empty will be nulled out.  Need to filter these out before running the linker
                if (string.IsNullOrEmpty(linkXmlFilePath))
                    continue;

                var absolutePath = linkXmlFilePath;
                if (!Path.IsPathRooted(linkXmlFilePath))
                    absolutePath = Path.Combine(runInformation.managedAssemblyFolderPath, linkXmlFilePath);

                if (File.Exists(absolutePath))
                    yield return absolutePath;
            }
        }

        private static string GetFullPath(string path)
        {
            return IL2CPPBuilder.GetShortPathName(Path.GetFullPath(path));
        }

        private static bool StripAssembliesTo(string outputFolder, out string output, out string error, IEnumerable<string> linkXmlFiles, UnityLinkerRunInformation runInformation)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var assemblies = runInformation.AssembliesToProcess();

            var args = new List<string>
            {
                $"-out={CommandLineFormatter.PrepareFileName(outputFolder)}",
            };

            args.AddRange(linkXmlFiles.Select(path => $"-x={CommandLineFormatter.PrepareFileName(path)}"));
            args.AddRange(runInformation.SearchDirectories.Select(d => $"-d={CommandLineFormatter.PrepareFileName(d)}"));
            args.AddRange(assemblies.Select(assembly => $"--include-unity-root-assembly={CommandLineFormatter.PrepareFileName(GetFullPath(assembly))}"));
            args.Add($"--dotnetruntime={runInformation.argumentProvider.Runtime}");
            args.Add($"--dotnetprofile={runInformation.argumentProvider.Profile}");
            args.Add("--use-editor-options");
            args.Add($"--include-directory={CommandLineFormatter.PrepareFileName(runInformation.managedAssemblyFolderPath)}");

            if (EditorUserBuildSettings.allowDebugging)
                args.Add("--editor-settings-flag=AllowDebugging");

            if (EditorUserBuildSettings.development)
                args.Add("--editor-settings-flag=Development");

            args.Add($"--rule-set={runInformation.argumentProvider.RuleSet}");
            args.Add($"--editor-data-file={CommandLineFormatter.PrepareFileName(runInformation.EditorToLinkerDataPath)}");

            if (runInformation.platformProvider.AllowOutputToBeMadePlatformDependent)
            {
                var platform = runInformation.platformProvider.Platform;
                if (string.IsNullOrEmpty(platform))
                    throw new ArgumentException("Platform is required if AllowOutputToBeMadePlatformDependent is true");

                args.Add($"--platform={platform}");
            }

            if (runInformation.platformProvider.AllowOutputToBeMadeArchitectureDependent)
            {
                var architecture = runInformation.platformProvider.Architecture;
                if (string.IsNullOrEmpty(architecture))
                    throw new ArgumentException("Architecture is required if AllowOutputToBeMadeArchitectureDependent is true");
                args.Add($"--architecture={architecture}");
            }

            if (runInformation.performEngineStripping)
            {
                args.Add("--enable-engine-module-stripping");

                if (UnityEngine.Connect.UnityConnectSettings.enabled)
                    args.Add("--engine-stripping-flag=EnableUnityConnect");

                if (UnityEngine.Analytics.PerformanceReporting.enabled)
                    args.Add("--engine-stripping-flag=EnablePerformanceReporting");

                if (UnityEngine.Analytics.Analytics.enabled)
                    args.Add("--engine-stripping-flag=EnableAnalytics");

                if (UnityEditor.CrashReporting.CrashReportingSettings.enabled)
                    args.Add("--engine-stripping-flag=EnableCrashReporting");
            }

            var modulesAssetPath = runInformation.ModulesAssetFilePath;
            if (File.Exists(modulesAssetPath))
                args.Add($"--engine-modules-asset-file={CommandLineFormatter.PrepareFileName(modulesAssetPath)}");

            var additionalArgs = System.Environment.GetEnvironmentVariable("UNITYLINKER_ADDITIONAL_ARGS");
            if (!string.IsNullOrEmpty(additionalArgs))
                args.Add(additionalArgs);

            additionalArgs = Debug.GetDiagnosticSwitch("VMUnityLinkerAdditionalArgs").value as string;
            if (!string.IsNullOrEmpty(additionalArgs))
                args.Add(additionalArgs.Trim('\''));

            return RunAssemblyLinker(args, out output, out error, UnityLinkerPath, runInformation.managedAssemblyFolderPath);
        }

        private static bool RunAssemblyLinker(IEnumerable<string> args, out string @out, out string err, string linkerPath, string workingDirectory)
        {
            var argString = args.Aggregate((buff, s) => buff + " " + s);
            var responseFile = Path.Combine(workingDirectory, "response.rsp");
            File.WriteAllText(responseFile, argString);
            UnityLogWriter.WriteStringToUnityLog($"Invoking UnityLinker with response file. response.rsp contents: {argString}\n");
            Runner.RunNetCoreProgram(linkerPath, $"@{CommandLineFormatter.PrepareFileName(responseFile)}", workingDirectory, null, null);

            @out = "";
            err = "";

            return true;
        }

        internal static void StripAssemblies(string managedAssemblyFolderPath, BaseUnityLinkerPlatformProvider unityLinkerPlatformProvider, IIl2CppPlatformProvider il2cppPlatformProvider,
            RuntimeClassRegistry rcr, ManagedStrippingLevel managedStrippingLevel)
        {
            var runInformation = new UnityLinkerRunInformation(IL2CPPBuilder.GetShortPathName(managedAssemblyFolderPath), unityLinkerPlatformProvider, il2cppPlatformProvider.target, rcr, managedStrippingLevel, il2cppPlatformProvider);
            RunAssemblyStripper(runInformation);
        }

        internal static void GenerateInternalCallSummaryFile(string icallSummaryPath, string managedAssemblyFolderPath, string strippedDLLPath)
        {
            var exe = Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Tools/InternalCallRegistrationWriter/InternalCallRegistrationWriter.exe");
            var dlls = Directory.GetFiles(strippedDLLPath, "UnityEngine.*Module.dll").Concat(new[] {Path.Combine(strippedDLLPath, "UnityEngine.dll")});
            var args = string.Format("-output=\"{0}\" -summary=\"{1}\" -assembly=\"{2}\"",
                Path.Combine(managedAssemblyFolderPath, "UnityICallRegistration.cpp"),
                icallSummaryPath,
                dlls.Aggregate((dllArg, next) => dllArg + ";" + next)
            );
            Runner.RunManagedProgram(exe, args);
        }

        static List<string> ProcessBuildPipelineGenerateAdditionalLinkXmlFiles(UnityLinkerRunInformation runInformation)
        {
            var results = new List<string>();
            var processors = BuildPipelineInterfaces.processors.unityLinkerProcessors;
            if (processors == null)
                return results;

            foreach (var processor in processors)
            {
                results.Add(processor.GenerateAdditionalLinkXmlFile(runInformation.BuildReport, runInformation.pipelineData));
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

        internal static IEnumerable<string> GetUserBlacklistFiles()
        {
            return Directory.GetFiles("Assets", "link.xml", SearchOption.AllDirectories).Select(s => Path.Combine(Directory.GetCurrentDirectory(), s));
        }

        private static void RunAssemblyStripper(UnityLinkerRunInformation runInformation)
        {
            string output;
            string error;
            var managedAssemblyFolderPath = runInformation.managedAssemblyFolderPath;
            var linkXmlFiles = GetLinkXmlFiles(runInformation);

            WriteEditorData(runInformation);

            var tempStripPath = GetFullPath(Path.Combine(managedAssemblyFolderPath, "tempStrip"));

            if (EditorUtility.DisplayCancelableProgressBar("Building Player", "Stripping assemblies", 0.0f))
                throw new OperationCanceledException();

            if (!StripAssembliesTo(
                tempStripPath,
                out output,
                out error,
                linkXmlFiles,
                runInformation))
                throw new Exception("Error in stripping assemblies: " + runInformation.AssembliesToProcess() + ", " + error);

            if (runInformation.engineStrippingSupported)
            {
                var icallSummaryPath = Path.Combine(managedAssemblyFolderPath, "ICallSummary.txt");
                GenerateInternalCallSummaryFile(icallSummaryPath, managedAssemblyFolderPath, tempStripPath);
            }

            if (runInformation.performEngineStripping)
            {
                var strippingInfo = runInformation.BuildReportData;

                if (strippingInfo != null)
                    UpdateBuildReport(ReadLinkerToEditorData(tempStripPath), strippingInfo);
            }

            // keep unstripped files for debugging purposes
            var tempUnstrippedPath = GetFullPath(Path.Combine(managedAssemblyFolderPath, "tempUnstripped"));
            if (debugUnstripped)
                Directory.CreateDirectory(tempUnstrippedPath);
            foreach (var file in Directory.GetFiles(managedAssemblyFolderPath))
            {
                var extension = Path.GetExtension(file);
                if (string.Equals(extension, ".dll", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(extension, ".winmd", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(extension, ".mdb", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(extension, ".pdb", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (debugUnstripped)
                        File.Move(file, Path.Combine(tempUnstrippedPath, Path.GetFileName(file)));
                    else
                        File.Delete(file);
                }
            }

            foreach (var file in Directory.GetFiles(tempStripPath))
                File.Move(file, Path.Combine(managedAssemblyFolderPath, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(tempStripPath))
                Directory.Move(dir, Path.Combine(managedAssemblyFolderPath, Path.GetFileName(dir)));
            Directory.Delete(tempStripPath);
        }

        public static List<string> GetLinkXmlFiles(UnityLinkerRunInformation runInformation)
        {
            var managedAssemblyFolderPath = runInformation.managedAssemblyFolderPath;

            var linkXmlFiles = new List<string>();
            linkXmlFiles.AddRange(Il2CppBlacklistPaths);

            if (runInformation.rcr != null)
            {
                var buildFiles = runInformation.BuildReport.GetFiles();
                linkXmlFiles.Add(WriteMethodsToPreserveBlackList(runInformation));
                linkXmlFiles.Add(WriteTypesInScenesBlacklist(runInformation, buildFiles));
                linkXmlFiles.Add(WriteSerializedTypesBlacklist(runInformation, buildFiles));
            }

            linkXmlFiles.AddRange(ProcessBuildPipelineGenerateAdditionalLinkXmlFiles(runInformation));
            linkXmlFiles.AddRange(GetUserBlacklistFiles());

            if (runInformation.isMonoBackend)
            {
                // The old Mono assembly stripper uses per-platform link.xml files if available. Apply these here.
                var buildToolsDirectory = BuildPipeline.GetBuildToolsDirectory(runInformation.target);
                if (!string.IsNullOrEmpty(buildToolsDirectory))
                {
                    var platformDescriptor = Path.Combine(buildToolsDirectory, "link.xml");
                    if (File.Exists(platformDescriptor))
                        linkXmlFiles.Add(platformDescriptor);
                }
            }

            return SanitizeLinkXmlFilePaths(linkXmlFiles, runInformation).ToList();
        }

        static bool BuildFileMatchesAssembly(BuildFile file, string assemblyName)
        {
            return file.path.ToNPath().FileNameWithoutExtension == assemblyName &&
                (file.role == "ManagedLibrary" ||
                    file.role == "DependentManagedLibrary" ||
                    file.role == "ManagedEngineAPI");
        }

        private static string WriteTypesInScenesBlacklist(UnityLinkerRunInformation runInformation, BuildFile[] buildFiles)
        {
            var items = runInformation.rcr.GetAllManagedTypesInScenes();

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

            var path = Path.Combine(runInformation.managedAssemblyFolderPath, "TypesInScenes.xml");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static string WriteSerializedTypesBlacklist(UnityLinkerRunInformation runInformation, BuildFile[] buildFiles)
        {
            var items = runInformation.rcr.GetAllSerializedClassesAsString();
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

            var path = Path.Combine(runInformation.managedAssemblyFolderPath, "SerializedTypes.xml");
            File.WriteAllText(path, sb.ToString());
            return path;
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
                        if (klass != null && !klass.IsDerivedFrom(CodeStrippingUtils.GameManagerTypeInfo))
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

        public static void WriteEditorData(UnityLinkerRunInformation runInformation)
        {
            var items = GetTypesInScenesInformation(runInformation.managedAssemblyFolderPath, runInformation.rcr).OrderBy(data => data.fullManagedTypeName ?? data.nativeClass);

            List<string> forceIncludeModules;
            List<string> forceExcludeModules;
            CollectIncludedAndExcludedModules(out forceIncludeModules, out forceExcludeModules);

            var editorToLinkerData = new EditorToLinkerData
            {
                typesInScenes = items.ToArray(),
                allNativeTypes = CollectNativeTypeData().ToArray(),
                forceIncludeModules = forceIncludeModules.ToArray(),
                forceExcludeModules = forceExcludeModules.ToArray()
            };

            File.WriteAllText(runInformation.EditorToLinkerDataPath, JsonUtility.ToJson(editorToLinkerData, true));
        }

        static List<EditorToLinkerData.TypeInSceneData> GetTypesInScenesInformation(string managedAssemblyDirectory, RuntimeClassRegistry rcr)
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

            foreach (var userAssembly in rcr.UsedTypePerUserAssembly)
            {
                // Some how stuff for assemblies that will not be in the build make it into UsedTypePerUserAssembly such as
                // ex: [UnityEditor.TestRunner.dll] UnityEditor.TestTools.TestRunner.TestListCacheData
                //
                // Filter anything out where the assembly doesn't exist so that UnityLinker can be strict about being able to find
                // all of the types that are reported as being in the scene.
                if (!File.Exists(Path.Combine(managedAssemblyDirectory, userAssembly.Key)))
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

        private static string WriteMethodsToPreserveBlackList(UnityLinkerRunInformation runInformation)
        {
            var contents = GetMethodPreserveBlacklistContents(runInformation.rcr);
            if (contents == null)
                return null;
            var methodPreserveBlackList = Path.Combine(runInformation.managedAssemblyFolderPath, "MethodsToPreserve.xml");
            File.WriteAllText(methodPreserveBlackList, contents);
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

        static public void StripForMonoBackend(BuildTarget buildTarget, RuntimeClassRegistry usedClasses, ManagedStrippingLevel managedStrippingLevel, BuildReport report)
        {
            if (managedStrippingLevel == ManagedStrippingLevel.Disabled)
                return;

            var stagingAreaData = Paths.Combine("Temp", "StagingArea", "Data");

            var il2cppPlatformProvider = new MonoBackendIl2CppPlatformProvider(buildTarget, Path.Combine(stagingAreaData, "Libraries"), report);
            var platformProvider = new MonoBackendUnityLinkerPlatformProvider(buildTarget);

            var managedAssemblyFolderPath = GetFullPath(Path.Combine(stagingAreaData, "Managed"));
            AssemblyStripper.StripAssemblies(managedAssemblyFolderPath, platformProvider, il2cppPlatformProvider, usedClasses, managedStrippingLevel);
        }

        class MonoBackendIl2CppPlatformProvider : BaseIl2CppPlatformProvider
        {
            public MonoBackendIl2CppPlatformProvider(BuildTarget target, string libraryFolder, BuildReport buildReport)
                : base(target, libraryFolder, buildReport, null)
            {
            }

            public override BaseUnityLinkerPlatformProvider CreateUnityLinkerPlatformProvider()
            {
                throw new NotSupportedException();
            }
        }

        class MonoBackendUnityLinkerPlatformProvider : BaseUnityLinkerPlatformProvider
        {
            private readonly string m_Platform;
            private readonly string m_Architecture;

            public MonoBackendUnityLinkerPlatformProvider(BuildTarget target) : base(target)
            {
                GetUnityLinkerPlatformStringsFromBuildTarget(target, out m_Platform, out m_Architecture);
            }

            public override string Platform => m_Platform;

            public override string Architecture => m_Architecture;

            public override bool AllowOutputToBeMadeArchitectureDependent => !string.IsNullOrEmpty(m_Architecture);

            public override bool supportsEngineStripping => false;

            private static void GetUnityLinkerPlatformStringsFromBuildTarget(BuildTarget target, out string platform, out string architecture)
            {
                switch (target)
                {
                    case BuildTarget.StandaloneWindows64:
                        platform = "WindowsDesktop";
                        architecture = "x64";
                        break;
                    case BuildTarget.StandaloneWindows:
                        platform = "WindowsDesktop";
                        architecture = "x86";
                        break;
                    case BuildTarget.Android:
                        // Do not supply architecture for Android.
                        // The build pipeline bundles multiple architectures for Android.
                        // Can't narrow down to a specific architecture at strip time, we work around
                        // that fact in the UnityLinker.
                        platform = "Android";
                        architecture = "";
                        break;
                    case BuildTarget.CloudRendering:
                    case BuildTarget.StandaloneLinux64:
                        platform = "Linux";
                        architecture = "x64";
                        break;
                    case BuildTarget.StandaloneOSX:
                        platform = "MacOSX";
                        architecture = "";
                        break;
                    case BuildTarget.WSAPlayer:
                        platform = "WinRT";
                        // Could be multiple values.  We don't have use of this information yet so don't bother with trying to figure out what it should be
                        architecture = "";
                        break;
                    case BuildTarget.iOS:
                        platform = "iOS";
                        architecture = "ARM64";
                        break;
                    case BuildTarget.tvOS:
                        platform = "tvOS";
                        architecture = "ARM64";
                        break;
                    default:
                        throw new ArgumentException($"Mapping to UnityLinker platform not implemented for {nameof(BuildTarget)} `{target}`");
                }
            }
        }
    }
}
