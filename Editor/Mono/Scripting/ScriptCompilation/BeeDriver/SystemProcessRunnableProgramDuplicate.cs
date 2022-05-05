// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NiceIO;
using Bee.BeeDriver;

namespace UnityEditor.Scripting.ScriptCompilation
{

    /// <summary>
    /// This is a temporary duplication of SystemRunnableProgram from bee. The version of that in trunk
    /// today has CreateNoWindow=false, while it has to be =true. When the next bee-integration batch lands
    /// in trunk, we will remove this file again.
    /// </summary>
    sealed class SystemProcessRunnableProgramDuplicate : RunnableProgram
    {
        string Executable { get; }
        string[] AlwaysArguments { get; }
        Dictionary<string, string> AlwaysEnvironmentVariables { get; }
        StdOutMode StdOutMode { get; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executable">The executable to run</param>
        /// <param name="alwaysArgument">Any arguments that should always be passed to the executable</param>
        public SystemProcessRunnableProgramDuplicate(string executable, string alwaysArgument) : this(executable, new[] {alwaysArgument})
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="executable">The executable to run</param>
        /// <param name="alwaysArguments">Any arguments that should always be passed to the executable</param>
        /// <param name="alwaysEnvironmentVariables">Environment variables to set when running this program</param>
        /// <param name="stdOutMode">The stdout logging behaviour to use</param>
        public SystemProcessRunnableProgramDuplicate(string executable, string[] alwaysArguments = null, Dictionary<string, string> alwaysEnvironmentVariables = null, StdOutMode stdOutMode = StdOutMode.LogStartArgumentsAndExitcode)
        {
            Executable = executable;
            AlwaysArguments = alwaysArguments ?? new string[0];
            AlwaysEnvironmentVariables = alwaysEnvironmentVariables ?? new Dictionary<string, string>();
            StdOutMode = stdOutMode;
        }

        /// <summary>
        /// The Start implementation.
        /// </summary>
        protected override RunningProgram StartImpl(string workingDirectory, string[] arguments, Dictionary<string, string> envVars)
        {
            var executable = new NPath(Executable).MakeAbsolute();
            if (!executable.Exists())
                executable = Executable; // fallback to system path

            var psi = new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = executable.ToString(SlashMode.Native),
                Arguments = AlwaysArguments.Concat(arguments).SeparateWithSpace(),
                CreateNoWindow = true,
                RedirectStandardError = !StdOutMode.HasFlag(StdOutMode.Stream),
                RedirectStandardOutput = !StdOutMode.HasFlag(StdOutMode.Stream),
                RedirectStandardInput = true,
                WorkingDirectory = workingDirectory,
            };
            foreach (var kvp in AlwaysEnvironmentVariables)
                psi.EnvironmentVariables[kvp.Key] = kvp.Value;
            if (envVars != null)
                foreach (var kvp in envVars)
                    psi.EnvironmentVariables[kvp.Key] = kvp.Value;

            return new SystemProcessRunningProgram(StdOutMode, psi);
        }

        class SystemProcessRunningProgram : RunningProgram
        {
            readonly List<string> _stdout = new();
            readonly List<string> _stderr = new();

            readonly TaskCompletionSource<Result> _taskCompletionSource;
            private bool _startedStreamReaders;
            private bool _deferProcessClose;

            public SystemProcessRunningProgram(StdOutMode stdoutMode, ProcessStartInfo processStartInfo) : base(stdoutMode)
            {
                _taskCompletionSource = new TaskCompletionSource<Result>();
                var process = new Process()
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true,
                };

