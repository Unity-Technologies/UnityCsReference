// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Why a <see cref="VisualElement"/> + <see cref="StylePropertyId"/> pair cannot be
    /// recorded. The set is intentionally small and user-actionable: the project setting
    /// can be toggled, the property can be replaced with a recordable one, and the
    /// element can be given a name. <see cref="NoBinderAvailable"/> is the degenerate
    /// "not recording into anything" case (no ancestor <see cref="PanelRenderer"/>):
    /// call sites typically treat it the same as "recording is not applicable here".
    /// <see cref="NoElementSelected"/> is the selection-shape case (probe called with
    /// a null element) and is kept distinct from <see cref="NoBinderAvailable"/> so the
    /// inspector banner can say "no element" instead of "not animatable".
    /// </summary>
    internal enum RecordabilityReason
    {
        Ok,
        NoElementSelected,
        ProjectSettingDisabled,
        PropertyNotRecordable,
        ElementHasNoName,
        NoBinderAvailable,
    }

    /// <summary>
    /// Single source of truth for whether a <see cref="VisualElement"/> + property pair
    /// can be recorded into the active clip. Resolves the <see cref="UIAnimationBinder"/>
    /// that owns the element (the panel-level binder on the ancestor
    /// <see cref="PanelRenderer"/>) and asks it for an addressable path. The binder's
    /// rules are the contract: the element is animatable iff the binder can produce a
    /// path for it. Unnamed ancestors are flattened and duplicate names resolve
    /// first-wins - both strictly less restrictive than the previous string-based rule
    /// that required every ancestor to carry a name.
    /// </summary>
    internal readonly struct VisualElementRecordability
    {
        // Per-reason blocked messages. Shared verbatim between the inspector banner
        // (see VisualElementInspector.UpdateControlsState) and the per-field tooltip
        // composed in StylePropertyBinding so the user sees one consistent explanation
        // in both places. The copy is intentionally action-oriented ("add a component",
        // "give this element a name") so the user knows what to change.
        internal static readonly string k_NoElementSelectedMessage =
            L10n.Tr("Recording disabled: No element selected.");
        internal static readonly string k_ProjectSettingDisabledMessage =
            L10n.Tr("Recording disabled: Enable PanelRenderer animation in UI Toolkit project settings.");
        internal static readonly string k_NoBinderAvailableMessage =
            L10n.Tr("Recording disabled: Add a PanelRenderer component to display this element and enable recording.");
        internal static readonly string k_ElementHasNoNameMessage =
            L10n.Tr("Recording disabled: Give this element a unique name to make it animatable.");
        internal static readonly string k_PropertyNotRecordableMessage =
            L10n.Tr("Recording disabled: This property cannot be recorded in the Animation window.");

        public readonly RecordabilityReason Reason;
        public readonly UIAnimationBinder Binder;
        public readonly string Path;

        VisualElementRecordability(RecordabilityReason reason, UIAnimationBinder binder, string path)
        {
            Reason = reason;
            Binder = binder;
            Path = path;
        }

        public bool CanRecord => Reason == RecordabilityReason.Ok;


        public string GetBlockedMessage()
        {
            switch (Reason)
            {
                case RecordabilityReason.Ok:
                    return null;
                case RecordabilityReason.NoElementSelected:
                    return k_NoElementSelectedMessage;
                case RecordabilityReason.ProjectSettingDisabled:
                    return k_ProjectSettingDisabledMessage;
                case RecordabilityReason.NoBinderAvailable:
                    return k_NoBinderAvailableMessage;
                case RecordabilityReason.ElementHasNoName:
                    return k_ElementHasNoNameMessage;
                case RecordabilityReason.PropertyNotRecordable:
                    return k_PropertyNotRecordableMessage;
                default:
                    return k_PropertyNotRecordableMessage;
            }
        }

        /// <summary>
        /// Probes recordability for a single (element, property) pair. Property checks
        /// short-circuit before touching any binder, so a shorthand or unmapped property
        /// cleanly reports <see cref="RecordabilityReason.PropertyNotRecordable"/>
        /// regardless of element state.
        /// </summary>
        internal static VisualElementRecordability Probe(VisualElement element, StylePropertyId stylePropertyId)
        {
            var propertyReason = ProbeProperty(stylePropertyId);
            if (propertyReason != RecordabilityReason.Ok)
                return new VisualElementRecordability(propertyReason, null, null);

            return ProbeElement(element);
        }

        /// <summary>
        /// Probes recordability for an element only (no specific property). Used for the
        /// inspector's per-element banner and for downstream code that derives the
        /// recordable set once and then asks per-property questions cheaply.
        /// </summary>
        internal static VisualElementRecordability ProbeElement(VisualElement element)
        {
            if (element == null)
                return new VisualElementRecordability(RecordabilityReason.NoElementSelected, null, null);

            if (!UIToolkitProjectSettings.enablePanelRendererAnimation)
                return new VisualElementRecordability(RecordabilityReason.ProjectSettingDisabled, null, null);

            if (!TryFindBinder(element, out var binder))
                return new VisualElementRecordability(RecordabilityReason.NoBinderAvailable, null, null);

            binder.UpdateElementNamesIfNeeded();

            if (!binder.TryGetPathForElement(element, out var path))
                return new VisualElementRecordability(RecordabilityReason.ElementHasNoName, binder, null);

            return new VisualElementRecordability(RecordabilityReason.Ok, binder, path);
        }

        static RecordabilityReason ProbeProperty(StylePropertyId stylePropertyId)
        {
            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return RecordabilityReason.PropertyNotRecordable;
            if (!AnimationRecordingStyleBridge.TryGetBindingPropertyName(stylePropertyId, out _))
                return RecordabilityReason.PropertyNotRecordable;
            return RecordabilityReason.Ok;
        }

        /// <summary>
        /// Resolves the binder responsible for animating <paramref name="element"/>.
        /// Walks up to the <see cref="IPanelComponentRootElement"/> and returns the
        /// panel-level binder on the associated <see cref="PanelRenderer"/>, creating
        /// it if necessary: probing recordability is itself a "now is the time" signal
        /// - the inspector / recording UI is materialising - so lazy creation must not
        /// leak through as a "not animatable" answer.
        /// </summary>
        static bool TryFindBinder(VisualElement element, out UIAnimationBinder binder)
        {
            binder = null;

            var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            if (panelRoot?.panelComponent?.gameObject == null)
                return false;

            var pr = panelRoot.panelComponent as PanelRenderer;
            if (pr == null)
                return false;

            binder = pr.GetOrCreateAnimationBinder();
            return binder != null;
        }
    }
}
