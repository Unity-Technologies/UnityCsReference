// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Text;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    internal static class Insights
    {
        /// <summary>
        /// Returns true if UI insights are enabled.
        /// Validate before triggering costly processing related to insights.
        /// </summary>
        public static bool Enabled => UI.Insights.Enabled();

        /// <summary>
        /// Registers an event and calculates the path to the nearest UI document container
        /// </summary>
        /// <param name="id">The type of event.</param>
        /// <param name="source">The source of the event.</param>
        public static void LogEvent(UI.InsightID id, VisualElement source)
        {
            // Skip reporting if element has no name
            if (string.IsNullOrEmpty(source.name))
                return;

            UI.Insights.LogEvent(id, source.name);
        }
    }
}
