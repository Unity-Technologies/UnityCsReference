// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomEditor(typeof(ResourceTableCollection))]
class ResourceTableCollectionEditor : UnityEditor.Editor
{
    const string k_Uxml = "LocalizationRuntime/UXML/ResourceTableCollectionInspector.uxml";
    const string k_ExtensionsField = "m_Extensions";

    VisualElement m_Source;

    void OnEnable() => Undo.undoRedoPerformed += OnUndoRedoPerformed;

    void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedoPerformed;

    void OnUndoRedoPerformed()
    {
        if (m_Source?.panel != null && target != null)
            BuildSource(m_Source);
    }

    public override VisualElement CreateInspectorGUI()
    {
        var collection = (ResourceTableCollection)target;
        var root = (EditorGUIUtility.Load(k_Uxml) as VisualTreeAsset).Instantiate();

        m_Source = root.Q("source");
        BuildSource(m_Source);
        AddPreloadToggle(root, collection);

        root.Q<Foldout>("tables-foldout").text = LocLabels.Tables;
        BuildTables(root.Q("tables"), root.Q("tables-actions"), collection);
        BuildExtensions(root);
        return root;
    }

    static void BuildTables(VisualElement host, VisualElement actions, ResourceTableCollection collection)
    {
        var locales = new List<(LocaleIdentifier Identifier, string Name)>();
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings != null)
        {
            foreach (var locale in settings.AvailableLocales)
            {
                if (locale != null)
                    locales.Add((locale.Identifier, locale.LocaleName));
            }
        }
        foreach (var table in collection.Tables)
        {
            if (table != null && !locales.Exists(l => l.Identifier == table.LocaleIdentifier))
                locales.Add((table.LocaleIdentifier, table.LocaleIdentifier.ToString()));
        }

        var grid = new MultiColumnListView
        {
            showBoundCollectionSize = false,
            selectionType = SelectionType.None,
            itemsSource = new List<ResourceTableCollection> { collection },
            showAlternatingRowBackgrounds = AlternatingRowBackground.None,
        };
        grid.AddToClassList(LocClasses.LocCollectionInspectorTable);
        grid.columns.Add(new Column
        {
            title = LocLabels.Table,
            width = 140,
            makeCell = () => new Label().WithClass(LocClasses.LocCellLabel),
            bindCell = (element, _) => ((Label)element).text = collection.TableCollectionName,
        });
        foreach (var locale in locales)
        {
            var identifier = locale.Identifier;
            grid.columns.Add(new Column
            {
                title = locale.Name,
                width = 60,
                makeCell = () =>
                {
                    var toggle = new Toggle();
                    toggle.SetEnabled(false);
                    toggle.AddToClassList(LocClasses.LocCellToggle);
                    return toggle;
                },
                bindCell = (element, _) => ((Toggle)element).SetValueWithoutNotify(collection.GetTable(identifier) != null),
            });
        }
        host.Add(grid);

