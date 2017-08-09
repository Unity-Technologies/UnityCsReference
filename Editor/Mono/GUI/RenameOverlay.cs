// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace UnityEditor
{
    // Usage:
    // BeginRename (string name, int userData, float delay) - Starts a rename
    // EndRename (bool acceptChanges) - Ends rename and caches state (use property interface for client reaction)
    // IsRenaming() - Is renaming active (we might still not show anything due to 'delay' parameter in BeginRename)
    // Clear() - Clears rename state
    // IMPORTANT:
    // OnEvent () - Should be called early in an OnGUI of an EditorWindow that is using this RenameOverlay (handles closing if clicking outside and closing while receving any input while delayed, and caches controlID for text field)
    // OnGUI (GUIStyle textFieldStyle) - Should be called late to ensure rendered on top and early for non-repaint event to handle input before other ui logic

    [System.Serializable]
    internal class RenameOverlay
    {
        [SerializeField]
        bool m_UserAcceptedRename;
        [SerializeField]
        string m_Name;
        [SerializeField]
        string m_OriginalName;
        [SerializeField]
        Rect m_EditFieldRect;
        [SerializeField]
        int m_UserData;
        [SerializeField]
        bool m_IsWaitingForDelay;
        [SerializeField]
        bool m_IsRenaming = false;
        [SerializeField]
        EventType m_OriginalEventType = EventType.Ignore;
        [SerializeField]
        bool m_IsRenamingFilename = false;
        [SerializeField]
        GUIView m_ClientGUIView;

        [System.NonSerialized]
        Rect m_LastScreenPosition;
        [System.NonSerialized]
        bool m_UndoRedoWasPerformed;

        [System.NonSerialized]
        DelayedCallback m_DelayedCallback;

        string k_RenameOverlayFocusName = "RenameOverlayField";

        // property interface
        public string name { get {return m_Name; }}
        public string originalName { get {return m_OriginalName; }}
        public bool userAcceptedRename { get {return m_UserAcceptedRename; }}
        public int userData { get {return m_UserData; }}
        public bool isWaitingForDelay { get {return m_IsWaitingForDelay; }}
        public Rect editFieldRect { get {return m_EditFieldRect; } set {m_EditFieldRect = value; }}
        public bool isRenamingFilename {get {return m_IsRenamingFilename; } set {m_IsRenamingFilename = value; }}

        private static GUIStyle s_DefaultTextFieldStyle = null;
        private static int s_TextFieldHash = "RenameFieldTextField".GetHashCode();
        private int m_TextFieldControlID;

        // Returns true if started renaming
        public bool BeginRename(string name, int userData, float delay)
        {
            if (m_IsRenaming)
            {
                return false;
            }

            m_Name = name;
            m_OriginalName = name;
            m_UserData = userData;
            m_UserAcceptedRename = false;
            m_IsWaitingForDelay = delay > 0f;
            m_IsRenaming = true;
            m_EditFieldRect = new Rect(0, 0, 0, 0);
            m_ClientGUIView = GUIView.current;

            if (delay > 0f)
                m_DelayedCallback = new DelayedCallback(BeginRenameInternalCallback, delay);
            else
                BeginRenameInternalCallback();
            return true;
        }

        void BeginRenameInternalCallback()
        {
            EditorGUI.s_RecycledEditor.text = m_Name;
            EditorGUI.s_RecycledEditor.SelectAll();
            RepaintClientView();

            m_IsWaitingForDelay = false;

            Undo.undoRedoPerformed -= UndoRedoWasPerformed;
            Undo.undoRedoPerformed += UndoRedoWasPerformed;
        }

        public void EndRename(bool acceptChanges)
        {
            EditorGUIUtility.editingTextField = false;
            if (!m_IsRenaming)
                return;

            Undo.undoRedoPerformed -= UndoRedoWasPerformed;
            if (m_DelayedCallback != null)
                m_DelayedCallback.Clear();

            RemoveMessage();

            if (isRenamingFilename)
                m_Name = InternalEditorUtility.RemoveInvalidCharsFromFileName(m_Name, true);

            m_IsRenaming = false;
            m_IsWaitingForDelay = false;
            m_UserAcceptedRename = acceptChanges;

            // For issuing event for client to react on end of rename
            RepaintClientView();
        }

        private void RepaintClientView()
        {
            if (m_ClientGUIView != null)
                m_ClientGUIView.Repaint();
        }

        public void Clear()
        {
            m_IsRenaming = false;
            m_UserAcceptedRename = false;
            m_Name = "";
            m_OriginalName = "";
            m_EditFieldRect = new Rect();
            m_UserData = 0;
            m_IsWaitingForDelay = false;
            m_OriginalEventType = EventType.Ignore;
            Undo.undoRedoPerformed -= UndoRedoWasPerformed;

            // m_IsRenamingFilename = false; // Only clear temp data used for renaming not state that we want to persist
        }

        void UndoRedoWasPerformed()
        {
            // If undo/redo was performed then close the rename overlay as it does not support undo/redo
            // We need to delay the EndRename until next OnGUI as clients poll the state of the rename overlay state there
            m_UndoRedoWasPerformed = true;
        }

        public bool HasKeyboardFocus()
        {
            return (GUI.GetNameOfFocusedControl() == k_RenameOverlayFocusName);
        }

        public bool IsRenaming()
        {
            return m_IsRenaming;
        }

        // Should be called as early as possible in an EditorWindow using this RenameOverlay
        // Returns: false if rename was ended due to input while waiting for delay
        public bool OnEvent()
        {
            if (!m_IsRenaming)
                return true;

            if (!m_IsWaitingForDelay)
            {
                // We get control ID separate from OnGUI because we want to call OnGUI early and late: handle input first but render on top
                GUIUtility.GetControlID(84895748, FocusType.Passive);
                GUI.SetNextControlName(k_RenameOverlayFocusName);
                EditorGUI.FocusTextInControl(k_RenameOverlayFocusName);
                m_TextFieldControlID = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, m_EditFieldRect);
            }

            // Workaround for Event not having the original eventType stored
            m_OriginalEventType = Event.current.type;

            // Clear state if necessary while waiting for rename (0.5 second)
            if (m_IsWaitingForDelay && (m_OriginalEventType == EventType.MouseDown || m_OriginalEventType == EventType.KeyDown))
            {
                EndRename(false);
                return false;
            }

            return true;
        }

        public bool OnGUI()
        {
            return OnGUI(null);
        }

        // Should be called when IsRenaming () returns true to draw the overlay and handle events.
        // Returns true if renaming is still active, false if not (user canceled, accepted, clicked outside edit rect etc).
        // If textFieldStyle == null then a default style is used.
        public bool OnGUI(GUIStyle textFieldStyle)
        {
            if (m_IsWaitingForDelay)
            {
                // Delayed start
                return true;
            }

            // Ended from outside
            if (!m_IsRenaming)
            {
                return false;
            }

            if (m_UndoRedoWasPerformed)
            {
                m_UndoRedoWasPerformed = false;
                EndRename(false);
                return false;
            }

            if (m_EditFieldRect.width <= 0 || m_EditFieldRect.height <= 0 || m_TextFieldControlID == 0)
            {
                // Due to call order dependency we might not have a valid rect to render yet or have called OnEvent when renaming was active and therefore controlID can be uninitialzied so
                // we ensure to issue repaints until these states are valid
                HandleUtility.Repaint();
                return true;
            }

            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    evt.Use();
                    EndRename(false);
                    return false;
                }
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.Use();
                    EndRename(true);
                    return false;
                }
            }

            // Detect if we clicked outside the text field (must be before text field below which steals keyboard control)
            if (m_OriginalEventType == EventType.MouseDown && !m_EditFieldRect.Contains(Event.current.mousePosition))
            {
                EndRename(true);
                return false;
            }

            m_Name = DoTextField(m_Name, textFieldStyle);

            if (evt.type == EventType.ScrollWheel)
                evt.Use();

            return true;
        }

        string DoTextField(string text, GUIStyle textFieldStyle)
        {
            if (m_TextFieldControlID == 0)
                Debug.LogError("RenameOverlay: Ensure to call OnEvent() as early as possible in the OnGUI of the current EditorWindow!");

            if (s_DefaultTextFieldStyle == null)
                s_DefaultTextFieldStyle = "PR TextField";

            if (isRenamingFilename)
                EatInvalidChars();

            GUI.changed = false;
            bool dummy;

            // Ensure the rename textfield has keyboardcontrol (Could have been stolen previously in this OnGUI)
            if (GUIUtility.keyboardControl != m_TextFieldControlID)
                GUIUtility.keyboardControl = m_TextFieldControlID;

            GUIStyle style = textFieldStyle ?? s_DefaultTextFieldStyle;
            Rect rect = EditorGUI.IndentedRect(m_EditFieldRect);
            rect.xMin -= style.padding.left; // Adjust rect so rename text matches client text

            return EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, m_TextFieldControlID, rect, text, style, null, out dummy, false, false, false);
        }

        void EatInvalidChars()
        {
            if (isRenamingFilename)
            {
                Event evt = Event.current;
                if (GUIUtility.keyboardControl == m_TextFieldControlID && evt.GetTypeForControl(m_TextFieldControlID) == EventType.KeyDown)
                {
                    string errorMsg = "";

                    string invalidChars = EditorUtility.GetInvalidFilenameChars();
                    if (invalidChars.IndexOf(evt.character) > -1)
                        errorMsg = "A file name can't contain any of the following characters:\t" + invalidChars;

                    if (errorMsg != "")
                    {
                        evt.Use();  // Eat character: prevents the textfield from inputting this evt.character
                        ShowMessage(errorMsg);
                    }
                    else
                    {
                        RemoveMessage();
                    }
                }

                // Remove tooltip if screenpos of overlay has changed (handles the case where the main window is being moved or docked window
                // is resized)
                if (evt.type == EventType.Repaint)
                {
                    Rect screenPos = GetScreenRect();
                    if (!Mathf.Approximately(m_LastScreenPosition.x, screenPos.x) || !Mathf.Approximately(m_LastScreenPosition.y, screenPos.y))
                    {
                        RemoveMessage();
                    }
                    m_LastScreenPosition = screenPos;
                }
            }
        }

        Rect GetScreenRect()
        {
            return GUIUtility.GUIToScreenRect(m_EditFieldRect);
        }

        void ShowMessage(string msg)
        {
            TooltipView.Show(msg, GetScreenRect());
        }

        void RemoveMessage()
        {
            TooltipView.Close();
        }
    }

    [Serializable]
    internal class SerializableDelayedCallback : ScriptableObject
    {
        [SerializeField]
        private long m_CallbackTicks;

        [SerializeField]
        private UnityEvent m_Callback;

        protected SerializableDelayedCallback()
        {
            m_Callback = new UnityEvent();
            EditorApplication.update += Update;
        }

        public static SerializableDelayedCallback SubscribeCallback(UnityAction action, TimeSpan delayUntilCallback)
        {
            var serializableDelayedCallback = ScriptableObject.CreateInstance<SerializableDelayedCallback>();
            serializableDelayedCallback.m_CallbackTicks = DateTime.UtcNow.Add(delayUntilCallback).Ticks;
            serializableDelayedCallback.m_Callback.AddPersistentListener(action, UnityEventCallState.EditorAndRuntime);
            return serializableDelayedCallback;
        }

        public void Cancel()
        {
            EditorApplication.update -= Update;
        }

        private void Update()
        {
            var utcNowTicks = DateTime.UtcNow.Ticks;
            if (utcNowTicks >= m_CallbackTicks)
            {
                EditorApplication.update -= Update;
                m_Callback.Invoke();
            }
        }
    }

    internal class DelayedCallback
    {
        private System.Action m_Callback;
        private double m_CallbackTime;

        public DelayedCallback(System.Action function, double timeFromNow)
        {
            Assert.IsTrue(function != null);

            m_Callback = function;
            m_CallbackTime = EditorApplication.timeSinceStartup + timeFromNow;
            EditorApplication.update += Update;
        }

        public void Clear()
        {
            EditorApplication.update -= Update;
            m_CallbackTime = 0.0;
            m_Callback = null;
        }

        void Update()
        {
            if (EditorApplication.timeSinceStartup > m_CallbackTime)
            {
                // Clear state before firing callback to ensure reset (callback could call ExitGUI)
                var callback = m_Callback;
                Clear();

                callback();
            }
        }
    }
} // end namespace UnityEditor
