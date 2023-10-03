// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IDragAndDropController<in TArgs>
    {
        bool CanStartDrag(IEnumerable<int> itemIndices);
        StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIndices, bool skipText = false);
        DragVisualMode HandleDragAndDrop(TArgs args);
        void OnDrop(TArgs args);
        void DragCleanup();
        IEnumerable<int> GetSortedSelectedIds();
    }

    internal enum DragVisualMode
    {
        None,
        Copy,
        Move,
        Rejected
    }

    internal struct StartDragArgs
    {
        public StartDragArgs(string title, DragVisualMode visualMode)
        {
            this.title = title;
            this.visualMode = visualMode;
            genericData = null;
            unityObjectReferences = null;
        }

        // This API is used by com.unity.entities, we cannot remove it yet.
        internal StartDragArgs(string title, object target)
        {
            this.title = title;
            visualMode = DragVisualMode.Move;
            genericData = null;
            unityObjectReferences = null;
            SetGenericData(DragAndDropData.dragSourceKey, target);
        }

        public string title { get; }

        public DragVisualMode visualMode { get; }

        internal Hashtable genericData { get; private set; }
        internal IEnumerable<Object> unityObjectReferences { get; private set; }

        public void SetGenericData(string key, object data)
        {
            genericData ??= new Hashtable();
            genericData[key] = data;
        }

        public void SetUnityObjectReferences(IEnumerable<Object> references)
        {
            unityObjectReferences = references;
        }
    }
}
