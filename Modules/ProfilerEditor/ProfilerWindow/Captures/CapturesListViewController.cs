// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;
using Label = UnityEngine.UIElements.Label;

namespace Unity.Profiling.Editor.UI
{
    internal class CapturesListViewController : ViewController
    {
        const string k_UxmlResourceName = "CapturesListView.uxml";
        const string k_UssClass_Dark = "captures-list-view--dark";
        const string k_UssClass_Light = "captures-list-view--light";

        const string k_UxmlTreeItemGuid = "CaptureFilesListViewItem.uxml";

        const string k_UxmlSearchField = "captures-list-view__search-field";
        const string k_UxmlTreeView = "captures-list-view__tree-view";
        const string k_UxmlIdentifierImportButton = "captures-list-view__toolbar__import-button";

        const string k_TreePersistencyKey = "unity.profiling.editor.ui.capturefileslistviewcontroller.treeview";
        const string k_TreePersistencyItemIdsKey = "unity.profiling.editor.ui.capturefileslistviewcontroller.treeview.itemids";
        const string k_UxmlTreeViewItemSession = "profiler-captures__card__session-label";
        const string k_UxmlTreeViewItemCapture = "profiler-captures__card__captures-container";
        const string k_UxmlNoCapturesHint = "profiler-captures__no-captures-message";

        // Model.
        readonly ProfilerWindow m_ProfilerWindow;
        readonly CaptureDataService m_CaptureDataService;
        readonly ScreenshotsManager m_ScreenshotsManager;

        string m_CurrentSearchText;

        // View.
        ToolbarButton m_ImportButton;
        TreeView m_CapturesCollection;
        VisualElement m_NoCapturesMessage;
        Dictionary<int, CaptureFileTreeItemViewController> m_TreeViewControllers;
        ToolbarSearchField m_SearchField;

        readonly struct CaptureItemData
        {
            public CaptureItemData(string name, bool sessionGroup, CaptureFileModel fileData)
            {
                Name = name;
                SessionGroup = sessionGroup;
                FileData = fileData;
            }

            public string Name { get; }
            public bool SessionGroup { get; }
            public CaptureFileModel FileData { get; }
        }

        public CapturesListViewController(ProfilerWindow profilerWindow,
            CaptureDataService captureDataService, ScreenshotsManager screenshotsManager)
        {
            m_ProfilerWindow = profilerWindow;
            m_CaptureDataService = captureDataService;
            m_ScreenshotsManager = screenshotsManager;

            m_TreeViewControllers = new Dictionary<int, CaptureFileTreeItemViewController>();

            m_CaptureDataService.AllCapturesChanged += RefreshView;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_CaptureDataService.AllCapturesChanged -= RefreshView;
                foreach (var captureItemData in m_TreeViewControllers)
                    captureItemData.Value.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeUssClass = EditorGUIUtility.isProSkin ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_SearchField.RegisterValueChangedCallback(evt => FilterTreeView(evt.newValue));
            m_ImportButton.clicked += ImportCapture;

            RefreshView();
        }

        void FilterTreeView(string searchString)
        {
            m_CurrentSearchText = searchString;
            RefreshView();
        }

        public void RefreshView()
        {
            bool hasCaptures = m_CaptureDataService.AllCaptures.Count > 0;
            UIUtility.SetElementDisplay(m_CapturesCollection, hasCaptures);
            UIUtility.SetElementDisplay(m_NoCapturesMessage, !hasCaptures);

            RefreshTreeView();
        }

