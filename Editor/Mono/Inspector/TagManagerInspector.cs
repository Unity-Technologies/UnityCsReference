// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Debug = System.Diagnostics.Debug;

namespace UnityEditor
{
    [CustomEditor(typeof(TagManager))]
    internal class TagManagerInspector : ProjectSettingsBaseEditor
    {
        const string k_ProjectPath = "Project/Tags and Layers";
        const string k_AssetPath = "ProjectSettings/TagManager.asset";

        const string k_BodyTemplate = "UXML/ProjectSettings/TagManagerInspector-Body.uxml";
        const string k_ProjectSettingsStyleSheet = "StyleSheets/ProjectSettings/ProjectSettingsCommon.uss";
        internal override string targetTitle => "Tags & Layers";

        bool isEditable => AssetDatabase.IsOpenForEdit(k_AssetPath, StatusQueryOptions.UseCachedIfPossible);

        static InitialExpansionState s_InitialExpansionState = InitialExpansionState.None;

        internal enum InitialExpansionState
        {
            None = 0,
            Tags = 1,
            Layers = 2,
            SortingLayers = 3,
            RenderingLayers = 4
        }

        internal class Styles
        {
            public static GUIContent tags = EditorGUIUtility.TrTextContent("Tags");
            public static GUIContent sortingLayers = EditorGUIUtility.TrTextContent("Sorting Layers");
            public static GUIContent layers = EditorGUIUtility.TrTextContent("Layers");
            public static GUIContent renderingLayers = EditorGUIUtility.TrTextContent("Rendering Layers");

            public static float elementHeight = EditorGUIUtility.singleLineHeight + 2;
            public const float headerListHeight = 3;
        }

        public TagManager tagManager => target as TagManager;

        internal static void ShowWithInitialExpansion(InitialExpansionState initialExpansionState)
        {
            s_InitialExpansionState = initialExpansionState;
            Selection.activeObject = EditorApplication.tagManager;
        }

        #region Sorting Layers

        bool CanEditSortLayerEntry(int index)
        {
            if (index < 0 || index >= tagManager.GetSortingLayerCount())
                return false;
            return !tagManager.IsSortingLayerDefault(index);
        }

        #endregion

        public override VisualElement CreateInspectorGUI()
        {
            var visualTreeAsset = EditorGUIUtility.Load(k_BodyTemplate) as VisualTreeAsset;
            var content = visualTreeAsset.Instantiate();

            var tagsProperty = serializedObject.FindProperty("tags");
            var sortingLayersProperty = serializedObject.FindProperty("m_SortingLayers");
            var layersProperty = serializedObject.FindProperty("layers");
            var renderingLayersProperty = serializedObject.FindProperty("m_RenderingLayers");

            Debug.Assert(layersProperty.arraySize == 32);
            Debug.Assert(renderingLayersProperty.arraySize == 32);

            var tagsList = SetupTags(content, tagsProperty);
            var sortingLayers = SetupSortingLayers(content, sortingLayersProperty);
            var layers = SetupLayers(content, layersProperty);
            var renderingLayers = SetupRenderingLayers(content, renderingLayersProperty);

            if (s_InitialExpansionState != InitialExpansionState.None)
            {
                tagsProperty.isExpanded = false;
                sortingLayersProperty.isExpanded = false;
                layersProperty.isExpanded = false;
                renderingLayersProperty.isExpanded = false;
                switch (s_InitialExpansionState)
                {
                    case InitialExpansionState.Tags:
                        tagsProperty.isExpanded = true;
                        tagsList.Q<Foldout>().value = true;
                        break;
                    case InitialExpansionState.SortingLayers:
                        sortingLayersProperty.isExpanded = true;
                        sortingLayers.Q<Foldout>().value = true;
                        break;
                    case InitialExpansionState.Layers:
                        layersProperty.isExpanded = true;
                        layers.Q<Foldout>().value = true;
                        break;
                    case InitialExpansionState.RenderingLayers:
                        renderingLayersProperty.isExpanded = true;
                        renderingLayers.Q<Foldout>().value = true;
                        break;
                }

                s_InitialExpansionState = InitialExpansionState.None;
            }

            content.Bind(serializedObject);
            return content;
        }

