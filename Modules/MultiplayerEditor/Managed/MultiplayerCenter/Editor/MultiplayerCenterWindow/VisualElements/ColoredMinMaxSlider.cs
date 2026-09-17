// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Custom MinMaxSlider that reposition a colored background based on its dragger position.
    /// </summary>
    [UxmlElement]
    partial class ColoredMinMaxSlider : MinMaxSlider
    {
        VisualElement m_Dragger;

        public ColoredMinMaxSlider()
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<ChangeEvent<Vector2>>(OnValueChanged);

            pickingMode = PickingMode.Ignore;
        }

        // used to disable all pointer and keyboard navigation event propagation
        // so the sliders is not draggable by users
        protected override void HandleEventTrickleDown(EventBase evt)
        {
            switch (evt)
            {
                case ChangeEvent<Vector2>:
                case GeometryChangedEvent:
                    base.HandleEventBubbleUp(evt);
                    return;
                case WheelEvent:
                    // Allow wheel events to propagate so parent ScrollView can scroll
                    return;
                default:
                    evt.StopImmediatePropagation();
                    break;
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_Dragger == null)
            {
                m_Dragger = this.Q("unity-dragger");
                m_Dragger.pickingMode = PickingMode.Ignore;
            }

            if (m_Dragger != null)
            {
                // It is important to always update the dragger image on Geometry change
                // to adapt to the Multiplayer Center Window resizing.
                UpdateDraggerImage(value);
            }

            evt.StopImmediatePropagation();
        }

        void OnValueChanged(ChangeEvent<Vector2> evt)
        {
            UpdateDraggerImage(evt.newValue);
        }

        void UpdateDraggerImage(Vector2 currentValue)
        {
            if (m_Dragger == null)
                return;

            // There is a 1 pixel coming from the border width that needs to be subtracted to get the right scaling.
            // Otherwise, the image would stop just 2 pixels before the far end for max values = 100.
            float draggerWidth = currentValue.y - currentValue.x - 1f;
            if (draggerWidth <= 0)
                return;

            // rescale the background image to fit the size of the full minmax background
            float scaleFactor = 100f / draggerWidth * 100f;
            m_Dragger.style.backgroundSize =
                new BackgroundSize(Length.Percent(scaleFactor), Length.Percent(100));

            // shift the background image to match the dragger position
            var leftPositionShift = -m_Dragger.style.left.value.value;
            m_Dragger.style.backgroundPositionX =
                new BackgroundPosition(BackgroundPositionKeyword.Left, Length.Pixels(leftPositionShift));
        }
    }
}
