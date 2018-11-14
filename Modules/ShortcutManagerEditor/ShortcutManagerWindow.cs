// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.ShortcutManagement
{
    enum RebindResolution
    {
        DoNotRebind,
        CreateConflict,
        UnassignExistingAndBind
    }

    interface IShortcutManagerWindowView
    {
        VisualElement GetVisualElementHierarchyRoot();
        void RefreshAll();
        void RefreshKeyboard();
        void RefreshCategoryList();
        void RefreshShortcutList();
        void UpdateSearchFilterOptions();

        RebindResolution HandleRebindWillCreateConflict(ShortcutEntry entry, IList<KeyCombination> newBinding, IList<ShortcutEntry> conflicts);
    }

    enum BindingState
    {
        NotBound = 0,
        BoundGlobally = 1,
        BoundToContext = 2
    }

    enum SearchOption
    {
        Name,
        Binding
    }

    interface IShortcutManagerWindowViewController
    {
        List<string> GetAvailableProfiles();
        string activeProfile
        {
            get; set;
        }
        bool CanCreateProfile(string newProfileId);
        void CreateProfile(string newProfileId);
        bool CanRenameActiveProfile();
        bool CanRenameActiveProfile(string newProfileId);
        void RenameActiveProfile(string newProfileId);
        bool CanDeleteActiveProfile();
        void DeleteActiveProfile();

        IList<string> GetCategories();

        void SetCategorySelected(string category);
        int selectedCategoryIndex
        {
            get;
        }

        SearchOption searchMode
        {
            get; set;
        }
        void SetSearch(string newSearch);
        string GetSearch();
        List<KeyCombination> GetBindingSearch();
        void SetBindingSearch(List<KeyCombination> newBindingSearch);

        bool ShouldShowSearchFilters();
        void GetSearchFilters(List<string> filters);
        string GetSelectedSearchFilter();
        void SetSelectedSearchFilter(string filter);


        IList<ShortcutEntry> GetShortcutList();
        IList<string> GetShortcutPathList();
        ShortcutEntry selectedEntry
        {
            get;
        }
        int selectedEntryIndex
        {
            get;
        }
        void ShortcutEntrySelected(ShortcutEntry shortcutEntry);

        void DragEntryAndDropIntoKey(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry);
        bool CanEntryBeAssignedToKey(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry);

        void SetKeySelected(KeyCode keyCode, EventModifiers eventModifier);
        KeyCode GetSelectedKey();
        EventModifiers GetSelectedEventModifiers();

        IList<ShortcutEntry> GetSelectedKeyShortcuts();
        int selectedKeyDetail
        {
            get;
        }

        void NavigateTo(ShortcutEntry shortcutEntry);

        void RequestRebindOfSelectedEntry(List<KeyCombination> newbinding);

        void BindSelectedEntryTo(List<KeyCombination> newbinding);
        IList<ShortcutEntry> GetSelectedEntryConflictsForGivenKeyCombination(List<KeyCombination> temporaryCombination);
        IList<ShortcutEntry> GetShortcutsBoundTo(KeyCode keyCode, EventModifiers modifiers);
    }

    interface IKeyBindingStateProvider
    {
        BindingState GetBindingStateForKeyWithModifiers(KeyCode keyCode, EventModifiers modifiers);
        bool CanBeSelected(KeyCode code);

        bool IsReservedKey(KeyCode code);
        bool IsModifier(KeyCode code);
        EventModifiers ModifierFromKeyCode(KeyCode k);
    }

    [Serializable]
    class SerializedShortcutManagerWindowState
    {
        [SerializeField]
        internal string selectedCategory;
        [SerializeField]
        internal SearchOption searchMode;
        [SerializeField]
        internal string search;
        [SerializeField]
        internal List<KeyCombination> bindingsSearch = new List<KeyCombination>();
        [SerializeField]
        internal SearchCategoryFilter searchCategoryFilter = SearchCategoryFilter.SearchWithinSelectedCategory;
        [SerializeField]
        internal KeyCode selectedKey;
        [SerializeField]
        internal EventModifiers selectedModifiers;
        [SerializeField]
        internal Identifier selectedEntryIdentifier;
    }

    enum SearchCategoryFilter
    {
        IgnoreCategory,
        SearchWithinSelectedCategory
    }

    class ShortcutManagerWindow : EditorWindow
    {
        SerializedShortcutManagerWindowState m_State = new SerializedShortcutManagerWindowState();
        ShortcutManagerWindowViewController m_ViewController;

        [MenuItem("Edit/Shortcuts...", false, 261)]
        static void Open()
        {
            var win = GetWindowDontShow<ShortcutManagerWindow>();
            win.ShowUtility();
        }

        void OnEnable()
        {
            //Workaround for the rootVisualContainer not having a height set on AuxWindows:
            rootVisualContainer.style.positionTop = 0;
            rootVisualContainer.style.positionBottom = 0;
            rootVisualContainer.style.positionLeft = 0;
            rootVisualContainer.style.positionRight = 0;
            rootVisualContainer.style.positionType = PositionType.Absolute;

            titleContent = new GUIContent("Shortcut Manager");
            minSize = new Vector2(740, 700);
            maxSize = new Vector2(740, 10000);

            var directory = ShortcutIntegration.instance.directory;
            var contextManager = ShortcutIntegration.instance.contextManager;
            var profileManager = ShortcutIntegration.instance.profileManager;
            var bindingValidator = ShortcutIntegration.instance.bindingValidator;
            m_ViewController = new ShortcutManagerWindowViewController(m_State, directory, bindingValidator, profileManager, contextManager);
            var view = new ShortcutManagerWindowView(m_ViewController, m_ViewController);
            m_ViewController.SetView(view);

            var root = view.GetVisualElementHierarchyRoot();
            rootVisualContainer.Add(root);

            m_ViewController.OnEnable();
            EditorApplication.modifierKeysChanged += OnModifiersMightHaveChanged;
        }

        //On Windows, shift key down does not send its own KeyDown event, so we need to listen to this event to workaround that.
        void OnModifiersMightHaveChanged()
        {
            if (focusedWindow == this)
                SendEvent(EditorGUIUtility.CommandEvent(EventCommandNames.ModifierKeysChanged));
        }

        void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= OnModifiersMightHaveChanged;
            m_ViewController.OnDisable();
        }

        public static void ShowConflicts()
        {
            var win = GetWindowDontShow<ShortcutManagerWindow>();
            win.m_ViewController.SelectConflictCategory();
            win.ShowUtility();
        }
    }
}