        actions.Add(new Button(() => ResourceTablesWindow.ShowWindow(collection)) { text = LocLabels.OpenInTablesWindow });
    }

    void BuildExtensions(VisualElement root)
    {
        var host = root.Q("extensions");
        var property = serializedObject.FindProperty(k_ExtensionsField);
        var list = new ListView
        {
            headerTitle = LocLabels.Extensions,
            showFoldoutHeader = true,
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            horizontalScrollingEnabled = false,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single,
        };
        list.makeItem = () => new PropertyField();
        list.bindItem = (element, index) =>
        {
            var array = serializedObject.FindProperty(k_ExtensionsField);
            if (array == null || index >= array.arraySize)
                return;
            var item = array.GetArrayElementAtIndex(index);
            var field = (PropertyField)element;
            field.label = ExtensionLabel(item);
            field.BindProperty(item);
        };
        list.unbindItem = (element, index) => ((PropertyField)element).Unbind();
        list.BindProperty(property);
        list.overridingAddButtonBehavior = (view, button) => ShowAddExtensionMenu(view, button);
        host.Add(list);
    }

    static string ExtensionLabel(SerializedProperty item)
    {
        var value = item.managedReferenceValue;
        return value != null ? ObjectNames.NicifyVariableName(value.GetType().Name) : L10n.Tr("Missing extension", null);
    }

    void ShowAddExtensionMenu(BaseListView list, Button anchor)
    {
        serializedObject.Update();
        var property = serializedObject.FindProperty(k_ExtensionsField);
        var present = new HashSet<System.Type>();
        for (var i = 0; i < property.arraySize; i++)
        {
            var value = property.GetArrayElementAtIndex(i).managedReferenceValue;
            if (value != null)
                present.Add(value.GetType());
        }

        var menu = new GenericDropdownMenu();
        var any = false;
        foreach (var type in TypeCache.GetTypesDerivedFrom<IResourceCollectionExtension>())
        {
            // A managed reference needs a concrete type with a parameterless constructor, and cannot be an Object.
            if (type.IsAbstract || type.IsGenericType || typeof(UnityEngine.Object).IsAssignableFrom(type) || type.GetConstructor(System.Type.EmptyTypes) == null)
                continue;
            any = true;
            var name = ObjectNames.NicifyVariableName(type.Name);
            if (present.Contains(type))
            {
                menu.AddDisabledItem(name, true);
                continue;
            }
            var captured = type;
            menu.AddItem(name, false, () =>
            {
                serializedObject.Update();
                var array = serializedObject.FindProperty(k_ExtensionsField);
                var index = array.arraySize;
                array.InsertArrayElementAtIndex(index);
                array.GetArrayElementAtIndex(index).managedReferenceValue = System.Activator.CreateInstance(captured);
                serializedObject.ApplyModifiedProperties();
                list.Rebuild();
            });
        }
        if (!any)
            menu.AddDisabledItem(L10n.Tr("No extensions are available", null), false);
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    static void AddPreloadToggle(VisualElement root, ResourceTableCollection collection)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return;

        var reference = AssetProviderEditors.CreateReference(collection);
        var toggle = new Toggle(LocLabels.Preload)
        {
            tooltip = L10n.Tr("Loads this collection's tables when localization initializes and when the locale changes, following the settings' preload behavior.", null),
            value = settings.PreloadTables.Contains(reference),
        };
        toggle.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(settings, "Change Preload");
            if (evt.newValue)
            {
                if (!settings.PreloadTables.Contains(reference))
                    settings.PreloadTables.Add(reference);
            }
            else
            {
                settings.PreloadTables.Remove(reference);
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        });
        root.Add(toggle);
    }

    void BuildSource(VisualElement container)
    {
        var pillRow = container.Q("source-pill-row");
        var pill = container.Q<Label>("source-pill");
        var body = container.Q("source-body");
        body.Clear();

        var collection = (ResourceTableCollection)target;
        var path = AssetDatabase.GetAssetPath(collection);
        var underResources = AssetProviderEditors.IsUnderResources(path);

        var providers = new List<IAssetProvider>();
        foreach (var provider in AssetProviderEditors.ChainProviders())
            providers.Add(provider);

        if (providers.Count == 0 && !underResources)
        {
            pillRow.style.display = DisplayStyle.None;
            body.Add(new HelpBox(L10n.Tr("No content sources are configured. Add one in Project Settings > Localization.", null), HelpBoxMessageType.Info));
            return;
        }

        var resolved = AssetProviderEditors.ResolveProvider(collection);

        pillRow.style.display = DisplayStyle.Flex;
        pill.text = underResources
            ? AssetProviderEditors.ProviderTitle(typeof(ResourceFolderProvider))
            : resolved != null ? AssetProviderEditors.ProviderTitle(resolved.GetType()) : L10n.Tr("None", null);
        pill.tooltip = L10n.Tr("The content source responsible for loading this collection's tables.", null);

        var consistent = AssetProviderEditors.IsCorrectlyAssigned(collection);
        if (underResources)
        {
            var note = new Label(L10n.Tr("Determined by the Resources folder location.", null));
            note.AddToClassList(LocClasses.LocSourceNote);
            note.AddToClassList(LocClasses.LocTextSecondary);
            body.Add(note);
        }
        else if (consistent)
        {
            // The dropdown names the source, so the pill would only repeat it.
            pillRow.style.display = DisplayStyle.None;

            var choices = new List<string>();
            var chain = new List<IAssetProvider>();
            var current = 0;
            foreach (var provider in providers)
            {
                if (provider is ResourceFolderProvider)
                    continue;
                if (provider.Id == collection.ProviderId)
                    current = chain.Count;
                choices.Add(AssetProviderEditors.ProviderTitle(provider.GetType()));
                chain.Add(provider);
            }
            var dropdown = new DropdownField(LocLabels.Provider, choices, current)
            {
                tooltip = L10n.Tr("The content source this collection registers its tables and assets with.", null)
            };
            dropdown.RegisterValueChangedCallback(_ =>
            {
                var index = dropdown.index;
                if (index < 0 || index >= chain.Count)
                    return;
                AssetProviderEditors.MoveCollection(collection, chain[index]);
                serializedObject.Update();
                container.schedule.Execute(() => BuildSource(container));
            });
            body.Add(dropdown);
        }

        if (consistent)
            return;

        // Inconsistent: warn and offer a one-click fix that assigns the collection to the correct source.
        if (underResources)
        {
            var help = new HelpBox(L10n.Tr("This collection is under a Resources folder but is not served by the Resources source. Its assets always ship in the build; add it to the Resources source.", null), HelpBoxMessageType.Warning);
            help.AddToClassList(LocClasses.LocSourceWarning);
            body.Add(help);
            AddFixButton(body, container, collection, L10n.Tr("Use Resources source", null));
            return;
        }

        // Non-Resources: the default source is an existing chain provider (resolving here does not mutate the chain).
        var targetProvider = AssetProviderEditors.ResolveForNewCollection(path);
        if (targetProvider == null)
        {
            var info = new HelpBox(L10n.Tr("No non-Resources content source is configured. Add one in Project Settings > Localization.", null), HelpBoxMessageType.Info);
            info.AddToClassList(LocClasses.LocSourceWarning);
            body.Add(info);
            return;
        }
        var warning = new HelpBox(L10n.Tr("This collection is not assigned to a content source in the chain. Add it to the default source.", null), HelpBoxMessageType.Warning);
        warning.AddToClassList(LocClasses.LocSourceWarning);
        body.Add(warning);
        AddFixButton(body, container, collection, string.Format(L10n.Tr("Add to default source ({0})", null), AssetProviderEditors.ProviderTitle(targetProvider.GetType())));
    }

    void AddFixButton(VisualElement parent, VisualElement source, ResourceTableCollection collection, string label)
    {
        var button = new Button(() =>
        {
            // Resolve on click so a missing Resources provider is added only when the user acts.
            var targetProvider = AssetProviderEditors.ResolveForNewCollection(AssetDatabase.GetAssetPath(collection));
            if (targetProvider == null)
                return;
            AssetProviderEditors.MoveCollection(collection, targetProvider);
            serializedObject.Update();
            AssetDatabase.SaveAssets();
            source.schedule.Execute(() => BuildSource(source));
        })
        {
            text = label
        };
        button.AddToClassList(LocClasses.LocFixButton);
        parent.Add(button);
    }
}
