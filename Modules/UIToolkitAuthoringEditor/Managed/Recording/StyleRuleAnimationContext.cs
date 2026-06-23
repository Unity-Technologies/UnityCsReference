// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Registry of the USS rule the Animation Window currently drives, so AnimationRecordingStyleBridge
    // can route rule-inspector edits to its clip. Rule-level analog of PerElementAnimationContext.
    internal static class StyleRuleAnimationContext
    {
        static StyleRule s_ActiveRule;
        static UIAnimationClip s_ActiveClip;

        internal static StyleRule activeRule => s_ActiveRule;
        internal static UIAnimationClip activeClip => s_ActiveClip;

        internal static void SetActive(StyleRule rule, UIAnimationClip clip)
        {
            s_ActiveRule = rule;
            s_ActiveClip = clip;
        }

        internal static void ClearActive(UIAnimationClip clip)
        {
            // Clear only if we still own the registration.
            if (ReferenceEquals(s_ActiveClip, clip))
            {
                s_ActiveRule = null;
                s_ActiveClip = null;
            }
        }

        // Resolves the active clip when recording the given rule; false when no preview is
        // active or the rule isn't the one currently driven.
        internal static bool TryResolveForRule(StyleRule rule, out UIAnimationClip clip)
        {
            clip = null;
            if (rule == null || !ReferenceEquals(rule, s_ActiveRule))
                return false;
            clip = s_ActiveClip;
            return clip != null;
        }
    }
}
