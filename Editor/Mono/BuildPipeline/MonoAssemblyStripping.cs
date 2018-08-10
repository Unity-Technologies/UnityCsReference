// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System;
using Mono.Cecil;
using Mono.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor.Utils;
using Debug = System.Diagnostics.Debug;
using System.Threading;

namespace UnityEditor
{
    internal class MonoProcessRunner
    {
        public StringBuilder Output  = new StringBuilder("");
        public StringBuilder Error   = new StringBuilder("");

        public bool Run(Process process)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;

            Thread outputReader = new Thread(ReadOutput);
            Thread errorReader  = new Thread(ReadErrors);

            process.Start();

            outputReader.Start(process);
            errorReader.Start(process);

            // We will only allow a single project to take up to 10 minutes to compile.
            // if it takes longer we assume something is wrong and kill it this may be
            // a bad assumption but it's what we currently do across the board.
            bool exitOK = process.WaitForExit(1000 * 60 * 10); // 10 minutes

            // We give each thread up to 5ms to finish capturing output from the process.
            // if they fail we assume that there is something wrong. This may not really
            // be the case but we simply assume it is as we don't want to wait around for
            // to long.
            DateTime start = DateTime.Now;
            while ((outputReader.IsAlive || errorReader.IsAlive) && (DateTime.Now - start).TotalMilliseconds < 5.0)
            {
                Thread.Sleep(0);
            }

            // If our threads are still running nuke the threads using the standard
            // exception mechanism, we forcefully do this because we can't wait around
            // all day to get output back from a bad compilation process.
            if (outputReader.IsAlive)
                outputReader.Abort();

            if (errorReader.IsAlive)
                errorReader.Abort();

            // Ensure things get cleaned up and threads are now gone.
            outputReader.Join();
            errorReader.Join();

            return exitOK;
        }

        void ReadOutput(object process)
        {
            Process p = process as Process;
            try
            {
                using (StreamReader outp = p.StandardOutput)
                {
                    Output.Append(outp.ReadToEnd());
                }
            }
            catch (ThreadAbortException) {}
        }

