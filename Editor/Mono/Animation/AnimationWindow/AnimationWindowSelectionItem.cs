// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [Serializable]
    abstract class AnimationWindowSelectionItem : System.IEquatable<AnimationWindowSelectionItem>, ISelectionBinding
    {
        [SerializeField] private int m_Id;
        [SerializeField] private GameObject m_GameObject;
        [SerializeField] private ScriptableObject m_ScriptableObject;
        [SerializeField] private AnimationClip m_AnimationClip;

        public virtual int id { get { return m_Id; } set { m_Id = value; } }

        public virtual GameObject gameObject { get { return m_GameObject; } set { m_GameObject = value; } }

        public virtual ScriptableObject scriptableObject { get { return m_ScriptableObject; } set { m_ScriptableObject = value; } }

        public virtual Object sourceObject { get { return (gameObject != null) ? (Object)gameObject : (Object)scriptableObject; } }

        public virtual AnimationClip animationClip { get { return m_AnimationClip; } set { m_AnimationClip = value; } }

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
                    return AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                return null;
            }
        }

        public bool disabled
        {
            get
            {
                // To be editable, a selection must at least contain an animation clip.
                return (animationClip == null);
            }
        }

        public virtual bool animationIsEditable
        {
            get
            {
                // Clip is imported and shouldn't be edited
                if (animationClip && (animationClip.hideFlags & HideFlags.NotEditable) != 0)
                    return false;

                // Object is a prefab - shouldn't be edited
                if (objectIsPrefab)
                    return false;

                return true;
            }
        }

        public virtual bool clipIsEditable
        {
            get
            {
                if (!animationClip)
                    return false;
                // Clip is imported and shouldn't be edited
                if ((animationClip.hideFlags & HideFlags.NotEditable) != 0)
                    return false;
                if (!AssetDatabase.IsOpenForEdit(animationClip, StatusQueryOptions.UseCachedIfPossible))
                    return false;

                return true;
            }
        }

        public virtual bool objectIsPrefab
        {
            get
            {
                // No gameObject selected
                if (!gameObject)
                    return false;

                if (EditorUtility.IsPersistent(gameObject))
                    return true;

                if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    return true;

                return false;
            }
        }

        public virtual bool objectIsOptimized
        {
            get
            {
                Animator animator = animationPlayer as Animator;
                if (animator == null)
                    return false;

                return animator.isOptimizable && !animator.hasTransformHierarchy;
            }
        }

        public virtual bool canPreview
        {
            get
            {
                if (rootGameObject != null)
                {
                    return !objectIsOptimized;
                }

                return false;
            }
        }

        public virtual bool canRecord
        {
            get
            {
                if (!animationIsEditable)
                    return false;

                return canPreview;
            }
        }

        public virtual bool canChangeAnimationClip
        {
            get
            {
                return rootGameObject != null;
            }
        }

        public virtual bool canAddCurves
        {
            get
            {
                if (gameObject != null)
                {
                    return !objectIsPrefab && clipIsEditable;
                }
                else if (scriptableObject != null)
                {
                    return true;
                }

                return false;
            }
        }

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
                    return (animator.runtimeAnimatorController != null);
                }

                return true;
            }
        }

        public virtual bool canSyncSceneSelection { get { return true; } }

        public int GetRefreshHash()
        {
            return unchecked (id * 19603 ^
                (animationClip != null ? 729 * animationClip.GetHashCode() : 0) ^
                (rootGameObject != null ? 27 * rootGameObject.GetHashCode() : 0) ^
                (scriptableObject != null ? scriptableObject.GetHashCode() : 0));
        }

        virtual public void Synchronize()
        {
            // nothing to do.
        }

        public bool Equals(AnimationWindowSelectionItem other)
        {
            return id == other.id &&
                animationClip == other.animationClip &&
                gameObject == other.gameObject &&
                scriptableObject == other.scriptableObject;
        }
    }
}
