// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

partial class ResourceTablesWindow : EditorWindow
{
    const string k_Uxml = "LocalizationRuntime/UXML/ResourceTablesWindow.uxml";
    const string k_KeyCellUxml = "LocalizationRuntime/UXML/ResourceTableKeyCell.uxml";
    const string k_ValueCellUxml = "LocalizationRuntime/UXML/ResourceTableValueCell.uxml";
    const string k_HeaderUxml = "LocalizationRuntime/UXML/ResourceTableColumnHeader.uxml";
    const string k_ActionsCellUxml = "LocalizationRuntime/UXML/ResourceTableActionsCell.uxml";
    const string k_EmptyStateUxml = "LocalizationRuntime/UXML/ResourceTableEmptyState.uxml";
    const string k_ImportExportDirPref = "LocalizationRuntime.ImportExportDir";

    const string k_EntriesField = "m_Entries";
    const string k_ActionsColumnName = "actions";
    const string k_KeyIdField = "m_KeyId";
    const string k_ValueField = "m_Value";
    const string k_VariantsField = "m_Variants";
    const string k_VariantKeyField = "m_Key";
    const string k_MetadataField = "m_Metadata";
    const string k_CollectionNameField = "m_TableCollectionName";

    sealed class VariantRow
    {
        public readonly long KeyId;
        public readonly string VariantKey;
        public readonly bool Last;
        public VariantRow(long keyId, string variantKey, bool last) { KeyId = keyId; VariantKey = variantKey; Last = last; }
    }

    [SerializeField] Dictionary<string, bool> m_HiddenLocales = new();
    [SerializeField] Dictionary<long, bool> m_ExpandedKeys = new();
    [SerializeField] ResourceTableCollection m_Collection;
    [SerializeField] string m_Search = string.Empty;
    [SerializeField] bool m_ShowId;

    long m_RenamingKey;
    readonly Dictionary<long, int> m_KeyItemId = new();
    readonly Dictionary<int, long> m_ItemIdToKey = new();
    readonly Dictionary<ResourceTable, SerializedObject> m_TableSerialized = new();
    readonly Dictionary<ResourceTable, Dictionary<long, int>> m_EntryIndex = new();
    SerializedObject m_SharedNameSerialized;
    TableSearch m_TableSearch;
    ResourceTableCollection m_SearchCollection;

    VisualTreeAsset m_KeyCellTemplate;
    VisualTreeAsset m_ValueCellTemplate;
    VisualTreeAsset m_HeaderTemplate;
    VisualTreeAsset m_ActionsCellTemplate;
    VisualTreeAsset m_EmptyStateTemplate;

    VisualElement m_Root;
    ListView m_CollectionsList;
    VisualElement m_ColumnsContainer;
    ToolbarSearchField m_SearchField;
    Label m_SearchInfo;
    MultiColumnTreeView m_Tree;
    TextField m_NewKeyField;
    TextField m_NameField;
    Button m_AddEntryButton;
    Button m_NewCollectionButton;
    VisualElement m_Info;
    VisualElement m_AddRow;
    string m_LastCollectionName;
    VisualElement m_EmptyHelp;

    [MenuItem("Window/Localization/Resource Tables")]
    public static void ShowWindow() => ShowWindow(null);

    public static void ShowWindow(ResourceTableCollection selected)
    {
        var window = GetWindow<ResourceTablesWindow>();
        window.titleContent = new GUIContent(L10n.Tr("Resource Tables", null), LocIcons.Tex(LocIcons.Table));
        window.minSize = new Vector2(720, 420);
        if (selected != null)
        {
            window.m_Collection = selected;
            window.RefreshAll();
        }
    }

