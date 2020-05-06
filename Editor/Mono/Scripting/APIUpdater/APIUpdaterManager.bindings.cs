// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.APIUpdater;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEditorInternal.APIUpdaterExtensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Mono.Cecil;
using System.Text;

namespace UnityEditorInternal.APIUpdating
{
    //TODO: ADRIANO:
    //
    // 1. If an error happen (in updater) during package adding/updating we may not update assemblies. We need to report to user.
    //    Ex: PA (1.0) -> PB (1.0); Add PB, PA; Update PB (to 1.1) and introduce update config; if an error happen when processing PB (1.1)
    //    we may not apply updates to PA.

    // Keep in sync with APIUpdaterManager.h
    internal enum APIUpdaterStatus
    {
        None,
        Offered,
        Accepted
    }

    //*undocumented*
    [NativeHeader("Editor/Src/Scripting/APIUpdater/APIUpdaterManager.h")]
    [StaticAccessor("APIUpdaterManager::GetInstance()", StaticAccessorType.Dot)]
    internal static class APIUpdaterManager
    {
        private const string k_AssemblyDependencyGraphFilePath = "Library/APIUpdater/project-dependencies.graph";

        private static HashSet<AssemblyUpdateCandidate> s_AssembliesToUpdate;

        public static extern bool WaitForVCSServerConnection(bool reportTimeout);
        public static extern void ReportExpectedUpdateFailure();
        public static extern void ReportGroupedAPIUpdaterFailure(string msg);
        public static extern int numberOfTimesAsked
        {
            [NativeName("NumberOfTimesAsked")] get;
        }

        public static extern void Reset();
        public static extern void ResetNumberOfTimesAsked();
        public static extern void ResetConsentStatus();
        public static extern void ReportUpdatedFiles(string[] filePaths);

        public static extern void SetAnswerToUpdateOffer(APIUpdaterStatus status);

        // Sets/gets a regular expression used to filter configuration sources assemblies
        // by name.
        public static extern string ConfigurationSourcesFilter { get; set; }

        [RequiredByNativeCode]
        internal static extern APIUpdaterStatus AskForConsent(bool askAgain);

        // These methods are used to persist the list of assemblies to be updated in the native side in order to preserve this list across domain reloads.
        static extern void ResetListOfAssembliesToBeUpdateInNativeSide();
        static extern void AddAssemblyToBeUpdatedInNativeSide(string assemblyName, string assemblyPath, string[] assemblyUpdateConfigSources);
        static extern bool GetAndRemoveAssemblyToBeUpdatedFromNative(out string outAssemblyName, out string outAssemblyPath, List<string> outUpdateConfigSources);

        [RequiredByNativeCode]
        internal static void UpdateAssemblies()
        {
            var assembliesToUpdate = GetAssembliesToBeUpdated();
            if (assembliesToUpdate.Count == 0)
                return;

            var assemblyPaths = assembliesToUpdate.Select(c => c.Path);
            var anyAssemblyInAssetsFolder = assemblyPaths.Any(path => path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase) != -1);

            // Only try to connect to VCS if there are files under VCS that need to be updated
            if (anyAssemblyInAssetsFolder)
            {
                var failedToConnectToVcs = false;
                if (WaitForVCSServerConnection(true))
                {
                    failedToConnectToVcs = Provider.enabled && !APIUpdaterHelper.CheckoutAndValidateVCSFiles(assemblyPaths);
                }

                if (failedToConnectToVcs)
                {
                    assembliesToUpdate.Clear();
                    return;
                }
            }

            var sw = Stopwatch.StartNew();
            var updatedCount = 0;

            var assembliesToCheckCount = assembliesToUpdate.Count;
            var tasks = assembliesToUpdate.Select(a => new AssemblyUpdaterUpdateTask(a)).ToArray();
            foreach (var task in tasks)
            {
                CollectAssemblyObsoleteAPIUsage(task.Candidate.Path);
                ThreadPool.QueueUserWorkItem(RunAssemblyUpdaterTask, task);
            }

            var finishOk = false;
            var waitEvents = tasks.Select(t => t.Event).ToArray();
            var timeout = TimeSpan.FromSeconds(30);
            if (WaitHandle.WaitAll(waitEvents, timeout))
            {
                if (!HandleAssemblyUpdaterErrors(tasks))
                {
                    updatedCount = ProcessSuccessfulUpdates(tasks);
                    finishOk = true;
                }
            }
            else
            {
                LogTimeoutError(tasks);
            }

