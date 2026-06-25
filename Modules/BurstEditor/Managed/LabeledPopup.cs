// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace Unity.Burst.Editor
{
    internal static class LabeledPopup
    {
        // Because the function given to dropdown menu needs takes its parameter
        // in the form of an object, we need someway to wrap the integer into one.
        private struct IntegerWrapper
        {
            public int Value { get; }

            public IntegerWrapper(int v)
            {
                Value = v;
            }
        }

        /// <summary>
        /// Enables having several popup menus functioning independently at the same time.
        /// </summary>
        private class PopperCallBack
        {
            public static PopperCallBack Instance = null;

            /// <summary>
            /// Name of the event send when an index have been chosen.
            /// </summary>
            private const string IndexChangeEventName = "PopperChangingIndex";

            private readonly int _controlID;
            private int _selectedIdx;
            private readonly GUIView _view;

            public PopperCallBack(int controlID)
            {
                _controlID = controlID;
                _selectedIdx = -1;
                _view = GUIView.current;
            }

            /// <summary>
            /// Tries to get selection chosen by dropdown menu <see cref="controlID"/>.
            /// </summary>
            /// <param name="controlId">ID of popup menu.</param>
            /// <param name="selectedIdx">Current selected index.</param>
            /// <returns>
            /// Either the selected target, or the <see cref="selectedIdx"/>
            /// if none were chosen yet.
            /// </returns>
            internal static int GetSelectionValue(int controlId, int selectedIdx)
            {
                var selected = selectedIdx;

                // A command event with message IndexChangeEvent will be sent whenever a choice on
                // the dropdown menu has been made. So if this is not the case return whatever index was given
                var evt = Event.current;
                if (evt.type != EventType.ExecuteCommand || evt.commandName != IndexChangeEventName) return selected;

                // If this is the popup opened right now: Set the selection idx appropriately
                if (Instance != null && controlId == Instance._controlID)
                {
                    selected = Instance._selectedIdx;
                    Instance = null;
                }

                return selected;
            }

            /// <summary>
            /// Sets selection on the opened dropdown, and sends an event
            /// to the view the popup is within.
            /// </summary>
            /// <param name="index">Index selected.</param>
            internal void SetSelection(object index)
            {
                _selectedIdx = ((IntegerWrapper)index).Value;
                _view.SendEvent(EditorGUIUtility.CommandEvent(IndexChangeEventName));
            }
        }

        /// <summary>
        /// Name used for getting a controlID for popup menus.
        /// </summary>
        private const string LabelControlName = "LabeledPopup";

        /// <summary>
        /// Create a immediate automatically positioned popup menu.
        /// </summary>
        /// <param name="index">Current active selection index.</param>
        /// <param name="display">Name to display as the button.</param>
        /// <param name="options">Display name for the dropdown menu.</param>
        /// <returns>The possibly new active selection index.</returns>
        public static int Popup(int index, GUIContent display, string[] options)
        {
            // GetControlRect so space is reserved for the button, and we get a
            // position to place the drop down context menu at.
            var pos = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight,
                EditorStyles.popup);

            var controlID = GUIUtility.GetControlID(LabelControlName.GetHashCode(), FocusType.Keyboard, pos);

            var selected = PopperCallBack.GetSelectionValue(controlID, index);

            if (GUI.Button(pos, display, EditorStyles.popup))
            {
                PopperCallBack.Instance = new PopperCallBack(controlID);

                var menu = new GenericMenu();
                for (var i = 0; i < options.Length; i++)
                {
                    var size = options[i];

                    menu.AddItem(EditorGUIUtility.TrTextContent(size), i == index, PopperCallBack.Instance.SetSelection, new IntegerWrapper(i));
                }
                menu.Popup(pos, index);
            }

            return selected;
        }
    }
}
