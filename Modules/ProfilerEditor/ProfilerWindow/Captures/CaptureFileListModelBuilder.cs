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
                uint generatedSessionId = 1;
                foreach (var sessionId in sortedSessionIds)
                {
                    var sessionName = $"{sessionId}".Insert(6, "-").Insert(4, "-");
                    sessionNames[sessionId] = sessionName;
                    generatedSessionId++;
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
        /// <param name="Captures">List of all Captures to process</param>
        /// <param name="sortedSessionIds">Returned list of sorted sessions</param>
        /// <param name="sessionsMap">Returned dictionary of lists for each session id</param>
        /// <returns>True if successful</returns>
        static bool MakeSortedSessionsListIds(in IReadOnlyList<CaptureFileModel> Captures, out List<uint> sortedSessionIds, out Dictionary<uint, List<CaptureFileModel>> sessionsMap)
        {
            if (Captures.Count <= 0)
            {
                sortedSessionIds = null;
                sessionsMap = null;
                return false;
            }

            // Pre-sort Captures
            var sortedCaptures = new List<CaptureFileModel>(Captures);
            sortedCaptures.Sort((l, r) => l.Timestamp.CompareTo(r.Timestamp));

            // Group Captures by sessionId
            var _sessionsMap = new Dictionary<uint, List<CaptureFileModel>>();
            var _sortedSessionIds = new List<uint>();
            foreach (var catpureFileModel in sortedCaptures)
            {
                if (!_sessionsMap.ContainsKey(catpureFileModel.SessionId))
                {
                    _sessionsMap.Add(catpureFileModel.SessionId, new List<CaptureFileModel>());
                    _sortedSessionIds.Add(catpureFileModel.SessionId);
                }

                _sessionsMap[catpureFileModel.SessionId].Add(catpureFileModel);
            }

            // Sort sessionId list so that generated names order is the same as visual order in UI
            _sortedSessionIds.Sort((l, r) => _sessionsMap[l][0].Timestamp.CompareTo(_sessionsMap[r][0].Timestamp));

            sessionsMap = _sessionsMap;
            sortedSessionIds = _sortedSessionIds;
            return true;
        }
    }
}
