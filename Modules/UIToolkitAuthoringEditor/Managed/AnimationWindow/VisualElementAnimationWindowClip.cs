// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Per-element clip wrapper. Overrides GetValueType so discrete style-enum rows
    // (e.g. Visibility) report typeof(int) when the clip has no GameObject root, instead
    // of the generic float fallback. The "discrete = int" assumption is UI-Toolkit-specific
    // and stays scoped to this module rather than baked into AnimationWindowClip itself.
    [Serializable]
    internal sealed class VisualElementAnimationWindowClip : AnimationWindowClip, IAnimationWindowClip
    {
        [SerializeField] UIAnimationClip m_UIAnimationClip;

        public VisualElementAnimationWindowClip(UIAnimationClip uiClip)
            : base(uiClip != null ? uiClip.animationClip : null)
        {
            m_UIAnimationClip = uiClip;
        }

        public new string name =>
            m_UIAnimationClip != null && !string.IsNullOrEmpty(m_UIAnimationClip.name)
                ? m_UIAnimationClip.name
                : base.name;

        // AnimationWindowClip.isReadOnly compares hideFlags for equality, which reads an imported clip
        // here as writable: the inner clip of a UI Toolkit clip carries HideInHierarchy alongside
        // NotEditable.
        internal static bool IsReadOnly(AnimationClip clip) =>
            clip == null || (clip.hideFlags & HideFlags.NotEditable) != 0;

        public new bool isReadOnly => IsReadOnly(animationClip);

        protected override Type GetValueType(EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
                return null;
            if (binding.isDiscreteCurve)
                return typeof(int);
            return typeof(float);
        }

        // The rename overlay is seeded from EditorCurveBinding.path, which a UI Toolkit binding leaves
        // empty, so what comes back is the bare name the user typed rather than a full element path.
        // Segments are "#name" joined by '/', matching UIAnimationBinder.GatherAnimatableElements.
        internal static string BuildRenamedElementPath(string oldElementPath, string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return null;

            if (newName.IndexOf('/') >= 0)
                return newName;

            var segment = newName[0] == '#' ? newName : UIAnimationPath.Segment(newName);
            return UIAnimationPath.ReplaceLastSegment(oldElementPath, segment);
        }

        /// <summary>
        /// Re-points curves at a renamed element. <paramref name="newPaths"/> carries the bare element
        /// name typed into the rename overlay, not a GameObject path: UI Toolkit bindings keep
        /// <c>path</c> empty and encode the hierarchy inside <c>propertyName</c>.
        /// </summary>
        // Shadows AnimationWindowClip.RenameCurves, which rewrites binding.path. For a UI Toolkit clip
        // that writes a path onto a binding that must keep an empty one, stranding the curve and leaving
        // the original row behind. The base method is not virtual, so re-implementing the interface on
        // this type is what re-points the call.
        public new void RenameCurves(IEnumerable<EditorCurveBinding> bindings, IEnumerable<string> newPaths, string undoLabel)
        {
            var clip = m_UIAnimationClip != null ? m_UIAnimationClip.animationClip : null;
            if (clip == null || bindings == null || newPaths == null)
                return;

            var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
            var renamed = new HashSet<string>(StringComparer.Ordinal);
            using (var bindingEnumerator = bindings.GetEnumerator())
            using (var pathEnumerator = newPaths.GetEnumerator())
            {
                while (bindingEnumerator.MoveNext() && pathEnumerator.MoveNext())
                {
                    if (pathEnumerator.Current == null)
                        continue;

                    var propertyName = bindingEnumerator.Current.propertyName;
                    if (!UIAnimationPath.TrySplitElementPath(propertyName, out var oldElementPath))
                        continue;

                    var newElementPath = BuildRenamedElementPath(oldElementPath, pathEnumerator.Current);
                    if (newElementPath == null)
                        continue;

                    mapping[oldElementPath] = newElementPath;
                    renamed.Add(propertyName);
                }
            }

            UIAnimationCurvePathRemap.RemapElementPaths(clip, mapping, renamed, undoLabel);
        }
    }
}
