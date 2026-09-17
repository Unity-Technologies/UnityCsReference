// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.Localization.Editor;

static class LocLabels
{
    public static string AddEntry => L10n.Tr("Add Entry", null);
    public static string AddLocale => L10n.Tr("Add Locale", null);
    public static string AddLocales => L10n.Tr("Add Locales", null);
    public static string CharacterSet => L10n.Tr("Character Set", null);
    public static string Code => L10n.Tr("Code", null);
    public static string DirectReferences => L10n.Tr("Direct References", null);
    public static string Collection => L10n.Tr("Collection", null);
    public static string Collections => L10n.Tr("Collections", null);
    public static string ContentSources => L10n.Tr("Content Sources", null);
    public static string DeferInitialization => L10n.Tr("Defer Initialization", null);
    public static string CreateLocalizationSettings => L10n.Tr("Create Localization Settings", null);
    public static string Enabled => L10n.Tr("Enabled", null);
    public static string Export => L10n.Tr("Export", null);
    public static string ExportCharacterSet => L10n.Tr("Export Character Set", null);
    public static string ExportXliff => L10n.Tr("Export XLIFF", null);
    public static string Fallback => L10n.Tr("Fallback", null);
    public static string Import => L10n.Tr("Import", null);
    public static string Locale => L10n.Tr("Locale", null);
    public static string Locales => L10n.Tr("Locales", null);
    public static string LocalizationSettings => L10n.Tr("Localization Settings...", null);
    public static string Name => L10n.Tr("Name", null);
    public static string NewCollection => L10n.Tr("New Collection", null);
    public static string NewKey => L10n.Tr("New Key", null);
    public static string NewTableEntry => L10n.Tr("New Table Entry", null);
    public static string None => L10n.Tr("None", null);
    public static string NotInBuilds => L10n.Tr("Not in Builds", null);
    public static string ProjectLocale => L10n.Tr("Project Locale", null);
    public static string ResourceTables => L10n.Tr("Resource Tables", null);
    public static string SetUpLocalization => L10n.Tr("Set Up Localization", null);
    public static string SourceLanguage => L10n.Tr("Source Language", null);
    public static string StartupSelectors => L10n.Tr("Startup Selectors", null);
    public static string Table => L10n.Tr("Table", null);
    public static string Tables => L10n.Tr("Tables", null);

    public static string AssetProviders => L10n.Tr("Asset Providers", null);
    public static string Cancel => L10n.Tr("Cancel", null);
    public static string Create => L10n.Tr("Create", null);
    public static string CreateTable => L10n.Tr("Create Table", null);
    public static string CreateTableCollection => L10n.Tr("Create Table Collection", null);
    public static string DefaultForNewCollections => L10n.Tr("Default for New Collections", null);
    public static string Edit => L10n.Tr("Edit", null);
    public static string EnableFallback => L10n.Tr("Enable Fallback", null);
    public static string EntryName => L10n.Tr("Entry Name", null);
    public static string Extensions => L10n.Tr("Extensions", null);
    public static string FormattedWithLocalVariables => L10n.Tr("Formatted with Local Variables", null);
    public static string Id => L10n.Tr("Id", null);
    public static string Key => L10n.Tr("Key", null);
    public static string ListKind => L10n.Tr("List Kind", null);
    public static string LocalVariables => L10n.Tr("Local Variables", null);
    public static string OpenInTablesWindow => L10n.Tr("Open in Tables Window", null);
    public static string OpenResourceTables => L10n.Tr("Open Resource Tables", null);
    public static string Preload => L10n.Tr("Preload", null);
    public static string PreloadBehavior => L10n.Tr("Preload Behavior", null);
    public static string Provider => L10n.Tr("Provider", null);
    public static string Pull => L10n.Tr("Pull", null);
    public static string Push => L10n.Tr("Push", null);
    public static string RemoveUnresolvedLocales => L10n.Tr("Remove Unresolved Locales", null);
    public static string SelectAll => L10n.Tr("Select All", null);
    public static string SelectNone => L10n.Tr("Select None", null);
    public static string Smart => L10n.Tr("Smart", null);
    public static string SmartStringsSettings => L10n.Tr("Smart Strings Settings", null);
    public static string Storage => L10n.Tr("Storage", null);
    public static string UnresolvedLocale => L10n.Tr("Unresolved Locale", null);
    public static string Values => L10n.Tr("Values", null);

    public static string ConnectedFile => L10n.Tr("Connected File", null);
    public static string RemoveMissingPulledKeys => L10n.Tr("Remove Missing Pulled Keys", null);

    public static string LocalesHelp => L10n.Tr("Expand a row for fallback and loading details. Disabled locales are excluded from builds and can't be selected at runtime.", null);
    public static string StartupSelectorsHelp => L10n.Tr("Checked top to bottom when localization starts; the first selector that returns a locale wins. If none succeed, the project locale is used.", null);
    public static string ContentSourcesHelp => L10n.Tr("Where localized tables and assets load from, tried top to bottom until one can load the request.", null);
    public static string SelectCollectionHelp => L10n.Tr("Select or create a Resource Table Collection to edit its entries.", null);
    public static string EditInTablesWindowHelp => L10n.Tr("Keys, values and metadata are edited in the Resource Tables window.", null);
    public static string NoOwningCollectionHelp => L10n.Tr("This asset does not belong to a Resource Table Collection, so it is ignored.", null);
    public static string WizardCreateSettings => L10n.Tr("Create the Localization Settings asset", null);
    public static string WizardAddLocales => L10n.Tr("Add the locales your project supports", null);
    public static string WizardProjectLocale => L10n.Tr("Confirm the project locale (the default language)", null);
    public static string WizardReferenceMode => L10n.Tr("Choose how assets are referenced", null);
}
