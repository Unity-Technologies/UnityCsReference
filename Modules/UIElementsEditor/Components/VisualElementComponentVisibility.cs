// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Whether a UI Toolkit component type is surfaced in the editor's component UI: the VisualElement
    /// inspector, the UI Builder inspector, and the UI Toolkit Debugger. A component struct marked
    /// <see cref="HideInInspector"/> is an implementation detail and appears in none of them.
    /// </summary>
    /// <remarks>
    /// This is orthogonal to <see cref="UnityEngine.UIElements.VisualElementComponentAttribute.exposeToUxml"/>,
    /// which decides only whether the component can be authored in UXML. A hidden component that is
    /// exposed to UXML still imports, exports, and appears in the schema; it is only absent from the UI.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static partial class VisualElementComponentVisibility
    {
        // Answered per type on the first query for it, so the map only ever holds the component types the
        // UI asked about. The Type keys would pin the outgoing scope, so it is cleared on reload.
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, bool> s_Hidden = new();

        public static bool IsHidden(Type componentType)
        {
            if (componentType == null)
                return false;

            if (s_Hidden.TryGetValue(componentType, out var hidden))
                return hidden;

            hidden = Attribute.IsDefined(componentType, typeof(HideInInspector), inherit: false);
            s_Hidden[componentType] = hidden;
            return hidden;
        }
    }
}
