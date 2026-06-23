// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Animation Window selection item for per-element <see cref="UIAnimationClip"/>s.
    /// There is no scene proxy: the "Add Property" menu is populated directly from the
    /// per-element <see cref="UIAnimationBinder"/>'s channel set. Clip-state plumbing
    /// (the <see cref="UIAnimationClip"/> / inner <see cref="AnimationClip"/> wrappers,
    /// the Create call-to-action pipeline, the GameObject-less IAnimationWindowSelectionItem surface)
    /// is inherited from <see cref="UIToolkitAnimationSelectionItemBase"/>; this subclass
    /// only carries the VE/PanelRenderer-specific bits.
    /// </summary>
    internal sealed class VisualElementAnimationSelectionItem : UIToolkitAnimationSelectionItemBase
    {
        static readonly string k_OnboardingLabelUnnamedSubject = L10n.Tr("this VisualElement");

        readonly PanelRenderer m_PanelRenderer;
        readonly VisualElement m_ClipOwner;

        VisualElementAnimationSelectionItem(
            AnimationWindow window,
            PanelRenderer panelRenderer,
            VisualElement clipOwner,
            UIAnimationClip uiClip)
            : base(window)
        {
            m_PanelRenderer = panelRenderer;
            m_ClipOwner = clipOwner;
            SetUIClip(uiClip);

            AnimationUtility.onCurveWasModified += CurveWasModified;
        }

        internal static VisualElementAnimationSelectionItem Create(
            AnimationWindow window,
            PanelRenderer panelRenderer,
            VisualElement clipOwner,
            UIAnimationClip uiClip)
        {
            var item = new VisualElementAnimationSelectionItem(window, panelRenderer, clipOwner, uiClip);
            item.m_Controller = new VisualElementAnimationWindowController(item);
            return item;
        }

        internal VisualElement clipOwner => m_ClipOwner;
        internal PanelRenderer panelRenderer => m_PanelRenderer;

        internal UIAnimationBinder GetOrCreateElementBinder()
        {
            var concretePanel = m_ClipOwner?.panel as Panel;
            return concretePanel?.GetOrCreateElementBinder(m_ClipOwner);
        }

        // A per-element selection drives exactly one (element, binder) pair: the clip owner.
        internal override void ForEachPreviewTarget(Action<VisualElement, UIAnimationBinder> action)
        {
            if (action == null || m_ClipOwner == null)
                return;
            var binder = GetOrCreateElementBinder();
            if (binder == null)
                return;
            action(m_ClipOwner, binder);
        }

        internal override UIAnimationBinder GetCanonicalBinder() => GetOrCreateElementBinder();

        // Routes inspector style edits to the per-element clip target (AnimationRecordingStyleBridge
        // resolves through PerElementAnimationContext).
        internal override void ActivateRecordingContext(UIAnimationClip clip, AnimationModeDriver driver)
        {
            if (clip == null || m_ClipOwner == null)
                return;
            var binder = GetOrCreateElementBinder();
            if (binder == null)
                return;
            PerElementAnimationContext.SetActive(clip, m_ClipOwner, binder, driver);
        }

        internal override void DeactivateRecordingContext(UIAnimationClip clip) => PerElementAnimationContext.ClearActive(clip);

        // We currently avoid creating clips or modifying VisualTreeAssets in the Main stage.
        public override bool canCreateClips => m_ClipOwner != null && IsInVisualElementEditingStage();

        static bool IsInVisualElementEditingStage() => StageUtility.GetCurrentStage() is VisualElementEditingStage;

        protected override string DialogSubjectName
        {
            get
            {
                if (m_ClipOwner == null)
                    return "VisualElement";
                if (!string.IsNullOrEmpty(m_ClipOwner.name))
                    return m_ClipOwner.name;
                return m_PanelRenderer != null ? m_PanelRenderer.gameObject.name : "VisualElement";
            }
        }

        // Persists unityAnimationClip via the same inline-sheet command the inspector "New..." button uses
        // when the element is VTA-backed (staging or VTA-cloned scene); falls back to runtime-only otherwise.
        protected override void AssignClipToTarget(UIAnimationClip newClip)
        {
            var styleValue = new StyleUIAnimationClip(newClip);

            if (m_ClipOwner.visualElementAsset != null && m_ClipOwner.visualTreeAssetSource != null)
            {
                SetInlineStylePropertyCommand<StyleUIAnimationClip>.Execute(CommandSources.Inspector,
                    m_ClipOwner,
                    StylePropertyId.UnityAnimationClip,
                    StylePropertyBinding.SetUIAnimationClip,
                    styleValue);
                return;
            }

            m_ClipOwner.style.unityAnimationClip = styleValue;
        }

        // The VE controller drives preview / record on m_ClipOwner; stop it before dropping the clip
        // so the next selection-change event starts from a clean slate.
        protected override void OnClipCleared()
        {
            (m_Controller as VisualElementAnimationWindowController)?.StopImmediately();
        }

        string m_CachedOnboardingLabel;
        bool m_IsOnboardingLabelCached;

        public override string onboardingLabel
        {
            get
            {
                if (!m_IsOnboardingLabelCached)
                {
                    if (!IsInVisualElementEditingStage())
                    {
                        m_CachedOnboardingLabel = null;
                    }
                    else
                    {
                        var subject = (m_ClipOwner != null && !string.IsNullOrEmpty(m_ClipOwner.name))
                            ? $"'#{m_ClipOwner.name}'"
                            : k_OnboardingLabelUnnamedSubject;
                        m_CachedOnboardingLabel = string.Format(k_OnboardingLabelFormat, subject);
                    }

                    // The cached text might become stale if the VisualElement name changes;
                    // changing the selection refreshes it by creating a new selection item.
                    m_IsOnboardingLabelCached = true;
                }
                return m_CachedOnboardingLabel;
            }
        }

        public override int GetRefreshHash()
        {
            var animClip = m_Clip?.animationClip;
            uint dirtyCount = animClip != null ? (uint)EditorUtility.GetDirtyCount(animClip) : 0;
            return new Hash128(
                (uint)(animClip != null ? animClip.GetHashCode() : 0),
                (uint)(m_ClipOwner != null ? m_ClipOwner.GetHashCode() : 0),
                (uint)(m_PanelRenderer != null ? m_PanelRenderer.GetHashCode() : 0),
                dirtyCount).GetHashCode();
        }

        public override void Synchronize()
        {
            // Treat a released or detached owner as "no clip" - reading resolvedStyle on a recycled
            // layout node throws from LayoutDataAccess. The responder picks up a fresh selection on
            // the next selection-change event (Animator-deletion behavior).
            if (m_ClipOwner == null || m_ClipOwner.resourcesReleased)
            {
                ClearClip();
                return;
            }

            var resolvedClip = m_ClipOwner.resolvedStyle.unityAnimationClip;
            if (resolvedClip == null)
            {
                ClearClip();
                return;
            }

            if (resolvedClip != m_UIClip)
                SetUIClip(resolvedClip);
        }

        public override bool IsCompatibleWith(UnityEngine.Object selectedObject)
        {
            if (selectedObject is VisualElementSelection veSelection && veSelection.Element != null)
            {
                var owner = VisualElementAnimationClipUtility.FindClipOwner(veSelection.Element);
                return owner != null && owner == m_ClipOwner;
            }
            return false;
        }

        public override EditorCurveBinding[] GetAnimatableBindings(GameObject go) => GetAnimatableBindings();

        public override EditorCurveBinding[] GetAnimatableBindings()
            => m_ClipOwner == null ? Array.Empty<EditorCurveBinding>() : GetAnimatableBindingsFromBinder(GetOrCreateElementBinder());

        // UI refresh hook, mirroring AnimationWindowSelectionItem.CurveWasModified. The per-element
        // deltas (binder housekeeping) replace what DrivenPropertyManager pruning does automatically
        // for GameObject paths.
        void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            if (clip == null || m_UIClip == null || clip != m_UIClip.animationClip)
                return;

            var binder = GetOrCreateElementBinder();
            if (binder != null)
            {
                // Drop stale entries for removed curves so binder.IsBound - the inspector affordance
                // signal - reflects the clip's current curves; the next sample re-populates surviving bindings.
                if (type != AnimationUtility.CurveModifiedType.CurveModified)
                    binder.ClearBindings();
                binder.IncrementBoundElementsStyleVersion();
            }

            if (m_Window == null)
                return;

            if (type == AnimationUtility.CurveModifiedType.CurveModified)
                m_Window.RefreshCurve(binding);
            else
                m_Window.RefreshClip();
            m_Window.Repaint();
        }

        public override void Dispose()
        {
            AnimationUtility.onCurveWasModified -= CurveWasModified;
            base.Dispose();
        }
    }
}
