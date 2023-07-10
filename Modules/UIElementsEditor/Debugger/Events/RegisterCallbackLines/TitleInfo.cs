// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
