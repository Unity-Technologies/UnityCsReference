// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    // This uses a normal editor window with a single view inside.
    internal class PaneDragTab : GUIView
    {
        const float kMaxArea = 50000.0f;

        [SerializeField]
#pragma warning disable 169
        bool m_Shadow;

        static PaneDragTab s_Get;
        const float kTopThumbnailOffset = 10;
        [SerializeField]
        Vector2 m_FullWindowSize = new Vector2(80, 60);

        [SerializeField]
        Rect m_TargetRect;
        [SerializeField]
        static GUIStyle s_PaneStyle, s_TabStyle;

        bool m_TabVisible;

        float m_TargetAlpha = 1.0f;

        DropInfo.Type m_Type = (DropInfo.Type)(-1);

        GUIContent m_Content;
        [SerializeField]
        internal ContainerWindow m_Window;
        [SerializeField]
        ContainerWindow m_InFrontOfWindow = null;

        static public PaneDragTab get
        {
            get
            {
                if (!s_Get)
                {
                    Object[] objs = Resources.FindObjectsOfTypeAll(typeof(PaneDragTab));
                    if (objs.Length != 0)
                        s_Get = (PaneDragTab)objs[0];
                    if (s_Get)
                    {
                        return s_Get;
                    }
                    s_Get = ScriptableObject.CreateInstance<PaneDragTab>();
                }
                return s_Get;
            }
        }

        public void SetDropInfo(DropInfo di, Vector2 mouseScreenPos, ContainerWindow inFrontOf)
        {
            if (m_Type != di.type || (di.type == DropInfo.Type.Pane && di.rect != m_TargetRect))
            {
                m_Type = di.type;

                switch (di.type)
                {
                    case DropInfo.Type.Window:
                        m_TargetAlpha = 0.6f;
                        break;
                    case DropInfo.Type.Pane:
                    case DropInfo.Type.Tab:
                        m_TargetAlpha = 1.0f;
                        break;
                }
            }

            switch (di.type)
            {
                case DropInfo.Type.Window:
                    m_TargetRect = new Rect(mouseScreenPos.x - m_FullWindowSize.x / 2, mouseScreenPos.y - m_FullWindowSize.y / 2,
                            m_FullWindowSize.x, m_FullWindowSize.y);
                    break;
                case DropInfo.Type.Pane:
                case DropInfo.Type.Tab:
                    m_TargetRect = di.rect;
                    break;
            }

            m_TabVisible = di.type == DropInfo.Type.Tab;

            m_TargetRect.x = Mathf.Round(m_TargetRect.x);
            m_TargetRect.y = Mathf.Round(m_TargetRect.y);
            m_TargetRect.width = Mathf.Round(m_TargetRect.width);
            m_TargetRect.height = Mathf.Round(m_TargetRect.height);

            m_InFrontOfWindow = inFrontOf;
            m_Window.MoveInFrontOf(m_InFrontOfWindow);

            // On Windows, repainting without setting proper size first results in one garbage frame... For some reason.
            SetWindowPos(m_TargetRect);
            // Yes, repaint.
            Repaint();
        }

        public void Close()
        {
            if (m_Window)
                m_Window.Close();
            DestroyImmediate(this, true);
            s_Get = null;
        }

        public void Show(Rect pixelPos, GUIContent content, Vector2 viewSize, Vector2 mouseScreenPosition)
        {
            m_Content = content;
            // scale not to be larger then maxArea pixels.
            var area = viewSize.x * viewSize.y;
            m_FullWindowSize = viewSize * Mathf.Sqrt(Mathf.Clamp01(kMaxArea / area));

            if (!m_Window)
            {
                m_Window = ScriptableObject.CreateInstance<ContainerWindow>();
                m_Window.m_DontSaveToLayout = true;
                SetMinMaxSizes(Vector2.zero, new Vector2(10000, 10000));
                SetWindowPos(pixelPos);
                m_Window.rootView = this;
            }
            else
            {
                SetWindowPos(pixelPos);
            }
            m_Window.Show(ShowMode.NoShadow, true, false);

            m_TargetRect = pixelPos;
        }

        void SetWindowPos(Rect screenPosition)
        {
            m_Window.position = screenPosition;
        }

        protected override void OldOnGUI()
        {
            if (s_PaneStyle == null)
            {
                s_PaneStyle = "dragtabdropwindow";
                s_TabStyle = "dragtab";
            }

            if (Event.current.type == EventType.Repaint)
            {
                Color oldGUIColor = GUI.color;
                GUI.color = Color.white;
                s_PaneStyle.Draw(new Rect(0, 0, position.width, position.height), m_Content, false,  false, true, true);
                if (m_TabVisible)
                {
                    s_TabStyle.Draw(new Rect(0, 0, position.width, position.height), m_Content, false,  false, true, true);
                }
                GUI.color = oldGUIColor;

                m_Window.SetAlpha(m_TargetAlpha);  //We currently only support this on macOS
            }
        }
    }
} // namespace
