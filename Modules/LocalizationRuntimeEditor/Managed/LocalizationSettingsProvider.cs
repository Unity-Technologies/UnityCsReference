// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Providers;
using Unity.Localization.Providers.FileTables;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

class LocalizationSettingsProvider : SettingsProvider
{
    const string k_Page = "LocalizationRuntime/UXML/LocalizationSettings.uxml";
    const string k_Wizard = "LocalizationRuntime/UXML/LocalizationWizard.uxml";
    const string k_Card = "LocalizationRuntime/UXML/ReferenceModeCard.uxml";
    const string k_ProvidersPath = "m_ResourceDatabase.m_AssetProvider.m_Providers";

    const string k_LocalesPath = "m_AvailableLocales";

    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_Card;

    VisualElement m_Root;
    SerializedObject m_Serialized;

    internal enum SetupState { NoSettings, NoLocales, NeedsProjectLocale, NeedsReferenceMode, InstallingPackage, Complete }

    public LocalizationSettingsProvider()
        : base("Project/Localization", SettingsScope.Project)
    {
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        m_Root = new ScrollView { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
        rootElement.Add(m_Root);
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        AddressablesReferenceMode.installStateChanged += ScheduleRebuild;
        BuildContent(m_Root);
    }

    public override void OnDeactivate()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        AddressablesReferenceMode.installStateChanged -= ScheduleRebuild;
        m_Serialized?.Dispose();
        m_Serialized = null;
        base.OnDeactivate();
    }

    void OnUndoRedoPerformed()
    {
        if (m_Root != null)
            BuildContent(m_Root);
    }

    void ScheduleRebuild() => m_Root?.schedule.Execute(() => BuildContent(m_Root));

    void BuildContent(VisualElement root)
    {
        root.Clear();

        var active = LocalizationEditorSettings.ActiveSettings;
        var state = GetSetupState(active);

        var page = (EditorGUIUtility.Load(k_Page) as VisualTreeAsset).Instantiate();
        root.Add(page);

        page.Q<Label>("title").text = L10n.Tr("Localization", null);
        var summary = page.Q<Label>("summary");
        if (state == SetupState.Complete)
            summary.text = FormatSummary(active);
        else
            summary.style.display = DisplayStyle.None;

        m_Serialized?.Dispose();
        m_Serialized = null;
        if (active != null)
            m_Serialized = new SerializedObject(active);
        var serialized = m_Serialized;

        var wizardRegion = page.Q<VisualElement>("wizard-region");
        if (state != SetupState.Complete)
            BuildWizard(wizardRegion, active, serialized, state);
        else
            wizardRegion.style.display = DisplayStyle.None;

        if (active != null && active.AvailableLocales.Count > 0)
            BuildLocalesTable(page.Q<VisualElement>("locales-content"), active);
        else
            page.Q<VisualElement>("locales-region").style.display = DisplayStyle.None;

        if (state == SetupState.Complete)
        {
            BuildProjectLocaleRow(page.Q<VisualElement>("project-locale-content"), active);
            BuildStartupSelectors(page.Q<VisualElement>("startup-selectors-content"), serialized);
            BuildDeferInitialization(page.Q<Toggle>("defer-initialization-toggle"), serialized);
            BuildContentSources(page.Q<VisualElement>("content-sources-content"), active, serialized);
        }
        else
        {
            page.Q<VisualElement>("project-locale-region").style.display = DisplayStyle.None;
            page.Q<VisualElement>("startup-selectors-region").style.display = DisplayStyle.None;
            page.Q<VisualElement>("content-sources-region").style.display = DisplayStyle.None;
        }

        if (serialized != null)
            root.Bind(serialized);
    }

