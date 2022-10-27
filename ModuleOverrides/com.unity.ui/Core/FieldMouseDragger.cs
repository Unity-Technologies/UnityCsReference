// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides the base class for field mouse draggers.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public abstract class BaseFieldMouseDragger
    {
        /// <summary>
        /// Sets the drag zone for the driven field.
        /// </summary>
        /// <param name="dragElement">The target of the drag operation.</param>
        public void SetDragZone(VisualElement dragElement)
        {
            SetDragZone(dragElement, new Rect(0, 0, -1, -1));
        }

        /// <summary>
        /// Sets the drag zone for the driven field.
        /// </summary>
        /// <param name="dragElement">The target of the drag operation.</param>
        /// <param name="hotZone">The rectangle that contains the drag zone.</param>
        public abstract void SetDragZone(VisualElement dragElement, Rect hotZone);
    }

    /// <summary>
    /// Provides dragging on a visual element to change a value field.
    /// </summary>
    /// <description>
    /// To create a field mouse dragger use <see cref="FieldMouseDragger{T}"/>
    /// and then set the drag zone using <see cref="BaseFieldMouseDragger.SetDragZone(VisualElement)"/>
    /// </description>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class FieldMouseDragger<T> : BaseFieldMouseDragger
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

        /// <inheritdoc />
        public sealed override void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<PointerDownEvent>(UpdateValueOnPointerDown);
                m_DragElement.UnregisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);
                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<PointerDownEvent>(UpdateValueOnPointerDown);
                m_DragElement.RegisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
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
                // We want to allow dragging when using a mouse in any context and when in an Editor context with any pointer type.
                if (evt.pointerType == PointerType.mouse)
                {
                    m_DragElement.CaptureMouse();
                    ProcessDownEvent(evt);
                }
                else if (m_DragElement.panel.contextType == ContextType.Editor)
                {
                    evt.PreventDefault();
                    m_DragElement.CapturePointer(evt.pointerId);
                    ProcessDownEvent(evt);
                }
            }
        }

        private void ProcessDownEvent(EventBase evt)
        {
            // Make sure no other elements can capture the mouse!
            evt.StopPropagation();

            dragging = true;
            m_DragElement.RegisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
            startValue = m_DrivenField.value;

            m_DrivenField.StartDragging();
            (m_DragElement.panel as BaseVisualElementPanel)?.uiElementsBridge?.SetWantsMouseJumping(1);
        }

        private void UpdateValueOnPointerMove(PointerMoveEvent evt)
        {
            ProcessMoveEvent(evt.shiftKey, evt.altKey, evt.deltaPosition);
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
            ProcessUpEvent(evt, evt.pointerId);
        }

        private void ProcessUpEvent(EventBase evt, int pointerId)
        {
            if (dragging)
            {
                dragging = false;
                m_DragElement.UnregisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
                m_DragElement.ReleasePointer(pointerId);
                if (evt is IMouseEvent)
                    m_DragElement.panel.ProcessPointerCapture(PointerId.mousePointerId);

                (m_DragElement.panel as BaseVisualElementPanel)?.uiElementsBridge?.SetWantsMouseJumping(0);
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
                IPanel panel = evt.elementTarget?.panel;
                panel.ReleasePointer(PointerId.mousePointerId);
                (panel as BaseVisualElementPanel)?.uiElementsBridge?.SetWantsMouseJumping(0);
            }
        }
    }
}
