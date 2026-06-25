// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    [UxmlElement]
    internal partial class ActivityIndicatorOverlay : VisualElement
    {
        const string k_UssClass_Hidden = "activity-indicator-overlay__hidden";

        bool m_IsAnimating = false;
        bool m_IsShowAfterDelayInProgress;
        VisualElement m_ActivityIndicator;

        public ActivityIndicatorOverlay()
        {
            AddToClassList("activity-indicator-overlay");

            m_ActivityIndicator = new VisualElement()
            {
                name = "activity-indicator-overlay__indicator",
                usageHints = UsageHints.DynamicTransform
            };
            Add(m_ActivityIndicator);

            ApplyStyleSheet();
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        // This is used to avoid visual flickering when very fast asynchronous operations
        // are queued in quick succession, such as when scrubbing through profiler frames.
        public async void ShowAfterDelay(int delayMs)
        {
            // If a show-after-delay is already in progress, ignore new requests.
            if (m_IsShowAfterDelayInProgress)
                return;

            // Wait for the delay (async).
            m_IsShowAfterDelayInProgress = true;
            await Task.Run(() =>
            {
                Thread.Sleep(delayMs);
            });

            // If cancelled whilst waiting, don't show.
            bool wasCancelled = m_IsShowAfterDelayInProgress == false;
            if (wasCancelled)
                return;

            Show();
        }

        public void Show()
        {
            if (m_IsAnimating)
                return;

            // If a show-after-delay is in progress, cancel it.
            m_IsShowAfterDelayInProgress = false;

            const long k_AnimationIntervalMS = 33L;
            m_IsAnimating = true;
            RemoveFromClassList(k_UssClass_Hidden);
            UIUtility.SetElementDisplay(this, true);
            m_ActivityIndicator.schedule.Execute(Animate).Every(k_AnimationIntervalMS).Until(() => !m_IsAnimating);
        }

        public void Hide()
        {
            // If a show-after-delay is in progress, cancel it.
            m_IsShowAfterDelayInProgress = false;

            m_IsAnimating = false;
            AddToClassList(k_UssClass_Hidden);

            // Hide the visual element once the opacity transition has completed.
            // TransitionEndEvent can't be used because it won't fire if no transition is required.
            const long k_HideTransitionDurationMs = 210L;
            schedule.Execute(() =>
            {
                // Abort if shown whilst transitioning to a hidden state.
                if (m_IsAnimating)
                    return;

                UIUtility.SetElementDisplay(this, false);
            }).StartingIn(k_HideTransitionDurationMs);
        }

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("ActivityIndicatorOverlay.uss") as StyleSheet;
            styleSheets.Add(uss);
        }

        void Animate(TimerState timerState)
        {
            const float k_RotationSpeed = 0.5f; // In degrees-per-millisecond.
            float lastRotationAngle = m_ActivityIndicator.style.rotate.value.angle.value;
            var increment = k_RotationSpeed * timerState.deltaTime;
            var rotationAngle = lastRotationAngle + increment;
            m_ActivityIndicator.style.rotate = new StyleRotate(new Rotate(rotationAngle));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // If a show-after-delay is in progress when we are detached, cancel it.
            m_IsShowAfterDelayInProgress = false;
        }
    }
}
