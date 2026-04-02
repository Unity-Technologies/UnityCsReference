// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    static class AnimationWindowCallbacks
    {
        [AutoStaticsCleanupOnCodeReload]
        public static Type AnimationWindowType = null;

        public delegate void StopAnimationPlaybackAndPreviewingCallback();
        public static StopAnimationPlaybackAndPreviewingCallback StopAnimationPlaybackAndPreviewing;

        public static EditorWindow[] GetAllAnimationWindows()
        {
            if (AnimationWindowCallbacks.AnimationWindowType == null ||
                !AnimationWindowCallbacks.AnimationWindowType.IsSubclassOf(typeof(EditorWindow)))
                return Array.Empty<EditorWindow>();

            var windows = Resources.FindObjectsOfTypeAll(AnimationWindowCallbacks.AnimationWindowType);
            return Resources.ConvertObjects<EditorWindow>(windows);
        }
    }
}
