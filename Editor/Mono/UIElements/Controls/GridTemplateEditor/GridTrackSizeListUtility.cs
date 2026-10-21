// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class GridTrackSizeListUtility
    {
        // Returns false, leaving destination untouched, when it already holds the same tracks.
        public static bool CopyIfChanged(ReadOnlySpan<GridTrackSize> source, List<GridTrackSize> destination)
        {
            if (source.Length == destination.Count)
            {
                int i = 0;
                while (i < source.Length && source[i].Equals(destination[i]))
                    ++i;
                if (i == source.Length)
                    return false;
            }

            destination.Clear();
            foreach (var t in source)
                destination.Add(t);
            return true;
        }
    }
}
