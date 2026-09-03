// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.PersistentVariables;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomPropertyDrawer(typeof(LocalizedString))]
class LocalizedStringDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
        => new LocalizedStringElement(property, string.IsNullOrEmpty(preferredLabel) ? property.displayName : preferredLabel, 0);
}

class LocalizedStringElement : LocalizedReferenceElement
{
    const int MaxDepth = 4;
    const string k_SmartToggleUxml = "LocalizationRuntime/UXML/LocalizedStringSmartToggle.uxml";
    const string k_ModeTabsUxml = "LocalizationRuntime/UXML/LocalizedStringModeTabs.uxml";
    const string k_PreviewUxml = "LocalizationRuntime/UXML/LocalizedStringPreview.uxml";

    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_SmartToggle;
    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_ModeTabs;
    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_Preview;

    [NoAutoStaticsCleanup] // fixed table built by its own initializer, not a cache
    static readonly (Type type, string label)[] VariableTypes =
    {
        (typeof(BoolVariable), "Bool"),
        (typeof(IntVariable), "Int"),
        (typeof(UIntVariable), "UInt"),
        (typeof(LongVariable), "Long"),
        (typeof(FloatVariable), "Float"),
        (typeof(DoubleVariable), "Double"),
        (typeof(StringVariable), "String"),
        (typeof(ObjectVariable), "Object"),
        (typeof(LocalizedString), "Localized String"),
    };

    enum Mode { Edit, Debug, Preview }

    Mode m_Mode = Mode.Edit;

    public LocalizedStringElement(SerializedProperty property, string label, int depth)
        : base(property, label, depth) => RebuildBody();

    private protected LocalizedStringElement(SerializedProperty property, string label, int depth, Layout paths)
        : base(property, label, depth, paths) => RebuildBody();

    protected override string NoneLabel => L10n.Tr("None (String)", null);

    protected override Type EntryType => typeof(StringEntry);

    protected override string LocalePreview(Locale locale, ResourceTable table)
        => FirstLine((table.GetEntry(SharedEntry.Id) as StringEntry)?.Value);

    protected override void BuildValuesHeaderTrailing(VisualElement slot)
    {
        s_SmartToggle ??= EditorGUIUtility.LoadRequired(k_SmartToggleUxml) as VisualTreeAsset;
        if (s_SmartToggle == null)
            return;
        s_SmartToggle.CloneTree(slot);

        slot.Q("smart-row").tooltip = L10n.Tr("Smart String: formatted through Smart Strings at runtime. Applies to every locale for this entry.", null);
        slot.Q<Label>("smart-badge").tooltip = L10n.Tr("Smart String", null);
        slot.Q<Label>("smart-label").text = L10n.Tr("Smart", null);

        var toggle = slot.Q<Toggle>("smart-toggle");
        toggle.value = SharedEntry.IsSmart;
        toggle.RegisterValueChangedCallback(evt =>
        {
            var shared = Collection.SharedData;
            Undo.RegisterCompleteObjectUndo(shared, "Toggle Smart String");
            SharedEntry.IsSmart = evt.newValue;
            EditorUtility.SetDirty(shared);
            RefreshDetail();
        });
    }

    protected override void BuildLocaleDetail(VisualElement detail, Locale locale, ResourceTable table)
    {
        var existing = table.GetEntry(SharedEntry.Id);
        if (existing != null && existing is not IStringEntry)
        {
            detail.Add(new HelpBox(L10n.Tr("This key is an asset entry in this locale, not a string.", null), HelpBoxMessageType.Warning));
            return;
        }

        if (!SharedEntry.IsSmart)
        {
            detail.Add(BuildValueEditor(table, CurrentValue(table)));
            return;
        }
        BuildModeTabs(detail, table);
    }

    void BuildModeTabs(VisualElement detail, ResourceTable table)
    {
        s_ModeTabs ??= EditorGUIUtility.LoadRequired(k_ModeTabsUxml) as VisualTreeAsset;
        if (s_ModeTabs == null)
        {
            detail.Add(BuildValueEditor(table, CurrentValue(table)));
            return;
        }
        s_ModeTabs.CloneTree(detail);

        var tabs = detail.Q<TabView>("mode-tabs");
        var edit = PrepareModeTab(tabs, "mode-edit", Mode.Edit, L10n.Tr("Edit", null));
        var debug = PrepareModeTab(tabs, "mode-debug", Mode.Debug, L10n.Tr("Debug", null));
        var preview = PrepareModeTab(tabs, "mode-preview", Mode.Preview, L10n.Tr("Preview", null));

        tabs.activeTab = m_Mode switch
        {
            Mode.Debug => debug,
            Mode.Preview => preview,
            _ => edit,
        };
        tabs.activeTabChanged += (_, _) => ShowActiveMode(tabs, table);
        ShowActiveMode(tabs, table);
    }

    static Tab PrepareModeTab(TabView tabs, string name, Mode mode, string label)
    {
        var tab = tabs.Q<Tab>(name);
        tab.label = label;
        tab.userData = mode;
        return tab;
    }