        VisualElement SetupTags(VisualElement content, SerializedProperty tagsProperty)
        {
            var tagsList = content.Q<ListView>("Tags");
            tagsList.fixedItemHeight = Styles.elementHeight;
            tagsList.headerTitle = Styles.tags.text;
            tagsList.makeItem = () =>
            {
                var tagListElement = new VisualElement { classList = { "tag-list__element" } };
                tagListElement.Add(new Label()
                {
                    name = "Title",
                    classList = { "tag-list__element__title" }
                });
                tagListElement.Add(new Label
                {
                    name = "Value"
                });
                return tagListElement;
            };
            tagsList.bindItem = (ve, index) =>
            {
                ve.Q<Label>("Title").text = $"Tag {index}";
                ve.Q<Label>("Value").text = index < tagsProperty.arraySize
                    ? tagsProperty.GetArrayElementAtIndex(index).stringValue
                    : "Unknown";
            };
            tagsList.onAdd += (listView) =>
            {
                var addButton = listView.Q<Button>("unity-list-view__add-button");
                PopupWindow.Show(addButton.worldBound, new EnterTagNamePopup(tagsProperty, s =>
                {
                    tagManager.AddTag(s);
                    serializedObject.ApplyModifiedProperties();
                }));
            };
            tagsList.onRemove += (listView) =>
            {
                if (tagsProperty.arraySize == 0)
                    return;

                var indexForRemoval = listView.selectedIndex;
                if (indexForRemoval == -1)
                    indexForRemoval = tagsProperty.arraySize - 1;

                var tag = tagsProperty.GetArrayElementAtIndex(indexForRemoval).stringValue;
                if (string.IsNullOrEmpty(tag))
                    return;

                var isPreset = Preset.IsEditorTargetAPreset(target);
                if (!isPreset)
                {
                    var go = GameObject.FindWithTag(tag);
                    if (go != null)
                    {
                        EditorUtility.DisplayDialog("Error", "Can't remove this tag because it is being used by " + go.name, "OK");
                        return;
                    }
                }

                tagManager.RemoveTag(tag);
                serializedObject.ApplyModifiedProperties();
            };
            //TextFields in Array are not bind correctly so we need to refresh them manually
            content.TrackPropertyValue(tagsProperty, sp => tagsList.RefreshItems());
            return tagsList;
        }

        VisualElement SetupSortingLayers(VisualElement content, SerializedProperty sortingLayersProperty)
        {
            var sortingLayers = content.Q<ListView>("SortingLayers");
            sortingLayers.fixedItemHeight = Styles.elementHeight;
            sortingLayers.headerTitle = Styles.sortingLayers.text;
            sortingLayers.makeItem = () => new TextField();

            void SortingLayersChanged(ChangeEvent<string> evt)
            {
                if (evt.target is not TextField textField)
                    return;
                evt.StopPropagation();
                var index = (int)textField.userData;
                tagManager.SetSortingLayerName(index, evt.newValue);
                serializedObject.ApplyModifiedProperties();
            }

            sortingLayers.bindItem = (ve, index) =>
            {
                var textField = ve as TextField;
                textField.label = $"Layer {index}";
                textField.SetValueWithoutNotify(tagManager.GetSortingLayerName(index));

                var isEnable = isEditable && CanEditSortLayerEntry(index);
                textField.SetEnabled(isEnable);
                textField.userData = index;

                textField.RegisterValueChangedCallback(SortingLayersChanged);
            };
            sortingLayers.unbindItem = (ve, index) =>
            {
                var textField = ve as TextField;
                textField.UnregisterValueChangedCallback(SortingLayersChanged);
            };
            sortingLayers.itemIndexChanged += (prev, next) =>
            {
                serializedObject.ApplyModifiedProperties();
                tagManager.UpdateSortingLayersOrder();
            };
            sortingLayers.onAdd += (listView) =>
            {
                serializedObject.ApplyModifiedProperties();
                tagManager.AddSortingLayer();
                serializedObject.Update();

                listView.selectedIndex = tagManager.GetSortingLayerCount() - 1; // select just added one

                if (SortingLayer.onLayerAdded != null)
                    SortingLayer.onLayerAdded(SortingLayer.layers[listView.selectedIndex]);
            };
            sortingLayers.onRemove += (listView) =>
            {
                if (tagManager.GetSortingLayerCount() == 0 || !CanEditSortLayerEntry(listView.selectedIndex))
                    return;

                if (SortingLayer.onLayerRemoved != null)
                    SortingLayer.onLayerRemoved(SortingLayer.layers[listView.selectedIndex]);

                listView.viewController.RemoveItem(listView.selectedIndex);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                tagManager.UpdateSortingLayersOrder();
            };
            //TextFields in Array are not bind correctly so we need to refresh them manually
            content.TrackPropertyValue(sortingLayersProperty, sp => sortingLayers.RefreshItems());
            return sortingLayers;
        }

