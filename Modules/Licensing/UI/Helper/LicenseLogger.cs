// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Helper;
using UnityEngine;

namespace UnityEditor.Licensing.UI
{
class LicenseLogger : ILicenseLogger
{
    const string k_Tag = "License";
    const string k_TagFormat = "{0}: {1}";

    public void DebugLogNoStackTrace(string message, LogType logType, string tag = k_Tag)
    {
        Debug.LogFormat(logType, LogOption.NoStacktrace, null, k_TagFormat, tag, message);
    }

    public void LogError(string message)
    {
        Debug.LogError(message);
    }
}
}
