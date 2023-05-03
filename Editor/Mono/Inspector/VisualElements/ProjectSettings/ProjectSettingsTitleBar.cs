// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class ProjectSettingsTitleBar : ProjectSettingsElementWithSO
    {
        public new class UxmlFactory : UxmlFactory<ProjectSettingsTitleBar, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlStringAttributeDescription m_Label = new() { name = "label" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ProjectSettingsTitleBar)ve).label = m_Label.GetValueFromBag(bag, cc);
            }
        }

        internal class Styles
        {
            public static StyleBlock settingsBtn { get; } = EditorResources.GetStyle("sb-settings-icon-btn");

            public const string k_TitleBarClassName = "project-settings-title-bar";
            public const string k_TitleLabelClassName = "project-settings-title-label";
        }

        string m_Label;

        public string label
        {
            get => m_Label;
            set
            {
                m_Label = value;
                m_LabelElement.text = m_Label;
            }
        }

        readonly Label m_LabelElement;
        Object[] m_TargetObjects;

        public ProjectSettingsTitleBar()
        {
            AddToClassList(Styles.k_TitleBarClassName);

            m_LabelElement = new Label();
            m_LabelElement.AddToClassList(Styles.k_TitleLabelClassName);
            Add(m_LabelElement);
            Add(new IMGUIContainer(DrawEditorHeaderItems));
        }

        protected override void Initialize()
        {
            m_TargetObjects = m_SerializedObject.targetObjects;
        }

        void DrawEditorHeaderItems()
        {
            GUILayout.BeginHorizontal();
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
