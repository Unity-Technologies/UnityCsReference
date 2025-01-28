// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains information about a single step in the build process.
    ///
    ///</summary>
    ///<seealso cref="Build.Reporting.BuildReport.steps" />
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildStep
    {
        ///<summary>The name of this build step.</summary>
        [NativeName("stepName")]
        public string name { get; }

        internal ulong durationTicks;
        ///<summary>The total duration for this build step.</summary>
        public TimeSpan duration {  get {  return new TimeSpan((long)durationTicks); } }

        ///<summary>All log messages recorded during this build step, in the order of which they occurred.</summary>
        ///<returns>An array of <see cref="BuildStepMessage" /> structs.</returns>
        public BuildStepMessage[] messages { get; }

        ///<summary>The nesting depth of the build step.</summary>
        ///<remarks>The build process is broken down into steps, and steps may themselves be broken down into sub-steps recursively. The nesting depth indicates how many higher-level build steps enclose this step. The step that represents the overall build process has depth 0, the sub-steps of that step have depth 1, and so on.</remarks>
        public int depth { get; }

        public override string ToString()
        {
            return UnityString.Format("{0} ({1}ms)", name, duration.TotalMilliseconds);
        }
    }
}