    void BuildWizard(VisualElement root, LocalizationSettings active, SerializedObject serialized, SetupState state)
    {
        var wizard = (EditorGUIUtility.Load(k_Wizard) as VisualTreeAsset).Instantiate();
        root.Add(wizard);

        SetWizardStep(wizard, 1,
            done: state != SetupState.NoSettings,
            active: state == SetupState.NoSettings,
            control: state == SetupState.NoSettings ? CreateSettingsButton() : null);

        SetWizardStep(wizard, 2,
            done: state is SetupState.NeedsProjectLocale or SetupState.NeedsReferenceMode or SetupState.InstallingPackage or SetupState.Complete,
            active: state == SetupState.NoLocales,
            control: state == SetupState.NoLocales
                ? new Button(() => LocaleGeneratorWindow.ShowWindow(ScheduleRebuild)) { text = L10n.Tr("Add locale", null) }
                : null);

        VisualElement step3Control = null;
        if (state == SetupState.NeedsProjectLocale)
        {
            var dropdown = BuildLocaleDropdown(active, active.ProjectLocale, code => { SetProjectLocale(active, code); ScheduleRebuild(); });
            dropdown.AddToClassList(LocClasses.LocDropdownFixed);
            step3Control = dropdown;
        }
        SetWizardStep(wizard, 3,
            done: state is SetupState.NeedsReferenceMode or SetupState.InstallingPackage or SetupState.Complete,
            active: state == SetupState.NeedsProjectLocale,
            control: step3Control);

        SetWizardStep(wizard, 4,
            done: state == SetupState.Complete,
            active: state is SetupState.NeedsReferenceMode or SetupState.InstallingPackage,
            control: state switch
            {
                SetupState.NeedsReferenceMode => BuildReferenceModeCards(active),
                SetupState.InstallingPackage => BuildInstallingIndicator(),
                _ => null
            });
    }

    Button CreateSettingsButton()
    {
        return new Button(() =>
        {
            var created = CreateSettingsAsset();
            if (created != null)
            {
                LocalizationEditorSettings.ActiveSettings = created;
                LanguageToolbar.Refresh();
                ScheduleRebuild();
            }
        })
        {
            text = L10n.Tr("Create localization settings", null)
        };
    }

    static void SetWizardStep(VisualElement wizard, int number, bool done, bool active, VisualElement control)
    {
        var badge = wizard.Q<Label>($"badge-{number}");
        badge.text = done ? "✓" : number.ToString();
        badge.AddToClassList(done ? LocClasses.LocWizardBadgeDone : active ? LocClasses.LocWizardBadgeActive : LocClasses.LocWizardBadgePending);

        var label = wizard.Q<Label>($"label-{number}");
        label.AddToClassList(active ? LocClasses.LocWizardStepLabelActive : done ? LocClasses.LocWizardStepLabelDone : LocClasses.LocWizardStepLabelPending);

        if (control != null)
            wizard.Q<VisualElement>($"control-{number}").Add(control);
    }

    VisualElement BuildReferenceModeCards(LocalizationSettings active)
    {
        var cards = new VisualElement();
        cards.AddToClassList(LocClasses.LocCards);

        cards.Add(ReferenceModeCard(
            L10n.Tr("Direct references", null),
            L10n.Tr("Simple and good for small projects. All assets are loaded at start and persist in memory.", null),
            gap: true,
            () => { ApplyDirectReferences(active); ScheduleRebuild(); }));

        cards.Add(ReferenceModeCard(
            L10n.Tr("Addressables", null),
            L10n.Tr("Scalable system that loads what is needed. Supports remote resources such as language packs. Uses the Addressables asset system and requires the localization package 2.0.", null),
            gap: false,
            () => AddressablesReferenceMode.InstallAndConfigure(active, ScheduleRebuild)));

        return cards;
    }

    static VisualElement BuildInstallingIndicator()
    {
        var row = new VisualElement();
        row.AddToClassList(LocClasses.LocWizardInstalling);
        row.Add(new LoadingSpinner());
        var label = new Label(L10n.Tr("Installing package...", null));
        label.AddToClassList(LocClasses.LocWizardInstallingLabel);
        row.Add(label);
        return row;
    }

    static VisualElement ReferenceModeCard(string title, string body, bool gap, Action onClick)
    {
        var card = new Button(onClick);
        card.ClearClassList();
        card.AddToClassList(LocClasses.LocCard);
        if (gap)
            card.AddToClassList(LocClasses.LocCardFirst);

        s_Card ??= EditorGUIUtility.LoadRequired(k_Card) as VisualTreeAsset;
        if (s_Card == null)
            return card;
        s_Card.CloneTree(card);
        card.Q<Label>("card-title").text = title;
        card.Q<Label>("card-body").text = body;
        return card;
    }

