// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        protected static readonly string k_OnboardingLabelFormat = L10n.Tr("To begin animating {0}, create a UI Animation Clip.");
        protected UIToolkitAnimationSelectionItemBase(AnimationWindow window)
        {
            m_Window = window;
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

        // Bookkeeping after the asset has been created and assigned. Default wires the new clip
        // into the shared clip-state fields so the Animation Window surfaces the new curves immediately.
        protected virtual void OnClipAssigned(UIAnimationClip newClip) => SetUIClip(newClip);

        // Re-point the shared clip-state fields at a (possibly null) UIAnimationClip, creating the
        // inner AnimationClip on demand. Subclasses go through this so the curve editor /
        // dope sheet always sees a consistent wrapper.
        protected void SetUIClip(UIAnimationClip uiClip)
        {
            if (uiClip != null && uiClip.animationClip == null)
                EnsureInnerAnimationClip(uiClip);

            m_UIClip = uiClip;
            var inner = uiClip != null ? uiClip.animationClip : null;
            m_Clip = inner != null ? new VisualElementAnimationWindowClip(inner) : null;
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

        // Drop the current clip and let subclasses tear down their controller-side state.
        protected void ClearClip()
        {
            if (m_Clip == null)
                return;

            OnClipCleared();
            SetUIClip(null);
        }

        // Hook for subclasses to tear down controller-side state when the clip is being cleared
        // (e.g. stop any in-flight recording session). Default is a no-op.
        protected virtual void OnClipCleared() { }

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

        public virtual IAnimationWindowClip CreateNewClip()
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
            set => m_Clip = value as AnimationWindowClip;
        }

        public virtual bool disabled => m_Clip == null || !m_Clip.isValid;
        public bool isReadOnly => m_Clip != null && m_Clip.isReadOnly;
        public virtual bool canChangeClip => true;
        // Gating on `disabled` keeps the toolbar "+" popup and the inline tree-row "Add Property"
        // button in sync; otherwise the popup lists properties that CreateDefaultCurves would
        // silently drop because selection.clip is null.
        public virtual bool canAddCurves => !disabled && !isReadOnly;
        public abstract bool canCreateClips { get; }

        private IAnimationWindowClip[] m_ClipsCache;
        public virtual IAnimationWindowClip[] GetClips()
        {
            if (m_Clip == null || !m_Clip.isValid)
                return Array.Empty<IAnimationWindowClip>();

            if (m_ClipsCache == null || m_ClipsCache[0] != m_Clip)
            {
                m_ClipsCache = new IAnimationWindowClip[] { m_Clip };
            }
            return m_ClipsCache;
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
            m_Controller?.Dispose();
            m_Controller = null;
        }
    }
}
