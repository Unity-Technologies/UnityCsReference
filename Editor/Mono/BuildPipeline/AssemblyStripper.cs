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
using UnityEditor.BuildReporting;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;
using PackageInfo = Unity.DataContract.PackageInfo;

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
                return new[] {
                    Path.Combine("..", "platform_native_link.xml")
                };
            }
        }

        private static string MonoLinker2Path
        {
            get
            {
                return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "il2cpp/build/UnityLinker.exe");
            }
        }

        private static string GetModuleWhitelist(string module, string moduleStrippingInformationFolder)
        {
            return Paths.Combine(moduleStrippingInformationFolder, module + ".xml");
        }

        private static bool StripAssembliesTo(string[] assemblies, string[] searchDirs, string outputFolder, string workingDirectory, out string output, out string error, string linkerPath, IIl2CppPlatformProvider platformProvider, IEnumerable<string> additionalBlacklist)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            additionalBlacklist = additionalBlacklist.Select(s => Path.IsPathRooted(s) ? s : Path.Combine(workingDirectory, s)).Where(File.Exists);

            var userBlackLists = GetUserBlacklistFiles();

            foreach (var ub in userBlackLists)
                Console.WriteLine("UserBlackList: " + ub);

            additionalBlacklist = additionalBlacklist.Concat(userBlackLists);

            var args = new List<string>
            {
                "--api=" + PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.activeBuildTargetGroup).ToString(),
                "-out=\"" + outputFolder + "\"",
                "-l=none",
                "-c=link",
                "--link-symbols",
                "-x=\"" + GetModuleWhitelist("Core", platformProvider.moduleStrippingInformationFolder) + "\"",
                "-f=\"" + Path.Combine(platformProvider.il2CppFolder, "LinkerDescriptors") + "\""
            };
            args.AddRange(additionalBlacklist.Select(path => "-x \"" + path + "\""));

            args.AddRange(searchDirs.Select(d => "-d \"" + d + "\""));
            args.AddRange(assemblies.Select(assembly => "-a  \"" + Path.GetFullPath(assembly) + "\""));

            return RunAssemblyLinker(args, out output, out error, linkerPath, workingDirectory);
        }

        private static bool RunAssemblyLinker(IEnumerable<string> args, out string @out, out string err, string linkerPath, string workingDirectory)
        {
            var argString = args.Aggregate((buff, s) => buff + " " + s);
            Console.WriteLine("Invoking UnityLinker with arguments: " + argString);
            Runner.RunManagedProgram(linkerPath, argString, workingDirectory, null, null);

            @out = "";
            err = "";

            return true;
        }

        private static List<string> GetUserAssemblies(RuntimeClassRegistry rcr, string managedDir)
        {
            return rcr.GetUserAssemblies().Where(s => rcr.IsDLLUsed(s)).Select(s => Path.Combine(managedDir, s)).ToList();
        }

        internal static void StripAssemblies(string stagingAreaData, IIl2CppPlatformProvider platformProvider, RuntimeClassRegistry rcr)
        {
            var managedAssemblyFolderPath = Path.GetFullPath(Path.Combine(stagingAreaData, "Managed"));
            var assemblies = GetUserAssemblies(rcr, managedAssemblyFolderPath);
            assemblies.AddRange(Directory.GetFiles(managedAssemblyFolderPath, "I18N*.dll", SearchOption.TopDirectoryOnly));
            var assembliesToStrip = assemblies.ToArray();

            var searchDirs = new[]
            {
                managedAssemblyFolderPath
            };

            RunAssemblyStripper(stagingAreaData, assemblies, managedAssemblyFolderPath, assembliesToStrip, searchDirs, MonoLinker2Path, platformProvider, rcr);
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

        internal static IEnumerable<string> GetUserBlacklistFiles()
        {
            return Directory.GetFiles("Assets", "link.xml", SearchOption.AllDirectories).Select(s => Path.Combine(Directory.GetCurrentDirectory(), s));
        }

        private static bool AddWhiteListsForModules(IEnumerable<string> nativeModules, ref IEnumerable<string> blacklists, string moduleStrippingInformationFolder)
        {
            bool result = false;
            foreach (var module in nativeModules)
            {
                var moduleWhitelist = GetModuleWhitelist(module, moduleStrippingInformationFolder);

                if (File.Exists(moduleWhitelist))
                {
                    if (!blacklists.Contains(moduleWhitelist))
                    {
                        blacklists = blacklists.Concat(new[] { moduleWhitelist });
                        result = true;
                    }
                }
            }
            return result;
        }

        private static void RunAssemblyStripper(string stagingAreaData, IEnumerable assemblies, string managedAssemblyFolderPath, string[] assembliesToStrip, string[] searchDirs, string monoLinkerPath, IIl2CppPlatformProvider platformProvider, RuntimeClassRegistry rcr)
        {
            string output;
            string error;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platformProvider.target);
            bool isMono = PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.Mono2x;
            bool stripEngineCode = rcr != null && PlayerSettings.stripEngineCode && platformProvider.supportsEngineStripping;
            IEnumerable<string> blacklists = Il2CppBlacklistPaths;
            if (rcr != null)
            {
                blacklists = blacklists.Concat(new[] {
                    WriteMethodsToPreserveBlackList(stagingAreaData, rcr, platformProvider.target),
                    WriteUnityEngineBlackList(stagingAreaData),
                    MonoAssemblyStripping.GenerateLinkXmlToPreserveDerivedTypes(stagingAreaData, managedAssemblyFolderPath, rcr)
                });
            }

            if (PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
            {
                var il2cppFolder = Path.Combine(platformProvider.il2CppFolder, "LinkerDescriptors");
                blacklists = blacklists.Concat(Directory.GetFiles(il2cppFolder, "*45.xml"));
            }
            if (isMono)
            {
                // Apply mono-specific assembly whitelists, taken from old Mono assembly stripping code path.
                var il2cppFolder = Path.Combine(platformProvider.il2CppFolder, "LinkerDescriptors");
                blacklists = blacklists.Concat(Directory.GetFiles(il2cppFolder, "*_mono.xml"));

                // The old Mono assembly stripper uses per-platform link.xml files if available. Apply these here.
                var platformDescriptor = Path.Combine(BuildPipeline.GetBuildToolsDirectory(platformProvider.target), "link.xml");
                if (File.Exists(platformDescriptor))
                    blacklists = blacklists.Concat(new[] {platformDescriptor});
            }

            if (!stripEngineCode)
            {
                // if we don't do stripping, add all modules blacklists.
                foreach (var file in Directory.GetFiles(platformProvider.moduleStrippingInformationFolder, "*.xml"))
                    blacklists = blacklists.Concat(new[] {file});
            }

            var tempStripPath = Path.GetFullPath(Path.Combine(managedAssemblyFolderPath, "tempStrip"));

            bool addedMoreBlacklists;
            do
            {
                addedMoreBlacklists = false;

                if (EditorUtility.DisplayCancelableProgressBar("Building Player", "Stripping assemblies", 0.0f))
                    throw new OperationCanceledException();

                if (!StripAssembliesTo(
                        assembliesToStrip,
                        searchDirs,
                        tempStripPath,
                        managedAssemblyFolderPath,
                        out output,
                        out error,
                        monoLinkerPath,
                        platformProvider,
                        blacklists))
                    throw new Exception("Error in stripping assemblies: " + assemblies + ", " + error);

                if (platformProvider.supportsEngineStripping)
                {
                    var icallSummaryPath = Path.Combine(managedAssemblyFolderPath, "ICallSummary.txt");
                    GenerateInternalCallSummaryFile(icallSummaryPath, managedAssemblyFolderPath, tempStripPath);

                    if (stripEngineCode)
                    {
                        // Find which modules we must include in the build based on Assemblies
                        HashSet<UnityType> nativeClasses;
                        HashSet<string> nativeModules;
                        CodeStrippingUtils.GenerateDependencies(tempStripPath, icallSummaryPath, rcr, stripEngineCode, out nativeClasses, out nativeModules, platformProvider);
                        // Add module-specific blacklists.
                        addedMoreBlacklists = AddWhiteListsForModules(nativeModules, ref blacklists, platformProvider.moduleStrippingInformationFolder);
                    }
                }

                // If we had to add more whitelists, we need to run AssemblyStripper again with the added whitelists.
            }
            while (addedMoreBlacklists);

            // keep unstripped files for debugging purposes
            var tempUnstrippedPath = Path.GetFullPath(Path.Combine(managedAssemblyFolderPath, "tempUnstripped"));
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
            Directory.Delete(tempStripPath);
        }

        private static string WriteMethodsToPreserveBlackList(string stagingAreaData, RuntimeClassRegistry rcr, BuildTarget target)
        {
            var methodPerserveBlackList = Path.IsPathRooted(stagingAreaData) ? "" : Directory.GetCurrentDirectory() + "/";
            methodPerserveBlackList += stagingAreaData  + "/methods_pointedto_by_uievents.xml";
            File.WriteAllText(methodPerserveBlackList, GetMethodPreserveBlacklistContents(rcr, target));
            return methodPerserveBlackList;
        }

        private static string WriteUnityEngineBlackList(string stagingAreaData)
        {
            // UnityEngine.dll would be stripped, as it contains no referenced symbols, only type forwarders.
            // Since we need those type forwarders, we generate blacklist to preserve the assembly (but no members).
            var unityEngineBlackList = Path.IsPathRooted(stagingAreaData) ? "" : Directory.GetCurrentDirectory() + "/";
            unityEngineBlackList += stagingAreaData  + "/UnityEngine.xml";
            File.WriteAllText(unityEngineBlackList, "<linker><assembly fullname=\"UnityEngine\" preserve=\"nothing\"/></linker>");
            return unityEngineBlackList;
        }

        private static string GetMethodPreserveBlacklistContents(RuntimeClassRegistry rcr, BuildTarget target)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<linker>");

            var groupedByAssembly = rcr.GetMethodsToPreserve().GroupBy(m => m.assembly);
            foreach (var assembly in groupedByAssembly)
            {
                var assemblyName = assembly.Key;
                // Convert assembly names to monolithic Unity Engine if target platform requires it.
                if (AssemblyHelper.IsUnityEngineModule(assemblyName) && !BuildPipeline.IsFeatureSupported("ENABLE_MODULAR_UNITYENGINE_ASSEMBLIES", target))
                    sb.AppendLine(string.Format("\t<assembly fullname=\"{0}\" ignoreIfMissing=\"1\">", "UnityEngine"));
                else
                    sb.AppendLine(string.Format("\t<assembly fullname=\"{0}\" ignoreIfMissing=\"1\">", assemblyName));
                var groupedByType = assembly.GroupBy(m => m.fullTypeName);
                foreach (var type in groupedByType)
                {
                    sb.AppendLine(string.Format("\t\t<type fullname=\"{0}\">", type.Key));
                    foreach (var method in type)
                        sb.AppendLine(string.Format("\t\t\t<method name=\"{0}\"/>", method.methodName));
                    sb.AppendLine("\t\t</type>");
                }
                sb.AppendLine("\t</assembly>");
            }

            sb.AppendLine("</linker>");
            return sb.ToString();
        }

        static public void InvokeFromBuildPlayer(BuildTarget buildTarget, RuntimeClassRegistry usedClasses)
        {
            var stagingAreaData = Paths.Combine("Temp", "StagingArea", "Data");

            var platformProvider = new BaseIl2CppPlatformProvider(buildTarget, Path.Combine(stagingAreaData, "Libraries"));

            AssemblyStripper.StripAssemblies(stagingAreaData, platformProvider, usedClasses);
        }
    }
}