    // Preserves any ResourceFolderProvider: its content is path-driven.
    internal static void ApplyDirectReferences(LocalizationSettings active)
    {
        var chain = active != null && active.Database != null ? active.Database.AssetProvider : null;
        if (chain == null)
            return;

        Undo.RegisterCompleteObjectUndo(active, "Set Reference Mode");
        RemoveProviders<JsonResourceProvider>(chain);
        var referenced = EnsureProvider<ReferencedAssetProvider>(chain, 0);
        active.SelectedProvider = referenced;
        // Collections created before a valid source existed were left unassigned, so re-register the survivors.
        AssetProviderEditors.RebuildRegistrations();
        EditorUtility.SetDirty(active);
        AssetDatabase.SaveAssets();
    }

    static T EnsureProvider<T>(AssetProvider chain, int index) where T : class, IAssetProvider, new()
    {
        var existing = chain.GetProvider<T>();
        if (existing != null)
            return existing;
        var created = new T();
        chain.InsertProvider(Math.Min(index, chain.Providers.Count), created);
        return created;
    }

    static void RemoveProviders<T>(AssetProvider chain) where T : class, IAssetProvider
    {
        var existing = chain.GetProvider<T>();
        while (existing != null)
        {
            chain.RemoveProvider(existing);
            existing = chain.GetProvider<T>();
        }
    }

