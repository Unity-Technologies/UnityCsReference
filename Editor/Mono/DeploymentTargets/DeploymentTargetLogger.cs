// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.DeploymentTargets
{
    internal abstract class DeploymentTargetLogger
    {
        // Starts reading log and redirects it to OnLogMessage callbacks
        internal abstract void Start();

        // Stops the logger. Can be used to implement closing process/file-stream
        internal abstract void Stop();

        // Can be used to implement clearing log sources e.g. clear ADB logcat
        internal abstract void Clear();

        // Event executed on every log message received from log source
        // First string parameter is used as log ID, e.g. it could be stdout, stderr or any other
        // For every unique ID passed to this event - a separate log file will be created
        // Second string parameter is log message
        internal event Action<string, string> logMessage;

        protected virtual void OnLogMessage(string id, string message)
        {
            logMessage?.Invoke(id, message);
        }
    }
}
