// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Resizer : VisualElement
    {
        private Vector2 m_Start;
        private Vector2 m_MinimumSize;
        private Rect m_StartPos;

        public MouseButton activateButton { get; set; }

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
            style.backgroundScaleMode = ScaleMode.ScaleAndCrop;

            m_Active = false;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            ClearClassList();
            AddToClassList("resizer");

            var icon = new VisualElement() {
                style = { backgroundScaleMode = ScaleMode.ScaleAndCrop }
            };
            icon.AddToClassList("resizer-icon");
            Add(icon);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (MouseCaptureController.IsMouseCaptured())
                return;

            var ce = parent as GraphElement;
            if (ce == null)
                return;

            if (!ce.IsResizable())
                return;

            if (e.button == (int)activateButton)
            {
                m_Start = this.ChangeCoordinatesTo(parent, e.localMousePosition);
                m_StartPos = parent.layout;
                // Warn user if target uses a relative CSS position type
                if (parent.style.positionType != PositionType.Manual)
                {
                    if (parent.style.positionType == PositionType.Absolute)
                    {
                        if (!(parent.style.flexDirection == FlexDirection.Column || parent.style.flexDirection == FlexDirection.Row))
                        {
                            Debug.LogWarning("Attempting to resize an object with an absolute position but no layout direction (row or column)");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to resize an object with a non manual position");
                    }
                }

                m_Active = true;
                this.CaptureMouse();
                e.StopPropagation();
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            if (!ce.IsResizable())
                return;

            if (!m_Active)
                return;

            if (e.button == (int)activateButton && m_Active)
            {
                m_Active = false;
                this.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            if (!ce.IsResizable())
                return;

            // Then can be resize in all direction
            if (parent.style.positionType == PositionType.Manual)
            {
                if (ClassListContains("resizeAllDir") == false)
                {
                    AddToClassList("resizeAllDir");
                    RemoveFromClassList("resizeHorizontalDir");
                    RemoveFromClassList("resizeVerticalDir");
                }
            }
            else if (parent.style.positionType == PositionType.Absolute)
            {
                if (parent.style.flexDirection == FlexDirection.Column)
                {
                    if (ClassListContains("resizeHorizontalDir") == false)
                    {
                        AddToClassList("resizeHorizontalDir");
                        RemoveFromClassList("resizeAllDir");
                        RemoveFromClassList("resizeVerticalDir");
                    }
                }
                else if (parent.style.flexDirection == FlexDirection.Row)
                {
                    if (ClassListContains("resizeVerticalDir") == false)
                    {
                        AddToClassList("resizeVerticalDir");
                        RemoveFromClassList("resizeAllDir");
                        RemoveFromClassList("resizeHorizontalDir");
                    }
                }
            }

            if (m_Active)
            {
                Vector2 diff = this.ChangeCoordinatesTo(parent, e.localMousePosition) - m_Start;
                var newSize = new Vector2(m_StartPos.width + diff.x, m_StartPos.height + diff.y);
                float minWidth = Math.Max(ce.style.minWidth.value, m_MinimumSize.x);
                float minHeight = Math.Max(ce.style.minHeight.value, m_MinimumSize.y);
                float maxWidth = ce.style.maxWidth.GetSpecifiedValueOrDefault(float.MaxValue);
                float maxHeight = ce.style.maxHeight.GetSpecifiedValueOrDefault(float.MaxValue);

                newSize.x = (newSize.x < minWidth) ? minWidth : ((newSize.x > maxWidth) ? maxWidth : newSize.x);
                newSize.y = (newSize.y < minHeight) ? minHeight : ((newSize.y > maxHeight) ? maxHeight : newSize.y);

                bool resized = false;

                if (ce.GetPosition().size != newSize)
                {
                    if (parent.style.positionType == PositionType.Manual)
                    {
                        ce.SetPosition(new Rect(ce.layout.x, ce.layout.y, newSize.x, newSize.y));
                        resized = true;
                    }
                    else if (parent.style.positionType == PositionType.Absolute)
                    {
                        if (parent.style.flexDirection == FlexDirection.Column)
                        {
                            ce.style.width = newSize.x;
                            resized = true;
                        }
                        else if (parent.style.flexDirection == FlexDirection.Row)
                        {
                            ce.style.height = newSize.y;
                            resized = true;
                        }
                    }
                }

                if (resized)
                {
                    ce.UpdatePresenterPosition();

                    GraphView graphView = ce.GetFirstAncestorOfType<GraphView>();
                    if (graphView != null && graphView.elementResized != null)
                    {
                        graphView.elementResized(ce);
                    }
                }

                e.StopPropagation();
            }
        }
    }
}
