// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    public interface ICanvasRaycastFilter
    {
        bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera);
    }

    [NativeClass("UI::CanvasGroup"),
     NativeHeader("Runtime/UI/CanvasGroup.h")]
    public sealed class CanvasGroup : Behaviour, ICanvasRaycastFilter
    {
        [NativeProperty("Alpha", false, TargetType.Function)] public extern float alpha { get; set; }
        [NativeProperty("Interactable", false, TargetType.Function)] public extern bool interactable { get; set; }
        [NativeProperty("BlocksRaycasts", false, TargetType.Function)] public extern bool blocksRaycasts { get; set; }
        [NativeProperty("IgnoreParentGroups", false, TargetType.Function)] public extern bool ignoreParentGroups { get; set; }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return blocksRaycasts;
        }
    }
}
