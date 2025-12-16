// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileListModelBuilder
    {
        const string k_SessionNameTemplate = "Session {0}";
        IReadOnlyList<CaptureFileModel> m_AllCaptures;
        DateTime m_CaptureDirectoryLastWriteTimestampUtc;

        public CaptureFileListModelBuilder(IReadOnlyList<CaptureFileModel> allCaptures, DateTime CaptureDirectoryLastWriteTimestampUtc)
        {
            m_AllCaptures = allCaptures;
            m_CaptureDirectoryLastWriteTimestampUtc = CaptureDirectoryLastWriteTimestampUtc;
        }

        /// <summary>
        /// Maps all sessions to the unique SessionId and generates session names
        /// </summary>
        /// <returns></returns>
        public CaptureFileListModel Build()
        {
            var sessionNames = new Dictionary<uint, string>();

            if (MakeSortedSessionsListIds(m_AllCaptures, out var sortedSessionIds, out var sessionsMap))
            {
                // Make session name based on the sorted order
                foreach (var sessionId in sortedSessionIds)
                {
                    var sessionName = $"{sessionId}".Insert(6, "-").Insert(4, "-");
                    sessionNames[sessionId] = sessionName;
                }
            }

            return new CaptureFileListModel(
                m_CaptureDirectoryLastWriteTimestampUtc,
                sessionNames,
                m_AllCaptures,
                sortedSessionIds,
                sessionsMap);
        }


        /// <summary>
        /// A utility function that makes a sorted list of Captures sessions and dictionary of sorted list of Captures inside each session
        /// </summary>
        /// <param name="captures">List of all Captures to process</param>
        /// <param name="outSortedSessionIds">Returned list of sorted sessions</param>
        /// <param name="outSessionsMap">Returned dictionary of lists for each session id</param>
        /// <returns>True if successful</returns>
        static bool MakeSortedSessionsListIds(in IReadOnlyList<CaptureFileModel> captures, out List<uint> outSortedSessionIds, out Dictionary<uint, List<CaptureFileModel>> outSessionsMap)
        {
            if (captures.Count <= 0)
            {
                outSortedSessionIds = null;
                outSessionsMap = null;
                return false;
            }

            // Pre-sort Captures
            var sortedCaptures = new List<CaptureFileModel>(captures);
            sortedCaptures.Sort((l, r) => l.Timestamp.CompareTo(r.Timestamp));

            // Group Captures by sessionId
            var sessionsMap = new Dictionary<uint, List<CaptureFileModel>>();
            var sortedSessionIds = new List<uint>();
            foreach (var captureFileModel in sortedCaptures)
            {
                if (!sessionsMap.ContainsKey(captureFileModel.DateUsedAsGroupingId))
                {
                    sessionsMap.Add(captureFileModel.DateUsedAsGroupingId, new List<CaptureFileModel>());
                    sortedSessionIds.Add(captureFileModel.DateUsedAsGroupingId);
                }

                sessionsMap[captureFileModel.DateUsedAsGroupingId].Add(captureFileModel);
            }

            // Sort sessionId list so that generated names order is the same as visual order in UI
            sortedSessionIds.Sort((l, r) => sessionsMap[l][0].Timestamp.CompareTo(sessionsMap[r][0].Timestamp));

            outSessionsMap = sessionsMap;
            outSortedSessionIds = sortedSessionIds;
            return true;
        }
    }
}
