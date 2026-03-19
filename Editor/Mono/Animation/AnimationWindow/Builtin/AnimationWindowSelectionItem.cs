// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.AnimationWindowBuiltin
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    abstract class AnimationWindowSelectionItem : System.IEquatable<AnimationWindowSelectionItem>, IAnimationWindowSelectionItem
    {
        [SerializeField] protected AnimationWindow m_Window;
        [SerializeField] GameObject m_GameObject;
        [SerializeReference] IAnimationWindowController m_Controller;
        [SerializeReference] AnimationWindowClip m_Clip;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "selection ")]
        protected AnimationWindowSelectionItem(AnimationWindow window)
        {
            m_Window = window;
        }

        public virtual void Dispose()
        {
            m_Controller?.Dispose();
        }

        public virtual GameObject gameObject { get { return m_GameObject; } set { m_GameObject = value; } }

        public virtual AnimationClip animationClip => m_Clip?.animationClip;

        public virtual AnimationWindowClip clip { get { return m_Clip; } set { m_Clip = value; } }

        public virtual IAnimationWindowController controller
        {
            get => m_Controller;
            set => m_Controller = value;
        }

        IAnimationWindowClip IAnimationWindowSelectionItem.clip
        {
            get => m_Clip;
            set => m_Clip = value as AnimationWindowClip;
        }

        public virtual IAnimationWindowClip[] GetClips()
        {
            if (rootGameObject == null)
                return Array.Empty<IAnimationWindowClip>();

            var animationClips = AnimationUtility.GetAnimationClips(rootGameObject);
            var animationWindowClips = new IAnimationWindowClip[animationClips.Length];

            for (int i = 0; i < animationClips.Length; ++i)
                animationWindowClips[i] = new AnimationWindowClip(animationClips[i], rootGameObject);

            return animationWindowClips;
        }

        public virtual IAnimationWindowClip CreateNewClip()
        {
            if (rootGameObject == null)
                return null;

            var animationClip = AnimationWindowWizard.CreateNewClip(rootGameObject.name);

            if (animationClip != null)
            {
                AnimationWindowWizard.AddClipToAnimationPlayerComponent(animationPlayer, animationClip);

                if (rootGameObject != null)
                    return new AnimationWindowClip(animationClip, rootGameObject);
                return new AnimationWindowClip(animationClip);
            }

            return null;
        }

        public virtual bool InitializeSelection()
        {
            return AnimationWindowWizard.InitializeGameObjectForAnimation(gameObject);
        }

        public virtual GameObject rootGameObject
        {
            get
            {
                Component animationPlayer = this.animationPlayer;
                if (animationPlayer != null)
                {
                    return animationPlayer.gameObject;
                }
                return null;
            }
        }

        public virtual Component animationPlayer
        {
            get
            {
                if (gameObject != null)
                    return GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                return null;
            }
        }

        // To be editable, a selection must at least contain an animation clip.
        public bool disabled
        {
            get
            {
                // Clip is null or invalid.
                if (clip == null || !clip.isValid)
                    return true;

                // Selection has a game object, but it is a dangling reference.
                if (gameObject == null && !ReferenceEquals(gameObject, null))
                    return true;

                if (animatorIsOptimized)
                    return true;

                return false;
            }
        }

        // Is the hierarchy in animator optimized
        public bool animatorIsOptimized
        {
            get
            {
                Animator animator = animationPlayer as Animator;
                if (animator == null)
                    return false;

                return animator.isOptimizable && !animator.hasTransformHierarchy;
            }
        }

        public bool isReadOnly
        {
            get
            {
                if (!clip?.isValid ?? true)
                    return true;
                // Clip is imported and shouldn't be edited
                if (clip.isReadOnly)
                    return true;

                return false;
            }
        }

        public virtual bool canChangeClip => rootGameObject != null;

        public virtual bool canAddCurves => gameObject != null && !isReadOnly;

        public virtual bool canCreateClips
        {
            get
            {
                Component animationPlayer = this.animationPlayer;
                if (animationPlayer == null)
                    return false;

                Animator animator = animationPlayer as Animator;
                if (animator != null)
                {
                    // Need a valid state machine to create clips in the Animator.
                    return animator.runtimeAnimatorController != null &&
                           (animator.runtimeAnimatorController.hideFlags & HideFlags.NotEditable) == 0;
                }

                return true;
            }
        }

        public virtual bool canSyncSceneSelection { get { return true; } }

        public int GetRefreshHash()
        {
            return new Hash128(
                    (uint)nameof(AnimationWindowSelectionItem).GetHashCode(),
                    (uint)(animationClip != null ? animationClip.GetHashCode() : 0),
                    (uint)(rootGameObject != null ? rootGameObject.GetHashCode() : 0),
                    0u)
                .GetHashCode();
        }

        virtual public void Synchronize()
        {
            // nothing to do.
        }

        public bool Equals(AnimationWindowSelectionItem other)
        {
            return
                animationClip == other.animationClip &&
                gameObject == other.gameObject;
        }

        // What is the first animation player component (Animator or Animation) when recursing parent tree toward root
        public static Component GetClosestAnimationPlayerComponentInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Animator animator))
                {
                    return animator;
                }

                if (tr.TryGetComponent(out Animation animation))
                {
                    return animation;
                }

                if (tr == tr.root)
                    break;

                tr = tr.parent;
            }
            return null;
        }

        public bool IsCompatibleWith(UnityEngine.Object selectedObject)
        {
            if (selectedObject is GameObject selectedGameObject)
            {
                return GetClosestAnimationPlayerComponentInParents(selectedGameObject.transform) == animationPlayer;
            }
            else if (selectedObject is Transform selectedTransform)
            {
                return GetClosestAnimationPlayerComponentInParents(selectedTransform) == animationPlayer;
            }
            else if (animationClip != null && selectedObject is AnimationClip selectedClip)
            {
                return animationClip.GetHashCode() == selectedClip.GetHashCode();
            }

            return false;
        }

        public EditorCurveBinding[] GetAnimatableBindings()
        {
            var root = rootGameObject;
            if (root != null)
            {
                return AnimationWindowUtility.GetAnimatableBindings(root);
            }

            return Array.Empty<EditorCurveBinding>();
        }

        public EditorCurveBinding[] GetAnimatableBindings(GameObject gameObject)
        {
            return AnimationUtility.GetAnimatableBindings(gameObject, rootGameObject);
        }

        public System.Type GetValueType(EditorCurveBinding binding)
        {
            var root = rootGameObject;
            if (root != null)
            {
                return AnimationUtility.GetEditorCurveValueType(root, binding);
            }
            else
            {
                if (binding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return typeof(float);
                }
            }
        }

        public bool isImported => false;
        public bool hasUnsavedChanges => false;

        public void SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void DiscardChanges()
        {
            throw new NotImplementedException();
        }

        public virtual void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            controller.OnPlayModeStateChanged(state);
        }

        // When curve is modified, we never trigger refresh right away. We order a refresh at later time by setting refresh to appropriate value.
        public virtual void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            // AnimationWindow doesn't care if some other clip somewhere changed
            if (clip != animationClip)
                return;

            if (m_Window == null)
            {
                Debug.LogError("Window cannot be null");
                return;
            }

            // Refresh curves that already exist.
            if (type == AnimationUtility.CurveModifiedType.CurveModified)
            {
                m_Window.RefreshCurve(binding);
            }
            else
            {
                // Otherwise do a full reload
                m_Window.RefreshClip();
            }

            // Force repaint to display live animation curve changes from other editor window (like timeline).
            m_Window.Repaint();
        }
    }
}
