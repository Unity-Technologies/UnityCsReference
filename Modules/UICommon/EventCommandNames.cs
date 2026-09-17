// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    /// <summary>
    /// Names of the commands the Editor sends to its windows and controls, such as copy, paste, or delete.
    /// </summary>
    /// <remarks>
    /// The Editor sends a command in two phases: a validation phase that asks whether anything handles the
    /// command, followed by an execution phase that performs it. Compare the command name against the
    /// constants in this class to handle these commands in custom Editor UI. In UI Toolkit, the phases are
    /// delivered as ValidateCommandEvent and ExecuteCommandEvent and the name is available through their
    /// commandName property. In IMGUI, they are delivered as events of type <see cref="EventType.ValidateCommand"/>
    /// and <see cref="EventType.ExecuteCommand"/> and the name is available through <see cref="Event.commandName"/>.
    ///
    /// To indicate that a command is handled, call the event's StopPropagation method in UI Toolkit, or
    /// <see cref="Event.Use"/> in IMGUI, during the validation phase.
    /// </remarks>
    public static class EventCommandNames
    {
        // Some of these strings are also hardcoded on the native side. Change them at your own risk!
        // For end users, please document relevant commands in UIE-Command-Events.md.

        /// <summary>
        /// The command name for the Cut command, which copies the current selection to the clipboard and removes it.
        /// </summary>
        public const string Cut = "Cut";

        /// <summary>
        /// The command name for the Copy command, which copies the current selection to the clipboard.
        /// </summary>
        public const string Copy = "Copy";

        /// <summary>
        /// The command name for the Paste command, which inserts the clipboard content into the current context.
        /// </summary>
        public const string Paste = "Paste";

        /// <summary>
        /// The command name for the Select All command, which selects every item in the current context.
        /// </summary>
        public const string SelectAll = "SelectAll";

        /// <summary>
        /// The command name for the Deselect All command, which clears the current selection.
        /// </summary>
        public const string DeselectAll = "DeselectAll";

        /// <summary>
        /// The command name for the Invert Selection command, which selects the items that are not part of the
        /// current selection and deselects the items that are.
        /// </summary>
        public const string InvertSelection = "InvertSelection";

        /// <summary>
        /// The command name for the Duplicate command, which creates a copy of the current selection.
        /// </summary>
        public const string Duplicate = "Duplicate";

        /// <summary>
        /// The command name for the Rename command, which starts renaming the current selection.
        /// </summary>
        public const string Rename = "Rename";

        /// <summary>
        /// The command name for the Delete command, which removes the current selection.
        /// </summary>
        public const string Delete = "Delete";

        /// <summary>
        /// The command name for the Soft Delete command, which removes the current selection. The Editor sends
        /// this command instead of <see cref="Delete"/> when the Delete key is pressed without modifiers, so a
        /// handler can implement a less destructive removal, such as keeping references intact.
        /// </summary>
        public const string SoftDelete = "SoftDelete";

        /// <summary>
        /// The command name for the Find command, which moves focus to the search field of the current window.
        /// </summary>
        public const string Find = "Find";

        /// <summary>
        /// The command name for the Select Children command, which extends the selection to the children of the
        /// currently selected items.
        /// </summary>
        public const string SelectChildren = "SelectChildren";

        /// <summary>
        /// The command name for the Select Prefab Root command, which moves the selection to the root of the
        /// Prefab that contains the currently selected GameObject.
        /// </summary>
        public const string SelectPrefabRoot = "SelectPrefabRoot";

        /// <summary>
        /// The command name for the notification the Editor sends to its windows after an undo or redo operation
        /// completes, so they can refresh the state they display.
        /// </summary>
        public const string UndoRedoPerformed = "UndoRedoPerformed";

        /// <summary>
        /// The command name for the Frame Selected command, which centers the current view on the selection.
        /// </summary>
        public const string FrameSelected = "FrameSelected";

        /// <summary>
        /// The command name for the Frame Selected With Lock command, which centers the current view on the
        /// selection and keeps following it when the selection moves.
        /// </summary>
        public const string FrameSelectedWithLock = "FrameSelectedWithLock";

        // The names below are point-to-point messages between Editor systems, not commands aimed at the focused UI.

        internal const string OnLostFocus = "OnLostFocus";

        // Used by IMGUIContainer to force editing textfield when focus is changed with tab
        internal const string NewKeyboardFocus = "NewKeyboardFocus";
        internal const string ModifierKeysChanged = "ModifierKeysChanged";

        // Used by ColorPicker
        internal const string EyeDropperUpdate = "EyeDropperUpdate";
        internal const string EyeDropperClicked = "EyeDropperClicked";
        internal const string EyeDropperCancelled = "EyeDropperCancelled";
        internal const string ColorPickerChanged = "ColorPickerChanged";
    }
}
