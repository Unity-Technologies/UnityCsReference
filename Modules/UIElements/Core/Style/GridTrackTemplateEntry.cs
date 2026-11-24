// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // One authored entry of a grid-template-* track list: a single track, or repeat(repeatCount, track).
    // The computed style only holds the expanded track list, so authoring reads and writes these to keep repeat() intact.
    [VisibleToOtherModules("UnityEditor.UIElementsModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal readonly struct GridTrackTemplateEntry : IEquatable<GridTrackTemplateEntry>
    {
        public readonly GridTrackSize track;
        public readonly int repeatCount;

        public GridTrackTemplateEntry(GridTrackSize track, int repeatCount = 0)
        {
            this.track = track;
            this.repeatCount = repeatCount;
        }

        public bool isRepeat => repeatCount > 0;

        public static void Flatten(IReadOnlyList<GridTrackTemplateEntry> entries, List<GridTrackSize> result)
        {
            result.Clear();
            if (entries == null)
                return;
            foreach (var entry in entries)
            {
                for (int i = Math.Max(1, entry.repeatCount); i > 0; --i)
                    result.Add(entry.track);
            }
        }

        public bool Equals(GridTrackTemplateEntry other) => track.Equals(other.track) && repeatCount == other.repeatCount;

        public override bool Equals(object obj) => obj is GridTrackTemplateEntry other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(track, repeatCount);

        public override string ToString() => isRepeat ? $"repeat({repeatCount}, {track})" : track.ToString();
    }
}
