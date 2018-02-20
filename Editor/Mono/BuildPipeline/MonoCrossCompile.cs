// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.Scripting.ScriptCompilation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace UnityEditor
{
    [Flags]
    internal enum CrossCompileOptions
    {
        Dynamic = 0,
        FastICall = 1 << 0,
        Static = 1 << 1,
        Debugging = 1 << 2,
        ExplicitNullChecks = 1 << 3,
        LoadSymbols = 1 << 4
    }

    internal class MonoCrossCompile
    {
        static public string ArtifactsPath = null;

        private class JobCompileAOT
        {
            public JobCompileAOT(BuildTarget target, string crossCompilerAbsolutePath,
                                 string assembliesAbsoluteDirectory, CrossCompileOptions crossCompileOptions,
                                 string input, string output, string additionalOptions)
            {
                m_target = target;
                m_crossCompilerAbsolutePath = crossCompilerAbsolutePath;
                m_assembliesAbsoluteDirectory = assembliesAbsoluteDirectory;
                m_crossCompileOptions = crossCompileOptions;
                m_input = input;
                m_output = output;
                m_additionalOptions = additionalOptions;
            }

            public void ThreadPoolCallback(System.Object threadContext)
            {
                try
                {
                    MonoCrossCompile.CrossCompileAOT(m_target, m_crossCompilerAbsolutePath,
                        m_assembliesAbsoluteDirectory, m_crossCompileOptions,
                        m_input, m_output, m_additionalOptions);
                }
                catch (Exception ex)
                {
                    m_Exception = ex;
                }
                m_doneEvent.Set();
            }

            private BuildTarget         m_target;
            private string              m_crossCompilerAbsolutePath;
            private string              m_assembliesAbsoluteDirectory;
            private CrossCompileOptions m_crossCompileOptions;
            public  string              m_input;
            public  string              m_output;
            public  string              m_additionalOptions;

            public ManualResetEvent     m_doneEvent = new ManualResetEvent(false);
            public Exception            m_Exception = null;
        }

        static public void CrossCompileAOTDirectory(BuildTarget buildTarget, CrossCompileOptions crossCompileOptions,
            string sourceAssembliesFolder, string targetCrossCompiledASMFolder,
            string additionalOptions)
        {
            CrossCompileAOTDirectory(buildTarget, crossCompileOptions, sourceAssembliesFolder, targetCrossCompiledASMFolder, "", additionalOptions);
        }

        static public void CrossCompileAOTDirectory(BuildTarget buildTarget, CrossCompileOptions crossCompileOptions,
            string sourceAssembliesFolder, string targetCrossCompiledASMFolder,
            string pathExtension, string additionalOptions)
        {
            string crossCompilerPath = BuildPipeline.GetBuildToolsDirectory(buildTarget);
            if (Application.platform == RuntimePlatform.OSXEditor)
                crossCompilerPath = Path.Combine(Path.Combine(crossCompilerPath, pathExtension), "mono-xcompiler");
            else
                crossCompilerPath = Path.Combine(Path.Combine(crossCompilerPath, pathExtension), "mono-xcompiler.exe");

            sourceAssembliesFolder = Path.Combine(Directory.GetCurrentDirectory(), sourceAssembliesFolder);
            targetCrossCompiledASMFolder = Path.Combine(Directory.GetCurrentDirectory(), targetCrossCompiledASMFolder);


            // Generate AOT Files (using OSX cross-compiler)
            foreach (string fileName in Directory.GetFiles(sourceAssembliesFolder))
            {
                if (Path.GetExtension(fileName) != ".dll")
                    continue;

                // Cross AOT compile
                string inputPath = Path.GetFileName(fileName);
                string outputPath = Path.Combine(targetCrossCompiledASMFolder, inputPath + ".s");

                if (EditorUtility.DisplayCancelableProgressBar("Building Player", "AOT cross compile " + inputPath, 0.95F))
                    throw new OperationCanceledException();

                CrossCompileAOT(buildTarget, crossCompilerPath, sourceAssembliesFolder,
                    crossCompileOptions, inputPath, outputPath, additionalOptions);
            }
        }

        static public bool CrossCompileAOTDirectoryParallel(BuildTarget buildTarget, CrossCompileOptions crossCompileOptions,
            string sourceAssembliesFolder, string targetCrossCompiledASMFolder,
            string additionalOptions)
        {
            return CrossCompileAOTDirectoryParallel(buildTarget, crossCompileOptions, sourceAssembliesFolder,
                targetCrossCompiledASMFolder, "", additionalOptions);
        }

        static public bool CrossCompileAOTDirectoryParallel(BuildTarget buildTarget, CrossCompileOptions crossCompileOptions,
            string sourceAssembliesFolder, string targetCrossCompiledASMFolder,
            string pathExtension, string additionalOptions)
        {
            string crossCompilerPath = BuildPipeline.GetBuildToolsDirectory(buildTarget);
            if (Application.platform == RuntimePlatform.OSXEditor)
                crossCompilerPath = Path.Combine(Path.Combine(crossCompilerPath, pathExtension), "mono-xcompiler");
            else
                crossCompilerPath = Path.Combine(Path.Combine(crossCompilerPath, pathExtension), "mono-xcompiler.exe");

            return CrossCompileAOTDirectoryParallel(
                crossCompilerPath, buildTarget, crossCompileOptions,
                sourceAssembliesFolder, targetCrossCompiledASMFolder, additionalOptions);
        }

        static private bool WaitForBuildOfFile(List<ManualResetEvent> events, ref long timeout)
        {
            long timeMs1 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            int finished = WaitHandle.WaitAny(events.ToArray(), (Int32)timeout);
            long timeMs2 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if (finished == WaitHandle.WaitTimeout)
                return false;

            events.RemoveAt(finished);
            timeout -= (timeMs2 - timeMs1);
            if (timeout < 0)
                timeout = 0;
            return true;
        }

        static public void DisplayAOTProgressBar(int totalFiles, int filesFinished)
        {
            string msg = String.Format(@"AOT cross compile ({0}/{1})",
                    (filesFinished + 1).ToString(), totalFiles.ToString());
            EditorUtility.DisplayProgressBar("Building Player", msg, 0.95F);
        }

        static public bool CrossCompileAOTDirectoryParallel(string crossCompilerPath, BuildTarget buildTarget, CrossCompileOptions crossCompileOptions,
            string sourceAssembliesFolder, string targetCrossCompiledASMFolder, string additionalOptions)
        {
            sourceAssembliesFolder = Path.Combine(Directory.GetCurrentDirectory(), sourceAssembliesFolder);
            targetCrossCompiledASMFolder = Path.Combine(Directory.GetCurrentDirectory(), targetCrossCompiledASMFolder);


            // Generate AOT Files (using OSX cross-compiler)
            int workerThreads = 1;
            int completionPortThreads = 1;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            List<JobCompileAOT> jobList = new List<JobCompileAOT>();
            List<ManualResetEvent> eventList = new List<ManualResetEvent>();


            bool success = true;

            var dllList = new List<string>(Directory.GetFiles(sourceAssembliesFolder)
                    .Where(path => Path.GetExtension(path) == ".dll"));

            int numFiles = dllList.Count;
            int numFinished = 0;
            DisplayAOTProgressBar(numFiles, numFinished);

            long timeout = System.Math.Min(30 * 1000 * 60, (numFiles + 3) * 1000 * 30); // 30 minute limit in case of FOOBARs, 30 seconds for each file

            foreach (string fileName in dllList)
            {
                // Cross AOT compile
                string inputPath = Path.GetFileName(fileName);
                string outputPath = Path.Combine(targetCrossCompiledASMFolder, inputPath + ".s");

                JobCompileAOT job = new JobCompileAOT(buildTarget, crossCompilerPath, sourceAssembliesFolder,
                        crossCompileOptions, inputPath, outputPath,
                        additionalOptions);
                jobList.Add(job);
                eventList.Add(job.m_doneEvent);
                ThreadPool.QueueUserWorkItem(job.ThreadPoolCallback);

                if (eventList.Count >= Environment.ProcessorCount)
                {
                    success = WaitForBuildOfFile(eventList, ref timeout);
                    DisplayAOTProgressBar(numFiles, numFinished);
                    numFinished += 1;
                    if (!success)
                        break;
                }
            }

            while (eventList.Count > 0)
            {
                success = WaitForBuildOfFile(eventList, ref timeout);
                DisplayAOTProgressBar(numFiles, numFinished);
                numFinished += 1;
                if (!success)
                    break;
            }

            foreach (var job in jobList)
            {
                if (job.m_Exception != null)
                {
                    Debug.LogErrorFormat("Cross compilation job {0} failed.\n{1}", job.m_input, job.m_Exception);
                    success = false;
                }
            }
            return success;
        }

        static bool IsDebugableAssembly(string fname)
        {
            return EditorCompilationInterface.Instance.IsRuntimeScriptAssembly(fname);
        }

        static void CrossCompileAOT(BuildTarget target, string crossCompilerAbsolutePath, string assembliesAbsoluteDirectory, CrossCompileOptions crossCompileOptions, string input, string output, string additionalOptions)
        {
            string arguments = "";

            // We don't want debugging for non-script assemblies (anyway source code is not available for the end users)
            if (!IsDebugableAssembly(input))
            {
                crossCompileOptions &= ~CrossCompileOptions.Debugging;
                crossCompileOptions &= ~CrossCompileOptions.LoadSymbols;
            }

            bool debugging = ((crossCompileOptions & CrossCompileOptions.Debugging) != 0);
            bool loadSymbols = ((crossCompileOptions & CrossCompileOptions.LoadSymbols) != 0);
            bool initDebugging = (debugging || loadSymbols);
            if (initDebugging)
                arguments += "--debug ";

            if (debugging)
            {
                // Do not put locals into registers when debugging
                arguments += "--optimize=-linears ";
            }

            arguments += "--aot=full,asmonly,";

            if (initDebugging)
                arguments += "write-symbols,";

            if ((crossCompileOptions & CrossCompileOptions.Debugging) != 0)
                arguments += "soft-debug,";
            else if (!initDebugging)
                arguments += "nodebug,";

            if (target != BuildTarget.iOS)
            {
                //arguments += "fail-if-methods-are-skipped,";
                arguments += "print-skipped,";
            }

            if (additionalOptions != null & additionalOptions.Trim().Length > 0)
                arguments += additionalOptions.Trim() + ",";

            string outputFileName = Path.GetFileName(output);
            // Mono outfile parameter doesnt take absolute paths,
            // So we temporarily write into the assembliesAbsoluteDirectory and move it away afterwards
            string outputTempPath = Path.Combine(assembliesAbsoluteDirectory, outputFileName);
            if ((crossCompileOptions & CrossCompileOptions.FastICall) != 0)
                arguments += "ficall,";
            if ((crossCompileOptions & CrossCompileOptions.Static) != 0)
                arguments += "static,";
            arguments += "outfile=\"" + outputFileName + "\" \"" + input + "\" ";


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = crossCompilerAbsolutePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.EnvironmentVariables["MONO_PATH"] = assembliesAbsoluteDirectory;
            process.StartInfo.EnvironmentVariables["GAC_PATH"] = assembliesAbsoluteDirectory;
            process.StartInfo.EnvironmentVariables["GC_DONT_GC"] = "yes please";
            if ((crossCompileOptions & CrossCompileOptions.ExplicitNullChecks) != 0)
                process.StartInfo.EnvironmentVariables["MONO_DEBUG"] = "explicit-null-checks";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;


            // todo: move this out of this file ... it needs to be initialised in the per-platform extension
            //        ArtifactsPath = Environment.ExpandEnvironmentVariables("%tmp%\\UnityPSVitaArtifacts\\");

            if (ArtifactsPath != null)
            {
                if (!Directory.Exists(ArtifactsPath)) Directory.CreateDirectory(ArtifactsPath);
                File.AppendAllText(ArtifactsPath + "output.txt", process.StartInfo.FileName + "\n");
                File.AppendAllText(ArtifactsPath + "output.txt", process.StartInfo.Arguments + "\n");
                File.AppendAllText(ArtifactsPath + "output.txt", assembliesAbsoluteDirectory + "\n");
                File.AppendAllText(ArtifactsPath + "output.txt", outputTempPath + "\n");
                File.AppendAllText(ArtifactsPath + "output.txt", input + "\n");
                File.AppendAllText(ArtifactsPath + "houtput.txt", outputFileName + "\n\n");
                File.Copy(assembliesAbsoluteDirectory + "\\" + input, ArtifactsPath + "\\" + input, true);
            }

            process.StartInfo.WorkingDirectory = assembliesAbsoluteDirectory;
            MonoProcessUtility.RunMonoProcess(process, "AOT cross compiler", outputTempPath);
            // For some reason we can't pass a full path to outfile, so we move the .s file after compilation instead
            File.Move(outputTempPath, output);

            //handle .def files if present for dlls
            if ((crossCompileOptions & CrossCompileOptions.Static) == 0)
            {
                string defOutputTempPath = Path.Combine(assembliesAbsoluteDirectory, outputFileName + ".def");
                if (File.Exists(defOutputTempPath))
                    File.Move(defOutputTempPath, output + ".def");
            }
        }
    }
}
