// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal readonly struct CurveRepairOptions
    {
        public readonly string BrokenElementPath;

        // An imported clip cannot be written, so a repair offered against one would do nothing.
        public readonly bool IsReadOnly;

        public CurveRepairOptions(string brokenElementPath, bool isReadOnly)
        {
            BrokenElementPath = brokenElementPath;
            IsReadOnly = isReadOnly;
        }
    }

    // What a broken curve row can be repaired to, and how to apply it. Kept apart from the menu that
    // presents it so the decisions can be exercised without IMGUI or an Animation Window.
    internal static class UIAnimationCurveRepair
    {
        internal const string UndoLabel = "Remap Animation Curves";

        internal static bool TryGetOptions(AnimationClip clip, UIAnimationBinder binder,
            string propertyName, out CurveRepairOptions options)
        {
            options = default;

            if (clip == null)
                return false;

            if (UIAnimationBindingResolution.Resolve(binder, propertyName, out var brokenPath, out _)
                != BindingResolution.Broken)
                return false;

            options = new CurveRepairOptions(brokenPath, VisualElementAnimationWindowClip.IsReadOnly(clip));
            return true;
        }

        /// <summary>
        /// Whether remapping <paramref name="brokenElementPath"/>'s subtree onto
        /// <paramref name="targetElementPath"/> would land any moved curve on one the clip already has.
        /// The same rule the picker refuses targets with and the write layer skips moves with.
        /// </summary>
        internal static bool WouldCollide(AnimationClip clip, string brokenElementPath, string targetElementPath) =>
            new UIAnimationCurvePathRemap.RemapConflicts(clip, brokenElementPath)
                .TryFindConflict(targetElementPath, out _);

        // Always remaps the whole recorded subtree. A leaf simply yields a single-entry mapping, so the
        // narrower "this path only" behaviour is a special case of this one rather than a separate mode.
        internal static int Apply(AnimationClip clip, string brokenElementPath, string targetElementPath)
        {
            var mapping = UIAnimationCurvePathRemap.BuildSubtreeMapping(clip, brokenElementPath, targetElementPath);
            return UIAnimationCurvePathRemap.RemapElementPaths(clip, mapping, UndoLabel);
        }

        // Taken from the path rather than element.name so the label always matches what the binder
        // addresses - the root contributes no segment however it is named.
        internal static string DescribeLastSegment(string elementPath)
        {
            if (string.IsNullOrEmpty(elementPath))
                return L10n.Tr("(animation root)");

            return UIAnimationPath.LastSegment(elementPath);
        }

        // Only sees the loaded panel, so it can undercount when a selector also matches elements in
        // documents that are not open. Acceptable because the result is advisory and the user drives
        // the repair; an automatic remap could not take that liberty.
        internal static int CountElementsUsingClip(VisualElement root, UIAnimationClip uiClip)
        {
            if (root == null || uiClip == null)
                return 0;

            var clipId = uiClip.GetEntityId();
            var count = 0;
            var stack = new Stack<VisualElement>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var element = stack.Pop();

                // Not resolvedStyle.animationNames: that allocates two arrays and resolves every id to a
                // managed object on each read, for a walk that only needs to match one id.
                var clips = element.computedStyle.animationNames;
                for (var i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == clipId)
                    {
                        count++;
                        break;
                    }
                }

                for (var i = 0; i < element.childCount; i++)
                    stack.Push(element[i]);
            }

            return count;
        }
    }
}
