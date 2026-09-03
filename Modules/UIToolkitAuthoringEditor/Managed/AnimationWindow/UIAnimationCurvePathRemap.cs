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
    // Re-points UI Toolkit animation curves at a new element path. For these clips
    // EditorCurveBinding.path is always empty and the element hierarchy lives in propertyName
    // ("#parent/#child/Opacity.x"), so a remap rewrites propertyName - not path, which is what
    // AnimationWindowClip.RenameCurves does.
    internal static class UIAnimationCurvePathRemap
    {
        static void CollectElementPaths(AnimationClip clip, HashSet<string> results)
        {
            if (clip == null || results == null)
                return;

            AddElementPaths(AnimationUtility.GetCurveBindings(clip), results);
            AddElementPaths(AnimationUtility.GetObjectReferenceCurveBindings(clip), results);
        }

        static void AddElementPaths(EditorCurveBinding[] bindings, HashSet<string> results)
        {
            foreach (var binding in bindings)
            {
                if (UIAnimationPath.TrySplitElementPath(binding.propertyName, out var elementPath))
                    results.Add(elementPath);
            }
        }

        // Maps oldElementPath and every descendant path the clip still records onto newElementPath.
        // Descendants are read from the clip, not the binder: a broken path is by definition absent from
        // the binder's element table, so the clip's own bindings are the only surviving record of the
        // subtree's shape. Matching anchors on a trailing separator so "#a" cannot claim "#ab".
        internal static Dictionary<string, string> BuildSubtreeMapping(AnimationClip clip, string oldElementPath, string newElementPath)
        {
            var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
            if (clip == null || oldElementPath == null || newElementPath == null)
                return mapping;

            var paths = new HashSet<string>(StringComparer.Ordinal);
            CollectElementPaths(clip, paths);

            foreach (var path in paths)
            {
                var rebased = UIAnimationPath.RebaseElementPath(path, oldElementPath, newElementPath);
                if (rebased != null)
                    mapping[path] = rebased;
            }

            return mapping;
        }

        /// <summary>
        /// Answers, for one candidate target after another, whether remapping the broken subtree onto it
        /// would land a curve where the clip already has one.
        /// </summary>
        // Same rule CollectMoves applies when it writes - a destination counts as taken only if a curve
        // will still be there once every move is applied, so curves moving out of the way do not block.
        // Precomputed because the picker asks this for every element in the panel.
        internal sealed class RemapConflicts
        {
            // Keyed by curve kind as well as name: the write layer moves float and object-reference
            // curves independently, so a destination held by one kind does not block the other.
            readonly HashSet<(bool isPPtr, string propertyName)> m_Occupied = new();
            readonly HashSet<(bool isPPtr, string propertyName)> m_Moving = new();
            readonly List<(bool isPPtr, string elementPath, string tail)> m_MovingParts = new();
            readonly string m_BrokenElementPath;

            internal RemapConflicts(AnimationClip clip, string brokenElementPath)
            {
                m_BrokenElementPath = brokenElementPath;
                if (clip == null || brokenElementPath == null)
                    return;

                Collect(AnimationUtility.GetCurveBindings(clip));
                Collect(AnimationUtility.GetObjectReferenceCurveBindings(clip));
            }

            void Collect(EditorCurveBinding[] bindings)
            {
                foreach (var binding in bindings)
                {
                    m_Occupied.Add((binding.isPPtrCurve, binding.propertyName));

                    if (!UIAnimationPath.TrySplitPropertyName(binding.propertyName, out var elementPath, out var tail))
                        continue;
                    if (UIAnimationPath.RebaseElementPath(elementPath, m_BrokenElementPath, m_BrokenElementPath) == null)
                        continue;

                    m_Moving.Add((binding.isPPtrCurve, binding.propertyName));
                    m_MovingParts.Add((binding.isPPtrCurve, elementPath, tail));
                }
            }

            internal bool TryFindConflict(string targetElementPath, out string conflictingPropertyName)
            {
                conflictingPropertyName = null;
                if (targetElementPath == null ||
                    string.Equals(targetElementPath, m_BrokenElementPath, StringComparison.Ordinal))
                    return false;

                foreach (var (isPPtr, elementPath, tail) in m_MovingParts)
                {
                    var rebased = UIAnimationPath.RebaseElementPath(elementPath, m_BrokenElementPath, targetElementPath);
                    if (rebased == null)
                        continue;

                    var destination = UIAnimationPath.BuildPropertyName(rebased, tail);
                    if (!m_Occupied.Contains((isPPtr, destination)) || m_Moving.Contains((isPPtr, destination)))
                        continue;

                    conflictingPropertyName = destination;
                    return true;
                }

                return false;
            }
        }

        internal static int RemapElementPaths(AnimationClip clip, IReadOnlyDictionary<string, string> oldToNewElementPaths, string undoLabel) =>
            RemapElementPaths(clip, oldToNewElementPaths, null, undoLabel);

        // A non-null restrictToPropertyNames limits the rewrite to those curves; every other curve sharing
        // a mapped element path stays put. Occupancy is still judged against the whole clip either way.
        internal static int RemapElementPaths(AnimationClip clip, IReadOnlyDictionary<string, string> oldToNewElementPaths,
            HashSet<string> restrictToPropertyNames, string undoLabel)
        {
            if (clip == null || oldToNewElementPaths == null || oldToNewElementPaths.Count == 0)
                return 0;

            if (VisualElementAnimationWindowClip.IsReadOnly(clip))
                return 0;

            var floatBindings = AnimationUtility.GetCurveBindings(clip);
            var pptrBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            var floatMoves = CollectMoves(clip, floatBindings, oldToNewElementPaths, restrictToPropertyNames);
            var pptrMoves = CollectMoves(clip, pptrBindings, oldToNewElementPaths, restrictToPropertyNames);

            if (floatMoves.Count == 0 && pptrMoves.Count == 0)
                return 0;

            Undo.RegisterCompleteObjectUndo(clip, undoLabel);

            ApplyMoves(clip, floatMoves, AnimationUtility.GetEditorCurve, AnimationUtility.SetEditorCurves);
            ApplyMoves(clip, pptrMoves, AnimationUtility.GetObjectReferenceCurve, AnimationUtility.SetObjectReferenceCurves);

            return floatMoves.Count + pptrMoves.Count;
        }

        static void ApplyMoves<TCurve>(AnimationClip clip, List<(EditorCurveBinding from, EditorCurveBinding to)> moves,
            Func<AnimationClip, EditorCurveBinding, TCurve> read, Action<AnimationClip, EditorCurveBinding[], TCurve[]> write)
        {
            if (moves.Count == 0)
                return;

            var from = new EditorCurveBinding[moves.Count];
            var to = new EditorCurveBinding[moves.Count];
            var values = new TCurve[moves.Count];
            for (var i = 0; i < moves.Count; i++)
            {
                from[i] = moves[i].from;
                to[i] = moves[i].to;
                values[i] = read(clip, moves[i].from);
            }

            // Removals and additions have to be two separate batched calls. Each call ends in a
            // SyncEditorCurves, and a discrete curve's removal does not take effect if an addition is
            // queued behind it inside the same unsynced batch - the old binding survives and the clip
            // ends up with both. Float curves tolerate the combined form; discrete ones do not.
            write(clip, from, new TCurve[moves.Count]);
            write(clip, to, values);
        }

        static List<(EditorCurveBinding from, EditorCurveBinding to)> CollectMoves(
            AnimationClip clip, EditorCurveBinding[] bindings, IReadOnlyDictionary<string, string> oldToNewElementPaths,
            HashSet<string> restrictToPropertyNames)
        {
            var moves = new List<(EditorCurveBinding from, EditorCurveBinding to)>();
            if (bindings.Length == 0)
                return moves;

            var candidates = new List<(EditorCurveBinding from, string target)>();
            var vacated = new HashSet<string>(StringComparer.Ordinal);

            foreach (var binding in bindings)
            {
                if (restrictToPropertyNames != null && !restrictToPropertyNames.Contains(binding.propertyName))
                    continue;
                if (!UIAnimationPath.TrySplitPropertyName(binding.propertyName, out var elementPath, out var tail))
                    continue;
                if (!oldToNewElementPaths.TryGetValue(elementPath, out var newElementPath))
                    continue;
                if (string.Equals(elementPath, newElementPath, StringComparison.Ordinal))
                    continue;

                candidates.Add((binding, UIAnimationPath.BuildPropertyName(newElementPath, tail)));
                vacated.Add(binding.propertyName);
            }

            if (candidates.Count == 0)
                return moves;

            // A target is only occupied if a binding will still be there once every move is applied,
            // so paths that swap (#a -> #b while #b -> #a) are not rejected as mutual conflicts.
            var survivors = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in bindings)
            {
                if (!vacated.Contains(binding.propertyName))
                    survivors.Add(binding.propertyName);
            }

            foreach (var candidate in candidates)
            {
                if (!survivors.Add(candidate.target))
                {
                    Debug.LogWarning(
                        $"Could not remap animation curve \"{candidate.from.propertyName}\" in \"{clip.name}\": " +
                        $"\"{candidate.target}\" is already animated. The curve was left unchanged.");
                    continue;
                }

                var to = candidate.from;
                to.propertyName = candidate.target;   // type and curve-kind flags must carry over or GetEditorCurve misses the source
                moves.Add((candidate.from, to));
            }

            return moves;
        }
    }
}
