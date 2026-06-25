// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Holds the set of longhand style property IDs that can be recorded for the current inspector target.
/// Null on <see cref="StyleInspectorElement.AuthoringContext.AnimationController"/> when animation recording is
/// not active; non-null while the inspector should apply recordability (field enabled state and recording routing).
/// Created exclusively by <see cref="VisualElementSelectionEditor"/> (or test helpers).
/// </summary>
internal sealed class StyleInspectorAnimationRecordingContext
{
    readonly HashSet<StylePropertyId> m_RecordableLonghandIds;

    StyleInspectorAnimationRecordingContext(HashSet<StylePropertyId> recordableLonghandIds)
    {
        m_RecordableLonghandIds = recordableLonghandIds;
    }

    /// <summary>
    /// Builds context from the current editor recording state and hierarchy.
    /// Returns null when animation recording is not active.
    /// Returns a context with an empty recordable set when the element is not animatable.
    /// Returns a context with the full recordable set for animatable elements.
    /// </summary>
    internal static StyleInspectorAnimationRecordingContext TryCreateForElement(VisualElement element)
    {
        if (element == null || !AnimationMode.InAnimationRecording())
            return null;

        if (TryCreatePerElementContext(element, out var perElementContext))
            return perElementContext;

        var recordability = VisualElementRecordability.ProbeElement(element);
        if (!recordability.CanRecord)
            return new StyleInspectorAnimationRecordingContext(s_EmptyRecordableSet);

        var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
        if (panelRoot?.panelComponent?.gameObject == null)
            return new StyleInspectorAnimationRecordingContext(s_EmptyRecordableSet);

        return new StyleInspectorAnimationRecordingContext(
            AnimationRecordingStyleBridge.GetRecordableStylePropertyIds(panelRoot.panelComponent.gameObject));
    }

    // Arms recording for a selected USS rule, but only while the Animation Window drives this rule's
    // clip. A rule applies at each matched element's root, so it reuses the per-element recordable set.
    internal static StyleInspectorAnimationRecordingContext TryCreateForRule(StyleRule rule)
    {
        if (rule == null || !AnimationMode.InAnimationRecording())
            return null;

        if (!StyleRuleAnimationContext.TryResolveForRule(rule, out _))
            return null;

        return new StyleInspectorAnimationRecordingContext(GetPerElementRecordableSet());
    }

    static bool TryCreatePerElementContext(VisualElement element, out StyleInspectorAnimationRecordingContext context)
    {
        context = null;

        var clipOwner = VisualElementAnimationClipUtility.FindClipOwner(element);
        if (clipOwner == null)
            return false;

        var concretePanel = clipOwner.panel as Panel;
        var binder = concretePanel?.GetOrCreateElementBinder(clipOwner);
        if (binder == null)
            return false;

        binder.UpdateElementNamesIfNeeded();
        if (!binder.TryGetPathForElement(element, out _))
            return false;

        context = new StyleInspectorAnimationRecordingContext(GetPerElementRecordableSet());
        return true;
    }

    static HashSet<StylePropertyId> s_PerElementRecordableSet;

    static HashSet<StylePropertyId> GetPerElementRecordableSet()
    {
        if (s_PerElementRecordableSet != null)
            return s_PerElementRecordableSet;

        var set = new HashSet<StylePropertyId>();
        int idCount = UIAnimationBinder.StylePropertyIdCount;
        for (int idValue = 1; idValue < idCount; idValue++)
        {
            var id = (StylePropertyId)idValue;
            if (StyleDebug.IsShorthandProperty(id))
                continue;
            if (UIAnimationBinder.GetChannelCount(id) > 0)
                set.Add(id);
        }
        s_PerElementRecordableSet = set;
        return set;
    }

    /// <summary>
    /// Test-only: simulates a recordable set without requiring a full PanelRenderer / binding pipeline.
    /// </summary>
    internal static StyleInspectorAnimationRecordingContext CreateForTests(HashSet<StylePropertyId> recordableLonghandIds)
    {
        return new StyleInspectorAnimationRecordingContext(recordableLonghandIds ?? new HashSet<StylePropertyId>());
    }

    static readonly HashSet<StylePropertyId> s_EmptyRecordableSet = new();

    internal bool HasRecordableProperties => m_RecordableLonghandIds.Count > 0;

    internal bool IsPropertyRecordable(StylePropertyId id)
    {
        if (!StyleDebug.IsShorthandProperty(id))
            return m_RecordableLonghandIds.Contains(id);

        using var listHandle = ListPool<StylePropertyId>.Get(out var longhandIds);
        StyleDebug.PopulateLonghandPropertyIds(id, longhandIds);
        foreach (var longhandId in longhandIds)
        {
            if (m_RecordableLonghandIds.Contains(longhandId))
                return true;
        }

        return false;
    }
}
