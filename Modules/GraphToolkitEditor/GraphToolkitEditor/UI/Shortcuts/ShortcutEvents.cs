// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

// ReSharper disable RedundantArgumentDefaultValue

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An event sent by the Frame All shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutFrameAllEvent : ShortcutEventBase<ShortcutFrameAllEvent>
    {
        public const string id = "Frame All";
        const KeyCode k_KeyCode = KeyCode.A;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Origin shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutFrameOriginEvent : ShortcutEventBase<ShortcutFrameOriginEvent>
    {
        public const string id = "Frame Origin";
        const KeyCode k_KeyCode = KeyCode.O;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Show Item Library shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutShowItemLibraryEvent : ShortcutEventBase<ShortcutShowItemLibraryEvent>
    {
        public const string id = "Show Item Library";
        const KeyCode k_KeyCode = KeyCode.Space;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Toggle Blackboard shortcut
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutToggleBlackboardEvent : ShortcutEventBase<ShortcutToggleBlackboardEvent>
    {
        public const string id = "Toggle Blackboard";
        const KeyCode k_KeyCode = KeyCode.B;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutToggleInspectorEvent : ShortcutEventBase<ShortcutToggleInspectorEvent>
    {
        public const string id = "Toggle Inspector";
        const KeyCode k_KeyCode = KeyCode.I;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutToggleMinimapEvent : ShortcutEventBase<ShortcutToggleMinimapEvent>
    {
        public const string id = "Toggle Minimap";
        const KeyCode k_KeyCode = KeyCode.M;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Convert Variable And Constant shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutConvertConstantAndVariableEvent : ShortcutEventBase<ShortcutConvertConstantAndVariableEvent>
    {
        public const string id = "Convert Variable And Constant";
        const KeyCode k_KeyCode = KeyCode.T;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Convert Wire To Portal shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutConvertWireToPortalEvent : ShortcutEventBase<ShortcutConvertWireToPortalEvent>
    {
        public const string id = "Convert Wire To Portal";
        const KeyCode k_KeyCode = KeyCode.P;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Convert To Local Subgraph shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutCreateLocalSubgraphFromSelectionEvent : ShortcutEventBase<ShortcutCreateLocalSubgraphFromSelectionEvent>
    {
        public const string id = "Create Local Subgraph from Selection";
        const KeyCode k_KeyCode = KeyCode.L;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    /* TODO OYT (GTF-804): For V1, access to the Align Items and Align Hierarchy features was removed as they are confusing to users. To be improved before making them accessible again.
    /// <summary>
    /// An event sent by the Align Nodes shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    public class ShortcutAlignNodesEvent : ShortcutEventBase<ShortcutAlignNodesEvent>
    {
        public const string id = "Align Nodes";
        const KeyCode k_KeyCode = KeyCode.I;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Align Hierarchies shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    public class ShortcutAlignNodeHierarchiesEvent : ShortcutEventBase<ShortcutAlignNodeHierarchiesEvent>
    {
        public const string id = "Align Hierarchies";
        const KeyCode k_KeyCode = KeyCode.I;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Shift;
    }
    */

    /// <summary>
    /// An event sent by the Create Sticky Note.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutCreateStickyNoteEvent : ShortcutEventBase<ShortcutCreateStickyNoteEvent>
    {
        public const string id = "Create Sticky Note";
        const KeyCode k_KeyCode = KeyCode.BackQuote;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Alt;
    }

    /// <summary>
    /// An event sent by the Create Sticky Note.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutCreatePlacematEvent : ShortcutEventBase<ShortcutCreatePlacematEvent>
    {
        public const string id = "Create Placemat";
        const KeyCode k_KeyCode = KeyCode.G;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action;
    }

    /// <summary>
    /// An event sent by the Delete and Reconnect shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    [UnityRestricted]
    internal class ShortcutDeleteAndReconnectEvent : ShortcutEventBase<ShortcutDeleteAndReconnectEvent>
    {
        public const string id = "Delete and Reconnect";
        const KeyCode keyCode = KeyCode.Delete;
        const ShortcutModifiers modifiers = ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Paste Without Wires shortcut.
    /// </summary>
    /// <remarks>The same shortcut is used for "Paste as New"</remarks>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortCutPasteWithoutWires : ShortcutEventBase<ShortCutPasteWithoutWires>
    {
        // TODO: Needs to be renamed when we have a proper implementation for the Paste/Duplicate with Wires.
        public const string id = "Paste without Wires";
        const KeyCode k_KeyCode = KeyCode.V;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Shift | ShortcutModifiers.Action;
    }

    /// <summary>
    /// An event sent by the Duplicate Without Wires shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortCutDuplicateWithoutWires : ShortcutEventBase<ShortCutDuplicateWithoutWires>
    {
        public const string id = "Duplicate without Wires";
        // TODO: Needs to be renamed when we have a proper implementation for the Paste/Duplicate with Wires.
        const KeyCode k_KeyCode = KeyCode.D;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Shift | ShortcutModifiers.Action;
    }

    /// <summary>
    /// An event sent by the Disconnect Wires shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutDisconnectWiresEvent : ShortcutEventBase<ShortcutDisconnectWiresEvent>
    {
        public const string id = "Disconnect Wires";
        const KeyCode k_KeyCode = KeyCode.W;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Toggle Node Collapse shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutToggleNodeCollapseEvent : ShortcutEventBase<ShortcutToggleNodeCollapseEvent>
    {
        public const string id = "Toggle Node Collapse";
        const KeyCode k_KeyCode = KeyCode.O;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Expand Subgraph shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, k_KeyCode, k_Modifiers)]
    [UnityRestricted]
    internal class ShortcutExtractContentsToPlacematEvent : ShortcutEventBase<ShortcutExtractContentsToPlacematEvent>
    {
        public const string id = "Extract Contents to Placemat";
        const KeyCode k_KeyCode = KeyCode.U;
        const ShortcutModifiers k_Modifiers = ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }
}
