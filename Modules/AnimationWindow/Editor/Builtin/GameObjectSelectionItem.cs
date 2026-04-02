// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor.AnimationWindowBuiltin
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class GameObjectSelectionItem : AnimationWindowSelectionItem
    {
        protected GameObjectSelectionItem(AnimationWindow window) : base(window)
        {
        }

        public static GameObjectSelectionItem Create(AnimationWindow window, GameObject gameObject)
        {
            return Create(window, window.state, gameObject);
        }

        public static GameObjectSelectionItem Create(AnimationWindowState state, GameObject gameObject)
        {
            return Create(null, state, gameObject);
        }

        static GameObjectSelectionItem Create(AnimationWindow window, AnimationWindowState state, GameObject gameObject)
        {
            var selectionItem = new GameObjectSelectionItem(window);

            selectionItem.gameObject = gameObject;
            AnimationClip animationClip = null;

            if (selectionItem.rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(selectionItem.rootGameObject);

                if (selectionItem.gameObject != null) // there is activeGO but clip is still null
                    animationClip = allClips.Length > 0 ? allClips[0] : null;
            }

            selectionItem.clip = animationClip != null
                ? new AnimationWindowClip(animationClip, selectionItem.gameObject)
                : null;

            var controller = new AnimationWindowControl();
            controller.state = state;

            selectionItem.controller = controller;

            return selectionItem;
        }

        public override AnimationClip animationClip
        {
            get
            {
                if (animationPlayer == null)
                    return null;

                return base.animationClip;
            }
        }

        public override void Synchronize()
        {
            if (rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(rootGameObject);
                if (allClips.Length > 0)
                {
                    if (!Array.Exists(allClips, x => x == animationClip))
                    {
                        clip = new AnimationWindowClip(allClips[0], gameObject);
                    }
                }
                else
                {
                    clip = null;
                }
            }
        }

        public override void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            // AnimationWindow doesn't care if some other clip somewhere changed
            if (clip != animationClip)
                return;

            if (controller is not AnimationWindowControl animationWindowControl)
            {
                Debug.LogError("controller needs to be of type AnimationWindowControl");
                return;
            }

            // Refresh curves that already exist.
            if (type == AnimationUtility.CurveModifiedType.CurveModified)
            {
                animationWindowControl.state.RefreshCurve(binding);
            }
            else
            {
                // Otherwise do a full reload
                animationWindowControl.state.refresh = AnimationWindowState.RefreshType.Everything;
            }

            // Force repaint to display live animation curve changes from other editor window (like timeline).
            m_Window?.Repaint();
        }
    }
}
