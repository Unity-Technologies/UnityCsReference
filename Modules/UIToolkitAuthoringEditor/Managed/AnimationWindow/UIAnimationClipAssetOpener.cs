// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor
{
    static class UIAnimationClipAssetOpener
    {
        [OnOpenAsset]
        static bool OnOpenAsset(EntityId entityId, int line)
            => TryOpen(EditorUtility.EntityIdToObject(entityId));

        // Double-clicking a UIAnimationClip asset opens/focuses the Animation Window (parity with
        // AnimationClip). The asset also becomes Selection.activeObject, so
        // SceneVisualElementAnimationResponder populates the window's selection; this handler only
        // surfaces the window.
        internal static bool TryOpen(Object asset)
        {
            if (asset is not UIAnimationClip)
                return false;

            EditorWindow.GetWindow<AnimationWindow>();
            return true;
        }
    }
}
