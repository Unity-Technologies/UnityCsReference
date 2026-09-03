// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Accessibility;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Maps <see cref="VisualElement"/> types to the <see cref="AccessibilityRole"/> their generated
    /// accessibility nodes carry.
    /// </summary>
    /// <remarks>
    /// The registry only answers "what role does this element type have"; whether an unregistered
    /// element produces a node at all (kept, flattened or omitted) is decided by
    /// <see cref="AccessibilityTreeGenerator"/>. Unregistered types deliberately have no role here so
    /// they can never be announced as interactive.
    /// </remarks>
    internal static class AccessibilityRoleRegistry
    {
        // Immutable after construction and only ever holds engine types from this module, so
        // nothing here can pin a user assembly across a code reload; safe to persist.
        [NoAutoStaticsCleanup]
        static readonly Dictionary<Type, AccessibilityRole> s_Roles = new()
        {
            { typeof(Button), AccessibilityRole.Button },
            { typeof(Label), AccessibilityRole.StaticText },
            { typeof(Toggle), AccessibilityRole.Toggle },
            { typeof(RadioButton), AccessibilityRole.Toggle },
            { typeof(TextInputBaseField<>), AccessibilityRole.TextField },
            { typeof(BaseSlider<>), AccessibilityRole.Slider },
            { typeof(BasePopupField<,>), AccessibilityRole.Dropdown },
            { typeof(ScrollView), AccessibilityRole.ScrollView },
        };

        /// <summary>
        /// Finds the role registered for the element's type, walking up the inheritance chain so
        /// derived controls (for example a custom Button subclass) keep their base control's role.
        /// Along the walk, a generic type also matches a role registered for its open definition,
        /// so control families like <c>TextInputBaseField&lt;T&gt;</c> register once for every
        /// value type.
        /// </summary>
        public static bool TryGetRole(VisualElement element, out AccessibilityRole role)
        {
            for (var type = element.GetType(); type != null && typeof(VisualElement).IsAssignableFrom(type); type = type.BaseType)
            {
                if (s_Roles.TryGetValue(type, out role))
                    return true;

                if (type.IsGenericType && s_Roles.TryGetValue(type.GetGenericTypeDefinition(), out role))
                    return true;
            }

            role = AccessibilityRole.None;
            return false;
        }
    }
}
