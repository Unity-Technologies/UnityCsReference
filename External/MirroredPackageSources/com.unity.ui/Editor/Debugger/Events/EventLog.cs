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
