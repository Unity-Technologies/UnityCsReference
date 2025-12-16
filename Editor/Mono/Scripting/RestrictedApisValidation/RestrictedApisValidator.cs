// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[NativeHeader("Editor/Src/Scripting/RestrictedApisValidation/RestrictedApisValidator.h")]
class RestrictedApisValidator
{
    [RequiredByNativeCode]
    static RestrictedApiValidationResult[] GetRestrictedApiUsage(string[] assemblyPaths)
    {
        StackTraceLogType? previousLogType = null;
        try
        {
            bool logReferences = (bool) Debug.GetDiagnosticSwitch("LogReferencesInApiValidation").value;

            previousLogType = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            var task = GetRestrictedApiUsageAsync(assemblyPaths, Application.persistentDataPath, logReferences);
            if (!task.Wait(5000))
            {
                Console.WriteLine($"AssemblyValidation task did not finish in 5s, status: {task.Status}. Returning empty array.");
                return Array.Empty<RestrictedApiValidationResult>();
            }
            return task.Result;
        }
        finally
        {
            if (previousLogType != null)
                Application.SetStackTraceLogType(LogType.Log, previousLogType.Value);
        }
    }

    static async Task<RestrictedApiValidationResult[]> GetRestrictedApiUsageAsync(string[] assemblyPaths, string userConfigPath, bool logReferences)
    {
        var restrictedApisConfig = RestrictedApisConfig.Load(userConfigPath);
        if (restrictedApisConfig.TotalConfiguredApis == 0)
        {
            Console.WriteLine("No restricted APIs configured; Validation skipped.");
            return Array.Empty<RestrictedApiValidationResult>();
        }

        LogConfigurationMessages(restrictedApisConfig);

        List<RestrictedApiValidationResult> results = new();

        var taskPairs = RunValidation(assemblyPaths, restrictedApisConfig, logReferences);

        var tasks = new List<Task<RestrictedApiUsage[]>>(taskPairs.Keys);
        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks).ConfigureAwait(false);
            tasks.Remove(completed);
            var state = taskPairs[completed];

