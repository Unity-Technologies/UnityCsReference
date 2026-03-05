// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    static class TopMarkersModelUtility
    {
        public static string GetFormattedMarkerName(this Marker marker, bool showFullScriptingMethodNames)
        {
            var name = marker.Name;
            if (showFullScriptingMethodNames)
                return name;

            var index = name.IndexOf("::", StringComparison.Ordinal);
            if (index != -1)
                return name.Substring(index + 2);

            return name;
        }
    }
}
