// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.AnimationWindowBuiltin
{
    [Serializable]
    internal class AnimationClipSelectionItem : AnimationWindowSelectionItem
    {
        AnimationClipSelectionItem(AnimationWindow window) : base(window)
        {
        }

        public static AnimationClipSelectionItem Create(AnimationClip animationClip, Object sourceObject = null) =>
            Create(null, animationClip, sourceObject);

        public static AnimationClipSelectionItem Create(AnimationWindow window, AnimationClip animationClip, Object sourceObject = null)
        {
            var selectionItem = new AnimationClipSelectionItem(window);
            selectionItem.gameObject = sourceObject as GameObject;

            if (animationClip == null)
                selectionItem.clip = null;
            else if (selectionItem.gameObject != null)
                selectionItem.clip = new AnimationWindowClip(animationClip, selectionItem.gameObject);
            else
                selectionItem.clip = new AnimationWindowClip(animationClip);

            selectionItem.controller = new DefaultAnimationWindowController
            {
                frameRate = animationClip != null ? animationClip.frameRate : 60.0f
            };

            return selectionItem;
        }

        public override bool canChangeClip { get { return false; } }

        public override bool canSyncSceneSelection { get { return false; } }
    }
}
