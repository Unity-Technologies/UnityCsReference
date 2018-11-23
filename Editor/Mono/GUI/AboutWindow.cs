// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VisualStudioIntegration;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class AboutWindow : EditorWindow
    {
        static void ShowAboutWindow()
        {
            AboutWindow w = EditorWindow.GetWindowWithRect<AboutWindow>(new Rect(100, 100, 570, 340), true, "About Unity");
            w.position = new Rect(100, 100, 570, 340);
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        private static GUIContent s_MonoLogo, s_AgeiaLogo, s_Header;

        private const string kSpecialThanksNames = "Thanks to Forest 'Yoggy' Johnson, Graham McAllister, David Janik-Jones, Raimund Schumacher, Alan J. Dickins and Emil 'Humus' Persson";

        private static void LoadLogos()
        {
            if (s_MonoLogo != null)
                return;
            s_MonoLogo = EditorGUIUtility.IconContent("MonoLogo");
            s_AgeiaLogo = EditorGUIUtility.IconContent("AgeiaLogo");
            s_Header = EditorGUIUtility.IconContent("AboutWindow.MainHeader");
        }

        public void OnEnable()
        {
            EditorApplication.update += UpdateScroll;
            m_LastScrollUpdate = EditorApplication.timeSinceStartup;

            AboutWindowNames.ParseCreditsIfNecessary();
        }

        public void OnDisable()
        {
            EditorApplication.update -= UpdateScroll;
        }

        float m_TextYPos = 120;
        float m_TextInitialYPos = 120;
        float m_TotalCreditsHeight = Mathf.Infinity;

        double m_LastScrollUpdate = 0.0f;

        public void UpdateScroll()
        {
            double deltaTime = EditorApplication.timeSinceStartup - m_LastScrollUpdate;
            m_LastScrollUpdate = EditorApplication.timeSinceStartup;

            if (GUIUtility.hotControl != 0)
                return;

            m_TextYPos -= 40f * (float)deltaTime;
            if (m_TextYPos < -m_TotalCreditsHeight)
                m_TextYPos = m_TextInitialYPos;
            Repaint();
        }

        bool m_ShowDetailedVersion = false;
        public void OnGUI()
        {
            LoadLogos();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(s_Header, GUIStyle.none);

            ListenForSecretCodes();

            var licenseTypeString = "";
            if (InternalEditorUtility.HasFreeLicense())
                licenseTypeString = " Personal";
            if (InternalEditorUtility.HasEduLicense())
                licenseTypeString = " Edu";

            GUILayout.BeginHorizontal();
            GUILayout.Space(52f); // Ident version information

            string extensionVersion = FormatExtensionVersionString();

            m_ShowDetailedVersion |= Event.current.alt;
            if (m_ShowDetailedVersion)
            {
                int t = InternalEditorUtility.GetUnityVersionDate();
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                string branch = InternalEditorUtility.GetUnityBuildBranch();
                string branchString = "";
                if (branch.Length > 0)
                {
                    branchString = "Branch: " + branch;
                }
                EditorGUILayout.SelectableLabel(
                    string.Format("Version {0}{1}{2}\n{3:r}\n{4}", InternalEditorUtility.GetFullUnityVersion(), licenseTypeString, extensionVersion, dt.AddSeconds(t), branchString),
                    GUILayout.Width(550), GUILayout.Height(42));

                m_TextInitialYPos = 120 - 12;
            }
            else
            {
                GUILayout.Label(string.Format("Version {0}{1}{2}", Application.unityVersion, licenseTypeString, extensionVersion));
            }

            if (Event.current.type == EventType.ValidateCommand)
                return;

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            float creditsWidth = position.width - 10;
            float chunkOffset = m_TextYPos;

            Rect scrollAreaRect = GUILayoutUtility.GetRect(10, m_TextInitialYPos);
            GUI.BeginGroup(scrollAreaRect);
            foreach (string nameChunk in AboutWindowNames.Names(null, true))
                chunkOffset = DoCreditsNameChunk(nameChunk, creditsWidth, chunkOffset);
            chunkOffset = DoCreditsNameChunk(kSpecialThanksNames, creditsWidth, chunkOffset);
            m_TotalCreditsHeight = chunkOffset - m_TextYPos;
            GUI.EndGroup();

            HandleScrollEvents(scrollAreaRect);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.Label(s_MonoLogo);
            GUILayout.Label("Scripting powered by The Mono Project.\n\n(c) 2011 Novell, Inc.", "MiniLabel", GUILayout.Width(200));
            GUILayout.Label(s_AgeiaLogo);
            GUILayout.Label("Physics powered by PhysX.\n\n(c) 2011 NVIDIA Corporation.", "MiniLabel", GUILayout.Width(200));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            var VSTUlabel = UnityVSSupport.GetAboutWindowLabel();
            if (VSTUlabel.Length > 0)
                GUILayout.Label(VSTUlabel, "MiniLabel");
            GUILayout.Label(InternalEditorUtility.GetUnityCopyright(), "MiniLabel");
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(InternalEditorUtility.GetLicenseInfo(), "AboutWindowLicenseLabel");
            GUILayout.EndVertical();
            GUILayout.Space(5);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        private void HandleScrollEvents(Rect scrollAreaRect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);

            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (scrollAreaRect.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_TextYPos += Event.current.delta.y;
                        m_TextYPos = Mathf.Min(m_TextYPos, m_TextInitialYPos);
                        m_TextYPos = Mathf.Max(m_TextYPos, -m_TotalCreditsHeight);
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
            }
        }

        private static float DoCreditsNameChunk(string nameChunk, float creditsWidth, float creditsChunkYOffset)
        {
            float creditsNamesHeight = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(nameChunk), creditsWidth);
            Rect creditsNamesRect = new Rect(5, creditsChunkYOffset, creditsWidth, creditsNamesHeight);
            GUI.Label(creditsNamesRect, nameChunk, EditorStyles.wordWrappedLabel);
            return creditsNamesRect.yMax;
        }

        private int m_InternalCodeProgress;
        private void ListenForSecretCodes()
        {
            if (Event.current.type != EventType.KeyDown || (int)Event.current.character == 0)
                return;

            if (SecretCodeHasBeenTyped("internal", ref m_InternalCodeProgress))
            {
                bool enabled = !EditorPrefs.GetBool("InternalMode", false);
                EditorPrefs.SetBool("InternalMode", enabled);
                ShowNotification(new GUIContent("Internal Mode " + (enabled ? "On" : "Off")));
                InternalEditorUtility.RequestScriptReload();
            }
        }

        private bool SecretCodeHasBeenTyped(string code, ref int characterProgress)
        {
            if (characterProgress < 0 || characterProgress >= code.Length || code[characterProgress] != Event.current.character)
                characterProgress = 0;

            // Don't use else here. Even if key was mismatch, it should still be recognized as first key of sequence if it matches.
            if (code[characterProgress] == Event.current.character)
            {
                characterProgress++;

                if (characterProgress >= code.Length)
                {
                    characterProgress = 0;
                    return true;
                }
            }
            return false;
        }

        private string FormatExtensionVersionString()
        {
            string extStr = EditorUserBuildSettings.selectedBuildTargetGroup.ToString();
            string ext = Modules.ModuleManager.GetExtensionVersion(extStr);

            if (!string.IsNullOrEmpty(ext))
                return " [" + extStr + ": " + ext + "]";

            return "";
        }
    }
} // namespace
