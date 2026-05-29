// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.IntegerTime;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using FrameRate = Unity.Timeline.Foundation.Time.FrameRate;

using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class PlayControls : Unity.Timeline.Foundation.Widgets.PlayControls, IDisposable
    {
        AnimationWindowState m_State;

        public static string s_PlayContentTooltip = L10n.Tr("Play the animation clip ({0}).");
        public static string s_PrevKeyContentTooltip = L10n.Tr("Go to previous keyframe ({0}).");
        public static string s_NextKeyContentTooltip = L10n.Tr("Go to next keyframe ({0}).");
        public static string s_FirstKeyContentTooltip = L10n.Tr("Go to the beginning of the animation clip ({0}).");
        public static string s_LastKeyContentTooltip = L10n.Tr("Go to the end of the animation clip ({0}).");

        internal new ToolbarToggle playToggle => base.playToggle;

        internal Button firstKeyframeButton => this.Q<Button>(k_FirstFrameElement);
        internal Button previousKeyframeButton => this.Q<Button>(k_PreviousFrameElement);
        internal Button nextKeyframeButton => this.Q<Button>(k_NextFrameElement);
        internal Button lastKeyframeButton => this.Q<Button>(k_LastFrameElement);

        public PlayControls()
        {
            PlayClicked += isPlaying =>
            {
                m_State.playing = isPlaying;
            };
            StepBackClicked += () => m_State.GoToPreviousKeyframe();
            StepForwardClicked += () => m_State.GoToNextKeyframe();
            FirstFrameClicked += () => m_State.GoToFirstKeyframe();
            LastFrameClicked += () => m_State.GoToLastKeyframe();
            PlayTimeChanged += (discreteTime) =>
            {
                m_State.currentFrame = (int)(double)(discreteTime * m_State.frameRate);
            };
            playRangeToggle.style.display = DisplayStyle.None;

            firstKeyframeButton.tooltip = String.Format(s_FirstKeyContentTooltip, ShortcutManager.instance.GetShortcutBinding("Animation/First Keyframe"));
            previousKeyframeButton.tooltip = String.Format(s_PrevKeyContentTooltip, ShortcutManager.instance.GetShortcutBinding("Animation/Previous Keyframe"));
            playToggle.tooltip = String.Format(s_PlayContentTooltip, ShortcutManager.instance.GetShortcutBinding("Animation/Play Animation"));
            nextKeyframeButton.tooltip = String.Format(s_NextKeyContentTooltip, ShortcutManager.instance.GetShortcutBinding("Animation/Next Keyframe"));
            lastKeyframeButton.tooltip = String.Format(s_LastKeyContentTooltip, ShortcutManager.instance.GetShortcutBinding("Animation/Last Keyframe"));
        }

        public void Initialize(AnimationWindowState state)
        {
            m_State = state;
            m_State.onRefresh += OnRefresh;

            OnRefresh();
        }

        public void Dispose()
        {
            m_State.onRefresh -= OnRefresh;
        }

        public void Update()
        {
            if (playToggle.value != m_State.playing)
                playToggle.SetValueWithoutNotify(m_State.playing);

            var time = (m_State.playing)
                ? new DiscreteTime(m_State.currentTime)
                : new DiscreteTime((double)m_State.currentFrame / m_State.frameRate);

            if (time != PlayTimeField.Time)
                PlayTimeField.SetValueWithoutNotify(time);
        }

        public void OnRefresh()
        {
            SetEnabled(!m_State.disabled);
            playToggle.SetEnabled(m_State.canPlay);

            PlayTimeField.FrameRate = new FrameRate((uint)m_State.frameRate);
        }
    }
}
