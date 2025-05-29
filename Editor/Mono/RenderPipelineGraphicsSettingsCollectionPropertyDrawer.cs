// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.Categorization;

namespace UnityEditor.Rendering.GraphicsSettingsInspectors
{
    //internal for tests
    [CustomPropertyDrawer(typeof(RenderPipelineGraphicsSettingsCollection))]
    internal class RenderPipelineGraphicsSettingsCollectionPropertyDrawer : PropertyDrawer
    {
        const string k_LineClass = "contextual-menu-button--handler";
        const string k_GraphicsSettingsClass = "project-settings-section__graphics-settings";
        const string k_GraphicsSettingsHighlightableClass = "graphics-settings__highlightable";
        const string k_GraphicsSettingsContentFollowupClass = "project-settings-section__content-followup";

        internal struct SettingsInfo : ICategorizable
        {
            public SerializedProperty property { get; private set; }
            public Type type { get; private set; }
            public HelpURLAttribute helpURLAttribute { get; private set; }
            public bool onlyForDevMode { get; private set; }
            public IRenderPipelineGraphicsSettings target => property?.boxedValue as IRenderPipelineGraphicsSettings;
            
            public static SettingsInfo? ExtractFrom(SerializedProperty property)
            {
                //boxedProperty can be null if we keep in the list a data blob for a type that have disappears
                //this can happens if user remove a IRenderPipelineGraphicsSettings from his project for instance
                Type type = property?.boxedValue?.GetType();
                if (type == null || !typeof(IRenderPipelineGraphicsSettings).IsAssignableFrom(type))
                    return null;

                // If GraphicsSettings is hidden, discard it
                bool hidden = type.GetCustomAttribute<HideInInspector>() != null;
                if (!Unsupported.IsDeveloperMode() && hidden)
                    return null;

                return new SettingsInfo()
                {
                    property = property.Copy(),
                    type = type,
                    helpURLAttribute = type.GetCustomAttribute<HelpURLAttribute>(),
                    onlyForDevMode = hidden,
                };
            }
        }

        //internal for tests
        internal static List<Category<LeafElement<SettingsInfo>>> Categorize(SerializedProperty property)
        {
            List<SettingsInfo> elements = new();

            var propertyIterator = property.Copy();
            var end = propertyIterator.GetEndProperty();
            propertyIterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(propertyIterator, end))
            {
                var info = SettingsInfo.ExtractFrom(propertyIterator);
                if (info == null)
                {
                    propertyIterator.NextVisible(false);
                    continue; //remove array length and hidden properties
                }
                elements.Add(info.Value);
                propertyIterator.NextVisible(false);
            }

            return elements.SortByCategory();
        }

        void DrawHelpButton(VisualElement root, HelpURLAttribute helpURLAttribute)
        {
            if (helpURLAttribute?.URL != null)
            {
                var button = new Button(Background.FromTexture2D(EditorGUIUtility.LoadIcon("_Help")), () => Help.BrowseURL(helpURLAttribute.URL));
                root.Add(button);
            }
        }

        void ShowContextualMenu(Rect rect, List<LeafElement<SettingsInfo>> siblings)
        {
            List<(IRenderPipelineGraphicsSettings target, SerializedProperty property)> targets = new(siblings.Count);
            foreach (SettingsInfo sibling in siblings)
                targets.Add((sibling.target, sibling.property));

            var contextualMenu = new GenericMenu(); //use ImGUI for now, need to be updated later
            RenderPipelineGraphicsSettingsContextMenuManager.PopulateContextMenu(targets, ref contextualMenu);
            contextualMenu.DropDown(new Rect(rect.position + Vector2.up * rect.size.y, Vector2.zero), shouldDiscardMenuOnSecondClick: true);
        }

        void DrawContextualMenuButton(VisualElement root, LeafElement<SettingsInfo> settingsInfo)
        {
            var button = new Button(Background.FromTexture2D(EditorGUIUtility.LoadIcon("pane options")));
            button.clicked += () => ShowContextualMenu(button.worldBound, settingsInfo.parent.content);
            root.Add(button);
        }

        void DrawHeader(VisualElement root, Category<LeafElement<SettingsInfo>> category)
        {
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.AddToClassList(k_LineClass);

            var label = new Label(category.name);
            label.AddToClassList(ProjectSettingsSection.Styles.header);
            line.Add(label);

            root.Add(line);

            var firstSetting = category[0];
            DrawHelpButton(line, firstSetting.data.helpURLAttribute);
            DrawContextualMenuButton(line, firstSetting);
        }

        void DrawContent(VisualElement root, SettingsInfo settingsInfo, bool first)
        {
            var graphicsSettings = new PropertyField(settingsInfo.property)
            {
                name = settingsInfo.type.Name,
                classList =
                {
                    ProjectSettingsSection.Styles.content,
                    k_GraphicsSettingsClass,
                }
            };
            if (!first)
                graphicsSettings.classList.Add(k_GraphicsSettingsContentFollowupClass);
            root.Add(graphicsSettings);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement { name = "GlobalSettingsCollection" };
            var graphicsSettings = property.FindPropertyRelative("m_List");
            Debug.Assert(graphicsSettings != null);

            foreach (var category in Categorize(graphicsSettings))
            {
                DrawHeader(root, category);

                DrawContent(root, category[0], first: true);
                for (int i = 1; i < category.count; ++i)
                    DrawContent(root, category[i], first: false);
            }

            return root;
        }
    }
}
