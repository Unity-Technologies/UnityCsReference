// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.StyleSheets;
using UnityEditor.Experimental;

namespace UnityEditor
{
    // This uses a normal editor window with a single view inside.
    internal class PaneDragTab : GUIView
    {
        const float kMaxArea = 50000.0f;

#pragma warning disable 169

        private static PaneDragTab s_Get;
        private float m_TargetAlpha = 1.0f;
        private DropInfo.Type m_Type = (DropInfo.Type)(-1);
        private GUIContent m_Content;

        [SerializeField] bool m_Shadow;
        [SerializeField] Vector2 m_FullWindowSize = new Vector2(80, 60);
        [SerializeField] Rect m_TargetRect;
        [SerializeField] internal ContainerWindow m_Window;
        [SerializeField] ContainerWindow m_InFrontOfWindow = null;

        private static class Styles
        {
            private static readonly StyleBlock tab = EditorResources.GetStyle("tab");
            public static readonly float tabMinWidth = tab.GetFloat(StyleCatalogKeyword.minWidth, 50.0f);
            public static readonly float tabMaxWidth = tab.GetFloat(StyleCatalogKeyword.maxWidth, 150.0f);
            public static readonly float tabWidthPadding = tab.GetFloat(StyleCatalogKeyword.paddingRight);

            public static GUIStyle dragtab = "dragtab";
            public static GUIStyle view = "TabWindowBackground";
            public static readonly GUIStyle tabLabel = new GUIStyle("dragtab") { name = "dragtab-label" };

            public static readonly SVC<Color> backgroundColor = new SVC<Color>("--theme-background-color");
        }

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

            m_TargetRect.x = Mathf.Floor(m_TargetRect.x);
            m_TargetRect.y = Mathf.Floor(m_TargetRect.y);
            m_TargetRect.width = Mathf.Floor(m_TargetRect.width);
            m_TargetRect.height = Mathf.Floor(m_TargetRect.height);

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

            // Do not steal focus from the pane
            m_Window.Show(ShowMode.NoShadow, loadPosition: true, displayImmediately: false, setFocus: false);

            m_TargetRect = pixelPos;
        }

        void SetWindowPos(Rect screenPosition)
        {
            m_Window.position = screenPosition;
        }

        protected override void OldOnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Rect windowRect = new Rect(0, 0, position.width, position.height);

            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(windowRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, Styles.backgroundColor, 0, 0);
            if (m_Type == DropInfo.Type.Tab)
            {
                Styles.dragtab.Draw(windowRect, false, true, false, false);
                GUI.Label(windowRect, m_Content, Styles.tabLabel);
            }
            else
            {
                const float dragTabOffsetX = 2f;
                const float dragTabHeight = DockArea.kTabHeight;

                float minWidth, expectedWidth;
                Styles.dragtab.CalcMinMaxWidth(m_Content, out minWidth, out expectedWidth);
                float tabWidth = Mathf.Max(Mathf.Min(expectedWidth, Styles.tabMaxWidth), Styles.tabMinWidth) + Styles.tabWidthPadding;
                Rect tabPositionRect = new Rect(1, 2f, tabWidth, dragTabHeight);
                float roundedPosX = Mathf.Floor(tabPositionRect.x);
                float roundedWidth = Mathf.Ceil(tabPositionRect.x + tabPositionRect.width) - roundedPosX;
                Rect tabContentRect = new Rect(roundedPosX, tabPositionRect.y, roundedWidth, tabPositionRect.height);
                Rect viewRect = new Rect(dragTabOffsetX, tabContentRect.yMax - 2f,
                    position.width - dragTabOffsetX * 2, position.height - tabContentRect.yMax);

                Styles.dragtab.Draw(tabContentRect, false, true, false, false);
                Styles.view.Draw(viewRect, GUIContent.none, false, false, true, true);
                GUI.Label(tabPositionRect, m_Content, Styles.tabLabel);
            }

            // We currently only support this on macOS
            m_Window.SetAlpha(m_TargetAlpha);
        }
    }
} // namespace