            sw.Stop();
            APIUpdaterLogger.WriteToFile(L10n.Tr("Update finished with {0} in {1} ms ({2}/{3} assembly(ies) updated)."), finishOk ? L10n.Tr("success") : L10n.Tr("error"), sw.ElapsedMilliseconds, updatedCount, assembliesToCheckCount);

            if (updatedCount > 0 && !EditorCompilationInterface.Instance.DoesProjectFolderHaveAnyScripts())
            {
                ReportPossibleUpdateFinished(false);
            }

            PersistListOfAssembliesToUpdate();
        }

        /*
         * We store this list at native side so it don't get lost at domain reloads
         * Code should call `GetAssembliesToBeUpdated()` instead of using `assembliesToUpdate` direclty.
         */
        private static void PersistListOfAssembliesToUpdate()
        {
            ResetListOfAssembliesToBeUpdateInNativeSide();
            foreach (var assembly in GetAssembliesToBeUpdated())
            {
                // no need to persist the dependency graph. See comment in RestoreFromNative() method.
                AddAssemblyToBeUpdatedInNativeSide(assembly.Name, assembly.Path, assembly.UpdateConfigSources.ToArray());
            }
        }

        private static HashSet<AssemblyUpdateCandidate> GetAssembliesToBeUpdated()
        {
            if (s_AssembliesToUpdate == null)
            {
                s_AssembliesToUpdate = new HashSet<AssemblyUpdateCandidate>();
                RestoreFromNative();
            }

            return s_AssembliesToUpdate;
        }

        private static void RestoreFromNative()
        {
            string assemblyName;
            string assemblyPath;
            List<string> updateConfigSources = new List<string>();

            while (GetAndRemoveAssemblyToBeUpdatedFromNative(out assemblyName, out assemblyPath, updateConfigSources))
            {
                s_AssembliesToUpdate.Add(new AssemblyUpdateCandidate
                {
                    Name = assemblyName,
                    Path = assemblyPath,
                    DependencyGraph = null, // No need to restore the dependency graph. It is only used during the collection phase (i.e, to figure out the list of
                                            // potential candidates for updating which happens before this step.
                    UpdateConfigSources = updateConfigSources
                });

                updateConfigSources = new List<string>();
            }
        }

        private static void LogTimeoutError(AssemblyUpdaterCheckAssemblyPublishConfigsTask[] tasks, TimeSpan waitedTime)
        {
            var timedOut = tasks.Where(t => !t.Event.WaitOne(0));

            var sb = new StringBuilder(L10n.Tr("Timeout while checking assemblies:"));
            foreach (var task in timedOut)
            {
                sb.AppendFormat("{1}{0}", Environment.NewLine, task.Candidate.Path);
            }

            sb.AppendFormat(L10n.Tr("{0}Timeout: {1} ms"), Environment.NewLine, waitedTime.Milliseconds);
            sb.AppendFormat(L10n.Tr("{0}Update configurations from those assemblies may have not been applied."), Environment.NewLine);
            APIUpdaterLogger.WriteErrorToConsole(sb.ToString());
        }

        private static void LogTimeoutError(AssemblyUpdaterUpdateTask[] tasks)
        {
            var completedSuccessfully = tasks.Where(t => t.Event.WaitOne(0)).ToArray();
            var timedOutTasks = tasks.Where(t => !t.Event.WaitOne(0));

            var sb = new StringBuilder(L10n.Tr("Timeout while updating assemblies:"));
            foreach (var updaterTask in timedOutTasks)
            {
                sb.AppendFormat("{1} (Output: {2}){0}", Environment.NewLine, updaterTask.Candidate.Path, updaterTask.OutputPath);
            }

            ReportIgnoredAssembliesDueToPreviousErrors(sb, completedSuccessfully);

            APIUpdaterLogger.WriteErrorToConsole(sb.ToString());
        }

