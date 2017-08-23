// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class Resizer : VisualElement
    {
        private Vector2 m_Start;
        private Rect m_StartPos;

        public MouseButton activateButton { get; set; }

        private Texture image { get; set; }

        private Vector2 m_MinimumSize;

        // We need to delay style creation because we need to make sure we have a GUISkin loaded.
        private GUIStyle m_StyleWidget;
        private GUIStyle m_StyleLabel;
        private GUIContent m_LabelText = new GUIContent();

        private readonly Rect k_WidgetTextOffset = new Rect(0, 0, 5, 5);

        bool m_Active;

        public Resizer() :
            this(new Vector2(30.0f, 30.0f))
        {
        }

        public Resizer(Vector2 minimumSize)
        {
            m_MinimumSize = minimumSize;
            style.positionType = PositionType.Absolute;
            style.positionTop = float.NaN;
            style.positionLeft = float.NaN;
            style.positionBottom = 0;
            style.positionRight = 0;
            // make clickable area bigger than render area
            style.paddingLeft = 10;
            style.paddingTop = 14;
            style.width = 20;
            style.height = 20;

            m_Active = false;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            GraphElementPresenter presenter = ce.presenter;
            if (presenter == null)
                return;

            if ((presenter.capabilities & Capabilities.Resizable) != Capabilities.Resizable)
                return;

            if (e.button == (int)activateButton)
            {
                m_Start = this.ChangeCoordinatesTo(parent, e.localMousePosition);
                m_StartPos = parent.layout;
                // Warn user if target uses a relative CSS position type
                if (parent.style.positionType != PositionType.Manual)
                {
                    Debug.LogWarning("Attempting to resize an object with a non manual position");
                }

                m_Active = true;
                this.TakeCapture();
                e.StopPropagation();
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            GraphElementPresenter presenter = ce.presenter;
            if (presenter == null)
                return;

            if ((presenter.capabilities & Capabilities.Resizable) != Capabilities.Resizable)
                return;

            if (!m_Active)
                return;

            if (e.button == (int)activateButton && m_Active)
            {
                m_Active = false;
                this.ReleaseCapture();
                e.StopPropagation();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            GraphElementPresenter presenter = ce.presenter;
            if (presenter == null)
                return;

            if ((presenter.capabilities & Capabilities.Resizable) != Capabilities.Resizable)
                return;

            if (m_Active && parent.style.positionType == PositionType.Manual)
            {
                Vector2 diff = this.ChangeCoordinatesTo(parent, e.localMousePosition) - m_Start;
                var newSize = new Vector2(m_StartPos.width + diff.x, m_StartPos.height + diff.y);

                if (newSize.x < m_MinimumSize.x)
                    newSize.x = m_MinimumSize.x;
                if (newSize.y < m_MinimumSize.y)
                    newSize.y = m_MinimumSize.y;

                presenter.position = new Rect(presenter.position.x, presenter.position.y, newSize.x, newSize.y);

                m_LabelText.text = String.Format("{0:0}", parent.layout.width) + "x" + String.Format("{0:0}", parent.layout.height);

                e.StopPropagation();
            }
        }

        public override void DoRepaint()
        {
            // TODO: I would like to listen for skin change and create GUIStyle then and only then
            if (m_StyleWidget == null)
            {
                m_StyleWidget = new GUIStyle("WindowBottomResize") { fixedHeight = 0 };
                image = m_StyleWidget.normal.background;
            }

            if (image == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            GUI.DrawTexture(contentRect, image, ScaleMode.ScaleAndCrop, true, 0, GUI.color, 0, 0);

            if (m_StyleLabel == null)
            {
                m_StyleLabel = new GUIStyle("Label");
            }

            if (m_Active)
            {
                // Get adjusted text offset
                Rect adjustedWidget = k_WidgetTextOffset;

                // Now define widget to locate label
                var widget = new Rect(layout.max.x + adjustedWidget.width,
                        layout.max.y + adjustedWidget.height,
                        200.0f, 20.0f);

                m_StyleLabel.Draw(widget, m_LabelText, false, false, false, false);
            }
        }
    }
}
