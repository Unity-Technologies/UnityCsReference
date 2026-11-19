// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // The one spelling and resolution contract for the "${component:<identity>}.<field>" binding selector,
    // shared by the runtime resolver and the editor authoring code so the two can't drift apart.
    // partial: the statics-cleanup generator emits the reload hook for the warn-once set below.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    static partial class ComponentBindingSelector
    {
        internal const string k_Prefix = "${component:";
        internal const char k_Suffix = '}';

        // Ranks how identity names componentType: 2 for the namespace-qualified name, 1 for the short name.
        // A caller resolves on the best rank present, and treats several matches of that rank as ambiguous.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static int MatchRank(Type componentType, string identity)
        {
            if (componentType == null || string.IsNullOrEmpty(identity))
                return 0;

            if (string.Equals(componentType.FullName, identity, StringComparison.Ordinal))
                return 2;

            return string.Equals(componentType.Name, identity, StringComparison.Ordinal) ? 1 : 0;
        }

        // The identity to author for componentType: the short name unless it clashes in the owner's scope.
        internal static string Identity(Type componentType, bool shortNameIsAmbiguous)
            => shortNameIsAmbiguous ? componentType.FullName : componentType.Name;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string Format(Type componentType, bool shortNameIsAmbiguous, string fieldBindingPath)
            => k_Prefix + Identity(componentType, shortNameIsAmbiguous) + k_Suffix + "." + fieldBindingPath;

        // Splits "${component:<identity>}.<field>" given as one string. False for anything that isn't a
        // component selector, so the caller falls back to the element's own property path.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool TrySplit(string bindingPath, out string identity, out string fieldPath)
        {
            identity = null;
            fieldPath = null;

            if (string.IsNullOrEmpty(bindingPath) || !bindingPath.StartsWith(k_Prefix, StringComparison.Ordinal))
                return false;

            // The identity runs to the closing brace; a '.' and a non-empty field path must follow it.
            var close = bindingPath.IndexOf(k_Suffix, k_Prefix.Length);
            if (close <= k_Prefix.Length || close + 2 >= bindingPath.Length || bindingPath[close + 1] != '.')
                return false;

            identity = bindingPath.Substring(k_Prefix.Length, close - k_Prefix.Length);
            fieldPath = bindingPath.Substring(close + 2);
            return true;
        }

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<string> s_AmbiguousIdentitiesWarned = new HashSet<string>();

        // Warn once per clashing identity: the runtime re-resolves on every binding update and the editor on
        // every inspector rebuild, so one diagnostic per identity, then the binding is skipped.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void WarnAmbiguous(string identity)
        {
            if (s_AmbiguousIdentitiesWarned.Add(identity))
                Debug.LogWarning($"[UI Toolkit] The component data binding selector '{k_Prefix}{identity}{k_Suffix}' is ambiguous: it matches more than one component. The binding is skipped. Use the namespace-qualified component type name to select one.");
        }

        // Tests that assert the diagnostic need the warn-once set empty, since it outlives a single test.
        internal static void ClearAmbiguityWarnings() => s_AmbiguousIdentitiesWarned.Clear();
    }
}