        private static bool HandleAssemblyUpdaterErrors(IList<AssemblyUpdaterUpdateTask> allTasks)
        {
            var tasksWithErrors = allTasks.Where(t => APIUpdaterAssemblyHelper.IsError(t.Result) || APIUpdaterAssemblyHelper.IsUnknown(t.Result) || t.Exception != null).ToArray();
            if (tasksWithErrors.Length == 0)
                return false;

            var sb = new StringBuilder(L10n.Tr("Unable to update following assemblies:"));
            foreach (var updaterTask in tasksWithErrors)
            {
                sb.Append(FormatErrorFromTask(updaterTask));
            }

            ReportIgnoredAssembliesDueToPreviousErrors(sb, allTasks.Except(tasksWithErrors).ToArray());

            APIUpdaterLogger.WriteErrorToConsole(sb.ToString());
            return true;
        }

        static string FormatErrorFromTask(AssemblyUpdaterUpdateTask updaterTask)
        {
            // this may happen if mono.exe (which we use to run AssemblyUpdater.exe) cannot run the executable
            // and reports an error (for example, *file not found*)
            var unknownStatusMessage = APIUpdaterAssemblyHelper.IsUnknown(updaterTask.Result)
                ? L10n.Tr(" does not match any return code from AssemblyUpdater.exe")
                : string.Empty;

            var exceptionMessage = updaterTask.Exception != null
                ? $"{Environment.NewLine}\tException: {updaterTask.Exception}"
                : string.Empty;

            return string.Format("{1} (Name = {2}, Error = {3}{7}) (Output: {4}){0}{5}{0}{6}{0}{8}",
                Environment.NewLine,
                updaterTask.Candidate.Path,
                updaterTask.Candidate.Name,
                updaterTask.Result,
                updaterTask.OutputPath,
                updaterTask.StdOut,
                updaterTask.StdErr,
                unknownStatusMessage,
                exceptionMessage);
        }

        private static void ReportIgnoredAssembliesDueToPreviousErrors(StringBuilder sb, IList<AssemblyUpdaterUpdateTask> completedSuccessfully)
        {
            if (completedSuccessfully.Count == 0)
                return;

            sb.AppendFormat(L10n.Tr("Following assemblies were successfully updated but due to the failed ones above they were ignored (not copied to the destination folder):"));
            foreach (var updaterTask in completedSuccessfully)
            {
                sb.AppendFormat("{1}\t(Result = {2}) (Output: {3}){0}{4}{0}", Environment.NewLine, updaterTask.Candidate.Path, updaterTask.Result, updaterTask.OutputPath, updaterTask.StdOut);
            }
        }

        private static int ProcessSuccessfulUpdates(AssemblyUpdaterUpdateTask[] tasks)
        {
            var assembliesToUpdate  = GetAssembliesToBeUpdated();
            var succeededUpdates = tasks.Where(t => t.Result == APIUpdaterAssemblyHelper.UpdatesApplied);
            if (!succeededUpdates.Any())
            {
                assembliesToUpdate.Clear();
                return 0;
            }

            if (AskForConsent(false) != APIUpdaterStatus.Accepted)
            {
                APIUpdaterLogger.WriteToFile(L10n.Tr("User declined to run APIUpdater"));
                return 0;
            }

            var assemblyPaths2 = succeededUpdates.Select(u => u.Candidate.Path).ToArray();
            APIUpdaterHelper.HandleFilesInPackagesVirtualFolder(assemblyPaths2);
            if (!APIUpdaterHelper.CheckReadOnlyFiles(assemblyPaths2))
                return 0;

            foreach (var succeed in succeededUpdates)
            {
                APIUpdaterLogger.WriteToFile("{0}{1}", Environment.NewLine, succeed.StdOut);
                FileUtil.MoveFileIfExists(succeed.OutputPath, succeed.Candidate.Path);
            }

            assembliesToUpdate.Clear();
            return succeededUpdates.Count();
        }

        private static void RunAssemblyUpdaterTask(object o)
        {
            var task = (AssemblyUpdaterTask)o;

            string stdOut = string.Empty;
            string stdErr = string.Empty;

            try
            {
                task.Result = APIUpdaterAssemblyHelper.Run(task.Arguments, AssemblyUpdaterTask.WorkingDirectory, out stdOut, out stdErr);
            }
            catch (Exception ex)
            {
                task.Exception = ex;
            }
            finally
            {
                task.StdErr = stdErr;
                task.StdOut = stdOut;

                task.Event.Set();
            }
        }

        [RequiredByNativeCode]
        internal static bool HasPrecompiledAssembliesToUpdate()
        {
            return GetAssembliesToBeUpdated().Count > 0;
        }

