// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Hardware;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    internal struct ProfilerChoise
    {
        public string     Name;
        public bool       Enabled;
        public Func<bool> IsSelected;
        public Action     ConnectTo;
    }

    internal class AttachProfilerUI
    {
        private static string kEnterIPText = "<Enter IP>";
        private static GUIContent ms_NotificationMessage;

        // keep this constant in sync with PLAYER_DIRECT_IP_CONNECT_GUID in GeneralConnection.h
        const int PLAYER_DIRECT_IP_CONNECT_GUID = 0xFEED;
        // keep this constant in sync with PLAYER_DIRECT_URL_CONNECT_GUID in GeneralConnection.h
        const int PLAYER_DIRECT_URL_CONNECT_GUID = 0xFEEE;

        protected void SelectProfilerClick(object userData, string[] options, int selected)
        {
            var profilers = (List<ProfilerChoise>)userData;
            if (selected < profilers.Count())
                profilers[selected].ConnectTo();
        }

        public bool IsEditor()
        {
            return ProfilerDriver.IsConnectionEditor();
        }

        public string GetConnectedProfiler()
        {
            return ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);
        }

        public static void DirectIPConnect(string ip)
        {
            // Profiler.DirectIPConnect is a blocking call, so a notification message and the console are used to show progress
            ConsoleWindow.ShowConsoleWindow(true);
            ms_NotificationMessage = new GUIContent("Connecting to player...(this can take a while)");
            ProfilerDriver.DirectIPConnect(ip);
            ms_NotificationMessage = null;
        }

        public static void DirectURLConnect(string url)
        {
            // Profiler.DirectURLConnect is a blocking call, so a notification message and the console are used to show progress
            ConsoleWindow.ShowConsoleWindow(true);
            ms_NotificationMessage = new GUIContent("Connecting to player...(this can take a while)");
            ProfilerDriver.DirectURLConnect(url);
            ms_NotificationMessage = null;
        }

        public void OnGUILayout(EditorWindow window)
        {
            OnGUI();

            if (ms_NotificationMessage != null)
                window.ShowNotification(ms_NotificationMessage);
            else
                window.RemoveNotification();
        }

        static void AddLastIPProfiler(List<ProfilerChoise> profilers)
        {
            string lastIP = ProfilerIPWindow.GetLastIPString();
            if (string.IsNullOrEmpty(lastIP))
                return;

            profilers.Add(new ProfilerChoise()
            {
                Name = lastIP,
                Enabled = true,
                IsSelected = () => { return ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID; },
                ConnectTo = () => { DirectIPConnect(lastIP); }
            });
        }

        static void AddPlayerProfilers(List<ProfilerChoise> profilers)
        {
            int[] connectionGuids = ProfilerDriver.GetAvailableProfilers();
            for (int index = 0; index < connectionGuids.Length; index++)
            {
                int guid = connectionGuids[index];
                string name = ProfilerDriver.GetConnectionIdentifier(guid);
                bool isProhibited = ProfilerDriver.IsIdentifierOnLocalhost(guid) && name.Contains("MetroPlayerX");
                bool enabled = !isProhibited && ProfilerDriver.IsIdentifierConnectable(guid);

                if (!enabled)
                {
                    if (isProhibited)
                        name += " (Localhost prohibited)";
                    else
                        name += " (Version mismatch)";
                }

                profilers.Add(new ProfilerChoise()
                {
                    Name = name,
                    Enabled = enabled,
                    IsSelected = () => { return ProfilerDriver.connectedProfiler == guid; },
                    ConnectTo = () => { ProfilerDriver.connectedProfiler = guid; }
                });
            }
        }

        static void AddDeviceProfilers(List<ProfilerChoise> profilers)
        {
            foreach (var device in DevDeviceList.GetDevices())
            {
                bool supportsPlayerConnection = (device.features & DevDeviceFeatures.PlayerConnection) != 0;
                if (!device.isConnected || !supportsPlayerConnection)
                    continue;

                var url = "device://" + device.id;
                profilers.Add(new ProfilerChoise()
                {
                    Name = device.name,
                    Enabled = true,
                    IsSelected = () =>
                        {
                            return (ProfilerDriver.connectedProfiler == PLAYER_DIRECT_URL_CONNECT_GUID)
                            && (ProfilerDriver.directConnectionUrl == url);
                        },
                    ConnectTo = () => { DirectURLConnect(url); }
                });
            }
        }

        void AddEnterIPProfiler(List<ProfilerChoise> profilers, Rect buttonScreenRect)
        {
            profilers.Add(new ProfilerChoise()
            {
                Name = kEnterIPText,
                Enabled = true,
                IsSelected = () => { return false; },
                ConnectTo = () => { ProfilerIPWindow.Show(buttonScreenRect); }
            });
        }

        public void OnGUI()
        {
            var m_CurrentProfiler = EditorGUIUtility.TextContent(GetConnectedProfiler() + "|Specifies the target player for receiving profiler and log data.");
            var size = EditorStyles.toolbarDropDown.CalcSize(m_CurrentProfiler);
            Rect connectRect = GUILayoutUtility.GetRect(size.x, size.y);

            if (!EditorGUI.DropdownButton(connectRect, m_CurrentProfiler, FocusType.Passive, EditorStyles.toolbarDropDown))
                return;

            var profilers = new List<ProfilerChoise>();
            profilers.Clear();
            AddPlayerProfilers(profilers);
            AddDeviceProfilers(profilers);
            AddLastIPProfiler(profilers);

            // Case 810030: Check if player is connected using AutoConnect Profiler feature via 'connect <ip> string in PlayerConnectionConfigFile
            // In that case ProfilerDriver.GetAvailableProfilers() won't return the connected player
            // But we still want to show that it's connected, because the data is incoming
            if (!ProfilerDriver.IsConnectionEditor())
            {
                bool anyEnabled = profilers.Any(p => p.IsSelected());
                if (!anyEnabled)
                {
                    profilers.Add(new ProfilerChoise()
                    {
                        Name = "(Autoconnected Player)",
                        Enabled = false,
                        IsSelected = () =>
                            {
                                return true;
                            },
                        ConnectTo = () => {}
                    });
                }
            }

            AddEnterIPProfiler(profilers, GUIUtility.GUIToScreenRect(connectRect));

            OnGUIMenu(connectRect, profilers);
        }

        protected virtual void OnGUIMenu(Rect connectRect, List<ProfilerChoise> profilers)
        {
            string[] names = profilers.Select(p => p.Name).ToArray();
            bool[] enabled = profilers.Select(p => p.Enabled).ToArray();
            int[] selected;
            int index = profilers.FindIndex(p => p.IsSelected());
            if (index == -1)
                selected = new int[0];
            else
                selected = new int[] { index };

            EditorUtility.DisplayCustomMenu(connectRect, names, enabled, selected, SelectProfilerClick, profilers);
        }
    }

    internal class ProfilerIPWindow : EditorWindow
    {
        private const string kTextFieldId = "IPWindow";
        private const string kLastIP = "ProfilerLastIP";
        internal string m_IPString;
        internal bool didFocus = false;

        public static void Show(Rect buttonScreenRect)
        {
            Rect rect = new Rect(buttonScreenRect.x, buttonScreenRect.yMax, 300, 50);
            ProfilerIPWindow w = EditorWindow.GetWindowWithRect<ProfilerIPWindow>(rect, true, "Enter Player IP");
            w.position = rect;
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        void OnEnable()
        {
            m_IPString = GetLastIPString();
        }

        public static string GetLastIPString()
        {
            return EditorPrefs.GetString(kLastIP, "");
        }

        void OnGUI()
        {
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            GUI.SetNextControlName(kTextFieldId);

            /*Rect contentRect = */ EditorGUILayout.BeginVertical();
            {
                GUILayout.Space(5);
                m_IPString = EditorGUILayout.TextField(m_IPString);


                if (!didFocus)
                {
                    didFocus = true;
                    EditorGUI.FocusTextInControl(kTextFieldId);
                }

                GUI.enabled = m_IPString.Length != 0;
                if (GUILayout.Button("Connect") || hitEnter)
                {
                    Close();
                    // Save ip
                    EditorPrefs.SetString(kLastIP, m_IPString);
                    AttachProfilerUI.DirectIPConnect(m_IPString);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndVertical();

            //position.height = contentRect.height;
        }
    }
}
