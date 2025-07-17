// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace Unity.Motion.Editor.AnimationWindow
{
    static class AnimationUtilityExt
    {
        // We should make this public.
        public static void UpdateTangentsFromMode(AnimationCurve curve) =>
            AnimationUtility.UpdateTangentsFromMode(curve);
    }
}
