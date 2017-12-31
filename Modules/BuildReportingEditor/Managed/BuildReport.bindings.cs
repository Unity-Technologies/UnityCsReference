// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeHeader("Runtime/Utilities/DateTime.h")]
    [NativeType(Header = "Modules/BuildReportingEditor/Public/BuildReport.h")]
    [NativeClass("BuildReporting::BuildReport")]
    public sealed class BuildReport : Object
    {
        private BuildReport()
        {
        }

        public extern BuildFile[] files { get; }

        [NativeName("BuildSteps")]
        public extern BuildStep[] steps { get; }

        public extern BuildSummary summary { get; }

        public StrippingInfo strippingInfo
        {
            get { return GetAppendices<StrippingInfo>().SingleOrDefault(); }
        }

        [NativeMethod("RelocateFiles")]
        internal extern void RecordFilesMoved(string originalPathPrefix, string newPathPrefix);

        [NativeMethod("AddFile")]
        internal extern void RecordFileAdded(string path, string role);

        [NativeMethod("AddFilesRecursive")]
        internal extern void RecordFilesAddedRecursive(string rootDir, string role);

        [NativeMethod("DeleteFile")]
        internal extern void RecordFileDeleted(string path);

        [NativeMethod("DeleteFilesRecursive")]
        internal extern void RecordFilesDeletedRecursive(string rootDir);

        [FreeFunction("BuildReporting::SummarizeErrors", HasExplicitThis = true)]
        internal extern string SummarizeErrors();

        internal extern void AddMessage(LogType messageType, string message);

        internal extern int BeginBuildStep(string stepName);
        internal extern void ResumeBuildStep(int depth);
        internal extern void EndBuildStep(int depth);

        internal extern void AddAppendix([NotNull] Object obj);

        internal TAppendix[] GetAppendices<TAppendix>() where TAppendix : Object
        {
            return GetAppendices(typeof(TAppendix)).Cast<TAppendix>().ToArray();
        }

        internal extern Object[] GetAppendices([NotNull] Type type);

        internal extern Object[] GetAllAppendices();

        [FreeFunction("BuildReporting::GetLatestReport")]
        internal static extern BuildReport GetLatestReport();
    }
}
