// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// What the UI Builder reports about a UI document it saved. The field names are those of the
/// <c>uiBuilderSave</c> analytics event.
/// </summary>
[Serializable]
sealed class UIBuilderSaveEventData
{
    public string assetGuid; // uxml asset guid
    public long assetSize; // uxml asset size, in bytes
    public int elements; // number of elements in the Builder Hierarchy
    public int depth; // maximum depth of the Builder Hierarchy
    public int styleSheets; // number of USS assets referenced by the UXML document
    public int selectors; // total number of USS selectors
    public int inlineStyles; // number of inline styles on the elements
    public int editorElements; // number of Editor-only elements in the Hierarchy
    public int customElements; // number of custom elements in the Hierarchy (to the current project)
    public bool editorExtension; // whether or not using Editor elements is allowed
    public string[] features; // whether or not specific features are used.
}

/// <summary>
/// Sends the <c>uiBuilderSave</c> analytics event, which tells how big UI documents get and which UI Toolkit
/// features they use.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class UIBuilderAnalytics
{
    internal const string SaveEventName = "uiBuilderSave";
    const string k_UIElementsModuleName = "UnityEngine.UIElementsModule";

    // The names go out with the event: keep them as they are.
    [Flags]
    internal enum Features
    {
        None                = 0,
        Instances           = 1 << 0,
        Transitions         = 1 << 1,
        Transforms          = 1 << 2,
        Imports             = 1 << 3,
        CustomProperties    = 1 << 4,
        AttributeOverrides  = 1 << 5,
        Variables           = 1 << 6,
        ListView            = 1 << 7,
        TreeView            = 1 << 8,
        DataBinding         = 1 << 9,
        UserDataBinding     = 1 << 10,
        CustomBinding       = 1 << 11,
        UxmlSerialization   = 1 << 13,
    }

    /// <summary>
    /// The data of the last save event, sent or not. For tests.
    /// </summary>
    [NoAutoStaticsCleanup] // only primitives and strings, safe to keep across code reloads
    internal static UIBuilderSaveEventData LastSaveEventData { get; private set; }

    /// <summary>
    /// Sends the save event of the document edited in <paramref name="context"/>, which was just written to its file.
    /// Nothing is sent from tests or batch mode.
    /// </summary>
    /// <param name="startTime">When the save started, in UTC.</param>
    /// <param name="context">The context the saved document is edited in.</param>
    public static void SendSaveEvent(DateTime startTime, VisualTreeAssetEditingContext context)
    {
        // The save already happened: a failure here must only cost the event.
        try
        {
            var data = CollectSaveEventData(context);
            if (data == null)
                return;

            LastSaveEventData = data;

            var now = DateTime.UtcNow;
            if (InternalEditorUtility.isHumanControllingUs)
                UsabilityAnalytics.SendEvent(SaveEventName, now, now - startTime, true, data);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Gathers the data of the save event of the document edited in <paramref name="context"/>, or returns null when
    /// it has no file.
    /// </summary>
    internal static UIBuilderSaveEventData CollectSaveEventData(VisualTreeAssetEditingContext context)
    {
        var document = context.EditedVisualTreeAsset;
        var path = document != null ? AssetDatabase.GetAssetPath(document) : null;
        if (string.IsNullOrEmpty(path))
            return null;

        // Resolves a package path to where the package is on disk, which is outside the project for a local one.
        var fullPath = Path.GetFullPath(path);
        var data = new UIBuilderSaveEventData
        {
            assetGuid = AssetDatabase.AssetPathToGUID(path),
            assetSize = File.Exists(fullPath) ? File.ReadAllText(fullPath).Length : 0, // in characters
            editorExtension = EditorExtensionAuthoring.IsEnabled(document),
        };

        var root = document.visualTreeNoAlloc;
        if (root != null)
        {
            for (var i = 0; i < root.childCount; ++i)
            {
                data.depth = Math.Max(data.depth, CountElements(root[i], data));
            }

            if (root.stylesheets != null)
            {
                foreach (var styleSheet in root.stylesheets)
                {
                    if (styleSheet == null)
                        continue;

                    ++data.styleSheets;
                    data.selectors += CountSelectors(styleSheet);
                }
            }
        }

        // The documents a sub-document is opened from count too.
        data.inlineStyles = CountInlineStyles(context.RootVisualTreeAsset);
        if (context.SubDocumentPath != null)
        {
            foreach (var template in context.SubDocumentPath)
            {
                data.inlineStyles += CountInlineStyles(template?.ResolveTemplate());
            }
        }

        var features = GetFeatures(document);
        data.features = features == Features.None
            ? Array.Empty<string>()
            : features.ToString().Replace(" ", "").Split(',');

        return data;
    }

    // Counts the asset and the assets under it, and returns how many levels deep they go. UXML objects, such as
    // bindings and the field that holds them, count as elements.
    static int CountElements(UxmlAsset asset, UIBuilderSaveEventData data)
    {
        ++data.elements;

        var type = ResolveType(asset.fullTypeName);
        var category = LibraryOrdering.GetCategory(type);
        if (asset.fullTypeName.Contains("UnityEditor") || category is { IsEditorOnly: true })
            ++data.editorElements;
        else if (asset.fullTypeName != TemplateAsset.UxmlInstanceTypeName && category == null)
            ++data.customElements;

        var depth = 0;
        for (var i = 0; i < asset.childCount; ++i)
        {
            depth = Math.Max(depth, CountElements(asset[i], data));
        }

        return depth + 1;
    }

    static int CountInlineStyles(VisualTreeAsset document)
    {
        if (document == null || document.inlineSheet == null)
            return 0;

        var count = 0;
        foreach (var rule in document.inlineSheet.rules)
        {
            count += rule.properties.Length;
        }

        return count;
    }

    static int CountSelectors(StyleSheet styleSheet)
    {
        var count = 0;
        foreach (var rule in styleSheet.rules)
        {
            if (rule.complexSelectors != null)
                count += rule.complexSelectors.Length;
        }

        return count;
    }

    static Features GetFeatures(VisualTreeAsset document)
    {
        var features = Features.None;

        using (ListPool<StyleSheet>.Get(out var styleSheets))
        {
            styleSheets.AddRange(document.stylesheets);
            if (document.inlineSheet != null)
                styleSheets.Add(document.inlineSheet);

            foreach (var styleSheet in styleSheets)
            {
                if (styleSheet == null)
                    continue;

                if (styleSheet.imports?.Length > 0)
                    features |= Features.Imports;

                foreach (var rule in styleSheet.rules)
                {
                    foreach (var property in rule.properties)
                    {
                        if (property.isCustomProperty)
                            features |= Features.CustomProperties;

                        StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(property.name, out var id);
                        if (IsTransition(id))
                            features |= Features.Transitions;
                        else if (id is StylePropertyId.TransformOrigin or StylePropertyId.Translate
                                 or StylePropertyId.Rotate or StylePropertyId.Scale)
                            features |= Features.Transforms;

                        if (property.ContainsVariable())
                            features |= Features.Variables;
                    }
                }
            }
        }

        HashSet<string> userAssemblies = null;
        foreach (var asset in document.DepthFirstTraversal())
        {
            switch (asset)
            {
                case TemplateAsset template:
                    features |= Features.Instances;
                    if (template.attributeOverrides.Count > 0)
                        features |= Features.AttributeOverrides;
                    break;

                case VisualElementAsset element:
                    if (element.fullTypeName == typeof(ListView).FullName)
                        features |= Features.ListView;
                    else if (element.fullTypeName == typeof(TreeView).FullName)
                        features |= Features.TreeView;

                    if (element.serializedData != null)
                    {
                        userAssemblies ??= new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
                        var type = ResolveType(element.fullTypeName);
                        if (type != null && userAssemblies.Contains(type.Assembly.GetName().Name + ".dll"))
                            features |= Features.UxmlSerialization;
                    }
                    break;

                case UxmlObjectAsset uxmlObject:
                    if (uxmlObject.fullTypeName == typeof(DataBinding).FullName)
                        features |= Features.DataBinding;
                    else if (IsUserType<DataBinding>(uxmlObject.fullTypeName))
                        features |= Features.UserDataBinding;
                    else if (IsUserType<CustomBinding>(uxmlObject.fullTypeName))
                        features |= Features.CustomBinding;
                    break;
            }
        }

        return features;
    }

    static bool IsTransition(StylePropertyId id)
        => id is StylePropertyId.Transition or StylePropertyId.TransitionDelay or StylePropertyId.TransitionDuration
            or StylePropertyId.TransitionProperty or StylePropertyId.TransitionTimingFunction;

    // Whether the type is a T that does not come with UI Toolkit.
    static bool IsUserType<T>(string fullTypeName)
    {
        var type = ResolveType(fullTypeName);
        return type != null && typeof(T).IsAssignableFrom(type) && type.Assembly.GetName().Name != k_UIElementsModuleName;
    }

    static Type ResolveType(string fullTypeName)
        => UxmlSerializedDataRegistry.GetDescription(fullTypeName)?.serializedDataType?.DeclaringType;
}