    void ShowActiveMode(TabView tabs, ResourceTable table)
    {
        var tab = tabs.activeTab;
        if (tab == null)
            return;
        m_Mode = (Mode)tab.userData;
        var value = CurrentValue(table);
        tab.Clear();
        tab.Add(m_Mode switch
        {
            Mode.Debug => BuildDebug(value),
            Mode.Preview => BuildPreview(table, value),
            _ => BuildValueEditor(table, value),
        });
    }

    string CurrentValue(ResourceTable table) => (table.GetEntry(SharedEntry.Id) as StringEntry)?.Value ?? string.Empty;

    VisualElement BuildValueEditor(ResourceTable table, string value)
    {
        var field = new TextField { multiline = true, value = value };
        field.AddToClassList(LocClasses.LsValueField);
        // One undo step per editing session, capturing the pre-edit state.
        field.RegisterCallback<FocusInEvent>(_ => Undo.RegisterCompleteObjectUndo(table, "Edit Localized Value"));
        field.RegisterValueChangedCallback(evt =>
        {
            table.AddStringEntry(SharedEntry.Key, evt.newValue);
            EditorUtility.SetDirty(table);
            UpdateRailPreview(table.LocaleIdentifier.Code);
        });
        return field;
    }

    VisualElement BuildDebug(string value)
    {
        var box = new VisualElement().WithClass(LocClasses.LsDebug);
        var formatter = Formatter();
        Format parsed = null;
        try
        {
            parsed = formatter.Parser.ParseFormat(value);
            foreach (var item in parsed.Items)
            {
                var text = item.RawText;
                var segment = new Label(string.IsNullOrEmpty(text) ? " " : text).WithClass(LocClasses.LsDebugSeg);
                if (item is Placeholder)
                    segment.AddToClassList(LocClasses.LsDebugPlaceholder);
                box.Add(segment);
            }
        }
        catch (Exception e)
        {
            box.Add(new Label(e.Message));
        }
        finally
        {
            parsed?.Dispose();
        }
        return box;
    }

    VisualElement BuildPreview(ResourceTable table, string value)
    {
        string text;
        try
        {
            var live = LiveLocalizedString();
            text = ResourceDatabase.Format(Formatter(), value, Array.Empty<object>(), live?.LocalVariables);
        }
        catch (Exception e)
        {
            text = e.Message;
        }

        s_Preview ??= EditorGUIUtility.LoadRequired(k_PreviewUxml) as VisualTreeAsset;
        if (s_Preview == null)
            return new Label(text);
        var container = s_Preview.Instantiate();
        container.Q<Label>("preview-text").text = text;
        container.Q<Label>("preview-hint").text = L10n.Tr("Formatted with local variables", null);
        return container;
    }

    protected override void BuildExtras(VisualElement slot)
    {
        var varsArray = Property.FindPropertyRelative(Paths.Variables);
        if (varsArray == null)
            return;

        var listView = new ListView
        {
            showFoldoutHeader = true,
            headerTitle = L10n.Tr("Local variables", null),
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single,
        };
        listView.AddToClassList(LocClasses.LsVarlist);
        var path = varsArray.propertyPath;
        listView.makeItem = () => new VisualElement();
        listView.bindItem = (element, index) =>
        {
            var array = Serialized.FindProperty(path);
            element.Clear();
            if (array == null || index >= array.arraySize)
                return;
            element.Add(BuildVariableRow(array.GetArrayElementAtIndex(index)));
        };
        listView.BindProperty(varsArray);
        listView.overridingAddButtonBehavior = (_, button) => ShowAddVariableMenu(button, varsArray);
        listView.itemsRemoved += _ => schedule.Execute(RebuildBody);
        listView.itemIndexChanged += (_, _) => schedule.Execute(RebuildBody);
        slot.Add(listView);
    }

    VisualElement BuildVariableRow(SerializedProperty element)
    {
        var nameProp = element.FindPropertyRelative("name");
        var varProp = element.FindPropertyRelative("variable");
        if (nameProp == null || varProp == null)
            return new Label(L10n.Tr("Unreadable variable", null));
        var variableType = VariableTypeOf(varProp);
        var isLocalizedString = variableType != null && typeof(LocalizedString).IsAssignableFrom(variableType);

        var container = new VisualElement();
        var row = Row();
        row.AddToClassList(LocClasses.LsVarRow);

        var nameField = new TextField { value = nameProp.stringValue, isDelayed = true };
        nameField.AddToClassList(LocClasses.LsVarName);
        nameField.tooltip = L10n.Tr("Referenced from the string as {name}; whitespace becomes '-'.", null);
        nameField.RegisterValueChangedCallback(evt =>
        {
            var normalized = NormalizeName(evt.newValue);
            if (normalized != evt.newValue)
                nameField.SetValueWithoutNotify(normalized);
            Serialized.Update();
            nameProp.stringValue = normalized;
            Serialized.ApplyModifiedProperties();
        });
        row.Add(nameField);

        Button typeButton = null;
        typeButton = new Button(() => ShowChangeTypeMenu(typeButton, varProp)) { text = DisplayName(variableType) };
        typeButton.AddToClassList(LocClasses.LsVarType);
        row.Add(typeButton);

        if (isLocalizedString)
        {
            row.Add(new VisualElement().WithClass(LocClasses.LsGrow));
        }
        else
        {
            var valueProp = varProp.FindPropertyRelative(Paths.VariableValue);
            if (valueProp != null)
            {
                var valueField = new PropertyField(valueProp, string.Empty);
                valueField.AddToClassList(LocClasses.LsVarValue);
                valueField.BindProperty(valueProp);
                row.Add(valueField);
            }
        }
        container.Add(row);

        if (isLocalizedString)
        {
            var box = new VisualElement().WithClass(LocClasses.LsNest);
            if (Depth + 1 >= MaxDepth)
                box.Add(new HelpBox(L10n.Tr("Nesting is too deep to edit here; open a dedicated field to edit further.", null), HelpBoxMessageType.Info));
            else
                box.Add(new LocalizedStringElement(varProp, nameProp.stringValue, Depth + 1, Paths));
            box.Add(new Label(L10n.Tr("Reads its own variables plus the parent's.", null)).WithClass(LocClasses.LsHint));
            container.Add(box);
        }
        return container;
    }