    void OnEnable()
    {
        LocalizationEditorSettings.CollectionsChanged += OnCollectionsChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

    void OnDisable()
    {
        LocalizationEditorSettings.CollectionsChanged -= OnCollectionsChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        DisposeTableSerializedObjects();
        m_SharedNameSerialized?.Dispose();
        m_SharedNameSerialized = null;
    }

    void OnCollectionsChanged()
    {
        RefreshCollectionsList();
        if (m_Collection != null)
            RebuildTree();
    }

    void OnUndoRedoPerformed()
    {
        if (m_Collection != null)
        {
            m_Collection.SharedData?.InvalidateCache();
            foreach (var table in m_Collection.Tables)
                table?.InvalidateCache();
        }
        RebuildTree();
    }

    Object[] UndoTargets()
    {
        var targets = new List<Object>();
        if (m_Collection != null)
            targets.Add(m_Collection);
        if (m_Collection != null && m_Collection.SharedData != null)
            targets.Add(m_Collection.SharedData);
        foreach (var table in Tables())
            targets.Add(table);
        return targets.ToArray();
    }

    void RecordUndo(string operation)
    {
        var targets = UndoTargets();
        if (targets.Length > 0)
            Undo.RegisterCompleteObjectUndo(targets, operation);
    }

    void CreateGUI() => BuildUI(rootVisualElement);

    internal void BuildUI(VisualElement root)
    {
        var rootTemplate = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
        m_KeyCellTemplate = EditorGUIUtility.Load(k_KeyCellUxml) as VisualTreeAsset;
        m_ValueCellTemplate = EditorGUIUtility.Load(k_ValueCellUxml) as VisualTreeAsset;
        m_HeaderTemplate = EditorGUIUtility.Load(k_HeaderUxml) as VisualTreeAsset;
        m_ActionsCellTemplate = EditorGUIUtility.Load(k_ActionsCellUxml) as VisualTreeAsset;
        m_EmptyStateTemplate = EditorGUIUtility.Load(k_EmptyStateUxml) as VisualTreeAsset;

        m_Root = root;
        rootTemplate.CloneTree(root);
        m_NewCollectionButton = root.Q<Button>("new-collection-button");
        m_NewCollectionButton.clicked += CreateCollection;

        m_SearchField = root.Q<ToolbarSearchField>("search-field");
        m_SearchField.tooltip = L10n.Tr("Filter entries. Supports k:key v:value type:string|asset smart:true loc:code and free text.", null);
        m_SearchField.SetValueWithoutNotify(m_Search);
        m_SearchField.RegisterValueChangedCallback(evt => SetSearchQuery(evt.newValue));
        m_SearchInfo = root.Q<Label>("search-info");
        root.Q<Button>("import-export-button").clicked += () => ShowImportExportMenu(root.Q<Button>("import-export-button"));

        m_NewKeyField = root.Q<TextField>("new-key-field");
        m_NewKeyField.RegisterValueChangedCallback(_ => UpdateAddEnabled());
        m_AddRow = root.Q("add-row");
        m_AddEntryButton = root.Q<Button>("add-entry-button");
        m_AddEntryButton.clicked += () => ShowAddEntryMenu(m_AddEntryButton);

        m_CollectionsList = root.Q<ListView>("collections-list");
        m_CollectionsList.bindItem = (element, i) =>
        {
            var collection = m_CollectionsList.itemsSource[i] as ResourceTableCollection;
            element.Q<Label>("name").text = collection != null ? collection.TableCollectionName : "<null>";
        };
        m_CollectionsList.selectionChanged += selection =>
        {
            foreach (var obj in selection)
            {
                m_Collection = obj as ResourceTableCollection;
                RebuildTree();
                break;
            }
        };
        m_ColumnsContainer = root.Q("columns-container");
        m_Info = root.Q("table-info");
        m_NameField = root.Q<TextField>("collection-name-field");
        m_NameField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (m_NameField.value == m_LastCollectionName)
                return;
            m_LastCollectionName = m_NameField.value;
            RefreshCollectionsList();
        });
        m_EmptyHelp = root.Q("empty-help");

