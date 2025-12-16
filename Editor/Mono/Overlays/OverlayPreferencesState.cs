// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEditor.Overlays;

namespace UnityEditor
{
    internal class OverlayPreferenceState : ScriptableObject
    {
        public class SettingsContent
        {
            public static readonly GUIContent OverlayBackgroundColorContent =
                EditorGUIUtility.TrTextContent("Overlay Background");
        }

        public void HandleUI(string searchContext)
        {
            foreach (var windowType in OverlayPrefs.GetSupportedWindowTypes())
            {
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(windowType.Name), EditorStyles.boldLabel);

                Color currentColor = OverlayPrefs.GetBackgroundColor(windowType);
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUILayout.ColorField(SettingsContent.OverlayBackgroundColorContent, currentColor);
                if (EditorGUI.EndChangeCheck())
                {
                    OverlayPrefs.SetBackgroundColor(windowType, newColor);
                }

                if (GUILayout.Button("Use Default", GUILayout.Width(120)))
                {
                    OverlayPrefs.RevertToDefaultColor(windowType);
                    OverlayPrefs.DeleteOverlayKey(windowType);
                }

                EditorGUILayout.Space();
            }
        }
    }

    class OverlayPreferencesProvider : SettingsProvider
    {
        OverlayPreferenceState m_PreferenceState;

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            return new OverlayPreferencesProvider();
        }

        public OverlayPreferencesProvider()
            : base("Preferences/Overlays", SettingsScope.User,
                GetSearchKeywordsFromGUIContentProperties<OverlayPreferenceState.SettingsContent>())
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_PreferenceState = ScriptableObject.CreateInstance<OverlayPreferenceState>();

            guiHandler = search =>
            {
                m_PreferenceState.HandleUI(search);
            };

            base.OnActivate(searchContext, rootElement);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            UnityEngine.Object.DestroyImmediate(m_PreferenceState);
        }
    }
}

