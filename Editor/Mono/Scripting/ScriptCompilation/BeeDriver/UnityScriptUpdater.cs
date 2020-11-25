// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal.APIUpdating;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class UnityScriptUpdater : SourceFileUpdaterBase
    {
        NPath ProjectRoot { get; }
        private RunnableProgram _scriptUpdaterProgram { get; }

        public UnityScriptUpdater(NPath projectRoot, RunnableProgram customScriptUpdaterProgram = null)
        {
            ProjectRoot = projectRoot;
            _scriptUpdaterProgram = customScriptUpdaterProgram ?? DefaultScriptUpdaterProgram();
        }

        static RunnableProgram DefaultScriptUpdaterProgram()
        {
            string scriptUpdaterExe = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/ScriptUpdater.exe";
            return new SystemProcessRunnableProgram(NetCoreRunProgram.NetCoreRunPath, scriptUpdaterExe);
        }

        internal class UnityScriptUpdaterTask : Task
        {
            private NPath _updateTxtFile { get; }

            public UnityScriptUpdaterTask(NPath scriptUpdaterRsp, NPath projectRoot,
                                          RunnableProgram unityScriptUpdaterProgram, NodeResult nodeResult,
                                          bool produceErrorIfNoUpdatesAreProduced) : base(nodeResult, produceErrorIfNoUpdatesAreProduced)
            {
                var tempOutputDirectory = new NPath($"Temp/ScriptUpdater/{Math.Abs(nodeResult.outputfile.GetHashCode())}").EnsureDirectoryExists();
                _updateTxtFile = tempOutputDirectory.MakeAbsolute().Combine("updates.txt").DeleteIfExists();

                var args = new[]
                {
                    "cs",
                    EditorApplication.applicationContentsPath,
                    $"\"{tempOutputDirectory}\"",
                    $"\"{APIUpdaterManager.ConfigurationSourcesFilter}\"",
                    scriptUpdaterRsp.InQuotes()
                };

                RunningProgram = unityScriptUpdaterProgram.Start(projectRoot.ToString(), args);
            }

            RunningProgram RunningProgram { get; }

            public override void WaitUntilFinished() => RunningProgram.WaitForExit();

            public override bool Finished => RunningProgram.HasExited;

            private Results _results;
            public override Results Results
            {
                get
                {
                    if (_results != null)
                        return _results;

                    _results = GatherResults();
                    return _results;
                }
            }

            Results ErrorResult(string message)
            {
                return new Results()
                {
                    Messages = new[] {new BeeDriverResult.Message(message, BeeDriverResult.MessageKind.Error)},
                    ProducedUpdates = Array.Empty<Update>()
                };
            }

            private Results GatherResults()
            {
                if (!RunningProgram.HasExited)
                    throw new ArgumentException($"Script updater for {NodeResult.outputfile}: results requested while RunningProgram has not exited");
                if (RunningProgram.ExitCode != 0)
                    return ErrorResult($"Script updater for {NodeResult.outputfile} failed with exitcode {RunningProgram.ExitCode} and stdout: {RunningProgram.GetStdoutAndStdErrCombined()}");

                if (!_updateTxtFile.FileExists())
                    return ErrorResult($"Script updater for {NodeResult.outputfile} failed to produce updates.txt file");

                var updateLines = _updateTxtFile.ReadAllLines();

                var updates = updateLines.Select(ParseLineIntoUpdate).ToArray();
                if (updates.Contains(null))
                    return ErrorResult($"Script updater for {NodeResult.outputfile} emitted an invalid line to updates.txt");

                return new Results()
                {
                    Messages = Array.Empty<BeeDriverResult.Message>(),
                    ProducedUpdates = updates.ToArray()
                };
            }

            internal static Update ParseLineIntoUpdate(string line)
            {
                var separator = " => ";
                var indexOfSeparator = line.IndexOf(separator);
                if (indexOfSeparator == -1)
                    return null;

                return new Update()
                {
                    tempFileWithNewContents = line.Substring(0, indexOfSeparator),
                    originalFileWithError = line.Substring(indexOfSeparator + separator.Length)
                };
            }

            public override void Abort()
            {
                RunningProgram.Abort();
            }
        }

        enum CanUpdateAny
        {
            Certainly,
            Maybe,
            No,
        }

        CanUpdateAny ContainsUpdatableCompilerMessage(NodeResult nodeResult, BeeDriver beeDriver)
        {
            if (!nodeResult.annotation.StartsWith("Csc"))
                return CanUpdateAny.No;

            var compilerMessages = BeeScriptCompilation.ParseCompilerOutput(nodeResult);

            bool IsOnlyMessageForThisFileLineAndColumn(CompilerMessage compilerMessage)
            {
                //we want to see if this is the only error on this location.  we will make an enumerable that matches all compilermessages that match this location
                //We do Skip(1).Any() as a bit of an unconventional way to express what we care about: is there more than 1 or not.
                return !compilerMessages.Where(m => MatchesFileLineAndColumn(compilerMessage, m)).Skip(1).Any();
            }

            var upgradableMessages = compilerMessages.Where(c => c.message.Contains("(UnityUpgradable")).ToArray();

            //Some (UnityUpgradable) errors can be paired with a genuine user error on the same line/column. When this happens it is a known problem
            //that we are unable to upgrade the UnityUpgradable error. So we'll only return Certainly if there is not an other compilermessage pointing to the
            //same file,line,column. otherwise, we return maybe, so that a failure to do the update won't print a console message saying there is a bug.
            if (upgradableMessages.Any(IsOnlyMessageForThisFileLineAndColumn))
                return CanUpdateAny.Certainly;
            if (upgradableMessages.Any())
                return CanUpdateAny.Maybe;

            //The "unknown type or namespace" genre of messages we are not sure about. Some of these are legit user programming errors, some of them
            //are caused because we moved/renamed a type. In this case we will run the script updater to figure out, and if no updates were produced then
            //apparently it was a real user programming mistake instead of an updatable error;
            if (PotentiallyUpdatableErrorMessages.IsAnyPotentiallyUpdatable(compilerMessages, nodeResult, beeDriver))
                return CanUpdateAny.Maybe;

            return CanUpdateAny.No;
        }

        private static bool MatchesFileLineAndColumn(CompilerMessage m, CompilerMessage compilerMessage)
        {
            return m.column == compilerMessage.column && m.line == compilerMessage.line && m.file == compilerMessage.file;
        }

        public override Task StartIfYouCanFixProblemsInTheseMessages(NodeResult nodeResult, BeeDriver beeDriver)
        {
            var containsUpdatableCompilerMessage = ContainsUpdatableCompilerMessage(nodeResult, beeDriver);
            if (containsUpdatableCompilerMessage == CanUpdateAny.No)
                return null;

            var assemblyInfo = Helpers.FindOutputDataAssemblyInfoFor(nodeResult, beeDriver);

            return new UnityScriptUpdaterTask(assemblyInfo.scriptUpdaterRsp, ProjectRoot, _scriptUpdaterProgram, nodeResult, containsUpdatableCompilerMessage == CanUpdateAny.Certainly);
        }
    }

    static class Helpers
    {
        public static AssemblyData_Out FindOutputDataAssemblyInfoFor(NodeResult nodeResult, BeeDriver beeDriver)
        {
            var scriptCompilationDataOut = beeDriver.DataFromBuildProgram.Get<ScriptCompilationData_Out>();
            var outputfileForwardSlash = new NPath(nodeResult.outputfile).ToString();
            var assemblyDataOut = scriptCompilationDataOut.assemblies.FirstOrDefault(a => a.path == outputfileForwardSlash);
            return assemblyDataOut ?? throw new ArgumentException($"Unable to find entry for {outputfileForwardSlash} in dataFromBuildProgram");
        }
    }
}
