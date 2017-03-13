// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

internal abstract class NativeCompiler : INativeCompiler
{
    protected virtual string objectFileExtension { get { return "o"; } }

    public abstract void CompileDynamicLibrary(string outFile, IEnumerable<string> sources, IEnumerable<string> includePaths, IEnumerable<string> libraries, IEnumerable<string> libraryPaths);

    protected virtual void SetupProcessStartInfo(ProcessStartInfo startInfo)
    {
    }

    protected void Execute(string arguments, string compilerPath)
    {
        var startInfo = new ProcessStartInfo(compilerPath, arguments);

        SetupProcessStartInfo(startInfo);

        RunProgram(startInfo);
    }

    protected void ExecuteCommand(string command, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(command, arguments.Aggregate((buff, s) => buff + " " + s));

        SetupProcessStartInfo(startInfo);

        RunProgram(startInfo);
    }

    private void RunProgram(ProcessStartInfo startInfo)
    {
        using (var program = new UnityEditor.Utils.Program(startInfo))
        {
            program.Start();

            while (!program.WaitForExit(100))
            {
            }

            var output = string.Empty;
            var standardOutput = program.GetStandardOutput();
            if (standardOutput.Length > 0)
                output = standardOutput.Aggregate((buf, s) => buf + Environment.NewLine + s);

            var errorOutput = program.GetErrorOutput();
            if (errorOutput.Length > 0)
                output += errorOutput.Aggregate((buf, s) => buf + Environment.NewLine + s);

            if (program.ExitCode != 0)
            {
                UnityEngine.Debug.LogError("Failed running " + startInfo.FileName + " " + startInfo.Arguments + "\n\n" + output);

                throw new Exception("IL2CPP compile failed.");
            }
        }
    }

    protected static string Aggregate(IEnumerable<string> items, string prefix, string suffix)
    {
        return items.Aggregate("", (current, additionalFile) => current + (prefix + additionalFile + suffix));
    }

    class Counter
    {
        public int index;
    }

    internal static void ParallelFor<T>(T[] sources, Action<T> action)
    {
        var threads = new Thread[Environment.ProcessorCount];
        var counter = new Counter();
        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(delegate(object obj)
                {
                    var c = (Counter)obj;
                    int index;
                    while ((index = Interlocked.Increment(ref c.index)) <= sources.Length)
                    {
                        action(sources[index - 1]);
                    }
                });
        }
        foreach (var t in threads)
            t.Start(counter);
        foreach (var t in threads)
            t.Join();
    }

    protected internal static IEnumerable<string> AllSourceFilesIn(string directory)
    {
        return Directory.GetFiles(directory, "*.cpp", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(directory, "*.c", SearchOption.AllDirectories));
    }

    protected internal static bool IsSourceFile(string source)
    {
        var extension = Path.GetExtension(source);
        return extension == "cpp" || extension == "c";
    }

    protected string ObjectFileFor(string source)
    {
        return Path.ChangeExtension(source, objectFileExtension);
    }
}
