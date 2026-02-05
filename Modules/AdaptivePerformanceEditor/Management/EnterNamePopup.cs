// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.AdaptivePerformance.Editor
{
    class EnterNamePopup : PopupWindowContent
    {
        public delegate void EnterDelegate(string str);
        readonly EnterDelegate EnterCB;
        private string m_NewProfileName = "New Scaler Profile";
        private bool m_NeedsFocus = true;
        private List<string> existingProfileNames = new List<string>();
        static string s_WarningPopup = L10n.Tr("Warning");
        static string s_WarningPopupOption = L10n.Tr("Ok");

        public EnterNamePopup(SerializedProperty profiles, EnterDelegate cb)
        {
            EnterCB = cb;
            for (int i = 0; i < profiles.arraySize; i++)
            {
                string profileName = profiles.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue;
                if (!string.IsNullOrEmpty(profileName))
                    existingProfileNames.Add(profileName);
            }
            m_NewProfileName = ObjectNames.GetUniqueName(existingProfileNames.ToArray(), m_NewProfileName);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing + 14);
        }

        public override void OnGUI(Rect windowRect)
        {
            GUILayout.Space(5);
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            GUI.SetNextControlName("ProfileName");
            m_NewProfileName = EditorGUILayout.TextField("New Profile Name", m_NewProfileName);

            if (m_NeedsFocus)
            {
                m_NeedsFocus = false;
                EditorGUI.FocusTextInControl("ProfileName");
            }

            GUI.enabled = m_NewProfileName.Length != 0;
            if (GUILayout.Button("Save") || hitEnter)
            {
                m_NewProfileName = m_NewProfileName.Trim();

                if (existingProfileNames.Contains(m_NewProfileName))
                {
                    EditorUtility.DisplayDialog(s_WarningPopup, L10n.Tr("The Adaptive Performance Scaler Profile named " + m_NewProfileName + " already exists. Please rename and try again."), s_WarningPopupOption);
                    return;
                }

                if (string.IsNullOrEmpty(m_NewProfileName))
                {
                    EditorUtility.DisplayDialog(s_WarningPopup, L10n.Tr("The Adaptive Performance Scaler Profile name is empty or contains white space only. Trailing white spaces are removed. Please rename and try again."), s_WarningPopupOption);
                    return;
                }

                EnterCB(m_NewProfileName);
                editorWindow.Close();
            }
        }
    }
}
