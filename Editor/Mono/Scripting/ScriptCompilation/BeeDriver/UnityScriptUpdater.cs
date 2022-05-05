// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bee.BeeDriver;
using Bee.Serialization;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal.APIUpdating;
using Bee.BinLog;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class UnityScriptUpdater : SourceFileUpdaterBase
    {
        readonly string _configurationSourcesFilter;
        NPath ProjectRoot { get; }
        RunnableProgram _scriptUpdaterProgram { get; }

        public UnityScriptUpdater(NPath projectRoot)
        {
            ProjectRoot = projectRoot;
            _scriptUpdaterProgram = DefaultScriptUpdaterProgram();
            _configurationSourcesFilter = APIUpdaterManager.ConfigurationSourcesFilter;
        }

        static RunnableProgram DefaultScriptUpdaterProgram()
        {
            NPath scriptUpdaterExe = $"{EditorApplication.applicationContentsPath}/Tools/ScriptUpdater/ScriptUpdater.exe";
            return new SystemProcessRunnableProgramDuplicate(NetCoreRunProgram.NetCoreRunPath, new[] {scriptUpdaterExe.InQuotes()}, stdOutMode: StdOutMode.LogStdOutOnFinish);
        }

        enum CanUpdateAny
        {
            Certainly,
            Maybe,
            No,
        }

        static CanUpdateAny ContainsUpdatableCompilerMessage(NodeFinishedMessage nodeResult, ObjectsFromDisk dataFromBuildProgram)
        {
            if (!nodeResult.Node.Annotation.StartsWith("Csc"))
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
            if (PotentiallyUpdatableErrorMessages.IsAnyPotentiallyUpdatable(compilerMessages, nodeResult, dataFromBuildProgram))
                return CanUpdateAny.Maybe;

            return CanUpdateAny.No;
        }

        private static bool MatchesFileLineAndColumn(CompilerMessage m, CompilerMessage compilerMessage)
        {
            return m.column == compilerMessage.column && m.line == compilerMessage.line && m.file == compilerMessage.file;
        }

        public override Task<Results> StartIfYouCanFixProblemsInTheseMessages(in NodeFinishedMessage nodeResult,
            ObjectsFromDisk dataFromBuildProgram, CancellationToken cancellationToken)
        {
            var nodeFinishedMessage = nodeResult;

            var containsUpdatableCompilerMessage = ContainsUpdatableCompilerMessage(nodeFinishedMessage, dataFromBuildProgram);
            if (containsUpdatableCompilerMessage == CanUpdateAny.No)
                return Task.FromResult(Results.Empty);

            var assemblyInfo = Helpers.FindOutputDataAssemblyInfoFor(nodeFinishedMessage, dataFromBuildProgram);

            var tempOutputDirectory = new NPath($"Temp/ScriptUpdater/{Math.Abs(nodeFinishedMessage.Node.OutputFile.GetHashCode())}").EnsureDirectoryExists();
            var updateTxtFile = tempOutputDirectory.MakeAbsolute().Combine("updates.txt").DeleteIfExists();

            var args = new[]
            {
                "cs",
                $"\"{EditorApplication.applicationContentsPath}\"",
                $"\"{tempOutputDirectory}\"",
                $"\"{_configurationSourcesFilter}\"",
                assemblyInfo.ScriptUpdaterRsp.ToNPath().InQuotes()
            };

            var runningScriptUpdater = _scriptUpdaterProgram.Start(ProjectRoot.ToString(), args);

            return AwaitScriptUpdaterResults(runningScriptUpdater, updateTxtFile, containsUpdatableCompilerMessage, nodeFinishedMessage.Node.OutputFile);
        }

        async Task<Results> AwaitScriptUpdaterResults(RunningProgram runningScriptUpdater, NPath updateTxtFile, CanUpdateAny containsUpdatableCompilerMessage, string nodeOutputFile)
        {
            //when the user asks us to cancel a build, we do not want to leave stray unity script updaters laying around. we will
            //just wait for them to finish.
            var noToken = CancellationToken.None;
            var scriptUpdaterResult = await runningScriptUpdater.WaitForExitAsync(noToken);

            if (scriptUpdaterResult.ExitCode != 0)
                return ErrorResult(
                    $"Script updater for {nodeOutputFile} failed with exitcode {scriptUpdaterResult.ExitCode} and stdout: {scriptUpdaterResult.Output}");

            if (!updateTxtFile.FileExists())
                return ErrorResult($"Script updater for {nodeOutputFile} failed to produce updates.txt file");

            var updateLines = updateTxtFile.ReadAllLines();
            var updates = updateLines.Select(ParseLineIntoUpdate).ToArray();
            if (updates.Contains(null))
                return ErrorResult($"Script updater for {nodeOutputFile} emitted an invalid line to updates.txt");

            return new Results()
            {
                Messages = (containsUpdatableCompilerMessage == CanUpdateAny.Certainly && updates.Length == 0)
                    ? new[]
                    {
                        new BeeDriverResult.Message(
                            $"Script updater for {nodeOutputFile} expected to be able to make an update, but wasn't able to",
                            BeeDriverResult.MessageKind.Warning)
                    }
                    : Array.Empty<BeeDriverResult.Message>(),
                ProducedUpdates = updates.ToArray()
            };

            static Results ErrorResult(string message) => new()
            {
                Messages = new[] {new BeeDriverResult.Message(message, BeeDriverResult.MessageKind.Error)},
                ProducedUpdates = Array.Empty<Update>()
            };
        }

        internal static Update ParseLineIntoUpdate(string line)
        {
            var separator = " => ";
            var indexOfSeparator = line.IndexOf(separator, StringComparison.InvariantCulture);
            if (indexOfSeparator == -1)
                return null;

            return new Update()
            {
                tempFileWithNewContents = line[..indexOfSeparator],
                originalFileWithError = line[(indexOfSeparator + separator.Length)..]
            };
        }
    }

    static class Helpers
    {
        public static AssemblyData_Out FindOutputDataAssemblyInfoFor(NodeFinishedMessage nodeResult, ObjectsFromDisk dataFromBuildProgram)
        {
            var scriptCompilationDataOut = dataFromBuildProgram.Get<ScriptCompilationData_Out>();
            var outputfileForwardSlash = new NPath(nodeResult.Node.OutputFile).ToString();
            var assemblyDataOut = scriptCompilationDataOut.Assemblies.FirstOrDefault(a => a.Path == outputfileForwardSlash);
            return assemblyDataOut ?? throw new ArgumentException($"Unable to find entry for {outputfileForwardSlash} in dataFromBuildProgram");
        }
    }
}
