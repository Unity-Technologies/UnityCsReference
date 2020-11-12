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

        class UnityScriptUpdaterTask : Task
        {
            public UnityScriptUpdaterTask(NPath scriptUpdaterRsp, string tempOutputDirectory, NPath projectRoot,
                                          RunnableProgram unityScriptUpdaterProgram, NodeResult nodeResult,
                                          bool produceErrorIfNoUpdatesAreProduced) : base(nodeResult, tempOutputDirectory, produceErrorIfNoUpdatesAreProduced)
            {
                var args = new[]
                {
                    "cs",
                    EditorApplication.applicationContentsPath,
                    $"\"{TempOutputDirectory}\"",
                    $"\"{APIUpdaterManager.ConfigurationSourcesFilter}\"",
                    scriptUpdaterRsp.InQuotes()
                };

                RunningProgram = unityScriptUpdaterProgram.Start(projectRoot.ToString(), args);
            }

            public override RunningProgram RunningProgram { get; }
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

        public override Task StartIfYouCanFixProblemsInTheseMessages(NodeResult nodeResult, string tempOutputDirectory, BeeDriver beeDriver)
        {
            var containsUpdatableCompilerMessage = ContainsUpdatableCompilerMessage(nodeResult, beeDriver);
            if (containsUpdatableCompilerMessage == CanUpdateAny.No)
                return null;

            var assemblyInfo = Helpers.FindOutputDataAssemblyInfoFor(nodeResult, beeDriver);

            if (!assemblyInfo.sourcesAreInsideProjectFolder)
                return null;

            //future improvement opportunities: We could do more refactoring so that we do not need to get the packageName and packageResolvePath
            //from the buildprogram, but that we use the data we have available in the editor already directly.  If we do both those combinations, then we could remove the dataForEditor file,
            //at least until we run into situations where we want to share more data from the buildprogram to the editor.
            return new UnityScriptUpdaterTask(assemblyInfo.scriptUpdaterRsp, tempOutputDirectory, ProjectRoot, _scriptUpdaterProgram, nodeResult, containsUpdatableCompilerMessage == CanUpdateAny.Certainly);
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
