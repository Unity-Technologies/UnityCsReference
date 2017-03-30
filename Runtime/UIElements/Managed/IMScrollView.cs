// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class IMScrollView : IMContainer
    {
        private ScrollViewState m_State;

        public Vector2 scrollPosition
        {
            get { return m_ScrollPosition; }
            private set { m_ScrollPosition = value; }
        }

        public float scrollPositionHorizontal
        {
            get { return m_ScrollPosition.x; }
            set { m_ScrollPosition.x = value; }
        }
        public float scrollPositionVertical
        {
            get { return m_ScrollPosition.y; }
            set { m_ScrollPosition.y = value; }
        }
        private Vector2 m_ScrollPosition;

        private GUIStyle m_HorizontalScrollbar;
        private GUIStyle m_VerticalScrollbar;
        private GUIStyle m_Background;

        public Rect viewRect;

        private bool m_NeedsVertical;
        private bool m_NeedsHorizontal;

        private Rect m_ClipRect;

        private readonly IMScroller m_HorizontalScroller;
        private readonly IMScroller m_VerticalScroller;

        public IMScrollView()
        {
            m_HorizontalScrollbar = GUIStyle.none;
            m_VerticalScrollbar = GUIStyle.none;
            m_Background = GUIStyle.none;
            m_HorizontalScroller = new IMScroller();
            m_VerticalScroller = new IMScroller();
        }

        public void SetProperties(Rect pos, Vector2 scrollPos, Rect viewRect, bool alwaysShowHorizontal,
            bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            position = pos;

            scrollPosition = scrollPos;
            this.viewRect = viewRect;
            m_HorizontalScrollbar = horizontalScrollbar;
            m_VerticalScrollbar = verticalScrollbar;
            m_Background = background;
            m_NeedsVertical = alwaysShowVertical;
            m_NeedsHorizontal = alwaysShowHorizontal;

            CheckState();

            GUIStyle horizThumbStyle = m_HorizontalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_HorizontalScrollbar.name + "thumb") : GUIStyle.none;
            GUIStyle horizLeftStyle = m_HorizontalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_HorizontalScrollbar.name + "leftbutton") : GUIStyle.none;
            GUIStyle horizRightStyle = m_HorizontalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_HorizontalScrollbar.name + "rightbutton") : GUIStyle.none;

            m_HorizontalScroller.SetProperties(new Rect(position.x, position.yMax - m_HorizontalScrollbar.fixedHeight, m_ClipRect.width, m_HorizontalScrollbar.fixedHeight),
                scrollPosition.x, Mathf.Min(m_ClipRect.width, this.viewRect.width), 0, this.viewRect.width,
                m_HorizontalScrollbar, horizThumbStyle, horizLeftStyle, horizRightStyle, true);

            GUIStyle vertThumbStyle = m_VerticalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_VerticalScrollbar.name + "thumb") : GUIStyle.none;
            GUIStyle vertUpStyle = m_VerticalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_VerticalScrollbar.name + "upbutton") : GUIStyle.none;
            GUIStyle vertBottomStyle = m_VerticalScrollbar != GUIStyle.none ? GUI.skin.GetStyle(m_VerticalScrollbar.name + "downbutton") : GUIStyle.none;

            m_VerticalScroller.SetProperties(new Rect(m_ClipRect.xMax + m_VerticalScrollbar.margin.left, m_ClipRect.y, m_VerticalScrollbar.fixedWidth, m_ClipRect.height),
                scrollPosition.y, Mathf.Min(m_ClipRect.height, this.viewRect.height), 0, this.viewRect.height,
                m_VerticalScrollbar, vertThumbStyle, vertUpStyle, vertBottomStyle, false);
        }

        public override void OnReuse()
        {
            base.OnReuse();
            m_HorizontalScroller.OnReuse();
            m_VerticalScroller.OnReuse();
        }

        // TODO Should probably be a DoScrollWheel func and handled by HandleEvent, but it currently
        // doesn't work as it's called on EndView and it created havoc with clip stack.
        public void HandleScrollWheel(Event evt)
        {
            if (m_State.position.Contains(evt.mousePosition))
            {
                m_State.scrollPosition.x = Mathf.Clamp(m_State.scrollPosition.x + (evt.delta.x * 20f), 0f, m_State.viewRect.width - m_State.visibleRect.width);
                m_State.scrollPosition.y = Mathf.Clamp(m_State.scrollPosition.y + (evt.delta.y * 20f), 0f, m_State.viewRect.height - m_State.visibleRect.height);
                m_State.apply = true;
                evt.Use();
            }
        }

        public override bool OnGUI(Event evt)
        {
            bool used = false;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.DragUpdated:
                    used = DoDragUpdated(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.Layout:
                case EventType.Used:
                    // Do nothing.
                    break;

                default:
                    if (evt.type == EventType.Repaint && m_Background != GUIStyle.none)
                    {
                        m_Background.Draw(position, position.Contains(evt.mousePosition), false, m_NeedsHorizontal && m_NeedsVertical, false);
                    }

                    if (m_NeedsHorizontal && m_HorizontalScrollbar != GUIStyle.none)
                    {
                        used = m_HorizontalScroller.OnGUI(evt);
                        scrollPositionHorizontal = m_HorizontalScroller.value;
                    }
                    else
                    {
                        if (m_HorizontalScrollbar != GUIStyle.none)
                            scrollPositionHorizontal = 0;
                        else
                            scrollPositionHorizontal = Mathf.Clamp(scrollPositionHorizontal, 0, Mathf.Max(viewRect.width - position.width, 0));
                    }

                    if (m_NeedsVertical && m_VerticalScrollbar != GUIStyle.none)
                    {
                        used = m_VerticalScroller.OnGUI(evt);
                        scrollPositionVertical = m_VerticalScroller.value;
                    }
                    else
                    {
                        if (m_VerticalScrollbar != GUIStyle.none)
                            scrollPositionVertical = 0;
                        else
                            scrollPositionVertical = Mathf.Clamp(scrollPositionVertical, 0, Mathf.Max(viewRect.height - position.height, 0));
                    }

                    break;
            }

            if (used)
            {
                evt.Use();
            }

            GUIClip.Internal_Push(m_ClipRect, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)), Vector2.zero, false);

            return used;
        }

        public void ScrollTo(Rect pos)
        {
            m_State.ScrollTo(pos);
        }

        public bool ScrollTowards(Rect pos, float maxDelta)
        {
            return m_State.ScrollTowards(pos, maxDelta);
        }

        public override void GenerateControlID()
        {
            m_HorizontalScroller.GenerateControlID();
            m_VerticalScroller.GenerateControlID();
            id = GUIUtility.GetControlID("ScrollView".GetHashCode(), FocusType.Passive);
        }

        private void CheckState()
        {
            Debug.Assert(id != 0, "Invalid zero control ID");

            m_State = (ScrollViewState)GUIUtility.GetStateObject(typeof(ScrollViewState), id);
            if (m_State.apply)
            {
                scrollPosition = m_State.scrollPosition;
                m_State.apply = false;
            }
            m_State.position = position;
            m_State.scrollPosition = scrollPosition;
            m_State.viewRect = viewRect;
            m_State.visibleRect = viewRect;
            m_State.visibleRect.width = position.width;
            m_State.visibleRect.height = position.height;

            m_ClipRect = new Rect(position);

            // Check if we need a horizontal scrollbar
            if (m_NeedsHorizontal || viewRect.width > m_ClipRect.width)
            {
                m_State.visibleRect.height = position.height - m_HorizontalScrollbar.fixedHeight + m_HorizontalScrollbar.margin.top;
                m_ClipRect.height -= m_HorizontalScrollbar.fixedHeight + m_HorizontalScrollbar.margin.top;
                m_NeedsHorizontal = true;
            }

            if (m_NeedsVertical || viewRect.height > m_ClipRect.height)
            {
                m_State.visibleRect.width = position.width - m_VerticalScrollbar.fixedWidth + m_VerticalScrollbar.margin.left;
                m_ClipRect.width -= m_VerticalScrollbar.fixedWidth + m_VerticalScrollbar.margin.left;
                m_NeedsVertical = true;
                if (!m_NeedsHorizontal && viewRect.width > m_ClipRect.width)
                {
                    m_State.visibleRect.height = position.height - m_HorizontalScrollbar.fixedHeight + m_HorizontalScrollbar.margin.top;
                    m_ClipRect.height -= m_HorizontalScrollbar.fixedHeight + m_HorizontalScrollbar.margin.top;
                    m_NeedsHorizontal = true;
                }
            }
        }

        bool DoDragUpdated(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
            {
                if (Mathf.Abs(args.mousePosition.y - position.y) < 8)
                {
                    scrollPositionVertical -= 16;
                    GUI.InternalRepaintEditorWindow();
                }
                else if (Mathf.Abs(args.mousePosition.y - position.yMax) < 8)
                {
                    scrollPositionVertical += 16;
                    GUI.InternalRepaintEditorWindow();
                }
            }
            return false;
        }
    }
}
