// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Build.Analysis
{
    internal enum BuildLogSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal enum BuildLogStatus
    {
        Complete,
        FileMissing,
        PartiallyRead,
    }

    internal readonly struct BuildLogStepCounts
    {
        public readonly int Warnings;
        public readonly int Errors;

        public BuildLogStepCounts(int warnings, int errors)
        {
            Warnings = warnings;
            Errors = errors;
        }
    }

    internal readonly struct BuildLogMessage
    {
        public readonly BuildLogSeverity Severity;

        // Index into BuildLogMessages.StepNames, or -1 when the message arrived outside any build step.
        public readonly int StepIndex;

        public readonly string Text;
        public readonly long TimestampTicks;

        public BuildLogMessage(BuildLogSeverity severity, int stepIndex, string text, long timestampTicks)
        {
            Severity = severity;
            StepIndex = stepIndex;
            Text = text;
            TimestampTicks = timestampTicks;
        }
    }

    /// <summary>
    /// Build log messages for one build, read from BuildLog.jsonl. Runtime-only: never serialized into
    /// BuildAnalysis.json, and the single source of truth for the message counts the UI displays.
    /// </summary>
    internal sealed class BuildLogMessages
    {
        [NoAutoStaticsCleanup]
        public static readonly BuildLogMessages Missing = new BuildLogMessages(
            Array.Empty<BuildLogMessage>(),
            Array.Empty<string>(),
            Array.Empty<BuildLogStepCounts>(),
            0, 0, 0,
            BuildLogStatus.FileMissing);

        public readonly BuildLogMessage[] Messages;

        // Steps in first-seen order, reconstructed from the log's StepStart/StepEnd records. This is the
        // log's own step list, not an index into BuildAnalysis.Tables.Steps — map by name to cross over.
        public readonly string[] StepNames;

        public readonly BuildLogStepCounts[] StepCounts;

        public readonly int ErrorCount;
        public readonly int WarningCount;
        public readonly int InfoCount;

        public readonly BuildLogStatus Status;

        public BuildLogMessages(
            BuildLogMessage[] messages,
            string[] stepNames,
            BuildLogStepCounts[] stepCounts,
            int errorCount,
            int warningCount,
            int infoCount,
            BuildLogStatus status)
        {
            Messages = messages;
            StepNames = stepNames;
            StepCounts = stepCounts;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            InfoCount = infoCount;
            Status = status;
        }
    }
}