    void ShowAddVariableMenu(Button anchor, SerializedProperty varsArray)
        => ShowMenu(anchor, VariableTypes, (Type)null, type => AddVariable(type, varsArray));

    void AddVariable(Type type, SerializedProperty varsArray)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add Local Variable");
        var undoGroup = Undo.GetCurrentGroup();

        Serialized.Update();
        var element = AppendVariable(varsArray);
        if (element == null || !SetVariableType(element.FindPropertyRelative("variable"), type))
            return;
        element.FindPropertyRelative("name").stringValue = UniqueVariableName(varsArray);
        Serialized.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(undoGroup);
        RebuildBody();
    }

    SerializedProperty AppendVariable(SerializedProperty varsArray)
    {
        if (!Paths.IsUxmlData)
        {
            var next = varsArray.arraySize;
            varsArray.InsertArrayElementAtIndex(next);
            return varsArray.GetArrayElementAtIndex(next);
        }

        // Built before the insert so a type that cannot be authored leaves the array untouched.
        var data = UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(NamedVariable));
        if (data == null)
            return null;

        var index = varsArray.arraySize;
        varsArray.InsertArrayElementAtIndex(index);
        // The element is a managed reference slot, so it only gains its children once the type change is applied.
        varsArray.GetArrayElementAtIndex(index).managedReferenceValue = data;
        Serialized.ApplyModifiedProperties();
        Serialized.Update();
        return varsArray.GetArrayElementAtIndex(index);
    }

    Type VariableTypeOf(SerializedProperty varProp)
    {
        var managed = varProp.managedReferenceValue?.GetType();
        return Paths.IsUxmlData ? managed?.DeclaringType : managed;
    }

    bool SetVariableType(SerializedProperty varProp, Type type)
    {
        if (!Paths.IsUxmlData)
        {
            varProp.managedReferenceValue = Activator.CreateInstance(type);
            return true;
        }
        // The creator reports a type it cannot author by logging and returning null.
        var data = UxmlSerializedDataCreator.CreateUxmlSerializedData(type);
        if (data == null)
            return false;
        varProp.managedReferenceValue = data;
        return true;
    }

    void ShowChangeTypeMenu(Button anchor, SerializedProperty varProp)
        => ShowMenu(anchor, VariableTypes, VariableTypeOf(varProp), type =>
        {
            Serialized.Update();
            if (!SetVariableType(varProp, type))
                return;
            Serialized.ApplyModifiedProperties();
            RebuildBody();
        });

    static string UniqueVariableName(SerializedProperty varsArray)
    {
        var existing = new HashSet<string>();
        for (var i = 0; i < varsArray.arraySize; i++)
        {
            var name = varsArray.GetArrayElementAtIndex(i).FindPropertyRelative("name");
            if (name != null)
                existing.Add(name.stringValue);
        }
        if (!existing.Contains("variable"))
            return "variable";
        for (var i = 2; ; ++i)
        {
            var candidate = $"variable-{i}";
            if (!existing.Contains(candidate))
                return candidate;
        }
    }

    LocalizedString LiveLocalizedString()
    {
        try
        {
            if (!Paths.IsUxmlData)
                return Property.boxedValue as LocalizedString;
            if (Property.boxedValue is not LocalizedString.UxmlSerializedData data)
                return null;
            var instance = data.CreateInstance();
            data.Deserialize(instance);
            return instance as LocalizedString;
        }
        catch
        {
            return null;
        }
    }

    static SmartFormatter Formatter()
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        return settings != null ? settings.GetSmartFormatter() : Smart.Default;
    }

    static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]))
                chars[i] = '-';
        }
        return new string(chars);
    }

    static string DisplayName(Type type)
    {
        if (type == null)
            return L10n.Tr("None", null);
        foreach (var (t, label) in VariableTypes)
        {
            if (t == type)
                return L10n.Tr(label, null);
        }
        return ObjectNames.NicifyVariableName(type.Name);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
