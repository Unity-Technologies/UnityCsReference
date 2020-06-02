// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutManagerWindowView : IShortcutManagerWindowView
    {
        const int k_ListItemHeight = 21;
        const int k_PixelsPadding = 78;
        VisualElement m_Root;
        IShortcutManagerWindowViewController m_ViewController;
        IKeyBindingStateProvider m_BindingStateProvider;

        TextElement m_ActiveProfileDropdownButton;
        Keyboard m_KeyboardElement;
        ListView m_ShortcutsTable;
        ToolbarPopupSearchField m_SearchTextField;

        bool m_StartedDrag;
        Vector2 m_MouseDownStartPos;
        ListView m_CategoryTreeView;
        ShortcutPopupSearchField m_KeyBindingSearchField;
        ShortcutEntry m_EditingBindings;
        VisualElement m_SearchFiltersContainer;
        VisualElement m_ShortcutsTableSearchFilterContainer;
        HorizontalColumnDragger m_HorizontalColumnDragger;

        public ShortcutManagerWindowView(IShortcutManagerWindowViewController viewController, IKeyBindingStateProvider bindingStateProvider)
        {
            m_ViewController = viewController;
            m_BindingStateProvider = bindingStateProvider;
            BuildVisualElementHierarchyRoot();
            UpdateShortcutTableSearchFilter();
        }

        public VisualElement GetVisualElementHierarchyRoot()
        {
            return m_Root;
        }

        public void RefreshAll()
        {
            RefreshKeyboard();
            RefreshShortcutList();
            RefreshProfiles();
        }

        public void RefreshKeyboard()
        {
            m_KeyboardElement.Refresh();
        }

        public void RefreshCategoryList()
        {
            m_CategoryTreeView.selectedIndex = m_ViewController.selectedCategoryIndex;
            m_CategoryTreeView.Refresh();
        }

        public void RefreshShortcutList()
        {
            m_ShortcutsTable.Refresh();
            m_HorizontalColumnDragger.RequestUpdatePositions();
        }

        public void UpdateSearchFilterOptions()
        {
            UpdateShortcutTableSearchFilter();
        }

        public RebindResolution HandleRebindWillCreateConflict(ShortcutEntry entry, IList<KeyCombination> newBinding, IList<ShortcutEntry> conflicts)
        {
            var title = L10n.Tr("Binding Conflict");
            var message = string.Format(L10n.Tr("The key {0} is already assigned to the \"{1}\" shortcut.\nDo you want to reassign this key?"), KeyCombination.SequenceToString(newBinding), conflicts[0].displayName);
            var result = EditorUtility.DisplayDialogComplex(title, message, L10n.Tr("Reassign"), L10n.Tr("Cancel"), L10n.Tr("Create Conflict"));
            switch (result)
            {
                case 0:
                    return RebindResolution.UnassignExistingAndBind;
                case 1:
                    return RebindResolution.DoNotRebind;
                case 2:
                    return RebindResolution.CreateConflict;
                default:
                    throw new Exception("Unrecognized option");
            }
        }

        static void ShowElement(VisualElement el)
        {
            el.style.display = DisplayStyle.Flex;
        }

        static void HideElement(VisualElement el)
        {
            el.style.display = DisplayStyle.None;
        }

        VisualElement MakeItemForShortcutTable()
        {
            //TODO: Read from a uxml
            var shortcutNameCell = new TextElement();
            var bindingContainer = new VisualElement();
            var shortcutBinding = new TextElement();
            var rebindControl = new ShortcutTextField();
            var warningIcon = new VisualElement();

            bindingContainer.AddToClassList("binding-container");
            bindingContainer.Add(shortcutBinding);
            bindingContainer.Add(rebindControl);
            bindingContainer.Add(warningIcon);

            HideElement(rebindControl);
            shortcutBinding.style.flexGrow = 1;

            rebindControl.input.RegisterCallback<BlurEvent>(OnRebindControlBlurred);
            rebindControl.RegisterCallback<DetachFromPanelEvent>(OnRebindControlDetachedFromPanel);
            rebindControl.OnCancel += RebindControl_OnCancel;

            warningIcon.AddToClassList("warning-icon");
            HideElement(warningIcon);

            var rowElement = new VisualElement();
            rowElement.AddToClassList("shortcut-row");
            rowElement.Add(shortcutNameCell);
            rowElement.Add(bindingContainer);

            rowElement.RegisterCallback<MouseDownEvent>(OnMouseDownCategoryTable);
            rowElement.RegisterCallback<MouseUpEvent>(OnMouseUpCategoryTable);
            rowElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedShortcutRow);

            return rowElement;
        }

        void OnGeometryChangedShortcutRow(GeometryChangedEvent evt)
        {
            m_HorizontalColumnDragger.UpdatePositionsIfRequested();
        }

        void RebindControl_OnCancel()
        {
            EndRebind();
        }

        void OnRebindControlBlurred(BlurEvent evt)
        {
            var el = (VisualElement)evt.target;
            if (el.style.visibility == Visibility.Visible)
                EndRebind();
        }

        void OnRebindControlDetachedFromPanel(DetachFromPanelEvent evt)
        {
            EndRebind(false);
        }

        void BindShortcutEntryItem(VisualElement shortcutElementTemplate, int index)
        {
            var shortcutEntry = m_ViewController.GetShortcutList()[index];

            var nameElement = (TextElement)shortcutElementTemplate[0];
            var bindingContainer = shortcutElementTemplate[1];
            var bindingTextElement = bindingContainer.Q<TextElement>();
            var bindingField = bindingContainer.Q<ShortcutTextField>();
            var warningIcon = shortcutElementTemplate.Q(null, "warning-icon");

            nameElement.text = m_ViewController.GetShortcutPathList()[index];
            nameElement.tooltip = nameElement.text;
            bindingTextElement.text = KeyCombination.SequenceToString(shortcutEntry.combinations);
            bindingField.SetValueWithoutNotify(shortcutEntry.combinations.ToList());
            bindingField.RegisterValueChangedCallback(EditingShortcutEntryBindingChanged);

            var conflict = m_ViewController.IsEntryPartOfConflict(shortcutEntry);

            if (conflict)
                ShowElement(warningIcon);
            else
                HideElement(warningIcon);

            if (shortcutEntry.overridden)
            {
                nameElement.AddToClassList("overridden");
                bindingTextElement.AddToClassList("overridden");
            }
            else
            {
                nameElement.RemoveFromClassList("overridden");
                bindingTextElement.RemoveFromClassList("overridden");
            }

            if (m_EditingBindings == shortcutEntry)
            {
                HideElement(bindingTextElement);
                ShowElement(bindingField);
                bindingField.Focus();
            }
            else
            {
                ShowElement(bindingTextElement);
                HideElement(bindingField);
            }
        }

        void EditingShortcutEntryBindingChanged(ChangeEvent<List<KeyCombination>> evt)
        {
            m_ViewController.RequestRebindOfSelectedEntry(evt.newValue);
            EndRebind();
        }

        static VisualElement MakeItemForCategoriesTable()
        {
            var element = new TextElement();
            element.AddToClassList("category-row");
            return element;
        }

        void BindCategoriesTableItem(VisualElement categoryElementTemplate, int index)
        {
            var elementTemplate = (TextElement)categoryElementTemplate;
            elementTemplate.text = m_ViewController.GetCategories()[index];

            if (index == m_ViewController.categorySeparatorIndex)
                elementTemplate.AddToClassList("first-row-of-section");
        }

        void CategorySelectionChanged(IEnumerable<object> selection)
        {
            Assert.AreEqual(1, selection.Count());

            m_ShortcutsTable.selectedIndex = -1;

            if (!selection.Any())
                m_ViewController.SetCategorySelected(null);
            else
                m_ViewController.SetCategorySelected((string)selection.First());
        }

        void BuildSearchField(VisualElement root)
        {
            var searchControlContainer = root.Q("searchControlContainer");

            searchControlContainer.style.flexGrow = 1;
            searchControlContainer.style.flexDirection = FlexDirection.RowReverse;

            m_SearchTextField = new ToolbarPopupSearchField();

            m_SearchTextField.style.flexBasis = 150f;
            m_SearchTextField.style.flexGrow = 0;
            m_SearchTextField.Q("unity-text-input").style.paddingRight = 0;

            m_SearchTextField.menu.AppendAction(EditorGUIUtility.TrTextContent("Command").text,
                a => SearchOptionSelected(SearchOption.Name),
                a => m_ViewController.searchMode == SearchOption.Name ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_SearchTextField.menu.AppendAction(EditorGUIUtility.TrTextContent("Shortcut").text,
                a => SearchOptionSelected(SearchOption.Binding),
                a => m_ViewController.searchMode == SearchOption.Binding ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_KeyBindingSearchField = new ShortcutPopupSearchField();

            m_KeyBindingSearchField.style.flexBasis = 150f;
            m_KeyBindingSearchField.style.flexGrow = 0;
            m_KeyBindingSearchField.Q("unity-text-input").style.paddingRight = 0;

            m_KeyBindingSearchField.menu.AppendAction(EditorGUIUtility.TrTextContent("Command").text,
                a => SearchOptionSelected(SearchOption.Name),
                a => m_ViewController.searchMode == SearchOption.Name ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_KeyBindingSearchField.menu.AppendAction(EditorGUIUtility.TrTextContent("Shortcut").text,
                a => SearchOptionSelected(SearchOption.Binding),
                a => m_ViewController.searchMode == SearchOption.Binding ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);


            ShowAppropriateSearchField();

            m_SearchTextField.value = m_ViewController.GetSearch();
            m_SearchTextField.RegisterValueChangedCallback(OnSearchStringChanged);

            m_KeyBindingSearchField.value = m_ViewController.GetBindingSearch();
            m_KeyBindingSearchField.OnWorkingValueChanged += SearchByBindingsChanged;

            searchControlContainer.Add(m_SearchTextField);
            searchControlContainer.Add(m_KeyBindingSearchField);

            root.Add(searchControlContainer);
        }

        public void RefreshProfiles()
        {
            m_ActiveProfileDropdownButton.text = m_ViewController.activeProfile;
        }

        void BuildProfileManagementRow(VisualElement header)
        {
            m_ActiveProfileDropdownButton = header.Q<TextElement>("activeProfileDropdownButtonText");
            m_ActiveProfileDropdownButton.text = m_ViewController.activeProfile;
            m_ActiveProfileDropdownButton.AddToClassList(PopupField<string>.textUssClassName);

            // Style active profile dropdown button as a popup field
            var activeProfileDropdownButton = header.Q("activeProfileDropdownButton");
            activeProfileDropdownButton.AddToClassList(BasePopupField<string, string>.inputUssClassName);
            activeProfileDropdownButton.RegisterCallback<MouseDownEvent>(OnProfileContextMenuMouseDown);

            var activeProfileDropdownArrow = activeProfileDropdownButton.Q("arrow");
            activeProfileDropdownArrow.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
            activeProfileDropdownArrow.pickingMode = PickingMode.Ignore;
        }

        void OnCreateProfileClicked()
        {
            PromptWindow.Show(L10n.Tr("Create profile"),
                L10n.Tr("Create a shortcut profile"),
                L10n.Tr("Enter the name of the profile you want to create"),
                L10n.Tr("Profile Name:"),
                L10n.Tr("New profile"),
                L10n.Tr("Create"),
                m_ViewController.CanCreateProfile,
                m_ViewController.CreateProfile);
        }

        void OnRenameProfileClicked()
        {
            PromptWindow.Show(L10n.Tr("Rename profile"),
                L10n.Tr("Rename a shortcut profile"),
                string.Format(L10n.Tr("Enter the new name you want to give the profile '{0}'"), m_ViewController.activeProfile),
                L10n.Tr("Profile Name:"),
                m_ViewController.activeProfile,
                L10n.Tr("Rename"),
                m_ViewController.CanRenameActiveProfile,
                m_ViewController.RenameActiveProfile);
        }

        void OnDeleteProfileClicked()
        {
            DeleteShortcutProfileWindow.Show(m_ViewController.activeProfile, () => m_ViewController.DeleteActiveProfile());
        }

        void OnActiveProfileChanged(ChangeEvent<string> evt)
        {
            m_ViewController.activeProfile = evt.newValue;
        }

        void OnActiveProfileChanged(string profile)
        {
            m_ViewController.activeProfile = profile;
        }

        void OnProfileContextMenuMouseDown(MouseDownEvent evt)
        {
            var targetElement = (VisualElement)evt.target;
            var genericMenu = new GenericMenu();

            foreach (var profile in m_ViewController.GetAvailableProfiles())
            {
                genericMenu.AddItem(new GUIContent(profile), false, () => OnActiveProfileChanged(profile));
            }

            genericMenu.AddSeparator("");

            genericMenu.AddItem(EditorGUIUtility.TrTextContent("Create new profile..."), false, OnCreateProfileClicked);

            if (m_ViewController.CanRenameActiveProfile())
                genericMenu.AddItem(EditorGUIUtility.TrTextContent("Rename profile..."), false, OnRenameProfileClicked);
            else
                genericMenu.AddDisabledItem(EditorGUIUtility.TrTextContent("Rename profile..."));

            if (m_ViewController.CanDeleteActiveProfile())
                genericMenu.AddItem(EditorGUIUtility.TrTextContent("Delete profile..."), false, OnDeleteProfileClicked);
            else
                genericMenu.AddDisabledItem(EditorGUIUtility.TrTextContent("Delete profile..."));

            genericMenu.DropDown(targetElement.worldBound);
        }

        void BuildLegendRow(VisualElement root)
        {
            var container = root.Q("legendContainer");

            var labels = new[] { L10n.Tr("Unassigned Key"), L10n.Tr("Assigned Key"), L10n.Tr("Global Key") };
            var classes = new[] { "unassigned", "contextuallyBound", "global" };

            for (var i = 0; i < labels.Length; i++)
            {
                var colorField = new VisualElement();
                var label = new TextElement() { text = labels[i] };
                colorField.AddToClassList("keyLegend");
                colorField.AddToClassList(classes[i]);
                container.Add(colorField);
                container.Add(label);
            }
        }

        void UpdateShortcutTableSearchFilter()
        {
            if (!m_ViewController.ShouldShowSearchFilters())
            {
                HideElement(m_ShortcutsTableSearchFilterContainer);
                return;
            }

            ShowElement(m_ShortcutsTableSearchFilterContainer);

            List<string> filters = new List<string>();
            m_ViewController.GetSearchFilters(filters);

            var selectedSearchFilter = m_ViewController.GetSelectedSearchFilter();

            //TODO: when I clear elements like this, do I need to unregister any callbacks registered to them?
            m_SearchFiltersContainer.Clear();
            foreach (var filter in filters)
            {
                var filterElement = new TextElement() {text = filter};
                filterElement.AddToClassList("filterElement");

                if (selectedSearchFilter == filter)
                {
                    filterElement.AddToClassList("active");
                }
                filterElement.RegisterCallback<MouseDownEvent>(OnFilterElementClicked);
                m_SearchFiltersContainer.Add(filterElement);
            }
        }

        void OnFilterElementClicked(MouseDownEvent evt)
        {
            var filterElement = (TextElement)evt.target;
            m_ViewController.SetSelectedSearchFilter(filterElement.text);
        }

        void BuildVisualElementHierarchyRoot()
        {
            m_Root = new VisualElement(){name = "ShortcutManagerView"};
            var headerTemplate = EditorResources.Load("UXML/ShortcutManager/ShortcutManagerView.uxml", typeof(UnityEngine.Object)) as VisualTreeAsset;
            headerTemplate.CloneTree(m_Root);
            var header = m_Root.Q("header");
            var keyboardContainer = m_Root.Q("headerAndKeyboardContainer");
            var searchRowContainer = m_Root.Q("searchRowContainer");
            var categoryContainer = m_Root.Q("categoryContainer");
            var shortcutsTableContainer = m_Root.Q("shortcutsTableContainer");
            m_ShortcutsTableSearchFilterContainer = shortcutsTableContainer.Q("shortcutsTableSearchFilterContainer");
            m_SearchFiltersContainer = shortcutsTableContainer.Q("searchFiltersContainer");


            m_KeyboardElement = new Keyboard(m_BindingStateProvider, m_ViewController.GetSelectedKey(), m_ViewController.GetSelectedEventModifiers());
            m_KeyboardElement.DragPerformed += OnKeyboardKeyDragPerformed;
            m_KeyboardElement.CanDrop += CanEntryBeAssignedToKey;
            //m_KeyboardElement.KeySelectedAction += KeySelected;
            m_KeyboardElement.TooltipProvider += GetToolTipForKey;
            m_KeyboardElement.ContextMenuProvider += GetContextMenuForKey;


            m_CategoryTreeView = new ListView((IList)m_ViewController.GetCategories(), k_ListItemHeight, MakeItemForCategoriesTable, BindCategoriesTableItem) { name = "categoryTreeView"};
            m_ShortcutsTable = new ListView((IList)m_ViewController.GetShortcutList(), k_ListItemHeight,  MakeItemForShortcutTable, BindShortcutEntryItem) {name = "shortcutsTable"};

            m_CategoryTreeView.selectedIndex = m_ViewController.selectedCategoryIndex;
            m_CategoryTreeView.onSelectionChange += CategorySelectionChanged;

            m_ShortcutsTable.Q<ScrollView>().showVertical = true;
            m_ShortcutsTable.onSelectionChange += ShortcutSelectionChanged;
            m_ShortcutsTable.onItemsChosen += ShortcutTableEntryChosen;
            m_ShortcutsTable.RegisterCallback<MouseDownEvent>(ShortcutTableRightClickDown);
            m_ShortcutsTable.RegisterCallback<MouseUpEvent>(ShortcutTableRightClickUp);
            m_ShortcutsTable.RegisterCallback<GeometryChangedEvent>(ShortcutTableGeometryChanged);

            m_Root.AddToClassList("ShortcutManagerView");
            if (EditorGUIUtility.isProSkin)
                m_Root.AddToClassList("isProSkin");
            m_Root.AddStyleSheetPath("StyleSheets/ShortcutManager/ShortcutManagerView.uss");

            // Set up resize handle manipulator
            // TODO: replace k_PixelsPadding with width of the text from the left header element after UXML/USS migration.
            m_HorizontalColumnDragger = new HorizontalColumnDragger(k_PixelsPadding, m_ShortcutsTable);
            var shortcutsTableResizeHandle = m_Root.Q("shortcutResizeHandle");
            shortcutsTableResizeHandle.AddManipulator(m_HorizontalColumnDragger);

            BuildProfileManagementRow(header);
            BuildLegendRow(header);

            BuildSearchField(searchRowContainer);

            shortcutsTableContainer.Add(m_ShortcutsTable);

            categoryContainer.Add(m_CategoryTreeView);
            keyboardContainer.Add(m_KeyboardElement);

            SetLocalizedText();
        }

        void SetLocalizedText()
        {
            m_Root.Q<TextElement>("categoryTableHeaderName").text = L10n.Tr("Category");
            m_Root.Q<TextElement>("shortcutsTableHeaderName").text = L10n.Tr("Command");
            m_Root.Q<TextElement>("shortcutsTableHeaderBindings").text = L10n.Tr("Shortcut");
            m_Root.Q<TextElement>("searchLabel").text = L10n.Tr("Search:");
        }

        GenericMenu GetContextMenuForEntries(IEnumerable<ShortcutEntry> entries)
        {
            if (entries == null || !entries.Any())
                return null;

            var menu = new GenericMenu();

            foreach (var entry in entries)
            {
                // Change / to : here to avoid deep submenu nesting
                var mangledPath = entry.displayName.Replace('/', ':');
                // Replace "/" with "Slash" in binding name to avoid submenu nesting
                var mangledBinding = entry.combinations.FirstOrDefault().ToString().Replace("/", "Slash");
                var rootItemLabel = $"{mangledPath} ({mangledBinding})";
                if (entry.overridden)
                    menu.AddItem(new GUIContent($"{rootItemLabel}/{L10n.Tr("Reset to default")}"), false, (x) => { m_ViewController.ResetToDefault(entry);}, entry);
                else
                    menu.AddDisabledItem(new GUIContent($"{rootItemLabel}/{L10n.Tr("Reset to default")}"));
                menu.AddItem(new GUIContent($"{rootItemLabel}/{L10n.Tr("Remove shortcut")}"), false, (x) => { m_ViewController.RemoveBinding(entry);}, entry);
            }

            return menu;
        }

        private GenericMenu GetContextMenuForKey(KeyCode keyCode, EventModifiers modifiers)
        {
            return GetContextMenuForEntries(m_ViewController.GetShortcutsBoundTo(keyCode, modifiers));
        }

        string GetToolTipForKey(KeyCode keyCode, EventModifiers modifiers)
        {
            var entries = m_ViewController.GetShortcutsBoundTo(keyCode, modifiers);
            if (entries == null || entries.Count == 0)
                return null;

            var builder = new StringBuilder();
            foreach (var entry in entries)
            {
                builder.AppendLine(entry.displayName);
            }

            // Trim last empty lines
            if (builder.Length > 0)
            {
                while (builder[builder.Length - 1] == '\r' || builder[builder.Length - 1] == '\n' || builder[builder.Length - 1] == '\t')
                    builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        void ShortcutTableEntryChosen(IEnumerable<object> objects)
        {
            var entry = (ShortcutEntry)objects.First();
            var row = m_ShortcutsTable.Query<VisualElement>().Checked().First();
            StartRebind(entry, row);
        }

        void StartRebind(ShortcutEntry entry, VisualElement row)
        {
            m_EditingBindings = entry;

            if (row != null)
            {
                var bindingContainer = row.Q(className: "binding-container");
                var textElement = bindingContainer.Q<TextElement>();
                var bindingInput = bindingContainer.Q<ShortcutTextField>();
                var warningIcon = bindingContainer.Q(className: "warning-icon");
                ShowElement(bindingInput);
                HideElement(textElement);
                HideElement(warningIcon);
                bindingInput.RegisterCallback<GeometryChangedEvent>(FocusElementDelayed);
            }
        }

        static void FocusElementDelayed(GeometryChangedEvent evt)
        {
            var element = (VisualElement)evt.target;
            element.Q(TextField.textInputUssName).Focus();
            element.UnregisterCallback<GeometryChangedEvent>(FocusElementDelayed);
        }

        void EndRebind(bool refresh = true)
        {
            if (m_EditingBindings == null)
                return;

            m_EditingBindings = null;
            //TODO: this refresh causes issues when trying to double click another binding, while a binding is being edited.
            if (refresh)
                m_ShortcutsTable.Refresh();

            // Rows are recreated when exiting the text field
            // Request update of positions to ensure rows match the column headers
            m_HorizontalColumnDragger.RequestUpdatePositions();
        }

        void OnSearchStringChanged(ChangeEvent<string> evt)
        {
            m_ViewController.SetSearch(evt.newValue);
        }

        void SearchByBindingsChanged(List<KeyCombination> newBindingSearch)
        {
            m_ViewController.SetBindingSearch(newBindingSearch);
        }

        void SearchOptionSelected(SearchOption searchOptionArg)
        {
            var newValue = (SearchOption)searchOptionArg;
            if (m_ViewController.searchMode != newValue)
            {
                m_ViewController.searchMode = newValue;
                m_SearchTextField.value = "";
                m_KeyBindingSearchField.value = new List<KeyCombination>();
                ShowAppropriateSearchField();
            }
        }

        void ShowAppropriateSearchField()
        {
            switch (m_ViewController.searchMode)
            {
                case SearchOption.Name:
                    ShowElement(m_SearchTextField);
                    HideElement(m_KeyBindingSearchField);
                    break;
                case SearchOption.Binding:
                    HideElement(m_SearchTextField);
                    ShowElement(m_KeyBindingSearchField);
                    break;
            }
        }

        void ShortcutSelectionChanged(IEnumerable<object> selection)
        {
            if (selection.Any())
            {
                var newSelection = (ShortcutEntry)selection.First();
                if (newSelection != m_ViewController.selectedEntry)
                {
                    m_ViewController.ShortcutEntrySelected(newSelection);
                    EndRebind();
                }
            }
        }

        void ShortcutTableRightClickDown(MouseDownEvent evt)
        {
            var slider = m_ShortcutsTable.Q<Scroller>(className: Scroller.verticalVariantUssClassName);
            var clickedIndex = (int)((evt.localMousePosition.y + slider.value) / m_ShortcutsTable.resolvedItemHeight);

            if (evt.button != (int)MouseButton.RightMouse)
                return;

            if (clickedIndex > m_ShortcutsTable.itemsSource.Count - 1)
                return;

            m_ShortcutsTable.selectedIndex = clickedIndex;
        }

        void ShortcutTableRightClickUp(MouseUpEvent evt)
        {
            if (evt.button != 1 || m_ViewController.selectedEntry == null)
                return;
            GenericMenu menu = GetContextMenuForEntries(new[] { m_ViewController.selectedEntry });
            menu.ShowAsContext();
        }

        void ShortcutTableGeometryChanged(GeometryChangedEvent evt)
        {
            m_HorizontalColumnDragger.UpdatePositions();
        }

        bool CanEntryBeAssignedToKey(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry)
        {
            return m_ViewController.CanEntryBeAssignedToKey(keyCode, eventModifier, entry);
        }

        void OnKeyboardKeyDragPerformed(KeyCode keyCode, EventModifiers eventModifier, ShortcutEntry entry)
        {
            m_ViewController.DragEntryAndDropIntoKey(keyCode, eventModifier, entry);
        }

        void OnMouseDownCategoryTable(MouseDownEvent evt)
        {
            m_MouseDownStartPos = evt.localMousePosition;
            m_StartedDrag = false;
            var visualElement = ((VisualElement)evt.currentTarget);

            visualElement.RegisterCallback<MouseMoveEvent>(OnMouseMoveCategoryTable);
            visualElement.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveWhileButtonDownCategoryTable);
        }

        void OnMouseLeaveWhileButtonDownCategoryTable(MouseLeaveEvent evt)
        {
            StartDrag((VisualElement)evt.currentTarget);
        }

        void OnMouseUpCategoryTable(MouseUpEvent evt)
        {
            m_StartedDrag = false;
            m_MouseDownStartPos = Vector2.zero;
            var visualElement = ((VisualElement)evt.currentTarget);
            visualElement.UnregisterCallback<MouseMoveEvent>(OnMouseMoveCategoryTable);
            visualElement.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveWhileButtonDownCategoryTable);
        }

        void OnMouseMoveCategoryTable(MouseMoveEvent evt)
        {
            if ((m_MouseDownStartPos - evt.localMousePosition).sqrMagnitude > 9.0)
            {
                StartDrag((VisualElement)evt.currentTarget);
            }
        }

        void StartDrag(VisualElement target)
        {
            if (m_StartedDrag)
                return;

            target.UnregisterCallback<MouseMoveEvent>(OnMouseMoveCategoryTable);
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveWhileButtonDownCategoryTable);
            m_StartedDrag = true;
            DragAndDrop.activeControlID = target.GetHashCode(); //TODO: how to handle activeControlID in UIELements
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("ShortcutCommandItem", m_ViewController.GetShortcutList()[m_ShortcutsTable.selectedIndex]);
            DragAndDrop.StartDrag("Assign command to key");
        }
    }

    class Key : TextElement
    {
        public KeyCode key;

        public Key(KeyCode k, string displayName)
        {
            key = k;
            name = k.ToString();
            text = displayName;

            //StyleUtility.StyleKey(this);
        }
    }

    class Keyboard : VisualElement
    {
        struct KeyDef
        {
            public KeyCode keycode;
            public string displayName;

            public KeyDef(KeyCode kc)
            {
                keycode = kc;
                displayName = kc.ToString();
            }

            public KeyDef(KeyCode kc, string name)
            {
                keycode = kc;
                displayName = name;
            }
        }

        const int k_KeySize = 34;

        EventModifiers m_CurrentModifiers;
        Key m_SelectedKey;

        [NonSerialized]
        Dictionary<EventModifiers, List<Key>> m_ModifierKeys = new Dictionary<EventModifiers, List<Key>>();

        [NonSerialized]
        List<Key> m_AllKeys = new List<Key>();

        IKeyBindingStateProvider m_KeyBindingStateProvider;

        internal event Action<KeyCode, EventModifiers> KeySelectedAction;
        internal event Action<KeyCode, EventModifiers, ShortcutEntry> DragPerformed;
        internal event Func<KeyCode, EventModifiers, ShortcutEntry, bool> CanDrop;
        internal event Func<KeyCode, EventModifiers, string> TooltipProvider;
        internal event Func<KeyCode, EventModifiers, GenericMenu> ContextMenuProvider;


        public Keyboard(IKeyBindingStateProvider keyBindingStateProvider, KeyCode initiallySelectedKey = KeyCode.None, EventModifiers initiallyActiveModifiers = EventModifiers.None)
        {
            m_KeyBindingStateProvider = keyBindingStateProvider;
            KeyDef[] bottomRow;

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                bottomRow = new[]
                {
                    new KeyDef(KeyCode.LeftAlt, "Option"),
                    new KeyDef(KeyCode.LeftCommand, "Command"),
                    new KeyDef(KeyCode.Space),
                    new KeyDef(KeyCode.RightCommand, "Command"),
                    new KeyDef(KeyCode.RightAlt, "Option"),
                };
            }
            else
            {
                bottomRow = new[]
                {
                    new KeyDef(KeyCode.LeftControl, "Control"),
                    new KeyDef(KeyCode.LeftAlt, "Alt"),
                    new KeyDef(KeyCode.Space),
                    new KeyDef(KeyCode.RightAlt, "Alt"),
                    new KeyDef(KeyCode.RightControl, "Control"),
                };
            }

            var keysList = new List<KeyDef[]>
            {
                new[]
                {
                    new KeyDef(KeyCode.Escape, "Esc"),
                    new KeyDef(KeyCode.None),
                    new KeyDef(KeyCode.F1),
                    new KeyDef(KeyCode.F2),
                    new KeyDef(KeyCode.F3),
                    new KeyDef(KeyCode.F4),
                    new KeyDef(KeyCode.None),
                    new KeyDef(KeyCode.F5),
                    new KeyDef(KeyCode.F6),
                    new KeyDef(KeyCode.F7),
                    new KeyDef(KeyCode.F8),
                    new KeyDef(KeyCode.None),
                    new KeyDef(KeyCode.F9),
                    new KeyDef(KeyCode.F10),
                    new KeyDef(KeyCode.F11),
                    new KeyDef(KeyCode.F12)
                },
                new[]
                {
                    new KeyDef(KeyCode.BackQuote, "`"),
                    new KeyDef(KeyCode.Alpha1, "1"),
                    new KeyDef(KeyCode.Alpha2, "2"),
                    new KeyDef(KeyCode.Alpha3, "3"),
                    new KeyDef(KeyCode.Alpha4, "4"),
                    new KeyDef(KeyCode.Alpha5, "5"),
                    new KeyDef(KeyCode.Alpha6, "6"),
                    new KeyDef(KeyCode.Alpha7, "7"),
                    new KeyDef(KeyCode.Alpha8, "8"),
                    new KeyDef(KeyCode.Alpha9, "9"),
                    new KeyDef(KeyCode.Alpha0, "0"),
                    new KeyDef(KeyCode.Minus, "-"),
                    new KeyDef(KeyCode.Equals, "="),
                    new KeyDef(KeyCode.Backspace, "←")
                },
                new[]
                {
                    new KeyDef(KeyCode.Tab),
                    new KeyDef(KeyCode.Q),
                    new KeyDef(KeyCode.W),
                    new KeyDef(KeyCode.E),
                    new KeyDef(KeyCode.R),
                    new KeyDef(KeyCode.T),
                    new KeyDef(KeyCode.Y),
                    new KeyDef(KeyCode.U),
                    new KeyDef(KeyCode.I),
                    new KeyDef(KeyCode.O),
                    new KeyDef(KeyCode.P),
                    new KeyDef(KeyCode.LeftBracket, "["),
                    new KeyDef(KeyCode.RightBracket, "]"),
                    new KeyDef(KeyCode.Backslash, "\\")
                },
                new[]
                {
                    new KeyDef(KeyCode.CapsLock, "Caps Lock"),
                    new KeyDef(KeyCode.A),
                    new KeyDef(KeyCode.S),
                    new KeyDef(KeyCode.D),
                    new KeyDef(KeyCode.F),
                    new KeyDef(KeyCode.G),
                    new KeyDef(KeyCode.H),
                    new KeyDef(KeyCode.J),
                    new KeyDef(KeyCode.K),
                    new KeyDef(KeyCode.L),
                    new KeyDef(KeyCode.Semicolon, ";"),
                    new KeyDef(KeyCode.Quote, "'"),
                    new KeyDef(KeyCode.Return),
                },
                new[]
                {
                    new KeyDef(KeyCode.LeftShift, "Shift"),
                    new KeyDef(KeyCode.Z),
                    new KeyDef(KeyCode.X),
                    new KeyDef(KeyCode.C),
                    new KeyDef(KeyCode.V),
                    new KeyDef(KeyCode.B),
                    new KeyDef(KeyCode.N),
                    new KeyDef(KeyCode.M),
                    new KeyDef(KeyCode.Comma, ","),
                    new KeyDef(KeyCode.Period, "."),
                    new KeyDef(KeyCode.Slash, "/"),
                    new KeyDef(KeyCode.RightShift, "Shift"),
                },
                bottomRow,
            };

            var cursorControlKeysList = new List<KeyDef[]>()
            {
                new[]
                {
                    new KeyDef(KeyCode.F13),
                    new KeyDef(KeyCode.F14),
                    new KeyDef(KeyCode.F15)
                },
                new[]
                {
                    new KeyDef(KeyCode.Insert, "Ins"),
                    new KeyDef(KeyCode.Home, "Hom"),
                    new KeyDef(KeyCode.PageUp, "Pg Up")
                },
                new[]
                {
                    new KeyDef(KeyCode.Delete, "Del"),
                    new KeyDef(KeyCode.End),
                    new KeyDef(KeyCode.PageDown, "Pg Dn")
                },
                new[]
                {
                    new KeyDef(KeyCode.None),
                },
                new[]
                {
                    new KeyDef(KeyCode.None),
                    new KeyDef(KeyCode.UpArrow, "↑"),
                    new KeyDef(KeyCode.None),
                },
                new[]
                {
                    new KeyDef(KeyCode.LeftArrow, "←"),
                    new KeyDef(KeyCode.DownArrow, "↓"),
                    new KeyDef(KeyCode.RightArrow, "→"),
                }
            };

            var dictionaryKeyStyle = new Dictionary<KeyCode, string>()
            {
                {KeyCode.Backspace, "flexible"},
                {KeyCode.Tab, "flexible"},
                {KeyCode.Slash, "flexible"},
                {KeyCode.CapsLock, "flexible"},
                {KeyCode.Return, "flexible"},
                {KeyCode.LeftShift, "flexible"},
                {KeyCode.RightShift, "flexible"},
                {KeyCode.LeftControl, "flexible"},
                {KeyCode.LeftWindows, "flexible"},
                {KeyCode.LeftCommand, "flexible"},
                {KeyCode.LeftAlt, "flexible"},
                {KeyCode.Space, "space"},
                {KeyCode.RightAlt, "flexible"},
                {KeyCode.RightWindows, "flexible"},
                {KeyCode.RightControl, "flexible"},
                {KeyCode.RightCommand, "flexible"},
            };

            var mainContainer = new VisualElement() { name = "fullKeyboardContainer" };

            var compactContainer = new VisualElement() { name = "compactKeyboardContainer" };
            var cursorControlContainer = new VisualElement() { name = "cursorControlKeyboardContainer" };

            BuildKeyboardVisualTree(keysList, dictionaryKeyStyle, compactContainer);

            BuildKeyboardVisualTree(cursorControlKeysList, dictionaryKeyStyle, cursorControlContainer);

            compactContainer.style.width = (keysList[0].Length) * k_KeySize;

            focusable = true;
            tabIndex = 0;

            mainContainer.Add(compactContainer);
            mainContainer.Add(cursorControlContainer);

            Add(mainContainer);

            m_CurrentModifiers = initiallyActiveModifiers;
            var keyElement = m_AllKeys.Find(el => el.key == initiallySelectedKey);
            m_SelectedKey = keyElement;

            mainContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            RegisterCallback<ExecuteCommandEvent>(HandleModifierKeysCommand);
            RegisterCallback<ValidateCommandEvent>(HandleModifierKeysCommand);

            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerfom);

            RegisterCallback<TooltipEvent>(OnTooltip);

            RefreshKeyboardBoundVisuals();
        }

        void HandleModifierKeysCommand<T>(CommandEventBase<T> evt) where T : CommandEventBase<T>, new()
        {
            if (evt.commandName == EventCommandNames.ModifierKeysChanged)
            {
                SetModifiers(evt.imguiEvent.modifiers);
                evt.StopPropagation();
            }
        }

        void OnTooltip(TooltipEvent evt)
        {
            if (TooltipProvider == null)
                return;

            var keyElement = evt.target as Key;
            if (keyElement == null)
                return;

            evt.rect = keyElement.worldBound;
            evt.tooltip = TooltipProvider(keyElement.key, m_CurrentModifiers);
        }

        void OnDragEnter(DragEnterEvent evt)
        {
            DoDragUpdate(evt.target);
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            DoDragUpdate(evt.target);
        }

        void DoDragUpdate(IEventHandler target)
        {
            var keyElement = target as Key;
            if (keyElement == null)
                return;

            var shortcutEntry = (ShortcutEntry)DragAndDrop.GetGenericData("ShortcutCommandItem");
            if (shortcutEntry == null)
                return;

            if (CanDrop != null && CanDrop(keyElement.key, m_CurrentModifiers, shortcutEntry))
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        void OnDragPerfom(DragPerformEvent evt)
        {
            var keyElement = (Key)evt.target;
            var shortcutEntry = (ShortcutEntry)DragAndDrop.GetGenericData("ShortcutCommandItem");
            if (DragPerformed != null)
                DragPerformed(keyElement.key, m_CurrentModifiers, shortcutEntry);
        }

        void BuildKeyboardVisualTree(List<KeyDef[]> keysList, Dictionary<KeyCode, string> dictionaryKeyStyle, VisualElement container)
        {
            foreach (var rowKey in keysList)
            {
                var keyRow = new VisualElement();

                foreach (var keyDef in rowKey)
                {
                    if (keyDef.keycode != KeyCode.None)
                    {
                        var keyElement = new Key(keyDef.keycode, keyDef.displayName);

                        string klass;
                        if (dictionaryKeyStyle.TryGetValue(keyDef.keycode, out klass))
                        {
                            keyElement.AddToClassList(klass);
                        }

                        if (m_KeyBindingStateProvider.IsModifier(keyDef.keycode))
                        {
                            EventModifiers modifier = m_KeyBindingStateProvider.ModifierFromKeyCode(keyDef.keycode);
                            List<Key> keyList;
                            if (!m_ModifierKeys.TryGetValue(modifier, out keyList))
                            {
                                keyList = new List<Key>(2);
                                m_ModifierKeys.Add(modifier, keyList);
                            }
                            keyList.Add(keyElement);
                            keyElement.AddToClassList("modifier");
                        }

                        if (m_KeyBindingStateProvider.IsReservedKey(keyDef.keycode))
                        {
                            keyElement.AddToClassList("reserved");
                        }

                        keyRow.Add(keyElement);
                        m_AllKeys.Add(keyElement);
                    }
                    else
                    {
                        var spacer = new VisualElement();
                        spacer.AddToClassList("flexible");
                        keyRow.Add(spacer);
                    }
                }

                keyRow.Children().Last().AddToClassList("last");

                container.Add(keyRow);
            }

            container.Children().First().AddToClassList("first");

            foreach (var child in container.Children())
            {
                child.AddToClassList("keyRow");
            }
        }

        void RefreshKeyboardBoundVisuals()
        {
            foreach (var key in m_AllKeys)
            {
                StyleKey(key);
            }
        }

        void StyleKey(Key key)
        {
            if (!m_KeyBindingStateProvider.CanBeSelected(key.key))
                return;

            var bindingState = m_KeyBindingStateProvider.GetBindingStateForKeyWithModifiers(key.key, m_CurrentModifiers);

            key.RemoveFromClassList("global");
            key.RemoveFromClassList("contextuallyBound");

            if (bindingState == BindingState.BoundGlobally)
                key.AddToClassList("global");
            else if (bindingState == BindingState.BoundToContext)
                key.AddToClassList("contextuallyBound");
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            SetModifiers(evt.modifiers);
            evt.StopPropagation();
        }

        void OnKeyUp(KeyUpEvent evt)
        {
            SetModifiers(evt.modifiers);
            evt.StopPropagation();
        }

        void SetModifiers(EventModifiers modifiers)
        {
            if (modifiers == m_CurrentModifiers)
                return;

            m_CurrentModifiers = modifiers;

            foreach (var modifierPair in m_ModifierKeys)
            {
                var modifierPressed = (m_CurrentModifiers & modifierPair.Key) == modifierPair.Key;
                foreach (var modifierKey in modifierPair.Value)
                {
                    if (modifierPressed)
                        modifierKey.AddToClassList("active");
                    else
                        modifierKey.RemoveFromClassList("active");
                }
            }

            RefreshKeyboardBoundVisuals();
            var selectedKey = m_SelectedKey != null ? m_SelectedKey.key : KeyCode.None;
            KeySelectedAction?.Invoke(selectedKey, m_CurrentModifiers);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            panel.focusController.SwitchFocus(this);
            var keyElement = evt.target as Key;
            if (keyElement != null)
            {
                switch (evt.button)
                {
                    case 0:
                        SetKeySelected(keyElement);
                        break;
                    case 1:
                        var menu = ContextMenuProvider?.Invoke(keyElement.key, m_CurrentModifiers);
                        menu?.DropDown(keyElement.worldBound);
                        break;
                    default:
                        break;
                }
            }
        }

        void SetKeySelected(Key keyElement)
        {
            if (!m_KeyBindingStateProvider.CanBeSelected(keyElement.key))
            {
                EventModifiers modifier = m_KeyBindingStateProvider.ModifierFromKeyCode(keyElement.key);
                if (modifier != EventModifiers.None)
                {
                    var newModifiers = m_CurrentModifiers ^ modifier;
                    SetModifiers(newModifiers);
                }

                return;
            }

            if (KeySelectedAction == null)
                return;

            var prevSelectedKey = m_SelectedKey;
            m_SelectedKey = keyElement;

            if (prevSelectedKey != null)
            {
                prevSelectedKey.RemoveFromClassList("selected");
            }

            KeySelectedAction(m_SelectedKey.key, m_CurrentModifiers);
            m_SelectedKey.AddToClassList("selected");
        }

        public void Refresh()
        {
            RefreshKeyboardBoundVisuals();
        }
    }

    class ShortcutTextField : TextInputBaseField<List<KeyCombination>>
    {
        List<KeyCombination> m_WorkingValue = new List<KeyCombination>();
        public VisualElement input => textInputBase;
        public List<KeyCombination> WorkingValue => m_WorkingValue;

        public event Action<List<KeyCombination>> OnWorkingValueChanged;
        public event Action OnCancel;

        const int maxChordLength = 1;
        HashSet<KeyCode> m_KeyDown = new HashSet<KeyCode>();

        private class ShortcutInput : TextInputBase
        {
            public override bool isPasswordField => false;
        }

        public ShortcutTextField() : base(kMaxLengthNone, char.MinValue, new ShortcutInput())
        {
            rawValue = new List<KeyCombination>();

            RegisterEvents(visualInput);
        }

        private void RegisterEvents(VisualElement input)
        {
            input.RegisterCallback<KeyDownEvent>(OnKeyDown);
            input.RegisterCallback<KeyUpEvent>(OnKeyUp);
            input.RegisterCallback<FocusEvent>((evt) => {
                StartNewCombination();
                evt.StopPropagation();
                textInputBase.editorEngine.MoveTextEnd();
            });
            input.RegisterCallback<BlurEvent>((evt) =>
            {
                Apply();
                evt.StopPropagation();
                textInputBase.editorEngine.MoveTextEnd();
            });
        }

        void OnKeyDown(KeyDownEvent kde)
        {
            if (kde.keyCode != KeyCode.None)
            {
                if (!m_KeyDown.Contains(kde.keyCode))
                {
                    //TODO: call ShortcutManagerWindowViewController.IsModifier instead
                    m_KeyDown.Add(kde.keyCode);

                    if (kde.keyCode == KeyCode.Escape)
                    {
                        Revert();
                        OnCancel?.Invoke();
                        kde.StopPropagation();
                    }
                    else if (kde.keyCode == KeyCode.Return)
                    {
                        Apply();
                        kde.StopPropagation();
                    }
                    else if (!ShortcutManagerWindowViewController.IsModifier(kde.keyCode))
                    {
                        AppendKeyCombination(kde.keyCode, kde.modifiers);
                        if (WorkingValue.Count == maxChordLength)
                        {
                            Apply();
                        }
                        kde.StopPropagation();
                    }
                }
            }
            textInputBase.editorEngine.MoveTextEnd();
        }

        void OnKeyUp(KeyUpEvent kue)
        {
            m_KeyDown.Remove(kue.keyCode);
            kue.StopPropagation();
            textInputBase.editorEngine.MoveTextEnd();
        }

        void InvokeWorkingValueChanged()
        {
            OnWorkingValueChanged?.Invoke(m_WorkingValue);
        }

        void AppendKeyCombination(KeyCode keyCode, EventModifiers modifiers)
        {
            m_WorkingValue.Add(KeyCombination.FromKeyboardInput(keyCode, modifiers));
            text = KeyCombination.SequenceToString(m_WorkingValue);
            InvokeWorkingValueChanged();
        }

        void StartNewCombination()
        {
            m_WorkingValue.Clear();
            text = "";
            InvokeWorkingValueChanged();
        }

        void Apply()
        {
            value = m_WorkingValue;
            if (hasFocus)
                focusController.SwitchFocus(null);
        }

        void Revert()
        {
            RevertWithoutNotify();
            InvokeWorkingValueChanged();
        }

        public void RevertWithoutNotify()
        {
            m_WorkingValue.Clear();
            m_WorkingValue.AddRange(value);
            text = KeyCombination.SequenceToString(m_WorkingValue);
        }

        public override void SetValueWithoutNotify(List<KeyCombination> newValue)
        {
            if (!ReferenceEquals(rawValue, newValue))
            {
                rawValue.Clear();
                if (newValue != null)
                    rawValue.AddRange(newValue);
            }

            text = KeyCombination.SequenceToString(rawValue);
            RevertWithoutNotify();


            if (!string.IsNullOrEmpty(viewDataKey))
                SaveViewData();
            MarkDirtyRepaint();
        }
    }

    internal class ShortcutSearchField : SearchFieldBase<ShortcutTextField, List<KeyCombination>>
    {
        public new static readonly string ussClassName = "unity-shortcut-search-field";
        public new class UxmlFactory : UxmlFactory<ShortcutSearchField> {}

        public event Action<List<KeyCombination>> OnWorkingValueChanged;

        private void ForwardWorkingValueChanged(List<KeyCombination> workingValue)
        {
            OnWorkingValueChanged?.Invoke(workingValue);
        }

        public ShortcutSearchField()
        {
            AddToClassList(ussClassName);

            textInputField.OnWorkingValueChanged += ForwardWorkingValueChanged;
        }

        protected override void ClearTextField()
        {
            value = null;
        }

        protected override bool FieldIsEmpty(List<KeyCombination> fieldValue)
        {
            return value == null || !value.Any();
        }
    }

    internal class ShortcutPopupSearchField : ShortcutSearchField, IToolbarMenuElement
    {
        public new class UxmlFactory : UxmlFactory<ShortcutPopupSearchField> {}

        public DropdownMenu menu { get; }

        public ShortcutPopupSearchField()
        {
            AddToClassList(popupVariantUssClassName);

            menu = new DropdownMenu();
            searchButton.clickable.clicked += this.ShowMenu;
        }
    }

    internal class HorizontalColumnDragger : MouseManipulator
    {
        const string k_ShortcutmanagerColumnposition = "ShortcutManager_ColumnPosition";
        Vector2 m_Start;
        bool m_Active;
        float m_ClampPositionPadding;
        ListView m_Elements;
        float m_TargetPosition;
        float m_VerticalScrollbarWidth;
        bool m_UpdatePositionsRequested;

        public HorizontalColumnDragger(float clampPositionPadding, ListView elements)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_Active = false;

            // TODO: Replace this with the maximum text width of the left and right TextElements
            m_ClampPositionPadding = clampPositionPadding;
            m_Elements = elements;

            m_Elements.Q(className: ScrollView.vScrollerUssClassName).RegisterCallback<GeometryChangedEvent>(ShortcutsTableGeometryChanged);
        }

        void ShortcutsTableGeometryChanged(GeometryChangedEvent evt)
        {
            var element = (VisualElement)evt.target;
            m_VerticalScrollbarWidth = element.layout.width;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                m_Start = e.localMousePosition;
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        void PersistHandlePosition()
        {
            EditorPrefs.SetFloat(k_ShortcutmanagerColumnposition, m_TargetPosition);
        }

        public void UpdatePositions()
        {
            if (m_TargetPosition == 0)
            {
                m_TargetPosition = EditorPrefs.GetFloat(k_ShortcutmanagerColumnposition, target.parent.layout.width / 2);
            }

            m_TargetPosition = Mathf.Clamp(m_TargetPosition, m_ClampPositionPadding, target.parent.layout.width - m_ClampPositionPadding - target.style.width.value.value);
            UpdateElements(m_TargetPosition);

            m_UpdatePositionsRequested = false;
        }

        public void RequestUpdatePositions()
        {
            m_UpdatePositionsRequested = true;
        }

        public void UpdatePositionsIfRequested()
        {
            if (m_UpdatePositionsRequested)
                UpdatePositions();
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active || !target.HasMouseCapture())
                return;

            Vector2 diff = e.localMousePosition - m_Start;

            m_TargetPosition = target.layout.x + diff.x;
            m_TargetPosition = Mathf.Clamp(m_TargetPosition, m_ClampPositionPadding, target.parent.layout.width - m_ClampPositionPadding - target.style.width.value.value);

            UpdateElements(m_TargetPosition);

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                return;

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
            PersistHandlePosition();
        }

        void UpdateElements(float handlePosition)
        {
            target.style.left = m_TargetPosition;
            UpdateElement(target.parent, handlePosition, 0);

            foreach (var el in m_Elements.Children())
            {
                UpdateElement(el, handlePosition, m_VerticalScrollbarWidth);
            }
        }

        void UpdateElement(VisualElement el, float handlePosition, float offset)
        {
            var totalPadding = el.ElementAt(0).computedStyle.paddingLeft.value.value + el.ElementAt(0).computedStyle.paddingRight.value.value + el.ElementAt(1).computedStyle.paddingLeft.value.value + el.ElementAt(1).computedStyle.paddingRight.value.value;
            var percentage = (handlePosition + (target.computedStyle.width.value.value) / 2 - totalPadding / 2) / (target.parent.layout.width - (totalPadding + offset));

            el.ElementAt(0).style.flexGrow = percentage;
            el.ElementAt(1).style.flexGrow = 1 - percentage;
        }
    }
}
