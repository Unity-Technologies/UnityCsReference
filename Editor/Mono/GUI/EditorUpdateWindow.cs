// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class EditorUpdateWindow : EditorWindow
    {
        static void ShowEditorErrorWindow(string errorString)
        {
            LoadResources();

            s_ErrorString = errorString;
            s_HasConnectionError = true;
            s_HasUpdate = false;

            ShowWindow();
        }

        static void ShowEditorUpdateWindow(string latestVersionString, string latestVersionMessage, string updateURL)
        {
            LoadResources();

            s_LatestVersionString = latestVersionString;
            s_LatestVersionMessage = latestVersionMessage;
            s_UpdateURL = updateURL;
            s_HasConnectionError = false;
            s_HasUpdate = updateURL.Length > 0;

            ShowWindow();
        }

        private static void ShowWindow()
        {
            EditorWindow.GetWindowWithRect(typeof(EditorUpdateWindow), new Rect(100, 100, 570, 400), true, s_Title.text);
        }

        private static GUIContent s_UnityLogo;
        private static GUIContent s_Title;
        private static GUIContent s_TextHasUpdate, s_TextUpToDate;
        private static GUIContent s_CheckForNewUpdatesText;

        private static string s_ErrorString;
        private static string s_LatestVersionString;
        private static string s_LatestVersionMessage;
        private static string s_UpdateURL;
        private static bool s_HasUpdate;
        private static bool s_HasConnectionError;
        private static bool s_ShowAtStartup;
        private Vector2 m_ScrollPos;

        private static void LoadResources()
        {
            if (s_UnityLogo != null)
                return;

            s_ShowAtStartup = EditorPrefs.GetBool("EditorUpdateShowAtStartup", true);

            s_Title = EditorGUIUtility.TextContent("Unity Editor Update Check");

            s_UnityLogo = EditorGUIUtility.IconContent("UnityLogo");
            s_TextHasUpdate = EditorGUIUtility.TextContent("There is a new version of the Unity Editor available for download.\n\nCurrently installed version is {0}\nNew version is {1}");
            s_TextUpToDate = EditorGUIUtility.TextContent("The Unity Editor is up to date. Currently installed version is {0}");

            s_CheckForNewUpdatesText = EditorGUIUtility.TextContent("Check for Updates");
        }

        public void OnGUI()
        {
            LoadResources();


            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUI.Box(new Rect(13, 8, s_UnityLogo.image.width, s_UnityLogo.image.height), s_UnityLogo, GUIStyle.none);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(120);
            GUILayout.BeginVertical();

            if (s_HasConnectionError)
            {
                GUILayout.Label(s_ErrorString, "WordWrappedLabel", GUILayout.Width(405));
            }
            else if (s_HasUpdate)
            {
                GUILayout.Label(string.Format(s_TextHasUpdate.text, InternalEditorUtility.GetFullUnityVersion(), s_LatestVersionString), "WordWrappedLabel", GUILayout.Width(300));

                GUILayout.Space(20);
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Width(405), GUILayout.Height(200));
                GUILayout.Label(s_LatestVersionMessage, "WordWrappedLabel");
                EditorGUILayout.EndScrollView();

                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Download new version", GUILayout.Width(200)))
                    Help.BrowseURL(s_UpdateURL);

                if (GUILayout.Button("Skip new version", GUILayout.Width(200)))
                {
                    EditorPrefs.SetString("EditorUpdateSkipVersionString", s_LatestVersionString);
                    Close();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(string.Format(s_TextUpToDate.text, Application.unityVersion), "WordWrappedLabel", GUILayout.Width(405));
            }


            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);


            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            s_ShowAtStartup = GUILayout.Toggle(s_ShowAtStartup, s_CheckForNewUpdatesText);
            if (GUI.changed)
                EditorPrefs.SetBool("EditorUpdateShowAtStartup", s_ShowAtStartup);

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
} // namespace
