// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Bee.BeeDriver;
using NiceIO;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;

namespace UnityEditor.Mono.Utils
{
    internal enum PramLogLevel
    {
        Quiet,
        Verbose,
        VeryVerbose,
        Trace,
    }

    /// <summary>
    /// Invocation wrapper for Pram
    /// (Platform Runtime Application Manager - https://github.cds.internal.unity3d.com/unity/pram)
    /// </summary>
    internal class Pram
    {
        private NPath pramDll;

        public PramLogLevel LogLevel { get; set; } = PramLogLevel.Verbose;

        public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
        public NPath[] ProviderLoadPaths { get; set; }

        public static readonly NPath PramDataDirectory = "Library/PramData";

        public Pram(NPath pramDll, params NPath[] providerLoadPaths)
        {
            PramDataDirectory.EnsureDirectoryExists();

            this.pramDll = pramDll;
            this.EnvironmentVariables.Add("PRAM_DIRECTORY", PramDataDirectory.ToString());
            this.ProviderLoadPaths = providerLoadPaths;
        }

        public Program CreateProgram(IEnumerable<string> arguments)
        {
            // *begin-nonstandard-formatting*
            var logLevelArgument = LogLevel switch
            {
                PramLogLevel.Quiet => "--quiet",
                PramLogLevel.Verbose => "--verbose",
                PramLogLevel.VeryVerbose => "--very-verbose",
                PramLogLevel.Trace => "--trace",
                _ => throw new ArgumentOutOfRangeException()
            };
            // *end-nonstandard-formatting*
            var providerLoadPathArguments = ProviderLoadPaths.Select(p => $"--provider-load-path={p.InQuotes()}");

            return new NetCoreProgram(pramDll.ToString(SlashMode.Native),
                providerLoadPathArguments
                    .Append(logLevelArgument)
                    .Concat(arguments)
                    .SeparateWith(" "),
                info =>
                {
                    foreach (var envVar in EnvironmentVariables)
                        info.EnvironmentVariables[envVar.Key] = envVar.Value;
                });
        }

        public Program AppKill(string provider, string applicationId, string environment) =>
            CreateProgram(new[] {"app-kill", "--environment", environment, provider, applicationId });

        public Program AppDeploy(string provider, string applicationId, string environment, NPath applicationPath) =>
            CreateProgram(new[] {"app-deploy", "--environment", environment, provider, applicationId, CommandLineFormatter.PrepareFileName(applicationPath.ToString()) });

        public Program AppStartDetached(string provider, string applicationId, string environment, params string[] arguments) =>
            CreateProgram(new[] {"app-start-detached", "--environment", environment, provider, applicationId, "--"}.Concat(arguments));
    }
}
