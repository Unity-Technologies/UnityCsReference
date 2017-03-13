// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal class GameObjectSelectionItem : AnimationWindowSelectionItem
    {
        public static GameObjectSelectionItem Create(GameObject gameObject)
        {
            GameObjectSelectionItem selectionItem = CreateInstance(typeof(GameObjectSelectionItem)) as GameObjectSelectionItem;
            selectionItem.hideFlags = HideFlags.HideAndDontSave;

            selectionItem.gameObject = gameObject;
            selectionItem.animationClip = null;
            selectionItem.timeOffset = 0.0f;
            selectionItem.id = 0; // no need for id since there's only one item in selection.

            if (selectionItem.rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(selectionItem.rootGameObject);

                if (selectionItem.animationClip == null && selectionItem.gameObject != null) // there is activeGO but clip is still null
                    selectionItem.animationClip = allClips.Length > 0 ? allClips[0] : null;
                else if (!Array.Exists(allClips, x => x == selectionItem.animationClip))  // clip doesn't belong to the currently active GO
                    selectionItem.animationClip = allClips.Length > 0 ? allClips[0] : null;
            }

            return selectionItem;
        }

        public override AnimationClip animationClip
        {
            set
            {
                base.animationClip = value;
            }
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
                        animationClip = allClips[0];
                    }
                }
                else
                {
                    animationClip = null;
                }
            }
        }
    }
}
