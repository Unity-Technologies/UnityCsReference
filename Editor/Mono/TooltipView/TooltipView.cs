// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor
{
    class TooltipView : GUIView
    {
        enum CloseState
        {
            Idle,
            CloseRequested,
            CloseApproved,
        }

        internal const float MAX_WIDTH = 300.0f;
        const double DYNAMICHINT_AUTO_EXTEND_DELAY = 1.4;

        private GUIContent m_tooltip = new GUIContent();
        private Vector2 m_optimalSize;
        private GUIStyle m_Style;
        private Rect m_hoverRect;

        internal DynamicHintContent CurrentDynamicHint { get => m_DynamicHintContent; }
        DynamicHintContent m_DynamicHintContent;

        private VisualElement m_VisualRoot;
        private GUIView hostView;
        static CloseState s_CloseState = CloseState.Idle;
        bool m_HoldingShift;

        internal static TooltipView S_guiView { get => s_guiView; }
        static TooltipView s_guiView;

        internal static bool s_ForceExtensionOfNextDynamicHint = false;
        internal static SavedBool s_EnableExtendedDynamicHints = new SavedBool("EnableExtendedDynamicHints", true);

        internal bool DynamicHintIsBeingDisplayed { get => m_DynamicHintIsBeingDisplayed; }
        bool m_DynamicHintIsBeingDisplayed = false;

        bool m_DynamicHintAutoExtendCountdownStarted = false;
        double m_DynamicHintAutoExtendTime;
        bool DynamicHintAutoExtendTimeReached { get { return EditorApplication.timeSinceStartup >= m_DynamicHintAutoExtendTime; } }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_guiView = this;
            m_VisualRoot = new VisualElement();
            m_VisualRoot.pseudoStates |= PseudoStates.Root;
            m_VisualRoot.style.overflow = Overflow.Hidden;
            EditorUIService.instance.AddDefaultEditorStyleSheets(m_VisualRoot);
            m_VisualRoot.style.flexGrow = 1;
            visualTree.Add(m_VisualRoot);
            EditorApplication.update += Update;
        }

        protected override void OnDisable()
        {
            s_CloseState = CloseState.Idle;
            base.OnDisable();
            s_guiView = null;
            EditorApplication.update -= Update;
        }

        protected override void OldOnGUI()
        {
            var evt = Event.current;
            if (evt == null || window == null) { return; }

            m_HoldingShift = evt.shift;

            if (m_DynamicHintContent == null)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.white;
                GUI.Box(new Rect(0, 0, m_optimalSize.x, m_optimalSize.y), m_tooltip, m_Style);
                GUI.color = prevColor;
            }
            else
            {
                m_DynamicHintContent.Extended = s_EnableExtendedDynamicHints && (m_HoldingShift || (m_DynamicHintIsBeingDisplayed && DynamicHintAutoExtendTimeReached));
                m_DynamicHintContent.Update();
                Size = m_DynamicHintContent.GetContentSize();
            }

            Repaint();

            if (s_CloseState == CloseState.CloseRequested && !m_HoldingShift)
            {
                if (m_DynamicHintContent != null && m_DynamicHintContent.GetRect().Contains(evt.mousePosition))
                {
                    return;
                }
                s_CloseState = CloseState.CloseApproved;
            }
        }

        void Setup(string tooltip, Rect rect, GUIView hostView)
        {
            if (!m_DynamicHintIsBeingDisplayed || !DynamicHintAutoExtendTimeReached)
            {
                StopExtendedDynamicHintCountdown();
            }

            // Calculate size and position tooltip view
            m_Style = EditorStyles.tooltip;
            s_CloseState = CloseState.Idle;

            this.hostView = hostView;
            m_hoverRect = rect;

            if (m_DynamicHintContent != null && m_DynamicHintContent.ToTooltipString() == tooltip)
            {
                StartExtendedDynamicHintCountdown();
                return;
            }

            m_VisualRoot.Clear();
            m_DynamicHintContent = DynamicHintUtility.Deserialize(tooltip);
            m_tooltip.text = m_DynamicHintContent == null ? tooltip : "";

            if (m_DynamicHintContent != null)
            {
                StartExtendedDynamicHintCountdown();

                if (s_ForceExtensionOfNextDynamicHint)
                {
                    m_DynamicHintAutoExtendTime = EditorApplication.timeSinceStartup;
                    s_ForceExtensionOfNextDynamicHint = false;
                }

                m_VisualRoot.Add(m_DynamicHintContent.CreateContent());
                m_DynamicHintContent.Extended = s_EnableExtendedDynamicHints && (m_HoldingShift || (m_DynamicHintIsBeingDisplayed && DynamicHintAutoExtendTimeReached));
                m_DynamicHintIsBeingDisplayed = true;
                Size = m_DynamicHintContent.GetContentSize();

                //Handle the fact that to click on a tooltip button, you need to hold shift
                m_VisualRoot.Query<Button>().ForEach((button) =>
                {
                    button.clickable.activators.Add(new ManipulatorActivationFilter
                        { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
                });
            }
            else
            {
                Size = m_Style.CalcSize(m_tooltip);
            }

            window.ShowTooltip();
            window.SetAlpha(1.0f);
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
                var fittedToScreen = ContainerWindow.FitRectToScreen(popupPosition, true, true);
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

        public static void Show(string tooltip, Rect rect, GUIView hostView = null)
        {
            if (s_guiView == null)
            {
                s_guiView = ScriptableObject.CreateInstance<TooltipView>();
            }

            if (s_guiView.window == null)
            {
                var newWindow = ScriptableObject.CreateInstance<ContainerWindow>();
                newWindow.m_DontSaveToLayout = true;
                newWindow.rootView = s_guiView;
                newWindow.SetMinMaxSizes(new Vector2(10.0f, 10.0f), new Vector2(2000.0f, 2000.0f));
                s_guiView.SetWindow(newWindow);
            }

            if (s_guiView.m_tooltip.text == tooltip && rect == s_guiView.m_hoverRect) { return; }

            s_guiView.Setup(tooltip, rect, hostView);
        }

        public static void Close()
        {
            s_CloseState = CloseState.CloseRequested;
        }

        internal static void ForceClose()
        {
            if (s_guiView == null || s_guiView.window == null) { return; }
            s_guiView.window.Close();
        }

        public static void SetAlpha(float percent)
        {
            if (s_guiView == null) { return; }

            s_guiView.window.SetAlpha(percent);
        }

        void Update()
        {
            if (s_CloseState != CloseState.CloseApproved) { return; }
            ForceClose();
            m_DynamicHintIsBeingDisplayed = false;
            StopExtendedDynamicHintCountdown();
        }

        void StopExtendedDynamicHintCountdown()
        {
            m_DynamicHintAutoExtendCountdownStarted = false;
            m_DynamicHintAutoExtendTime = double.PositiveInfinity;
        }

        void StartExtendedDynamicHintCountdown()
        {
            if (m_DynamicHintAutoExtendCountdownStarted) { return; }
            m_DynamicHintAutoExtendCountdownStarted = true;
            m_DynamicHintAutoExtendTime = EditorApplication.timeSinceStartup + DYNAMICHINT_AUTO_EXTEND_DELAY;
        }
    }
}
