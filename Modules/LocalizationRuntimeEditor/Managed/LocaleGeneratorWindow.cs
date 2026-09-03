// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

/// <summary>
/// Provides a window that adds locales to the active localization settings by picking from the system cultures.
/// </summary>
/// <remarks>
/// This is a rebuild of the localization package's Locale Generator. Because module locales are plain objects, it
/// adds them to the <see cref="LocalizationSettings"/> locale list rather than creating per-locale assets.
/// </remarks>
class LocaleGeneratorWindow : EditorWindow
{
    const string k_Window = "LocalizationRuntime/UXML/LocaleGeneratorWindow.uxml";
    const string k_Item = "LocalizationRuntime/UXML/LocaleGeneratorItem.uxml";

    internal class Row
    {
        public LocaleIdentifier Id;
        public string EnglishName;
        public string Code;
        public bool InProject;
        public bool Selected;
        public List<Row> Children;
    }

    readonly List<Row> m_All = new();
    readonly List<Row> m_Roots = new();
    readonly List<TreeViewItemData<Row>> m_Items = new();
    VisualTreeAsset m_ItemTemplate;
    MultiColumnTreeView m_Tree;
    Action m_OnLocalesAdded;
    Button m_AddButton;
    string m_Search = string.Empty;

    /// <summary>
    /// Opens the window.
    /// </summary>
    /// <param name="onLocalesAdded">A callback invoked once locales are added to the settings.</param>
    public static void ShowWindow(Action onLocalesAdded = null)
    {
        var window = GetWindow<LocaleGeneratorWindow>(true, L10n.Tr("Add locales", null));
        window.m_OnLocalesAdded = onLocalesAdded;
        window.minSize = new Vector2(440, 460);
    }

    void CreateGUI() => BuildUI(rootVisualElement);

    internal void BuildUI(VisualElement root)
    {
        BuildChoices();

        (EditorGUIUtility.Load(k_Window) as VisualTreeAsset).CloneTree(root);
        m_ItemTemplate = EditorGUIUtility.Load(k_Item) as VisualTreeAsset;

        var search = root.Q<ToolbarSearchField>("search");
        search.RegisterValueChangedCallback(evt => { m_Search = evt.newValue ?? string.Empty; ApplyFilter(); });

        search.schedule.Execute(() => (search.Q<TextField>() as VisualElement ?? search).Focus());

        m_Tree = root.Q<MultiColumnTreeView>("list");
        SetupColumns();

        m_AddButton = root.Q<Button>("add-locales");
        m_AddButton.text = L10n.Tr("Add locales", null);
        m_AddButton.clicked += AddSelected;

        ApplyFilter();
        UpdateAddButton();
    }

    void SetupColumns()
    {
        var enabled = m_Tree.columns["enabled"];
        enabled.makeCell = MakeToggleCell;
        enabled.bindCell = BindToggleCell;

        var name = m_Tree.columns["name"];
        name.makeCell = MakeLabelCell;
        name.bindCell = (element, index) => BindLabelCell(element, index, code: false);

        var codeColumn = m_Tree.columns["code"];
        codeColumn.makeCell = MakeLabelCell;
        codeColumn.bindCell = (element, index) => BindLabelCell(element, index, code: true);
    }

    bool HasSelection()
    {
        foreach (var row in m_All)
        {
            if (row.Selected && !row.InProject)
                return true;
        }
        return false;
    }

    void UpdateAddButton()
    {
        m_AddButton.SetEnabled(HasSelection());
    }

    void BuildChoices()
    {
        m_All.Clear();
        m_Roots.Clear();

        var settings = LocalizationEditorSettings.ActiveSettings;
        var byCode = new Dictionary<string, Row>(StringComparer.OrdinalIgnoreCase);
        var groupCodes = new List<string>();

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            if (culture.LCID == CultureInfo.InvariantCulture.LCID)
                continue;
            if (culture.EnglishName.Contains("Legacy"))
                continue;
            if (byCode.ContainsKey(culture.Name))
                continue;

            var id = new LocaleIdentifier(culture.Name);
            var row = new Row
            {
                Id = id,
                EnglishName = culture.EnglishName,
                Code = culture.Name,
                InProject = settings != null && settings.GetLocale(id) != null
            };
            byCode.Add(row.Code, row);
            m_All.Add(row);
            groupCodes.Add(GroupCode(culture));
        }

