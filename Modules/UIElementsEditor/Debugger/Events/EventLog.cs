// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.UIElements.Debugger
{
    class EventLog
    {
        public List<EventLogLine> lines { get; } = new List<EventLogLine>();

        public EventLog(params EventLogLine[] eventLogLines)
        {
            lines.AddRange(eventLogLines);
        }

        public void AddLine(EventLogLine eventLogLine)
        {
            lines.Add(eventLogLine);
        }

        public void Clear()
        {
            lines.Clear();
        }
    }
}
