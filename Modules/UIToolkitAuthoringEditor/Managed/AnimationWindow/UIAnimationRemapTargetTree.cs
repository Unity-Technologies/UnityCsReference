// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal enum UIAnimationRemapTargetStatus
    {
        Addressable,
        Unnamed,
        NameShadowed,
        Occupied,
    }

    // One row of the remap picker. Mirrors the VisualElement hierarchy rather than the binder's path
    // table, so a row can exist for an element the binder cannot address - Status says which.
    internal sealed class UIAnimationRemapTarget
    {
        internal VisualElement Element { get; }
        internal string DisplayName { get; }

        // Null unless Addressable. Empty string is a real path: the animation root.
        internal string ElementPath { get; }

        internal UIAnimationRemapTargetStatus Status { get; }

        // The curve standing in the way, when Status is Occupied. Named so the row can say which one.
        internal string ConflictingPropertyName { get; }

        // Lets the view decide whether to prune a subtree that contains nothing pickable, without the
        // model taking a position on it.
        internal bool HasAddressableDescendant { get; }

        internal IReadOnlyList<UIAnimationRemapTarget> Children { get; }

        internal UIAnimationRemapTarget(VisualElement element, string elementPath,
            UIAnimationRemapTargetStatus status, string conflictingPropertyName,
            bool hasAddressableDescendant, IReadOnlyList<UIAnimationRemapTarget> children)
        {
            Element = element;
            DisplayName = string.IsNullOrEmpty(element?.name) ? k_UnnamedLabel : element.name;
            ElementPath = elementPath;
            Status = status;
            ConflictingPropertyName = conflictingPropertyName;
            HasAddressableDescendant = hasAddressableDescendant;
            Children = children;
        }

        const string k_UnnamedLabel = "(unnamed)";
    }

    internal static class UIAnimationRemapTargetTree
    {
        /// <summary>
        /// The elements <paramref name="brokenElementPath"/> can be remapped onto, and for each the reason
        /// it cannot be. <paramref name="clip"/> is what makes an occupied target answerable.
        /// </summary>
        internal static UIAnimationRemapTarget Build(UIAnimationBinder binder, AnimationClip clip,
            string brokenElementPath)
        {
            if (binder == null)
                return null;

            binder.UpdateElementNamesIfNeeded();
            var registered = binder.GetRegisteredElements();
            if (registered.Count == 0)
                return null;

            var conflicts = new UIAnimationCurvePathRemap.RemapConflicts(clip, brokenElementPath);

            // Inverted once: TryGetPathForElement is a linear scan of the same list, so asking it per
            // node would make building the tree quadratic.
            var pathByElement = new Dictionary<VisualElement, string>(registered.Count);
            for (var i = 0; i < registered.Count; i++)
                pathByElement[registered[i].Value] = registered[i].Key;

            var root = ResolveTreeRoot(registered);
            return root == null ? null : BuildTarget(root, pathByElement, conflicts);
        }

        static UIAnimationRemapTarget BuildTarget(VisualElement element,
            Dictionary<VisualElement, string> pathByElement, UIAnimationCurvePathRemap.RemapConflicts conflicts)
        {
            var childCount = element.hierarchy.childCount;
            var children = new List<UIAnimationRemapTarget>(childCount);
            var hasAddressableDescendant = false;

            for (var i = 0; i < childCount; i++)
            {
                var child = BuildTarget(element.hierarchy[i], pathByElement, conflicts);
                children.Add(child);
                hasAddressableDescendant |=
                    child.Status == UIAnimationRemapTargetStatus.Addressable || child.HasAddressableDescendant;
            }

            var addressable = pathByElement.TryGetValue(element, out var path);
            string conflict = null;

            // A named element with no path lost it to a same-named sibling registered first; the binder
            // keeps only the first, so the rest are unreachable until one of them is renamed.
            var status = !addressable
                ? string.IsNullOrEmpty(element.name)
                    ? UIAnimationRemapTargetStatus.Unnamed
                    : UIAnimationRemapTargetStatus.NameShadowed
                // A repair moves the whole recorded subtree, so one collision anywhere in it would leave
                // the curve half-moved. The write layer refuses those; the picker refuses them up front.
                : conflicts.TryFindConflict(path, out conflict)
                    ? UIAnimationRemapTargetStatus.Occupied
                    : UIAnimationRemapTargetStatus.Addressable;

            return new UIAnimationRemapTarget(element, addressable ? path : null, status, conflict,
                hasAddressableDescendant, children);
        }

        // The binder's paths are relative to its own root, which is not exposed. The lowest common
        // ancestor of everything it registered contains every addressable element, which is all the tree
        // needs: the path shown to the user always comes from the binder, never from tree position.
        internal static VisualElement ResolveTreeRoot(IReadOnlyList<KeyValuePair<string, VisualElement>> registered)
        {
            if (registered == null || registered.Count == 0)
                return null;

            var root = registered[0].Value;
            for (var i = 1; i < registered.Count && root != null; i++)
                root = LowestCommonAncestor(root, registered[i].Value);

            return root;
        }

        static VisualElement LowestCommonAncestor(VisualElement a, VisualElement b)
        {
            if (a == null || b == null)
                return a ?? b;

            var ancestors = new HashSet<VisualElement>();
            for (var element = a; element != null; element = element.hierarchy.parent)
                ancestors.Add(element);

            for (var element = b; element != null; element = element.hierarchy.parent)
            {
                if (ancestors.Contains(element))
                    return element;
            }

            return null;
        }
    }
}
