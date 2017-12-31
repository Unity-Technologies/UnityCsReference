// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildStep
    {
        [NativeName("stepName")]
        public string name { get; }

        internal ulong durationTicks;
        public TimeSpan duration {  get {  return new TimeSpan((long)durationTicks); } }

        public BuildStepMessage[] messages { get; }

        public int depth { get; }

        public override string ToString()
        {
            return string.Format("{0} ({1}ms)", name, duration.TotalMilliseconds);
        }
    }
}
