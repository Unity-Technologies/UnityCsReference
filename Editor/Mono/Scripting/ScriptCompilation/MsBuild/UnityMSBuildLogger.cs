// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.BuildService;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class UnityMSBuildLogger
{
    // Must match Unity.BuildService.ProjectCache.ProjectCacheSummary.LogCode.
    const string k_ProjectCacheSummaryLogCode = "UnityProjectCacheSummary";
    // Must match Unity.BuildService.Host.ProjectModel.InMemoryBuildManagerLogger.CscInvokedCode.
    const string k_CscInvokedLogCode = "UnityCscInvoked";
    // The solution root project; it compiles no scripts of its own.
    const string k_EntryPointProjectSuffix = ".EntryPoint.csproj";

    public static void LogBuildFailedWithoutResult(Exception exception, TimeSpan elapsed, string lastProgressText, string configuration, bool generateBinLog)
    {
        var binLog = generateBinLog
            ? GetPublishBinLogPath(configuration)
            : "not enabled (Preferences > Diagnostics > ScriptCompilationMsBuildBinlog, or -generate-binlog)";

        // A faulted Task wraps everything in an AggregateException; the inner one explains the failure.
        var cause = exception is AggregateException aggregate && aggregate.InnerExceptions.Count == 1
            ? aggregate.InnerExceptions[0]
            : exception;

        Debug.LogError($@"Script compilation failed: the MSBuild build host did not report a build result.

  Ran for {elapsed:hh\:mm\:ss}, last activity: {lastProgressText}
  Build host logs: {Path.Combine(Path.GetTempPath(), "Unity.BuildService.Host")}
  MSBuild binary log: {binLog}

{cause}");
    }

    // Mirrors BuildManagerParameters, which names the log "{buildPhase}-{configuration}.binlog"
    // under <project>/Logs. Publish is the phase that produces the assemblies.
    internal static string GetPublishBinLogPath(string configuration) =>
        Path.GetFullPath(Path.Combine("Logs", $"Publish-{configuration}.binlog"));

    public static void LogProjectBuildManagerMessages(BuildResultMessage buildResultMessage)
    {
        Console.WriteLine($"[PBM]: {buildResultMessage.GetPbmLogMessages().Count} messages");
        foreach (var message in buildResultMessage.GetPbmLogMessages())
        {
            Console.WriteLine("[PBM]: " + message);
        }
    }

    // stdout, so it reaches Editor.log without a Console entry on every compile. The Csc lines match the ones Bee
    // prints per compiled assembly, which tests and tools count.
    public static void LogBuildSummary(BuildResultMessage buildResultMessage)
    {
        var projectResults = buildResultMessage.GetProjectResults();
        foreach (var logMessage in buildResultMessage.GetLogMessages())
        {
            if (logMessage.MessageType != LogMessageType.Message)
                continue;

            if (logMessage.Code == k_ProjectCacheSummaryLogCode)
                Console.WriteLine(logMessage.Message);
            else if (logMessage.Code == k_CscInvokedLogCode
                     && !string.IsNullOrEmpty(logMessage.File)
                     && !logMessage.File.EndsWith(k_EntryPointProjectSuffix, StringComparison.OrdinalIgnoreCase)
                     && projectResults.TryGetValue(logMessage.File, out var projectResult)
                     && !string.IsNullOrEmpty(projectResult.AssemblyName))
                Console.WriteLine($"Csc {projectResult.AssemblyName}.dll");
        }
    }

    public static void LogCompilerMessages(BuildResultMessage buildResultMessage, bool buildingForEditor, IReadOnlyList<LogMessage> scriptUpdaterMessages = null)
    {
        const int kLogIdentifierFor_EditorMessages = 1234;
        const int kLogIdentifierFor_PlayerMessages = 1235;
        int logIdentifier = buildingForEditor ? kLogIdentifierFor_EditorMessages : kLogIdentifierFor_PlayerMessages;
        Debug.RemoveLogEntriesByIdentifier(logIdentifier);

        var fileInstanceIdCache = new Dictionary<string, EntityId>();

        foreach (var projectResults in buildResultMessage.GetProjectResults())
        {
            foreach (LogMessage logMessage in projectResults.Value.GetLogMessages())
            {
                if (logMessage.MessageType == LogMessageType.Message || IsScriptUpdaterManifestPointer(logMessage))
                {
                    continue;
                }

                var file = GetReportedFilePath(logMessage.File, projectResults.Key);
                var instanceId = LookupInstanceId(fileInstanceIdCache, file);
                Debug.LogCompilerMessage(FormatLogMessageToString(logMessage, file), file, logMessage.LineNumber, logMessage.ColumnNumber,
                                       buildingForEditor, logMessage.MessageType == LogMessageType.Error, logIdentifier, instanceId);
            }
        }

        LogBuildSummary(buildResultMessage);

        foreach (var logMessage in buildResultMessage.GetLogMessages())
        {
            if (logMessage.MessageType == LogMessageType.Message)
                continue;

            if (IsScriptUpdaterManifestPointer(logMessage))
                continue;

            var instanceId = LookupInstanceId(fileInstanceIdCache, logMessage.File);
            Debug.LogCompilerMessage(FormatLogMessageToString(logMessage, logMessage.File), logMessage.File, logMessage.LineNumber, logMessage.ColumnNumber,
                                   buildingForEditor, logMessage.MessageType == LogMessageType.Error, logIdentifier, instanceId);
        }

        // Not tied to a source location, so logged as plain text rather than in compiler message form.
        foreach (var logMessage in scriptUpdaterMessages ?? Array.Empty<LogMessage>())
        {
            Debug.LogCompilerMessage(logMessage.Message, string.Empty, 0, 0,
                buildingForEditor, logMessage.MessageType == LogMessageType.Error, logIdentifier, EntityId.None);
        }
    }

    // Tells the Editor where the script updater wrote its updated files; the updater's own messages are logged instead.
    private static bool IsScriptUpdaterManifestPointer(LogMessage logMessage) =>
        logMessage.Code == MsBuildCompilation.k_PendingScriptUpdatesWarningCode;

    private static string FormatLogMessageToString(LogMessage logMessage, string file)
    {
        // Match the Roslyn/legacy pipeline shape parsed by PostProcessorOutputParser and
        // expected by test regexes like Regex(".*error.*") (case-sensitive):
        // <file>(<line>,<col>): error|warning <code>: <message>
        var severity = logMessage.MessageType == LogMessageType.Error ? "error" : "warning";
        return $"{file}({logMessage.LineNumber},{logMessage.ColumnNumber}): {severity} {logMessage.Code}: {logMessage.Message}";
    }

    // The .NET SDK compiles with GenerateFullPaths, so csc roots every diagnostic path at the project folder, also an
    // analyzer location that is not a file, e.g. "ReportAdditionalFiles". Report such a path as the analyzer gave it.
    internal static string GetReportedFilePath(string file, string projectFile)
    {
        if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(projectFile) || !Path.IsPathRooted(file) || File.Exists(file) || Directory.Exists(file))
            return file;

        var projectDirectory = Path.GetDirectoryName(projectFile);
        if (string.IsNullOrEmpty(projectDirectory))
            return file;

        var relativePath = Path.GetRelativePath(projectDirectory, file);
        if (Path.IsPathRooted(relativePath) || relativePath == ".." || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return file;

        return relativePath;
    }

    private static EntityId LookupInstanceId(IDictionary<string, EntityId> fileInstanceIdCache, string filePath)
    {
        // in batch mode, we don't have a Console Window, so we don't need an instance id
        if (Application.isBatchMode || string.IsNullOrEmpty(filePath))
        {
            return EntityId.None;
        }

        if (fileInstanceIdCache.TryGetValue(filePath, out var instanceId))
        {
            return instanceId;
        }

        var relativepath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);

        // The AssetDatabase does not expect absolute paths. In this case, we
        // try to get the Logical path for the supplied filePath and pass that along
        var logicalFilePath = FileUtil.GetLogicalPath(relativepath);
        if (string.IsNullOrEmpty(logicalFilePath))
        {
            return EntityId.None;
        }

        var guid = AssetDatabase.GUIDFromAssetPath(logicalFilePath);
        if (guid.Empty())
        {
            return EntityId.None;
        }

        // script compilation errors can happen before the asset database is initialized, so we reserve the instance id ahead of time (it is deterministic)
        instanceId = AssetDatabase.ReserveMonoScriptEntityId(guid);

        fileInstanceIdCache.Add(filePath, instanceId);

        return instanceId;
    }
}
