// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class ItemOverlay : CanvasOverlay
    {
        public enum State : ushort
        {
            Valid = 0,
            Invalid = 1
        }

        const string k_Name = "itemOverlay";
        const string k_Style = "itemOverlay";
        const string k_InvalidStyle = "itemOverlay--invalid";

        TimeRange m_Range;
        float m_PosY;
        Label m_Label;
        float m_Height;
        State m_State;

        public float height
        {
            get => m_Height;
            set => m_Height = value;
        }

        public TimeRange range
        {
            get => m_Range;
            set
            {
                m_Range = value;
                ForceUpdate();
            }
        }

        public string label
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public State state
        {
            get => m_State;
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    UpdateState();
                }
            }
        }

        public ItemOverlay()
        {
            name = k_Name;

            m_Label = new Label
            {
                name = "label",
                style =
                {
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis)
                }
            };
            Add(m_Label);

            UIResources.OverlayStylesheet.ApplyTo(this);
            this.AddToTimelineClassList(k_Style);

            UpdateState();
        }

        public void SnapTo(Rect worldRect)
        {
            Rect localRect = parent.WorldToLocal(worldRect);
            m_PosY = localRect.y;
            height = localRect.height;
        }

        public void SetWorldY(float worldY)
        {
            m_PosY = parent.WorldToLocal(new Vector2(0f, worldY)).y;
        }

        protected override void Update(ICanvas canvas)
        {
            CanvasTransform canvasTransform = canvas.canvasTransform;
            Vector3 oldPos = resolvedStyle.translate;

            float leftPos = canvasTransform.TimeToPixel(m_Range.start);
            float rightPos = canvasTransform.TimeToPixel(m_Range.end);
            float width = rightPos - leftPos;

            style.translate = new Vector3(leftPos, m_PosY, oldPos.z);
            style.width = width;
            style.height = height;
        }

        void UpdateState()
        {
            if (m_State == State.Valid)
            {
                this.RemoveFromTimelineClassList(k_InvalidStyle);
            }
            else if (m_State == State.Invalid)
            {
                this.AddToTimelineClassList(k_InvalidStyle);
            }
        }
    }
}
