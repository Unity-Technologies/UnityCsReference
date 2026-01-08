// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.BuildService;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class UnityMSBuildLogger
{
    public static void LogProjectBuildManagerMessages(BuildResultMessage buildResultMessage)
    {
        Console.WriteLine($"[PBM]: {buildResultMessage.GetPbmLogMessages().Count} messages");
        foreach (var message in buildResultMessage.GetPbmLogMessages())
        {
            Console.WriteLine("[PBM]: " + message);
        }
    }
    public static void LogCompilerMessages(BuildResultMessage buildResultMessage, bool buildingForEditor)
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
                if (logMessage.MessageType == LogMessageType.Message)
                {
                    continue;
                }

                var instanceId = LookupInstanceId(fileInstanceIdCache, logMessage.File);
                Debug.LogCompilerMessage(FormatLogMessageToString(logMessage), logMessage.File, logMessage.LineNumber, logMessage.ColumnNumber,
                                       buildingForEditor, logMessage.MessageType == LogMessageType.Error, logIdentifier, instanceId);
            }
        }

        foreach (var logMessage in buildResultMessage.GetLogMessages())
        {
            if (logMessage.MessageType == LogMessageType.Message)
            {
                continue;
            }

            var instanceId = LookupInstanceId(fileInstanceIdCache, logMessage.File);
            Debug.LogCompilerMessage(FormatLogMessageToString(logMessage), logMessage.File, logMessage.LineNumber, logMessage.ColumnNumber,
                                   buildingForEditor, logMessage.MessageType == LogMessageType.Error, logIdentifier, instanceId);
        }
    }

    private static string FormatLogMessageToString(LogMessage logMessage)
    {
        return $@"{logMessage.MessageType} {logMessage.Code}({logMessage.LineNumber},{logMessage.ColumnNumber}): {logMessage.File}
{logMessage.Message}";
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