        void RefreshTreeView()
        {
            m_CapturesCollection.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_CapturesCollection.makeItem = MakeTreeItem;
            m_CapturesCollection.bindItem = BindTreeItem;
            m_CapturesCollection.unbindItem = UnbindTreeItem;

            var fullCaptureList = m_CaptureDataService.FullCaptureList;
            if (fullCaptureList.AllCaptures.Count == 0)
            {
                m_CapturesCollection.SetRootItems(new List<TreeViewItemData<CaptureItemData>>());
                return;
            }

            // Clear up items that have been removed from disk, or ones where
            // we need to rebuild because the file model has been changed.
            var itemsToClear = new List<int>();
            foreach (var keyValuePair in m_TreeViewControllers)
            {
                var found = false;
                foreach (var captureFileModel in fullCaptureList.AllCaptures)
                {
                    if (captureFileModel.Equals(keyValuePair.Value.Model))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                keyValuePair.Value.Dispose();
                itemsToClear.Add(keyValuePair.Key);
            }

            // Remove items from dictionary now we've finished iterating it
            foreach (var itemToClear in itemsToClear)
                m_TreeViewControllers.Remove(itemToClear);

            // Build tree
            var tree = new List<TreeViewItemData<CaptureItemData>>();
            var usedIds = new HashSet<int>();
            foreach (var sessionId in fullCaptureList.SortedSessionIds)
            {
                // add all session ids to usedIds so we can avoid duplicates in their children
                var sessionTreeItemId = (int)sessionId;
                usedIds.Add(sessionTreeItemId);
            }

            var oldItemEntries = new HashSet<int>(SessionState.GetIntArray(k_TreePersistencyItemIdsKey, new int[0]));
            var sessionsToExpand = new HashSet<int>();
            foreach (var sessionId in fullCaptureList.SortedSessionIds)
            {
                // Add all Captures with the same session id
                var children = fullCaptureList.SessionsMap[sessionId];
                var sessionTreeItemId = (int)sessionId;
                var childrenItems = new List<TreeViewItemData<CaptureItemData>>();
                foreach (var child in children)
                {
                    if (m_CurrentSearchText?.Length > 0 && !child.Name.Contains(m_CurrentSearchText, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    // generate a persistent tree item id based on the session ID and the Capture file name.
                    var captureTreeItemId = sessionTreeItemId + child.Name.GetHashCode();

                    // in case of CaptureId clashes, increment it until it is unique
                    while (usedIds.Contains(captureTreeItemId))
                        captureTreeItemId++;

                    usedIds.Add(captureTreeItemId);
                    childrenItems.Add(new TreeViewItemData<CaptureItemData>(captureTreeItemId, new CaptureItemData(child.Name, false, child)));

                    if (!oldItemEntries.Contains(captureTreeItemId))
                        // There's been an addition/name change in the Captures of this session, so we need to expand it to show that change
                        // There is a minor chance of false positives here when we have to increment the id more often than before due to clashes,
                        // but it's not a likely scenario, nor a big deal
                        sessionsToExpand.Add(sessionTreeItemId);
                }

                // Filtering from search might mean we have no results, so don't bother adding an empty group.
                if (childrenItems.Count == 0)
                    continue;

                // Generate session name
                var sessionName = String.Format($"{m_CaptureDataService.SessionNames[sessionId]}");// - {children.First().ProductName}");
                var groupItem = new TreeViewItemData<CaptureItemData>(sessionTreeItemId, new CaptureItemData(sessionName, true, null), childrenItems);
                tree.Add(groupItem);
            }

            var usedIdsArray = new int[usedIds.Count];
            usedIds.CopyTo(usedIdsArray, 0, usedIds.Count);
            SessionState.SetIntArray(k_TreePersistencyItemIdsKey, usedIdsArray);

            // Add a delayed execution here to avoid Windows scrolling to the wrong place: PROFB-333
            m_CapturesCollection.schedule.Execute(() =>
            {
                m_CapturesCollection.SetRootItems(tree);
                m_CapturesCollection.RefreshItems();

                if (sessionsToExpand.Count > 0)
                {
                    // Expand everything next frame, as otherwise
                    // TreeView might expand it wrongly
                    m_CapturesCollection.schedule.Execute(() =>
                    {
                        foreach (var id in sessionsToExpand)
                        {
                            m_CapturesCollection.ExpandItem(id);
                        }
                    });
                }
            });
        }

        void ImportCapture()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Import Profiler Capture",
                ProfilerUserSettings.LastImportPath, new[] { "Profiler Captures (.data, .raw)", "data,raw" });
            if (path.Length == 0)
                return;

            ProfilerUserSettings.LastImportPath = path;

            if (!m_CaptureDataService.Import(path))
                Debug.LogFormat($"{path} has already been imported or is locked.");
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_ImportButton = view.Q<ToolbarButton>(k_UxmlIdentifierImportButton);
            m_CapturesCollection = view.Q<TreeView>(k_UxmlTreeView);
            // Selection is not handled via the Tree View.
            // If a selection was made via the Tree View controls, e.g. by starting a click on a capture and ending it outside of it, clear it.
            // Without this, users could end up with a blue selection highlight on a capture that wasn't opened.
            m_CapturesCollection.selectedIndicesChanged += (a) => m_CapturesCollection.ClearSelection();
            m_CapturesCollection.viewDataKey = k_TreePersistencyKey;
            m_NoCapturesMessage = view.Q(k_UxmlNoCapturesHint);
            m_SearchField = view.Q<ToolbarSearchField>(k_UxmlSearchField);
        }

        VisualElement MakeTreeItem()
        {
            return ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlTreeItemGuid);
        }

        void BindTreeItem(VisualElement element, int index)
        {
            var itemData = m_CapturesCollection.GetItemDataForIndex<CaptureItemData>(index);

            var sessionCard = element.Q<Label>(k_UxmlTreeViewItemSession);
            var fileDataCard = element.Q(k_UxmlTreeViewItemCapture);
            UIUtility.SetElementDisplay(sessionCard, itemData.SessionGroup);
            UIUtility.SetElementDisplay(fileDataCard, !itemData.SessionGroup);
            if (!itemData.SessionGroup)
            {
                var itemId = m_CapturesCollection.GetIdForIndex(index);

                if (!m_TreeViewControllers.TryGetValue(itemId, out var viewController))
                    viewController = new CaptureFileTreeItemViewController(itemData.FileData, m_CaptureDataService, m_ScreenshotsManager, m_ProfilerWindow);

                viewController.IsLoaded = m_ProfilerWindow.CaptureFileIsOpen(itemData.FileData.FullPath);

                fileDataCard.Add(viewController.View);
                AddChild(viewController);
                m_TreeViewControllers[itemId] = viewController;
            }
            else
                sessionCard.text = itemData.Name;
        }

        void UnbindTreeItem(VisualElement element, int index)
        {
            var itemData = m_CapturesCollection.GetItemDataForIndex<CaptureItemData>(index);
            if (itemData.SessionGroup)
                return;

            var fileDataCard = element.Q(k_UxmlTreeViewItemCapture);
            fileDataCard.Clear();

            var itemId = m_CapturesCollection.GetIdForIndex(index);

            if (!m_TreeViewControllers.TryGetValue(itemId, out var viewController))
                return;

            RemoveChild(viewController);
        }
    }
}
