// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.InternalBridge
{
    static class VisualElementBridge
    {
        public static bool IsOSXContextualMenuPlatform => UIElementsUtility.isOSXContextualMenuPlatform;
        public static Matrix4x4 GetWorldTransformInverse(this VisualElement ve)
        {
            return ve.worldTransformInverse;
        }

        public static bool HasFocus<TValue>(this TextInputBaseField<TValue> ve)
        {
            return ve.hasFocus;
        }

        public static Color GetPlayModeTintColor(this VisualElement ve)
        {
            return ve.playModeTintColor;
        }
    }
}
