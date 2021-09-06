// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Hardware;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace UnityEditor.Networking.PlayerConnection
{
    internal interface IConnectionStateInternal : IConnectionState
    {
        EditorWindow parentWindow { get; }
        GUIContent notificationMessage { get; }
        bool deepProfilingSupported { get; }
        void AddItemsToTree(ConnectionTreeViewWindow connectionTreeViewWindow, Rect position);
        string connectionDisplayName { get; set; }
    }

    internal enum EditorConnectionTarget
    {
        None,
        MainEditorProcessPlaymode,
        MainEditorProcessEditmode,
        // add out-off-process player/profiler here
    }

    public static class PlayerConnectionGUIUtility
    {
        public static IConnectionState GetConnectionState(EditorWindow parentWindow, Action<string> connectedCallback = null)
        {
            return new GeneralConnectionState(parentWindow, (player, editorConnectionTarget) => connectedCallback?.Invoke(player));
        }

        internal static IConnectionState GetConnectionState(EditorWindow parentWindow, Action<EditorConnectionTarget> editorModeTargetSwitchedCallback, Func<EditorConnectionTarget, bool> editorModeTargetConnectionStatus, Action<string, EditorConnectionTarget?> connectedCallback = null)
        {
            return new GeneralConnectionState(parentWindow, connectedCallback, editorModeTargetSwitchedCallback, editorModeTargetConnectionStatus);
        }
    }

    static class Styles
    {
        public static readonly GUIStyle defaultDropdown = "MiniPullDown";
        public static readonly GUIContent dropdownButton = UnityEditor.EditorGUIUtility.TrTextContent("", "Target Selection: Choose the target to connect to.");
    }

    public static class PlayerConnectionGUI
    {
        public static void ConnectionTargetSelectionDropdown(Rect rect, IConnectionState state, GUIStyle style = null)
        {
            var internalState = state as IConnectionStateInternal;
            if (internalState?.parentWindow)
            {
                if (internalState.notificationMessage != null)
                    internalState.parentWindow.ShowNotification(internalState.notificationMessage);
                else
                    internalState.parentWindow.RemoveNotification();
            }
            style ??= Styles.defaultDropdown;
            if (!UnityEditor.EditorGUI.DropdownButton(rect, Styles.dropdownButton, FocusType.Keyboard, style))
                return;

            ConnectionTreeViewWindow ctvw = new ConnectionTreeViewWindow(internalState, rect);
            PopupWindow.Show(rect, ctvw);
        }
    }

    public static class PlayerConnectionGUILayout
    {
        public static void ConnectionTargetSelectionDropdown(IConnectionState state, GUIStyle style = null)
        {
            if (style == null)
                style = Styles.defaultDropdown;
            Styles.dropdownButton.text = state.connectedToTarget == ConnectionTarget.Editor ? "Edit Mode" : state.connectionName;

            Styles.dropdownButton.text = ConnectionUIHelper.GetToolbarContent(state.connectionName);
            var size = style.CalcSize(Styles.dropdownButton);
            Rect connectRect = GUILayoutUtility.GetRect(size.x, size.y);

            PlayerConnectionGUI.ConnectionTargetSelectionDropdown(connectRect, state, style);
        }
    }

    internal class GeneralConnectionState : IConnectionStateInternal
    {
        static class Content
        {
            public static readonly GUIContent Playmode = UnityEditor.EditorGUIUtility.TrTextContent("Play Mode");
            public static readonly GUIContent Editmode = UnityEditor.EditorGUIUtility.TrTextContent("Edit Mode");
            public static readonly GUIContent EnterIPText = UnityEditor.EditorGUIUtility.TrTextContent("<Enter IP>");
            public static readonly GUIContent AutoconnectedPlayer = UnityEditor.EditorGUIUtility.TrTextContent("(Autoconnected Player)");
            public static readonly GUIContent ConnectingToPlayerMessage = UnityEditor.EditorGUIUtility.TrTextContent("Connecting to player... (this can take a while)");

            public static readonly string LocalHostProhibited = L10n.Tr(" (Localhost prohibited)");
            public static readonly string VersionMismatch = L10n.Tr(" (Version mismatch)");

            public static readonly string Editor = "Editor";
            public static readonly string DirectConnection = "Direct Connection";
        }
        static GUIContent s_NotificationMessage;

        public GUIContent notificationMessage => s_NotificationMessage;

        // keep this constant in sync with PLAYER_DIRECT_IP_CONNECT_GUID in GeneralConnection.h
        const int PLAYER_DIRECT_IP_CONNECT_GUID = 0xFEED;
        // keep this constant in sync with PLAYER_DIRECT_URL_CONNECT_GUID in GeneralConnection.h
        const int PLAYER_DIRECT_URL_CONNECT_GUID = 0xFEEE;
        const string k_EditorConnectionName = "Editor";

        public EditorWindow parentWindow { get; private set; }

        public ConnectionTarget connectedToTarget => ProfilerDriver.IsConnectionEditor() ? ConnectionTarget.Editor : ConnectionTarget.Player;

        public string connectionName
        {
            get
            {
                string name = ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);
                if (m_EditorModeTargetState.HasValue && name.Contains(k_EditorConnectionName))
                {
                    if (m_EditorModeTargetConnectionStatus(EditorConnectionTarget.MainEditorProcessEditmode))
                        name = Content.Editmode.text;
                    else
                        name = Content.Playmode.text;
                }
                return name;
            }
        }

        public string connectionDisplayName { get; set; }

        public bool deepProfilingSupported => ProfilerDriver.IsDeepProfilingSupported(ProfilerDriver.connectedProfiler);

        event Action<string, EditorConnectionTarget?> connected;


        event Action<EditorConnectionTarget> switchedEditorModeTarget;
        Func<EditorConnectionTarget, bool> m_EditorModeTargetConnectionStatus;
        EditorConnectionTarget? m_EditorModeTargetState = null;


        static List<WeakReference> s_AllGeneralAttachToPlayerStates = new List<WeakReference>();

        public GeneralConnectionState(EditorWindow parentWindow, Action<string, EditorConnectionTarget?> connectedCallback = null, Action<EditorConnectionTarget> editorModeTargetSwitchedCallback = null, Func<EditorConnectionTarget, bool> editorModeTargetConnectionStatus = null)
        {
            this.parentWindow = parentWindow;
            if (parentWindow != null)
                connected += (player, editorConnectionTarget) => this.parentWindow.Repaint();

            if (connectedCallback != null)
                connected += connectedCallback;

            if (editorModeTargetSwitchedCallback != null)
            {
                Debug.Assert(editorModeTargetConnectionStatus != null, $"{nameof(editorModeTargetConnectionStatus)} can't be null when a {nameof(editorModeTargetSwitchedCallback)} is provided.");
                switchedEditorModeTarget += editorModeTargetSwitchedCallback;
                m_EditorModeTargetConnectionStatus = editorModeTargetConnectionStatus;
                m_EditorModeTargetState = EditorConnectionTarget.None;
            }

            s_AllGeneralAttachToPlayerStates.Add(new WeakReference(this));
        }

        private static void SuccessfullyConnectedToPlayer(string player, EditorConnectionTarget? editorConnectionTarget = null)
        {
            for (int i = s_AllGeneralAttachToPlayerStates.Count - 1; i >= 0; i--)
            {
                if (s_AllGeneralAttachToPlayerStates[i] == null || !s_AllGeneralAttachToPlayerStates[i].IsAlive)
                {
                    s_AllGeneralAttachToPlayerStates.RemoveAt(i);
                }
                var generalConnectionState = (s_AllGeneralAttachToPlayerStates[i].Target as GeneralConnectionState);
                generalConnectionState.connected?.Invoke(player, editorConnectionTarget);
                if (editorConnectionTarget.HasValue)
                {
                    generalConnectionState.switchedEditorModeTarget?.Invoke(editorConnectionTarget.Value);
                }
                else
                {
                    if (player.Contains(k_EditorConnectionName))
                    {
                        // if e.g. the console or the memory profiler connects to the Editor, the profiler should switch to PlayMode profiling, not to Editmode profiling
                        // especially since falling back onto the Editor is the default.
                        generalConnectionState.switchedEditorModeTarget?.Invoke(EditorConnectionTarget.MainEditorProcessPlaymode);
                    }
                    else
                    {
                        generalConnectionState.switchedEditorModeTarget?.Invoke(EditorConnectionTarget.None);
                    }
                }
            }
        }

        public virtual void AddItemsToTree(ConnectionTreeViewWindow menu, Rect position)
        {
            bool hasAnyConnectionOpen = false;
            AddAvailablePlayerConnections(menu, ref hasAnyConnectionOpen);
            AddAvailableDeviceConnections(menu, ref hasAnyConnectionOpen);
            AddLastConnectedIP(menu, ref hasAnyConnectionOpen);

            // Case 810030: Check if player is connected using AutoConnect Profiler feature via 'connect <ip> string in PlayerConnectionConfigFile
            // In that case ProfilerDriver.GetAvailableProfilers() won't return the connected player
            // But we still want to show that it's connected, because the data is incoming
            if (!ProfilerDriver.IsConnectionEditor() && !hasAnyConnectionOpen)
            {
                menu.AddDisabledItem(new ConnectionDropDownItem(Content.AutoconnectedPlayer.text, ProfilerDriver.connectedProfiler, Content.DirectConnection, ConnectionDropDownItem.ConnectionMajorGroup.Direct, () => true, null));
            }

            AddConnectionViaEnterIPWindow(menu, GUIUtility.GUIToScreenRect(position));
        }

        internal static void DirectIPConnect(string ip)
        {
            // Profiler.DirectIPConnect is a blocking call, so a notification message is used to show the progress
            if (ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID)
                return;
            s_NotificationMessage = Content.ConnectingToPlayerMessage;
            ProfilerDriver.DirectIPConnect(ip);
            s_NotificationMessage = null;
            SuccessfullyConnectedToPlayer(ip);
        }

        internal static void DirectURLConnect(string url)
        {
            // Profiler.DirectURLConnect is a blocking call, so a notification message is used to show the progress
            s_NotificationMessage = Content.ConnectingToPlayerMessage;
            ProfilerDriver.DirectURLConnect(url);
            s_NotificationMessage = null;
            SuccessfullyConnectedToPlayer(url);
        }

        void AddLastConnectedIP(ConnectionTreeViewWindow menuOptions, ref bool hasOpenConnection)
        {
            string lastIP = AttachToPlayerPlayerIPWindow.GetLastIPString();
            if (string.IsNullOrEmpty(lastIP))
                return;

            bool isConnected = ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID;
            hasOpenConnection |= isConnected;
            menuOptions.AddItem(new ConnectionDropDownItem(lastIP, PLAYER_DIRECT_IP_CONNECT_GUID,
                Content.DirectConnection, ConnectionDropDownItem.ConnectionMajorGroup.Direct, () => ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID,
                () => DirectIPConnect(lastIP)));
        }

        internal static string GetConnectionName(int guid)
        {
            // Connection identifier is constructed in the PlayerConnection::ConstructWhoamiString()
            // in a form "{platform name}(host name or ip)[:port]
            var name = ProfilerDriver.GetConnectionIdentifier(guid);

            // Ignore Editor connections which named explicitly after project name.
            var portSpacerIndex = name.LastIndexOf(')');
            if (portSpacerIndex == -1)
                return name;

            // Port already specified
            if (name.Length > (portSpacerIndex + 1) && name[portSpacerIndex + 1] == ':')
                return name;

            // If port hasn't been specified in the connection identifier, we place it with "host name or ip" segment.
            var port = ProfilerDriver.GetConnectionPort(guid);
            return string.Format("{0}:{1})", name.Substring(0, portSpacerIndex), port);
        }

        void AddAvailablePlayerConnections(ConnectionTreeViewWindow menuOptions, ref bool hasOpenConnection,
            Func<bool> disabler = null)
        {
            int[] connectionGuids = ProfilerDriver.GetAvailableProfilers();
            for (int index = 0; index < connectionGuids.Length; index++)
            {
                int guid = connectionGuids[index];
                string name = GetConnectionName(guid);
                bool isProhibited = ProfilerDriver.IsIdentifierOnLocalhost(guid) && (name.Contains("MetroPlayerX") || name.Contains("UWPPlayerX"));
                bool enabled = !isProhibited && ProfilerDriver.IsIdentifierConnectable(guid);

                bool isConnected = ProfilerDriver.connectedProfiler == guid;
                hasOpenConnection |= isConnected;
                if (!enabled)
                {
                    if (isProhibited)
                        name += Content.LocalHostProhibited;
                    else
                        name += Content.VersionMismatch;
                }
                if (enabled)
                {
                    if (m_EditorModeTargetState.HasValue && name.Contains(k_EditorConnectionName))
                    {
                        if (!menuOptions.HasItem(Content.Playmode.text) && !name.StartsWith("Profiler-"))
                        {
                            menuOptions.AddItem(new ConnectionDropDownItem(Content.Playmode.text, guid, Content.Editor,
                                ConnectionDropDownItem.ConnectionMajorGroup.Editor,
                                () => ProfilerDriver.connectedProfiler == guid &&
                                !m_EditorModeTargetConnectionStatus(EditorConnectionTarget
                                    .MainEditorProcessPlaymode),
                                () =>
                                {
                                    ProfilerDriver.connectedProfiler = guid;
                                    SuccessfullyConnectedToPlayer(connectionName,
                                        EditorConnectionTarget.MainEditorProcessPlaymode);
                                }));

                            menuOptions.AddItem(new ConnectionDropDownItem(Content.Editmode.text, guid, Content.Editor,
                                ConnectionDropDownItem.ConnectionMajorGroup.Editor,
                                () => ProfilerDriver.connectedProfiler == guid &&
                                m_EditorModeTargetConnectionStatus(EditorConnectionTarget
                                    .MainEditorProcessEditmode),
                                () =>
                                {
                                    ProfilerDriver.connectedProfiler = guid;
                                    SuccessfullyConnectedToPlayer(connectionName,
                                        EditorConnectionTarget.MainEditorProcessEditmode);
                                }));
                        }
                    }
                    else
                    {
                        menuOptions.AddItem(new ConnectionDropDownItem(
                            name, guid, null, ConnectionDropDownItem.ConnectionMajorGroup.Unknown,
                            () => ProfilerDriver.connectedProfiler == guid,
                            () =>
                            {
                                ProfilerDriver.connectedProfiler = guid;
                                if (ProfilerDriver.connectedProfiler == guid)
                                {
                                    SuccessfullyConnectedToPlayer(connectionName);
                                }
                            }));
                    }
                }
                else
                    menuOptions.AddDisabledItem(new ConnectionDropDownItem(name, guid, null, ConnectionDropDownItem.ConnectionMajorGroup.Unknown, () => ProfilerDriver.connectedProfiler == guid, null));
            }
        }

        void AddAvailableDeviceConnections(ConnectionTreeViewWindow menuOptions, ref bool hasOpenConnection)
        {
            foreach (var device in DevDeviceList.GetDevices())
            {
                bool supportsPlayerConnection = (device.features & DevDeviceFeatures.PlayerConnection) != 0;
                if (!device.isConnected || !supportsPlayerConnection)
                    continue;

                var url = "device://" + device.id;
                bool isConnected = ProfilerDriver.connectedProfiler == PLAYER_DIRECT_URL_CONNECT_GUID && ProfilerDriver.directConnectionUrl == url;
                hasOpenConnection |= isConnected;
                //iphone handles the naming differently to android
                menuOptions.AddItem(new ConnectionDropDownItem(
                    device.type == "Android"
                    ? device.name.Substring(device.name.IndexOf('(') + 1).TrimEnd(')')
                    : device.name, PLAYER_DIRECT_URL_CONNECT_GUID, "Devices" ,
                    ConnectionDropDownItem.ConnectionMajorGroup.Local,
                    () => ProfilerDriver.connectedProfiler == PLAYER_DIRECT_URL_CONNECT_GUID &&
                    ProfilerDriver.directConnectionUrl == url, () => DirectURLConnect(url), true, device.type == "iOS" ? "IPhonePlayer" : device.type));
            }
        }

        void AddConnectionViaEnterIPWindow(ConnectionTreeViewWindow menuOptions, Rect buttonScreenRect,
            Func<bool> disabler = null)
        {
            menuOptions.AddItem(new ConnectionDropDownItem(Content.EnterIPText.text, -2, Content.DirectConnection, ConnectionDropDownItem.ConnectionMajorGroup.Direct, () => false, () =>
            {
                AttachToPlayerPlayerIPWindow.Show(buttonScreenRect);
                GUIUtility.ExitGUI(); // clear the gui state to prevent hot control issues
            }));
        }

        private bool disposed = false; // To detect redundant calls

        ~GeneralConnectionState()
        {
            if (!disposed)
                // Referring to the interface here, because the user only knows about the public IConnectionState interfaces and nothing about the internal GeneralConnectionState (except for the fact that it's about to show up in this error's callstack)
                Debug.LogError("IConnectionState was not Disposed! Please make sure to call Dispose in OnDisable of the EditorWindow in which it was used.");
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (s_AllGeneralAttachToPlayerStates != null)
                {
                    for (int i = s_AllGeneralAttachToPlayerStates.Count - 1; i >= 0; i--)
                    {
                        if (!s_AllGeneralAttachToPlayerStates[i].IsAlive || s_AllGeneralAttachToPlayerStates[i].Target == this)
                        {
                            s_AllGeneralAttachToPlayerStates.RemoveAt(i);
                        }
                    }
                }
                parentWindow = null;
                disposed = true;
            }
        }
    }

    internal class AttachToPlayerPlayerIPWindow : EditorWindow
    {
        static class Content
        {
            public static readonly GUIContent ConnectButtonContent = UnityEditor.EditorGUIUtility.TrTextContent("Connect");
            public static readonly string EnterPlayerIPWindowName = L10n.Tr("Enter Player IP");
        }
        private const string k_TextFieldControlId = "IPWindow";
        private const string k_LastIPEditorPrefKey = "ProfilerLastIP";

        private string m_IPString;
        private bool m_DidFocus = false;


        public static void Show(Rect buttonScreenRect)
        {
            Rect rect = new Rect(buttonScreenRect.x, buttonScreenRect.yMax, 300, 50);
            AttachToPlayerPlayerIPWindow w = EditorWindow.GetWindowWithRect<AttachToPlayerPlayerIPWindow>(rect, true, Content.EnterPlayerIPWindowName);
            w.position = rect;
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        void OnEnable()
        {
            m_IPString = GetLastIPString();
        }

        public static string GetLastIPString()
        {
            return EditorPrefs.GetString(k_LastIPEditorPrefKey, "");
        }

        void OnGUI()
        {
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);

            UnityEditor.EditorGUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                GUI.SetNextControlName(k_TextFieldControlId);
                m_IPString = UnityEditor.EditorGUILayout.TextField(m_IPString);


                if (!m_DidFocus)
                {
                    m_DidFocus = true;
                    UnityEditor.EditorGUI.FocusTextInControl(k_TextFieldControlId);
                }

                GUI.enabled = m_IPString.Length != 0;
                if (GUILayout.Button(Content.ConnectButtonContent) || hitEnter)
                {
                    Close();
                    // Save ip
                    EditorPrefs.SetString(k_LastIPEditorPrefKey, m_IPString);
                    GeneralConnectionState.DirectIPConnect(m_IPString);
                    GUIUtility.ExitGUI();
                }
            }
            UnityEditor.EditorGUILayout.EndVertical();
        }
    }
}