    void BuildLocalesTable(VisualElement root, LocalizationSettings active)
    {
        if (HasUnresolvedLocales(active))
        {
            root.Insert(0, new HelpBox(L10n.Tr("Some locales could not be loaded, usually because a package or script that defines them is missing. Restore the missing dependency to recover them, or remove them.", null), HelpBoxMessageType.Warning));
            var repair = new Button(() =>
            {
                Undo.RegisterCompleteObjectUndo(active, "Remove Unresolved Locales");
                active.CompactLocales();
                EditorUtility.SetDirty(active);
                ScheduleRebuild();
            }) { text = L10n.Tr("Remove unresolved locales", null) };
            repair.AddToClassList(LocClasses.LocRepairButton);
            root.Insert(1, repair);
        }

        var locales = new List<Locale>(active.AvailableLocales);
        var resourcesServed = AssetProviderEditors.LocaleCodesServedByResources();
        var localesArray = m_Serialized?.FindProperty(k_LocalesPath);

        var list = root.Q<MultiColumnListView>("locales-list");
        if (list == null)
            return;
        list.itemsSource = locales;
        // Reordering writes through the serialized array, so without one there is nothing to persist the move to.
        list.reorderable = localesArray != null;

        var enabled = list.columns["enabled"];
        enabled.makeCell = () =>
        {
            var toggle = new Toggle();
                toggle.AddToClassList(LocClasses.LocCellToggle);
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (toggle.userData is Locale target)
                    ApplyLocaleEnabled(active, target, evt.newValue);
            });
            return toggle;
        };
        enabled.bindCell = (element, index) => BindEnabledCell((Toggle)element, locales, index, resourcesServed);
        enabled.unbindCell = (element, _) => ((Toggle)element).userData = null;

        var localeColumn = list.columns["locale"];
        localeColumn.makeCell = () => new Label().WithClass(LocClasses.LocCellLabel).WithClass(LocClasses.LocCellLabelTruncate);
        localeColumn.bindCell = (element, index) => BindNameCell((Label)element, locales, index);

        var code = list.columns["code"];
        code.makeCell = () => new Label().WithClass(LocClasses.LocCellLabel);
        code.bindCell = (element, index) => ((Label)element).text = At(locales, index)?.Code ?? string.Empty;

        var fallback = list.columns["fallback"];
        // Choices depend on the locale, so the dropdown is rebuilt per bind rather than retargeted.
        fallback.makeCell = () => new VisualElement().WithClass(LocClasses.LocCellControl);
        fallback.bindCell = (element, index) =>
        {
            element.Clear();
            var locale = At(locales, index);
            if (locale != null)
                element.Add(BuildFallbackDropdown(active, locale));
        };
        fallback.unbindCell = (element, _) => element.Clear();

        list.overridingAddButtonBehavior = (_, _) => LocaleGeneratorWindow.ShowWindow(ScheduleRebuild);
        // The source is a copy, so the default remove would drop the row without touching the settings.
        list.onRemove = view =>
        {
            RemoveLocaleAt(active, view.selectedIndex);
            ScheduleRebuild();
        };
        if (localesArray != null)
        {
            list.itemIndexChanged += (_, _) =>
            {
                ApplyOrder(localesArray, locales);
                ScheduleRebuild();
            };
        }

        list.Rebuild();
    }

    void ApplyOrder(SerializedProperty localesArray, List<Locale> ordered)
    {
        for (var target = 0; target < ordered.Count && target < localesArray.arraySize; target++)
        {
            var source = IndexOfReference(localesArray, ordered[target], target);
            if (source > target)
                localesArray.MoveArrayElement(source, target);
        }
        m_Serialized.ApplyModifiedProperties();
        m_Serialized.Update();
    }

    static int IndexOfReference(SerializedProperty localesArray, Locale locale, int from)
    {
        for (var i = from; i < localesArray.arraySize; i++)
        {
            if (ReferenceEquals(localesArray.GetArrayElementAtIndex(i).managedReferenceValue, locale))
                return i;
        }
        return -1;
    }

    static Locale At(List<Locale> locales, int index) => index >= 0 && index < locales.Count ? locales[index] : null;

    void BindNameCell(Label label, List<Locale> locales, int index)
    {
        var locale = At(locales, index);
        label.EnableInClassList(LocClasses.LocRowNameDisabled, locale != null && !locale.Enabled);
        if (locale == null)
        {
            label.text = L10n.Tr("Unresolved locale", null);
            label.tooltip = L10n.Tr("The package or script that defines this locale is missing.", null);
            return;
        }
        label.text = locale.LocaleName;
        label.tooltip = NativeName(locale) ?? string.Empty;
    }

    static void BindEnabledCell(Toggle toggle, List<Locale> locales, int index, HashSet<string> resourcesServed)
    {
        var locale = At(locales, index);
        // Cleared first so SetValueWithoutNotify can never be attributed to the row this pooled cell last showed.
        toggle.userData = null;
        toggle.SetEnabled(locale != null);
        toggle.SetValueWithoutNotify(locale != null && locale.Enabled);
        if (locale == null)
            return;
        toggle.tooltip = resourcesServed.Contains(locale.Code)
            ? L10n.Tr("When disabled, this locale is not selectable at runtime. Its assets are in a Resources folder, so they still ship in builds.", null)
            : L10n.Tr("When disabled, this locale's tables are excluded from builds and it is not selectable at runtime.", null);
        toggle.userData = locale;
    }

    void ApplyLocaleEnabled(LocalizationSettings active, Locale locale, bool enabled)
    {
        Undo.RegisterCompleteObjectUndo(active, enabled ? "Enable Locale" : "Disable Locale");
        locale.Enabled = enabled;
        if (!enabled)
        {
            if (active.ProjectLocale == locale)
                active.ProjectLocale = FirstEnabledLocale(active, locale);
            ClearFallbacksReferencing(active, locale.Code);
            if (LocalizationSettings.Instance == active && LocalizationSettings.SelectedLocale == locale)
                LocalizationSettings.SelectedLocale = active.ProjectLocale ?? FirstEnabledLocale(active, locale);
        }
        EditorUtility.SetDirty(active);
        AssetProviderEditors.SetLocaleEnabled(locale, enabled);
        ScheduleRebuild();
    }

    void RemoveLocaleAt(LocalizationSettings active, int index)
    {
        var locales = active.AvailableLocales;
        if (index < 0 || index >= locales.Count)
            return;
        var locale = locales[index];
        if (locale == null)
            return;

        Undo.RegisterCompleteObjectUndo(active, "Remove Locale");
        var wasProjectLocale = active.ProjectLocale == locale;
        var wasSelected = LocalizationSettings.Instance == active && LocalizationSettings.SelectedLocale == locale;
        ClearFallbacksReferencing(active, locale.Code);
        active.RemoveLocale(locale);
        // Leave the project locale null when no enabled locale remains; never fall back to a disabled one.
        if (wasProjectLocale)
            active.ProjectLocale = FirstEnabledLocale(active, null);
        // Mirror the disable path so bound listeners stop resolving a locale that is gone.
        if (wasSelected)
            LocalizationSettings.SelectedLocale = active.ProjectLocale ?? FirstEnabledLocale(active, null);
        EditorUtility.SetDirty(active);
        AssetProviderEditors.SetLocaleEnabled(locale, false);
        LanguageToolbar.Refresh();
    }

    void BuildProjectLocaleRow(VisualElement root, LocalizationSettings active)
    {
        var dropdown = BuildLocaleDropdown(active, active.ProjectLocale, code => { SetProjectLocale(active, code); ScheduleRebuild(); });
        dropdown.AddToClassList(LocClasses.LocDropdownFixed);
        dropdown.tooltip = L10n.Tr("The default locale selected when the game starts.", null);
        root.Add(dropdown);

        if (active.ProjectLocale != null && !active.ProjectLocale.Enabled)
        {
            var warning = new HelpBox(L10n.Tr("The project locale is disabled.", null), HelpBoxMessageType.Warning).WithClass(LocClasses.LocProjectLocaleWarning);
            root.Add(warning);
        }
    }

    static void BuildDeferInitialization(Toggle toggle, SerializedObject serialized)
    {
        toggle.tooltip = L10n.Tr(
            "Wait for an explicit call to LocalizationSettings.InitializeAsync, so a startup locale that depends on " +
            "a save file or a platform sign-in is not decided too early. Until then an awaited read waits, and a " +
            "synchronous read throws because it cannot. Play Mode only; the Editor is unaffected.", null);
        toggle.BindProperty(serialized.FindProperty("m_DeferInitialization"));
    }

    void BuildStartupSelectors(VisualElement root, SerializedObject serialized)
    {
        var selectors = serialized.FindProperty("m_StartupSelectors");
        // StartupLocaleSelectorDrawer already titles each row by its concrete type, so no element label is needed here.
        root.Add(MakeManagedReferenceList(serialized, selectors, (_, button) => ShowAddSelectorMenu(serialized, button)));
    }

    static ListView MakeManagedReferenceList(SerializedObject serialized, SerializedProperty array, Action<BaseListView, Button> onAdd,
        Func<Type, string> elementLabel = null)
    {
        var path = array.propertyPath;
        var listView = new ListView
        {
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single
        };
        listView.AddToClassList(LocClasses.LocList);
        listView.makeItem = () => new PropertyField();
        listView.bindItem = (element, index) =>
        {
            var arr = serialized.FindProperty(path);
            if (arr == null || index >= arr.arraySize)
                return;
            var item = arr.GetArrayElementAtIndex(index);
            var field = (PropertyField)element;
            if (elementLabel != null)
            {
                // A null managed reference means the type no longer resolves; say so rather than showing nothing.
                var type = item.managedReferenceValue?.GetType();
                field.label = type != null ? elementLabel(type) : L10n.Tr("Unresolved", null);
            }
            field.BindProperty(item);
        };
        listView.unbindItem = (element, _) => ((PropertyField)element).Unbind();
        listView.BindProperty(array);
        listView.overridingAddButtonBehavior = onAdd;
        return listView;
    }

    void ShowAddSelectorMenu(SerializedObject serialized, Button button)
    {
        // The startup-selector list is order-based and allows repeated selector types, so offer every type.
        var types = new List<Type>(AssetProviderEditors.GetInstantiableTypes<IStartupLocaleSelector>(excludeUnityObjects: true));
        types.Sort((a, b) => string.Compare(SelectorDisplayName(a), SelectorDisplayName(b), StringComparison.Ordinal));

        var menu = new GenericDropdownMenu();
        foreach (var type in types)
        {
            var captured = type;
            menu.AddItem(SelectorDisplayName(type), false, () =>
            {
                serialized.Update();
                var arr = serialized.FindProperty("m_StartupSelectors");
                var idx = arr.arraySize;
                arr.InsertArrayElementAtIndex(idx);
                arr.GetArrayElementAtIndex(idx).managedReferenceValue = Activator.CreateInstance(captured);
                serialized.ApplyModifiedProperties();
                ScheduleRebuild();
            });
        }
        menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Auto);
    }

    void BuildContentSources(VisualElement root, LocalizationSettings active, SerializedObject serialized)
    {
        var providersProp = serialized.FindProperty(k_ProvidersPath);
        if (providersProp != null)
        {
            var list = MakeManagedReferenceList(serialized, providersProp, (_, button) => ShowAddSourceMenu(serialized, button),
                type => AssetProviderEditors.ProviderTitle(type));
            list.onRemove = view => RemoveContentSource(active, serialized, view.selectedIndex);
            root.Add(list);
        }

        // Anything under a Resources folder ships even for disabled locales; surface which locales that affects.
        var resourcesWarnings = AssetProviderEditors.ResourcesServedDisabledTables();
        if (resourcesWarnings.Count > 0)
        {
            var warn = new HelpBox(string.Format(L10n.Tr("Resources folder: tables for {0} still ship while disabled. Move them to built content to exclude them.", null), DistinctLocaleNames(resourcesWarnings)), HelpBoxMessageType.Warning);
            root.Add(warn);
        }

        var providers = active.Database != null ? active.Database.AssetProvider?.Providers : null;
        var nonResources = new List<IAssetProvider>();
        if (providers != null)
        {
            foreach (var provider in providers)
            {
                if (provider != null && provider is not ResourceFolderProvider)
                    nonResources.Add(provider);
            }
        }
        if (nonResources.Count > 0)
        {
            var defaultRow = new VisualElement().WithClass(LocClasses.LocDefaultRow);
            defaultRow.Add(new Label(L10n.Tr("Default for new collections", null)).WithClass(LocClasses.LocDefaultRowLabel));
            var choices = new List<string>();
            var titleCounts = new Dictionary<string, int>();
            foreach (var provider in nonResources)
            {
                var title = AssetProviderEditors.ProviderTitle(provider.GetType());
                // Duplicate provider types render identically, so an ordinal tells the user which instance is the default.
                var seen = titleCounts.TryGetValue(title, out var n) ? n : 0;
                titleCounts[title] = seen + 1;
                choices.Add(seen == 0 ? title : $"{title} ({seen + 1})");
            }
            var currentIndex = nonResources.IndexOf(active.SelectedProvider);
            if (currentIndex < 0)
                currentIndex = 0;
            var defaultDropdown = new DropdownField(choices, currentIndex)
            {
                tooltip = L10n.Tr("New collections saved outside a Resources folder use this source. Overridable per collection.", null)
            };
            defaultDropdown.AddToClassList(LocClasses.LocDropdownFixed);
            defaultDropdown.RegisterValueChangedCallback(_ =>
            {
                var index = defaultDropdown.index;
                Undo.RegisterCompleteObjectUndo(active, "Change Default Content Source");
                active.SelectedProvider = index >= 0 && index < nonResources.Count ? nonResources[index] : null;
                EditorUtility.SetDirty(active);
            });
            defaultRow.Add(defaultDropdown);
            root.Add(defaultRow);
        }
    }

    void ShowAddSourceMenu(SerializedObject serialized, Button button)
    {
        var types = AssetProviderEditors.AddableProviderTypes(AssetProviderEditors.ChainProviders());

        var menu = new GenericDropdownMenu();
        if (types.Count == 0)
            menu.AddDisabledItem(L10n.Tr("Every content source is already added", null), false);
        foreach (var type in types)
        {
            var captured = type;
            menu.AddItem(AssetProviderEditors.ProviderTitle(type), false, () =>
            {
                var instance = (IAssetProvider)Activator.CreateInstance(captured);
                serialized.Update();
                var arr = serialized.FindProperty(k_ProvidersPath);
                var idx = arr.arraySize;
                arr.InsertArrayElementAtIndex(idx);
                arr.GetArrayElementAtIndex(idx).managedReferenceValue = instance;
                serialized.ApplyModifiedProperties();
                AssetProviderEditors.RebuildRegistrations();
                ScheduleRebuild();
            });
        }
        menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Auto);
    }

    void RemoveContentSource(LocalizationSettings active, SerializedObject serialized, int selectedIndex)
    {
        var arr = serialized.FindProperty(k_ProvidersPath);
        if (arr == null || arr.arraySize == 0)
            return;
        var index = selectedIndex >= 0 && selectedIndex < arr.arraySize ? selectedIndex : arr.arraySize - 1;
        arr.DeleteArrayElementAtIndex(index);
        serialized.ApplyModifiedProperties();

        active.Database?.AssetProvider?.RevalidateSelection();
        AssetProviderEditors.RebuildRegistrations();
        EditorUtility.SetDirty(active);
        ScheduleRebuild();
    }

    internal static string SelectorDisplayName(Type type)
    {
        return (type != null ? type.Name : string.Empty) switch
        {
            nameof(CommandLineLocaleSelector) => L10n.Tr("Command Line Locale Selector", null),
            nameof(SystemLocaleSelector) => L10n.Tr("System Locale Selector", null),
            nameof(SpecificLocaleSelector) => L10n.Tr("Specific Locale Selector", null),
            nameof(PlayerPrefLocaleSelector) => L10n.Tr("Player Pref Locale Selector", null),
            var name => ObjectNames.NicifyVariableName(name)
        };
    }

    static string DistinctLocaleNames(List<(Locale locale, string collectionName)> pairs)
    {
        var names = new List<string>();
        foreach (var (locale, _) in pairs)
        {
            if (locale != null && !names.Contains(locale.LocaleName))
                names.Add(locale.LocaleName);
        }
        return string.Join(", ", names);
    }

    internal static SetupState GetSetupState(LocalizationSettings settings)
    {
        if (settings == null)
            return SetupState.NoSettings;
        if (settings.AvailableLocales == null || settings.AvailableLocales.Count == 0)
            return SetupState.NoLocales;
        if (settings.ProjectLocale == null)
            return SetupState.NeedsProjectLocale;
        // Installing wins over the provider check so undo mid-install keeps the spinner up.
        if (AddressablesReferenceMode.IsInstalling)
            return SetupState.InstallingPackage;
        if (settings.SelectedProvider == null)
            return SetupState.NeedsReferenceMode;
        if (!HasNonResourcesProvider(settings))
            return SetupState.NeedsReferenceMode;
        return SetupState.Complete;
    }

    static bool HasNonResourcesProvider(LocalizationSettings settings)
    {
        var providers = settings.Database != null ? settings.Database.AssetProvider?.Providers : null;
        if (providers == null)
            return false;
        for (var i = 0; i < providers.Count; i++)
        {
            if (providers[i] != null && providers[i] is not ResourceFolderProvider)
                return true;
        }
        return false;
    }

    internal static string FormatSummary(LocalizationSettings settings)
    {
        var total = settings.AvailableLocales.Count;
        var enabled = 0;
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale != null && locale.Enabled)
                enabled++;
        }
        var def = settings.ProjectLocale != null ? settings.ProjectLocale.LocaleName : L10n.Tr("None", null);
        // Translate whole clauses, not isolated words, so translators can reorder and pluralize per language.
        var localesPart = total == 1 ? string.Format(L10n.Tr("{0} locale", null), total) : string.Format(L10n.Tr("{0} locales", null), total);
        var enabledPart = string.Format(L10n.Tr("{0} enabled", null), enabled);
        var defaultPart = string.Format(L10n.Tr("Default {0}", null), def);
        return $"{localesPart} · {enabledPart} · {defaultPart}";
    }

    static string NativeName(Locale locale)
    {
        var culture = locale.CultureInfo;
        if (culture == null || string.IsNullOrEmpty(culture.Name))
            return null;
        var native = culture.NativeName;
        if (string.IsNullOrEmpty(native) || string.Equals(native, locale.LocaleName, StringComparison.OrdinalIgnoreCase))
            return null;
        return native;
    }

    static void SetProjectLocale(LocalizationSettings active, string code)
    {
        if (active == null)
            return;
        Undo.RegisterCompleteObjectUndo(active, "Set Project Locale");
        var previous = active.ProjectLocale;
        var next = string.IsNullOrEmpty(code) ? null : active.GetLocale(new LocaleIdentifier(code));
        active.ProjectLocale = next;
        if (LocalizationSettings.Instance == active && LocalizationSettings.SelectedLocale == previous)
            LocalizationSettings.SelectedLocale = next;
        EditorUtility.SetDirty(active);
    }

    static DropdownField BuildFallbackDropdown(LocalizationSettings settings, Locale locale)
    {
        var choices = new List<string> { L10n.Tr("None", null) };
        var codes = new List<string> { null };
        foreach (var other in settings.AvailableLocales)
        {
            if (other == null || other == locale || !other.Enabled)
                continue;
            choices.Add(other.LocaleName);
            codes.Add(other.Code);
        }

        // Locale codes compare case-insensitively everywhere else, so a stored "EN" must match an available "en".
        var currentIndex = string.IsNullOrEmpty(locale.FallbackCode) ? 0 : IndexOfCode(codes, locale.FallbackCode);
        if (currentIndex < 0)
        {
            var target = settings.GetLocale(new LocaleIdentifier(locale.FallbackCode));
            choices.Add(target != null
                ? string.Format(L10n.Tr("{0} (disabled)", null), target.LocaleName)
                : string.Format(L10n.Tr("{0} (missing)", null), locale.FallbackCode));
            codes.Add(locale.FallbackCode);
            currentIndex = choices.Count - 1;
        }

        var dropdown = new DropdownField(choices, currentIndex).WithClass(LocClasses.LocFallbackDropdown);
        dropdown.RegisterValueChangedCallback(_ =>
        {
            var index = dropdown.index;
            var code = index >= 0 && index < codes.Count ? codes[index] : null;
            Undo.RegisterCompleteObjectUndo(settings, "Set Fallback");
            locale.FallbackCode = code;
            EditorUtility.SetDirty(settings);
        });
        return dropdown;
    }

    static int IndexOfCode(List<string> codes, string code)
    {
        for (var i = 0; i < codes.Count; i++)
        {
            if (string.Equals(codes[i], code, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    static bool HasUnresolvedLocales(LocalizationSettings settings)
    {
        var locales = settings.AvailableLocales;
        for (var i = 0; i < locales.Count; i++)
        {
            if (locales[i] == null)
                return true;
        }
        return false;
    }

    static Locale FirstEnabledLocale(LocalizationSettings settings, Locale except)
    {
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale != null && locale != except && locale.Enabled)
                return locale;
        }
        return null;
    }

    static void ClearFallbacksReferencing(LocalizationSettings settings, string code)
    {
        if (string.IsNullOrEmpty(code))
            return;
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale != null && string.Equals(locale.FallbackCode, code, StringComparison.OrdinalIgnoreCase))
                locale.FallbackCode = null;
        }
    }

    static DropdownField BuildLocaleDropdown(LocalizationSettings settings, Locale current, Action<string> onSetCode)
    {
        var choices = new List<string> { L10n.Tr("None", null) };
        var locales = new List<Locale> { null };
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale == null || !locale.Enabled)
                continue;
            choices.Add(locale.LocaleName);
            locales.Add(locale);
        }

        var currentIndex = current != null ? locales.IndexOf(current) : 0;
        if (current != null && currentIndex < 0)
        {
            choices.Add(string.Format(L10n.Tr("{0} (disabled)", null), current.LocaleName));
            locales.Add(current);
            currentIndex = choices.Count - 1;
        }

        var dropdown = new DropdownField(choices, currentIndex).WithClass(LocClasses.LocDropdownGrow);
        dropdown.RegisterValueChangedCallback(_ =>
        {
            var index = dropdown.index;
            var selected = index >= 0 && index < locales.Count ? locales[index] : null;
            onSetCode(selected != null ? selected.Code : null);
        });
        return dropdown;
    }

    static LocalizationSettings CreateSettingsAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create localization settings", null), L10n.Tr("Localization Settings", null), "asset",
            L10n.Tr("Choose where to save the Localization settings asset.", null));
        if (string.IsNullOrEmpty(path))
            return null;

        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        return settings;
    }

    [SettingsProvider]
    static SettingsProvider CreateProvider() => new LocalizationSettingsProvider();
}
