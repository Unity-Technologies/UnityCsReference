// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileListModel : IEquatable<CaptureFileListModel>
    {
        public CaptureFileListModel(
            DateTime captureDirectoryLastWriteTimestampUtc,
            IReadOnlyDictionary<uint, string> sessionNames,
            IReadOnlyList<CaptureFileModel> allCaptures,
            IReadOnlyList<uint> sortedSessionIds,
            IReadOnlyDictionary<uint, List<CaptureFileModel>> sessionsMap)
        {
            CaptureDirectoryLastWriteTimestampUtc = captureDirectoryLastWriteTimestampUtc;
            SessionNames = sessionNames;
            AllCaptures = allCaptures;
            SortedSessionIds = sortedSessionIds;
            SessionsMap = sessionsMap;
        }

        public DateTime CaptureDirectoryLastWriteTimestampUtc { get; private set; }
        public IReadOnlyDictionary<uint, string> SessionNames { get; }
        public IReadOnlyList<CaptureFileModel> AllCaptures { get; }
        public IReadOnlyList<uint> SortedSessionIds { get; }
        public IReadOnlyDictionary<uint, List<CaptureFileModel>> SessionsMap { get; }

        /// <summary>
        /// Updates the timestamp. Only call this if a new build of the model is equal to the old one, except for the timestamp.
        /// </summary>
        /// <param name="newTimestamp"></param>
        public void UpdateTimeStamp(DateTime newTimestamp)
        {
            CaptureDirectoryLastWriteTimestampUtc = newTimestamp;
        }

        public bool Equals(CaptureFileListModel other)
        {
            // purposefully ignoring CaptureDirectoryTimestampUtc
            // the timestamp is irrelevant for the comparison as the content might be the same regardless of the timestamp

            if (ReferenceEquals(this, other))
                return true;
            if (other is null
                || AllCaptures.Count != other.AllCaptures.Count
                || SessionNames.Count != other.SessionNames.Count
                || (SessionsMap?.Count ?? -1) != (other.SessionsMap?.Count ?? -1)
                || (SortedSessionIds?.Count ?? -1) != (other.SortedSessionIds?.Count ?? -1))
                return false;

            // Go over all sorted session ids and compare the Captures in each session to make sure they are the same
            for (int i = 0; i < SortedSessionIds?.Count; ++i)
            {
                var sessionId = SortedSessionIds[i];

                if (sessionId != other.SortedSessionIds[i]
                    || sessionId != other.SortedSessionIds[i]
                    || SessionNames[sessionId] != other.SessionNames[sessionId])
                    return false;
                var capturesInThisSession = SessionsMap[sessionId].GetEnumerator();
                var othersCapturesInThisSession = other.SessionsMap[sessionId].GetEnumerator();
                while (capturesInThisSession.MoveNext())
                {
                    // Make sure the other session has the same Capture
                    if (!othersCapturesInThisSession.MoveNext()) return false;
                    if (!capturesInThisSession.Current.Equals(othersCapturesInThisSession.Current)) return false;
                }
                // Make sure the other session doesn't have more Captures
                if (othersCapturesInThisSession.MoveNext()) return false;
            }
            return true;
        }
    }
}
