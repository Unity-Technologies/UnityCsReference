// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class IMGUIContainer : VisualContainer
    {
        // Set this delegate to have your IMGUI code execute inside the container
        private readonly Action m_OnGUIHandler;

        public int executionContext { get; set; }
        internal Rect lastWorldClip { get; set; }

        private GUILayoutUtility.LayoutCache m_Cache = null;
        private GUILayoutUtility.LayoutCache cache
        {
            get
            {
                if (m_Cache == null)
                    m_Cache = new GUILayoutUtility.LayoutCache();
                return m_Cache;
            }
        }

        public ContextType contextType { get; set; }

        public IMGUIContainer(Action onGUIHandler)
        {
            m_OnGUIHandler = onGUIHandler;
            contextType = ContextType.Editor;
        }

        public override void DoRepaint(IStylePainter args)
        {
            lastWorldClip = args.currentWorldClip;
            HandleEvent(new Event {type = EventType.Repaint, mousePosition = args.mousePosition}, this);
        }

        internal override void ChangePanel(IVisualElementPanel p)
        {
            if (elementPanel != null)
            {
                elementPanel.IMGUIContainersCount--;
            }

            base.ChangePanel(p);

            if (elementPanel != null)
            {
                elementPanel.IMGUIContainersCount++;
            }
        }

        // global GUI values.
        // container saves and restores them before doing his thing
        private struct GUIGlobals
        {
            public Matrix4x4 matrix;
            public Color color;
            public Color contentColor;
            public Color backgroundColor;
            public bool enabled;
            public bool changed;
        }

        private GUIGlobals m_GUIGlobals;

        private void SaveGlobals()
        {
            m_GUIGlobals.matrix = GUI.matrix;
            m_GUIGlobals.color = GUI.color;
            m_GUIGlobals.contentColor = GUI.contentColor;
            m_GUIGlobals.backgroundColor = GUI.backgroundColor;
            m_GUIGlobals.enabled = GUI.enabled;
            m_GUIGlobals.changed = GUI.changed;
        }

        private void RestoreGlobals()
        {
            GUI.matrix = m_GUIGlobals.matrix;
            GUI.color = m_GUIGlobals.color;
            GUI.contentColor = m_GUIGlobals.contentColor;
            GUI.backgroundColor = m_GUIGlobals.backgroundColor;
            GUI.enabled = m_GUIGlobals.enabled;
            GUI.changed = m_GUIGlobals.changed;
        }

        private bool DoOnGUI(Event evt)
        {
            if (m_OnGUIHandler == null
                || panel == null)
            {
                Debug.LogWarning("Null panel");
                return false;
            }

            SaveGlobals();
            int ctx = executionContext != 0 ? executionContext : elementPanel.instanceID;
            UIElementsUtility.BeginContainerGUI(cache, ctx, evt, this);

            EventType originalEventType = Event.current.type;

            m_OnGUIHandler(); // native will catch exceptions thrown here

            GUIUtility.CheckForTabEvent(evt);

            // The Event will probably be nuked with the next function call, so we get its type now.
            EventType eventType = Event.current.type;

            UIElementsUtility.EndContainerGUI();
            RestoreGlobals();

            if (eventType == EventType.Used)
            {
                if (originalEventType == EventType.MouseDown)
                    this.TakeKeyboardFocus();
                Dirty(ChangeType.Repaint);
                return true;
            }
            return false;
        }

        public override void OnLostKeyboardFocus()
        {
            GUIUtility.keyboardControl = 0;
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            if (m_OnGUIHandler == null || panel == null)
            {
                return EventPropagation.Continue;
            }

            // layout event
            bool ret = DoOnGUI(new Event(evt) { type = EventType.Layout });
            // the actual event
            ret |= DoOnGUI(evt);

            if (ret)
            {
                return EventPropagation.Stop;
            }

            if (evt.type == EventType.MouseUp && this.HasCapture())
            {
                // This can happen if a MouseDown was caught by a different IM element but we ended up here on the
                // MouseUp event because no other element consumed it, including the one that had capture.
                // Example case: start text selection in a text field, but drag mouse all the way into another
                // part of the editor, release the mouse button.  Since the mouse up was sent to another container,
                // we end up here and that is perfectly legal (unfortunately unavoidable for now since no IMGUI control
                // used the event), but hot control might still belong to the IM text field at this point.
                // We can safely release the hot control which will release the capture as the same time.
                GUIUtility.hotControl = 0;
            }

            return EventPropagation.Continue;
        }
    }
}
