// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Scripting;

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
        void RefreshMouse();
        void RefreshCategoryList();
        void RefreshShortcutList();
        void RefreshProfiles();
        void UpdateSearchFilterOptions();

        RebindResolution HandleRebindWillCreateConflict(ShortcutEntry entry, IList<KeyCombination> newBinding, IList<ShortcutEntry> conflicts);
    }

    enum BindingState
    {
        NotBound = 0,
        BoundGlobally = 1,
        BoundToContext = 2,
        BoundMixed = 3
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
        string CanCreateProfile(string newProfileId);
        void CreateProfile(string newProfileId);
        bool CanRenameActiveProfile();
        string CanRenameActiveProfile(string newProfileId);
        void RenameActiveProfile(string newProfileId);
        bool CanDeleteActiveProfile();
        void DeleteActiveProfile();
        void ResetToDefault(ShortcutEntry entry);
        void RemoveBinding(ShortcutEntry entry);
        IList<string> GetCategories();
        int categorySeparatorIndex { get; }

        bool CanImportProfile(string path, bool letUserDecide = true);
        void ImportProfile(string path);
        bool CanExportProfile();
        void ExportProfile(string path);

        bool IsCategorySelected(string category);
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

        void RequestRebindOfSelectedEntry(List<KeyCombination> newBinding);

        void BindSelectedEntryTo(List<KeyCombination> newbinding);
        IList<ShortcutEntry> GetSelectedEntryConflictsForGivenKeyCombination(List<KeyCombination> temporaryCombination);
        IList<ShortcutEntry> GetShortcutsBoundTo(KeyCode keyCode, EventModifiers modifiers);
        bool IsEntryPartOfConflict(ShortcutEntry shortcutEntry);
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
        ShortcutManagerWindowView m_View;
        ShortcutManagerWindowViewController m_ViewController;

        static readonly Vector2 k_MinSizeToShowKeyboard = new Vector2(850, 400);

        [RequiredByNativeCode]
        static void Open() => GetWindow<ShortcutManagerWindow>();

        void OnEnable()
        {
            var rootElement = rootVisualElement;
            //Workaround for the rootVisualContainer not having a height set on AuxWindows:
            rootElement.StretchToParentSize();

            titleContent = new GUIContent("Shortcuts");
            minSize = k_MinSizeToShowKeyboard;
            maxSize = new Vector2(10000, 10000);

            var directory = ShortcutIntegration.instance.directory;
            var contextManager = ShortcutIntegration.instance.contextManager;
            var profileManager = ShortcutIntegration.instance.profileManager;
            var bindingValidator = ShortcutIntegration.instance.bindingValidator;
            m_ViewController = new ShortcutManagerWindowViewController(m_State, directory, bindingValidator, profileManager, contextManager, ShortcutIntegration.instance);
            m_View = new ShortcutManagerWindowView(m_ViewController, m_ViewController);
            m_ViewController.SetView(m_View);

            var root = m_View.GetVisualElementHierarchyRoot();
            rootElement.Add(root);

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

        internal override void OnResized()
        {
            if (m_View.deviceContainer is null)
                return;

            var displayKeyboard = position.width >= k_MinSizeToShowKeyboard.x &&
                                  position.height >= k_MinSizeToShowKeyboard.y
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            SetKeyboardDisplay(displayKeyboard);
        }

        // Used by tests
        internal void SetKeyboardDisplay(DisplayStyle style)
        {
            m_View.SetKeyboardDisplay(style);
        }

        public static void ShowConflicts()
        {
            var win = GetWindowDontShow<ShortcutManagerWindow>();
            win.m_ViewController.SelectConflictCategory();
            win.ShowUtility();
        }
    }
}
