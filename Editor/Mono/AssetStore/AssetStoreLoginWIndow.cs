// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    /**
     * Login dialog for the asset store.
     * This login session is shared with the asset store window ie.
     * login/logout here will do the same in the asset store window
     * and vice versa.
     */
    internal class AssetStoreLoginWindow : EditorWindow
    {
        class Styles
        {
            public GUIStyle link = new GUIStyle(EditorStyles.miniLabel);
            public Styles()
            {
                link.normal.textColor = new Color(.26f, .51f, .75f, 1f);
            }
        }
        static Styles styles;

        // errorMessage is null on success
        public delegate void LoginCallback(string errorMessage);

        /** Make a best effort login to the asset store by trying on order:
         * 1, reuse previously saved session
         * 2, show login window
         */
        static public void Login(string loginReason, LoginCallback callback)
        {
            if (AssetStoreClient.HasActiveSessionID)
                AssetStoreClient.Logout();

            // Show login window if we cannot use saved session ID
            if (!AssetStoreClient.RememberSession || !AssetStoreClient.HasSavedSessionID)
            {
                ShowAssetStoreLoginWindow(loginReason, callback);
                return;
            }

            AssetStoreClient.LoginWithRememberedSession(delegate(string errorMessage) {
                    if (string.IsNullOrEmpty(errorMessage))
                        callback(errorMessage);
                    else
                        ShowAssetStoreLoginWindow(loginReason, callback);
                });
        }

        /** Logout of the asset store
          */
        static public void Logout()
        {
            AssetStoreClient.Logout();
        }

        // Logout of the asset store
        static public bool IsLoggedIn
        {
            get { return AssetStoreClient.HasActiveSessionID; }
        }

        /** Show the login window */
        static public void ShowAssetStoreLoginWindow(string loginReason, LoginCallback callback)
        {
            AssetStoreLoginWindow w = EditorWindow.GetWindowWithRect<AssetStoreLoginWindow>(new Rect(100, 100, 360, 140), true, "Login to Asset Store");
            w.position = new Rect(100, 100, w.position.width, w.position.height);
            w.m_Parent.window.m_DontSaveToLayout = true;
            w.m_Password = "";
            w.m_LoginCallback = callback;
            w.m_LoginReason = loginReason;
            w.m_LoginRemoteMessage = null;
            UsabilityAnalytics.Track("/AssetStore/Login");
        }

        static GUIContent s_AssetStoreLogo;

        static void LoadLogos()
        {
            if (s_AssetStoreLogo != null)
                return;
            // s_AssetStoreLogo = EditorGUIUtility.IconContent ("WelcomeScreen.AssetStoreLogo");
            s_AssetStoreLogo = new GUIContent(""); // TODO: need to have logo created
        }

        const float kBaseHeight = 110.0f;
        string m_LoginReason;
        string m_LoginRemoteMessage = null;
        string m_Username = "";
        string m_Password = "";

        LoginCallback m_LoginCallback;

        public void OnDisable()
        {
            if (m_LoginCallback != null)
                m_LoginCallback(m_LoginRemoteMessage);
            m_LoginCallback = null;
            m_Password = null;
        }

        public void OnGUI()
        {
            if (styles == null)
                styles = new Styles();

            LoadLogos();

            if (AssetStoreClient.LoginInProgress() || AssetStoreClient.LoggedIn())
                GUI.enabled = false;

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(s_AssetStoreLogo, GUIStyle.none, GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label(m_LoginReason, EditorStyles.wordWrappedLabel);
            Rect lastReasonRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            Rect lastMessageRect = new Rect(0f, 0f, 0f, 0f);
            if (m_LoginRemoteMessage != null)
            {
                Color oldColor = GUI.color;
                GUI.color = Color.red;
                GUILayout.Label(m_LoginRemoteMessage, EditorStyles.wordWrappedLabel);
                GUI.color = oldColor;
                lastMessageRect = GUILayoutUtility.GetLastRect();
            }
            float newHeight = lastReasonRect.height + lastMessageRect.height + kBaseHeight;
            if (Event.current.type == EventType.Repaint && newHeight != position.height)
            {
                // Debug.Log(newHeight.ToString() + " " + position.height + " " + lastReasonRect.height.ToString() + " " + lastMessageRect.height.ToString());
                position = new Rect(position.x, position.y, position.width, newHeight);
                Repaint();
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("username");
            m_Username = EditorGUILayout.TextField("Username", m_Username);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_Password = EditorGUILayout.PasswordField("Password", m_Password, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(new GUIContent("Forgot?", "Reset your password"), styles.link, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("https://accounts.unity3d.com/password/new");
            }

            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.EndHorizontal();

            bool oldRememberMe = AssetStoreClient.RememberSession;
            bool newRememberMe = EditorGUILayout.Toggle("Remember me", oldRememberMe);
            if (newRememberMe != oldRememberMe)
                AssetStoreClient.RememberSession = newRememberMe;

            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create account"))
            {
                AssetStore.Open("createuser/");
                m_LoginRemoteMessage = "Cancelled - create user";
                Close();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel"))
            {
                m_LoginRemoteMessage = "Cancelled";
                Close();
            }
            GUILayout.Space(5);
            if (GUILayout.Button("Login"))
            {
                DoLogin();
                Repaint();
            }
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.EndVertical();

            if (Event.current.Equals(Event.KeyboardEvent("return")))
            {
                DoLogin();
                Repaint();
            }

            if (m_Username == "")
                EditorGUI.FocusTextInControl("username");
        }

        void DoLogin()
        {
            m_LoginRemoteMessage = null;

            if (AssetStoreClient.HasActiveSessionID)
                AssetStoreClient.Logout();

            AssetStoreClient.LoginWithCredentials(m_Username, m_Password, AssetStoreClient.RememberSession,
                delegate(string errorMessage) {
                    m_LoginRemoteMessage = errorMessage;
                    if (errorMessage == null)
                    {
                        Close();
                    }
                    else
                    {
                        Repaint();
                    }
                });
        }
    } // class AssetStoreLoginWindow
} // namespace UnityEditor
