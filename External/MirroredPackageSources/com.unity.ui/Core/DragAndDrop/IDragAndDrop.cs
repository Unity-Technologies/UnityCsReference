using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IDragAndDrop
    {
        void StartDrag(StartDragArgs args);
        void AcceptDrag();
        void SetVisualMode(DragVisualMode visualMode);
        IDragAndDropData data { get; }
    }

    internal interface IDragAndDropData
    {
        object GetGenericData(string key);
        object userData { get; }
        IEnumerable<Object> unityObjectReferences { get; }
    }
}
