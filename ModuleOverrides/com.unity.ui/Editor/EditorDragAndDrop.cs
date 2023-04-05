// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class EditorDragAndDrop : DragAndDropData, IDragAndDrop
    {
        [InitializeOnLoadMethod]
        private static void RegisterEditorClient()
        {
            DragAndDropUtility.RegisterMakeClientFunc(() => new EditorDragAndDrop());
        }

        public override object source => DragAndDrop.GetGenericData(dragSourceKey);
        public override IEnumerable<Object> unityObjectReferences => DragAndDrop.objectReferences;

        public override DragVisualMode visualMode
        {
            get
            {
                return DragAndDrop.visualMode switch
                {
                    DragAndDropVisualMode.Copy => DragVisualMode.Copy,
                    DragAndDropVisualMode.None => DragVisualMode.None,
                    DragAndDropVisualMode.Move => DragVisualMode.Move,
                    DragAndDropVisualMode.Rejected => DragVisualMode.Rejected,
                    _ => throw new ArgumentException($"Visual mode {DragAndDrop.visualMode} is not supported", nameof(visualMode), null)
                };
            }
        }

        public override object GetGenericData(string key)
        {
            return DragAndDrop.GetGenericData(key);
        }

        public override void SetGenericData(string key, object data)
        {
            DragAndDrop.SetGenericData(key, data);
        }

        public void StartDrag(StartDragArgs args, Vector3 pointerPosition)
        {
            DragAndDrop.PrepareStartDrag();

            if (args.unityObjectReferences != null)
                DragAndDrop.objectReferences = args.unityObjectReferences.ToArray();

            SetVisualMode(args.visualMode);
            foreach (DictionaryEntry entry in args.genericData)
                DragAndDrop.SetGenericData((string)entry.Key, entry.Value);

            if (Event.current != null)
                DragAndDrop.StartDrag(args.title);
        }

        public void UpdateDrag(Vector3 pointerPosition)
        {
            // Nothing to do here, DragAndDrop handles the title position.
        }

        public void AcceptDrag()
        {
            DragAndDrop.AcceptDrag();
        }

        public void SetVisualMode(DragVisualMode mode)
        {
            DragAndDrop.visualMode = mode switch
            {
                DragVisualMode.Copy => DragAndDropVisualMode.Copy,
                DragVisualMode.None => DragAndDropVisualMode.None,
                DragVisualMode.Move => DragAndDropVisualMode.Move,
                DragVisualMode.Rejected => DragAndDropVisualMode.Rejected,
                _ => throw new ArgumentException($"Visual mode {visualMode} is not supported", nameof(visualMode), null)
            };
        }

        public void DragCleanup()
        {
            // Nothing to do here.
        }

        public DragAndDropData data => this;
    }
}
