using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class CodeLineInfo : IRegisteredCallbackLine
    {
        public LineType type => LineType.CodeLine;
        public string text { get; }
        public VisualElement callbackHandler { get; }

        public string fileName { get; }
        public int lineNumber { get; }
        public int lineHashCode { get; }

        public bool highlighted { get; set; }

        public CodeLineInfo(string text, VisualElement handler, string fileName, int lineNumber, int lineHashCode)
        {
            this.text = text;
            callbackHandler = handler;
            this.fileName = fileName;
            this.lineNumber = lineNumber;
            this.lineHashCode = lineHashCode;
        }
    }
}
