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
    /// Builds context from the current editor recording state and hierarchy. Returns null when
    /// animation recording is not active or the element is not under an animatable panel.
    /// </summary>
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

        var recordability = VisualElementRecordability.ProbeElement(element);
        if (!recordability.CanRecord)
            return new StyleInspectorAnimationRecordingContext(s_EmptyRecordableSet);

        var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
        if (panelRoot?.panelComponent?.gameObject == null)
            return new StyleInspectorAnimationRecordingContext(s_EmptyRecordableSet);

        return new StyleInspectorAnimationRecordingContext(
            AnimationRecordingStyleBridge.GetRecordableStylePropertyIds(panelRoot.panelComponent.gameObject));
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
