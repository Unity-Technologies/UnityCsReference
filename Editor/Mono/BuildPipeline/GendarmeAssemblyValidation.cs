// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;

internal struct GendarmeOptions
{
    public string RuleSet;
    public string ConfigFilePath;
    public string[] UserAssemblies;
}

internal abstract class GendarmeValidationRule : IValidationRule
{
    private readonly string _gendarmeExePath;

    protected GendarmeValidationRule(string gendarmeExePath)
    {
        _gendarmeExePath = gendarmeExePath;
    }

    public ValidationResult Validate(IEnumerable<string> userAssemblies, params object[] options)
    {
        var commandLine = BuildGendarmeCommandLineArguments(userAssemblies);
        var result = new ValidationResult
        {
            Success = true,
            Rule = this,
            CompilerMessages = null
        };

        try
        {
            result.Success = StartManagedProgram(_gendarmeExePath, commandLine, new GendarmeOutputParser(), ref result.CompilerMessages);
        }
        catch (Exception e)
        {
            result.Success = false;
            result.CompilerMessages = new[]
            {
                new CompilerMessage
                {
                    file = "Exception",
                    message = e.Message,
                    line = 0,
                    column = 0,
                    type = CompilerMessageType.Error
                }
            };
        }

        return result;
    }

    protected abstract GendarmeOptions ConfigureGendarme(IEnumerable<string> userAssemblies);

    protected string BuildGendarmeCommandLineArguments(IEnumerable<string> userAssemblies)
    {
        var options = ConfigureGendarme(userAssemblies);
        if (options.UserAssemblies == null || options.UserAssemblies.Length == 0)
            return null;

        var commandLine = new List<string>
        {
            "--config " + options.ConfigFilePath,
            "--set " + options.RuleSet,
        };
        commandLine.AddRange(options.UserAssemblies);

        return commandLine.Aggregate((agg, i) => agg + " " + i);
    }

    private static bool StartManagedProgram(string exe, string arguments, CompilerOutputParserBase parser, ref IEnumerable<CompilerMessage> compilerMessages)
    {
        using (var p = ManagedProgramFor(exe, arguments))
        {
            p.LogProcessStartInfo();
            try
            {
                p.Start();
            }
            catch
            {
                throw new Exception("Could not start " + exe);
            }

            p.WaitForExit();

            if (p.ExitCode == 0)
                return true;

            compilerMessages = parser.Parse(p.GetErrorOutput(), p.GetStandardOutput(), true);
        }

        return false;
    }

    private static ManagedProgram ManagedProgramFor(string exe, string arguments)
    {
        return new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, exe, arguments, false, null);
    }
}
