// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutManagerWindowViewController : IShortcutManagerWindowViewController, IKeyBindingStateProvider
    {
        readonly string m_AllUnityCommands = L10n.Tr("All Unity Commands");
        readonly string m_CommandsWithConflicts = L10n.Tr("Binding Conflicts");
        readonly string m_MainMenu = L10n.Tr("Main Menu");

        readonly int m_AllUntiyCommandsIndex = 0;
        readonly int m_ConflictsIndex = 1;
        readonly int m_MainMenuIndex = 2;

        SerializedShortcutManagerWindowState m_SerializedState;
        IShortcutProfileManager m_ShortcutProfileManager;
        IDirectory m_Directory;
        IContextManager m_ContextManager;
        IShortcutManagerWindowView m_ShortcutManagerWindowView;
        IBindingValidator m_BindingValidator;

        List<ShortcutEntry> m_AllEntries = new List<ShortcutEntry>();
        Dictionary<string, List<ShortcutEntry>> m_CategoryToEntriesList = new Dictionary<string, List<ShortcutEntry>>();
        Dictionary<KeyCombination, List<ShortcutEntry>> m_KeyBindingRootToBoundEntries;

        List<string> m_Categories = new List<string>();
        int m_SelectedCategoryIndex;

        List<ShortcutEntry> m_SelectedCategoryShortcutList = new List<ShortcutEntry>();
        List<string> m_SelectedCategoryShortcutPathList = new List<string>();
        ShortcutEntry m_SelectedEntry;
        int m_SelectedEntryIndex;

        List<ShortcutEntry> m_ShortcutsBoundToSelectedKey = new List<ShortcutEntry>();


        public ShortcutManagerWindowViewController(SerializedShortcutManagerWindowState state, IDirectory directory, IBindingValidator bindingValidator, IShortcutProfileManager profileManager, IContextManager contextManager)
        {
            m_SerializedState = state;
            m_ShortcutProfileManager = profileManager;
            m_Directory = directory;
            m_ContextManager = contextManager;
            m_BindingValidator = bindingValidator;

            BuildCategoryList();
            selectedCategory = m_SerializedState.selectedCategory;
            selectedEntry = m_Directory.FindShortcutEntry(m_SerializedState.selectedEntryIdentifier);

            BuildKeyMapBindingStateData();

            PopulateShortcutsBoundToSelectedKey();
        }

        public void OnEnable()
        {
            m_ShortcutProfileManager.activeProfileChanged += OnActiveProfileChanged;
        }

        public void OnDisable()
        {
            m_ShortcutProfileManager.activeProfileChanged -= OnActiveProfileChanged;
        }

        void OnActiveProfileChanged(IShortcutProfileManager shortcutProfileManager, ShortcutProfile previousActiveProfile, ShortcutProfile currentActiveProfile)
        {
            UpdateCommandsWithConflicts();
            BuildKeyMapBindingStateData();
            PopulateShortcutList();
            m_ShortcutManagerWindowView.RefreshAll();
        }

        //There is a mutual dependecy between the view and the viewcontroller,
        //Ideally we would inject dependecies via the constructor, but since we have a circular dependecy
        //that is not possible, so until I find a better solution, we inject the dependency here vie this method.
        public void SetView(IShortcutManagerWindowView view)
        {
            m_ShortcutManagerWindowView = view;
        }

        public IShortcutManagerWindowView GetView()
        {
            return m_ShortcutManagerWindowView;
        }

        void BuildCategoryList()
        {
            const string separator = "/";
            var entries = new List<ShortcutEntry>();
            m_Directory.GetAllShortcuts(entries);

            m_AllEntries.Clear();
            m_AllEntries.Capacity = entries.Count;
            HashSet<string> categories = new HashSet<string>();

            var menuItems = new List<ShortcutEntry>();

            foreach (var entry in entries)
            {
                var identifier = entry.identifier.path;
                m_AllEntries.Add(entry);

                if (entry.type == ShortcutType.Menu)
                {
                    menuItems.Add(entry);
                }
                else
                {
                    var index = identifier.IndexOf(separator);
                    if (index > -1)
                    {
                        var category = identifier.Substring(0, index);
                        categories.Add(category);
                        if (!m_CategoryToEntriesList.ContainsKey(category))
                        {
                            m_CategoryToEntriesList.Add(category, new List<ShortcutEntry>());
                        }

                        m_CategoryToEntriesList[category].Add(entry);
                    }
                }
            }

            var shortcutNameComparer = new ShortcutNameComparer();
            m_AllEntries.Sort(shortcutNameComparer);

            m_CategoryToEntriesList.Add(m_AllUnityCommands, m_AllEntries);


            m_CategoryToEntriesList.Add(m_CommandsWithConflicts, new List<ShortcutEntry>());

            UpdateCommandsWithConflicts();
            menuItems.Sort(shortcutNameComparer);
            m_CategoryToEntriesList.Add(m_MainMenu, menuItems);


            m_Categories = categories.ToList();

            m_Categories.Sort();
            m_Categories.Insert(m_AllUntiyCommandsIndex, m_AllUnityCommands);
            m_Categories.Insert(m_ConflictsIndex, m_CommandsWithConflicts);
            m_Categories.Insert(m_MainMenuIndex, m_MainMenu);
        }

        void UpdateCommandsWithConflicts()
        {
            GetAllCommandsWithConflicts(m_CategoryToEntriesList[m_CommandsWithConflicts]);
        }

        void GetAllCommandsWithConflicts(List<ShortcutEntry> output)
        {
            output.Clear();
            m_Directory.FindShortcutsWithConflicts(output, m_ContextManager);
            output.Sort(new ConflictComparer());
        }

        public List<string> GetAvailableProfiles()
        {
            return m_ShortcutProfileManager.GetProfiles()
                .Select(p => p.id)
                .Concat(new[] { "Default" })
                .ToList();
        }

        public string activeProfile
        {
            get
            {
                var profile = m_ShortcutProfileManager.activeProfile;
                return profile == null ? "Default" : profile.id;
            }
            set
            {
                if (value == "Default")
                    m_ShortcutProfileManager.activeProfile = null;
                else
                {
                    var profile = m_ShortcutProfileManager.GetProfileById(value);
                    m_ShortcutProfileManager.activeProfile = profile;
                }
            }
        }

        public bool CanCreateProfile(string newProfileId)
        {
            return m_ShortcutProfileManager.CanCreateProfile(newProfileId) == CanCreateProfileResult.Success;
        }

        public void CreateProfile(string newProfileId)
        {
            var newProfile = m_ShortcutProfileManager.CreateProfile(newProfileId);
            m_ShortcutProfileManager.activeProfile = newProfile;
        }

        public bool CanRenameActiveProfile()
        {
            return m_ShortcutProfileManager.activeProfile != null;
        }

        public bool CanRenameActiveProfile(string newProfileId)
        {
            if (m_ShortcutProfileManager.activeProfile == null)
                return false;

            return m_ShortcutProfileManager.CanRenameProfile(m_ShortcutProfileManager.activeProfile, newProfileId) == CanRenameProfileResult.Success;
        }

        public void RenameActiveProfile(string newProfileId)
        {
            if (m_ShortcutProfileManager.activeProfile == null)
                return;

            m_ShortcutProfileManager.RenameProfile(m_ShortcutProfileManager.activeProfile, newProfileId);
        }

        public bool CanDeleteActiveProfile()
        {
            if (m_ShortcutProfileManager.activeProfile == null)
                return false;

            return m_ShortcutProfileManager.CanDeleteProfile(m_ShortcutProfileManager.activeProfile) == CanDeleteProfileResult.Success;
        }

        public void DeleteActiveProfile()
        {
            if (m_ShortcutProfileManager.activeProfile == null)
                return;

            m_ShortcutProfileManager.DeleteProfile(m_ShortcutProfileManager.activeProfile);
        }

        public IList<string> GetCategories()
        {
            return m_Categories;
        }

        public void SetCategorySelected(string category)
        {
            selectedCategory = category;
            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        string  selectedCategory
        {
            get { return m_SerializedState.selectedCategory; }
            set
            {
                var newCategoryIndex = 0;
                if (!string.IsNullOrEmpty(value))
                    newCategoryIndex = m_Categories.IndexOf(value);

                m_SelectedCategoryIndex = Math.Max(0, newCategoryIndex);
                m_SerializedState.selectedCategory = m_Categories[m_SelectedCategoryIndex];
                PopulateShortcutList();
            }
        }

        public int selectedKeyDetail => - 1;

        public SearchOption searchMode
        {
            get { return m_SerializedState.searchMode; }
            set { m_SerializedState.searchMode = value; }
        }

        public void SetSearch(string newSearch)
        {
            m_SerializedState.search = newSearch;
            PopulateShortcutList();
            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        public string GetSearch()
        {
            return m_SerializedState.search;
        }

        public List<KeyCombination> GetBindingSearch()
        {
            return m_SerializedState.bindingsSearch;
        }

        public void SetBindingSearch(List<KeyCombination> newBindingSearch)
        {
            m_SerializedState.bindingsSearch = newBindingSearch;
            PopulateShortcutList();
            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        public bool ShouldShowSearchFilters()
        {
            if (!IsSearching())
                return false;

            return m_SelectedCategoryIndex != m_AllUntiyCommandsIndex;
        }

        public void GetSearchFilters(List<string> filters)
        {
            if (m_SelectedCategoryIndex != m_AllUntiyCommandsIndex)
                filters.Add(m_Categories[m_SelectedCategoryIndex]);
            filters.Add(m_AllUnityCommands);
        }

        public string GetSelectedSearchFilter()
        {
            switch (m_SerializedState.searchCategoryFilter)
            {
                case SearchCategoryFilter.IgnoreCategory:
                    return m_AllUnityCommands;
                case SearchCategoryFilter.SearchWithinSelectedCategory:
                    return m_Categories[m_SelectedCategoryIndex];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetSelectedSearchFilter(string filter)
        {
            if (filter == m_AllUnityCommands)
                m_SerializedState.searchCategoryFilter = SearchCategoryFilter.IgnoreCategory;
            else if (filter == m_Categories[m_SelectedCategoryIndex])
                m_SerializedState.searchCategoryFilter = SearchCategoryFilter.SearchWithinSelectedCategory;
            else
                throw new ArgumentException("invalid filter", nameof(filter));

            PopulateShortcutList();

            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        public int selectedCategoryIndex =>  m_SelectedCategoryIndex;

        void PopulateShortcutList()
        {
            m_SelectedCategoryShortcutList.Clear();
            m_SelectedCategoryShortcutPathList.Clear();

            var category = selectedCategory;

            if (IsSearching() && m_SerializedState.searchCategoryFilter == SearchCategoryFilter.IgnoreCategory)
                category = m_AllUnityCommands;

            var identifierList = m_CategoryToEntriesList[category];

            foreach (var indentifier in identifierList)
            {
                var entry = indentifier;
                if (BelongsToSearch(entry))
                {
                    m_SelectedCategoryShortcutList.Add(entry);
                    m_SelectedCategoryShortcutPathList.Add(entry.identifier.path.StartsWith(category) ? entry.identifier.path.Substring(category.Length + 1) : entry.identifier.path);
                }
            }
        }

        bool BelongsToSearch(ShortcutEntry entry)
        {
            if (!IsSearching())
                return true;

            switch (searchMode)
            {
                case SearchOption.Name:
                    return entry.identifier.path.IndexOf(m_SerializedState.search, StringComparison.InvariantCultureIgnoreCase) >= 0;
                case SearchOption.Binding:
                    return entry.StartsWith(m_SerializedState.bindingsSearch);
            }

            return false;
        }

        public IList<ShortcutEntry> GetShortcutList()
        {
            return m_SelectedCategoryShortcutList;
        }

        public IList<string> GetShortcutPathList()
        {
            return m_SelectedCategoryShortcutPathList;
        }

        public ShortcutEntry selectedEntry
        {
            get { return m_SelectedEntry; }
            private set
            {
                m_SelectedEntry = value;
                if (m_SelectedEntry != null)
                {
                    m_SerializedState.selectedEntryIdentifier = m_SelectedEntry.identifier;
                    m_SelectedEntryIndex = m_SelectedCategoryShortcutList.FindIndex(entry => entry == m_SelectedEntry);
                }
                else
                {
                    m_SerializedState.selectedEntryIdentifier = new Identifier();
                    m_SelectedEntryIndex = -1;
                }
            }
        }

        public void ShortcutEntrySelected(ShortcutEntry entry)
        {
            selectedEntry = entry;
        }

        public int selectedEntryIndex => m_SelectedEntryIndex;

        public void DragEntryAndDropIntoKey(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry)
        {
            if (!CanEntryBeAssignedToKey(keyCode, eventModifier, entry))
                throw new InvalidOperationException("This would create a conflict");

            var keyCombination = new List<KeyCombination>();
            keyCombination.Add(KeyCombination.FromKeyboardInput(keyCode, eventModifier));
            RebindEntry(entry, keyCombination);
        }

        string GetUniqueProfileId()
        {
            var desiredId = "Default copy";
            const string desiredIdTemplate = "Default copy ({0})";

            var existingProfileIds = GetAvailableProfiles();
            int index;
            for (index = 1; existingProfileIds.Contains(desiredId) && index != int.MaxValue; index++)
            {
                desiredId = string.Format(desiredIdTemplate, index);
            }

            return index == int.MaxValue ? null : desiredId;
        }

        void RebindEntry(ShortcutEntry entry, List<KeyCombination> keyCombination)
        {
            // Ensure we have an active profile, if not create a new one
            if (m_ShortcutProfileManager.activeProfile == null)
            {
                var uniqueProfileId = GetUniqueProfileId();
                if (uniqueProfileId == null)
                {
                    Debug.LogWarning("Could not create unique profile id.");
                    return;
                }

                m_ShortcutProfileManager.activeProfile = m_ShortcutProfileManager.CreateProfile(uniqueProfileId);
            }

            m_ShortcutProfileManager.ModifyShortcutEntry(entry.identifier, keyCombination);
            UpdateCommandsWithConflicts();
            BuildKeyMapBindingStateData();
            PopulateShortcutList();
            m_ShortcutManagerWindowView.RefreshAll();
        }

        public bool CanEntryBeAssignedToKey(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry)
        {
            var keycombination = KeyCombination.FromKeyboardInput(keyCode, eventModifier);
            List<ShortcutEntry> entries;
            if (m_KeyBindingRootToBoundEntries.TryGetValue(keycombination, out entries))
            {
                foreach (var boundEntry in entries)
                {
                    if (IsGlobalContext(entry))
                        return false;

                    if (boundEntry.context.IsAssignableFrom(entry.context) || entry.context.IsAssignableFrom(boundEntry.context))
                        return false;
                }
            }

            return true;
        }

        void PopulateShortcutsBoundToSelectedKey()
        {
            m_ShortcutsBoundToSelectedKey.Clear();
            var keycombination = KeyCombination.FromKeyboardInput(m_SerializedState.selectedKey, m_SerializedState.selectedModifiers);
            List<ShortcutEntry> entries;
            if (m_KeyBindingRootToBoundEntries.TryGetValue(keycombination, out entries))
            {
                m_ShortcutsBoundToSelectedKey.AddRange(entries);
            }
        }

        public IList<ShortcutEntry> GetSelectedKeyShortcuts()
        {
            return m_ShortcutsBoundToSelectedKey;
        }

        public void NavigateTo(ShortcutEntry shortcutEntry)
        {
            SetCategorySelected(FindCategoryFor(shortcutEntry));
            selectedEntry = shortcutEntry;
            m_ShortcutManagerWindowView.RefreshCategoryList();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        string FindCategoryFor(ShortcutEntry shortcutEntry)
        {
            foreach (var pair in m_CategoryToEntriesList)
            {
                var foundId = pair.Value.Find(entry => entry == shortcutEntry);
                if (foundId != null)
                    return pair.Key;
            }

            return m_AllUnityCommands;
        }

        public void SetKeySelected(KeyCode keyCode, EventModifiers eventModifier)
        {
            m_SerializedState.selectedKey = keyCode;
            m_SerializedState.selectedModifiers = eventModifier;
            PopulateShortcutsBoundToSelectedKey();
        }

        public KeyCode GetSelectedKey()
        {
            return m_SerializedState.selectedKey;
        }

        public EventModifiers GetSelectedEventModifiers()
        {
            return m_SerializedState.selectedModifiers;
        }

        public void RequestRebindOfSelectedEntry(List<KeyCombination> newbinding)
        {
            var conflicts = FindConflictsIfRebound(selectedEntry, newbinding);

            if (conflicts.Count == 0)
            {
                RebindEntry(selectedEntry, newbinding);
            }
            else
            {
                var howToHandle = m_ShortcutManagerWindowView.HandleRebindWillCreateConflict(selectedEntry, newbinding, conflicts);
                switch (howToHandle)
                {
                    case RebindResolution.DoNotRebind:
                        break;
                    case RebindResolution.CreateConflict:
                        RebindEntry(selectedEntry, newbinding);
                        break;
                    case RebindResolution.UnassignExistingAndBind:
                        foreach (var conflictEntry in conflicts)
                            RebindEntry(conflictEntry, new List<KeyCombination>());
                        RebindEntry(selectedEntry, newbinding);
                        break;
                    default:
                        throw new Exception("Unhandled enum case");
                }
            }
        }

        public void BindSelectedEntryTo(List<KeyCombination> keyCombination)
        {
            RebindEntry(selectedEntry, keyCombination);
        }

        public IList<ShortcutEntry> GetSelectedEntryConflictsForGivenKeyCombination(List<KeyCombination> temporaryCombination)
        {
            if (temporaryCombination.Count == 0)
                return null;

            return FindConflictsIfRebound(selectedEntry, temporaryCombination);
        }

        public IList<ShortcutEntry> GetShortcutsBoundTo(KeyCode keyCode, EventModifiers modifiers)
        {
            var keyCombination = KeyCombination.FromKeyboardInput(keyCode, modifiers);
            List<ShortcutEntry> entries;
            if (m_KeyBindingRootToBoundEntries.TryGetValue(keyCombination, out entries))
            {
                return entries;
            }

            return null;
        }

        //TODO: find a better place for this logic, Directory maybe?
        IList<ShortcutEntry> FindConflictsIfRebound(ShortcutEntry entry, List<KeyCombination> newCombination)
        {
            var conflictingShortcuts = new List<ShortcutEntry>();
            m_Directory.FindPotentialConflicts(entry.context, newCombination, conflictingShortcuts, m_ContextManager);
            conflictingShortcuts.Remove(entry);
            return conflictingShortcuts;
        }

        bool IsSearching()
        {
            switch (searchMode)
            {
                case SearchOption.Name:
                    return !string.IsNullOrEmpty(m_SerializedState.search);
                case SearchOption.Binding:
                    return m_SerializedState.bindingsSearch.Any();
            }

            return false;
        }

        void BuildKeyMapBindingStateData()
        {
            m_KeyBindingRootToBoundEntries = new Dictionary<KeyCombination, List<ShortcutEntry>>();
            foreach (var entry in m_AllEntries)
            {
                var binding = entry.combinations;
                if (binding != null && binding.Any())
                {
                    var firstKeyBinding = binding.First();
                    if (!m_KeyBindingRootToBoundEntries.ContainsKey(firstKeyBinding))
                    {
                        m_KeyBindingRootToBoundEntries[firstKeyBinding] = new List<ShortcutEntry>();
                    }
                    m_KeyBindingRootToBoundEntries[firstKeyBinding].Add(entry);
                }
            }
        }

        public BindingState GetBindingStateForKeyWithModifiers(KeyCode keyCode, EventModifiers modifiers)
        {
            var keycombination = KeyCombination.FromKeyboardInput(keyCode, modifiers);
            List<ShortcutEntry> entries;
            if (m_KeyBindingRootToBoundEntries.TryGetValue(keycombination, out entries))
            {
                foreach (var entry in entries)
                {
                    if (IsGlobalContext(entry))
                    {
                        return BindingState.BoundGlobally;
                    }
                }
                return BindingState.BoundToContext;
            }

            return BindingState.NotBound;
        }

        public bool CanBeSelected(KeyCode code)
        {
            return !IsModifier(code) && !IsReservedKey(code);
        }

        public bool IsModifier(KeyCode code)
        {
            return ModifierFromKeyCode(code) != EventModifiers.None;
        }

        public bool IsReservedKey(KeyCode code)
        {
            return !m_BindingValidator.IsBindingValid(code);
        }

        public EventModifiers ModifierFromKeyCode(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return EventModifiers.Shift;
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return EventModifiers.Control;
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return EventModifiers.Alt;
                case KeyCode.LeftCommand:
                case KeyCode.RightCommand:
                    return EventModifiers.Command;
                default:
                    return EventModifiers.None;
            }
        }

        static bool IsGlobalContext(ShortcutEntry entry)
        {
            return entry.context == ContextManager.globalContextType;
        }

        public void SelectConflictCategory()
        {
            SetCategorySelected(m_CommandsWithConflicts);
            GetView().RefreshCategoryList();
        }
    }

    class ConflictComparer : IComparer<ShortcutEntry>
    {
        IComparer<IEnumerable<KeyCombination>> m_KeyCombinationSequenceComprarer;
        public ConflictComparer()
        {
            m_KeyCombinationSequenceComprarer = new KeyCombinationSequenceComparer();
        }

        public int Compare(ShortcutEntry x, ShortcutEntry y)
        {
            return m_KeyCombinationSequenceComprarer.Compare(x?.combinations, y?.combinations);
        }
    }

    class KeyCombinationSequenceComparer : IComparer<IEnumerable<KeyCombination>>
    {
        IComparer<KeyCombination> m_KeyCombinationComparer;

        public KeyCombinationSequenceComparer()
        {
            m_KeyCombinationComparer = new KeyCombinationComparer();
        }

        public int Compare(IEnumerable<KeyCombination> x, IEnumerable<KeyCombination> y)
        {
            if (x == null && y != null)
                return -1;
            if (x != null && y == null)
                return 1;
            if (x == null && y == null)
                return 0;

            var xEnum = x.GetEnumerator();
            var yEnum = y.GetEnumerator();

            var xValid = xEnum.MoveNext();
            var yValid = yEnum.MoveNext();

            while (xValid && yValid)
            {
                var elementCompare = m_KeyCombinationComparer.Compare(xEnum.Current, yEnum.Current);
                if (elementCompare != 0)
                {
                    xEnum.Dispose();
                    yEnum.Dispose();
                    return elementCompare;
                }

                xValid = xEnum.MoveNext();
                yValid = xEnum.MoveNext();
            }

            xEnum.Dispose();
            yEnum.Dispose();

            if (xValid == yValid)
                return 0;
            if (xValid && !yValid)
                return 1;

            return -1;
        }
    }

    class KeyCombinationComparer : IComparer<KeyCombination>
    {
        public int Compare(KeyCombination x, KeyCombination y)
        {
            var modifierCompare = x.modifiers.CompareTo(y.modifiers);

            if (modifierCompare != 0)
                return modifierCompare;

            return x.keyCode.CompareTo(y.keyCode);
        }
    }
    class ShortcutNameComparer : IComparer<ShortcutEntry>
    {
        public int Compare(ShortcutEntry x, ShortcutEntry y)
        {
            return string.Compare(x?.identifier.path, y?.identifier.path);
        }
    }
}