        private static T Profile<T>(bool enable, string msg, Func<T> f)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return f();
            }
            finally
            {
                sw.Stop();
                if (enable)
                    UnityEngine.Debug.LogFormat("{0} took {1} ms", msg, sw.ElapsedMilliseconds);
            }
        }

        private static void Profile(string msg, Action f)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                f();
            }
            finally
            {
                sw.Stop();
                UnityEngine.Debug.LogFormat("{0} took {1} ms", msg, sw.ElapsedMilliseconds);
            }
        }

        [RequiredByNativeCode]
        internal static void ProcessImportedAssemblies(string[] assemblies)
        {
            var sw = Stopwatch.StartNew();

            var depGraph = UpdateDependencyGraph(assemblies);
            var sortedCandidatesForUpdating = FindCandidatesForUpdatingSortedByDependency(assemblies, depGraph);

            var assembliesToUpdate = GetAssembliesToBeUpdated();

            CollectImportedAssembliesToBeUpdated(assembliesToUpdate, sortedCandidatesForUpdating);
            UpdatePublishUpdaterConfigStatusAndAddDependents(assembliesToUpdate, sortedCandidatesForUpdating, depGraph);

            SaveDependencyGraph(depGraph, k_AssemblyDependencyGraphFilePath);

            sw.Stop();
            APIUpdaterLogger.WriteToFile(L10n.Tr("Processing imported assemblies took {0} ms ({1}/{2} assembly(ies))."), sw.ElapsedMilliseconds, assembliesToUpdate.Count, sortedCandidatesForUpdating.Count());

            UpdateAssemblies();
        }

        private static void CollectImportedAssembliesToBeUpdated(HashSet<AssemblyUpdateCandidate> assembliesToUpdate, IEnumerable<AssemblyUpdateCandidate> candidatesForUpdating)
        {
            foreach (var importedCandidate in candidatesForUpdating)
            {
                if (importedCandidate.MayRequireUpdating)
                    assembliesToUpdate.Add(importedCandidate);
            }
        }

        private static void UpdatePublishUpdaterConfigStatusAndAddDependents(HashSet<AssemblyUpdateCandidate> assembliesToUpdate, IEnumerable<AssemblyUpdateCandidate> candidatesForUpdating, AssemblyDependencyGraph depGraph)
        {
            var tasks = candidatesForUpdating
                .Where(a => AssemblyHelper.IsManagedAssembly(a.Path) && IsAssemblyInPackageFolder(a))
                .Select(a => new AssemblyUpdaterCheckAssemblyPublishConfigsTask(a)).ToArray();

            if (tasks.Length == 0)
                return;

            foreach (var task in tasks)
            {
                ThreadPool.QueueUserWorkItem(RunAssemblyUpdaterTask, task);
            }

            var waitEvents = tasks.Select(t => t.Event).ToArray();
            var timeout = TimeSpan.FromSeconds(30);
            if (!WaitHandle.WaitAll(waitEvents, timeout))
            {
                LogTimeoutError(tasks, timeout);
            }

            var nonTimedOutTasks = tasks.Where(t => t.Event.WaitOne(0)).ToArray();
            if (HandleCheckAssemblyPublishUpdaterConfigErrors(nonTimedOutTasks))
                return;

            foreach (var task in nonTimedOutTasks)
            {
                if ((task.Result & APIUpdaterAssemblyHelper.ContainsUpdaterConfigurations) == APIUpdaterAssemblyHelper.ContainsUpdaterConfigurations)
                {
                    var importedCandidate = task.Candidate;

                    importedCandidate.DependencyGraph.Status |= AssemblyStatus.PublishesUpdaterConfigurations;
                    AddDependentAssembliesToUpdateList(assembliesToUpdate, depGraph, importedCandidate);
                }
            }
        }

        private static bool HandleCheckAssemblyPublishUpdaterConfigErrors(AssemblyUpdaterCheckAssemblyPublishConfigsTask[] nonTimedOutTasks)
        {
            var withErrors = nonTimedOutTasks.Where(t => APIUpdaterAssemblyHelper.IsError(t.Result) || t.Exception != null).ToArray();
            if (withErrors.Length == 0)
                return false;

            var sb = new StringBuilder(L10n.Tr("Failed to check following assemblies for updater configurations:\r\n"));
            foreach (var failedAssemblyInfo in withErrors)
            {
                sb.AppendFormat(L10n.Tr("{0} (ret = {1}):\r\n{2}\r\n{3}\r\n"), failedAssemblyInfo.Candidate.Path, failedAssemblyInfo.Result, failedAssemblyInfo.StdOut, failedAssemblyInfo.StdErr);
            }
            sb.Append("\r\n--------------");

            APIUpdaterLogger.WriteErrorToConsole(sb.ToString());

            return true;
        }

        private static void AddDependentAssembliesToUpdateList(HashSet<AssemblyUpdateCandidate> assembliesToUpdate, AssemblyDependencyGraph depGraph, AssemblyUpdateCandidate imported)
        {
            var dependents = depGraph.GetDependentsOf(imported.Name);
            var candidatesToUpdate = dependents.Select(assemblyName => CandidateForUpdatingFrom(assemblyName, depGraph));

            foreach (var candidate in candidatesToUpdate.Where(c => c != null))
                assembliesToUpdate.Add(candidate);
        }

        private extern static void CollectAssemblyObsoleteAPIUsage(string assemblyPath);
        private extern static void ReportPossibleUpdateFinished(bool hasCompilerErrors);


        private static bool IsAssemblyInPackageFolder(AssemblyUpdateCandidate candidate)
        {
            return candidate.Path.IsInPackage();
        }

        /*
         * Given a list of assemblies, returns those that references assemblies contributing updater configurations
         * sorted by dependency (i.e, given assemblies, A, B & C such A -> B -> C, should return in C, B, A order)
         */
        private static IEnumerable<AssemblyUpdateCandidate> FindCandidatesForUpdatingSortedByDependency(IEnumerable<string> assemblyPaths, AssemblyDependencyGraph depGraph)
        {
            var candidates = new HashSet<AssemblyUpdateCandidate>();
            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                var depInfo = depGraph.FindAssembly(assemblyName);
                Debug.Assert(depInfo != null);

                // Any referenced assemblies contains updater configs?
                var referencedAssembliesWithUpdaterConfigs = depInfo.Dependencies.Where(a => (depGraph.FindAssembly(a.Name)?.Status & AssemblyStatus.PublishesUpdaterConfigurations) == AssemblyStatus.PublishesUpdaterConfigurations);
                if (referencedAssembliesWithUpdaterConfigs.Any())
                {
                    IEnumerable<string> updateConfigSources = ResolvePathOfAssembliesWithUpdaterConfigurations(referencedAssembliesWithUpdaterConfigs);
                    candidates.Add(new AssemblyUpdateCandidate
                    {
                        Name = assemblyName,
                        Path = assemblyPath,
                        DependencyGraph = depInfo,
                        UpdateConfigSources = updateConfigSources
                    });
                }
            }

            // add the candidates sorted based on the dependency graph...
            var result = new List<AssemblyUpdateCandidate>();
            foreach (var assemblyName in depGraph.SortedDependents())
            {
                // We may have assemblies with the same name in different folders
                // (for example GUISystem/Standalone/UnityEngine.UI.dll & GUISystem/UnityEngine.UI.dll)
                var filteredCandidates = candidates.Where(c => CompareIgnoreCase(c.Name, assemblyName));
                result.AddRange(filteredCandidates);
            }

            return result;
        }

        // We only resolve Unity assemblies and assemblies coming from packages.
        // We may want to change this to support *any assembly* to contribute with updater configurations
        private static IEnumerable<string> ResolvePathOfAssembliesWithUpdaterConfigurations(IEnumerable<AssemblyDependencyGraph.DependencyEntry> assemblies)
        {
            foreach (var assemblyName in assemblies)
            {
                var resolved = ResolveAssemblyPath(assemblyName.Name);
                if (resolved != null)
                    yield return Path.GetFullPath(resolved);
            }
        }

        private static string ResolveAssemblyPath(string assemblyName)
        {
            //find the assembly in Data/Managed or Data/Managed/UnityEngine
            var assemblyFileName = assemblyName + ".dll";
            var managedPath = GetUnityEditorManagedPath();
            var pathInManagedFolder = Path.Combine(managedPath, assemblyFileName);
            if (File.Exists(pathInManagedFolder))
                return pathInManagedFolder;

            var pathInUnityEngineFolder = Path.Combine(Path.Combine(managedPath, "UnityEngine"), assemblyFileName);
            if (File.Exists(pathInUnityEngineFolder))
                return pathInUnityEngineFolder;

            var assetsAssemblies = new HashSet<string>(AssetDatabase.GetAllAssetPaths().Where(assetPath => Path.GetExtension(assetPath) == ".dll").ToArray());

            // If the same assembly exist in multiple folders, choose the shortest path one.
            var resolvedList = assetsAssemblies.Where(a => CompareIgnoreCase(AssemblyNameFromPath(a), assemblyName)).ToArray();
            var assemblyPathInAssetsFolder = resolvedList.OrderBy(path => path.Length).FirstOrDefault();
            if (resolvedList.Length > 1)
            {
                APIUpdaterLogger.WriteToFile(L10n.Tr("Warning : Multiple matches found for assembly name '{0}'. Shortest path one ({1}) chosen as the source of updates. Full list: {2}"), assemblyName, assemblyPathInAssetsFolder, string.Join(Environment.NewLine, resolvedList));
            }

            if (assemblyPathInAssetsFolder != null && (assemblyPathInAssetsFolder.IsInPackage() || assemblyPathInAssetsFolder.IsInAssetsFolder()))
            {
                return assemblyPathInAssetsFolder;
            }

            //TODO: In order to support *pre-built* assemblies referencing assemblies (PA) built out of
            //      packages deployed as source code we need to find PA which is located in Library/ScriptAssemblies folder
            //      In this case, we need to look at the mono islands (editorComp.GetAllMonoIslands(EditorScriptCompilationOptions.BuildingForEditor))
            //      and consider the ones which have its source in packages:
            //      var isInPackage = i._files.Any(path => editorComp.IsPathInPackageDirectory(path));

            return null;
        }

        private static bool CompareIgnoreCase(string lhs, string rhs)
        {
            return string.Compare(lhs, rhs, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private static AssemblyUpdateCandidate CandidateForUpdatingFrom(string candidateAssemblyName, AssemblyDependencyGraph rootDepGraph)
        {
            string resolvedAssemblyPath = ResolveAssemblyPath(candidateAssemblyName);

            // this may happen if, for instance, after setting the dependency A -> B, *B* gets removed and *A* gets updated to a new version (we don't remove
            // the dependency from the graph.
            if (string.IsNullOrEmpty(resolvedAssemblyPath))
                return null;

            var depGraph = rootDepGraph.FindAssembly(candidateAssemblyName);
            var referencesAssemblyWithUpdaterConfigs = depGraph.Dependencies.Where(depAssembly => (rootDepGraph.FindAssembly(depAssembly.Name)?.Status & AssemblyStatus.PublishesUpdaterConfigurations) == AssemblyStatus.PublishesUpdaterConfigurations);
            var updaterConfigSources = ResolvePathOfAssembliesWithUpdaterConfigurations(referencesAssemblyWithUpdaterConfigs);

            return new AssemblyUpdateCandidate
            {
                Name = candidateAssemblyName,
                Path = resolvedAssemblyPath,
                DependencyGraph = depGraph,
                UpdateConfigSources = updaterConfigSources
            };
        }

        private static AssemblyDependencyGraph UpdateDependencyGraph(IEnumerable<string> addedAssemblyPaths)
        {
            var dependencyGraph = ReadOrCreateAssemblyDependencyGraph(k_AssemblyDependencyGraphFilePath);
            foreach (var addedAssemblyPath in addedAssemblyPaths)
            {
                var assemblyDependencies = AssemblyDependenciesFrom(addedAssemblyPath);
                dependencyGraph.SetDependencies(AssemblyNameFromPath(addedAssemblyPath), assemblyDependencies);

                FixUnityAssembliesStatusInDependencyGraph(dependencyGraph, assemblyDependencies);
            }

            SaveDependencyGraph(dependencyGraph, k_AssemblyDependencyGraphFilePath);

            return dependencyGraph;
        }

        private static void FixUnityAssembliesStatusInDependencyGraph(AssemblyDependencyGraph dependencyGraph, IEnumerable<string> assemblyNames)
        {
            var unityAssemblies = assemblyNames.Where(an => an.StartsWith("UnityEngine") || an.StartsWith("UnityEditor"));
            foreach (var assemblyName in unityAssemblies)
            {
                var dep = dependencyGraph.FindAssembly(assemblyName);
                dep.Status |= AssemblyStatus.PublishesUpdaterConfigurations; // we know that those assemblies contains update configs
            }
        }

        private static string[] AssemblyDependenciesFrom(string assemblyPath)
        {
            using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = false }))
            {
                return a.MainModule.AssemblyReferences.Select(assemblyReference => assemblyReference.Name).ToArray();
            }
        }

        private static void SaveDependencyGraph(AssemblyDependencyGraph dependencyGraph, string path)
        {
            try
            {
                var targetDir = Path.GetDirectoryName(path);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                using (var file = File.Open(path, System.IO.FileMode.Create))
                {
                    dependencyGraph.SaveTo(file);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                APIUpdaterLogger.WriteToFile(string.Format(L10n.Tr("Failed to save assembly dependency graph ({0}). Exception: {1}")), path, ex);
            }
            catch (IOException ex)
            {
                APIUpdaterLogger.WriteToFile(string.Format(L10n.Tr("Failed to save assembly dependency graph ({0}). Exception: {1}")), path, ex);
            }
        }

        private static AssemblyDependencyGraph ReadOrCreateAssemblyDependencyGraph(string assemblyDependencyGraphFilePath)
        {
            try
            {
                if (File.Exists(assemblyDependencyGraphFilePath))
                {
                    using (var stream = File.OpenRead(assemblyDependencyGraphFilePath))
                    {
                        return AssemblyDependencyGraph.LoadFrom(stream);
                    }
                }
            }
            catch (IOException e)
            {
                APIUpdaterLogger.WriteToFile(string.Format(L10n.Tr("Failed to read assembly dependency graph ({0}). Exception: {1}")), assemblyDependencyGraphFilePath, e);
            }

            return new AssemblyDependencyGraph();
        }

        private static string AssemblyNameFromPath(string assemblyPath)
        {
            return Path.GetFileNameWithoutExtension(assemblyPath);
        }

        private static string GetUnityEditorManagedPath()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed");
        }
    }

    internal class AssemblyUpdateCandidate : IEquatable<AssemblyUpdateCandidate>
    {
        public string Name;
        public string Path;
        public AssemblyDependencyGraph.DependencyEntry DependencyGraph;
        public IEnumerable<string> UpdateConfigSources;

        public bool MayRequireUpdating
        {
            get { return UpdateConfigSources.Any(); }
        }

        public static implicit operator bool(AssemblyUpdateCandidate a)
        {
            return a.Name != null;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode() * 37 + Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as AssemblyUpdateCandidate;
            if (other == null)
                return false;

            return Equals(other);
        }

        public bool Equals(AssemblyUpdateCandidate other)
        {
            return other.Name == Name && other.Path == Path;
        }
    }

    internal class AssemblyUpdaterTask
    {
        private AssemblyUpdateCandidate _candidate;

        public string StdOut { set; get; }
        public string StdErr { set; get; }

        static AssemblyUpdaterTask()
        {
            WorkingDirectory = UnityEngine.Application.dataPath + "/.."; // We can't access this property in a "non main thread" so we resolve and cache it in the main thread.
        }

        public AssemblyUpdaterTask(AssemblyUpdateCandidate a)
        {
            _candidate = a;

            Event = new ManualResetEvent(false);
        }

        public string Arguments { get; internal set; }

        public EventWaitHandle Event
        {
            get; internal set;
        }

        public int Result { get; internal set; }

        public AssemblyUpdateCandidate Candidate { get { return _candidate; } }
        public Exception Exception { get; internal set; }

        public static string WorkingDirectory { get; internal set; }
    }

    internal class AssemblyUpdaterUpdateTask : AssemblyUpdaterTask
    {
        public AssemblyUpdaterUpdateTask(AssemblyUpdateCandidate a) : base(a)
        {
            OutputPath = Path.GetTempFileName();
            Arguments = APIUpdaterAssemblyHelper.ArgumentsForUpdateAssembly(a.Path, OutputPath, a.UpdateConfigSources);
        }

        public string OutputPath { get; internal set; }
    }

    internal class AssemblyUpdaterCheckAssemblyPublishConfigsTask : AssemblyUpdaterTask
    {
        public AssemblyUpdaterCheckAssemblyPublishConfigsTask(AssemblyUpdateCandidate a) : base(a)
        {
            Arguments = APIUpdaterAssemblyHelper.ArgumentsForCheckingForUpdaterConfigsOn(a.Path);
        }
    }
}
