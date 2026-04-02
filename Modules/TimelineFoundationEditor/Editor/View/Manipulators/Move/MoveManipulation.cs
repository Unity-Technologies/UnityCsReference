// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    class MoveManipulation
    {
        const float k_EdgeAttractionInPixels = 10.0f;
        public bool enabled { get; set; } = true;
        public MoveOverlay moveOverlay { get; }
        public MoveBehaviour moveBehaviour => m_Behaviour.behaviour;
        public MoveBehaviourOverlay behaviourOverlay => m_Behaviour.overlay;

        public bool isUsingPreview => m_BehaviourStateMachine is { needsPreview: true };
        public bool isAttached => m_BehaviourStateMachine is { isAttached: true };
        public bool isDetached => m_BehaviourStateMachine is { isDetached: true };
        public bool manipulationActive => m_BehaviourStateMachine is { isActive: true };
        public bool supportsDetach => m_Behaviour.behaviour != null && m_Behaviour.behaviour.SupportsMoveToTrack();

        MoveBehaviourBundle m_PendingBehaviour;
        MoveBehaviourBundle m_Behaviour;
        ManipulationContext m_ManipulationContext;
        MoveBehaviourStateMachine m_BehaviourStateMachine;
        SnapEngine m_SnapEngine;
        MoveManipulationTimeInfo m_MoveManipulationTimeInfo;
        ISequenceViewModel m_ViewModel;

        public MoveManipulation()
        {
            moveOverlay = new MoveOverlay();
        }

        public void SetBehaviour(MoveBehaviourBundle moveBehaviourBundle, IOverlayManager overlayManager)
        {
            if (m_Behaviour.SameTypeAs(moveBehaviourBundle))
                return;
            m_PendingBehaviour = moveBehaviourBundle;
            overlayManager.AddOverlay(m_PendingBehaviour.overlay);
            if (!manipulationActive)
                ApplyPendingBehaviour();
        }

        public void BeginMove(ISequenceViewModel viewModel, IManipulationHandler handler, ManipulationContext manipulationContext, ViewContext viewContext, DiscreteTime startTime)
        {
            if (!enabled || manipulationActive)
                return;

            m_ViewModel = viewModel;
            m_ManipulationContext = manipulationContext;
            m_BehaviourStateMachine = new MoveBehaviourStateMachine();
            m_SnapEngine = new SnapEngine();
            m_MoveManipulationTimeInfo = new MoveManipulationTimeInfo(startTime, m_ManipulationContext.totalRange);

            m_BehaviourStateMachine.Start(m_ViewModel, handler, m_Behaviour.behaviour, m_ManipulationContext);
            m_SnapEngine.AddEdges(viewContext, m_Behaviour.behaviour);
        }

        public void UpdateTimeInfo(DiscreteTime time)
        {
            if (!manipulationActive)
                return;
            m_MoveManipulationTimeInfo.UpdatePreviewRange(time, m_BehaviourStateMachine.validRange);
        }

        public void ProcessMoveOnTrack()
        {
            if (!manipulationActive)
                return;
            if (m_BehaviourStateMachine.isAttached)
                m_BehaviourStateMachine.Move(m_Behaviour.behaviour, m_MoveManipulationTimeInfo.previewRange.start);
        }

        public void ProcessMoveAttach(Track trackUnderMouse, ViewContext viewContext)
        {
            if (!manipulationActive)
                return;

            m_BehaviourStateMachine.Attach(m_Behaviour.behaviour, trackUnderMouse, m_MoveManipulationTimeInfo.previewRange.start);
            m_SnapEngine.AddEdges(viewContext, m_Behaviour.behaviour);
        }

        public void ProcessMoveDetach()
        {
            if (!manipulationActive)
                return;
            m_BehaviourStateMachine.Detach(m_Behaviour.behaviour);
            m_SnapEngine.RemoveAllEdges();
        }

        public DiscreteTime GetLastMoveDelta()
        {
            if (manipulationActive)
                return m_MoveManipulationTimeInfo.lastDelta;
            return DiscreteTime.Zero;
        }

        public void ApplyMove()
        {
            if (!manipulationActive)
                return;
            m_BehaviourStateMachine.Commit(m_Behaviour.behaviour);
        }

        public void CancelMove()
        {
            if (!manipulationActive)
                return;
            m_BehaviourStateMachine.Cancel(m_Behaviour.behaviour);
        }

        public void EndMove()
        {
            m_ViewModel = default;
            m_ManipulationContext = default;
            m_BehaviourStateMachine = default;
            m_SnapEngine = default;
            m_MoveManipulationTimeInfo = default;
        }

        public void ApplySnapping(ICanvas canvas, bool edgeSnap, bool snapToFrame)
        {
            if (!manipulationActive)
                return;
            if (snapToFrame)
                m_MoveManipulationTimeInfo.ApplySnapToFrame(canvas);
            if (edgeSnap)
                m_MoveManipulationTimeInfo.ApplyEdgeSnap(canvas, m_SnapEngine, k_EdgeAttractionInPixels);
        }

        public void UpdateAttachedOverlay()
        {
            if (!moveOverlay.isShown)
                return;

            moveOverlay.UpdateRange(m_MoveManipulationTimeInfo.previewRange);
            moveOverlay.SetItemOverlayState(m_BehaviourStateMachine.needsPreview, m_BehaviourStateMachine.isValid);
            moveOverlay.SetSnapState(m_MoveManipulationTimeInfo.isSnappedLeft, m_MoveManipulationTimeInfo.isSnappedRight);
        }

        public void UpdateDetachedOverlay()
        {
            if (!moveOverlay.isShown)
                return;

            moveOverlay.UpdateRange(m_MoveManipulationTimeInfo.previewRange);
            moveOverlay.SetItemOverlayState(true, m_BehaviourStateMachine.isValid);
            moveOverlay.SetSnapState(false, false);
        }

        public void SetupOverlay(IManipulationContextProvider context)
        {
            if (moveOverlay == null)
                return;

            foreach (Item item in m_ManipulationContext.allItems)
            {
                ItemElement itemElement = context.GetElementFor(item);
                if (itemElement != null && (item.isMarker || item.isClip))
                    moveOverlay.AddItemOverlay(item, itemElement.worldBound);
            }
        }

        public void ShowOverlay(bool showEdgeSnapDebug = false)
        {
            if (moveOverlay == null)
                return;

            moveOverlay.SetInitialRange(m_ManipulationContext.totalRange);

            if (showEdgeSnapDebug)
                ManipulatorUtils.ShowSnapEngineDebug(moveOverlay, m_SnapEngine, k_EdgeAttractionInPixels);

            moveOverlay.Show();
        }

        public void HideOverlay()
        {
            if (moveOverlay == null)
                return;

            moveOverlay.Hide();
            moveOverlay.SetSnapState(false, false);
            moveOverlay.RemoveClipOverlays();
            ManipulatorUtils.RemoveSnapEngineDebug(moveOverlay);
        }

        public void ShowBehaviourOverlay()
        {
            if (m_Behaviour.overlay == null)
                return;

            m_Behaviour.overlay.BuildIndicatorsList(m_Behaviour.behaviour.targets);
            m_Behaviour.overlay.Show();
            m_Behaviour.overlay.schedule.Execute(UpdateBehaviourOverlay).Until(() => !m_Behaviour.overlay.isShown);
        }

        public void HideBehaviourOverlay()
        {
            if (m_Behaviour.overlay == null)
                return;
            m_Behaviour.overlay.Hide();
        }

        public static void MoveItemSelectionTo(IManipulationContextProvider contextProvider, MoveManipulation manipulation, DiscreteTime time) =>
            MoveItemsTo(contextProvider.GetViewModel(), contextProvider.GetManipulationHandler(), manipulation, time, contextProvider.GetManipulationContext());

        public static void MoveItemsTo(ISequenceViewModel viewModel, IManipulationHandler handler, MoveManipulation manipulation, DiscreteTime time, ManipulationContext context)
        {
            if (manipulation.manipulationActive)
                return;
            manipulation.m_BehaviourStateMachine = new MoveBehaviourStateMachine();
            manipulation.m_BehaviourStateMachine.Start(viewModel, handler, manipulation.m_Behaviour.behaviour, context);
            manipulation.m_BehaviourStateMachine.Move(manipulation.m_Behaviour.behaviour, time);
            manipulation.m_BehaviourStateMachine.Commit(manipulation.m_Behaviour.behaviour);
            manipulation.m_BehaviourStateMachine = default;
        }

        public void ApplyPendingBehaviour(ManipulationContext context)
        {
            if (m_PendingBehaviour.IsDefault() ||
                (manipulationActive && !m_BehaviourStateMachine.isAttached))
                return;

            if (manipulationActive)
            {
                HideBehaviourOverlay();
                m_PendingBehaviour.overlay.cursorChanged = m_Behaviour.overlay.cursorChanged;
                m_Behaviour.overlay.cursorChanged = null;
                m_PendingBehaviour.behaviour.TransferManipulationFrom(m_Behaviour.behaviour, context);
            }

            ApplyPendingBehaviour();
        }

        public void ApplyPendingBehaviour()
        {
            if (m_PendingBehaviour.IsDefault())
                return;
            bool overlayDisplayed = m_Behaviour.overlay is { visible: true };

            m_Behaviour.overlay?.RemoveFromHierarchy();
            m_Behaviour = m_PendingBehaviour;
            m_PendingBehaviour = default;

            if (manipulationActive && overlayDisplayed)
                ShowBehaviourOverlay();
        }

        void UpdateBehaviourOverlay()
        {
            if (m_ViewModel == null) return;

            if (isUsingPreview)
                m_Behaviour.overlay?.UpdateIndicatorsWithItemPreview(moveBehaviour, moveOverlay.GetShownItemOverlaysRanges());
            else
                m_Behaviour.overlay?.UpdateIndicators(moveBehaviour, m_ViewModel.sequenceData.lookup);
        }
    }
}