            if (completed.IsCompletedSuccessfully)
            {
                results.Add(new (state.AssemblyPath, (long) TimeSpan.FromTicks(Stopwatch.GetTimestamp() - state.StartTime).TotalMilliseconds , completed.Result));
            }
            else if (completed.IsFaulted)
            {
                LogError(state.AssemblyPath, completed.Exception);
            }
            else if (completed.IsCanceled)
            {
                Console.WriteLine($"Verification of `{state.AssemblyPath}` has been canceled.");
            }
        }
        return results.ToArray();

        static void LogConfigurationMessages(RestrictedApisConfig unfriendlyApisConfig)
        {
            foreach (var message in unfriendlyApisConfig.Messages)
            {
                Debug.Log(message);
            }
        }
    }

    static Dictionary<Task<RestrictedApiUsage[]>, (string AssemblyPath, long StartTime, Task<RestrictedApiUsage[]> Task)> RunValidation(string[] assemblyPaths, RestrictedApisConfig restrictedApisConfig, bool logReferences = false)
    {
        var result = new Dictionary<Task<RestrictedApiUsage[]>, (string AssemblyPath, long StartTime, Task<RestrictedApiUsage[]> Task)>(assemblyPaths.Length);
        foreach (var assemblyPath in assemblyPaths)
        {
            var tuple = (AssemblyPath: assemblyPath, Stopwatch.GetTimestamp(), Task: Task.Run(() => GetRestrictedApiUsageForAssembly(assemblyPath, restrictedApisConfig, logReferences)));
            result.Add(tuple.Task, tuple);
        }

        return result;
    }

    static RestrictedApiUsage[] GetRestrictedApiUsageForAssembly(string assemblyPath, RestrictedApisConfig restrictedApisConfig, bool logReferences)
    {
        List<RestrictedApiUsage> results = new();

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) });

        CollectRestrictedMethodReferences(results, assembly, restrictedApisConfig, logReferences);
        CollectRestrictedTypeReferences(results, assembly, restrictedApisConfig, logReferences);

        return results.ToArray();

        static void CollectRestrictedMethodReferences(List<RestrictedApiUsage> results, AssemblyDefinition assembly, RestrictedApisConfig restrictedApisConfig, bool logReferences)
        {
            foreach (var t in assembly.MainModule.GetTypes())
            {
                foreach (var m in t.Methods)
                {
                    CollectReferencesToRestrictedApi(m, restrictedApisConfig, logReferences, results);
                }
            }
        }

        static void CollectRestrictedTypeReferences(List<RestrictedApiUsage> results, AssemblyDefinition assembly, RestrictedApisConfig restrictedApisConfig, bool logReferences)
        {
            foreach (var typeReference in assembly.MainModule.GetTypeReferences())
            {
                var restrictedApiDetails = restrictedApisConfig.RestrictedApiSeverityFor(typeReference);
                if (restrictedApiDetails.Severity == RestrictedApiSeverity.Hidden)
                    continue;

                if (logReferences)
                {
                    Console.WriteLine($"[Restricted API Validation] Checking reference '{typeReference.FullName}'");
                }
                results.Add(new RestrictedApiUsage("N/A", typeReference.FullName, restrictedApiDetails.Description, restrictedApiDetails.DocumentationUrl,  restrictedApiDetails.Severity, RestrictedApiKind.Type, string.Empty));
            }
        }
    }

    static void CollectReferencesToRestrictedApi(MethodDefinition method, RestrictedApisConfig restrictedApisConfig, bool logReferences, List<RestrictedApiUsage> results)
    {
        var restrictedApiUsages = CollectRestrictApiUsagesFor(method, restrictedApisConfig, logReferences);
        results.AddRange(restrictedApiUsages);

        static IEnumerable<RestrictedApiUsage> CollectRestrictApiUsagesFor(MethodDefinition method, RestrictedApisConfig restrictedApisConfig, bool logReferences)
        {
            if (!method.HasBody)
                return Array.Empty<RestrictedApiUsage>();

            var result = new List<RestrictedApiUsage>();
            foreach (var inst in method.Body.Instructions)
            {
                if (!IsRestrictedApiMemberReferenceCandidate(inst))
                    continue;

                var apiUsage = RestrictedApiUsageFor(method, inst, restrictedApisConfig, logReferences);
                if (apiUsage.Severity != RestrictedApiSeverity.Hidden)
                    result.Add(apiUsage);
            }

            return result;
        }

        static RestrictedApiUsage RestrictedApiUsageFor(MethodDefinition declaringMethod, Instruction inst, RestrictedApisConfig restrictedApisConfig, bool logReferences)
        {
            var memberReference = (MemberReference) inst.Operand;
            if (logReferences)
            {
                Console.WriteLine($"[Unsupported API Validation] Checking reference '{memberReference.FullName}'");
            }

            var details = restrictedApisConfig.RestrictedApiSeverityFor(memberReference);
            if (details.Severity == RestrictedApiSeverity.Hidden)
                return new RestrictedApiUsage();

            TryGetDebugInformation(declaringMethod, inst, out var debugInformation);
            return new RestrictedApiUsage(
                declaringMethod.FullName,
                memberReference.FullName,
                details.Description,
                details.DocumentationUrl,
                details.Severity,
                memberReference is TypeReference ? RestrictedApiKind.Type :  RestrictedApiKind.Method,
                debugInformation);
        }

        static bool IsRestrictedApiMemberReferenceCandidate(Instruction inst) => inst.Operand is MemberReference;

        static void TryGetDebugInformation(MethodDefinition method, Instruction inst, out string debugInformation)
        {
            debugInformation = string.Empty;
            var sequencePoint = method.DebugInformation?.GetSequencePoint(inst);
            if (sequencePoint != null)
            {
                debugInformation = $"({sequencePoint.Document.Url} {sequencePoint.StartLine},{sequencePoint.StartColumn})";
            }
        }
    }

    static void LogError(string assemblyPath, Exception exception)
    {
        switch (exception)
        {
            case AggregateException { InnerException: not null } aggregateException:
                LogError(assemblyPath, aggregateException.InnerException);
                break;

            case BadImageFormatException:
                Debug.LogWarning($"Unable to check assembly {assemblyPath} for restricted APIs. BadImageFormatException caught; this usually means a corrupted binary (dll) or the associated debug information (.pdb):\n{Environment.NewLine}{exception}");
                break;

            default:
                Debug.LogError($"Unable to check assembly {assemblyPath} for restricted APIs. Exception caught:{Environment.NewLine}{exception}");
                break;
        }
    }
}