        void ReadErrors(object process)
        {
            Process p = process as Process;
            try
            {
                using (StreamReader err = p.StandardError)
                {
                    Error.Append(err.ReadToEnd());
                }
            }
            catch (ThreadAbortException) {}
        }
    }

    internal class MonoProcessUtility
    {
        public static string ProcessToString(Process process)
        {
            return process.StartInfo.FileName + " " +
                process.StartInfo.Arguments + " current dir : " +
                process.StartInfo.WorkingDirectory + "\n";
        }

        public static void RunMonoProcess(Process process, string name, string resultingFile)
        {
            MonoProcessRunner runner = new MonoProcessRunner();
            bool exitOK = runner.Run(process);
            if (process.ExitCode != 0 || !File.Exists(resultingFile))
            {
                string detailedMessage = "Failed " + name + ": " + ProcessToString(process) + " result file exists: " + File.Exists(resultingFile) + ". Timed out: " + !exitOK;
                detailedMessage += "\n\n";
                detailedMessage += "stdout:\n" + runner.Output + "\n";
                detailedMessage += "stderr:\n" + runner.Error + "\n";
                System.Console.WriteLine(detailedMessage);
                throw new UnityException(detailedMessage);
                /// ;; TODO add micromscorlib warning
            }
        }

        public static Process PrepareMonoProcess(string workDir)
        {
            var process = new Process();

            var executableName = Application.platform == RuntimePlatform.WindowsEditor ? "mono.exe" : "mono";
            process.StartInfo.FileName = Paths.Combine(MonoInstallationFinder.GetMonoInstallation(), "bin", executableName);

            // ;; TODO fix this hack for strange process handle duplication problem inside mono
            process.StartInfo.EnvironmentVariables["_WAPI_PROCESS_HANDLE_OFFSET"] = "5";

            // We run the linker on .NET 2.0 profile
            var monoProfile = BuildPipeline.CompatibilityProfileToClassLibFolder(ApiCompatibilityLevel.NET_2_0);
            process.StartInfo.EnvironmentVariables["MONO_PATH"] = MonoInstallationFinder.GetProfileDirectory(monoProfile);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.WorkingDirectory = workDir;

            return process;
        }

        public static Process PrepareMonoProcessBleedingEdge(string workDir)
        {
            var process = new Process();

            var executableName = Application.platform == RuntimePlatform.WindowsEditor ? "mono.exe" : "mono";
            process.StartInfo.FileName = Paths.Combine(MonoInstallationFinder.GetMonoBleedingEdgeInstallation(), "bin", executableName);

            // ;; TODO fix this hack for strange process handle duplication problem inside mono
            process.StartInfo.EnvironmentVariables["_WAPI_PROCESS_HANDLE_OFFSET"] = "5";

            // We run the stripper on .NET 4.6 profile
            var monoProfile = BuildPipeline.CompatibilityProfileToClassLibFolder(ApiCompatibilityLevel.NET_4_6);
            process.StartInfo.EnvironmentVariables["MONO_PATH"] = MonoInstallationFinder.GetProfileDirectory(monoProfile);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.WorkingDirectory = workDir;

            return process;
        }
    }

    internal class MonoAssemblyStripping
    {
        static void ReplaceFile(string src, string dst)
        {
            if (File.Exists(dst))
                FileUtil.DeleteFileOrDirectory(dst);

            FileUtil.CopyFileOrDirectory(src, dst);
        }

        static public void MonoCilStrip(BuildTarget buildTarget, string managedLibrariesDirectory, string[] fileNames)
        {
            string basePath = MonoInstallationFinder.GetProfileDirectory(BuildPipeline.CompatibilityProfileToClassLibFolder(ApiCompatibilityLevel.NET_4_6), MonoInstallationFinder.MonoBleedingEdgeInstallation);
            string cilStripper = Path.Combine(basePath, "mono-cil-strip.exe");

            foreach (string fileName in fileNames)
            {
                Process process = MonoProcessUtility.PrepareMonoProcessBleedingEdge(managedLibrariesDirectory);
                string outFile = fileName + ".out";

                process.StartInfo.Arguments = "\"" + cilStripper + "\"";
                process.StartInfo.Arguments += " \"" + fileName + "\" \"" + fileName + ".out\"";

                MonoProcessUtility.RunMonoProcess(process, "byte code stripper", Path.Combine(managedLibrariesDirectory, outFile));

                ReplaceFile(managedLibrariesDirectory + "/" + outFile, managedLibrariesDirectory + "/" + fileName);
                File.Delete(managedLibrariesDirectory + "/" + outFile);
            }
        }

        public static string GenerateLinkXmlToPreserveDerivedTypes(string stagingArea, string librariesFolder, RuntimeClassRegistry usedClasses)
        {
            string path = Path.GetFullPath(Path.Combine(stagingArea, "preserved_derived_types.xml"));

            using (TextWriter w = new StreamWriter(path))
            {
                w.WriteLine("<linker>");
                foreach (var assembly in CollectAllAssemblies(librariesFolder, usedClasses))
                {
                    if (AssemblyHelper.IsUnityEngineModule(assembly.Name.Name))
                        continue;

                    var typesToPreserve = new HashSet<TypeDefinition>();
                    CollectBlackListTypes(typesToPreserve, assembly.MainModule.Types, usedClasses.GetAllManagedBaseClassesAsString());

                    // don't write out xml file for assemblies with no types since link.xml files on disk have special meaning to IL2CPP stripping
                    if (typesToPreserve.Count == 0)
                        continue;

                    w.WriteLine("<assembly fullname=\"{0}\">", assembly.Name.Name);
                    foreach (var typeToPreserve in typesToPreserve)
                        w.WriteLine("<type fullname=\"{0}\" preserve=\"all\"/>", typeToPreserve.FullName);
                    w.WriteLine("</assembly>");
                }
                w.WriteLine("</linker>");
            }

            return path;
        }

        // this logic produces similar list of assemblies that IL2CPP will convert (it differs in the way it collects winmd files)
        public static IEnumerable<AssemblyDefinition> CollectAllAssemblies(string librariesFolder, RuntimeClassRegistry usedClasses)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.RemoveSearchDirectory(".");
            resolver.RemoveSearchDirectory("bin");
            resolver.AddSearchDirectory(librariesFolder);

            var usedDlls = usedClasses.GetUserAssemblies().Where(s => usedClasses.IsDLLUsed(s)).Select(file => AssemblyNameReference.Parse(Path.GetFileNameWithoutExtension(file)));
            return CollectAssembliesRecursive(usedDlls.Select(dll => ResolveAssemblyReference(resolver, dll)).Where(a => a != null));
        }

        private static HashSet<AssemblyDefinition> CollectAssembliesRecursive(IEnumerable<AssemblyDefinition> assemblies)
        {
            var pendingAssemblies = new HashSet<AssemblyDefinition>(assemblies, new AssemblyDefinitionComparer());

            var previousCount = 0;
            while (pendingAssemblies.Count > previousCount)
            {
                previousCount = pendingAssemblies.Count;
                pendingAssemblies.UnionWith(pendingAssemblies.ToArray().SelectMany(a => ResolveAssemblyReferences(a)));
            }

            return pendingAssemblies;
        }

        public static IEnumerable<AssemblyDefinition> ResolveAssemblyReferences(AssemblyDefinition assembly)
        {
            return ResolveAssemblyReferences(assembly.MainModule.AssemblyResolver, assembly.MainModule.AssemblyReferences);
        }

        public static IEnumerable<AssemblyDefinition> ResolveAssemblyReferences(IAssemblyResolver resolver, IEnumerable<AssemblyNameReference> assemblyReferences)
        {
            return assemblyReferences.Select(reference => ResolveAssemblyReference(resolver, reference)).Where(a => a != null);
        }

        public static AssemblyDefinition ResolveAssemblyReference(IAssemblyResolver resolver, AssemblyNameReference assemblyName)
        {
            try
            {
                return resolver.Resolve(assemblyName, new ReaderParameters { AssemblyResolver = resolver, ApplyWindowsRuntimeProjections = true });
            }
            catch (AssemblyResolutionException)
            {
                // Skip module dlls if we can't find them - we might build for a platform without modular UnityEngine support.
                if (AssemblyHelper.IsUnityEngineModule(assemblyName.Name))
                    return null;

                // DefaultAssemblyResolver doesn't handle windows runtime references correctly. But that is okay, as they cannot derive from managed types anyway
                // Besides, if any assembly is missing, UnityLinker will stub it and we should not care about it
                return null;
            }
        }

        class AssemblyDefinitionComparer : IEqualityComparer<AssemblyDefinition>
        {
            public bool Equals(AssemblyDefinition x, AssemblyDefinition y)
            {
                return x.FullName == y.FullName;
            }

            public int GetHashCode(AssemblyDefinition obj)
            {
                return obj.FullName.GetHashCode();
            }
        }

        private static void CollectBlackListTypes(HashSet<TypeDefinition> typesToPreserve, IList<TypeDefinition> types, List<string> baseTypes)
        {
            if (types == null)
                return;

            foreach (TypeDefinition typ in types)
            {
                if (typ == null)
                    continue;

                foreach (string baseType in baseTypes)
                {
                    if (!DoesTypeEnheritFrom(typ, baseType))
                        continue;
                    typesToPreserve.Add(typ);
                    break;
                }
                CollectBlackListTypes(typesToPreserve, typ.NestedTypes, baseTypes);
            }
        }

        private static bool DoesTypeEnheritFrom(TypeReference type, string typeName)
        {
            while (type != null)
            {
                if (type.FullName == typeName)
                    return true;

                // If we can't resolve this might lead to trouble later. However, let's wait until it
                // matters and throw there (maybe the error message will be better). Here we can just
                // assume the type does not inherit from the given base class.
                var typeDefinition = type.Resolve();
                if (typeDefinition == null)
                    return false;

                type = typeDefinition.BaseType;
            }
            return false;
        }
    }
}
