// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    class TrimManipulation : IEdgeManipulation
    {
        public bool manipulationActive { get; private set; }
        public ManipulationBehaviourOverlay overlay => m_Behaviour.overlay;

        ISequenceViewModel m_ViewModel;
        IManipulationHandler m_Handler;
        TrimBehaviour.Location m_Location;
        UniqueID m_ManipulatedItemId;
        TrimBehaviourBundle m_Behaviour;
        TrimBehaviourBundle m_PendingBehaviour;

        public IReadOnlyList<Item> GetManipulatedItems()
        {
            if (!manipulationActive)
                return new List<Item>();
            return m_Behaviour.behaviour.GetManipulatedItems();
        }

        public TimeRange GetValidRange()
        {
            return manipulationActive ? TimeRange.MaxRange : TimeRange.Empty;
        }

        public Cursor GetCursor()
        {
            if (!manipulationActive || !m_Behaviour.overlay.cursor.HasValue)
                return EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.None);
            return m_Behaviour.overlay.cursor.Value;
        }

        public virtual bool IsValid(Item target, EdgeHandle.Location location, EventModifiers modifiers) => true;

        public void Begin(ISequenceViewModel viewModel, IManipulationHandler handler, Item target, EdgeHandle.Location location, out DiscreteTime edgeInitialTime)
        {
            var handleLocation = ConvertTrimHandleLocation(location);
            edgeInitialTime = handleLocation == TrimBehaviour.Location.Start ? target.start : target.end;
            Begin(viewModel, handler, target.ID, handleLocation);
            ShowBehaviourOverlay();
        }

        public void Begin(ISequenceViewModel viewModel, IManipulationHandler handler, UniqueID itemID, TrimBehaviour.Location location)
        {
            if (manipulationActive)
                return;

            m_ViewModel = viewModel;
            m_Handler = handler;
            m_Location = location;
            m_ManipulatedItemId = itemID;
            BeginManipulation(m_Behaviour);
            manipulationActive = true;
        }

        void BeginManipulation(TrimBehaviourBundle bundle)
        {
            Item manipulatedItem = m_ViewModel.sequenceData.GetItemFromId(m_ManipulatedItemId);
            bundle.behaviour.BeginManipulation(m_ViewModel, m_Handler, m_Location, manipulatedItem);
            bundle.overlay.BeginManipulation(manipulatedItem);
        }

        public void Process(DiscreteTime initialTime, DiscreteTime requestedTime, DiscreteTime rawTime)
        {
            ApplyPendingBehaviour();
            ProcessTrim(requestedTime);
        }

        public void End()
        {
            EndTrim();
            HideBehaviourOverlay();
        }

        public void Apply() => m_Behaviour.behaviour.CommitManipulation();

        public void Cancel() => m_Behaviour.behaviour.CancelManipulation();

        public void SetBehaviour(TrimBehaviourBundle trimBehaviourBundle, IOverlayManager overlayManager)
        {
            if (m_Behaviour.SameTypeAs(trimBehaviourBundle))
                return;
            m_PendingBehaviour = trimBehaviourBundle;
            overlayManager.AddOverlay(m_PendingBehaviour.overlay);
            if (!manipulationActive)
                ApplyPendingBehaviour();
        }

        public void ProcessTrim(DiscreteTime time)
        {
            if (!manipulationActive)
                return;
            m_Behaviour.behaviour.TrimManipulation(time);
        }

        public void EndTrim()
        {
            manipulationActive = false;
            m_ViewModel = default;
            m_Handler = default;
            m_ManipulatedItemId = default;
        }

        public void ApplyPendingBehaviour()
        {
            if (m_PendingBehaviour.IsDefault())
                return;

            bool overlayDisplayed = m_Behaviour.overlay is { visible: true };
            if (manipulationActive)
            {
                HideBehaviourOverlay();
                BeginManipulation(m_PendingBehaviour);
            }

            m_Behaviour.overlay?.RemoveFromHierarchy();
            m_Behaviour = m_PendingBehaviour;
            m_PendingBehaviour = default;

            if (manipulationActive && overlayDisplayed)
                ShowBehaviourOverlay();
        }

        public void ShowBehaviourOverlay()
        {
            if (m_Behaviour.overlay == null)
                return;

            Item item = m_ViewModel.sequenceData.GetItemFromId(m_ManipulatedItemId);

            m_Behaviour.overlay.BuildIndicatorsList(new[] { item.parent });
            m_Behaviour.overlay.ResetIndicators(m_Behaviour.behaviour, m_ViewModel.sequenceData.lookup);
            m_Behaviour.overlay.UpdateIndicators(m_Behaviour.behaviour, m_ViewModel.sequenceData.lookup);

            m_Behaviour.overlay.Show();
            m_Behaviour.overlay.schedule.Execute(UpdateBehaviourOverlay).Until(() => !m_Behaviour.overlay.isShown);
        }

        public void HideBehaviourOverlay()
        {
            if (m_Behaviour.overlay == null)
                return;

            m_Behaviour.overlay.Hide();
        }

        void UpdateBehaviourOverlay()
        {
            if (m_ViewModel != null)
                m_Behaviour.overlay?.UpdateIndicators(m_Behaviour.behaviour, m_ViewModel.sequenceData.lookup);
        }

        public static void TrimItem(ISequenceViewModel viewModel, IManipulationHandler handler,
            TrimManipulation trimManipulation, UniqueID itemID, DiscreteTime time, TrimBehaviour.Location location)
        {
            if (trimManipulation.manipulationActive)
                return;
            Item item = viewModel.sequenceData.GetItemFromId(itemID);
            trimManipulation.m_Behaviour.behaviour.BeginManipulation(viewModel, handler, location, item);
            trimManipulation.m_Behaviour.behaviour.TrimManipulation(time);
            trimManipulation.m_Behaviour.behaviour.CommitManipulation();
        }

        static TrimBehaviour.Location ConvertTrimHandleLocation(EdgeHandle.Location location)
        {
            return location == EdgeHandle.Location.Left ? TrimBehaviour.Location.Start : TrimBehaviour.Location.End;
        }
    }
}
