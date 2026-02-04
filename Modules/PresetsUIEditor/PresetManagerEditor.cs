// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Presets
{
    [CustomEditor(typeof(PresetManager))]
    internal sealed class PresetManagerEditor : ProjectSettingsBaseEditor
    {
        const string k_ProjectSettingsStyleSheet = "StyleSheets/ProjectSettings/ProjectSettingsCommon.uss";
        const string k_view = "UXML/ProjectSettings/PresetManagerSettingsView.uxml";
        const string k_PresetPerTypeList = "UXML/ProjectSettings/PresetPerTypeList.uxml";
        const string k_PresetPerTypeListItem = "UXML/ProjectSettings/PresetPerTypeListItem.uxml";
        internal override string targetTitle => "Preset Manager";
        class Content
        {
            public static GUIContent presetManager = EditorGUIUtility.TrTextContent("Preset management");
        }

        struct DefaultPresetListData
        {
            public PresetType presetType;
            public SerializedProperty defaultPresets;
            public string className;
        }

        string m_Search = string.Empty;
        SerializedProperty m_DefaultPresets;
        List<VisualElement> m_Defaults;
        VisualElement m_RootVisualElement;
        VisualTreeAsset m_presetPerTypeListVisualAsset;
        VisualTreeAsset m_presetPerTypeListItemVisualAsset;
        int m_LastDefaultPresetsArraySize = -1;

        static string ClassName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "Unsupported Type";
            int lastDot = fullTypeName.LastIndexOf(".");
            if (lastDot == -1)
                return fullTypeName;
            return fullTypeName.Substring(lastDot + 1);
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_DefaultPresets = serializedObject.FindProperty("m_DefaultPresets");

            m_RootVisualElement = new VisualElement();
            VisualTreeAsset presetManagerSettingsView = EditorGUIUtility.Load(k_view) as VisualTreeAsset;
            presetManagerSettingsView.CloneTree(m_RootVisualElement);

            m_presetPerTypeListVisualAsset = EditorGUIUtility.Load(k_PresetPerTypeList) as VisualTreeAsset;
            m_presetPerTypeListItemVisualAsset = EditorGUIUtility.Load(k_PresetPerTypeListItem) as VisualTreeAsset;
            CreateDefaultPresetsLists();

            var searchField = m_RootVisualElement.Q<ToolbarSearchField>("searchField");
            searchField.value = m_Search;
            searchField.RegisterValueChangedCallback(evt =>
            {
                m_Search = evt.newValue;
                RefreshDefaultPresetVisibility();
            });

            var addDefaultButton = m_RootVisualElement.Q<UnityEngine.UIElements.Button>("addDefaultButton");
            addDefaultButton.clicked += () =>
            {
                AddPresetTypeWindow.Show(addDefaultButton.worldBound, OnPresetTypeWindowSelection, string.IsNullOrEmpty(m_Search) ? null : m_Search);
            };
            
            return m_RootVisualElement;
        }

        void OnEnable()
        {
            Undo.undoRedoEvent += OnUndoRedo;
            EditorApplication.update += CheckSerializedObjectChanged;
        }

        void OnDisable()
        {
            EditorApplication.update -= CheckSerializedObjectChanged;
            Undo.undoRedoEvent -= OnUndoRedo;
        }
        void OnDestroy()
        {
            EditorApplication.update -= CheckSerializedObjectChanged;
            Undo.undoRedoEvent -= OnUndoRedo;
        }

        private void CreateDefaultPresetsLists()
        {
            var presetPerTypeListContainer = m_RootVisualElement.Q<VisualElement>("presetPerTypeListContainer");
            presetPerTypeListContainer.Clear();

            m_LastDefaultPresetsArraySize = m_DefaultPresets.arraySize;
            var defaultPresetList = new List<DefaultPresetListData>(m_DefaultPresets.arraySize);
            for (int i = 0; i < m_DefaultPresets.arraySize; ++i)
            {
                SerializedProperty defaultPresetListElement = m_DefaultPresets.GetArrayElementAtIndex(i);
                var presetType = new PresetType(defaultPresetListElement.FindPropertyRelative("first"));
                defaultPresetList.Add( new DefaultPresetListData
                {
                    presetType = presetType,
                    defaultPresets = defaultPresetListElement.FindPropertyRelative("second"),
                    className = ClassName(presetType.GetManagedTypeName())
                });
            }
            defaultPresetList.Sort((a, b) => a.className.CompareTo(b.className));

            m_Defaults = new List<VisualElement>(defaultPresetList.Count);
            for (int i = 0; i < defaultPresetList.Count; ++i)
            {
                var data = defaultPresetList[i];
                var listContainer = new VisualElement();
                m_presetPerTypeListVisualAsset.CloneTree(listContainer);
                listContainer.name = data.className;
                presetPerTypeListContainer.Add(listContainer);
                listContainer.Q<Label>("filterLabel").text = EditorGUIUtility.TrTextContent("Filter").text;
                listContainer.Q<Label>("classNameLabel").text = data.className;
                listContainer.Q<Label>("fullNameLabel").text = $"({data.presetType.GetManagedTypeName()})";
                listContainer.Q<VisualElement>("icon").style.backgroundImage = data.presetType.GetIcon();
               
                var listView = listContainer.Q<ListView>("defaultPresetList");
                listView.userData = data.presetType;
                

                listView.makeItem = () =>
                {
                    var container = new VisualElement();
                    m_presetPerTypeListItemVisualAsset.CloneTree(container);
                    return container;
                };

                listView.bindItem = (element, index) =>
                {
                    if (!data.defaultPresets.isValid)
                        return;
                    var toggle = element.Q<Toggle>("enabledToggle");
                    var presetProperty = data.defaultPresets.GetArrayElementAtIndex(index);
                    var boolProp = presetProperty.FindPropertyRelative("m_Disabled");

                    EventCallback<ChangeEvent<bool>> toggleCallback = evt =>
                    {
                        Undo.RecordObjects(targets, "Toggle Preset Enabled");
                        boolProp.boolValue = !evt.newValue;
                        serializedObject.ApplyModifiedProperties();
                    };
                    
                    toggle.RegisterValueChangedCallback(toggleCallback);
                    toggle.userData = toggleCallback;

                    toggle.SetValueWithoutNotify(!boolProp.boolValue);

                    toggle.SetProperty("serialized-property", boolProp);

                    var filterField = element.Q<TextField>("filterField");
                    filterField.BindProperty(presetProperty.FindPropertyRelative("m_Filter"));

                    var presetField = element.Q<UIElements.ObjectField>("presetField");
                    
                    var display = presetField.Query<VisualElement>().Class("unity-object-field-display").First();
                    presetField.objectType = typeof(Preset);
                    presetField.BindProperty(presetProperty.FindPropertyRelative("m_Preset"));

                    EventCallback<DragPerformEvent> dragPerformCallback = evt =>
                    {
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
#pragma warning restore UA2001
                        if (draggedObject is Preset preset && preset.GetPresetType() == data.presetType)
                        {
                            presetField.value = preset;
                        }
                        DragAndDrop.AcceptDrag();
                        if (display != null)
                            display.RemoveFromClassList("unity-object-field-display--accept-drop");
                        evt.StopPropagation();
                    };
                    presetField.RegisterCallback(dragPerformCallback, TrickleDown.TrickleDown);
                    presetField.SetProperty("drag-perform", dragPerformCallback);

                    EventCallback<DragUpdatedEvent> dragUpdatedCallback = evt =>
                    {
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
#pragma warning restore UA2001
                        if (draggedObject is Preset preset && preset.GetPresetType() == data.presetType)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (display != null)
                                display.AddToClassList("unity-object-field-display--accept-drop");
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            if (display != null)
                                display.RemoveFromClassList("unity-object-field-display--accept-drop");
                        }
                        evt.StopPropagation();
                    };
                    presetField.RegisterCallback(dragUpdatedCallback, TrickleDown.TrickleDown);
                    presetField.SetProperty("drag-updated", dragUpdatedCallback);

                    EventCallback<MouseDownEvent> mouseDownCallback = evt =>
                    {
                        if (evt.button == 0 && evt.target is VisualElement ve && ve.fullTypeName == "UnityEditor.UIElements.ObjectField+ObjectFieldSelector")
                        {
                            var objectField = (UIElements.ObjectField)ve.parent.parent;
                            var presetObject = (Preset)objectField.value;
                            var presetType = (PresetType)listView.userData;
                            var property = serializedObject.FindProperty(objectField.bindingPath);
                            var presetContext = new PresetContext(presetType, presetObject, property, false);
                            PresetSelector.ShowSelector(presetContext);
                            presetContext.OnSelectionChanged = (x) => { objectField.value = x; };
                            evt.StopPropagation();
                        }
                    };
                    presetField.RegisterCallback(mouseDownCallback, TrickleDown.TrickleDown);
                    presetField.SetProperty("mouse-down", mouseDownCallback);
                };
                
                listView.unbindItem = (element, index) =>
                {
                    var toggle = element.Q<Toggle>("enabledToggle");
                    if (toggle.userData is EventCallback<ChangeEvent<bool>> toggleCallback)
                    {
                        toggle.UnregisterValueChangedCallback(toggleCallback);
                        toggle.userData = null;
                    }
                    
                    toggle.SetProperty("serialized-property", null);
                    
                    var presetField = element.Q<UIElements.ObjectField>("presetField");
                    if (presetField != null)
                    {
                        var dragPerformCallback = presetField.GetProperty("drag-perform") as EventCallback<DragPerformEvent>;
                        if (dragPerformCallback != null)
                            presetField.UnregisterCallback(dragPerformCallback, TrickleDown.TrickleDown);

                        var dragUpdatedCallback = presetField.GetProperty("drag-updated") as EventCallback<DragUpdatedEvent>;
                        if (dragUpdatedCallback != null)
                            presetField.UnregisterCallback(dragUpdatedCallback, TrickleDown.TrickleDown);

                        var mouseDownCallback = presetField.GetProperty("mouse-down") as EventCallback<MouseDownEvent>;
                        if (mouseDownCallback != null)
                            presetField.UnregisterCallback(mouseDownCallback, TrickleDown.TrickleDown);

                        presetField.SetProperty("drag-perform", null);
                        presetField.SetProperty("drag-updated", null);
                        presetField.SetProperty("mouse-down", null);
                        presetField.Unbind();
                    }

                    var filterField = element.Q<TextField>("filterField");
                    if (filterField != null)
                        filterField.Unbind();
                };
                
                m_Defaults.Add(listContainer);
                listView.BindProperty(data.defaultPresets);
            }
            RefreshDefaultPresetVisibility();
        }

        private void RefreshDefaultPresetVisibility()
        {
            foreach (var defaultPreset in m_Defaults)
            {
                defaultPreset.style.display = defaultPreset.name.ToLower().Contains(m_Search.ToLower()) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void OnPresetTypeWindowSelection(PresetType type)
        {
            Undo.RecordObjects(targets, "Preset Manager");
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var manager in targets.Cast<PresetManager>())
#pragma warning restore UA2001
            {
                manager.AddPresetType(type);
            }
            serializedObject.Update();
            CreateDefaultPresetsLists();
            Undo.FlushUndoRecordObjects();
        }

        [SettingsProvider]
        static SettingsProvider CreatePresetManagerProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Preset Manager", "ProjectSettings/PresetManager.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>());
            provider.activateHandler = (text, root) =>
            {
                var serializedObject = provider.settingsEditor.serializedObject;
                var titleBar = new ProjectSettingsTitleBar("Preset Manager");
                titleBar.Initialize(serializedObject);

                var styleSheet = EditorGUIUtility.Load(k_ProjectSettingsStyleSheet) as StyleSheet;
                root.styleSheets.Add(styleSheet);

                root.Add(titleBar);
                root.Add(provider.settingsEditor.CreateInspectorGUI());
            };
            return provider;
        }

        private void CheckSerializedObjectChanged()
        {
            if (serializedObject.isValid)
            {
                serializedObject.Update();
                if (m_DefaultPresets.isValid && m_LastDefaultPresetsArraySize != m_DefaultPresets.arraySize)
                {
                    CreateDefaultPresetsLists();
                }    
            }
        }

        private void OnUndoRedo(in UndoRedoInfo undoInfo)
        {
            if (serializedObject.isValid)
            {
                serializedObject.Update();
                RefreshToggleValues();
            }
        }

        private void RefreshToggleValues()
        {
            var toggles = m_RootVisualElement.Query<Toggle>("enabledToggle").ToList();
            foreach (var toggle in toggles)
            {
                var property = toggle.GetProperty("serialized-property") as SerializedProperty;
                if (property != null && property.isValid)
                {
                    toggle.SetValueWithoutNotify(!property.boolValue);
                }
            }
        }
    }
}
