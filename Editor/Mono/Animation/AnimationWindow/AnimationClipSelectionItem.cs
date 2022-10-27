// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [Serializable]
    internal class AnimationClipSelectionItem : AnimationWindowSelectionItem
    {
        public static AnimationClipSelectionItem Create(AnimationClip animationClip, Object sourceObject = null)
        {
            var selectionItem = new AnimationClipSelectionItem
            {
                gameObject = sourceObject as GameObject,
                scriptableObject = sourceObject as ScriptableObject,
                animationClip = animationClip,
                id = 0
            };

            return selectionItem;
        }

        public override bool canPreview { get { return false; } }

        public override bool canRecord { get { return false; } }

        public override bool canChangeAnimationClip { get { return false; } }

        public override bool canSyncSceneSelection { get { return false; } }
    }
}
