using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class EditorDragAndDrop : IDragAndDrop, IDragAndDropData
    {
        [InitializeOnLoadMethod]
        private static void RegisterEditorClient()
        {
            DragAndDropUtility.RegisterMakeClientFunc(() => new EditorDragAndDrop());
        }

        private const string k_UserDataKey = "user_data";

        public object userData => DragAndDrop.GetGenericData(k_UserDataKey);
        public IEnumerable<Object> unityObjectReferences => DragAndDrop.objectReferences;

        public object GetGenericData(string key)
        {
            return DragAndDrop.GetGenericData(key);
        }

        public void StartDrag(StartDragArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            if (args.unityObjectReferences != null)
                DragAndDrop.objectReferences = args.unityObjectReferences.ToArray();

            DragAndDrop.SetGenericData(k_UserDataKey, args.userData);
            foreach (DictionaryEntry entry in args.genericData)
                DragAndDrop.SetGenericData((string)entry.Key, entry.Value);

            DragAndDrop.StartDrag(args.title);
        }

        public void AcceptDrag()
        {
            DragAndDrop.AcceptDrag();
        }

        public void SetVisualMode(DragVisualMode visualMode)
        {
            switch (visualMode)
            {
                case DragVisualMode.Copy:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    break;
                case DragVisualMode.None:
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                    break;
                case DragVisualMode.Move:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    break;
                case DragVisualMode.Rejected:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    break;
                default:
                    throw new ArgumentException($"Visual mode {visualMode} is not supported", nameof(visualMode), null);
            }
        }

        public IDragAndDropData data
        {
            get { return this; }
        }
    }
}
