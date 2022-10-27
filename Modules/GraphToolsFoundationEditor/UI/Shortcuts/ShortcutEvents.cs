// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

// ReSharper disable RedundantArgumentDefaultValue

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// An event sent by the Frame All shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutFrameAllEvent : ShortcutEventBase<ShortcutFrameAllEvent>
    {
        public const string id = "Frame All";
        const KeyCode k_KeyCode = KeyCode.A;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Origin shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutFrameOriginEvent : ShortcutEventBase<ShortcutFrameOriginEvent>
    {
        public const string id = "Frame Origin";
        const KeyCode k_KeyCode = KeyCode.O;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Previous shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutFramePreviousEvent : ShortcutEventBase<ShortcutFramePreviousEvent>
    {
        public const string id = "Frame Previous";
        const KeyCode k_KeyCode = KeyCode.LeftBracket;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Next shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutFrameNextEvent : ShortcutEventBase<ShortcutFrameNextEvent>
    {
        public const string id = "Frame Next";
        const KeyCode k_KeyCode = KeyCode.RightBracket;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Delete shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutDeleteEvent : ShortcutEventBase<ShortcutDeleteEvent>
    {
        public const string id = "Delete";
        const KeyCode k_KeyCode = KeyCode.Backspace;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Show Item Library shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutShowItemLibraryEvent : ShortcutEventBase<ShortcutShowItemLibraryEvent>
    {
        public const string id = "Show Item Library";
        const KeyCode k_KeyCode = KeyCode.Space;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Convert Variable And Constant shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutConvertConstantAndVariableEvent : ShortcutEventBase<ShortcutConvertConstantAndVariableEvent>
    {
        public const string id = "Convert Variable And Constant";
        const KeyCode k_KeyCode = KeyCode.C;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /* TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
    /// <summary>
    /// An event sent by the Align Nodes shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutAlignNodesEvent : ShortcutEventBase<ShortcutAlignNodesEvent>
    {
        public const string id = "Align Nodes";
        const KeyCode k_KeyCode = KeyCode.I;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Align Hierarchies shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    class ShortcutAlignNodeHierarchiesEvent : ShortcutEventBase<ShortcutAlignNodeHierarchiesEvent>
    {
        public const string id = "Align Hierarchies";
        const KeyCode k_KeyCode = KeyCode.I;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Shift;
    }
    */

    /// <summary>
    /// An event sent by the Create Sticky Note.
    /// </summary>
    [ToolShortcutEvent(null, id)]
    class ShortcutCreateStickyNoteEvent : ShortcutEventBase<ShortcutCreateStickyNoteEvent>
    {
        public const string id = "Create Sticky Note";
    }
}
