// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// UI Toolkit counterpart to <see cref="AnimationPropertyContextualMenu"/>: builds the
    /// <see cref="PropertyModification"/> array for a style-property field and forwards the
    /// DropdownMenu to <see cref="AnimationPropertyContextualMenu"/>, which owns the shared
    /// Add/Update/Remove/Goto-Key menu layout used by both the IMGUI and UI Toolkit surfaces.
    /// </summary>
    internal static class VisualElementContextualPropertyMenu
    {
        /// <summary>
        /// Appends the animation contextual items to <paramref name="menu"/>. No-ops when the
        /// property is shorthand / unmapped, when the element has no addressable path on the
        /// active responder's target, or when no responder is currently registered.
        /// </summary>
        internal static void Populate(DropdownMenu menu, VisualElement element, StylePropertyId stylePropertyId)
        {
            if (menu == null || element == null)
                return;

            if (!TryBuildModifications(element, stylePropertyId, out var modifications, out var targetObject))
                return;

            // Section break from the inspector's pre-existing items. DropdownMenu.AppendSeparator
            // suppresses leading separators on an empty menu, so this is a no-op when there is
            // nothing above us.
            menu.AppendSeparator();

            AnimationPropertyContextualMenu.Instance.PopulateDropdownContextMenu(menu, modifications, targetObject);
        }

        /// <summary>
        /// Builds the <see cref="PropertyModification"/> array for <paramref name="element"/> +
        /// <paramref name="stylePropertyId"/>. Two shapes depending on the active animation
        /// path: target = UIAnimationClip with bare property paths for per-element clips,
        /// target = PanelRenderer with element-prefixed paths for the panel-wide animator path.
        /// </summary>
        internal static bool TryBuildModifications(
            VisualElement element,
            StylePropertyId stylePropertyId,
            out PropertyModification[] modifications,
            out Object targetObject)
        {
            modifications = null;
            targetObject = null;

            if (element == null)
                return false;

            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return false;

            if (!AnimationRecordingStyleBridge.TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;

            // Per-element clip wins when one is in scope: its path resolution does not depend on
            // the panel-wide naming rule, so an unnamed element backed by a clip still gets a
            // usable menu. Falls back to the panel-wide probe otherwise.
            if (PerElementAnimationContext.TryResolveForElementForProbe(element, out var uiClip, out _, out var perElementPath))
            {
                if (uiClip == null)
                    return false;
                targetObject = uiClip;
                modifications = BuildModificationsForChannels(uiClip, perElementPath, propName, stylePropertyId);
                return modifications != null && modifications.Length > 0;
            }

            var recordability = VisualElementRecordability.ProbeElement(element);
            if (!recordability.CanRecord)
                return false;

            var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            var panelRenderer = panelRoot?.panelComponent as PanelRenderer;
            if (panelRenderer == null)
                return false;

            targetObject = panelRenderer;
            modifications = BuildModificationsForChannels(panelRenderer, recordability.Path, propName, stylePropertyId);
            return modifications != null && modifications.Length > 0;
        }

        // Emits one modification per channel suffix so composite properties (Translate, Color,
        // Filter, etc.) are addressed as their full set of curves; the responder's KeyExists /
        // CurveExists return true if any channel matches.
        static PropertyModification[] BuildModificationsForChannels(
            Object target,
            string elementPath,
            string propName,
            StylePropertyId stylePropertyId)
        {
            var suffixes = UIAnimationBinder.GetChannelSuffixes(stylePropertyId);
            if (suffixes == null || suffixes.Count == 0)
                return null;

            var list = new PropertyModification[suffixes.Count];
            for (int i = 0; i < suffixes.Count; i++)
            {
                var suffix = suffixes[i];
                // Empty suffix => single-channel property; pass null so BuildStyleKeyPropertyName
                // skips the suffix join entirely (no trailing dot).
                var trimmed = string.IsNullOrEmpty(suffix) ? null : suffix;
                var path = AnimationRecordingStyleBridge.BuildStyleKeyPropertyName(elementPath, propName, trimmed);
                list[i] = new PropertyModification
                {
                    target = target,
                    propertyPath = path,
                    value = string.Empty,
                };
            }
            return list;
        }
    }
}
