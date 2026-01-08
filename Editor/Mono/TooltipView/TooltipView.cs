// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor
{
    class TooltipView : GUIView
    {
        internal const float MAX_WIDTH = 300.0f;

        private GUIContent m_tooltip = new GUIContent();
        private Vector2 m_optimalSize;
        private GUIStyle m_Style;
        private Rect m_hoverRect;

        private VisualElement m_VisualRoot;
        private GUIView hostView;

        internal static TooltipView S_guiView { get => s_guiView; }
        static TooltipView s_guiView;

        private static double s_AutoCloseAfterTime;

        protected override void OnEnable()
        {
            base.OnEnable();
            s_guiView = this;
            m_VisualRoot = new VisualElement();
            m_VisualRoot.pseudoStates |= PseudoStates.Root;
            m_VisualRoot.style.overflow = Overflow.Hidden;
            UIElementsEditorUtility.AddDefaultEditorStyleSheets(m_VisualRoot);
            m_VisualRoot.style.flexGrow = 1;
            visualTree.Add(m_VisualRoot);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_guiView = null;
        }

        protected override void OldOnGUI()
        {
            var evt = Event.current;

            if (evt == null || window == null)
            {
                return;
            }

            Color prevColor = GUI.color;
            GUI.color = Color.white;
            GUI.Box(new Rect(0, 0, m_optimalSize.x, m_optimalSize.y), m_tooltip, m_Style);
            GUI.color = prevColor;
        }

        void Setup(string tooltip, Rect rect, GUIView hostView)
        {
            // Calculate size and position tooltip view
            m_Style = new GUIStyle(EditorStyles.tooltip) { richText = true };

            this.hostView = hostView;
            m_hoverRect = rect;

            m_VisualRoot.Clear();
            m_tooltip.text = tooltip;

            Size = m_Style.CalcSize(m_tooltip);

            window.ShowTooltip();
            s_guiView.mouseRayInvisible = true;
        }

        Vector2 Size
        {
            get { return position.size; }
            set
            {
                m_Style.wordWrap = false;
                m_optimalSize = value;

                if (m_optimalSize.x > MAX_WIDTH)
                {
                    m_Style.wordWrap = true;
                    m_optimalSize.x = MAX_WIDTH;
                    m_optimalSize.y = m_Style.CalcHeight(m_tooltip, MAX_WIDTH);
                }

                var popupPosition = new Rect
                    (
                    Mathf.Floor(m_hoverRect.x + (m_hoverRect.width / 2) - (m_optimalSize.x / 2)),
                    Mathf.Floor(m_hoverRect.y + (m_hoverRect.height) + 10.0f),
                    m_optimalSize.x, m_optimalSize.y
                    );

                if (hostView != null)
                {
                    var viewRect = hostView.screenPosition;
                    if (popupPosition.x < viewRect.x)
                        popupPosition.x = viewRect.x;
                    if (popupPosition.xMax > viewRect.xMax)
                        popupPosition.x -= popupPosition.xMax - viewRect.xMax;
                    if (popupPosition.y < viewRect.y)
                        popupPosition.y = viewRect.y;
                    if (popupPosition.yMax > viewRect.yMax)
                        popupPosition.y -= popupPosition.yMax - viewRect.yMax;

                    popupPosition.y = Mathf.Max(popupPosition.y, Mathf.Floor(m_hoverRect.y + (m_hoverRect.height) + 10.0f));
                }

                // If when fitted to screen, the tooltip would overlap the hover area
                // (and thus potentially mouse) -- for example when the control is near
                // the bottom of screen, place it atop of the hover area instead.
                var fittedToScreen = ContainerWindow.FitRectToMouseScreen(popupPosition, true, null);
                if (fittedToScreen.Overlaps(m_hoverRect))
                {
                    popupPosition.y = m_hoverRect.y - m_optimalSize.y - 10.0f;
                }

                window.position = popupPosition;
                m_VisualRoot.style.width = m_optimalSize.x;
                m_VisualRoot.style.height = m_optimalSize.y;
                position = new Rect(0, 0, m_optimalSize.x, m_optimalSize.y);
            }
        }

        [RequiredByNativeCode]
        public static void Show(string tooltip, Rect rect, GUIView hostView = null)
        {
            CancelAutoClose();

            if (s_guiView && s_guiView.m_tooltip.text == tooltip && rect == s_guiView.m_hoverRect)
                return;

            ForceClose();
            s_guiView = ScriptableObject.CreateInstance<TooltipView>();

            var newWindow = ScriptableObject.CreateInstance<ContainerWindow>();
            newWindow.m_DontSaveToLayout = true;
            newWindow.rootView = s_guiView;
            newWindow.SetMinMaxSizes(new Vector2(10.0f, 10.0f), new Vector2(2000.0f, 2000.0f));
            s_guiView.SetWindow(newWindow);

            s_guiView.Setup(tooltip, rect, hostView);
        }

        public static void AutoCloseAfterDelay(float delayInSeconds)
        {
            s_AutoCloseAfterTime = EditorApplication.timeSinceStartup + delayInSeconds;
        }

        public static void CancelAutoClose()
        {
            s_AutoCloseAfterTime = double.PositiveInfinity;
        }

        [RequiredByNativeCode]
        public static void Close()
        {
            ForceClose();
        }

        internal static void ForceClose()
        {
            // Closing is not guaranteed to result in everything destroyed in one frame.
            var temp = s_guiView?.window;
            s_guiView = null;
            temp?.Close();
        }

        [RequiredByNativeCode]
        static void StaticUpdate()
        {
            if (s_guiView != null)
                s_guiView.Update();
        }
            
        void Update()
        {
            if (EditorApplication.timeSinceStartup > s_AutoCloseAfterTime)
                Close();
        }
    }
}
