// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains information about a single log message recorded during the build process.
    ///
    ///</summary>
    ///<seealso cref="Build.Reporting.BuildStep.messages" />
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildStepMessage
    {
        ///<summary>The <see cref="LogType" /> of the log message.</summary>
        public LogType type { get; }
        ///<summary>The text content of the log message.</summary>
        public string content { get; }
    }
}
