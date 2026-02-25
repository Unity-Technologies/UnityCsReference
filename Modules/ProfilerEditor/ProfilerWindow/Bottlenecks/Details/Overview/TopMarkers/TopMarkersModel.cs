// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    readonly struct TopMarkersModel
    {
        public TopMarkersModel(Marker[] markers)
        {
            Markers = markers;
        }

        public Marker[] Markers { get; }

        public struct Marker
        {
            public Marker(
                int markerId,
                ulong value,
                Unit units,
                uint numberOfInstances,
                int frameIndex,
                int threadIndex)
            {
                MarkerId = markerId;
                Value = value;
                Units = units;
                NumberOfInstances = numberOfInstances;
                FrameIndex = frameIndex;
                ThreadIndex = threadIndex;
                Name = null;
            }

            public string FormatValue()
            {
                return Units switch
                {
                    Unit.TimeNanoseconds => TimeFormatterUtility.FormatTimeNsToMs(Value),
                    Unit.Bytes => EditorUtility.FormatBytes(Convert.ToInt64(Value)),
                    _ => throw new ArgumentOutOfRangeException($"Invalid marker unit .{Units}'."),
                };
            }

            public int MarkerId { get; }
            public ulong Value { get; }
            public Unit Units { get; }
            public uint NumberOfInstances { get; }
            public int FrameIndex { get; }
            public int ThreadIndex { get; }

            public string Name { get; set; }

            public enum Unit
            {
                TimeNanoseconds,
                Bytes,
            }
        }
    }
}
