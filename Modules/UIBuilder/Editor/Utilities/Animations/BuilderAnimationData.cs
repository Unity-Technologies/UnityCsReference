// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using System;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Groups the six animation-longhand property manipulators (always read together) so the Builder can
    /// compose the parallel lists that drive <see cref="Unity.UIToolkit.Editor.StyleAnimationListView"/>.
    /// </summary>
    readonly struct BuilderAnimationData : IDisposable
    {
        public BuilderAnimationData(StyleSheet styleSheet, StyleRule styleRule, VisualElement element, bool editorExtensionMode)
        {
            clip = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.Clip, editorExtensionMode);
            duration = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.Duration, editorExtensionMode);
            delay = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.Delay, editorExtensionMode);
            iterationCount = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.IterationCount, editorExtensionMode);
            direction = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.Direction, editorExtensionMode);
            playState = styleSheet.GetStylePropertyManipulator(element, styleRule, AnimationStyleNames.PlayState, editorExtensionMode);
        }

        public readonly StylePropertyManipulator clip;
        public readonly StylePropertyManipulator duration;
        public readonly StylePropertyManipulator delay;
        public readonly StylePropertyManipulator iterationCount;
        public readonly StylePropertyManipulator direction;
        public readonly StylePropertyManipulator playState;

        public void Dispose()
        {
            clip?.Dispose();
            duration?.Dispose();
            delay?.Dispose();
            iterationCount?.Dispose();
            direction?.Dispose();
            playState?.Dispose();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
