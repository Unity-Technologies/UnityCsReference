// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    static class ConsoleWindowHelper
    {
        const string k_LogIdentifier = "GraphToolkit";

        public static void LogSticky(string message, string file, LogType logType, LogOption logOptions, int instanceId, string windowId)
        {
            EditorBridge.AddMessageWithDoubleClickCallback(message, file, logType, logOptions, instanceId, (k_LogIdentifier + windowId).GetHashCode());
        }


        public static void RemoveLogEntries(string windowId)
        {
            EngineBridge.RemoveLogEntriesByIdentifier((k_LogIdentifier + windowId).GetHashCode());
        }

        public static void ShowConsoleWindow(bool immediate = true)
        {
            EditorBridge.ShowConsoleWindow(immediate);
        }
    }
}
