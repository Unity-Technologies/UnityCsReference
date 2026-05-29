// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [NativeHeader("Modules/UIElementsEditor/UIAnimationBinder.bindings.h")]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class UIAnimationBinderEditorBindings
    {
        [FreeFunction("UIAnimationBinderEditorBindings::GetAllAnimatableProperties")]
        public static extern EditorCurveBinding[] GetAllAnimatableProperties(UIAnimationBinder binder, Type discriminatorType);

        [FreeFunction("UIAnimationBinderEditorBindings::ReadCurrentBoundValue")]
        public static extern bool ReadCurrentBoundValue(UIAnimationBinder binder, string attribute,
            out int kind, out float floatValue, out int intValue, out EntityId entityId);

        [FreeFunction("UIAnimationBinderEditorBindings::TryResolveAttribute")]
        public static extern bool TryResolveAttribute(UIAnimationBinder binder, string attribute,
            out int elementIndex, out int propertyId, out int channel, out int kind);

        [FreeFunction("UIAnimationBinderEditorBindings::TryGetChannelKindForAttribute")]
        public static extern bool TryGetChannelKindForAttribute(string attribute, out int kind);
    }
}
