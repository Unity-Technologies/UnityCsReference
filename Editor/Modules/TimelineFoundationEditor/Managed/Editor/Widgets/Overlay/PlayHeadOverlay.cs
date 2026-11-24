// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    class PlayHeadOverlay : CanvasOverlay
    {
        const string k_Name = "playHeadOverlay";
        static readonly StylesheetResource s_Stylesheet = UIResources.StylesheetFactory.Get<PlayHeadOverlay>();

        TimeTooltipOverlay m_TimeTooltip;
        VerticalLineOverlay m_LineOverlay;
        protected HandleOverlay m_Handle;

        float m_WorldPixel;
        DiscreteTime m_Time;
        public DiscreteTime time
        {
            get => m_Time;
            set
            {
                m_Time = value;
                ForceUpdate();
                m_TimeTooltip.time = m_Time;
                m_LineOverlay.time = m_Time;
            }
        }

        public PlayHeadOverlay(PickingMode pickingMode = PickingMode.Ignore)
        {
            this.AddToTimelineClassList(k_Name);
            s_Stylesheet.ApplyTo(this);

            name = k_Name;
            m_Handle = new HandleOverlay(pickingMode);
            m_Handle.pickingMode = PickingMode.Position;

            Add(m_Handle);

            m_LineOverlay = new VerticalLineOverlay();
            Add(m_LineOverlay);

            m_TimeTooltip = new TimeTooltipOverlay();
            m_TimeTooltip.Hide();
            Add(m_TimeTooltip);
        }

        public void SetTooltipDisplayState(bool show)
        {
            if (show)
                m_TimeTooltip.Show();
            else
                m_TimeTooltip.Hide();
        }

        protected override void Update(ICanvas canvas)
        {
            m_WorldPixel = canvas.TimeToWorldPixel(m_Time);
            style.translate = new Vector3(WorldToLocalX(m_WorldPixel), 0, 0);
            MarkDirtyRepaint();
        }
    }
}
