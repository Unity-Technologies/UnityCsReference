// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class MetroCertificatePasswordWindow : EditorWindow
    {
        private static readonly GUILayoutOption kLabelWidth = GUILayout.Width(110);
        private static readonly GUILayoutOption kButtonWidth = GUILayout.Width(110);
        private const float kSpace = 5;
        private const char kPasswordChar = '\u25cf';
        private const string kPasswordId = "password";

        private string path;
        private string password;
        private GUIContent message;
        private GUIStyle messageStyle;
        private string focus;

        public static void Show(string path)
        {
            var windows = (MetroCertificatePasswordWindow[])Resources.FindObjectsOfTypeAll(typeof(MetroCertificatePasswordWindow));
            var window = ((windows.Length > 0) ? windows[0] : ScriptableObject.CreateInstance<MetroCertificatePasswordWindow>());

            window.path = path;
            window.password = string.Empty;
            window.message = GUIContent.none;

            window.messageStyle = new GUIStyle(GUI.skin.label);
            window.messageStyle.fontStyle = FontStyle.Italic;

            window.focus = kPasswordId;

            if (windows.Length > 0)
            {
                window.Focus();
            }
            else
            {
                window.titleContent = EditorGUIUtility.TextContent("Enter Windows Store Certificate Password");

                window.position = new Rect(100, 100, 350, 90);
                window.minSize = new Vector2(window.position.width, window.position.height);
                window.maxSize = window.minSize;

                window.ShowUtility();
            }
        }

        public void OnGUI()
        {
            var e = Event.current;
            var close = false;
            var enter = false;

            if (e.type == EventType.KeyDown)
            {
                close = (e.keyCode == KeyCode.Escape);
                enter = ((e.keyCode == KeyCode.Return) || (e.keyCode == KeyCode.KeypadEnter));
            }

            using (HorizontalLayout.DoLayout())
            {
                GUILayout.Space(kSpace * 2);

                using (VerticalLayout.DoLayout())
                {
                    GUILayout.FlexibleSpace();

                    using (HorizontalLayout.DoLayout())
                    {
                        GUILayout.Label(EditorGUIUtility.TextContent("Password|Certificate password."), kLabelWidth);
                        GUI.SetNextControlName(kPasswordId);
                        password = GUILayout.PasswordField(password, kPasswordChar);
                    }

                    GUILayout.Space(kSpace * 2);

                    using (HorizontalLayout.DoLayout())
                    {
                        GUILayout.Label(message, messageStyle);

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(EditorGUIUtility.TextContent("Ok"), kButtonWidth) || enter)
                        {
                            message = GUIContent.none;

                            try
                            {
                                if (PlayerSettings.WSA.SetCertificate(path, password))
                                {
                                    close = true;
                                }
                                else
                                {
                                    message = EditorGUIUtility.TextContent("Invalid password.");
                                }
                            }
                            catch (UnityException ex)
                            {
                                Debug.LogError(ex.Message);
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(kSpace * 2);
            }

            if (close)
            {
                Close();
            }
            else if (focus != null)
            {
                EditorGUI.FocusTextInControl(focus);
                focus = null;
            }
        }
    }
}
