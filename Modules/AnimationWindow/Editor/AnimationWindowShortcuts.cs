// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;

namespace UnityEditor
{
    public sealed partial class AnimationWindow : EditorWindow, IHasCustomMenu
    {
        static void ExecuteShortcut(ShortcutArguments args, Action<AnimEditor> exp)
        {
            var animationWindow = (AnimationWindow)args.context;
            var animEditor = animationWindow.animEditor;

            if (EditorWindow.focusedWindow != animationWindow)
                return;

            if (animEditor.stateDisabled)
                return;

            exp(animEditor);

            animEditor.Repaint();
        }

        static void ExecuteShortcut(ShortcutArguments args, Action<AnimationWindowState> exp)
        {
            ExecuteShortcut(args, animEditor => exp(animEditor.state));
        }

        [Shortcut("Animation/Show Curves", typeof(AnimationWindow), KeyCode.C, displayName = "Animation/Switch Between Curves and Dopesheet")]
        static void SwitchBetweenCurvesAndDopesheet(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.SwitchBetweenCurvesAndDopesheet(); });
        }

        [Shortcut("Animation/Play Animation", typeof(AnimationWindow), KeyCode.Space)]
        static void TogglePlayAnimation(ShortcutArguments args)
        {
            ExecuteShortcut(args, state =>
            {
                state.playing = !state.playing;
            });
        }

        [Shortcut("Animation/Next Frame", typeof(AnimationWindow), KeyCode.Period)]
        static void NextFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToNextFrame());
        }

        [Shortcut("Animation/Previous Frame", typeof(AnimationWindow), KeyCode.Comma)]
        static void PreviousFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToPreviousFrame());
        }

        [Shortcut("Animation/Previous Keyframe", typeof(AnimationWindow), KeyCode.Comma, ShortcutModifiers.Alt)]
        static void PreviousKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToPreviousKeyframe());
        }

        [Shortcut("Animation/Next Keyframe", typeof(AnimationWindow), KeyCode.Period, ShortcutModifiers.Alt)]
        static void NextKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToNextKeyframe());
        }

        [Shortcut("Animation/First Keyframe", typeof(AnimationWindow), KeyCode.Comma, ShortcutModifiers.Shift)]
        static void FirstKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToFirstKeyframe());
        }

        [Shortcut("Animation/Last Keyframe", typeof(AnimationWindow), KeyCode.Period, ShortcutModifiers.Shift)]
        static void LastKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, state => state.GoToLastKeyframe());
        }

        [Shortcut("Animation/Key Selected", null, KeyCode.K)]
        static void KeySelected(ShortcutArguments args)
        {
            AnimationWindow animationWindow = AnimationWindow.GetAllAnimationWindows().Find(aw => (aw.state.previewing || aw == EditorWindow.focusedWindow));
            if (animationWindow == null)
                return;

            var animEditor = animationWindow.animEditor;

            animEditor.SaveCurveEditorKeySelection();
            AnimationWindowUtility.AddSelectedKeyframes(animEditor.state, AnimationKeyTime.Frame(animEditor.state.currentFrame, animEditor.state.frameRate));
            animEditor.state.ClearCandidates();
            animEditor.UpdateSelectedKeysToCurveEditor();

            animEditor.Repaint();
        }

        [Shortcut("Animation/Key Modified", null, KeyCode.K, ShortcutModifiers.Shift)]
        static void KeyModified(ShortcutArguments args)
        {
            AnimationWindow animationWindow = AnimationWindow.GetAllAnimationWindows().Find(aw => (aw.state.previewing || aw == EditorWindow.focusedWindow));
            if (animationWindow == null)
                return;

            var animEditor = animationWindow.animEditor;

            animEditor.CreateKeyframesAtCurrentTime();
        }

        [Shortcut("Animation/Toggle Ripple", typeof(AnimationWindow), KeyCode.Alpha2, ShortcutModifiers.Shift)]
        static void ToggleRipple(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.state.rippleTime = !animEditor.state.rippleTime; });
        }

        [ClutchShortcut("Animation/Ripple (Clutch)", typeof(AnimationWindow), KeyCode.Alpha2)]
        static void ClutchRipple(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.state.rippleTimeClutch = args.stage == ShortcutStage.Begin; });
        }

        [Shortcut("Animation/Frame All", typeof(AnimationWindow), KeyCode.A)]
        static void FrameAll(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.FrameClipDelayed(); });
        }
    }
}
