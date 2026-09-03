// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.AnimationWindowBuiltin;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Shared base for UI Toolkit Animation Window selection items. GameObject-less selections
    /// own an optional <see cref="UIAnimationClip"/> + inner <see cref="AnimationClip"/>; this
    /// base centralizes the create clip pipeline and the clip-state plumbing
    /// (<see cref="m_UIClip"/> / <see cref="m_Clip"/> / <see cref="SetUIClip"/>) so the
    /// curve-editing surface stays consistent. Subclasses parameterize the dialog subject,
    /// the target-side assignment, and the per-target read/write hooks.
    /// </summary>
    internal abstract class UIToolkitAnimationSelectionItemBase : IAnimationWindowSelectionItem
    {
        protected readonly AnimationWindow m_Window;
        protected IAnimationWindowController m_Controller;
        protected UIAnimationClip m_UIClip;
        protected AnimationWindowClip m_Clip;

        // Full ordered clip list for the dropdown; the active clip (m_UIClip) is always one of these.
        readonly List<UIAnimationClip> m_UIClips = new();
        readonly List<UIAnimationClip> m_ScratchClips = new();

        protected static readonly string k_OnboardingLabelFormat = L10n.Tr("To begin animating {0}, create a UI Animation Clip.", null);
        protected UIToolkitAnimationSelectionItemBase(AnimationWindow window)
        {
            m_Window = window;
            AnimationUtility.onCurveWasModified += CurveWasModified;
        }

        internal AnimationWindow animationWindow => m_Window;
        internal UIAnimationClip uiAnimationClip => m_UIClip;

        // GameObject-less selections; null engages the Animation Window's curve-display fallbacks
        // and keeps the onboarding panel in the no-GameObject branch (see Layout.Update).
        public GameObject gameObject => null;
        public GameObject rootGameObject => null;
        public Component animationPlayer => null;

        public IAnimationWindowController controller => m_Controller;
        public bool canSyncSceneSelection => true;

        public bool isImported => false;
        public bool hasUnsavedChanges => false;
        public void SaveChanges() { }
        public void DiscardChanges() { }

        // Subject string fed into AnimationClipNewButtonController's save dialog.
        // Null means "use the controller's default 'the selected element' wording".
        protected abstract string DialogSubjectName { get; }

        // Persist the freshly-created UIAnimationClip on the subclass-specific target
        // (inline VTA sheet for a VisualElement, ...).
        protected abstract void AssignClipToTarget(UIAnimationClip newClip);

        // Bookkeeping after the asset has been created and assigned. Wires the new clip into the shared clip-state fields and the dropdown list.
        protected virtual void OnClipAssigned(UIAnimationClip newClip)
        {
            if (newClip != null && !m_UIClips.Contains(newClip))
            {
                m_UIClips.Add(newClip);
                m_ClipsCacheDirty = true;
            }
            SetUIClip(newClip);
        }

        // Re-point the shared clip-state fields at a (possibly null) UIAnimationClip, creating the
        // inner AnimationClip on demand. Subclasses go through this so the curve editor /
        // dope sheet always sees a consistent wrapper. This changes the ACTIVE clip only; the
        // dropdown list (m_UIClips) is maintained separately by ReconcileClips / OnClipAssigned.
        protected void SetUIClip(UIAnimationClip uiClip)
        {
            if (uiClip != null && uiClip.animationClip == null)
                EnsureInnerAnimationClip(uiClip);

            m_UIClip = uiClip;
            var inner = uiClip != null ? uiClip.animationClip : null;
            m_Clip = inner != null ? new VisualElementAnimationWindowClip(uiClip) : null;
            m_ClipsCacheDirty = true;
        }

        // Without an inner AnimationClip, `disabled` reports true and the Animation Window paints
        // "No animatable object selected" (or the staging onboarding text when one is exposed).
        static void EnsureInnerAnimationClip(UIAnimationClip uiClip)
        {
            var innerClip = new AnimationClip();
            var settings = AnimationUtility.GetAnimationClipSettings(innerClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(innerClip, settings);

            uiClip.animationClip = innerClip;

            if (AssetDatabase.IsMainAsset(uiClip))
            {
                innerClip.name = uiClip.name;
                // The inner clip is an implementation detail; hide it from the Project window and Inspector.
                innerClip.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(innerClip, uiClip);
                EditorUtility.SetDirty(uiClip);
            }
        }

        // Drop the current clip + list and let subclasses tear down their controller-side state.
        protected void ClearClip()
        {
            bool hadClip = m_Clip != null;
            if (!hadClip && m_UIClips.Count == 0)
                return;

            if (hadClip)
                OnClipCleared();
            m_UIClips.Clear();
            SetUIClip(null);
        }

        // Hook for subclasses to tear down controller-side state when the clip is being cleared
        // (e.g. stop any in-flight recording session). Default is a no-op.
        protected virtual void OnClipCleared() { }

        // AnimationWindowState routes this event to builtin items only; a UI Toolkit one listens for itself.
        void CurveWasModified(AnimationClip clip, EditorCurveBinding binding,
            AnimationUtility.CurveModifiedType type)
        {
            if (clip == null || m_UIClip == null || clip != m_UIClip.animationClip)
                return;

            OnClipCurvesChanged(type);

            if (m_Window == null)
                return;

            if (type == AnimationUtility.CurveModifiedType.CurveModified)
                m_Window.RefreshCurve(binding);
            else
                m_Window.RefreshClip();

            m_Window.Repaint();
        }

        // Hook for subclasses to follow the clip's curves on the target side. Default is a no-op.
        protected virtual void OnClipCurvesChanged(AnimationUtility.CurveModifiedType type) { }

        UIAnimationClip TryCreateAndAssignNewUIAnimationClip()
        {
            if (!canCreateClips)
                return null;

            var newUIClip = AnimationClipNewButtonController.CreateNewUIAnimationClipFromDialog(
                DialogSubjectName,
                AssignClipToTarget);
            if (newUIClip != null)
                OnClipAssigned(newUIClip);
            return newUIClip;
        }

        public virtual IAnimationWindowClip CreateNewClip(string suggestedName = null)
        {
            return TryCreateAndAssignNewUIAnimationClip() != null ? clip : null;
        }

        // Return value reflects whether a clip was created, independent of whether `clip` is non-null.
        public virtual bool InitializeSelection() => TryCreateAndAssignNewUIAnimationClip() != null;

        // Test-only entry that bypasses the SaveFilePanel dialog by accepting an explicit asset path.
        internal UIAnimationClip CreateAndAssignNewUIAnimationClip(string path)
        {
            if (!canCreateClips)
                return null;

            var newUIClip = UIAnimationClipFactory.CreateAssetAndAssignToField(path, AssignClipToTarget);
            if (newUIClip != null)
                OnClipAssigned(newUIClip);
            return newUIClip;
        }

        public virtual IAnimationWindowClip clip
        {
            get => m_Clip;
            // The dropdown sets clip to the picked wrapper; re-point the active UIAnimationClip so the controller follows, or store m_Clip for an unrecognized wrapper.
            set
            {
                var incoming = value as AnimationWindowClip;
                var matched = FindUIClipFor(incoming);
                if (matched != null && matched != m_UIClip)
                    SetUIClip(matched);
                else
                    m_Clip = incoming;
            }
        }

        // Activates one of this target's own clips, as picking it in the clip dropdown would. Clips the
        // target does not own are refused so an unrelated caller cannot repoint the window at a clip this
        // selection has no context for; Synchronize then keeps the pick and the refresh hash rebuilds the view.
        internal bool TrySetActiveClip(UIAnimationClip uiClip)
        {
            if (uiClip == null)
                return false;
            if (m_UIClip == uiClip)
                return true;
            if (!m_UIClips.Contains(uiClip))
                return false;

            SetUIClip(uiClip);
            return true;
        }

        // Match a dropdown wrapper to its UIAnimationClip by inner AnimationClip identity (AnimationWindowClip's equality).
        UIAnimationClip FindUIClipFor(AnimationWindowClip wrapper)
        {
            var inner = wrapper?.animationClip;
            if (inner == null)
                return null;
            for (int i = 0; i < m_UIClips.Count; i++)
            {
                var candidate = m_UIClips[i];
                if (candidate != null && candidate.animationClip == inner)
                    return candidate;
            }
            // The active clip may not have been reconciled into the list yet (fresh create).
            if (m_UIClip != null && m_UIClip.animationClip == inner)
                return m_UIClip;
            return null;
        }

        public virtual bool disabled => m_Clip == null || !m_Clip.isValid;
        // Not m_Clip.isReadOnly: the field's static type binds to AnimationWindowClip's equality test
        // rather than the wrapper's, which would let the window offer edits the write layer refuses.
        public bool isReadOnly => m_Clip != null && m_Clip.isValid && VisualElementAnimationWindowClip.IsReadOnly(m_Clip.animationClip);
        public virtual bool canChangeClip => true;
        // Gating on `disabled` keeps the toolbar "+" popup and the inline tree-row "Add Property"
        // button in sync; otherwise the popup lists properties that CreateDefaultCurves would
        // silently drop because selection.clip is null.
        public virtual bool canAddCurves => !disabled && !isReadOnly;
        public abstract bool canCreateClips { get; }

        IAnimationWindowClip[] m_ClipsCache;
        bool m_ClipsCacheDirty = true;

        // The active clip's entry is m_Clip itself, so selection.clip stays reference-equal to its dropdown entry.
        public virtual IAnimationWindowClip[] GetClips()
        {
            // Before the first Synchronize the list is empty; surface just the active clip.
            if (m_UIClips.Count == 0)
            {
                if (m_Clip == null || !m_Clip.isValid)
                    return Array.Empty<IAnimationWindowClip>();
                if (m_ClipsCache == null || m_ClipsCache.Length != 1 || m_ClipsCache[0] != m_Clip)
                    m_ClipsCache = new IAnimationWindowClip[] { m_Clip };
                return m_ClipsCache;
            }

            if (m_ClipsCacheDirty || m_ClipsCache == null || m_ClipsCache.Length != CountValidClips())
                RebuildClipsCache();
            return m_ClipsCache;
        }

        int CountValidClips()
        {
            int count = 0;
            for (int i = 0; i < m_UIClips.Count; i++)
            {
                var c = m_UIClips[i];
                if (c != null && c.animationClip != null)
                    count++;
            }
            return count;
        }

        void RebuildClipsCache()
        {
            int count = CountValidClips();
            if (count == 0)
            {
                m_ClipsCache = Array.Empty<IAnimationWindowClip>();
                m_ClipsCacheDirty = false;
                return;
            }

            var cache = new IAnimationWindowClip[count];
            int w = 0;
            for (int i = 0; i < m_UIClips.Count; i++)
            {
                var c = m_UIClips[i];
                if (c == null || c.animationClip == null)
                    continue;
                cache[w++] = (c == m_UIClip && m_Clip != null && m_Clip.isValid)
                    ? m_Clip
                    : new VisualElementAnimationWindowClip(c);
            }
            m_ClipsCache = cache;
            m_ClipsCacheDirty = false;
        }

        // Preserves the active clip if it survived the refresh (so a dropdown pick / in-flight preview sticks), else falls back to the first clip.
        protected void ReconcileClips(IEnumerable<UIAnimationClip> resolvedClips)
        {
            m_ScratchClips.Clear();
            if (resolvedClips != null)
            {
                foreach (var uiClip in resolvedClips)
                {
                    if (uiClip == null || m_ScratchClips.Contains(uiClip))
                        continue;
                    m_ScratchClips.Add(uiClip);
                }
            }

            if (m_ScratchClips.Count == 0)
            {
                ClearClip();
                return;
            }

            bool ensuredInner = false;
            for (int i = 0; i < m_ScratchClips.Count; i++)
            {
                var c = m_ScratchClips[i];
                if (c.animationClip == null)
                {
                    EnsureInnerAnimationClip(c);
                    ensuredInner = true;
                }
            }

            if (!SameSequence(m_UIClips, m_ScratchClips))
            {
                m_UIClips.Clear();
                m_UIClips.AddRange(m_ScratchClips);
                m_ClipsCacheDirty = true;
            }
            else if (ensuredInner)
            {
                m_ClipsCacheDirty = true;
            }

            var active = (m_UIClip != null && m_UIClips.Contains(m_UIClip)) ? m_UIClip : m_UIClips[0];
            if (active != m_UIClip || m_Clip == null || m_Clip.animationClip != active.animationClip)
                SetUIClip(active);
        }

        static bool SameSequence(List<UIAnimationClip> a, List<UIAnimationClip> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i], b[i]))
                    return false;
            }
            return true;
        }

        protected List<UIAnimationClip> BuildClipListWithAppended(UIAnimationClip newClip)
        {
            var list = new List<UIAnimationClip>(m_UIClips.Count + 1);
            for (int i = 0; i < m_UIClips.Count; i++)
            {
                var c = m_UIClips[i];
                if (c != null && c != newClip)
                    list.Add(c);
            }
            if (newClip != null)
                list.Add(newClip);
            return list;
        }

        public virtual void Synchronize() { }
        public virtual EditorCurveBinding[] GetAnimatableBindings(GameObject _) => Array.Empty<EditorCurveBinding>();
        public virtual EditorCurveBinding[] GetAnimatableBindings() => Array.Empty<EditorCurveBinding>();

        // Per-element clips have no GameObject root, so rows self-describe their kind: discrete int
        // curves stay int, PPtr rows carry their own type, everything else is a continuous float.
        public virtual Type GetValueType(EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
                return null;
            if (binding.isDiscreteCurve)
                return typeof(int);
            return typeof(float);
        }

        // Resolves a binder's animatable bindings; all rows share the UIAnimationClip type.
        protected static EditorCurveBinding[] GetAnimatableBindingsFromBinder(UIAnimationBinder binder)
        {
            if (binder == null)
                return Array.Empty<EditorCurveBinding>();

            binder.UpdateElementNamesIfNeeded();
            return UIAnimationBinderEditorBindings.GetAllAnimatableProperties(binder, typeof(UIAnimationClip));
        }

        public virtual string onboardingLabel => null;

        // --- Preview / record surface driven by VisualElementAnimationWindowController ---
        // Element-less selections override these; the inert defaults make a target do nothing.

        // Invokes the action on every (element, binder) pair this target drives (preview fan-out).
        internal virtual void ForEachPreviewTarget(Action<VisualElement, UIAnimationBinder> action) { }

        // Representative binder for single-target work (snapshot, default reads, post-sample identity).
        internal virtual UIAnimationBinder GetCanonicalBinder() => null;

        // Registers/clears the inspector recording-context this target routes style edits through.
        internal virtual void ActivateRecordingContext(UIAnimationClip clip, AnimationModeDriver driver) { }
        internal virtual void DeactivateRecordingContext(UIAnimationClip clip) { }

        public abstract int GetRefreshHash();
        public abstract bool IsCompatibleWith(UnityEngine.Object selectedObject);

        public virtual void Dispose()
        {
            AnimationUtility.onCurveWasModified -= CurveWasModified;
            UIAnimationBindingResolution.Release(this);
            m_Controller?.Dispose();
            m_Controller = null;
        }
    }
}
