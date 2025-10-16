// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    [Serializable]
    class BuilderSaveEventData
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

    [Flags]
    enum Features
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
        UxmlSerialization   = 1 << 13
    }

    internal static class BuilderAnalyticsUtility
    {
        internal const string uieCoreModule = "UnityEngine.UIElementsModule";

        // Used in tests
        public static BuilderSaveEventData cachedSaveEventData { get; private set; }

        static HashSet<string> userAssemblies;
        internal static Dictionary<string, Type> typeCache;

        /// <summary>
        /// Gets the feature usage in a uxml file
        /// </summary>
        private static string[] GetFeatureUsage(VisualTreeAsset vta)
        {
            var features = Features.None;
            var stylesheets = new List<StyleSheet>();
            stylesheets.AddRange(vta.stylesheets);

            if (vta.inlineSheet != null)
            {
                stylesheets.Add(vta.inlineSheet);
            }

            foreach (var stylesheet in stylesheets)
            {
                if (stylesheet.imports?.Length > 0)
                    features |= Features.Imports;

                foreach (var rule in stylesheet.rules)
                {
                    if (rule.customPropertiesCount > 0)
                        features |= Features.CustomProperties;

                    foreach (var property in rule.properties)
                    {
                        StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(property.name, out var id);
                        if (id.IsTransitionId())
                        {
                            features |= Features.Transitions;
                        }
                        else if (id is StylePropertyId.TransformOrigin or StylePropertyId.Translate
                                 or StylePropertyId.Rotate or StylePropertyId.Scale)
                        {
                            features |= Features.Transforms;
                        }

                        if (property.ContainsVariable())
                        {
                            features |= Features.Variables;
                        }
                    }
                }
            }

            using var templatesListPool = ListPool<TemplateAsset>.Get(out var templateAssets);
            templateAssets.AddRange(vta.DepthFirstTraversalOfType<TemplateAsset>());

            using var allAssetsListPool = ListPool<UxmlAsset>.Get(out var allAssets);
            allAssets.AddRange(vta.DepthFirstTraversal());

            using var uxmlObjectsPool = ListPool<UxmlObjectAsset>.Get(out var uxmlObjects);
            uxmlObjects.AddRange(vta.DepthFirstTraversalOfType<UxmlObjectAsset>());

            using var visualElementAssetsListPool = ListPool<VisualElementAsset>.Get(out var visualElementAssets);
            visualElementAssets.AddRange(vta.DepthFirstTraversalOfType<VisualElementAsset>());

            if (templateAssets.Count > 0)
            {
                features |= Features.Instances;

                if (templateAssets.Exists(x => x.attributeOverrides.Count > 0))
                {
                    features |= Features.AttributeOverrides;
                }
            }

            if (visualElementAssets.Exists(x => x.fullTypeName == typeof(ListView).FullName))
            {
                features |= Features.ListView;
            }
            if (visualElementAssets.Exists(x => x.fullTypeName == typeof(TreeView).FullName))
            {
                features |= Features.TreeView;
            }
            if (uxmlObjects.Exists(x => x.fullTypeName == typeof(DataBinding).FullName))
            {
                features |= Features.DataBinding;
            }
            if (uxmlObjects.Exists(IsCustomBinding<DataBinding>))
            {
                features |= Features.UserDataBinding;
            }
            if (uxmlObjects.Exists(IsCustomBinding<CustomBinding>))
            {
                features |= Features.CustomBinding;
            }
            if (visualElementAssets.Exists(x => x.serializedData != null && IsCustomElement(x)))
            {
                features |= Features.UxmlSerialization;
            }

            return features == 0 ? Array.Empty<string>() : features.ToString().Replace(" ", "").Split(",");
        }

        static bool IsCustomBinding<T>(UxmlObjectAsset o)
        {
            var desc = UxmlSerializedDataRegistry.GetDescription(o.fullTypeName);
            if (desc != null)
            {
                var uxmlType = desc.serializedDataType.DeclaringType;
                return typeof(T).IsAssignableFrom(uxmlType) && uxmlType.Assembly.GetName().Name != uieCoreModule;
            }
            return false;
        }

        static bool IsCustomElement(VisualElementAsset vta)
        {
            return typeCache.ContainsKey(vta.fullTypeName);
        }

        /// <summary>
        /// It gathers and sends the data for the builder save event
        /// </summary>
        public static void SendSaveEvent(DateTime startTime, BuilderDocumentOpenUXML openUxml, string uxmlPath,
            int assetSize)
        {
            try
            {
                // If we encounter any error while collecting the data we should still be able to save and skip
                // the analytics call
                var vta = openUxml.visualTreeAsset;
                var openUssFiles = openUxml.openUSSFiles;
                var openUxmlFiles = openUxml.openUXMLFiles;
                var editorExtensionMode = openUxml.fileSettings.editorExtensionMode;
                var elementsInfo = VisualTreeAssetUtilities.GetElementsInfo(vta);

                if (typeCache == null)
                {
                    userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
                    typeCache = new Dictionary<string, Type>();

                    var assemblies = CurrentAssemblies.GetLoadedAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        if (!userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                            continue;

                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.FullName != null) typeCache[type.FullName] = type;
                        }
                    }
                }

                var features = GetFeatureUsage(vta);

                var selectorCount = 0;
                foreach (var styleSheetFile in openUssFiles)
                {
                    selectorCount += styleSheetFile.GetComplexSelectorsCount();
                }

                var inlineStyleCount = 0;
                foreach (var openUxmlFile in openUxmlFiles)
                {
                    if (openUxmlFile.visualTreeAsset.inlineSheet == null)
                    {
                        continue;
                    }

                    foreach (var styleRule in openUxmlFile.visualTreeAsset.inlineSheet.rules)
                    {
                        inlineStyleCount += styleRule.properties.Length;
                    }
                }

                var guid = AssetDatabase.AssetPathToGUID(uxmlPath);

                var saveEventData = new BuilderSaveEventData
                {
                    assetGuid = guid,
                    assetSize = assetSize,
                    elements = elementsInfo.elements,
                    depth = elementsInfo.hierarchyDepth,
                    styleSheets = openUssFiles.Count,
                    selectors = selectorCount,
                    inlineStyles = inlineStyleCount,
                    editorElements = elementsInfo.editorElements,
                    customElements = elementsInfo.customElements,
                    editorExtension = editorExtensionMode,
                    features = features
                };

                cachedSaveEventData = saveEventData;

                var now = DateTime.UtcNow;
                var duration = now - startTime;

                // This check avoids analytics sent by tests or batch mode
                if (InternalEditorUtility.isHumanControllingUs)
                {
                    UsabilityAnalytics.SendEvent("uiBuilderSave", now, duration, true, saveEventData);
                }
            }
            catch (Exception e)
            {
                // If there's any error we should log it to potentially fix it
                Debug.LogError(e);
            }
        }
    }
}
