// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildLogReader
    {
        BuildLogMessages Read(string logPath);
    }

    /// <summary>
    /// Reads build messages from the BuildLog.jsonl the build pipeline writes into the build's metadata folder.
    /// </summary>
    internal sealed class BuildLogReader : IBuildLogReader
    {
        static readonly ProfilerMarker s_ReadMarker = new ProfilerMarker("BuildLogReader.Read");

        public BuildLogMessages Read(string logPath)
        {
            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
                return BuildLogMessages.Missing;

            using (s_ReadMarker.Auto())
            {
                try
                {
                    return Parse(File.ReadLines(logPath));
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    return BuildLogMessages.Missing;
                }
            }
        }

        internal static BuildLogMessages Parse(IEnumerable<string> lines)
        {
            return new Parser().Run(lines);
        }

        // Field names must match BuildLog.jsonl
        [Serializable]
        private sealed class Record
        {
            public string timestamp;
            public string log_level;
            public string message;
        }

        [Serializable]
        private sealed class StepRecord
        {
            public string type;
            public string stepName;
            public int depth;
        }

        private readonly struct OpenStep
        {
            public readonly int StepIndex;
            public readonly int Depth;

            public OpenStep(int stepIndex, int depth)
            {
                StepIndex = stepIndex;
                Depth = depth;
            }
        }

        private sealed class Parser
        {
            // The build pipeline writes structured events into the same stream as log messages
            // (BuildLogMessages.cpp): step boundaries, plus BuildStart and BuildStatistic. They are
            // machine telemetry, not messages for a user, so none of them reach the console.
            private const string k_StructuredEventPrefix = "{\"type\":\"";
            private const string k_StepRecordPrefix = "{\"type\":\"Step";
            private const string k_StepStart = "StepStart";

            private readonly Record m_Record = new Record();
            private readonly StepRecord m_StepRecord = new StepRecord();

            private readonly List<BuildLogMessage> m_Messages = new List<BuildLogMessage>();
            private readonly List<string> m_StepNames = new List<string>();
            private readonly Dictionary<string, int> m_StepIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly List<int> m_StepWarnings = new List<int>();
            private readonly List<int> m_StepErrors = new List<int>();

            // Open steps, innermost last. Each entry keeps the depth the log reported, because stack
            // position alone is unreliable: a build opens its root step before BuildLog exists, so the
            // log's first StepStart can arrive at a depth deeper than the number of steps seen.
            private readonly List<OpenStep> m_OpenSteps = new List<OpenStep>();

            private readonly Dictionary<string, string> m_TextPool = new Dictionary<string, string>(StringComparer.Ordinal);

            private int m_ErrorCount;
            private int m_WarningCount;
            private int m_InfoCount;
            private bool m_Malformed;

            public BuildLogMessages Run(IEnumerable<string> lines)
            {
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!TryReadRecord(line))
                        continue;

                    if (m_Record.message.StartsWith(k_StepRecordPrefix, StringComparison.Ordinal))
                        ApplyStepRecord(m_Record.message);
                    else if (!m_Record.message.StartsWith(k_StructuredEventPrefix, StringComparison.Ordinal))
                        AddMessage();
                }

                var stepCounts = new BuildLogStepCounts[m_StepNames.Count];
                for (var i = 0; i < stepCounts.Length; i++)
                    stepCounts[i] = new BuildLogStepCounts(m_StepWarnings[i], m_StepErrors[i]);

                return new BuildLogMessages(
                    m_Messages.ToArray(),
                    m_StepNames.ToArray(),
                    stepCounts,
                    m_ErrorCount,
                    m_WarningCount,
                    m_InfoCount,
                    m_Malformed ? BuildLogStatus.PartiallyRead : BuildLogStatus.Complete);
            }

            private bool TryReadRecord(string line)
            {
                m_Record.timestamp = null;
                m_Record.log_level = null;
                m_Record.message = null;

                try
                {
                    JsonUtility.FromJsonOverwrite(line, m_Record);
                }
                catch (ArgumentException)
                {
                    m_Malformed = true;
                    return false;
                }

                if (m_Record.message == null)
                {
                    m_Malformed = true;
                    return false;
                }

                return true;
            }

            private void ApplyStepRecord(string json)
            {
                m_StepRecord.type = null;
                m_StepRecord.stepName = null;
                m_StepRecord.depth = 0;

                try
                {
                    JsonUtility.FromJsonOverwrite(json, m_StepRecord);
                }
                catch (ArgumentException)
                {
                    m_Malformed = true;
                    return;
                }

                if (string.IsNullOrEmpty(m_StepRecord.stepName))
                {
                    m_Malformed = true;
                    return;
                }

                // Both a start and an end at depth d close everything at or below d: the step itself for
                // an end, and any sibling left open by a lost StepEnd for a start.
                CloseStepsAtOrBelow(m_StepRecord.depth);

                if (string.Equals(m_StepRecord.type, k_StepStart, StringComparison.Ordinal))
                    m_OpenSteps.Add(new OpenStep(ResolveStepIndex(m_StepRecord.stepName), m_StepRecord.depth));
            }

            private void CloseStepsAtOrBelow(int depth)
            {
                var keep = m_OpenSteps.Count;
                while (keep > 0 && m_OpenSteps[keep - 1].Depth >= depth)
                    keep--;

                if (keep < m_OpenSteps.Count)
                    m_OpenSteps.RemoveRange(keep, m_OpenSteps.Count - keep);
            }

            private int ResolveStepIndex(string stepName)
            {
                if (m_StepIndexByName.TryGetValue(stepName, out var index))
                    return index;

                index = m_StepNames.Count;
                m_StepNames.Add(stepName);
                m_StepWarnings.Add(0);
                m_StepErrors.Add(0);
                m_StepIndexByName.Add(stepName, index);
                return index;
            }

            private void AddMessage()
            {
                var severity = ToSeverity(m_Record.log_level);
                var stepIndex = m_OpenSteps.Count > 0 ? m_OpenSteps[m_OpenSteps.Count - 1].StepIndex : -1;

                switch (severity)
                {
                    case BuildLogSeverity.Error:
                        m_ErrorCount++;
                        if (stepIndex >= 0)
                            m_StepErrors[stepIndex]++;
                        break;

                    case BuildLogSeverity.Warning:
                        m_WarningCount++;
                        if (stepIndex >= 0)
                            m_StepWarnings[stepIndex]++;
                        break;

                    default:
                        m_InfoCount++;
                        break;
                }

                m_Messages.Add(new BuildLogMessage(
                    severity,
                    stepIndex,
                    Intern(m_TextPool, m_Record.message),
                    ToTicks(m_Record.timestamp)));
            }

            private static string Intern(Dictionary<string, string> pool, string value)
            {
                if (value == null)
                    return null;

                if (pool.TryGetValue(value, out var existing))
                    return existing;

                pool.Add(value, value);
                return value;
            }

            private static BuildLogSeverity ToSeverity(string logLevel)
            {
                switch (logLevel)
                {
                    case "Error":
                    case "Fatal":
                        return BuildLogSeverity.Error;

                    case "Warning":
                        return BuildLogSeverity.Warning;

                    // Info, Debug, Trace and anything unrecognised.
                    default:
                        return BuildLogSeverity.Info;
                }
            }

            private static long ToTicks(string timestamp)
            {
                return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out var parsed) ? parsed.Ticks : 0;
            }
        }
    }
}