        for (var i = 0; i < m_All.Count; i++)
        {
            var row = m_All[i];
            if (!string.Equals(row.Code, groupCodes[i], StringComparison.OrdinalIgnoreCase)
                && byCode.TryGetValue(groupCodes[i], out var parent))
            {
                parent.Children ??= new List<Row>();
                parent.Children.Add(row);
            }
            else
            {
                m_Roots.Add(row);
            }
        }

        m_Roots.Sort(CompareByName);
        foreach (var root in m_Roots)
            root.Children?.Sort(CompareByName);
    }

    static int CompareByName(Row a, Row b) => string.Compare(a.EnglishName, b.EnglishName, StringComparison.OrdinalIgnoreCase);

    static string GroupCode(CultureInfo culture)
    {
        var current = culture;
        while (current.Parent != null && current.Parent != current && !string.IsNullOrEmpty(current.Parent.Name))
            current = current.Parent;
        return current.Name;
    }

    VisualElement MakeToggleCell()
    {
        var toggle = m_ItemTemplate.Instantiate().Q<Toggle>("sel");
        toggle.RegisterValueChangedCallback(evt =>
        {
            if (toggle.userData is Row row && !row.InProject)
            {
                row.Selected = evt.newValue;
                UpdateAddButton();
            }
        });
        return toggle;
    }

    void BindToggleCell(VisualElement element, int index)
    {
        var row = m_Tree.GetItemDataForIndex<Row>(index);
        var toggle = (Toggle)element;
        toggle.userData = row;
        toggle.SetValueWithoutNotify(row.InProject || row.Selected);
        toggle.SetEnabled(!row.InProject);
        toggle.tooltip = row.InProject ? L10n.Tr("Already in the project.", null) : null;
    }

    static VisualElement MakeLabelCell()
    {
        var label = new Label();
        label.AddToClassList(LocClasses.LocaleGenCell);
        return label;
    }

    void BindLabelCell(VisualElement element, int index, bool code)
    {
        var row = m_Tree.GetItemDataForIndex<Row>(index);
        var label = (Label)element;
        label.text = code ? row.Code : row.EnglishName;
        label.EnableInClassList(LocClasses.LocaleGenInProject, row.InProject);
    }

    void ApplyFilter()
    {
        m_Items.Clear();
        var searching = !string.IsNullOrEmpty(m_Search);
        var expand = new List<int>();
        var id = 0;

        foreach (var root in m_Roots)
        {
            // A matching language keeps all of its regions; otherwise it keeps only the regions that match themselves.
            var rootMatches = Matches(root);
            List<TreeViewItemData<Row>> children = null;
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                {
                    if (!rootMatches && !Matches(child))
                        continue;
                    children ??= new List<TreeViewItemData<Row>>();
                    children.Add(new TreeViewItemData<Row>(id++, child));
                }
            }

            if (!rootMatches && children == null)
                continue;

            var rootId = id++;
            m_Items.Add(new TreeViewItemData<Row>(rootId, root, children));
            if (searching && children != null)
                expand.Add(rootId);
        }

        if (m_Tree != null)
        {
            m_Tree.SetRootItems(m_Items);
            m_Tree.Rebuild();
            foreach (var rootId in expand)
                m_Tree.ExpandItem(rootId);
        }
        UpdateAddButton();
    }

    bool Matches(Row row)
    {
        return string.IsNullOrEmpty(m_Search)
            || row.EnglishName.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0
            || row.Code.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void AddSelected()
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
        {
            Debug.LogWarning("No active Localization settings; create one in Project Settings > Localization first.");
            return;
        }

        var added = new List<Locale>();
        Undo.RegisterCompleteObjectUndo(settings, "Add Locales");
        foreach (var row in m_All)
        {
            if (!row.Selected || row.InProject)
                continue;
            var locale = new Locale(row.Id.Code, row.EnglishName);
            settings.AddLocale(locale);
            added.Add(locale);
        }

        foreach (var locale in added)
        {
            var parent = locale.CultureInfo?.Parent;
            while (parent != null && !parent.Equals(CultureInfo.InvariantCulture))
            {
                var found = settings.GetLocale(new LocaleIdentifier(parent.Name));
                if (found != null && found != locale)
                {
                    locale.FallbackCode = found.Code;
                    break;
                }
                parent = parent.Parent;
            }
        }

        if (added.Count > 0)
        {
            EditorUtility.SetDirty(settings);
            LanguageToolbar.Refresh();
            m_OnLocalesAdded?.Invoke();
        }
        Close();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
