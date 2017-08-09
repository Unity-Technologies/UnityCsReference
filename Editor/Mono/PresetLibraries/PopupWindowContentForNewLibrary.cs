// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    class PopupWindowContentForNewLibrary : PopupWindowContent
    {
        string m_NewLibraryName = "";
        int m_SelectedIndexInPopup = 0;
        string m_ErrorString = null;
        Rect m_WantedSize;

        Func<string, PresetFileLocation, string> m_CreateLibraryCallback;

        class Texts
        {
            public GUIContent header = new GUIContent("Create New Library");
            public GUIContent name = new GUIContent("Name");
            public GUIContent location = new GUIContent("Location");
            public GUIContent[] fileLocations = new[] {new GUIContent("Preferences Folder"), new GUIContent("Project Folder")};
            public PresetFileLocation[] fileLocationOrder = new[] { PresetFileLocation.PreferencesFolder, PresetFileLocation.ProjectFolder }; // must match order of fileLocations above
        }
        static Texts s_Texts;

        public PopupWindowContentForNewLibrary(Func<string, PresetFileLocation, string> createLibraryCallback)
        {
            m_CreateLibraryCallback = createLibraryCallback;
        }

        public override void OnGUI(Rect rect)
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            KeyboardHandling(editorWindow);

            float labelWidth = 80f;

            Rect size = EditorGUILayout.BeginVertical();
            if (Event.current.type != EventType.Layout)
                m_WantedSize = size;

            // Header
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(s_Texts.header, EditorStyles.boldLabel);
            } GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            {
                // Name
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(s_Texts.name, GUILayout.Width(labelWidth));

                    EditorGUI.FocusTextInControl("NewLibraryName");
                    GUI.SetNextControlName("NewLibraryName");
                    m_NewLibraryName = GUILayout.TextField(m_NewLibraryName);
                } GUILayout.EndHorizontal();

                // Location
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(s_Texts.location, GUILayout.Width(labelWidth));
                    m_SelectedIndexInPopup = EditorGUILayout.Popup(m_SelectedIndexInPopup, s_Texts.fileLocations);
                }
                GUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
                m_ErrorString = null;

            // Create
            GUILayout.BeginHorizontal();
            {
                if (!string.IsNullOrEmpty(m_ErrorString))
                {
                    Color orgColor = GUI.color;
                    GUI.color = new Color(1, 0.8f, 0.8f);
                    GUILayout.Label(GUIContent.Temp(m_ErrorString), EditorStyles.helpBox);
                    GUI.color = orgColor;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(GUIContent.Temp("Create")))
                {
                    CreateLibraryAndCloseWindow(editorWindow);
                }
            } GUILayout.EndHorizontal();

            GUILayout.Space(15);

            EditorGUILayout.EndVertical();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(350, m_WantedSize.height > 0 ? m_WantedSize.height : 90);
        }

        void KeyboardHandling(EditorWindow editorWindow)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.KeyDown:
                    switch (evt.keyCode)
                    {
                        case KeyCode.KeypadEnter:
                        case KeyCode.Return:
                            CreateLibraryAndCloseWindow(editorWindow);
                            break;
                        case KeyCode.Escape:
                            editorWindow.Close();
                            break;
                    }
                    break;
            }
        }

        void CreateLibraryAndCloseWindow(EditorWindow editorWindow)
        {
            PresetFileLocation fileLocation = s_Texts.fileLocationOrder[m_SelectedIndexInPopup];
            m_ErrorString = m_CreateLibraryCallback(m_NewLibraryName, fileLocation);
            if (string.IsNullOrEmpty(m_ErrorString))
                editorWindow.Close();
        }
    }
}
