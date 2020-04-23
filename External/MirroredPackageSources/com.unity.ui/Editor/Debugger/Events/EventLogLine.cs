using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class EventLogLine
    {
        public int lineNumber { get; }
        public string timestamp { get; }
        public string eventName { get; }
        public string target { get; }
        public EventDebuggerEventRecord eventBase { get; }

        public EventLogLine(int lineNumber, string timestamp = "", string eventName = "", string target = "", EventDebuggerEventRecord eventBase = null)
        {
            this.lineNumber = lineNumber;
            this.timestamp = timestamp;
            this.eventName = eventName;
            this.target = target;
            this.eventBase = eventBase;
        }
    }
}
