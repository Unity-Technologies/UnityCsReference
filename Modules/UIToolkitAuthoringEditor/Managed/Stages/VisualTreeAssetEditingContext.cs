// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct VisualTreeAssetEditingContext
{
    public readonly VisualTreeAsset RootVisualTreeAsset;
    public readonly TemplateAsset[] SubDocumentPath;
    public readonly SubDocumentOptions SubDocumentOptions;
    public readonly PanelSettings PanelSettings;

    public VisualTreeAsset EditedVisualTreeAsset
    {
        get
        {
            if (SubDocumentPath == null || SubDocumentPath.Length == 0)
                return RootVisualTreeAsset;
            return SubDocumentPath[^1]?.ResolveTemplate();
        }
    }

    /// <summary>
    /// Creates an isolation editing context for the main <see cref="VisualTreeAsset"/> asset.
    /// </summary>
    /// <param name="visualTreeAsset">The <see cref="VisualTreeAsset"/> asset that will be edited.</param>
    /// <param name="panelSettings">The panel settings that should be used for previewing purposes.</param>
    public VisualTreeAssetEditingContext(VisualTreeAsset visualTreeAsset, PanelSettings panelSettings = null)
        : this(visualTreeAsset, null, SubDocumentOptions.None, panelSettings)
    {
    }

    /// <summary>
    /// Creates an editing context.
    /// </summary>
    /// <param name="visualTreeAsset">The main <see cref="VisualTreeAsset"/> asset.</param>
    /// <param name="subDocumentPath">The path of <see cref="TemplateAsset"/> assets leading to the asset that will be edited.</param>
    /// <param name="subDocumentOptions">Options to open the edition in context or in isolation, when a valid <paramref name="subDocumentPath"/> is provided.</param>
    /// <param name="panelSettings">The panel settings that should be used for previewing purposes.</param>
    public VisualTreeAssetEditingContext(VisualTreeAsset visualTreeAsset, TemplateAsset[] subDocumentPath, SubDocumentOptions subDocumentOptions = SubDocumentOptions.InContext, PanelSettings panelSettings = null)
    {
        ValidateContext(visualTreeAsset, subDocumentPath, subDocumentOptions, panelSettings);

        RootVisualTreeAsset = visualTreeAsset;
        SubDocumentPath = subDocumentPath;
        SubDocumentOptions = SubDocumentPath == null  || subDocumentPath.Length == 0 ? SubDocumentOptions.None : subDocumentOptions;
        PanelSettings = panelSettings;
    }

    private static void ValidateContext(VisualTreeAsset visualTreeAsset, TemplateAsset[] subDocumentPath, SubDocumentOptions subDocumentOptions, PanelSettings panelSettings)
    {
        if (!visualTreeAsset)
            throw new ArgumentException($"Cannot edit without a root {nameof(VisualTreeAsset)}.", nameof(visualTreeAsset));
        switch (subDocumentOptions)
        {
            case SubDocumentOptions.None:
                if (subDocumentPath != null && subDocumentPath.Length > 0)
                    throw new ArgumentException($"Invalid sub-document options '{subDocumentOptions}' provided to edit a sub-document of a {nameof(VisualTreeAsset)}. Please use either '{nameof(SubDocumentOptions.Isolation)}' or '{nameof(SubDocumentOptions.InContext)}'.", nameof(subDocumentOptions));
                break;
            case SubDocumentOptions.InContext:
            case SubDocumentOptions.Isolation:
                if (subDocumentPath == null || subDocumentPath.Length == 0)
                    throw new ArgumentException($"Invalid sub-document options '{subDocumentOptions}' provided to edit a root {nameof(VisualTreeAsset)}. Please use '{nameof(SubDocumentOptions.None)}'.", nameof(subDocumentOptions));
                if (!ValidateSubDocumentIsPartOrMainAssetHierarchy(visualTreeAsset, subDocumentPath))
                    throw new ArgumentException($"Provided {nameof(TemplateAsset)} is not part of the '{visualTreeAsset.name}' {nameof(VisualTreeAsset)}.", nameof(subDocumentPath));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(subDocumentOptions), subDocumentOptions, null);
        }
    }

    internal static bool ValidateSubDocumentIsPartOrMainAssetHierarchy(VisualTreeAsset visualTreeAsset, Span<TemplateAsset> subDocumentPath)
    {
        if (subDocumentPath == null || subDocumentPath.Length == 0)
            return false;

        foreach (var doc in subDocumentPath)
        {
            if (doc == null)
                return false;
        }

        var subDocument = subDocumentPath[0];
        foreach (var template in visualTreeAsset.DepthFirstTraversalOfType<TemplateAsset>())
        {
            var subDocumentVta = subDocument.ResolveTemplate();
            var templateVta = template.ResolveTemplate();

            if (subDocumentVta == templateVta && subDocument.id == template.id)
            {
                if (subDocumentPath.Length == 1)
                    return true;

                if (templateVta && ValidateSubDocumentIsPartOrMainAssetHierarchy(templateVta, subDocumentPath[1..]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reimports the assets being used by the editing context.
    /// </summary>
    /// <param name="context">The context to reimport</param>
    /// <returns>A new instance of the context, with the assets reloaded.</returns>
    public static VisualTreeAssetEditingContext Reload(VisualTreeAssetEditingContext context)
    {
        var rootPath = AssetDatabase.GetAssetPath(context.RootVisualTreeAsset);

        var path = context.SubDocumentPath;
        if (context.SubDocumentPath != null && context.SubDocumentPath.Length > 0)
        {
            path = new TemplateAsset[context.SubDocumentPath.Length];
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(rootPath);
            for (var i = 0; i < context.SubDocumentPath.Length; ++i)
            {
                var templateId = context.SubDocumentPath[i].id;
                foreach (var templateAsset in vta.DepthFirstTraversalOfType<TemplateAsset>())
                {
                    if (templateAsset.id == templateId)
                    {
                        path[i] = templateAsset;
                        vta = templateAsset.ResolveTemplate();
                        break;
                    }
                }
            }
        }

        return new VisualTreeAssetEditingContext(
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(rootPath),
            path,
            context.SubDocumentOptions,
            context.PanelSettings
        );
    }

    /// <summary>
    /// Reimports the assets being used by the editing context.
    /// </summary>
    /// <param name="context">The context to reimport</param>
    /// <returns>A new instance of the context, with the assets reloaded.</returns>
    public static VisualTreeAssetEditingContext Reimport(VisualTreeAssetEditingContext context)
    {
        using (new AssetDatabase.AssetEditingScope())
        {
            var rootPath = AssetDatabase.GetAssetPath(context.RootVisualTreeAsset);

            var path = context.SubDocumentPath;
            if (context.SubDocumentPath != null && context.SubDocumentPath.Length > 0)
            {
                var template = context.SubDocumentPath[^1];
                var editedVisualTreeAsset = template.ResolveTemplate();

                ReimportReferencedStyleSheets(editedVisualTreeAsset);

                var editedPath = AssetDatabase.GetAssetPath(editedVisualTreeAsset);
                AssetDatabase.ImportAsset(editedPath, ImportAssetOptions.ForceSynchronousImport);
                path = new TemplateAsset[context.SubDocumentPath.Length];
                var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(rootPath);
                for (var i = 0; i < context.SubDocumentPath.Length; ++i)
                {
                    var templateId = context.SubDocumentPath[i].id;
                    foreach (var templateAsset in vta.DepthFirstTraversalOfType<TemplateAsset>())
                    {
                        if (templateAsset.id == templateId)
                        {
                            path[i] = templateAsset;
                            vta = templateAsset.ResolveTemplate();
                            break;
                        }
                    }
                }
            }
            else
            {
                ReimportReferencedStyleSheets(context.RootVisualTreeAsset);
                AssetDatabase.ImportAsset(rootPath, ImportAssetOptions.ForceSynchronousImport);
            }

            return new VisualTreeAssetEditingContext(
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(rootPath),
                path,
                context.SubDocumentOptions,
                context.PanelSettings
            );
        }

        static void ReimportReferencedStyleSheets(VisualTreeAsset vta)
        {
            var styleSheets = vta.GetAllReferencedStyleSheets();
            foreach (var styleSheet in styleSheets)
            {
                var styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
                AssetDatabase.ImportAsset(styleSheetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }

    public VisualElementEditFlags GetElementEditFlags(VisualElement element)
    {
        var hasVea = element.visualElementAsset != null;
        switch (SubDocumentOptions)
        {
            case SubDocumentOptions.None:
            {
                if (element.visualTreeAssetSource == RootVisualTreeAsset)
                    return VisualElementEditFlags.FullyEditable;
                return hasVea && !string.IsNullOrEmpty(element.name)
                    ? VisualElementEditFlags.Attributes
                    : VisualElementEditFlags.None;
            }
            case SubDocumentOptions.Isolation:
            {
                var templateAsset = SubDocumentPath[^1];
                var visualTreeAsset = templateAsset.ResolveTemplate();

                if (element.visualTreeAssetSource == visualTreeAsset)
                    return VisualElementEditFlags.FullyEditable;
                return hasVea && !string.IsNullOrEmpty(element.name)
                    ? VisualElementEditFlags.Attributes
                    : VisualElementEditFlags.None;
            }
            case SubDocumentOptions.InContext:
                return GetInContextElementEditFlags(element);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset)
    {
        using var _ = HashSetPool<string>.Get(out var visitedSet);
        visitedSet.Add(AssetDatabase.GetAssetPath(RootVisualTreeAsset));
        if (SubDocumentPath != null)
        {
            for (var i = 0; i < SubDocumentPath.Length; ++i)
            {
                var subDocument = SubDocumentPath[i];
                var template = subDocument.ResolveTemplate();
                visitedSet.Add(AssetDatabase.GetAssetPath(template));
            }
        }

        return WillCauseCircularDependency(visualTreeAsset, visitedSet);
    }

    private bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset, HashSet<string> visitedPaths)
    {
        if (!visitedPaths.Add(AssetDatabase.GetAssetPath(visualTreeAsset)))
            return true;

        foreach (var template in visualTreeAsset.templateDependencies)
        {
            if (WillCauseCircularDependency(template, visitedPaths))
                return true;
        }

        return false;
    }

    private VisualElementEditFlags GetInContextElementEditFlags(VisualElement element)
    {
        if (element.visualTreeAssetSource == RootVisualTreeAsset)
            return VisualElementEditFlags.None;

        using var _ = ListPool<TemplateAsset>.Get(out var path);
        GetPath(element, path);
        if (path.Count < SubDocumentPath.Length)
            return VisualElementEditFlags.None;

        for (var i = 0; i < SubDocumentPath.Length; ++i)
        {
            if (SubDocumentPath[i] != path[i])
                return VisualElementEditFlags.None;

            if (SubDocumentPath[i].id != path[i].id)
                return VisualElementEditFlags.None;
        }

        var templateAsset = SubDocumentPath[^1];
        var visualTreeAsset = templateAsset.ResolveTemplate();
        if (element.visualTreeAssetSource == visualTreeAsset)
            return VisualElementEditFlags.FullyEditable;
        return !string.IsNullOrEmpty(element.name)
            ? VisualElementEditFlags.Attributes
            : VisualElementEditFlags.None;
    }

    // Internal for tests.
    internal static void GetPath(VisualElement element, List<TemplateAsset> path)
    {
        if (element == null)
            return;

        var parentContainer = element.GetFirstAncestorOfType<TemplateContainer>();

        GetPath(parentContainer, path);

        if (element.templateAsset != null)
            path.Add(element.templateAsset);
    }
}
