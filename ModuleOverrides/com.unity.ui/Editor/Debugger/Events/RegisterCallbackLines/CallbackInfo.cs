// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
