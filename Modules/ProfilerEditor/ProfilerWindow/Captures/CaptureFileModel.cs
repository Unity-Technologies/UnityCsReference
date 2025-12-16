// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileModel : IEquatable<CaptureFileModel>
    {
        public CaptureFileModel(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            FullPath = filePath;

            var highlights = BottlenecksChartViewModelBuilder.BuildFromFile(filePath);
            var dateUsedAsGroupingId = 0;
            HighlightsFileVersion = highlights?.HighlightsFileVersion ?? -1;

            if (highlights == null || highlights.HighlightsFileVersion < 2)
            {
                var creationTime = File.GetLastWriteTime(filePath);
                dateUsedAsGroupingId = creationTime.Year * 10000 + creationTime.Month * 100 + creationTime.Day;

                ProductName = "";
                DateUsedAsGroupingId = (uint)dateUsedAsGroupingId;
                SessionId = 0;
                Timestamp = creationTime;
                Platform = (RuntimePlatform)(-1);
                UnityVersion = "";
                GraphicsDeviceType = (GraphicsDeviceType)(-1);
            }
            else
            {
                // If the capture isn't new enough to have the date, fall back to file creation time
                var timeStamp = highlights.TimeOfRecordingNative == 0 ?
                    File.GetLastWriteTime(filePath) : highlights.DateTimeOfRecording;
                dateUsedAsGroupingId = timeStamp.Year * 10000 + timeStamp.Month * 100 + timeStamp.Day;

                ProductName = highlights.ProductName;
                DateUsedAsGroupingId = (uint)dateUsedAsGroupingId;
                SessionId = highlights.RuntimeSessionId;
                Timestamp = timeStamp;
                Platform = highlights.Platform;
                UnityVersion = highlights.UnityVersion;
                GraphicsDeviceType = highlights.GraphicsDeviceType;
            }

            highlights?.Dispose();
        }

        public int HighlightsFileVersion { get; }
        public string Name { get; }
        public string FullPath { get; }
        public string ProductName { get; }
        public uint DateUsedAsGroupingId { get; }
        public uint SessionId { get; }
        public DateTime Timestamp { get; }
        public RuntimePlatform Platform { get; }
        public bool EditorPlatform => Platform is RuntimePlatform.OSXEditor or RuntimePlatform.WindowsEditor or RuntimePlatform.LinuxEditor;
        public string UnityVersion { get; }

        public GraphicsDeviceType GraphicsDeviceType { get; }

        public bool Equals(CaptureFileModel other)
        {
            return other != null &&
                HighlightsFileVersion == other.HighlightsFileVersion &&
                Name == other.Name &&
                FullPath == other.FullPath &&
                ProductName == other.ProductName &&
                DateUsedAsGroupingId == other.DateUsedAsGroupingId &&
                SessionId == other.SessionId &&
                Timestamp == other.Timestamp &&
                Platform == other.Platform &&
                UnityVersion == other.UnityVersion &&
                GraphicsDeviceType == other.GraphicsDeviceType;
        }
    }
}
