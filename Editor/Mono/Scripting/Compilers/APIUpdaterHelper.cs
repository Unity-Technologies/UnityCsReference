// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.Environment;

using UnityEditor.PackageManager;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEditorInternal.APIUpdating;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Scripting.Compilers
{
    class APIUpdaterHelper
    {
        public static string[] FilterReferenceToTypeWithChangedNamespaceErrors(string candidateErrorFilePath)
        {
            void PrepareFileWithTypesWithMovedFromAttribute(string filePath)
            {
                // builds a list of types that `MovedFromAttribute` applied to it, groupped by assembly
                var sb1 = new StringBuilder();
                var searchPaths = new[]
                {
                    Path.Combine(MonoFrameworkPath, "Managed"),
                    Path.Combine(MonoFrameworkPath, "Managed/UnityEngine"),
                    Application.dataPath
                };

                sb1.AppendLine($"Search Paths:{searchPaths.Length}");
                foreach (var sp in searchPaths)
                    sb1.AppendLine(sp);

                var g = TypeCache.GetTypesWithAttribute(typeof(MovedFromAttribute)).GroupBy(t => t.Assembly.Location);
                foreach (var byAssembly in g)
                {
                    sb1.AppendLine($"{byAssembly.Key}:{byAssembly.Count()}");
                    foreach (var tt in byAssembly)
                    {
                        sb1.AppendLine(tt.FullName);
                    }
                }

                sb1.Remove(sb1.Length - Environment.NewLine.Length, Environment.NewLine.Length);  // remove last new line
                File.WriteAllText(filePath, sb1.ToString());
            }

            static string Quote(string path)
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    return CommandLineFormatter.EscapeCharsWindows(path);
                }

                return CommandLineFormatter.EscapeCharsQuote(path);
            }

            static string NonObsoleteApiUpdaterDetectorPath()
            {
                return EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/APIUpdater.NonObsoleteApiUpdaterDetector.exe";
            }

            var typesWithMovedFromFilePath = FileUtil.GetUniqueTempPathInProject();
            try
            {
                PrepareFileWithTypesWithMovedFromAttribute(typesWithMovedFromFilePath);

                var programPath = NonObsoleteApiUpdaterDetectorPath();
                var arguments = $"{Quote(Path.GetFullPath(candidateErrorFilePath))} {Quote(Path.GetFullPath(typesWithMovedFromFilePath))} {Quote(Path.GetFullPath("Logs/ApiUpdaterCheck.txt"))} {DateTime.Now.Ticks}";

                Program program = new ManagedProgram(MonoInstallationPath, null, programPath, arguments, false, null);
                try
                {
                    program.Start();
                    program.WaitForExit();

                    if (program.ExitCode != 0)
                    {
                        var separator = $"{Environment.NewLine}-----------------------{Environment.NewLine}";
                        Debug.Log($"[Script Updater] Unable to check errors listed in `{candidateErrorFilePath}`. File contents:{Environment.NewLine}{File.ReadAllText(candidateErrorFilePath)}{Environment.NewLine}{separator}{Environment.NewLine}{typesWithMovedFromFilePath} (types):{Environment.NewLine}{File.ReadAllText(typesWithMovedFromFilePath)}{separator}{Environment.NewLine}Program exited with code = {program.ExitCode}{Environment.NewLine}Program Output:{Environment.NewLine}{string.Join("\n", program.GetErrorOutput())}");

                        return new string[0];
                    }

                    return program.GetStandardOutput();
                }
                catch (Exception)
                {
                    program.LogProcessStartInfo();
                    throw;
                }
                finally
                {
                    program.Dispose();
                }
            }
            catch (Exception)
            {
                var errorMsg = new StringBuilder($"Unable to check whether compiler errors are due types moved do different namespaces.{NewLine}");
                if (File.Exists(candidateErrorFilePath))
                {
                    errorMsg.AppendLine($"Errors to be checked ('{candidateErrorFilePath}'):{NewLine}{File.ReadAllText(candidateErrorFilePath)}");
                }
                else
                {
                    errorMsg.AppendLine($"Could not open file with list errors to be checked: '{candidateErrorFilePath}'");
                }

                if (File.Exists(typesWithMovedFromFilePath))
                {
                    errorMsg.AppendLine($"List of types with MovedFromAttribute ('{typesWithMovedFromFilePath}'):{NewLine}{File.ReadAllText(typesWithMovedFromFilePath)}");
                }
                else
                {
                    errorMsg.AppendLine($"Could not open file with list types with MovedFromAttribute: '{typesWithMovedFromFilePath}'");
                }

                Debug.Log(errorMsg.ToString());
                throw;
            }
        }

        public static bool IsReferenceToMissingObsoleteMember(string namespaceName, string className)
        {
            try
            {
                var found = FindTypeInLoadedAssemblies(t => t.Name == className && t.Namespace == namespaceName && IsUpdateable(t));
                return found != null;
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new Exception(ex.Message + ex.LoaderExceptions.Aggregate("", (acc, curr) => acc + "\r\n\t" + curr.Message));
            }
        }

        public static void UpdateScripts(string responseFile, string sourceExtension, string[] sourceFiles)
        {
            bool anyFileInAssetsFolder = false;
            var pathMappingsFilePath = Path.GetTempFileName();
            var filePathMappings = new List<string>(sourceFiles.Length);
            foreach (var source in sourceFiles)
            {
                anyFileInAssetsFolder |= (source.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase) != -1);

                var f = CommandLineFormatter.PrepareFileName(source);
                if (f != source) // assume path represents a virtual path and needs to be mapped.
                {
                    f = Paths.UnifyDirectorySeparator(f);
                    filePathMappings.Add(f + " => " + source);
                }
            }

            // Only try to connect to VCS if there are files under VCS that need to be updated
            if (anyFileInAssetsFolder && !APIUpdaterManager.WaitForVCSServerConnection(true))
            {
                return;
            }

            File.WriteAllLines(pathMappingsFilePath, filePathMappings.ToArray());

            var tempOutputPath = "Library/Temp/ScriptUpdater/" + new System.Random().Next() + "/";
            try
            {
                var arguments = ArgumentsForScriptUpdater(
                    sourceExtension,
                    tempOutputPath,
                    pathMappingsFilePath,
                    responseFile);

                RunUpdatingProgram("ScriptUpdater.exe", arguments, tempOutputPath, anyFileInAssetsFolder);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            catch (Exception ex) when (!(ex is StackOverflowException) && !(ex is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Debug.LogError(L10n.Tr("[API Updater] ScriptUpdater threw an exception. Check the following message in the log."));
                Debug.LogException(ex);

                APIUpdaterManager.ReportExpectedUpdateFailure();
            }
        }

        public static string ArgumentsForScriptUpdater(string sourceExtension, string tempOutputPath, string pathMappingsFilePath, string responseFile)
        {
            return sourceExtension
                + " "
                + CommandLineFormatter.PrepareFileName(MonoFrameworkPath)
                + " "
                + CommandLineFormatter.PrepareFileName(tempOutputPath)
                + " \"" + APIUpdaterManager.ConfigurationSourcesFilter + "\" " // Quote the filter (regex) to avoid issues when passing through command line arg.)
                + CommandLineFormatter.PrepareFileName(pathMappingsFilePath)
                + " "
                + responseFile;  // Response file is always relative and without spaces, no need to quote.
        }

        static void RunUpdatingProgram(string executable, string arguments, string tempOutputPath, bool anyFileInAssetsFolder)
        {
            var scriptUpdaterPath = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable; // ManagedProgram will quote this path for us.
            using (var program = new ManagedProgram(MonoInstallationPath, null, scriptUpdaterPath, arguments, false, null))
            {
                program.LogProcessStartInfo();
                program.Start();
                program.WaitForExit();

                Console.WriteLine(string.Join(Environment.NewLine, program.GetStandardOutput()));

                HandleUpdaterReturnValue(program, tempOutputPath, anyFileInAssetsFolder);
            }
        }

        static void HandleUpdaterReturnValue(ManagedProgram program, string tempOutputPath, bool anyFileInAssetsFolder)
        {
            if (program.ExitCode == 0)
            {
                Console.WriteLine(string.Join(Environment.NewLine, program.GetErrorOutput()));
                CopyUpdatedFiles(tempOutputPath, anyFileInAssetsFolder);
                return;
            }

            APIUpdaterManager.ReportExpectedUpdateFailure();
            if (program.ExitCode > 0)
                ReportAPIUpdaterFailure(program.GetErrorOutput());
            else
                ReportAPIUpdaterCrash(program.GetErrorOutput());
        }

        static void ReportAPIUpdaterCrash(IEnumerable<string> errorOutput)
        {
            Debug.LogErrorFormat(L10n.Tr("Failed to run script updater.{0}Please, report a bug to Unity with these details{0}{1}"), Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
        }

        static void ReportAPIUpdaterFailure(IEnumerable<string> errorOutput)
        {
            var msg = string.Format(L10n.Tr("APIUpdater encountered some issues and was not able to finish.{0}{1}"), Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
            APIUpdaterManager.ReportGroupedAPIUpdaterFailure(msg);
        }

        static void CopyUpdatedFiles(string tempOutputPath, bool anyFileInAssetsFolder)
        {
            if (!Directory.Exists(tempOutputPath))
                return;

            var files = Directory.GetFiles(tempOutputPath, "*.*", SearchOption.AllDirectories);

            var pathsRelativeToTempOutputPath = files.Select(path => path.Replace(tempOutputPath, ""));
            if (anyFileInAssetsFolder && Provider.enabled && !CheckoutAndValidateVCSFiles(pathsRelativeToTempOutputPath))
                return;

            var destRelativeFilePaths = files.Select(sourceFileName => sourceFileName.Substring(tempOutputPath.Length)).ToArray();

            HandleFilesInPackagesVirtualFolder(destRelativeFilePaths);

            if (!CheckReadOnlyFiles(destRelativeFilePaths))
                return;

            foreach (var sourceFileName in files)
            {
                var relativeDestFilePath = sourceFileName.Substring(tempOutputPath.Length);

                // Packman team is considering using hardlinks to implement the private cache (as of today PM simply copies the content of the package into
                // Library/PackageCache folder for each project)
                //
                // If this ever changes we'll need to change our implementation (and remove the link instead of simply updating in place) otherwise updating a package
                // in one project would result in that package being updated in all projects in the local computer.
                File.Copy(sourceFileName, relativeDestFilePath, true);
            }

            if (destRelativeFilePaths.Length > 0)
            {
                Console.WriteLine("[API Updater] Updated Files:");
                foreach (var path in destRelativeFilePaths)
                    Console.WriteLine(path);

                Console.WriteLine();
            }
            APIUpdaterManager.ReportUpdatedFiles(destRelativeFilePaths);

            FileUtil.DeleteFileOrDirectory(tempOutputPath);
        }

        internal static void HandleFilesInPackagesVirtualFolder(string[] destRelativeFilePaths)
        {
            var filesFromReadOnlyPackages = new List<string>();
            foreach (var path in destRelativeFilePaths.Select(path => path.Replace("\\", "/"))) // package manager paths are always separated by /
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo == null)
                {
                    if (filesFromReadOnlyPackages.Count > 0)
                    {
                        Console.WriteLine(
                            L10n.Tr("[API Updater] At least one file from a readonly package and one file from other location have been updated (that is not expected).{0}File from other location: {0}\t{1}{0}Files from packages already processed: {0}{2}"),
                            Environment.NewLine,
                            path,
                            string.Join($"{Environment.NewLine}\t", filesFromReadOnlyPackages.ToArray()));
                    }

                    continue;
                }

                if (packageInfo.source == PackageSource.BuiltIn)
                {
                    Debug.LogError($"[API Updater] Builtin package '{packageInfo.displayName}' ({packageInfo.version}) files requires updating (Unity version {Application.unityVersion}). This should not happen. Please, report to Unity");
                    return;
                }

                if (packageInfo.source != PackageSource.Local && packageInfo.source != PackageSource.Embedded)
                {
                    // Packman creates a (readonly) cache under Library/PackageCache in a way that even if multiple projects uses the same package each one should have its own
                    // private cache so it is safe for the updater to simply remove the readonly attribute and update the file.
                    filesFromReadOnlyPackages.Add(path);
                }

                // PackageSource.Embedded / PackageSource.Local are considered writtable, so nothing to do, i.e, we can simply overwrite the file contents.
            }

            foreach (var relativeFilePath in filesFromReadOnlyPackages)
            {
                var fileAttributes = File.GetAttributes(relativeFilePath);
                if ((fileAttributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                    continue;

                File.SetAttributes(relativeFilePath, fileAttributes & ~FileAttributes.ReadOnly);
            }

            PackageManager.ImmutableAssets.SetAssetsAllowedToBeModified(filesFromReadOnlyPackages.ToArray());
        }

        internal static bool CheckReadOnlyFiles(string[] destRelativeFilePaths)
        {
            // Verify that all the files we need to copy are now writable
            // Problems after API updating during ScriptCompilation if the files are not-writable
            var readOnlyFiles = destRelativeFilePaths.Where(path => (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
            if (readOnlyFiles.Any())
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (files not writable): {0}"), readOnlyFiles.Select(path => path).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            return true;
        }

        internal static bool CheckoutAndValidateVCSFiles(IEnumerable<string> files)
        {
            // We're only interested in files that would be under VCS, i.e. project
            // assets or local packages. Incoming paths might use backward slashes; replace with
            // forward ones as that's what Unity/VCS functions operate on.
            var versionedFiles = files.Select(f => f.Replace('\\', '/')).Where(Provider.PathIsVersioned).ToArray();

            // Fail if the asset database GUID can not be found for the input asset path.
            var assetPath = versionedFiles.FirstOrDefault(f => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(f)));
            if (assetPath != null)
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to add file to list): {0}"), assetPath);
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            var notEditableFiles = new List<string>();
            if (!AssetDatabase.MakeEditable(versionedFiles, null, notEditableFiles))
            {
                var notEditableList = notEditableFiles.Aggregate(string.Empty, (text, file) => text + $"\n\t{file}");
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to check out): {0}"), notEditableList);
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            return true;
        }

        static string GetValueFromNormalizedMessage(IEnumerable<string> lines, string marker)
        {
            string value = null;
            var foundLine = lines.FirstOrDefault(l => l.StartsWith(marker));
            if (foundLine != null)
            {
                value = foundLine.Substring(marker.Length).Trim();
            }
            return value;
        }

        static bool IsUpdateable(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (attrs.Length != 1)
                return false;

            var oa = (ObsoleteAttribute)attrs[0];
            return oa.Message.Contains("UnityUpgradable");
        }

        static Type FindTypeInLoadedAssemblies(Func<Type, bool> predicate)
        {
            var found = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !IsIgnoredAssembly(assembly.GetName()))
                .SelectMany(GetValidTypesIn)
                .FirstOrDefault(predicate);

            return found;
        }

        static IEnumerable<Type> GetValidTypesIn(Assembly a)
        {
            Type[] types;
            try
            {
                types = a.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            return types.Where(t => t != null);
        }

        static bool IsIgnoredAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return _ignoredAssemblies.Any(candidate => Regex.IsMatch(name, candidate));
        }

        static string[] _ignoredAssemblies = { "^UnityScript$", "^System\\..*", "^mscorlib$" };

        static string MonoInstallationPath = MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge");
        static string MonoFrameworkPath = MonoInstallationFinder.GetFrameWorksFolder();
    }
}