        RefreshAll();
    }

    void SetSearchQuery(string query)
    {
        query ??= string.Empty;
        if (query == m_Search)
            return;
        m_Search = query;
        RebuildTree(rebuildSerialized: false);
    }

    void UpdateResultInfo(HashSet<long> visible)
    {
        if (m_SearchInfo == null || !HasCollection || string.IsNullOrWhiteSpace(m_Search))
            return;
        var total = m_Collection.SharedData.Entries.Count;
        m_SearchInfo.text = $"{(visible?.Count ?? total)} {L10n.Tr("of", null)} {total}";
    }

    bool HasCollection => m_Collection != null && m_Collection.SharedData != null;

    IEnumerable<ResourceTable> Tables()
    {
        if (m_Collection == null)
            yield break;

        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
        {
            foreach (var table in m_Collection.Tables)
                if (table != null)
                    yield return table;
            yield break;
        }

        foreach (var locale in settings.AvailableLocales)
        {
            if (locale == null)
                continue;
            var table = m_Collection.GetTable(locale.Identifier);
            if (table != null)
                yield return table;
        }
        foreach (var table in m_Collection.Tables)
            if (table != null && settings.GetLocale(table.LocaleIdentifier) == null)
                yield return table;
    }

    IEnumerable<ResourceTable> VisibleTables()
    {
        foreach (var table in Tables())
            if (!m_HiddenLocales.ContainsKey(table.LocaleIdentifier.Code))
                yield return table;
    }

    int VisibleTableCount()
    {
        var count = 0;
        foreach (var _ in VisibleTables())
            count++;
        return count;
    }

    void RefreshAll()
    {
        if (m_CollectionsList == null)
            return;
        RefreshCollectionsList();
        RebuildTree();
    }

    void RefreshCollectionsList()
    {
        if (m_CollectionsList == null)
            return;

        var collections = AssetProviderEditors.GetKnownCollections();
        m_CollectionsList.itemsSource = collections;
        m_CollectionsList.Rebuild();
        var selectedIndex = m_Collection != null ? collections.IndexOf(m_Collection) : -1;
        if (selectedIndex >= 0)
            m_CollectionsList.SetSelectionWithoutNotify(new List<int> { selectedIndex });
        else
            m_CollectionsList.ClearSelection();

        m_NewCollectionButton?.SetEnabled(LocalizationTableAuthoring.CanCreateCollection(out _));
    }

    void UpdateAddEnabled()
    {
        var key = m_NewKeyField?.value?.Trim();
        var enabled = HasCollection && !string.IsNullOrEmpty(key) && m_Collection.SharedData.GetEntry(key) == null;
        m_AddEntryButton?.SetEnabled(enabled);
    }

    void RebuildTree() => RebuildTree(true);

    void RebuildTree(bool rebuildSerialized)
    {
        if (m_ColumnsContainer == null)
            return;
        m_ColumnsContainer.Clear();
        if (m_SearchInfo != null)
            m_SearchInfo.text = string.Empty;

        BuildInfoRow();
        UpdateAddEnabled();
        m_NewKeyField?.SetEnabled(HasCollection);

        if (!HasCollection)
        {
            ShowNoCollectionState();
            return;
        }
        m_EmptyHelp.style.display = DisplayStyle.None;

        if (m_Collection.SharedData.Entries.Count == 0)
        {
            m_ColumnsContainer.Add(EmptyState(LocIcons.Table, L10n.Tr("No entries yet", null),
                L10n.Tr("Type a key name above and choose Add Entry to create the first entry.", null), null, null));
            return;
        }

        var visible = ComputeVisibleKeys();
        UpdateResultInfo(visible);
        var items = BuildItems(visible, out var matchedAny);
        if (!matchedAny)
        {
            m_ColumnsContainer.Add(EmptyState(LocIcons.Search, L10n.Tr("No entries match your search", null),
                null, L10n.Tr("Clear search", null), () => { m_Search = string.Empty; m_SearchField?.SetValueWithoutNotify(string.Empty); RebuildTree(); }));
            return;
        }

        if (rebuildSerialized || m_TableSerialized.Count == 0)
            RebuildSerializedObjects();

        m_Tree = new MultiColumnTreeView
        {
            showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            reorderable = string.IsNullOrEmpty(m_Search)
        };
        m_Tree.AddToClassList(LocClasses.LocTree);
        m_Tree.RegisterCallback<KeyDownEvent>(OnTreeKeyDown);
        m_Tree.headerContextMenuPopulateEvent += OnHeaderContextMenu;

        // The Key and actions columns are structural: not optional, so the header menu can't hide them.
        m_Tree.columns.Add(new Column
        {
            title = L10n.Tr("Key", null),
            width = 186f,
            optional = false,
            makeHeader = KeyHeader,
            makeCell = MakeKeyCell,
            bindCell = BindKeyCell
        });

        // ID is always a column so it stays toggleable in the header menu; its state persists in m_ShowId.
        var idColumn = new Column
        {
            title = L10n.Tr("ID", null),
            width = 120f,
            visible = m_ShowId,
            makeCell = () => { var label = new Label(); label.AddToClassList(LocClasses.LocIdCell); return label; },
            bindCell = BindIdCell
        };
        idColumn.propertyChanged += (_, args) => { if (args.propertyName == "visible") m_ShowId = idColumn.visible; };
        m_Tree.columns.Add(idColumn);

        foreach (var table in Tables())
        {
            var localeTable = table;
            var code = localeTable.LocaleIdentifier.Code;
            var localeColumn = new Column
            {
                title = code,
                stretchable = true,
                minWidth = 130f,
                width = 200f,
                visible = !m_HiddenLocales.ContainsKey(code),
                makeHeader = () => LocaleHeader(localeTable),
                makeCell = MakeValueCell,
                bindCell = (element, row) => BindLocaleCell(element, localeTable, row)
            };
            localeColumn.propertyChanged += (_, args) =>
            {
                if (args.propertyName != "visible")
                    return;
                if (localeColumn.visible)
                    m_HiddenLocales.Remove(code);
                else
                    m_HiddenLocales[code] = true;
            };
            m_Tree.columns.Add(localeColumn);
        }
        m_Tree.columns.Add(new Column
        {
            name = k_ActionsColumnName,
            title = string.Empty,
            width = 30f,
            optional = false,
            makeCell = MakeActionsCell,
            bindCell = BindActionsCell
        });

        m_Tree.SetRootItems(items);
        m_Tree.Rebuild();
        foreach (var keyId in m_ExpandedKeys.Keys)
            if (m_KeyItemId.TryGetValue(keyId, out var itemId))
                m_Tree.ExpandItem(itemId);
        if (m_Tree.viewController != null)
            m_Tree.viewController.itemIndexChanged += OnRowsReordered;
        m_ColumnsContainer.Add(m_Tree);
    }

    void RebuildSerializedObjects()
    {
        DisposeTableSerializedObjects();
        foreach (var table in Tables())
        {
            var so = new SerializedObject(table);
            m_TableSerialized[table] = so;
            m_EntryIndex[table] = BuildEntryIndex(so);
        }
    }

    void DisposeTableSerializedObjects()
    {
        foreach (var so in m_TableSerialized.Values)
            so?.Dispose();
        m_TableSerialized.Clear();
        m_EntryIndex.Clear();
    }

    SerializedObject GetTableSerialized(ResourceTable table)
    {
        if (table == null)
            return null;
        if (!m_TableSerialized.TryGetValue(table, out var so) || so == null)
        {
            so = new SerializedObject(table);
            m_TableSerialized[table] = so;
        }
        return so;
    }

    SerializedProperty EntryProperty(ResourceTable table, long keyId)
    {
        var so = GetTableSerialized(table);
        var entries = so?.FindProperty(k_EntriesField);
        if (entries == null || !entries.isArray)
            return null;
        if (m_EntryIndex.TryGetValue(table, out var map) && map.TryGetValue(keyId, out var index)
            && index >= 0 && index < entries.arraySize)
        {
            var element = entries.GetArrayElementAtIndex(index);
            var keyProp = element.FindPropertyRelative(k_KeyIdField);
            if (keyProp != null && keyProp.longValue == keyId)
                return element;
            m_EntryIndex[table] = BuildEntryIndex(so);
        }
        return FindEntryProperty(entries, keyId);
    }

    static Dictionary<long, int> BuildEntryIndex(SerializedObject so)
    {
        var map = new Dictionary<long, int>();
        var entries = so.FindProperty(k_EntriesField);
        if (entries == null || !entries.isArray)
            return map;
        for (var i = 0; i < entries.arraySize; i++)
        {
            var keyProp = entries.GetArrayElementAtIndex(i).FindPropertyRelative(k_KeyIdField);
            if (keyProp != null)
                map[keyProp.longValue] = i;
        }
        return map;
    }

    static SerializedProperty FindEntryProperty(SerializedProperty entries, long keyId)
    {
        for (var i = 0; i < entries.arraySize; i++)
        {
            var element = entries.GetArrayElementAtIndex(i);
            var keyProp = element.FindPropertyRelative(k_KeyIdField);
            if (keyProp != null && keyProp.longValue == keyId)
                return element;
        }
        return null;
    }

    void OnTreeKeyDown(KeyDownEvent evt)
    {
        // Backspace/Delete belong to text editing when a field has focus; only treat them as shortcuts otherwise.
        if (evt.target is VisualElement target && (target is TextField || target.GetFirstAncestorOfType<TextField>() != null))
            return;
        if (m_Tree == null || m_Tree.selectedIndex < 0)
            return;
        var data = m_Tree.GetItemDataForIndex<object>(m_Tree.selectedIndex);

        // F2 renames the selected key, matching the Hierarchy and Project windows.
        if (evt.keyCode == KeyCode.F2)
        {
            if (data is long renameId)
            {
                m_RenamingKey = renameId;
                RebuildTree();
                evt.StopPropagation();
            }
            return;
        }

        if (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace)
            return;
        if (data is long keyId)
            DeleteKey(keyId);
        else if (data is VariantRow variantRow)
            RemoveVariant(variantRow);
        evt.StopPropagation();
    }

    void OnRowsReordered(int srcIndex, int dstIndex)
    {
        if (m_Collection == null || m_Collection.SharedData == null || m_Tree == null)
            return;
        var order = new List<long>();
        foreach (var id in m_Tree.GetRootIds())
        {
            if (m_ItemIdToKey.TryGetValue(id, out var keyId))
                order.Add(keyId);
        }
        if (order.Count == 0)
            return;
        RecordUndo("Reorder Keys");
        m_Collection.SharedData.SetKeyOrder(order);
        EditorUtility.SetDirty(m_Collection.SharedData);
        m_Tree.schedule.Execute(RebuildTree);
    }

    void BuildInfoRow()
    {
        var display = HasCollection ? DisplayStyle.Flex : DisplayStyle.None;
        m_Info.style.display = display;
        if (m_AddRow != null)
            m_AddRow.style.display = display;
        m_NameField.Unbind();
        m_LastCollectionName = null;
        m_SharedNameSerialized?.Dispose();
        m_SharedNameSerialized = null;
        if (!HasCollection)
            return;
        var shared = m_Collection.SharedData;
        if (shared != null)
        {
            m_LastCollectionName = shared.TableCollectionName;
            m_SharedNameSerialized = new SerializedObject(shared);
            var nameProp = m_SharedNameSerialized.FindProperty(k_CollectionNameField);
            if (nameProp != null)
                m_NameField.BindProperty(nameProp);
        }
    }

    void ShowNoCollectionState()
    {
        var localeCount = LocalizationEditorSettings.ActiveSettings?.AvailableLocales.Count ?? 0;
        if (localeCount == 0)
        {
            m_EmptyHelp.style.display = DisplayStyle.None;
            m_ColumnsContainer.Add(EmptyState(LocIcons.Table,
                L10n.Tr("This project has no locales yet", null),
                L10n.Tr("Add at least one locale in the Localization settings before creating a table collection.", null),
                L10n.Tr("Open Localization settings", null),
                () => SettingsService.OpenProjectSettings("Project/Localization")));
        }
        else
        {
            m_EmptyHelp.style.display = DisplayStyle.Flex;
        }
    }

    VisualElement EmptyState(string glyphIcon, string title, string hint, string buttonText, Action onButton)
    {
        var root = m_EmptyStateTemplate.Instantiate();
        LocIcons.Apply(root.Q("glyph"), glyphIcon);
        root.Q<Label>("title").text = title;
        var hintLabel = root.Q<Label>("hint");
        if (string.IsNullOrEmpty(hint))
            hintLabel.style.display = DisplayStyle.None;
        else
            hintLabel.text = hint;
        var button = root.Q<Button>("action");
        if (string.IsNullOrEmpty(buttonText) || onButton == null)
            button.style.display = DisplayStyle.None;
        else
        {
            button.text = buttonText;
            button.clicked += onButton;
        }
        return root;
    }

    VisualElement KeyHeader() => Header(L10n.Tr("Key", null), L10n.Tr("Shared metadata and columns", null), ShowKeyHeaderMenu);

    VisualElement LocaleHeader(ResourceTable table)
    {
        var name = LocaleDisplayName(table.LocaleIdentifier.Code);
        var titleText = string.IsNullOrEmpty(name) ? table.LocaleIdentifier.Code : name;
        return Header(titleText, L10n.Tr("Table metadata and columns", null), b => ShowLocaleHeaderMenu(b, table));
    }

    VisualElement Header(string title, string menuTooltip, Action<Button> onMenu)
    {
        var header = m_HeaderTemplate.Instantiate();
        header.Q<Label>("title").text = title;
        var button = header.Q<Button>("menu-button");
        LocIcons.Apply(button, LocIcons.Menu);
        button.tooltip = menuTooltip;
        button.clicked += () => onMenu(button);
        return header;
    }

    void ShowImportExportMenu(Button anchor)
    {
        var menu = new GenericDropdownMenu();
        if (!HasCollection)
        {
            menu.AddDisabledItem(L10n.Tr("Select a collection first", null), false);
            menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
            return;
        }
        foreach (var exporter in TableFormatRegistry.Exporters)
        {
            var format = exporter;
            menu.AddItem($"{L10n.Tr("Export", null)}/{format.DisplayName}", false, () => ExportCollection(format));
        }
        foreach (var importer in TableFormatRegistry.Importers)
        {
            var format = importer;
            menu.AddItem($"{L10n.Tr("Import", null)}/{format.DisplayName} ({L10n.Tr("Merge", null)})", false, () => ImportCollection(format, replace: false));
            menu.AddItem($"{L10n.Tr("Import", null)}/{format.DisplayName} ({L10n.Tr("Replace", null)})", false, () => ImportCollection(format, replace: true));
        }
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    void ExportCollection(ITableCollectionExporter exporter)
    {
        if (!HasCollection)
            return;
        var dir = EditorPrefs.GetString(k_ImportExportDirPref, string.Empty);
        var path = EditorUtility.SaveFilePanel($"{L10n.Tr("Export", null)} {m_Collection.TableCollectionName}", dir, m_Collection.TableCollectionName, exporter.FileExtension);
        if (string.IsNullOrEmpty(path))
            return;
        EditorPrefs.SetString(k_ImportExportDirPref, Path.GetDirectoryName(path));
        var reporter = new EditorProgressBarReporter();
        try
        {
            using var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(false));
            exporter.Export(writer, m_Collection, reporter);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(L10n.Tr("Export failed", null), e.Message, L10n.Tr("OK", null));
            return;
        }
        finally
        {
            reporter.Clear();
        }
        EditorUtility.RevealInFinder(path);
    }

    void ImportCollection(ITableCollectionImporter importer, bool replace)
    {
        if (!HasCollection)
            return;
        var dir = EditorPrefs.GetString(k_ImportExportDirPref, string.Empty);
        var path = EditorUtility.OpenFilePanel($"{L10n.Tr("Import into", null)} {m_Collection.TableCollectionName}", dir, importer.FileExtension);
        if (string.IsNullOrEmpty(path))
            return;
        EditorPrefs.SetString(k_ImportExportDirPref, Path.GetDirectoryName(path));
        var options = new TableImportOptions
        {
            CreateUndo = true,
            RemoveMissingEntries = replace,
            CreateMissingKeys = true,
            CreateMissingLocaleTables = true
        };
        var reporter = new EditorProgressBarReporter();
        try
        {
            using var reader = new StreamReader(path);
            importer.ImportInto(reader, m_Collection, options, reporter);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(L10n.Tr("Import failed", null), e.Message, L10n.Tr("OK", null));
        }
        finally
        {
            reporter.Clear();
            // Importers can mutate the collection before throwing; refresh even on failure.
            m_Collection.SharedData?.InvalidateCache();
            RebuildTree();
        }
    }

    void ShowKeyHeaderMenu(Button anchor)
    {
        var menu = new GenericDropdownMenu();
        menu.AddItem(L10n.Tr("Shared metadata…", null), false, () =>
            MetadataPopup.Show(anchor.worldBound, $"{L10n.Tr("Shared metadata", null)}: {m_Collection.TableCollectionName}", m_Collection.SharedData, "m_Metadata", MetadataType.SharedTableData,
                () => { EditorUtility.SetDirty(m_Collection.SharedData); RebuildTree(); }));
        AddColumnItems(menu);
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    void AddColumnItems(GenericDropdownMenu menu)
    {
        menu.AddItem($"{L10n.Tr("Columns", null)}/{L10n.Tr("ID", null)}", m_ShowId, () => { m_ShowId = !m_ShowId; RebuildTree(); });
        var visible = VisibleTableCount();
        foreach (var table in Tables())
        {
            var code = table.LocaleIdentifier.Code;
            var isVisible = !m_HiddenLocales.ContainsKey(code);
            var name = LocaleDisplayName(code);
            var label = string.IsNullOrEmpty(name) ? $"{L10n.Tr("Columns", null)}/{code}" : $"{L10n.Tr("Columns", null)}/{code} · {name}";
            if (isVisible && visible <= 1)
                menu.AddDisabledItem(label, true);
            else
                menu.AddItem(label, isVisible, () => { ToggleLocale(code); RebuildTree(); });
        }

        var existing = new HashSet<string>();
        foreach (var table in Tables())
            existing.Add(table.LocaleIdentifier.Code);
        var settings = LocalizationEditorSettings.ActiveSettings;
        var anyMissing = false;
        if (settings != null)
        {
            foreach (var locale in settings.AvailableLocales)
            {
                if (locale == null || existing.Contains(locale.Code))
                    continue;
                anyMissing = true;
                var captured = locale;
                var name = LocaleDisplayName(locale.Code);
                var label = string.IsNullOrEmpty(name) ? $"{L10n.Tr("Add locale", null)}/{locale.Code}" : $"{L10n.Tr("Add locale", null)}/{locale.Code} · {name}";
                menu.AddItem(label, false, () => AddLocaleToCollection(captured));
            }
        }
        if (!anyMissing)
            menu.AddDisabledItem($"{L10n.Tr("Add locale", null)}/{L10n.Tr("(all project locales added)", null)}", false);
    }

    static void OnHeaderContextMenu(ContextualMenuPopulateEvent evt, Column column)
    {
        var items = evt.menu.MenuItems();
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] is DropdownMenuAction action && action.name == k_ActionsColumnName)
            {
                items.RemoveAt(i);
                break;
            }
        }
    }

    void ShowLocaleHeaderMenu(Button anchor, ResourceTable table)
    {
        var code = table.LocaleIdentifier.Code;
        var menu = new GenericDropdownMenu();
        menu.AddItem(L10n.Tr("Table metadata…", null), false, () =>
            MetadataPopup.Show(anchor.worldBound, $"{L10n.Tr("Table metadata", null)}: {code}", table, "m_Metadata", MetadataType.ResourceTable,
                () => { EditorUtility.SetDirty(table); RebuildTree(); }));

        var visible = VisibleTableCount();
        if (visible > 1)
            menu.AddItem(L10n.Tr("Hide column", null), false, () => { ToggleLocale(code); RebuildTree(); });
        else
            menu.AddDisabledItem(L10n.Tr("Hide column", null), false);
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    void ToggleLocale(string code)
    {
        if (!m_HiddenLocales.Remove(code))
            m_HiddenLocales[code] = true;
    }

    static string LocaleDisplayName(string code)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var locale = settings?.GetLocale(new LocaleIdentifier(code));
        return locale?.LocaleName;
    }

    List<TreeViewItemData<object>> BuildItems(HashSet<long> visible, out bool matchedAny)
    {
        var roots = new List<TreeViewItemData<object>>();
        m_KeyItemId.Clear();
        m_ItemIdToKey.Clear();
        matchedAny = false;
        var id = 0;
        foreach (var sharedEntry in m_Collection.SharedData.Entries)
        {
            var keyId = sharedEntry.Id;
            if (visible != null && !visible.Contains(keyId))
                continue;
            matchedAny = true;
            var variantKeys = AuthoredVariantKeys(keyId);
            List<TreeViewItemData<object>> children = null;
            if (variantKeys != null && variantKeys.Count > 0)
            {
                children = new List<TreeViewItemData<object>>();
                for (var i = 0; i < variantKeys.Count; i++)
                    children.Add(new TreeViewItemData<object>(id++, new VariantRow(keyId, variantKeys[i], i == variantKeys.Count - 1)));
            }
            var parentId = id++;
            m_KeyItemId[keyId] = parentId;
            m_ItemIdToKey[parentId] = keyId;
            roots.Add(new TreeViewItemData<object>(parentId, keyId, children));
        }
        return roots;
    }

    HashSet<long> ComputeVisibleKeys()
    {
        if (!HasCollection || string.IsNullOrWhiteSpace(m_Search))
            return null;
        if (m_TableSearch == null || m_SearchCollection != m_Collection)
        {
            m_TableSearch = new TableSearch(m_Collection);
            m_SearchCollection = m_Collection;
        }
        return m_TableSearch.Filter(m_Search);
    }

    void CreateCollection()
    {
        if (!LocalizationTableAuthoring.CanCreateCollection(out var reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        var path = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create resource table collection", null), "New Table Collection", "asset",
            L10n.Tr("Choose where to save the collection and its per-locale tables.", null));
        if (string.IsNullOrEmpty(path))
            return;

        var collection = LocalizationTableAuthoring.CreateCollection(
            Path.GetDirectoryName(path)?.Replace('\\', '/'), Path.GetFileNameWithoutExtension(path));
        if (collection == null)
            return;

        m_Collection = collection;
        RefreshAll();
    }

    void AddLocaleToCollection(Locale locale)
    {
        if (!HasCollection || locale == null || m_Collection.GetTable(locale.Identifier) != null)
            return;
        LocalizationTableAuthoring.EnsureLocaleTable(m_Collection, locale);
        RefreshCollectionsList();
        RebuildTree();
    }

    static Label Styled(string text, params string[] classes)
    {
        var label = new Label(text);
        foreach (var c in classes)
            label.AddToClassList(c);
        return label;
    }
}
