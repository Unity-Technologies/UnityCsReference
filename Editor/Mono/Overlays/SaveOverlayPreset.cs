// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Overlays
{
    [EditorWindowTitle(title = "Save Window Preset")]
    sealed class SaveOverlayPreset : EditorWindow
    {
        bool m_DidFocus;
        const int k_Width = 200;
        const int k_Height = 48;
        const int k_HelpBoxHeight = 40;

        static readonly string k_InvalidChars = EditorUtility.GetInvalidFilenameChars();
        static readonly string s_InvalidCharsFormatString = L10n.Tr("Invalid characters: {0}");
        string m_CurrentInvalidChars = "";

        string m_PresetName = "Default";
        EditorWindow m_Window;

        Action<string> save;
        internal static SaveOverlayPreset ShowWindow(EditorWindow window, Action<string> callback)
        {
            SaveOverlayPreset w = GetWindowDontShow<SaveOverlayPreset>();
            w.m_Window = window;
            w.save = callback;
            w.m_PresetName = window.overlayCanvas.lastAppliedPresetName;
            if (string.IsNullOrEmpty(w.m_PresetName))
                w.m_PresetName = "Default";
            w.minSize = w.maxSize = new Vector2(k_Width, k_Height);
            w.ShowAuxWindow();
            return w;
        }

        void UpdateCurrentInvalidChars()
        {
            m_CurrentInvalidChars = new string(m_PresetName.Intersect(k_InvalidChars).Distinct().ToArray());
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            bool hitEscape = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Escape);
            if (hitEscape)
            {
                Close();
                GUIUtility.ExitGUI();
            }
            GUI.SetNextControlName("m_PreferencesName");
            EditorGUI.BeginChangeCheck();
            m_PresetName = EditorGUILayout.TextField(m_PresetName);
            m_PresetName = m_PresetName.TrimEnd();
            if (EditorGUI.EndChangeCheck())
            {
                UpdateCurrentInvalidChars();
            }

            if (!m_DidFocus)
            {
                m_DidFocus = true;
                EditorGUI.FocusTextInControl("m_PreferencesName");
            }

            if (m_CurrentInvalidChars.Length != 0)
            {
                EditorGUILayout.HelpBox(string.Format(s_InvalidCharsFormatString, m_CurrentInvalidChars), MessageType.Warning);
                minSize = new Vector2(k_Width, k_Height + k_HelpBoxHeight);
            }
            else
            {
                minSize = new Vector2(k_Width, k_Height);
            }

            bool canSaveLayout = m_PresetName.Length > 0 && m_CurrentInvalidChars.Length == 0;
            EditorGUI.BeginDisabled(!canSaveLayout);

            if (GUILayout.Button("Save") || hitEnter && canSaveLayout)
            {
                Close();

                if (OverlayPresetManager.Exists(m_Window.GetType(), m_PresetName))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite preset?",
                        "Do you want to overwrite '" + m_PresetName + "' preset?",
                        "Overwrite", "Cancel"))
                        GUIUtility.ExitGUI();
                }

                save(m_PresetName);

                GUIUtility.ExitGUI();
            }
            else
            {
                m_DidFocus = false;
            }

            EditorGUI.EndDisabled();
        }
    }
}
