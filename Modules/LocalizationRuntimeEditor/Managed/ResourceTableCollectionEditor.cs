// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomEditor(typeof(ResourceTableCollection))]
class ResourceTableCollectionEditor : UnityEditor.Editor
{
    const string k_Uxml = "LocalizationRuntime/UXML/ResourceTableCollectionInspector.uxml";

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

        var tables = root.Q("tables");
        foreach (var table in collection.Tables)
        {
            if (table == null)
                continue;
            var label = new Label($"{table.LocaleIdentifier.Code}  ({table.name})");
            label.AddToClassList(LocClasses.LocCollectionInspectorTable);
            tables.Add(label);
        }
        return root;
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
            var dropdown = new DropdownField(L10n.Tr("Provider", null), choices, current)
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
