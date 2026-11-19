// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder;

/// <summary>
/// What a <see cref="UIBuilderWindow"/> brings back after a domain reload or when it is opened again: the document
/// and the sub-document it edits, the preview theme, the hierarchy rows and the selection.
/// </summary>
/// <remarks>
/// Assets are kept by GUID and elements by the ids of their <see cref="VisualElementAsset"/>s, which are what stays
/// the same when the documents are loaded again. The window's unsaved document is kept by reference: it lasts as
/// long as the window, across domain reloads, and a window opened later does not get it back.
/// </remarks>
[Serializable]
sealed class UIBuilderWindowState
{
    [Serializable]
    public struct SelectedRule
    {
        public string StyleSheetGuid;
        public int RuleIndex;
    }

    /// <summary>The root document when it is the window's unsaved one, which has no GUID.</summary>
    public VisualTreeAsset UnsavedRootDocument;

    public string RootDocumentGuid;
    public int[] SubDocumentPath;
    public SubDocumentOptions SubDocumentOptions;
    public string PanelSettingsGuid;

    /// <summary>A style sheet edited on its own. Its preview document is not kept: it is resolved again.</summary>
    public string StyleSheetGuid;

    /// <summary>The preview theme, as stored by <see cref="PreviewThemeState"/>.</summary>
    public string ThemeKey;

    /// <summary>Expanded and selected rows of the element hierarchy, the search text and the scroll position.</summary>
    public HierarchyViewState HierarchyViewState;

    /// <summary>The selected style rules. When there are any, they are the selection, not the hierarchy rows.</summary>
    public List<SelectedRule> SelectedRules = new();

    public bool HasDocument => UnsavedRootDocument != null || !string.IsNullOrEmpty(RootDocumentGuid) || !string.IsNullOrEmpty(StyleSheetGuid);

    public void Clear()
    {
        UnsavedRootDocument = null;
        RootDocumentGuid = null;
        SubDocumentPath = null;
        SubDocumentOptions = SubDocumentOptions.None;
        PanelSettingsGuid = null;
        StyleSheetGuid = null;
        ThemeKey = null;
        HierarchyViewState = null;
        SelectedRules.Clear();
    }

    /// <summary>
    /// Records <paramref name="styleSheet"/> as edited on its own, in place of a document, previewed with
    /// <paramref name="panelSettings"/>. The view state and the selection are left as they are.
    /// </summary>
    public void SetStyleSheet(StyleSheet styleSheet, PanelSettings panelSettings = null)
    {
        UnsavedRootDocument = null;
        RootDocumentGuid = null;
        SubDocumentPath = null;
        SubDocumentOptions = SubDocumentOptions.None;
        PanelSettingsGuid = GetGuid(panelSettings);
        StyleSheetGuid = GetGuid(styleSheet);
    }

    public bool TryGetStyleSheet(out StyleSheet styleSheet, out PanelSettings panelSettings)
    {
        styleSheet = LoadAsset<StyleSheet>(StyleSheetGuid);
        panelSettings = styleSheet != null ? LoadAsset<PanelSettings>(PanelSettingsGuid) : null;
        return styleSheet != null;
    }

    /// <summary>
    /// Records <paramref name="document"/>. The view state and the selection are left as they are.
    /// </summary>
    public void SetDocument(VisualTreeAssetEditingContext document)
    {
        StyleSheetGuid = null;
        RootDocumentGuid = GetGuid(document.RootVisualTreeAsset);
        UnsavedRootDocument = RootDocumentGuid == null ? document.RootVisualTreeAsset : null;
        PanelSettingsGuid = GetGuid(document.PanelSettings);
        SubDocumentOptions = document.SubDocumentOptions;

        var path = document.SubDocumentPath;
        if (path is not { Length: > 0 })
        {
            SubDocumentPath = null;
            return;
        }

        SubDocumentPath = new int[path.Length];
        for (var i = 0; i < path.Length; ++i)
            SubDocumentPath[i] = path[i].id;
    }

    /// <summary>
    /// Resolves the recorded document. A sub-document that can no longer be found opens its root document instead.
    /// </summary>
    public bool TryGetDocument(out VisualTreeAssetEditingContext document)
    {
        document = default;

        var root = UnsavedRootDocument != null ? UnsavedRootDocument : LoadAsset<VisualTreeAsset>(RootDocumentGuid);
        if (root == null)
            return false;

        var panelSettings = LoadAsset<PanelSettings>(PanelSettingsGuid);
        if (SubDocumentPath is { Length: > 0 } && SubDocumentOptions != SubDocumentOptions.None &&
            TryResolveSubDocumentPath(root, SubDocumentPath, out var path))
        {
            document = new VisualTreeAssetEditingContext(root, path, SubDocumentOptions, panelSettings);
            return true;
        }

        document = new VisualTreeAssetEditingContext(root, panelSettings);
        return true;
    }

    public void SetSelectedRules(IEnumerable<UnityEngine.Object> selectedObjects)
    {
        SelectedRules.Clear();
        foreach (var selected in selectedObjects)
        {
            if (selected is not StyleRuleSelection { StyleRule: { styleSheet: { } styleSheet } rule })
                continue;

            var guid = GetGuid(styleSheet);
            var index = Array.IndexOf(styleSheet.rules, rule);
            if (guid != null && index >= 0)
                SelectedRules.Add(new SelectedRule { StyleSheetGuid = guid, RuleIndex = index });
        }
    }

    public List<StyleRule> ResolveSelectedRules()
    {
        var rules = new List<StyleRule>();
        foreach (var selected in SelectedRules)
        {
            var styleSheet = LoadAsset<StyleSheet>(selected.StyleSheetGuid);
            if (styleSheet != null && selected.RuleIndex >= 0 && selected.RuleIndex < styleSheet.rules.Length)
                rules.Add(styleSheet.rules[selected.RuleIndex]);
        }

        return rules;
    }

    static bool TryResolveSubDocumentPath(VisualTreeAsset root, int[] templateIds, out TemplateAsset[] path)
    {
        path = new TemplateAsset[templateIds.Length];
        var document = root;
        for (var i = 0; i < templateIds.Length; ++i)
        {
            if (document == null)
                return false;

            TemplateAsset found = null;
            foreach (var template in document.DepthFirstTraversalOfType<TemplateAsset>())
            {
                if (template.id == templateIds[i])
                {
                    found = template;
                    break;
                }
            }

            if (found == null)
                return false;

            path[i] = found;
            document = found.ResolveTemplate();
        }

        return VisualTreeAssetEditingContext.ValidateSubDocumentIsPartOrMainAssetHierarchy(root, path);
    }

    static string GetGuid(UnityEngine.Object asset)
    {
        if (asset == null)
            return null;

        var path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
    }

    static T LoadAsset<T>(string guid) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
