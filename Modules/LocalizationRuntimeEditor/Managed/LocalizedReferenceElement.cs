// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

abstract class LocalizedReferenceElement : VisualElement
{
    const string k_Uxml = "LocalizationRuntime/UXML/LocalizedReferenceDrawer.uxml";
    const string k_RefButtonUxml = "LocalizationRuntime/UXML/LocalizedReferenceButton.uxml";
    const string k_DetailUxml = "LocalizationRuntime/UXML/LocalizedReferenceDetail.uxml";
    internal const string StyleSheet = "LocalizationRuntime/StyleSheets/LocalizedReferenceDrawer.uss";

    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_Skeleton;
    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_RefButton;
    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_Detail;

    readonly SerializedProperty m_Property;
    readonly SerializedObject m_Object;
    readonly int m_Depth;

    readonly SerializedProperty m_TableRefProp;
    readonly SerializedProperty m_TableEntryRefProp;
    readonly SerializedProperty m_KeyId;
    readonly SerializedProperty m_KeyName;
    readonly SerializedProperty m_EnableFallback;

    Foldout m_Foldout;
    Button m_RefButton;
    Label m_RefNone;
    Label m_RefCollection;
    Label m_RefSeparator;
    Label m_RefKey;
    VisualElement m_WarningSlot;
    VisualElement m_CreateRow;
    VisualElement m_TogglesRow;
    VisualElement m_EntryNameRow;
    TextField m_EntryNameField;
    VisualElement m_EntryNameWarning;
    VisualElement m_ValuesSection;
    Label m_ValuesCount;
    VisualElement m_ValuesHeaderTrailing;
    ListView m_Rail;
    VisualElement m_DetailContainer;
    VisualElement m_ExtrasSlot;

    ResourceTableCollection m_Collection;
    SharedTableData.SharedTableEntry m_SharedEntry;
    string m_SelectedLocaleCode;

    protected ResourceTableCollection Collection => m_Collection;
    protected SharedTableData.SharedTableEntry SharedEntry => m_SharedEntry;
    protected SerializedObject Serialized => m_Object;
    protected SerializedProperty Property => m_Property;
    protected int Depth => m_Depth;

    /// <summary>Label shown in the reference button when nothing is referenced.</summary>
    protected abstract string NoneLabel { get; }

    // The same values live under different names on a serialized LocalizedString and on the UxmlSerializedData the
    // binding generates, so the layout is a parameter rather than an assumption.
    private protected readonly struct Layout
    {
        public readonly string Table;
        public readonly string Entry;
        public readonly string Fallback;
        public readonly string Variables;
        public readonly string VariableValue;
        // The UXML shape stores a UxmlSerializedData wherever the serialized shape stores the value itself.
        public readonly bool IsUxmlData;

        public Layout(string table, string entry, string fallback, string variables, string variableValue, bool isUxmlData)
        {
            Table = table;
            Entry = entry;
            Fallback = fallback;
            Variables = variables;
            VariableValue = variableValue;
            IsUxmlData = isUxmlData;
        }

        public static Layout Reference => new("m_TableReference", "m_TableEntryReference", "m_EnableFallback", "m_LocalVariables.m_Variables", "m_Value", false);
        public static Layout UxmlData => new("Table", "Entry", "Fallback", "Variables", "Value", true);
    }

    private protected Layout Paths { get; }

    protected LocalizedReferenceElement(SerializedProperty property, string label, int depth)
        : this(property, label, depth, Layout.Reference)
    {
    }

