// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditor.Search
{
    static class SearchInit
    {
        [InitializeOnLoadMethod]
        internal static void ScheduleIndexationOnStartup()
        {
            EditorApplication.wantsToQuit -= UnityQuit;
            EditorApplication.wantsToQuit += UnityQuit;

            // IndexingOnEditorStartup_Started is Used to signal that we already have started indexing and that further domain reload shouldn't restart it.
            // Do not run indexing on startup if we are in a secondary process.
            if (!Utils.IsMainProcess() || EditorUtility.isInSafeMode || !SearchSettings.indexOnEditorStartup || SessionState.GetBool("IndexingOnEditorStartup_Started", false))
            {
                return;
            }

            EditorApplication.delayCall -= IndexationOnStartup;
            EditorApplication.delayCall += IndexationOnStartup;
        }

        struct MarkerData
        {
            public string name;
            public double avgTime;
            public int sampleCount;
            public double totalTime;
        }

        static void LogPerf(StringBuilder builder, string lineMsg)
        {
            builder.AppendLine(lineMsg);
        }

        internal static bool UnityQuit()
        {
            if (SearchSettings.logIndexingPerformanceReport)
            {
                var perTypeMarkers = new List<MarkerData>();
                var genericMarkers = new List<MarkerData>();

                var reportDir = "Logs/IndexingPerformanceReport";
                if (!Directory.Exists(reportDir))
                {
                    Directory.CreateDirectory(reportDir);
                }

                var report = new StringBuilder();
                LogPerf(report, $"[Indexing Performance] All time units in seconds");
                var trackerNames = EditorPerformanceTracker.GetAvailableTrackers();
                foreach (var trackerName in trackerNames)
                {
                    if (trackerName.StartsWith("SearchImporter") || trackerName.StartsWith("NativeImporter"))
                    {
                        var sampleCount = EditorPerformanceTracker.GetSampleCount(trackerName);
                        var totalTime = EditorPerformanceTracker.GetTotalTime(trackerName);
                        var avgTime = EditorPerformanceTracker.GetAverageTime(trackerName);

                        var markerData = new MarkerData()
                        {
                            name = trackerName,
                            avgTime = avgTime,
                            sampleCount = sampleCount,
                            totalTime = totalTime
                        };
                        if (trackerName.StartsWith("SearchImporter.per_ext"))
                        {
                            perTypeMarkers.Add(markerData);
                        }
                        else
                        {
                            genericMarkers.Add(markerData);
                        }
                    }
                }

                foreach (var marker in genericMarkers)
                {
                    LogPerf(report, $"{marker.name} samples:{marker.sampleCount} totalTime:{marker.totalTime} avgTime:{marker.avgTime}");
                }

                foreach (var marker in perTypeMarkers)
                {
                    LogPerf(report, $"{marker.name} samples:{marker.sampleCount} totalTime:{marker.totalTime} avgTime:{marker.avgTime}");
                }

                var projectName = Path.GetFileName(Application.dataPath.Replace("/Assets", ""));
                var dateTime = DateTime.Now;
                var fileName = $"{reportDir}/{projectName}{dateTime:yyyy-MM-dd_HH-mm-ss}.log";
                File.WriteAllText(fileName, report.ToString());
            }
            return true;
        }

        internal static void IndexationOnStartup()
        {
            // Acessing the default DB will import it and start Indexing.
            var db = SearchDatabase.GetDefaultSearchDatabase();

            // IndexingOnEditorStartup_Started is Used to signal that we already have started indexing and that further domain reload shouldn't restart it.
            SessionState.SetBool("IndexingOnEditorStartup_Started", true);
            Console.WriteLine($"[Indexing] Starting Initial Indexing for {db.name}");
        }
    }
}
