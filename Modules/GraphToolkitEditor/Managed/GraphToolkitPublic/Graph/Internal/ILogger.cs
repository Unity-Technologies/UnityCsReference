// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    interface ILogger
    {
        void LogError(object message, object context = null);
        void LogWarning(object message, object context = null);
        void Log(object message, object context = null);
    }
}
