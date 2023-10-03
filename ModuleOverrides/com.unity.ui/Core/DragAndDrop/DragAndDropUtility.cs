// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal static class DragAndDropUtility
    {
        static Func<IDragAndDrop> s_MakeDragAndDropClientFunc;
        static IDragAndDrop s_DragAndDropEditor;
        static IDragAndDrop s_DragAndDropPlayMode;

        internal static IDragAndDrop GetDragAndDrop(IPanel panel)
        {
            if (panel.contextType == ContextType.Player)
            {
                return s_DragAndDropPlayMode ??= new DefaultDragAndDropClient();
            }

            return s_DragAndDropEditor ??= s_MakeDragAndDropClientFunc != null ? s_MakeDragAndDropClientFunc.Invoke() : new DefaultDragAndDropClient();
        }

        internal static void RegisterMakeClientFunc(Func<IDragAndDrop> makeClient)
        {
            s_MakeDragAndDropClientFunc = makeClient;
            s_DragAndDropEditor = null;
        }
    }

    internal class DefaultDragAndDropClient : DragAndDropData, IDragAndDrop
    {
        public override DragVisualMode visualMode => m_VisualMode;
        public override object source => GetGenericData(dragSourceKey);

        readonly Hashtable m_GenericData = new();

        public override IEnumerable<Object> unityObjectReferences => m_UnityObjectReferences;

        Label m_DraggedInfoLabel;
        DragVisualMode m_VisualMode;
        IEnumerable<Object> m_UnityObjectReferences;

        public override object GetGenericData(string key)
        {
            return m_GenericData.ContainsKey(key) ? m_GenericData[key] : null;
        }

        public override void SetGenericData(string key, object value)
        {
            m_GenericData[key] = value;
        }

        public void StartDrag(StartDragArgs args, Vector3 pointerPosition)
        {
            if (args.unityObjectReferences != null)
                m_UnityObjectReferences = args.unityObjectReferences.ToArray();

            m_VisualMode = args.visualMode;
            foreach (DictionaryEntry entry in args.genericData)
            {
                m_GenericData[(string)entry.Key] = entry.Value;
            }

            if (string.IsNullOrEmpty(args.title))
                return;

            var sourceElement = source as VisualElement;
            var root = sourceElement?.panel.visualTree;
            if (root == null)
                return;

            m_DraggedInfoLabel ??= new Label
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute },
            };
            m_DraggedInfoLabel.text = args.title;
            m_DraggedInfoLabel.style.top = pointerPosition.y;
            m_DraggedInfoLabel.style.left = pointerPosition.x;
            root.Add(m_DraggedInfoLabel);
        }

        public void UpdateDrag(Vector3 pointerPosition)
        {
            if (m_DraggedInfoLabel == null)
                return;

            m_DraggedInfoLabel.style.top = pointerPosition.y;
            m_DraggedInfoLabel.style.left = pointerPosition.x;
        }

        public void AcceptDrag()
        {
            // Nothing to do here.
        }

        public void SetVisualMode(DragVisualMode mode)
        {
            m_VisualMode = mode;
        }

        public void DragCleanup()
        {
            m_UnityObjectReferences = null;
            m_GenericData?.Clear();
            SetVisualMode(DragVisualMode.None);
            m_DraggedInfoLabel?.RemoveFromHierarchy();
        }

        public DragAndDropData data => this;
    }
}
