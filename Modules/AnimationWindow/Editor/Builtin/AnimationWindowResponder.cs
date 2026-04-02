// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.AnimationWindowBuiltin
{
    class AnimationWindowResponder : IAnimationWindowResponder
    {
        public bool OnSelectionChange(AnimationWindow window, UnityObject selectedObject, out IAnimationWindowSelectionItem newSelection)
        {
            if (selectedObject is GameObject activeGameObject)
            {
                return EditGameObject(window, activeGameObject, out newSelection);
            }
            else
            {
                if (selectedObject is Transform activeTransform)
                {
                    return EditGameObject(window, activeTransform.gameObject, out newSelection);
                }
                else if (selectedObject is AnimationClip activeAnimationClip)
                {
                    return EditAnimationClip(window, activeAnimationClip, out newSelection);
                }
            }

            newSelection = null;
            return false;
        }

        bool EditGameObject(AnimationWindow window, GameObject gameObject, out IAnimationWindowSelectionItem newSelection)
        {
            newSelection = null;

            if (EditorUtility.IsPersistent(gameObject))
                return false;

            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            // No Animator/Animation component.
            var animationPlayer = AnimationWindowSelectionItem.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
            if (animationPlayer == null)
                return false;

            if (ShouldUpdateGameObjectSelection(window.selection, animationPlayer))
            {
                newSelection = GameObjectSelectionItem.Create(window, gameObject);
                return true;
            }

            newSelection = window.selection;
            return true;
        }

        bool EditAnimationClip(AnimationWindow window, AnimationClip animationClip, out IAnimationWindowSelectionItem newSelection)
        {
            if (!window.selection.IsCompatibleWith(animationClip))
            {
                newSelection = AnimationClipSelectionItem.Create(window, animationClip);
                return true;
            }

            newSelection = window.selection;
            return true;
        }

        bool ShouldUpdateGameObjectSelection(IAnimationWindowSelectionItem selection, Component animationPlayer)
        {
            if (selection is not GameObjectSelectionItem)
                return true;

            // Animation player has changed. Update selection.
            if (animationPlayer != selection.animationPlayer)
                return true;

            // No clip in current selection, favour new selection.
            if (selection.clip == null)
                return true;

            // Make sure that animation clip is still referenced in animation player.
            if (selection.rootGameObject != null)
            {
                var allClips = selection.GetClips();
                if (!Array.Exists(allClips, x => x?.Equals(selection.clip) ?? false))
                    return true;
            }

            return false;
        }
    }
}