    private protected LocalizedReferenceElement(SerializedProperty property, string label, int depth, Layout paths)
    {
        m_Property = property.Copy();
        m_Object = property.serializedObject;
        m_Depth = depth;
        Paths = paths;

        m_TableRefProp = property.FindPropertyRelative(paths.Table);
        m_TableEntryRefProp = property.FindPropertyRelative(paths.Entry);
        m_KeyId = property.FindPropertyRelative($"{paths.Entry}.m_KeyId");
        m_KeyName = property.FindPropertyRelative($"{paths.Entry}.m_Key");
        m_EnableFallback = property.FindPropertyRelative(paths.Fallback);

        LoadSkeleton();
        LocStyles.Apply(this, StyleSheet);
        InjectReferenceButton(label);
        WireEntryName();
        BuildToggles();

        // Custom-built content is not auto-bound, so rebuild it when an undo/redo changes the underlying data.
        RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
        RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);
        // Subclasses call RebuildBody() at the end of their constructor, once their own state is set.
    }

    bool SerializedDataAlive => m_Property.isValid && m_Object.targetObject != null;

    void OnUndoRedo()
    {
        if (SerializedDataAlive)
            RebuildBody();
    }

    void LoadSkeleton()
    {
        s_Skeleton ??= EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
        s_Skeleton?.CloneTree(this);

        m_Foldout = this.Q<Foldout>("root-foldout");
        m_WarningSlot = this.Q("warning-slot");
        m_CreateRow = this.Q("create-row");
        m_TogglesRow = this.Q("toggles-row");
        m_EntryNameRow = this.Q("entry-name-row");
        m_EntryNameField = this.Q<TextField>("entry-name-field");
        m_EntryNameWarning = this.Q("entry-name-warning");
        m_ValuesSection = this.Q("values-section");
        m_ValuesCount = this.Q<Label>("values-count");
        m_ValuesHeaderTrailing = this.Q("values-header-trailing");
        m_Rail = this.Q<ListView>("values-rail");
        m_DetailContainer = this.Q("values-detail");
        m_ExtrasSlot = this.Q("extras-slot");

        // Static text lives in UXML but must translate with the editor language, so set it through L10n.Tr here.
        this.Q<Label>("entry-name-label").text = L10n.Tr("Entry name", null);
        this.Q<Label>("values-label").text = L10n.Tr("Values", null);
        m_EntryNameWarning.tooltip = L10n.Tr("Name is empty or already used; the key was not renamed.", null);

        ConfigureRail();
    }

    void ConfigureRail()
    {
        m_Rail.selectionType = SelectionType.Single;
        m_Rail.fixedItemHeight = 24;
        m_Rail.bindItem = (element, index) =>
        {
            var source = m_Rail.itemsSource;
            if (source == null || index < 0 || index >= source.Count || m_Collection == null || m_SharedEntry == null)
                return;
            var locale = source[index] as Locale;
            if (locale == null)
                return;
            var localeLabel = element.Q<Label>("rail-locale");
            localeLabel.text = string.IsNullOrEmpty(locale.LocaleName) ? locale.Code : locale.LocaleName;
            localeLabel.tooltip = LocaleTitle(locale);
            var preview = element.Q<Label>("rail-preview");
            var notable = element.Q<Label>("rail-notable");
            var table = m_Collection.GetTable(locale.Identifier);
            var hasTable = table != null;
            var value = hasTable ? LocalePreview(locale, table) : null;
            var empty = string.IsNullOrEmpty(value);
            preview.text = !hasTable ? L10n.Tr("No value", null) : (empty ? L10n.Tr("(empty)", null) : value);
            preview.EnableInClassList(LocClasses.LsPreviewEmpty, empty);
            notable.text = L10n.Tr("⚠ No table", null);
            notable.style.display = hasTable ? DisplayStyle.None : DisplayStyle.Flex;
        };
        m_Rail.selectionChanged += _ =>
        {
            if (m_Rail.selectedItem is Locale locale)
            {
                m_SelectedLocaleCode = locale.Code;
                RefreshDetail();
            }
        };
    }

    void InjectReferenceButton(string label)
    {
        if (m_Foldout != null)
            m_Foldout.text = label;

        s_RefButton ??= EditorGUIUtility.LoadRequired(k_RefButtonUxml) as VisualTreeAsset;
        if (s_RefButton == null)
            return;
        m_RefButton = s_RefButton.Instantiate().Q<Button>("ref-button");
        m_RefButton.RemoveFromHierarchy();
        m_RefButton.clicked += OpenReferencePicker;
        m_RefNone = m_RefButton.Q<Label>("ref-none");
        m_RefCollection = m_RefButton.Q<Label>("ref-collection");
        m_RefSeparator = m_RefButton.Q<Label>("ref-separator");
        m_RefKey = m_RefButton.Q<Label>("ref-key");
        // The button lives in the foldout header, so keep its pointer events from toggling the foldout.
        m_RefButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());

        var header = m_Foldout?.Q<Toggle>();
        if (header != null)
            header.Add(m_RefButton);
        else
            Insert(0, m_RefButton);
    }

    void WireEntryName()
    {
        m_EntryNameField.RegisterValueChangedCallback(evt =>
            m_EntryNameWarning.style.display = IsInvalidRename(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None);
        m_EntryNameField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                m_EntryNameField.Blur();
        });
        m_EntryNameField.RegisterCallback<FocusOutEvent>(_ => CommitRename());
    }

    protected void RebuildBody()
    {
        m_Object.Update();
        var reference = TableRef();
        m_Collection = ResolveCollection(reference);
        m_SharedEntry = ResolveSharedEntry(m_Collection);
        UpdateRefButton();

        m_WarningSlot.Clear();
        if (HasReference(reference) && (m_Collection == null || m_SharedEntry == null))
        {
            var message = m_Collection == null
                ? string.Format(L10n.Tr("Could not find Table Collection: {0}", null), ReferenceCollectionLabel(reference))
                : L10n.Tr("Could not find the referenced entry in this collection.", null);
            m_WarningSlot.Add(new HelpBox(message, HelpBoxMessageType.Warning));
        }

        BuildCreateRow();

        var resolved = m_Collection != null && m_SharedEntry != null;
        m_EntryNameRow.style.display = resolved ? DisplayStyle.Flex : DisplayStyle.None;
        m_ValuesSection.style.display = resolved ? DisplayStyle.Flex : DisplayStyle.None;
        m_ExtrasSlot.style.display = resolved ? DisplayStyle.Flex : DisplayStyle.None;
        if (!resolved)
        {
            m_Rail.itemsSource = null;
            m_ExtrasSlot.Clear();
            return;
        }

        m_EntryNameField.SetValueWithoutNotify(m_SharedEntry.Key);
        m_EntryNameWarning.style.display = DisplayStyle.None;
        BuildValues();

        m_ExtrasSlot.Clear();
        BuildExtras(m_ExtrasSlot);
    }

    void UpdateRefButton()
    {
        if (m_RefButton == null)
            return;
        var keyName = m_SharedEntry != null ? m_SharedEntry.Key : m_KeyName.stringValue;
        var unset = m_Collection == null || string.IsNullOrEmpty(keyName);

        m_RefNone.text = NoneLabel;
        m_RefNone.style.display = unset ? DisplayStyle.Flex : DisplayStyle.None;
        var reference = unset ? DisplayStyle.None : DisplayStyle.Flex;
        m_RefCollection.style.display = reference;
        m_RefSeparator.style.display = reference;
        m_RefKey.style.display = reference;

        if (unset)
        {
            m_RefButton.tooltip = string.Empty;
            return;
        }
        m_RefCollection.text = m_Collection.TableCollectionName;
        m_RefKey.text = keyName;
        m_RefButton.tooltip = $"{m_Collection.TableCollectionName} / {keyName}";
    }

    void OpenReferencePicker()
        => Search.LocalizedReferencePicker.Show(NoneLabel, FilterEntryInterface, FilterAssetType, (collection, keyId) =>
        {
            if (!SerializedDataAlive)
                return;
            SetReference(collection, keyId);
            RebuildBody();
        });

    // Applied by the search provider rather than as query text, so the user cannot widen past what the field accepts.
    Type FilterEntryInterface
        => typeof(IStringEntry).IsAssignableFrom(EntryType) ? typeof(IStringEntry)
            : typeof(IAssetEntry).IsAssignableFrom(EntryType) ? typeof(IAssetEntry)
            : typeof(IResourceEntry);

    protected virtual Type FilterAssetType => null;

    void SetReference(ResourceTableCollection collection, long keyId)
    {
        m_Object.Update();
        m_TableRefProp.boxedValue = collection != null ? AssetProviderEditors.CreateReference(collection) : default(TableReference);
        m_TableEntryRefProp.boxedValue = collection != null ? (TableEntryReference)keyId : default(TableEntryReference);
        m_Object.ApplyModifiedProperties();
    }

    TableReference TableRef() => (TableReference)m_TableRefProp.boxedValue;

    ResourceTableCollection ResolveCollection(TableReference reference)
    {
        var guid = reference.TableCollectionNameGuid;
        var name = reference.TableCollectionName;
        if (guid.Empty() && string.IsNullOrEmpty(name))
            return null;
        foreach (var collection in AssetProviderEditors.GetKnownCollections())
        {
            if (collection == null || collection.SharedData == null)
                continue;
            if (!guid.Empty() && collection.SharedData.TableCollectionNameGuid == guid)
                return collection;
            if (!string.IsNullOrEmpty(name) && collection.TableCollectionName == name)
                return collection;
        }
        return null;
    }

    SharedTableData.SharedTableEntry ResolveSharedEntry(ResourceTableCollection collection)
    {
        var shared = collection != null ? collection.SharedData : null;
        if (shared == null)
            return null;
        var id = m_KeyId.longValue;
        if (id != 0)
            return shared.GetEntry(id);
        var name = m_KeyName.stringValue;
        return !string.IsNullOrEmpty(name) ? shared.GetEntry(name) : null;
    }

    bool HasReference(TableReference reference)
        => !reference.IsEmpty || m_KeyId.longValue != 0 || !string.IsNullOrEmpty(m_KeyName.stringValue);

    static string ReferenceCollectionLabel(TableReference reference)
        => !string.IsNullOrEmpty(reference.TableCollectionName) ? reference.TableCollectionName : reference.TableCollectionNameGuid.ToString();

    void BuildCreateRow()
    {
        m_CreateRow.Clear();

        var newEntry = new Button(OpenNewEntry) { text = L10n.Tr("New entry…", null) };
        newEntry.tooltip = L10n.Tr("Create a new key in a table collection and point this reference at it.", null);
        m_CreateRow.Add(newEntry);

        if (m_Collection != null)
        {
            var edit = new Button(() => ResourceTablesWindow.ShowWindow(m_Collection)) { text = L10n.Tr("Edit", null) };
            edit.tooltip = L10n.Tr("Open the referenced collection in the Resource Tables window.", null);
            edit.AddToClassList(LocClasses.LsCreateRowTrailing);
            m_CreateRow.Add(edit);
        }
        else
        {
            var create = new Button(() => ResourceTablesWindow.ShowWindow(null)) { text = L10n.Tr("Create table collection", null) };
            create.AddToClassList(LocClasses.LsCreateRowTrailing);
            m_CreateRow.Add(create);
        }
    }

    void OpenNewEntry()
    {
        NewEntryPopup.Show(m_RefButton.worldBound, m_Collection, (collection, name) =>
        {
            if (!SerializedDataAlive)
                return;
            var id = LocalizationTableAuthoring.CreateEntry(collection, name, EntryType);
            if (id != 0)
                SetReference(collection, id);
            RebuildBody();
        });
    }

    /// <summary>The entry kind this drawer creates for a new key (for example <c>StringEntry</c> or <c>AssetEntry</c>).</summary>
    protected abstract Type EntryType { get; }

    void BuildToggles()
    {
        m_TogglesRow.Clear();

        var fallback = new Toggle(L10n.Tr("Enable fallback", null)) { tooltip = L10n.Tr("When the selected locale has no value, resolve through its fallback chain.", null) };
        fallback.BindProperty(m_EnableFallback);
        m_TogglesRow.Add(fallback);
    }

    bool IsInvalidRename(string value)
    {
        if (m_SharedEntry == null || m_Collection == null)
            return false;
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return true;
        if (trimmed == m_SharedEntry.Key)
            return false;
        return m_Collection.SharedData.GetEntry(trimmed) != null;
    }

    void CommitRename()
    {
        if (m_SharedEntry == null || m_Collection == null)
            return;
        var trimmed = m_EntryNameField.value?.Trim();
        if (IsInvalidRename(m_EntryNameField.value) || trimmed == m_SharedEntry.Key)
        {
            m_EntryNameField.SetValueWithoutNotify(m_SharedEntry.Key);
            return;
        }
        var shared = m_Collection.SharedData;
        Undo.RegisterCompleteObjectUndo(shared, "Rename Entry");
        if (shared.RenameKey(m_SharedEntry.Id, trimmed))
        {
            EditorUtility.SetDirty(shared);
            LocalizationEditorSettings.RaiseCollectionsChanged();
        }
        RebuildBody();
    }

    void BuildValues()
    {
        var locales = ProjectLocales();
        var source = new List<Locale>(locales.Count);
        for (var i = 0; i < locales.Count; i++)
        {
            if (locales[i] != null)
                source.Add(locales[i]);
        }
        m_ValuesCount.text = $"({source.Count})";

        m_ValuesHeaderTrailing.Clear();
        BuildValuesHeaderTrailing(m_ValuesHeaderTrailing);

        m_DetailContainer.Clear();

        if (locales.Count == 0)
        {
            m_Rail.style.display = DisplayStyle.None;
            m_Rail.itemsSource = null;
            m_DetailContainer.Add(new HelpBox(L10n.Tr("No locales in the project. Add locales in Project Settings > Localization.", null), HelpBoxMessageType.Info));
            return;
        }

        if (source.Count == 0)
        {
            m_Rail.style.display = DisplayStyle.None;
            m_Rail.itemsSource = null;
            m_DetailContainer.Add(new HelpBox(L10n.Tr("No locales in the project resolve. Repair them in Project Settings > Localization.", null), HelpBoxMessageType.Warning));
            return;
        }

        if (string.IsNullOrEmpty(m_SelectedLocaleCode) || FindLocale(m_SelectedLocaleCode) == null)
            m_SelectedLocaleCode = DefaultLocaleCode(source);

        m_Rail.style.display = DisplayStyle.Flex;
        m_Rail.style.height = Mathf.Min(source.Count, 6) * m_Rail.fixedItemHeight;
        m_Rail.itemsSource = source;
        m_Rail.RefreshItems();
        m_Rail.SetSelectionWithoutNotify(new[] { source.FindIndex(l => l != null && l.Code == m_SelectedLocaleCode) });

        RefreshDetail();
    }

    protected void RefreshDetail()
    {
        if (m_DetailContainer == null)
            return;
        m_DetailContainer.Clear();

        if (string.IsNullOrEmpty(m_SelectedLocaleCode))
            return;
        var locale = FindLocale(m_SelectedLocaleCode);
        if (locale == null)
            return;

        s_Detail ??= EditorGUIUtility.LoadRequired(k_DetailUxml) as VisualTreeAsset;
        if (s_Detail == null)
            return;
        s_Detail.CloneTree(m_DetailContainer);
        var detail = m_DetailContainer.Q("detail");
        detail.Q<Label>("detail-title").text = LocaleTitle(locale);
        WireMetadataButton(detail.Q<Button>("detail-metadata"));

        var table = m_Collection.GetTable(locale.Identifier);
        if (table == null)
        {
            detail.Add(new HelpBox(string.Format(L10n.Tr("No {0} table exists in this collection yet.", null), locale.Code), HelpBoxMessageType.Warning));
            var createTable = new Button(() => { LocalizationTableAuthoring.EnsureLocaleTable(m_Collection, locale); RebuildBody(); }) { text = L10n.Tr("Create table", null) };
            createTable.AddToClassList(LocClasses.LsDetailCreateTable);
            detail.Add(createTable);
            return;
        }

        if (m_Collection.SharedData.IsVariant(m_SharedEntry.Id))
        {
            detail.Add(new HelpBox(L10n.Tr("This entry uses variants. Edit its per-variant values in the Resource Tables window.", null), HelpBoxMessageType.Info));
            var open = new Button(() => ResourceTablesWindow.ShowWindow(m_Collection)) { text = L10n.Tr("Open Resource Tables", null) };
            open.AddToClassList(LocClasses.LsDetailCreateTable);
            detail.Add(open);
            return;
        }

        BuildLocaleDetail(detail, locale, table);
    }

    protected void UpdateRailPreview(string localeCode)
    {
        var source = m_Rail?.itemsSource;
        if (source == null)
            return;
        for (var i = 0; i < source.Count; i++)
        {
            if (source[i] is Locale locale && locale.Code == localeCode)
            {
                m_Rail.RefreshItem(i);
                return;
            }
        }
    }

    void WireMetadataButton(Button button)
    {
        LocIcons.Apply(button, LocIcons.Metadata);
        button.tooltip = L10n.Tr("Metadata", null);
        if (m_SharedEntry.Metadata != null && m_SharedEntry.Metadata.HasData)
            button.AddToClassList(LocClasses.LocIconBtnActive);
        button.clicked += () =>
        {
            var shared = m_Collection.SharedData;
            var index = SharedEntryIndex(shared, m_SharedEntry.Id);
            if (index < 0)
                return;
            string path;
            using (var so = new SerializedObject(shared))
                path = so.FindProperty("m_Entries").GetArrayElementAtIndex(index).FindPropertyRelative("m_Metadata").propertyPath;
            MetadataPopup.Show(button.worldBound, $"{L10n.Tr("Metadata", null)}: {m_SharedEntry.Key}", shared, path, MetadataType.SharedTableEntry,
                () => { EditorUtility.SetDirty(shared); RefreshDetail(); });
        };
    }

    static int SharedEntryIndex(SharedTableData shared, long id)
    {
        for (var i = 0; i < shared.Entries.Count; i++)
        {
            if (shared.Entries[i] != null && shared.Entries[i].Id == id)
                return i;
        }
        return -1;
    }

    /// <summary>One-line preview for the rail row (empty for "(empty)").</summary>
    protected abstract string LocalePreview(Locale locale, ResourceTable table);

    /// <summary>Builds the editor for the selected locale (below the locale title and metadata row).</summary>
    protected abstract void BuildLocaleDetail(VisualElement detail, Locale locale, ResourceTable table);

    /// <summary>Fills the trailing slot in the Values header (the string drawer's Smart toggle).</summary>
    protected virtual void BuildValuesHeaderTrailing(VisualElement slot) { }

    /// <summary>Fills the extras slot below the values (the string drawer's Local Variables).</summary>
    protected virtual void BuildExtras(VisualElement slot) { }

    protected static VisualElement Row() => new VisualElement().WithClass(LocClasses.LsRow);

    /// <summary>Shows a single-select dropdown below <paramref name="anchor"/>; the item equal to <paramref name="current"/> is checked.</summary>
    protected static void ShowMenu<T>(Button anchor, IEnumerable<(T value, string label)> items, T current, Action<T> onPick)
    {
        var menu = new GenericDropdownMenu();
        foreach (var (value, label) in items)
        {
            var captured = value;
            menu.AddItem(L10n.Tr(label, null), EqualityComparer<T>.Default.Equals(captured, current), () => onPick(captured));
        }
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    protected IReadOnlyList<Locale> ProjectLocales()
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        return settings != null ? settings.AvailableLocales : Array.Empty<Locale>();
    }

    string DefaultLocaleCode(List<Locale> source)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var project = settings != null ? settings.ProjectLocale : null;
        if (project != null)
        {
            foreach (var locale in source)
            {
                if (locale.Code == project.Code)
                    return locale.Code;
            }
        }
        return source[0].Code;
    }

    protected Locale FindLocale(string code)
    {
        foreach (var locale in ProjectLocales())
        {
            if (locale != null && locale.Code == code)
                return locale;
        }
        return null;
    }

    protected static string LocaleTitle(Locale locale)
        => string.IsNullOrEmpty(locale.LocaleName) ? locale.Code : $"{locale.LocaleName} ({locale.Code})";

    protected static string FirstLine(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var newline = value.IndexOf('\n');
        return newline >= 0 ? value.Substring(0, newline) : value;
    }
}

static class LocalizedDrawerExtensions
{
    public static T WithClass<T>(this T element, UniqueStyleString className) where T : VisualElement
    {
        element.AddToClassList(className);
        return element;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
