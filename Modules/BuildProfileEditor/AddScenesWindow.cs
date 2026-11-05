// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Picker listing the scene assets of the project that are not in a scene list yet,
    /// so that several of them can be added at once.
    /// </summary>
    internal class AddScenesWindow : EditorWindow
    {
        class SceneEntry
        {
            public GUID guid;
            public string name;
            public string path;
        }

        const string k_SceneAssetFilter = "t:SceneAsset";
        const string k_WindowUssClass = "add-scenes-window";
        const string k_SearchBarUssClass = "add-scenes-search-bar";
        const string k_HeaderUssClass = "add-scenes-header";
        const string k_ItemIconUssClass = "add-scenes-item-icon";
        const string k_FooterButtonUssClass = "add-scenes-footer-button";
        const float k_MinWidth = 280f;
        const float k_Height = 320f;

        Action<IList<GUID>> m_OnScenesSelected;
        HashSet<GUID> m_ExcludedScenes;
        List<SceneEntry> m_Scenes;
        List<SceneEntry> m_VisibleScenes;
        ListView m_SceneListView;
        Button m_AddButton;

        internal static void ShowWindow(VisualElement anchor, HashSet<GUID> excludedScenes, Action<IList<GUID>> onScenesSelected)
        {
            var window = CreateInstance<AddScenesWindow>();
            window.m_ExcludedScenes = excludedScenes;
            window.m_OnScenesSelected = onScenesSelected;

            var anchorRect = GUIUtility.GUIToScreenRect(anchor.worldBound);
            window.ShowAsDropDown(anchorRect, new Vector2(Mathf.Max(anchorRect.width, k_MinWidth), k_Height));
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        }

        void CreateGUI()
        {
            BuildSceneEntries();
            bool hasScenes = m_Scenes.Count > 0;

            var root = rootVisualElement;
            root.styleSheets.Add(EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet);
            root.AddToClassList(k_WindowUssClass);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.AddToClassList(k_SearchBarUssClass);
            var searchField = new ToolbarSearchField();
            searchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));
            searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown, TrickleDown.TrickleDown);
            toolbar.Add(searchField);
            root.Add(toolbar);

            var header = new Label(TrText.scenes);
            header.AddToClassList(k_HeaderUssClass);
            root.Add(header);

            m_SceneListView = new ListView
            {
                itemsSource = m_VisibleScenes,
                selectionType = SelectionType.Multiple,
                makeItem = MakeSceneItem,
                bindItem = BindSceneItem,
                makeNoneElement = MakeNoMatchElement
            };
            m_SceneListView.AddToClassList("flex-grow-1");
            m_SceneListView.AddToClassList("flex-basis-0");
            m_SceneListView.selectionChanged += _ => UpdateAddButtonState();
            m_SceneListView.itemsChosen += _ => AddSelectedScenes();
            root.Add(m_SceneListView);

            if (!hasScenes)
            {
                toolbar.Hide();
                m_SceneListView.Hide();

                var emptyState = new VisualElement();
                emptyState.AddToClassList("flex-grow-1");
                emptyState.AddToClassList("px-small");
                emptyState.AddToClassList("py-small");
                emptyState.Add(new HelpBox(TrText.noScenesToAdd, HelpBoxMessageType.Info));
                root.Add(emptyState);
            }

            var footer = new VisualElement();
            footer.AddToClassList("flex-row");
            footer.AddToClassList("flex-shrink-0");
            footer.AddToClassList("flex-align-right");
            footer.AddToClassList("px-small");
            footer.AddToClassList("py-small");

            var closeButton = new Button(Close) { text = hasScenes ? TrText.cancelButtonText : TrText.close };
            closeButton.AddToClassList(k_FooterButtonUssClass);
            footer.Add(closeButton);

            m_AddButton = new Button(AddSelectedScenes) { text = TrText.add };
            m_AddButton.AddToClassList(k_FooterButtonUssClass);
            m_AddButton.AddToClassList("ml-medium");
            if (!hasScenes)
                m_AddButton.Hide();
            footer.Add(m_AddButton);
            root.Add(footer);

            UpdateAddButtonState();

            searchField.schedule.Execute(() => searchField.Q<TextField>()?.Q("unity-text-input")?.Focus());
        }

        static VisualElement MakeSceneItem()
        {
            var item = new VisualElement();
            item.AddToClassList("flex-row");
            item.AddToClassList("align-items-center");

            var icon = new Image();
            icon.AddToClassList(k_ItemIconUssClass);
            item.Add(icon);

            var label = new Label { displayTooltipWhenElided = false };
            label.AddToClassList("text-overflow-ellipsis");
            item.Add(label);

            return item;
        }

        void BindSceneItem(VisualElement element, int index)
        {
            var scene = m_VisibleScenes[index];
            element.Q<Image>().image = AssetDatabase.GetCachedIcon(scene.path);
            element.Q<Label>().text = scene.name;
            element.tooltip = scene.path;
        }

        static VisualElement MakeNoMatchElement()
        {
            var label = new Label(TrText.noScenesMatchSearch);
            label.AddToClassList(BaseListView.emptyLabelUssClassName);
            return label;
        }

        void OnSearchFieldKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    if (m_VisibleScenes.Count == 0)
                        return;

                    if (m_SceneListView.selectedIndex < 0)
                        m_SceneListView.SetSelection(0);

                    m_SceneListView.Focus();
                    evt.StopPropagation();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    AddSelectedScenes();
                    evt.StopPropagation();
                    break;
            }
        }

        void BuildSceneEntries()
        {
            m_Scenes = new List<SceneEntry>();
            foreach (var guidString in AssetDatabase.FindAssets(k_SceneAssetFilter))
            {
                if (!GUID.TryParse(guidString, out var guid) || m_ExcludedScenes.Contains(guid))
                    continue;

                var path = AssetDatabase.GUIDToAssetPath(guidString);
                if (string.IsNullOrEmpty(path))
                    continue;

                m_Scenes.Add(new SceneEntry { guid = guid, name = Path.GetFileNameWithoutExtension(path), path = path });
            }

            m_Scenes.Sort((a, b) =>
            {
                int result = string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                return result != 0 ? result : string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase);
            });
            m_VisibleScenes = new List<SceneEntry>(m_Scenes);
        }

        void ApplyFilter(string query)
        {
            m_VisibleScenes.Clear();
            foreach (var scene in m_Scenes)
            {
                if (string.IsNullOrEmpty(query) || scene.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    m_VisibleScenes.Add(scene);
            }

            m_SceneListView.ClearSelection();
            m_SceneListView.Rebuild();

            if (!string.IsNullOrEmpty(query) && m_VisibleScenes.Count > 0)
            {
                m_SceneListView.SetSelection(0);
                m_SceneListView.ScrollToItem(0);
            }

            UpdateAddButtonState();
        }

        void AddSelectedScenes()
        {
            var selectedIndices = new List<int>(m_SceneListView.selectedIndices);
            if (selectedIndices.Count == 0)
                return;

            selectedIndices.Sort();

            var guids = new List<GUID>(selectedIndices.Count);
            foreach (var index in selectedIndices)
                guids.Add(m_VisibleScenes[index].guid);

            m_OnScenesSelected?.Invoke(guids);
            Close();
        }

        void UpdateAddButtonState()
        {
            m_AddButton?.SetEnabled(m_SceneListView.selectedIndex >= 0);
        }
    }
}
