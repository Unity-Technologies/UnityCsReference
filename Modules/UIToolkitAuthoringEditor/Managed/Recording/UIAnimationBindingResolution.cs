// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal enum BindingResolution
    {
        // No binder, or its element table is empty - e.g. a UIAnimationClip asset opened straight from
        // the Project window with no panel loaded. Resolution is unknowable, which is not the same as broken.
        Unknown,
        Resolved,
        Broken,
    }

    // These caches reach a whole panel's elements, so a reload that left them populated would hold the
    // previous domain's tree alive. Auto clears the lookup, restores the dirty flag's initializer and
    // nulls the rest.
    internal static partial class UIAnimationBindingResolution
    {
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Auto)]
        static UIAnimationBinder s_Binder;

        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Auto)]
        static readonly Dictionary<string, VisualElement> s_PathLookup = new(StringComparer.Ordinal);

        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Auto)]
        static bool s_LookupDirty = true;

        // Not Auto: the selection is disposable and Auto would dispose it. This is a cache that borrows
        // the selection, so cleanup has to forget it, not end its life.
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
        static UIToolkitAnimationSelectionItemBase s_BinderOwner;

        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Auto)]
        static UIAnimationBinder s_BinderForOwner;

        /// <summary>
        /// The binder behind <paramref name="selection"/>, cached until the element table is rebuilt.
        /// Callers that must not act on a stale answer should ask the selection directly instead.
        /// </summary>
        // GetCanonicalBinder can re-walk the panel to find a representative element - the style-rule
        // selection picks whichever element the rule currently matches first - which is far too expensive
        // to repeat for every row on every repaint. That representative can change without the selection
        // changing, so the cache is dropped whenever the element table is rebuilt, which is what adding or
        // removing an element triggers.
        internal static UIAnimationBinder GetBinderFor(UIToolkitAnimationSelectionItemBase selection)
        {
            if (selection == null)
                return null;

            if (!ReferenceEquals(selection, s_BinderOwner))
            {
                s_BinderOwner = selection;
                s_BinderForOwner = selection.GetCanonicalBinder();
            }

            return s_BinderForOwner;
        }

        internal static BindingResolution Resolve(UIAnimationBinder binder, string propertyName) =>
            Resolve(binder, propertyName, out _, out _);

        /// <summary>
        /// <see cref="Resolve(UIAnimationBinder, string)"/> for a caller that already split the property
        /// name - a null <paramref name="elementPath"/> stands for a name that had no path shape.
        /// </summary>
        internal static BindingResolution ResolveElementPath(UIAnimationBinder binder, string elementPath)
        {
            if (binder == null || elementPath == null)
                return BindingResolution.Unknown;

            var lookup = GetPathLookup(binder);
            if (lookup.Count == 0)
                return BindingResolution.Unknown;

            return lookup.ContainsKey(elementPath) ? BindingResolution.Resolved : BindingResolution.Broken;
        }

        // Hands back the split path and the table it was judged against so a caller that needs either
        // does not re-derive them, which would also give it a second chance to observe different state.
        internal static BindingResolution Resolve(UIAnimationBinder binder, string propertyName,
            out string elementPath, out IReadOnlyDictionary<string, VisualElement> lookup)
        {
            elementPath = null;
            lookup = null;

            if (binder == null)
                return BindingResolution.Unknown;

            if (!UIAnimationPath.TrySplitElementPath(propertyName, out elementPath))
                return BindingResolution.Unknown;

            lookup = GetPathLookup(binder);
            if (lookup.Count == 0)
                return BindingResolution.Unknown;

            return lookup.ContainsKey(elementPath) ? BindingResolution.Resolved : BindingResolution.Broken;
        }

        // The binder's own GetVisualElementFromPropertyName is unusable here: it returns null whenever the
        // property name carries no '/', which is exactly how a curve on the animation root itself is
        // stored, so every owner-level curve would read as broken. Reading the element table directly also
        // keeps the empty root path meaningful - it is registered only when exposeRootElement is set.
        internal static IReadOnlyDictionary<string, VisualElement> GetPathLookup(UIAnimationBinder binder)
        {
            if (!ReferenceEquals(binder, s_Binder))
            {
                if (s_Binder != null)
                    s_Binder.ElementCachesClearedEvent -= Invalidate;

                s_Binder = binder;

                if (s_Binder != null)
                    s_Binder.ElementCachesClearedEvent += Invalidate;

                s_LookupDirty = true;
            }

            if (binder == null)
            {
                s_PathLookup.Clear();
                return s_PathLookup;
            }

            binder.UpdateElementNamesIfNeeded();

            if (s_LookupDirty)
            {
                s_PathLookup.Clear();
                var registered = binder.GetRegisteredElements();
                for (var i = 0; i < registered.Count; i++)
                    s_PathLookup[registered[i].Key] = registered[i].Value;
                s_LookupDirty = false;
            }

            return s_PathLookup;
        }

        // ElementCachesClearedEvent fires before the table is repopulated, so the rebuild has to be
        // deferred to the next query rather than done here. The cached binder goes too: the element it
        // was resolved from may no longer be the one its selection would pick now. Emptying the lookup
        // rather than only flagging it drops the element references now instead of holding them until
        // some later query happens to refill it.
        static void Invalidate()
        {
            s_PathLookup.Clear();
            s_LookupDirty = true;
            s_BinderOwner = null;
            s_BinderForOwner = null;
        }

        // Nothing else drops the cached panel within a session: ElementCachesClearedEvent only fires
        // while the binder is alive, so a selection that goes away with its stage would otherwise stay
        // reachable until an unrelated binder was queried.
        internal static void Release(UIToolkitAnimationSelectionItemBase selection)
        {
            // Guarded separately: a null argument matches a null owner, which would drop a cache the
            // caller never owned.
            if (selection == null || !ReferenceEquals(selection, s_BinderOwner))
                return;

            s_BinderOwner = null;
            s_BinderForOwner = null;
            GetPathLookup(null);   // unsubscribes from the binder, drops it and empties the lookup
        }
    }
}
