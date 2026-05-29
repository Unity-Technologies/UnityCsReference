// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Process-wide registry of the active per-element animation context. Lets
    /// <see cref="AnimationRecordingStyleBridge"/> route inspector style edits to the
    /// per-element <see cref="UIAnimationClip"/> target without depending on
    /// <see cref="VisualElementAnimationWindowController"/>.
    /// </summary>
    internal static class PerElementAnimationContext
    {
        static UIAnimationClip s_ActiveClip;
        static VisualElement s_ActiveClipOwner;
        static UIAnimationBinder s_ActiveBinder;
        static AnimationModeDriver s_ActiveDriver;

        internal static UIAnimationClip activeClip => s_ActiveClip;
        internal static VisualElement activeClipOwner => s_ActiveClipOwner;
        internal static UIAnimationBinder activeBinder => s_ActiveBinder;
        internal static AnimationModeDriver activeDriver => s_ActiveDriver;

        internal static void SetActive(UIAnimationClip clip, VisualElement clipOwner, UIAnimationBinder binder, AnimationModeDriver driver)
        {
            s_ActiveClip = clip;
            s_ActiveClipOwner = clipOwner;
            s_ActiveBinder = binder;
            s_ActiveDriver = driver;
        }

        internal static void ClearActive(UIAnimationClip clip)
        {
            // Clear only if we still own the registration.
            if (ReferenceEquals(s_ActiveClip, clip))
            {
                s_ActiveClip = null;
                s_ActiveClipOwner = null;
                s_ActiveBinder = null;
                s_ActiveDriver = null;
            }
        }

        /// <summary>
        /// Returns the active per-element context for <paramref name="element"/> when a
        /// controller has registered an active preview and the element is the clip owner
        /// or a binder-named descendant. Resolved path follows
        /// <see cref="UIAnimationBinder.TryGetPathForElement"/>.
        /// </summary>
        internal static bool TryResolveForElement(
            VisualElement element,
            out UIAnimationClip clip,
            out UIAnimationBinder binder,
            out AnimationModeDriver driver,
            out string elementPath)
        {
            clip = null;
            binder = null;
            driver = null;
            elementPath = null;

            if (element == null || s_ActiveClip == null || s_ActiveClipOwner == null || s_ActiveBinder == null)
                return false;

            if (!IsSelfOrDescendant(element, s_ActiveClipOwner))
                return false;

            s_ActiveBinder.UpdateElementNamesIfNeeded();
            if (!s_ActiveBinder.TryGetPathForElement(element, out var path))
                return false;

            clip = s_ActiveClip;
            binder = s_ActiveBinder;
            driver = s_ActiveDriver;
            elementPath = path;
            return true;
        }

        /// <summary>
        /// Inspector-probe variant: tries the AnimationWindow preview registration first then falls back to the binder - 
        /// </summary>
        internal static bool TryResolveForElementForProbe(
            VisualElement element,
            out UIAnimationClip clip,
            out UIAnimationBinder binder,
            out string elementPath)
        {
            if (TryResolveForElement(element, out clip, out binder, out _, out elementPath))
                return true;

            clip = null;
            binder = null;
            elementPath = null;
            if (element == null)
                return false;

            var system = element.GetStylePropertyAnimationSystem();
            if (system == null)
                return false;

            for (var current = element; current != null; current = current.parent)
            {
                if (!system.TryGetActiveClipForOwner(current, out var runtimeClip, out var runtimeBinder))
                    continue;

                // current is the root element of the clip, and element is animated at "runtimePath"
                runtimeBinder.UpdateElementNamesIfNeeded();
                if (!runtimeBinder.TryGetPathForElement(element, out var runtimePath))
                    return false;

                clip = runtimeClip;
                binder = runtimeBinder;
                elementPath = runtimePath;
                return true;
            }

            return false;
        }

        static bool IsSelfOrDescendant(VisualElement element, VisualElement ancestor)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (ReferenceEquals(current, ancestor))
                    return true;
            }
            return false;
        }
    }
}
