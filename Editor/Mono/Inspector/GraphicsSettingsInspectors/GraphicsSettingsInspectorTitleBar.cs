// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class GraphicsSettingsInspectorTitleBar : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorTitleBar, UxmlTraits> { }

        internal class Styles
        {
            public static Lazy<GUIStyle> ImguiStylesHeader = new(() => "SettingsHeader");
            public static StyleBlock settingsPanel { get; } = EditorResources.GetStyle("sb-settings-panel-client-area");

            public static StyleBlock header { get; } =  EditorResources.GetStyle("sb-settings-header");

            public static StyleBlock settingsBtn { get; } =  EditorResources.GetStyle("sb-settings-icon-btn");

            public static readonly GUIContent mainHeader =  EditorGUIUtility.TrTextContent("Graphics");
        }

        Object[] m_TargetObjects;

        protected override void Initialize()
        {
            m_TargetObjects = m_SerializedObject.targetObjects;
            Add(new IMGUIContainer(Draw));
        }

        void Draw()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            GUILayout.BeginHorizontal();
            GUILayout.Space(Styles.settingsPanel.GetFloat(StyleCatalogKeyword.marginLeft));
            GUILayout.Label(Styles.mainHeader, Styles.ImguiStylesHeader.Value, GUILayout.MaxHeight(Styles.header.GetFloat("max-height")),
                GUILayout.MinWidth(160));
            GUILayout.FlexibleSpace();

            var btnWidth = Styles.settingsBtn.GetFloat(StyleCatalogKeyword.width);
            var btnHeight = Styles.settingsBtn.GetFloat(StyleCatalogKeyword.height);
            var btnMargin = Styles.settingsBtn.GetFloat(StyleCatalogKeyword.marginTop);

            var currentRect = GUILayoutUtility.GetRect(btnWidth, btnHeight);
            currentRect.y = btnMargin;
            EditorGUIUtility.DrawEditorHeaderItems(currentRect, m_TargetObjects);
            var settingsRect = GUILayoutUtility.GetRect(btnWidth, btnHeight);
            settingsRect.y = currentRect.y;

            // Settings; process event even for disabled UI
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var showMenu = EditorGUI.DropdownButton(settingsRect, GUIContent.none, FocusType.Passive, EditorStyles.optionsButtonStyle);
            GUI.enabled = wasEnabled;
            if (showMenu)
                EditorUtility.DisplayObjectContextMenu(settingsRect, m_TargetObjects, 0);
            GUILayout.EndHorizontal();
        }
    }
}
