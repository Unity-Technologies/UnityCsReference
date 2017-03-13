// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AnimationClipSelectionItem : AnimationWindowSelectionItem
    {
        public static AnimationClipSelectionItem Create(AnimationClip animationClip, Object sourceObject)
        {
            AnimationClipSelectionItem selectionItem = CreateInstance(typeof(AnimationClipSelectionItem)) as AnimationClipSelectionItem;
            selectionItem.hideFlags = HideFlags.HideAndDontSave;

            selectionItem.gameObject = sourceObject as GameObject;
            selectionItem.scriptableObject = sourceObject as ScriptableObject;
            selectionItem.animationClip = animationClip;
            selectionItem.timeOffset = 0.0f;
            selectionItem.id = 0; // no need for id since there's only one item in selection.

            return selectionItem;
        }

        public override bool canPreview { get { return false; } }

        public override bool canRecord { get { return false; } }

        public override bool canChangeAnimationClip { get { return false; } }

        public override bool canSyncSceneSelection { get { return false; } }
    }
}
