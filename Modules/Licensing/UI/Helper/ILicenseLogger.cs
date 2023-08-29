// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Licensing.UI.Helper
{
interface ILicenseLogger
{
    public void DebugLogNoStackTrace(string message, LogType logType = LogType.Log, string tag = "License");

    public void LogError(string message);
}
}
