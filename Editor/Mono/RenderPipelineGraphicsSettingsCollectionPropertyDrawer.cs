// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.GraphicsSettingsInspectors
{
    //internal for tests
    [CustomPropertyDrawer(typeof(RenderPipelineGraphicsSettingsCollection))]
    internal class RenderPipelineGraphicsSettingsCollectionPropertyDrawer : PropertyDrawer
    {
        const string k_LineClass = "contextual-menu-button--handler";
        const string k_GraphicsSettingsClass = "project-settings-section__graphics-settings";
        const string k_GraphicsSettingsHighlightableClass = "graphics-settings__highlightable";

        internal class GraphicsSettingsDrawerInfo
        {
            public SerializedProperty property { get; private set; }
            public HelpURLAttribute helpURLAttribute { get; private set; }
            public string category { get; private set; }
            public CategoryInfo parentCategory { get; internal set; }
            public string name { get; private set; }
            public bool onlyForDevMode { get; private set; }
            public IRenderPipelineGraphicsSettings target => property?.boxedValue as IRenderPipelineGraphicsSettings;

            private GraphicsSettingsDrawerInfo() { }

            public static GraphicsSettingsDrawerInfo ExtractFrom(SerializedProperty property)
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

                string name = null;
                string category = type.GetCustomAttribute<CategoryAttribute>()?.Category;
                int pos = category?.IndexOf('/') ?? -1;
                if (!string.IsNullOrEmpty(category) && pos >= 0)
                {
                    //before first '/': we use it as the cathegory
                    //everything after first '/': we use it as the name
                    name = category.Substring(pos + 1);
                    category = category.Substring(0, pos);
                }

                name ??= ObjectNames.NicifyVariableName(type.Name);
                category ??= name;


                return new GraphicsSettingsDrawerInfo()
                {
                    property = property.Copy(),
                    helpURLAttribute = type.GetCustomAttribute<HelpURLAttribute>(),
                    category = category,
                    parentCategory = null,
                    name = name,
                    onlyForDevMode = hidden
                };
            }
        }

        //internal for tests
        internal class CategoryInfo : IEnumerable<GraphicsSettingsDrawerInfo>
        {
            SortedDictionary<string, GraphicsSettingsDrawerInfo> settingsInfos;

            public int count => settingsInfos.Count;

            public GraphicsSettingsDrawerInfo first
            {
                get
                {
                    var e = settingsInfos.GetEnumerator();
                    e.MoveNext();
                    return e.Current.Value;
                }
            }

            public string category => first.category;

            private CategoryInfo()
            {
            }

            public bool Contains(GraphicsSettingsDrawerInfo settingsInfo)
                => settingsInfos.ContainsKey(settingsInfo.name);

            public void Add(GraphicsSettingsDrawerInfo settingsInfo)
            {
                settingsInfo.parentCategory = this;
                settingsInfos.Add(settingsInfo.name, settingsInfo);
            }

            public static CategoryInfo ExtractFrom(GraphicsSettingsDrawerInfo property)
            {
                var categoryInfo = new CategoryInfo()
                {
                    settingsInfos = new() { { property.name, property } }
                };
                property.parentCategory = categoryInfo;
                return categoryInfo;
            }

            IEnumerator<GraphicsSettingsDrawerInfo> IEnumerable<GraphicsSettingsDrawerInfo>.GetEnumerator()
                => settingsInfos.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => (this as IEnumerable<GraphicsSettingsDrawerInfo>).GetEnumerator();
        }

        //internal for tests
        internal static SortedDictionary<string, CategoryInfo> Categorize(SerializedProperty property)
        {
            SortedDictionary<string, CategoryInfo> categories = new();

            var propertyIterator = property.Copy();
            var end = propertyIterator.GetEndProperty();
            propertyIterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(propertyIterator, end))
            {
                var info = GraphicsSettingsDrawerInfo.ExtractFrom(propertyIterator);
                if (info == null)
                {
                    propertyIterator.NextVisible(false);
                    continue; //remove array length property
                }

                //sort per type in category
                if (categories.TryGetValue(info.category, out var categoryInfo))
                {
                    if (categoryInfo.Contains(info))
                        Debug.LogWarning($"{nameof(IRenderPipelineGraphicsSettings)} {info.name} is duplicated. Only showing first one.");
                    else
                        categoryInfo.Add(info);

                    propertyIterator.NextVisible(false);
                    continue;
                }

                //sort per category
                categories.Add(info.category, CategoryInfo.ExtractFrom(info));

                propertyIterator.NextVisible(false);
            }

            return categories;
        }

        void DrawHelpButton(VisualElement root, HelpURLAttribute helpURLAttribute)
        {
            if (helpURLAttribute?.URL != null)
            {
                var button = new Button(Background.FromTexture2D(EditorGUIUtility.LoadIcon("_Help")), () => Help.BrowseURL(helpURLAttribute.URL));
                root.Add(button);
            }
        }
        
        void ShowContextualMenu(Rect rect, IRenderPipelineGraphicsSettings target, SerializedProperty property)
        {
            var contextualMenu = new GenericMenu(); //use ImGUI for now, need to be updated later
            RenderPipelineGraphicsSettingsContextMenuManager.PopulateContextMenu(target, property, ref contextualMenu);
            contextualMenu.DropDown(new Rect(rect.position + Vector2.up * rect.size.y, Vector2.zero), shouldDiscardMenuOnSecondClick: true);
        }
        
        void DrawContextualMenuButton(VisualElement root, GraphicsSettingsDrawerInfo drawerInfo)
        {
            var button = new Button(Background.FromTexture2D(EditorGUIUtility.LoadIcon("pane options")));
            button.clicked += () => ShowContextualMenu(button.worldBound, drawerInfo.target, drawerInfo.property);
            root.Add(button);
        }

        void DrawHeader(VisualElement root, CategoryInfo categoryInfo)
        {
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.AddToClassList(k_LineClass);

            var label = new Label(categoryInfo.category);
            label.AddToClassList(ProjectSettingsSection.Styles.header);
            line.Add(label);

            root.Add(line);

            if (categoryInfo.count > 1)
                return;

            var onlySettings = categoryInfo.first;
            DrawHelpButton(line, onlySettings.helpURLAttribute);
            DrawContextualMenuButton(line, onlySettings);
        }

        void DrawSubheader(VisualElement root, GraphicsSettingsDrawerInfo settingsInfo)
        {
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.AddToClassList(k_LineClass);

            var label = new Label(settingsInfo.name);
            label.AddToClassList(ProjectSettingsSection.Styles.subheader);
            line.Add(label);

            DrawHelpButton(line, settingsInfo.helpURLAttribute);
            DrawContextualMenuButton(line, settingsInfo);
            
            root.Add(line);
        }

        void DrawContent(VisualElement root, GraphicsSettingsDrawerInfo settingsInfo)
        {
            var graphicsSettings = new PropertyField(settingsInfo.property)
            {
                name = settingsInfo.name,
                classList =
                {
                    ProjectSettingsSection.Styles.content,
                    k_GraphicsSettingsClass,
                    k_GraphicsSettingsHighlightableClass,
                }
            };
            root.Add(graphicsSettings);

            if (settingsInfo.onlyForDevMode)
                graphicsSettings.RegisterCallback<GeometryChangedEvent>(UpdatePropertyLabel);
        }

        static void UpdatePropertyLabel(GeometryChangedEvent evt)
        {
            var propertyField = (PropertyField)evt.target;
            propertyField.Query<Label>(className: "unity-property-field__label")
                .ForEach(l =>
                {
                    l.enableRichText = true;
                    l.text = $"{l.text} <b>(DevMode Only)</b>";
                    l.tooltip = "Field is only visible in developer mode";
                });

            propertyField.UnregisterCallback<GeometryChangedEvent>(UpdatePropertyLabel);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement { name = "GlobalSettingsCollection" };
            var graphicsSettings = property.FindPropertyRelative("m_List");
            Debug.Assert(graphicsSettings != null);

            foreach (var categoryInfo in Categorize(graphicsSettings).Values)
            {
                DrawHeader(root, categoryInfo);
                foreach (var graphicSettingsInfo in categoryInfo)
                {
                    if (categoryInfo.count > 1)
                        DrawSubheader(root, graphicSettingsInfo);
                    DrawContent(root, graphicSettingsInfo);
                }
            }

            return root;
        }
    }
}
