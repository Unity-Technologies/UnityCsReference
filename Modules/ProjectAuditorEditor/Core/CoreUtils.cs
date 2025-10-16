// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
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
            if (!type.CustomAttributes.Any())
                return true;
            var analysisPlatformAttributes = type.GetCustomAttributes<AnalysisPlatformAttribute>();
            return !analysisPlatformAttributes.Any() || analysisPlatformAttributes.Any(a => a.Platform == platform);
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
