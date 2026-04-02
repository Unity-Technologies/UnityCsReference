// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    abstract class TrimBehaviourOverlay : ManipulationBehaviourOverlay
    {
        const string k_Line = "trimLine";

        SequenceLookup m_Lookup;
        TrimBehaviour m_TrimBehaviour;

        readonly TimeTooltipOverlay m_TimeBarTooltipOverlay;
        readonly TimeTooltipContainer m_CanvasTimeTooltipContainer;
        readonly DurationTooltipOverlay m_DurationTooltipOverlay;

        VerticalLineOverlay m_Line;

        protected TrimBehaviourOverlay()
        {
            m_Line = new VerticalLineOverlay { name = k_Line };
            Add(m_Line);
            m_TimeBarTooltipOverlay = new TimeTooltipOverlay();
            m_CanvasTimeTooltipContainer = new TimeTooltipContainer();
            Add(m_TimeBarTooltipOverlay);
            Add(m_CanvasTimeTooltipContainer);
            m_DurationTooltipOverlay = new DurationTooltipOverlay();
            m_CanvasTimeTooltipContainer.AddTooltip(m_DurationTooltipOverlay);
        }

        public void UpdateIndicators(TrimBehaviour trimBehaviour, SequenceLookup lookup)
        {
            m_Lookup = lookup;
            m_TrimBehaviour = trimBehaviour;
            UpdateTooltip();
            MarkDirtyRepaint();
        }

        void UpdateTooltip()
        {
            if (m_TrimBehaviour == null)
            {
                HideOverlays();
                return;
            }

            UniqueID manipulatedID = m_TrimBehaviour.GetManipulatedItems()[0].ID;
            Item item = m_Lookup.GetItemFromId(manipulatedID);

            DiscreteTime? time = GetTime(item);

            if (time.HasValue)
            {
                m_TimeBarTooltipOverlay.time = time.Value;
                m_Line.time = time.Value;
                UpdateTimeContainerTooltips(manipulatedID, time, item);
                ShowOverlays();
            }
            else
            {
                HideOverlays();
            }
        }

        protected virtual void UpdateTimeContainerTooltips(UniqueID manipulatedID, DiscreteTime? time, Item item)
        {
            var element = GetItemElement(manipulatedID);
            if (element != null)
            {
                m_CanvasTimeTooltipContainer.RequestedY = this.WorldToLocal(element.worldBound.position).y + element.layout.height;
            }

            m_DurationTooltipOverlay.SetValues(time.Value, item.GetVisibleRange().duration);
            m_CanvasTimeTooltipContainer.time = time.Value;
        }

        protected void AddTooltipToTimeContainer(TimeTooltipOverlay tooltipOverlay) => m_CanvasTimeTooltipContainer.AddTooltip(tooltipOverlay);

        DiscreteTime? GetTime(Item item)
        {
            DiscreteTime? time = null;
            switch (m_TrimBehaviour.location)
            {
                case TrimBehaviour.Location.Start:
                    time = item.start;
                    break;
                case TrimBehaviour.Location.End:
                    time = item.end;
                    break;
            }

            return time;
        }

        void ShowOverlays()
        {
            m_Line.Show();
            m_TimeBarTooltipOverlay.Show();
            m_CanvasTimeTooltipContainer.Show();
        }

        void HideOverlays()
        {
            m_Line.Hide();
            m_TimeBarTooltipOverlay.Hide();
            m_CanvasTimeTooltipContainer.Hide();
        }

        protected override void GenerateVisualContent(MeshGenerationContext context)
        {
            ClearIndicators();
            if (m_TrimBehaviour == null)
                return;

            Item item = m_Lookup.GetItemFromId(m_TrimBehaviour.GetManipulatedItems()[0].ID);
            switch (m_TrimBehaviour.location)
            {
                case TrimBehaviour.Location.Start:
                    DiscreteTime? startIndicatorTime = GetStartIndicatorTime(item);
                    if (startIndicatorTime.HasValue)
                        AddStartIndicatorAtTime(startIndicatorTime.Value, item.parent.ID);
                    break;
                case TrimBehaviour.Location.End:
                    DiscreteTime? endIndicatorTime = GetEndIndicatorTime(item);
                    if (endIndicatorTime.HasValue)
                        AddEndIndicatorAtTime(endIndicatorTime.Value, item.parent.ID);
                    break;
            }

            base.GenerateVisualContent(context);
        }

        public virtual void ResetIndicators(TrimBehaviour behaviour, SequenceLookup lookup) { }

        public virtual void BeginManipulation(Item item)
        {
            m_DurationTooltipOverlay.initialDuration = item.GetVisibleRange().duration;
        }

        protected virtual DiscreteTime? GetStartIndicatorTime(Item item) { return null; }
        protected virtual DiscreteTime? GetEndIndicatorTime(Item item) { return null; }
    }
}
