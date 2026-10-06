// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal sealed class HorizontalLayout : IDisposable
    {
        [NoAutoStaticsCleanup] // Stateless singleton layout helper; safe to persist across code reload.
        private static readonly HorizontalLayout instance = new HorizontalLayout();

        public static IDisposable DoLayout()
        {
            GUILayout.BeginHorizontal();
            return instance;
        }

        private HorizontalLayout()
        {
        }

        void IDisposable.Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }
    internal sealed class VerticalLayout : IDisposable
    {
        [NoAutoStaticsCleanup] // Stateless singleton layout helper; safe to persist across code reload.
        private static readonly VerticalLayout instance = new VerticalLayout();

        public static IDisposable DoLayout()
        {
            GUILayout.BeginVertical();
            return instance;
        }

        private VerticalLayout()
        {
        }

        void IDisposable.Dispose()
        {
            GUILayout.EndVertical();
        }
    }

    internal class MetroCreateTestCertificateWindow : EditorWindow
    {
        private static readonly GUILayoutOption kLabelWidth = GUILayout.Width(110);
        private static readonly GUILayoutOption kButtonWidth = GUILayout.Width(110);
        private const float kSpace = 5;
        private const char kPasswordChar = '\u25cf';
        private const string kPublisherId = "publisher";
        private const string kPasswordId = "password";
        private const string kConfirmId = "confirm";

        private string path;
        private string publisher;
        private string password;
        private string confirm;
        private GUIContent message;
        private GUIStyle messageStyle;
        private string focus;

        public static void Show(string publisher)
        {
            var windows = (MetroCreateTestCertificateWindow[])Resources.FindObjectsOfTypeAll(typeof(MetroCreateTestCertificateWindow));
            var window = ((windows.Length > 0) ? windows[0] : ScriptableObject.CreateInstance<MetroCreateTestCertificateWindow>());

            window.path = Path.Combine(Application.dataPath, "WSATestCertificate.pfx").Replace('\\', '/');
            window.publisher = publisher;
            window.password = string.Empty;
            window.confirm = window.password;
            window.message = (File.Exists(window.path) ? L10n.TextContent("Current file will be overwritten.", null, null, null) : GUIContent.none);

            window.messageStyle = new GUIStyle(GUI.skin.label);
            window.messageStyle.fontStyle = FontStyle.Italic;

            window.focus = kPublisherId;

            if (windows.Length > 0)
            {
                window.Focus();
            }
            else
            {
                window.titleContent = L10n.TextContent("Create Test Certificate for Windows Store", null, null, null);

                window.position = new Rect(100, 100, 350, 140);
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
                        GUILayout.Label(L10n.TextContent("Publisher", "Publisher of the package.", null, null), kLabelWidth);
                        GUI.SetNextControlName(kPublisherId);
                        publisher = GUILayout.TextField(publisher);
                    }

                    GUILayout.Space(kSpace);

                    using (HorizontalLayout.DoLayout())
                    {
                        GUILayout.Label(L10n.TextContent("Password", "Certificate password.", null, null), kLabelWidth);
                        GUI.SetNextControlName(kPasswordId);
                        password = GUILayout.PasswordField(password, kPasswordChar);
                    }

                    GUILayout.Space(kSpace);

                    using (HorizontalLayout.DoLayout())
                    {
                        GUILayout.Label(L10n.TextContent("Confirm password", "Re-enter certificate password.", null, null), kLabelWidth);
                        GUI.SetNextControlName(kConfirmId);
                        confirm = GUILayout.PasswordField(confirm, kPasswordChar);
                    }

                    GUILayout.Space(kSpace * 2);

                    using (HorizontalLayout.DoLayout())
                    {
                        GUILayout.Label(message, messageStyle);

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(L10n.TextContent("Create", null, null, null), kButtonWidth) || enter)
                        {
                            message = GUIContent.none;

                            if (string.IsNullOrEmpty(publisher))
                            {
                                message = L10n.TextContent("Publisher must be specified.", null, null, null);
                                focus = kPublisherId;
                            }
                            /*else if (!IsValidPublisher(publisher))
                            {
                                message = EditorGUIUtility.TrTextContent("Invalid publisher.");
                                focus = kPublisherId;
                            }*/
                            else if (password != confirm)
                            {
                                if (string.IsNullOrEmpty(confirm))
                                {
                                    message = L10n.TextContent("Confirm the password.", null, null, null);
                                    focus = kConfirmId;
                                }
                                else
                                {
                                    message = L10n.TextContent("Passwords do not match.", null, null, null);
                                    password = string.Empty;
                                    confirm = password;
                                    focus = kPasswordId;
                                }
                            }
                            else
                            {
                                try
                                {
                                    EditorUtility.WSACreateTestCertificate(path, publisher, password, true);

                                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                                    if (!PlayerSettings.WSA.SetCertificate(FileUtil.GetProjectRelativePath(path), password))
                                    {
                                        message = L10n.TextContent("Invalid password.", null, null, null);
                                    }

                                    close = true;
                                }
                                catch (UnityException ex)
                                {
                                    Debug.LogError(ex.Message);
                                }
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
