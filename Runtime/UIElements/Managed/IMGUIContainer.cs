// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    //TODO: rename to IMGUIAdapter or something, as it's NOT a VisualContainer
    public class IMGUIContainer : VisualElement
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

        internal override void DoRepaint(IStylePainter painter)
        {
            lastWorldClip = painter.currentWorldClip;
            HandleEvent(painter.repaintEvent, this);
        }

        internal override void ChangePanel(BaseVisualElementPanel p)
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
                return false;
            }

            SaveGlobals();
            int ctx = executionContext != 0 ? executionContext : elementPanel.instanceID;
            UIElementsUtility.BeginContainerGUI(cache, ctx, evt, this);

            EventType originalEventType = Event.current.type;

            try
            {
                m_OnGUIHandler();
            }
            catch (Exception exception)
            {
                // only for layout events: we always intercept any exceptions to not interrupt event processing
                if (originalEventType == EventType.Layout)
                {
                    // really this means: don't log ExitGUIException's
                    if (!GUIUtility.ShouldRethrowException(exception))
                    {
                        Debug.LogException(exception);
                    }
                }
                else
                {
                    // rethrow event if not in layout
                    throw;
                }
            }

            GUIUtility.CheckForTabEvent(evt);

            // The Event will probably be nuked with the next function call, so we get its type now.
            EventType eventType = Event.current.type;

            UIElementsUtility.EndContainerGUI();
            RestoreGlobals();

            if (eventType == EventType.Used)
            {
                Dirty(ChangeType.Repaint);
                return true;
            }
            return false;
        }

        public override void OnLostKeyboardFocus()
        {
            // nuke keyboard focus when losing focus ourselves
            GUIUtility.keyboardControl = 0;
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            if (m_OnGUIHandler == null || elementPanel == null || elementPanel.IMGUIEventInterests.WantsEvent(evt.type) == false)
            {
                return EventPropagation.Continue;
            }

            EventType originalEventType = evt.type;
            evt.type = EventType.Layout;
            // layout event
            bool ret = DoOnGUI(evt);
            // the actual event
            evt.type = originalEventType;
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

            // If we detect that we were removed while processing this event, hi-jack the event loop to early exit
            // In IMGUI/Editor this is actually possible just by calling EditorWindow.Close() for example
            if (elementPanel == null)
            {
                GUIUtility.ExitGUI();
            }

            return EventPropagation.Continue;
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;
            if (widthMode != MeasureMode.Exactly || heightMode != MeasureMode.Exactly)
            {
                DoOnGUI(new Event { type = EventType.Layout });
                measuredWidth = m_Cache.topLevel.minWidth;
                measuredHeight = m_Cache.topLevel.minHeight;
            }

            switch (widthMode)
            {
                case MeasureMode.Exactly:
                    measuredWidth = desiredWidth;
                    break;
                case MeasureMode.AtMost:
                    measuredWidth = Mathf.Min(measuredWidth, desiredWidth);
                    break;
            }

            switch (heightMode)
            {
                case MeasureMode.Exactly:
                    measuredHeight = desiredHeight;
                    break;
                case MeasureMode.AtMost:
                    measuredHeight = Mathf.Min(measuredHeight, desiredHeight);
                    break;
            }

            return new Vector2(measuredWidth, measuredHeight);
        }
    }
}