                process.Exited += (sender, args) =>
                {
                    //the mono implementation of System.Diagnostic.Process is quite brittle, especially when it comes to calling methods on a Process
                    //from different threads. You basically must ensure that any operation you call on a Process is done with a mutex held, so you only
                    //ever call one method at a time. We use the SystemProcessRunningProgram object as mutex object. If you ever observe NullReferenceExceptions
                    //inside mono process code that looks like it really cannot happen (say:  "if (stdout) stdout.Something()", it is very likely that you
                    //are looking at a multithreading bug where you were calling two methods from two different threads at the same time and you got unlucky.
                    lock (this)
                    {
                        //It is important that the process gets closed when it's no longer required, as there are native resources that need to be freed.
                        //CloseProcessAndSetResult will also flush the stdout/stderr buffers. We chose to not put them burden of closing the process object on the user of SystemProcessRunningProgram,
                        //and instead auto close the underlying process after it exits.
                        //_however_ things are never so simple. The process could exit so quickly, and we could receive this .Exited event so quickly
                        //that our SystemRunnableProcess constructor didn't finish yet, and the stdout/stderr streamreaders haven't started. If this happens
                        //.BeginErrorReadLine() will throw an exception when we already closed the process. We deal with this situation by checking
                        //here if those readers have already started. if yes->great we close the process. if not-> we defer the closing of the process until the
                        //streamreaders are setup. all reads/writes of the state that tracks if we want to defer the process close and if the streamreaders started
                        //are guarded by lock(this).

                        if (_startedStreamReaders || stdoutMode.HasFlag(StdOutMode.Stream))
                        {
                            CloseProcessAndSetResult(process);
                        }
                        else
                            _deferProcessClose = true;
                    }
                };

                if (stdoutMode.HasFlag(StdOutMode.LogStartArgumentsAndExitcode))
                {
                    Console.WriteLine($"Starting: {processStartInfo.FileName} {processStartInfo.Arguments}");
                    Console.WriteLine($"WorkingDir: {new NPath(processStartInfo.WorkingDirectory).MakeAbsolute()}");
                }
                try
                {
                    process.Start();
                }
                catch (Win32Exception e)
                {
                    string MakeFailureMessage()
                    {
                        const int E_FAIL = unchecked((int)0x80004005);
                        const int ERROR_FILE_NOT_FOUND = 0x2;

                        if (e.ErrorCode == E_FAIL && e.NativeErrorCode == ERROR_FILE_NOT_FOUND)
                            return $"Failed to run {process.StartInfo.FileName} as the executable did not exist";
                        return $"Exception when trying to start {process.StartInfo.FileName}: " + e;
                    }

                    _taskCompletionSource.SetResult(new Result() { ExitCode = 123, Output = MakeFailureMessage()});
                    return;
                }

                if (!stdoutMode.HasFlag(StdOutMode.Stream))
                {
                    static void AddReceivedData(List<string> stdout, string argsData)
                    {
                        if (argsData != null)
                            stdout.Add(argsData);
                    }

                    lock (this)
                    {
                        process.OutputDataReceived += (sender, args) => AddReceivedData(_stdout, args.Data);
                        process.ErrorDataReceived += (sender, args) => AddReceivedData(_stderr, args.Data);
                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();

                        if (_deferProcessClose)
                            CloseProcessAndSetResult(process);

                        _startedStreamReaders = true;
                    }
                }
            }

            private void CloseProcessAndSetResult(Process process)
            {
                var processExitCode = process.ExitCode;

                //before calling .Close(), it's important to first call .WaitForExit(). Even if the .Exited event has already been raised.
                //.WaitForExit() will make sure that not only the process has exited, but that both the stdout and stderr AsyncStreamReaders inside
                //of the Process class have received an EOF on their stream.  If you do not call .WaitForExit() then depending on timing situations,
                //it is possible to miss the stdout of quickly exiting processes.
                process.WaitForExit();
                process.Close();
                SetResult(processExitCode);
            }

            private void SetResult(int processExitCode)
            {
                string MakeOutput()
                {
                    if (_stderr.Count == 0)
                        return _stdout.SeparateWith("\n");
                    if (_stdout.Count == 0)
                        return _stderr.SeparateWith("\n");
                    return new[] {"STDOUT:"}.Concat(_stdout).Concat(new[] {"STDERR:"}).Concat(_stderr)
                        .SeparateWith("\n");
                }

                var result = new Result()
                {
                    ExitCode = processExitCode, Output = MakeOutput()
                };
                PrintToStdoutIfRequired(result);
                _taskCompletionSource.TrySetResult(result);
            }

            public override Task<Result> WaitForExitAsync(CancellationToken cancellationToken = default) => _taskCompletionSource.Task.WithCancellation(cancellationToken);
            public override bool HasExited(out Result result)
            {
                if (_taskCompletionSource.Task.IsCompleted)
                {
                    result = _taskCompletionSource.Task.Result;
                    return true;
                }

                result = null;
                return false;
            }
        }
    }

    static class EnumerableExtensions
    {
        public static string SeparateWithSpace(this IEnumerable<String> values)
        {
            return values.SeparateWith(" ");
        }

        public static string SeparateWith(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values.ToArray());
        }
    }

}
