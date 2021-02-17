using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class CallbackInfo : IRegisteredCallbackLine
    {
        public LineType type => LineType.Callback;
        public string text { get; }
        public VisualElement callbackHandler { get; }

        public CallbackInfo(string text, VisualElement handler)
        {
            this.text = text;
            callbackHandler = handler;
        }
    }
}
