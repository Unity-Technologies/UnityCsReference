// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    enum LineType
    {
        Title,
        Callback,
        CodeLine
    }

    interface IRegisteredCallbackLine
    {
        LineType type { get; }
        string text { get; }
        VisualElement callbackHandler { get; }
    }
}
