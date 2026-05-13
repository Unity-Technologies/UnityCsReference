// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;

namespace UnityEditorInternal
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IAnimationWindowPropertyHandler
    {
        // Returns the channel index [0..N] for ordering within a group, or -1 if not handled.
        int GetChannelIndex(string propertyName);

        // Returns the group name (prefix before the channel suffix), or null if not handled.
        string GetPropertyGroupName(string propertyName);

        // --- Value field rendering (called inside BeginChangeCheck/EndChangeCheck) ---
        // Renders a custom value control for the given curve.
        // animatableObjectType is the node's component type; handlers should
        // return false immediately for types they do not own.
        // Returns true if the field was handled (default rendering is skipped).
        // newValue must be set to the (possibly unchanged) current value when returning true.
        // handlerData is opaque per-node storage that the handler may read/write freely.
        bool TryDoValueField(Rect valueFieldRect, Rect valueFieldDragRect, int controlId,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object currentValue, out object newValue, ref object handlerData);

        // --- Context menu population ---
        // Appends handler-specific items to the hierarchy row context menu.
        // handlerData is the current per-node state; setHandlerData persists
        // a new value back to the node (safe to capture in deferred menu callbacks).
        // Returns true if any items were added.
        bool TryPopulateContextMenu(GenericMenu menu,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object handlerData, Action<object> setHandlerData);

        // --- Drag-and-drop validation ---
        // Validates whether dragged objects are acceptable for the given PPtr curve.
        // Returns true if the handler claims this curve and accepts the drop.
        // validatedReferences receives the (possibly transformed) objects to use as keyframe values.
        bool TryValidateDragDrop(EditorCurveBinding curveBinding, Type curveValueType,
            Type animatableObjectType, UnityEngine.Object[] draggedObjects,
            out UnityEngine.Object[] validatedReferences);
    }
}
