// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class Resizer : VisualElement
    {
        private Vector2 m_Start;
        private Vector2 m_MinimumSize;
        private Rect m_StartPos;
        private Action m_OnResizedCallback;
        static readonly Vector2 k_ResizerSize = new Vector2(30.0f, 30.0f);

        public MouseButton activateButton { get; set; }

        bool m_Active;

        public Resizer() :
            this(k_ResizerSize)
        {
        }

        public Resizer(Action onResizedCallback) :
            this(k_ResizerSize, onResizedCallback)
        {
        }

        public Resizer(Vector2 minimumSize, Action onResizedCallback = null)
        {
            m_MinimumSize = minimumSize;
            style.position = Position.Absolute;
            style.top = float.NaN;
            style.left = float.NaN;

            style.backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
            style.backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop);
            style.backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop);
            style.backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop);

            m_Active = false;
            m_OnResizedCallback = onResizedCallback;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            ClearClassList();
            AddToClassList("resizer");

            var icon = new VisualElement() {
                style =
                {
                    backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop),
                    backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop),
                    backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop),
                    backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop)
                }
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

            IPanel panel = (e.target as VisualElement)?.panel;
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
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
                if (!parent.isLayoutManual && parent.resolvedStyle.position == Position.Relative)
                {
                    Debug.LogWarning("Attempting to resize an object with a non absolute position");
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
                if (m_OnResizedCallback != null)
                    m_OnResizedCallback();

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
            if (ce.resizeRestriction == ResizeRestriction.None)
            {
                if (ClassListContains("resizeAllDir") == false)
                {
                    AddToClassList("resizeAllDir");
                    RemoveFromClassList("resizeHorizontalDir");
                    RemoveFromClassList("resizeVerticalDir");
                }
            }
            else if (ce.resolvedStyle.position == Position.Absolute)
            {
                if (ce.resolvedStyle.flexDirection == FlexDirection.Column)
                {
                    if (ClassListContains("resizeHorizontalDir") == false)
                    {
                        AddToClassList("resizeHorizontalDir");
                        RemoveFromClassList("resizeAllDir");
                        RemoveFromClassList("resizeVerticalDir");
                    }
                }
                else if (ce.resolvedStyle.flexDirection == FlexDirection.Row)
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
                float minWidth = ce.resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : ce.resolvedStyle.minWidth.value;
                minWidth = Math.Max(minWidth, m_MinimumSize.x);
                float minHeight = ce.resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : ce.resolvedStyle.minHeight.value;
                minHeight = Math.Max(minHeight, m_MinimumSize.y);
                float maxWidth = ce.resolvedStyle.maxWidth == StyleKeyword.None ? float.MaxValue : ce.resolvedStyle.maxWidth.value;
                float maxHeight = ce.resolvedStyle.maxHeight == StyleKeyword.None ? float.MaxValue : ce.resolvedStyle.maxHeight.value;

                newSize.x = (newSize.x < minWidth) ? minWidth : ((newSize.x > maxWidth) ? maxWidth : newSize.x);
                newSize.y = (newSize.y < minHeight) ? minHeight : ((newSize.y > maxHeight) ? maxHeight : newSize.y);

                bool resized = false;

                if (ce.GetPosition().size != newSize)
                {
                    if (ce.isLayoutManual)
                    {
                        ce.SetPosition(new Rect(ce.layout.x, ce.layout.y, newSize.x, newSize.y));
                        resized = true;
                    }
                    else if (ce.resizeRestriction == ResizeRestriction.None)
                    {
                        ce.style.width = newSize.x;
                        ce.style.height = newSize.y;
                        resized = true;
                    }
                    else if (parent.resolvedStyle.flexDirection == FlexDirection.Column)
                    {
                        ce.style.width = newSize.x;
                        resized = true;
                    }
                    else if (parent.resolvedStyle.flexDirection == FlexDirection.Row)
                    {
                        ce.style.height = newSize.y;
                        resized = true;
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
