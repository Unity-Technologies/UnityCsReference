using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class TitleInfo : IRegisteredCallbackLine
    {
        public LineType type => LineType.Title;
        public string text { get; }
        public VisualElement callbackHandler { get; }

        public TitleInfo(string text, VisualElement handler)
        {
            this.text = text;
            callbackHandler = handler;
        }
    }
}
