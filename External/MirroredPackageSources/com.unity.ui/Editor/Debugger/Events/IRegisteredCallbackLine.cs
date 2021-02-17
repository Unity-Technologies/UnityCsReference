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