        VisualElement SetupLayers(VisualElement content, SerializedProperty layersProperty)
        {
            var layers = content.Q<ListView>("Layers");
            layers.fixedItemHeight = Styles.elementHeight;
            layers.headerTitle = Styles.layers.text;
            layers.makeItem = () => new TextField();

            void LayersChanged(ChangeEvent<string> evt)
            {
                if (evt.target is not TextField textField)
                    return;
                evt.StopPropagation();
                var index = (int)textField.userData;
                layersProperty.GetArrayElementAtIndex(index).stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            }

            layers.bindItem = (ve, index) =>
            {
                // Layers up to 8 used to be reserved for Builtin Layers
                // As layers with indices 3, 6 and 7 were empty,
                // it was decided to change them to User Layers
                // However, we cannot shift layers around so we need to explicitly handle
                // the gap where layer index == 3 in the layer stack
                var isUserLayer = index is > 5 or 3;
                var editable = isEditable && isUserLayer;

                var textField = ve as TextField;
                var layerName = layersProperty.GetArrayElementAtIndex(index).stringValue;

                textField.label = isUserLayer ? $" User Layer {index}" : $" Builtin Layer {index}";
                textField.SetValueWithoutNotify(layerName);
                textField.SetEnabled(editable);
                textField.userData = index;

                textField.RegisterValueChangedCallback(LayersChanged);
            };
            layers.unbindItem = (ve, index) =>
            {
                var textField = ve as TextField;
                textField.UnregisterValueChangedCallback(LayersChanged);
            };
            //TextFields in Array are not bind correctly so we need to refresh them manually
            content.TrackPropertyValue(layersProperty, sp => layers.RefreshItems());
            return layers;
        }

        VisualElement SetupRenderingLayers(VisualElement content, SerializedProperty renderingLayersProperty)
        {
            var renderingLayers = content.Q<ListView>("RenderingLayers");
            renderingLayers.fixedItemHeight = Styles.elementHeight;
            renderingLayers.headerTitle = Styles.renderingLayers.text;
            renderingLayers.makeItem = () => new TextField();

            void RenderingLayersChanged(ChangeEvent<string> evt)
            {
                if (evt.target is not TextField textField)
                    return;
                evt.StopPropagation();
                var index = (int)textField.userData;
                renderingLayersProperty.GetArrayElementAtIndex(index).stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            }

            renderingLayers.bindItem = (ve, index) =>
            {
                var textField = ve as TextField;
                textField.label = $" Rendering Layer {index}";
                textField.SetValueWithoutNotify(tagManager.RenderingLayerToString(index));

                var isEnabled = isEditable && !tagManager.IsIndexReservedForDefaultRenderingLayer(index);
                textField.SetEnabled(isEnabled);
                textField.userData = index;

                textField.RegisterValueChangedCallback(RenderingLayersChanged);
            };
            renderingLayers.unbindItem = (ve, index) =>
            {
                var textField = ve as TextField;
                textField.UnregisterValueChangedCallback(RenderingLayersChanged);
            };
            //TextFields in Array are not bind correctly so we need to refresh them manually
            content.TrackPropertyValue(renderingLayersProperty, sp => renderingLayers.RefreshItems());
            return renderingLayers;
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(k_ProjectPath, k_AssetPath, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
            provider.activateHandler = (text, root) =>
            {
                var serializedObject = provider.settingsEditor.serializedObject;
                var titleBar = new ProjectSettingsTitleBar("Tags and Layers");
                titleBar.Initialize(serializedObject);

                var styleSheet = EditorGUIUtility.Load(k_ProjectSettingsStyleSheet) as StyleSheet;
                root.styleSheets.Add(styleSheet);

                root.Add(titleBar);
                root.Add(provider.settingsEditor.CreateInspectorGUI());
            };
            return provider;
        }

        class EnterTagNamePopup : PopupWindowContent
        {
            public delegate void EnterDelegate(string str);

            readonly EnterDelegate m_EnterCallback;
            string m_NewTagName = "New tag";
            bool m_NeedsFocus = true;

            public EnterTagNamePopup(SerializedProperty tags, EnterDelegate callback)
            {
                m_EnterCallback = callback;

                var existingTagNames = new List<string>();
                for (var i = 0; i < tags.arraySize; i++)
                {
                    var tagName = tags.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(tagName))
                        existingTagNames.Add(tagName);
                }

                m_NewTagName = ObjectNames.GetUniqueName(existingTagNames.ToArray(), m_NewTagName);
            }

            public override Vector2 GetWindowSize() =>
                new(400, EditorGUI.kSingleLineHeight * 2 + EditorGUI.kControlVerticalSpacing + 14);

            public override void OnGUI(Rect windowRect)
            {
                GUILayout.Space(5);
                var evt = Event.current;
                var hitEnter = evt.type == EventType.KeyDown && evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter;
                GUI.SetNextControlName("TagName");
                m_NewTagName = EditorGUILayout.TextField("New Tag Name", m_NewTagName);

                if (m_NeedsFocus)
                {
                    m_NeedsFocus = false;
                    EditorGUI.FocusTextInControl("TagName");
                }

                GUI.enabled = m_NewTagName.Length != 0;
                var savePressed = GUILayout.Button("Save");
                if (string.IsNullOrWhiteSpace(m_NewTagName) || (!savePressed && !hitEnter))
                    return;
                m_EnterCallback(m_NewTagName);
                editorWindow.Close();
            }
        }
    }
}
