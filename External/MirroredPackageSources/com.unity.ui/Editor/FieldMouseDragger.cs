using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Allows dragging on a numeric field's label to change the value.
    /// </summary>
    public class FieldMouseDragger<T>
    {
        /// <summary>
        /// FieldMouseDragger's constructor.
        /// </summary>
        /// <param name="drivenField">The field.</param>
        public FieldMouseDragger(IValueField<T> drivenField)
        {
            m_DrivenField = drivenField;
            m_DragElement = null;
            m_DragHotZone = new Rect(0, 0, -1, -1);
            dragging = false;
        }

        private readonly IValueField<T> m_DrivenField;
        private VisualElement m_DragElement;
        private Rect m_DragHotZone;

        /// <summary>
        /// Is dragging.
        /// </summary>
        public bool dragging { get; set; }
        /// <summary>
        /// Start value before drag.
        /// </summary>
        public T startValue { get; set; }

        /// <summary>
        /// Set drag zone.
        /// </summary>
        /// <param name="dragElement">The drag element (like the label).</param>
        public void SetDragZone(VisualElement dragElement)
        {
            SetDragZone(dragElement, new Rect(0, 0, -1, -1));
        }

        /// <summary>
        /// Set drag zone.
        /// </summary>
        /// <param name="dragElement">The drag element (like the label).</param>
        /// <param name="hotZone">The rectangle that contains the drag zone.</param>
        public void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<PointerDownEvent>(UpdateValueOnPointerDown);
                m_DragElement.UnregisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
                m_DragElement.UnregisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);

                m_DragElement.UnregisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.UnregisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.UnregisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);

                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<PointerDownEvent>(UpdateValueOnPointerDown);
                m_DragElement.RegisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
                m_DragElement.RegisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);

                m_DragElement.RegisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.RegisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.RegisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
            }
        }

        private bool CanStartDrag(int button, Vector2 localPosition)
        {
            return button == 0 && (m_DragHotZone.width < 0 || m_DragHotZone.height < 0 ||
                m_DragHotZone.Contains(m_DragElement.WorldToLocal(localPosition)));
        }

        private void UpdateValueOnPointerDown(PointerDownEvent evt)
        {
            if (CanStartDrag(evt.button, evt.localPosition))
            {
                if (evt.pointerId != PointerId.mousePointerId)
                {
                    evt.PreventDefault();
                    m_DragElement.CapturePointer(evt.pointerId);
                    ProcessDownEvent(evt);
                }
                else
                {
                    evt.StopImmediatePropagation();
                }
            }
        }

        private void UpdateValueOnMouseDown(MouseDownEvent evt)
        {
            if (CanStartDrag(evt.button, evt.mousePosition))
            {
                m_DragElement.CaptureMouse();
                ProcessDownEvent(evt);
            }
        }

        private void ProcessDownEvent(EventBase evt)
        {
            // Make sure no other elements can capture the mouse!
            evt.StopPropagation();

            dragging = true;
            startValue = m_DrivenField.value;

            m_DrivenField.StartDragging();
            EditorGUIUtility.SetWantsMouseJumping(1);
        }

        private void UpdateValueOnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId == PointerId.mousePointerId)
                return;

            ProcessMoveEvent(evt.shiftKey, evt.altKey, evt.deltaPosition);
        }

        private void UpdateValueOnMouseMove(MouseMoveEvent evt)
        {
            ProcessMoveEvent(evt.shiftKey, evt.altKey, evt.mouseDelta);
        }

        private void ProcessMoveEvent(bool shiftKey, bool altKey, Vector2 deltaPosition)
        {
            if (dragging)
            {
                DeltaSpeed s = shiftKey ? DeltaSpeed.Fast : (altKey ? DeltaSpeed.Slow : DeltaSpeed.Normal);
                m_DrivenField.ApplyInputDeviceDelta(deltaPosition, s, startValue);
            }
        }

        private void UpdateValueOnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == PointerId.mousePointerId)
                return;

            ProcessUpEvent(evt, evt.pointerId);
        }

        private void UpdateValueOnMouseUp(MouseUpEvent evt)
        {
            ProcessUpEvent(evt, PointerId.mousePointerId);
        }

        private void ProcessUpEvent(EventBase evt, int pointerId)
        {
            if (dragging)
            {
                dragging = false;
                m_DragElement.ReleasePointer(pointerId);
                if (evt is IMouseEvent)
                    m_DragElement.panel.ProcessPointerCapture(PointerId.mousePointerId);

                EditorGUIUtility.SetWantsMouseJumping(0);
                m_DrivenField.StopDragging();
            }
        }

        private void UpdateValueOnKeyDown(KeyDownEvent evt)
        {
            if (dragging && evt.keyCode == KeyCode.Escape)
            {
                dragging = false;
                m_DrivenField.value = startValue;
                m_DrivenField.StopDragging();
                IPanel panel = (evt.target as VisualElement)?.panel;
                panel.ReleasePointer(PointerId.mousePointerId);
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
        }
    }
}
