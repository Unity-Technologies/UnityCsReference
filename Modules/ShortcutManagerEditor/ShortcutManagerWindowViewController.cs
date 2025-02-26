// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutManagerWindowViewController : IShortcutManagerWindowViewController, IKeyBindingStateProvider
    {
        internal static readonly string k_AllUnityCommands = L10n.Tr("All Unity Commands");
        internal static readonly string k_CommandsWithConflicts = L10n.Tr("Binding Conflicts");
        internal static readonly string k_MainMenu = L10n.Tr("Main Menu");

        static readonly string k_ProfileNameEmpty = L10n.Tr("Profile name is empty");
        static readonly string k_ProfileNameUnsupported = L10n.Tr("Profile name is unsupported");
        static readonly string k_ProfileExists = L10n.Tr("Profile already exists");
        static readonly string k_DefaultRename = L10n.Tr("Default profile cannot be renamed");
        static readonly string k_ProfileNotFound = L10n.Tr("Couldn't find active profile");

        const int k_AllUnityCommandsIndex = 0;
        const int k_ConflictsIndex = 1;
        const int k_MainMenuIndex = 2;

        SerializedShortcutManagerWindowState m_SerializedState;
        IShortcutProfileManager m_ShortcutProfileManager;
        IDirectory m_Directory;
        IContextManager m_ContextManager;
        IShortcutManagerWindowView m_ShortcutManagerWindowView;
        IBindingValidator m_BindingValidator;
        IAvailableShortcutsChangedNotifier m_AvailableShortcutsChangedNotifier;

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

        public ShortcutManagerWindowViewController(SerializedShortcutManagerWindowState state, IDirectory directory, IBindingValidator bindingValidator, IShortcutProfileManager profileManager, IContextManager contextManager, IAvailableShortcutsChangedNotifier availableShortcutsChangedNotifier)
        {
            m_SerializedState = state;
            m_ShortcutProfileManager = profileManager;
            m_Directory = directory;
            m_ContextManager = contextManager;
            m_BindingValidator = bindingValidator;
            m_AvailableShortcutsChangedNotifier = availableShortcutsChangedNotifier;

            InitializeCategoryList();
            BuildKeyMapBindingStateData();
            PopulateShortcutsBoundToSelectedKey();
        }

        void InitializeCategoryList()
        {
            BuildCategoryList();
            selectedCategory = m_SerializedState.selectedCategory;
            selectedEntry = m_Directory.FindShortcutEntry(m_SerializedState.selectedEntryIdentifier);
        }

        public void OnEnable()
        {
            m_ShortcutProfileManager.activeProfileChanged += OnActiveProfileChanged;
            m_ShortcutProfileManager.loadedProfilesChanged += OnLoadedProfilesChanged;
            m_AvailableShortcutsChangedNotifier.availableShortcutsChanged += OnAvailableShortcutsChanged;
        }

        public void OnDisable()
        {
            m_ShortcutProfileManager.activeProfileChanged -= OnActiveProfileChanged;
            m_ShortcutProfileManager.loadedProfilesChanged -= OnLoadedProfilesChanged;
            m_AvailableShortcutsChangedNotifier.availableShortcutsChanged -= OnAvailableShortcutsChanged;
        }

        void OnActiveProfileChanged(IShortcutProfileManager shortcutProfileManager, ShortcutProfile previousActiveProfile, ShortcutProfile currentActiveProfile)
        {
            UpdateInternalCache();
        }

        void OnAvailableShortcutsChanged()
        {
            InitializeCategoryList();
            UpdateInternalCache();
        }

        void UpdateInternalCache()
        {
            UpdateCommandsWithConflicts();
            BuildKeyMapBindingStateData();
            PopulateShortcutList();
            m_ShortcutManagerWindowView.RefreshAll();
        }

        void OnLoadedProfilesChanged(IShortcutProfileManager shortcutProfileManager)
        {
            m_ShortcutManagerWindowView.RefreshProfiles();
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

        public void ResetToDefault(ShortcutEntry entry)
        {
            var newBinding = entry.GetDefaultCombinations();
            var conflicts = FindConflictsIfRebound(entry, newBinding);

            if (conflicts.Count == 0)
            {
                m_ShortcutProfileManager.ResetToDefault(entry);
            }
            else
            {
                var howToHandle = m_ShortcutManagerWindowView.HandleRebindWillCreateConflict(entry, newBinding, conflicts);
                switch (howToHandle)
                {
                    case RebindResolution.DoNotRebind:
                        break;
                    case RebindResolution.CreateConflict:
                        m_ShortcutProfileManager.ResetToDefault(entry);
                        break;
                    case RebindResolution.UnassignExistingAndBind:
                        foreach (var conflictEntry in conflicts)
                            RebindEntry(conflictEntry, new List<KeyCombination>());
                        m_ShortcutProfileManager.ResetToDefault(entry);
                        break;
                    default:
                        throw new Exception("Unhandled enum case");
                }
            }

            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        public void RemoveBinding(ShortcutEntry entry)
        {
            RebindEntry(entry, new List<KeyCombination>());
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

            m_CategoryToEntriesList.Clear();

            foreach (var entry in entries)
            {
                var identifier = entry.displayName;
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

            m_CategoryToEntriesList.Add(k_AllUnityCommands, m_AllEntries);


            m_CategoryToEntriesList.Add(k_CommandsWithConflicts, new List<ShortcutEntry>());

            UpdateCommandsWithConflicts();
            menuItems.Sort(shortcutNameComparer);
            m_CategoryToEntriesList.Add(k_MainMenu, menuItems);


            m_Categories = categories.ToList();

            m_Categories.Sort();
            m_Categories.Insert(k_AllUnityCommandsIndex, k_AllUnityCommands);
            m_Categories.Insert(k_ConflictsIndex, k_CommandsWithConflicts);
            m_Categories.Insert(k_MainMenuIndex, k_MainMenu);
        }

        void UpdateCommandsWithConflicts()
        {
            GetAllCommandsWithConflicts(m_CategoryToEntriesList[k_CommandsWithConflicts]);
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

        public string CanCreateProfile(string newProfileId)
        {
            var canCreateProfileResult = m_ShortcutProfileManager.CanCreateProfile(newProfileId);

            switch (canCreateProfileResult)
            {
                case CanCreateProfileResult.InvalidProfileId:
                    return string.IsNullOrWhiteSpace(newProfileId) ? k_ProfileNameEmpty : k_ProfileNameUnsupported;
                case CanCreateProfileResult.DuplicateProfileId:
                    return k_ProfileExists;
                default:
                    return null;
            }
        }

        public void CreateProfile(string newProfileId)
        {
            var newProfile = m_ShortcutProfileManager.CreateProfile(newProfileId);
            m_ShortcutProfileManager.activeProfile = newProfile;
        }

        public bool CanRenameActiveProfile()
        {
            return CanDeleteActiveProfile();
        }

        public string CanRenameActiveProfile(string newProfileId)
        {
            if (m_ShortcutProfileManager.activeProfile == null)
                return k_DefaultRename;

            switch (m_ShortcutProfileManager.CanRenameProfile(m_ShortcutProfileManager.activeProfile, newProfileId))
            {
                case CanRenameProfileResult.ProfileNotFound:
                    return k_ProfileNotFound;
                case CanRenameProfileResult.InvalidProfileId:
                    return string.IsNullOrWhiteSpace(newProfileId) ? k_ProfileNameEmpty : k_ProfileNameUnsupported;
                case CanRenameProfileResult.DuplicateProfileId:
                    return k_ProfileExists;
                default:
                    return null;
            }
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

        public int categorySeparatorIndex { get; } = k_MainMenuIndex;

        public bool IsCategorySelected(string category)
        {
            return selectedCategory != null && selectedCategory.Equals(category);
        }

        public void SetCategorySelected(string category)
        {
            selectedCategory = category;
            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        string selectedCategory
        {
            get
            {
                return m_SerializedState.selectedCategory;
            }
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
            get
            {
                return m_SerializedState.searchMode;
            }
            set
            {
                m_SerializedState.searchMode = value;
            }
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

            return m_SelectedCategoryIndex != k_AllUnityCommandsIndex;
        }

        public void GetSearchFilters(List<string> filters)
        {
            if (m_SelectedCategoryIndex != k_AllUnityCommandsIndex)
                filters.Add(m_Categories[m_SelectedCategoryIndex]);
            filters.Add(k_AllUnityCommands);
        }

        public string GetSelectedSearchFilter()
        {
            switch (m_SerializedState.searchCategoryFilter)
            {
                case SearchCategoryFilter.IgnoreCategory:
                    return k_AllUnityCommands;
                case SearchCategoryFilter.SearchWithinSelectedCategory:
                    return m_Categories[m_SelectedCategoryIndex];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetSelectedSearchFilter(string filter)
        {
            if (filter == k_AllUnityCommands)
                m_SerializedState.searchCategoryFilter = SearchCategoryFilter.IgnoreCategory;
            else if (filter == m_Categories[m_SelectedCategoryIndex])
                m_SerializedState.searchCategoryFilter = SearchCategoryFilter.SearchWithinSelectedCategory;
            else
                throw new ArgumentException("invalid filter", nameof(filter));

            PopulateShortcutList();

            m_ShortcutManagerWindowView.UpdateSearchFilterOptions();
            m_ShortcutManagerWindowView.RefreshShortcutList();
        }

        public int selectedCategoryIndex => m_SelectedCategoryIndex;

        void PopulateShortcutList()
        {
            m_SelectedCategoryShortcutList.Clear();
            m_SelectedCategoryShortcutPathList.Clear();

            var category = selectedCategory;

            if (IsSearching() && m_SerializedState.searchCategoryFilter == SearchCategoryFilter.IgnoreCategory)
                category = k_AllUnityCommands;

            var identifierList = m_CategoryToEntriesList[category];

            foreach (var identifier in identifierList)
            {
                var entry = identifier;
                if (BelongsToSearch(entry))
                {
                    m_SelectedCategoryShortcutList.Add(entry);
                    m_SelectedCategoryShortcutPathList.Add(entry.displayName.StartsWith(category) ? entry.displayName.Substring(category.Length + 1) : entry.displayName);
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
                    return entry.displayName.IndexOf(m_SerializedState.search, StringComparison.InvariantCultureIgnoreCase) >= 0;
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
            get
            {
                return m_SelectedEntry;
            }
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
            if (!m_BindingValidator.IsKeyValid(keyCode, out _))
            {
                return false;
            }

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

            return k_AllUnityCommands;
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

        public void RequestRebindOfSelectedEntry(List<KeyCombination> newBinding)
        {
            newBinding = CheckAndAdjustReservedModifierUse(newBinding);
            if (newBinding == null)
                return;
            
            var conflicts = FindConflictsIfRebound(selectedEntry, newBinding);
            if (conflicts.Count == 0)
            {
                RebindEntry(selectedEntry, newBinding);
            }
            else
            {
                var howToHandle = m_ShortcutManagerWindowView.HandleRebindWillCreateConflict(selectedEntry, newBinding, conflicts);
                switch (howToHandle)
                {
                    case RebindResolution.DoNotRebind:
                        break;
                    case RebindResolution.CreateConflict:
                        RebindEntry(selectedEntry, newBinding);
                        break;
                    case RebindResolution.UnassignExistingAndBind:
                        foreach (var conflictEntry in conflicts)
                            RebindEntry(conflictEntry, new List<KeyCombination>());
                        RebindEntry(selectedEntry, newBinding);
                        break;
                    default:
                        throw new Exception("Unhandled enum case");
                }
            }
        }

        List<KeyCombination> CheckAndAdjustReservedModifierUse(List<KeyCombination> binding)
        {
            // Get reserved modifiers of selected entry's context
            var ctxReservedModifiers = ShortcutModifiers.None;
            var attribute = selectedEntry.context.GetCustomAttribute<ReserveModifiersAttribute>(true);
            if (attribute != null)
                ctxReservedModifiers = attribute.modifier;

            // Check if incoming binding includes any of the selected shortcut context's reserved modifiers
            var bindingUsesReservedModifier = false;
            if (ctxReservedModifiers != ShortcutModifiers.None)
            {
                foreach (var keyCombination in binding)
                {
                    if (keyCombination.modifiers.HasFlag(ctxReservedModifiers))
                    {
                        bindingUsesReservedModifier = true;
                        break;
                    }
                }
            }

            if (bindingUsesReservedModifier)
            {
                // Build alternative binding with reserved modifiers ommited
                var altBinding = new List<KeyCombination>();
                for (int i = 0; i < binding.Count; ++i)
                {
                    var altModifiers = binding[i].modifiers & ~ctxReservedModifiers;
                    altBinding.Add(new KeyCombination(binding[i].keyCode, altModifiers));
                }

                // Prompt user
                var ctxModifiersStr = new StringBuilder();
                KeyCombination.VisualizeModifiers(ctxReservedModifiers, ctxModifiersStr);

                var title = L10n.Tr("Attempt to bind Reserved Modifier");
                var altBindingStr = KeyCombination.SequenceToString(altBinding);
                var message = string.Format(
                    L10n.Tr("You can't bind \"{0}\" to {1}, because {1} contains {3} which is a reserved modifier in the \"{2}\" shortcut context.\n\n" +
                            "You can choose to bind just \"{4}\" instead. If you do, \"{4}\" will also work with the reserved modifier {3}."),
                    selectedEntry.displayName,
                    KeyCombination.SequenceToString(binding),
                    ObjectNames.NicifyVariableName(selectedEntry.context.Name),
                    ctxModifiersStr,
                    altBindingStr);
                var okLabel = string.Format(L10n.Tr("Bind \'{0}\' instead"), altBindingStr);
                var cancelLabel = L10n.Tr("Cancel");

                if (EditorUtility.DisplayDialog(title, message, okLabel, cancelLabel))
                    return altBinding;

                return null;
            }
            return binding;
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

        public bool IsEntryPartOfConflict(ShortcutEntry shortcutEntry)
        {
            return m_CategoryToEntriesList[k_CommandsWithConflicts].Contains(shortcutEntry);
        }

        //TODO: find a better place for this logic, Directory maybe?
        IList<ShortcutEntry> FindConflictsIfRebound(ShortcutEntry entry, List<KeyCombination> newCombination)
        {
            var conflictingShortcuts = new List<ShortcutEntry>();
            m_Directory.FindPotentialConflicts(entry.context, entry.tag, newCombination, conflictingShortcuts, m_ContextManager);
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
            bool foundEntries = m_KeyBindingRootToBoundEntries.TryGetValue(keycombination, out entries);

            if (Application.platform != RuntimePlatform.OSXEditor && (keycombination.modifiers & ShortcutModifiers.Action) != 0)
            {
                var altkeycombination = new KeyCombination(keycombination.keyCode,
                    (keycombination.modifiers & ~ShortcutModifiers.Action) | ShortcutModifiers.Control);

                if (m_KeyBindingRootToBoundEntries.TryGetValue(altkeycombination, out List<ShortcutEntry> additionalEntries))
                {
                    foundEntries = true;
                    if (entries == null) entries = new List<ShortcutEntry>();
                    entries.AddRange(additionalEntries);
                }
            }

            if (foundEntries)
            {
                var state = BindingState.NotBound;

                foreach (var entry in entries)
                    state |= IsGlobalContext(entry) ? BindingState.BoundGlobally : BindingState.BoundToContext;

                return state;
            }

            return BindingState.NotBound;
        }

        public bool CanBeSelected(KeyCode code)
        {
            return !IsModifier(code) && !IsReservedKey(code);
        }

        public static bool IsModifier(KeyCode code)
        {
            return ModifierFromKeyCode(code) != EventModifiers.None;
        }

        bool IKeyBindingStateProvider.IsModifier(KeyCode code)
        {
            return IsModifier(code);
        }

        public bool IsReservedKey(KeyCode code)
        {
            return !IsModifier(code) && !m_BindingValidator.IsKeyValid(code, out _);
        }

        EventModifiers IKeyBindingStateProvider.ModifierFromKeyCode(KeyCode k)
        {
            return ModifierFromKeyCode(k);
        }

        public static EventModifiers ModifierFromKeyCode(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return EventModifiers.Shift;
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return EventModifiers.Alt;
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return EventModifiers.Control;
                case KeyCode.LeftCommand:
                case KeyCode.RightCommand:
                    return SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX ? EventModifiers.Command : EventModifiers.None;
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
            SetCategorySelected(k_CommandsWithConflicts);
            GetView().RefreshCategoryList();
        }

        public bool CanImportProfile(string path, bool letUserDecide = true)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            var profileId = m_ShortcutProfileManager.GetProfileId(path);

            // This is mainly intended to disable modal dialogs when running tests
            letUserDecide = letUserDecide && InternalEditorUtility.isHumanControllingUs && !InternalEditorUtility.inBatchMode;

            if (m_ShortcutProfileManager.GetProfileById(profileId) != null
                && (!letUserDecide || !EditorUtility.DisplayDialog(L10n.Tr("Profile already exists"),
                L10n.Tr($"A profile with the \"{profileId}\" id already exists.\nDo you want to overwrite the existing profile with values from the selected file?"),
                L10n.Tr("Yes"), L10n.Tr("No"))))
                return false;

            return true;
        }

        public void ImportProfile(string path)
        {
            m_ShortcutProfileManager.ImportProfile(path);
            activeProfile = m_ShortcutProfileManager.GetProfileId(path);
        }

        public bool CanExportProfile() => m_ShortcutProfileManager.activeProfile != null;

        public void ExportProfile(string path) => m_ShortcutProfileManager.ExportProfile(path);
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
            return string.Compare(x?.displayName, y?.displayName);
        }
    }
}
