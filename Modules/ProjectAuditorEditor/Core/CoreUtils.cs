// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class CoreUtils
    {
        // We look at the Type (passed in as type) to see what attributes it has
        // If it does not have any AnalysisPlatformAttributes then it is considered to support all platforms
        // Otherwise we check to see if the attributes match the currently selected platform (passed in as platform)
        public static bool SupportsPlatform(Type type, BuildTarget platform)
        {
            var analysisPlatformAttributes = Attribute.GetCustomAttributes(type);
            return analysisPlatformAttributes.Length == 0 || Array.Exists(analysisPlatformAttributes, a => ((AnalysisPlatformAttribute)a).Platform == platform);
        }

        public static Severity LogTypeToSeverity(LogType logType)
        {
            switch (logType)
            {
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    return Severity.Error;
                case LogType.Warning:
                    return Severity.Warning;
                default:
                    return Severity.Info;
            }
        }
    }
}
