using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetExtensions
    {
        public static readonly FieldInfo UsingsListFieldInfo =
            typeof(VisualTreeAsset).GetField("m_Usings", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly IComparer<VisualTreeAsset.UsingEntry> s_UsingEntryPathComparer = new UsingEntryPathComparer();

        class UsingEntryPathComparer : IComparer<VisualTreeAsset.UsingEntry>
        {
            public int Compare(VisualTreeAsset.UsingEntry x, VisualTreeAsset.UsingEntry y)
            {
                return Comparer<string>.Default.Compare(x.path, y.path);
            }
        }

        public static VisualTreeAsset DeepCopy(this VisualTreeAsset vta)
        {
            var newTreeAsset = VisualTreeAssetUtilities.CreateInstance();

            vta.DeepOverwrite(newTreeAsset);

            return newTreeAsset;
        }

        public static void DeepOverwrite(this VisualTreeAsset vta, VisualTreeAsset other)
        {
            // It's important to keep the same physical inlineSheet
            // object in memory in the "other" asset and just overwrite
            // its contents. The default "FromJsonOverwrite" below will
            // actually replace the inlineSheet on other with vta's inlineSheet.
            // So, to fix this, we save the reference to the original
            // inlineSheet and restore it afterwards. case 1263454
            var originalInlineSheet = other.inlineSheet;

            var json = JsonUtility.ToJson(vta);
            JsonUtility.FromJsonOverwrite(json, other);

            other.inlineSheet = originalInlineSheet;
            if (vta.inlineSheet != null)
            {
                if (other.inlineSheet != null)
                    vta.inlineSheet.DeepOverwrite(other.inlineSheet);
                else
                    other.inlineSheet = vta.inlineSheet.DeepCopy();
            }

            other.name = vta.name;
        }

        internal static string GenerateUXML(this VisualTreeAsset vta, string vtaPath, bool writingToFile = false)
        {
            string result = null;
            try
            {
                result = VisualTreeAssetToUXML.GenerateUXML(vta, vtaPath, writingToFile);
            }
            catch (Exception ex)
            {
                if (!vta.name.Contains(BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix))
                {
                    var message = string.Format(BuilderConstants.InvalidUXMLDialogMessage, vta.name);
                    BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidUXMLDialogTitle, message);
                    vta.name = vta.name + BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix;
                }
                else
                {
                    var name = vta.name.Replace(BuilderConstants.InvalidUXMLOrUSSAssetNameSuffix, string.Empty);
                    var message = string.Format(BuilderConstants.InvalidUXMLDialogMessage, name);
                    Builder.ShowWarning(message);
                }
                Debug.LogError(ex.Message + "\n" + ex.StackTrace);
            }
            return result;
        }

        internal static void LinkedCloneTree(this VisualTreeAsset vta, VisualElement target)
        {
            VisualTreeAssetLinkedCloneTree.CloneTree(vta, target);
        }

        public static bool IsEmpty(this VisualTreeAsset vta)
        {
            return vta.visualElementAssets.Count <= 1 && vta.templateAssets.Count <= 0; // Because of the <UXML> tag, there's always one.
        }


        public static VisualElementAsset GetRootUXMLElement(this VisualTreeAsset vta)
        {
            return vta.visualElementAssets[0];
        }

        public static int GetRootUXMLElementId(this VisualTreeAsset vta)
        {
            return vta.GetRootUXMLElement().id;
        }

        public static bool IsRootUXMLElement(this VisualTreeAsset vta, VisualElementAsset vea)
        {
            return vea == vta.GetRootUXMLElement();
        }

        public static bool IsRootElement(this VisualTreeAsset vta, VisualElementAsset vea)
        {
            return vea.parentId == vta.GetRootUXMLElementId();
        }

        internal static VisualElementAsset FindElementByType(this VisualTreeAsset vta, string fullTypeName)
        {
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    return vea;
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    return vea;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByType(this VisualTreeAsset vta, string fullTypeName)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(vea);
            }
            return foundList;
        }

        internal static VisualElementAsset FindElementByName(this VisualTreeAsset vta, string name)
        {
            foreach (var vea in vta.visualElementAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    return vea;
            }
            foreach (var vea in vta.templateAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    return vea;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByName(this VisualTreeAsset vta, string name)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                string currentName;
                vea.TryGetAttributeValue("name", out currentName);
                if (currentName == name)
                    foundList.Add(vea);
            }
            return foundList;
        }

        internal static List<VisualElementAsset> FindElementsByClass(this VisualTreeAsset vta, string className)
        {
            var foundList = new List<VisualElementAsset>();
            foreach (var vea in vta.visualElementAssets)
            {
                if (vea.classes.Contains(className))
                    foundList.Add(vea);
            }
            foreach (var vea in vta.templateAssets)
            {
                if (vea.classes.Contains(className))
                    foundList.Add(vea);
            }
            return foundList;
        }

        public static void ConvertAllAssetReferencesToPaths(this VisualTreeAsset vta)
        {
            var sheets = new HashSet<StyleSheet>();
            foreach (var asset in vta.visualElementAssets)
            {
                sheets.Clear();

                foreach (var styleSheet in asset.stylesheets)
                    sheets.Add(styleSheet);

                foreach (var sheetPath in asset.stylesheetPaths)
                {
                    var sheetAsset = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(sheetPath);
                    if (sheetAsset == null)
                    {
                        sheetAsset = Resources.Load<StyleSheet>(sheetPath);
                        if (sheetAsset == null)
                            continue;
                    }

                    sheets.Add(sheetAsset);
                }

                asset.stylesheetPaths.Clear();
                foreach (var sheet in sheets)
                {
                    var path = AssetDatabase.GetAssetPath(sheet);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    asset.stylesheetPaths.Add(path);
                }
            }

            var fieldInfo = UsingsListFieldInfo;
            if (fieldInfo != null)
            {
                var usings = fieldInfo.GetValue(vta) as List<VisualTreeAsset.UsingEntry>;
                if (usings != null && usings.Count > 0)
                {
                    for (int i = 0; i < usings.Count; ++i)
                    {
                        if (usings[i].asset == null)
                            continue;

                        var u = usings[i];
                        u.path = AssetDatabase.GetAssetPath(u.asset);
                        usings[i] = u;
                    }
                }
            }
            else
            {
                Debug.LogError("UI Builder: VisualTreeAsset.m_Usings field has not been found! Update the reflection code!");
            }
        }

        static void GetAllReferencedStyleSheets(VisualElementAsset vea, HashSet<StyleSheet> sheets)
        {
            var styleSheets = vea.stylesheets;
            if (styleSheets != null)
            {
                foreach (var styleSheet in styleSheets)
                    if (styleSheet != null) // Possible if the path is not valid.
                        sheets.Add(styleSheet);
            }

            var styleSheetPaths = vea.GetStyleSheetPaths();
            if (styleSheetPaths != null)
            {
                foreach (var sheetPath in styleSheetPaths)
                {
                    var sheetAsset = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(sheetPath);
                    if (sheetAsset == null)
                    {
                        sheetAsset = Resources.Load<StyleSheet>(sheetPath);
                        if (sheetAsset == null)
                            continue;
                    }

                    sheets.Add(sheetAsset);
                }
            }
        }

        internal static List<StyleSheet> GetAllReferencedStyleSheets(this VisualTreeAsset vta)
        {
            var sheets = new HashSet<StyleSheet>();

            foreach (var vea in vta.visualElementAssets)
                if (vta.IsRootElement(vea) || vta.IsRootUXMLElement(vea))
                    GetAllReferencedStyleSheets(vea, sheets);

            foreach (var vea in vta.templateAssets)
                if (vta.IsRootElement(vea))
                    GetAllReferencedStyleSheets(vea, sheets);

            return sheets.ToList();
        }

        public static string GetPathFromTemplateName(this VisualTreeAsset vta, string templateName)
        {
            var templateAsset = vta.ResolveTemplate(templateName);
            if (templateAsset == null)
                return null;

            return AssetDatabase.GetAssetPath(templateAsset);
        }

        public static string GetTemplateNameFromPath(this VisualTreeAsset vta, string path)
        {
            var fieldInfo = UsingsListFieldInfo;
            if (fieldInfo != null)
            {
                var usings = fieldInfo.GetValue(vta) as List<VisualTreeAsset.UsingEntry>;
                if (usings != null && usings.Count > 0)
                {
                    var lookingFor = new VisualTreeAsset.UsingEntry(null, path);
                    int index = usings.BinarySearch(lookingFor, s_UsingEntryPathComparer);
                    if (index >= 0 && usings[index].path == path)
                    {
                        return usings[index].alias;
                    }
                }
            }
            else
            {
                Debug.LogError("UI Builder: VisualTreeAsset.m_Usings field has not been found! Update the reflection code!");
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        public static bool TemplateExists(this VisualTreeAsset windowVTA, VisualTreeAsset draggingInVTA)
        {
            var checkedTemplates = new HashSet<VisualTreeAsset>();
            return TemplateExists(windowVTA, draggingInVTA, checkedTemplates);
        }

        internal static bool IsUsingTemplate(List<VisualTreeAsset.UsingEntry> usings, string path, VisualTreeAsset template)
        {
            foreach (var usingEntry in usings)
            {
                if ((!string.IsNullOrEmpty(path) && usingEntry.path == path) || usingEntry.asset == template)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TemplateExists(this VisualTreeAsset parentTemplate, VisualTreeAsset templateToCheck, HashSet<VisualTreeAsset> checkedTemplates)
        {
            var fieldInfo = UsingsListFieldInfo;
            if (fieldInfo != null && templateToCheck != null)
            {
                var usings = fieldInfo.GetValue(templateToCheck) as List<VisualTreeAsset.UsingEntry>;
                if (usings != null && usings.Count > 0)
                {
                    checkedTemplates.Add(templateToCheck);

                    var assetPath = AssetDatabase.GetAssetPath(parentTemplate);
                    var isUsingTemplate = IsUsingTemplate(usings, assetPath, parentTemplate);

                    if (isUsingTemplate)
                    {
                        return true;
                    }

                    var templates = templateToCheck.templateDependencies;
                    foreach (var template in templates)
                    {
                        if (checkedTemplates.Contains(template))
                        {
                            continue;
                        }

                        if (TemplateExists(parentTemplate, template, checkedTemplates))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static TemplateAsset AddTemplateInstance(
            this VisualTreeAsset vta, VisualElementAsset parent, string path)
        {
            var templateName = vta.GetTemplateNameFromPath(path);
            if (!vta.TemplateExists(templateName))
            {
                var resolvedAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (resolvedAsset)
                {
                    vta.RegisterTemplate(templateName, resolvedAsset);
                }
                else
                {
                    vta.RegisterTemplate(templateName, path);
                }
            }

            var templateAsset = new TemplateAsset(templateName, BuilderConstants.UxmlInstanceTypeName);
            VisualTreeAssetUtilities.InitializeElement(templateAsset);

            templateAsset.AddProperty("template", templateName);

            return VisualTreeAssetUtilities.AddElementToDocument(vta, templateAsset, parent) as TemplateAsset;
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, string fullTypeName, int index = -1)
        {
            var vea = new VisualElementAsset(fullTypeName);
            VisualTreeAssetUtilities.InitializeElement(vea);
            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, VisualElement visualElement, int index = -1)
        {
            var fullTypeName = visualElement.GetType().ToString();
            var vea = new VisualElementAsset(fullTypeName);
            VisualTreeAssetUtilities.InitializeElement(vea);

            visualElement.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);
            visualElement.SetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName, vta);

            var overriddenAttributes = visualElement.GetOverriddenAttributes();
            foreach (var attribute in overriddenAttributes)
                vea.AddProperty(attribute.Key, attribute.Value);

            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        internal static VisualElementAsset AddElement(
            this VisualTreeAsset vta, VisualElementAsset parent, VisualElementAsset vea)
        {
            return VisualTreeAssetUtilities.AddElementToDocument(vta, vea, parent);
        }

        public static void RemoveElement(
            this VisualTreeAsset vta, VisualElementAsset element)
        {
            if (element is TemplateAsset)
                vta.templateAssets.Remove(element as TemplateAsset);
            else
                vta.visualElementAssets.Remove(element);
        }

        public static void ReparentElement(
            this VisualTreeAsset vta,
            VisualElementAsset elementToReparent,
            VisualElementAsset newParent,
            int index = -1)
        {
            VisualTreeAssetUtilities.ReparentElementInDocument(vta, elementToReparent, newParent, index);
        }

        public static StyleSheet GetOrCreateInlineStyleSheet(this VisualTreeAsset vta)
        {
            if (vta.inlineSheet == null)
                vta.inlineSheet = StyleSheetUtilities.CreateInstance();
            return vta.inlineSheet;
        }

        public static StyleRule GetOrCreateInlineStyleRule(this VisualTreeAsset vta, VisualElementAsset vea)
        {
            bool wasCreated;
            return vta.GetOrCreateInlineStyleRule(vea, out wasCreated);
        }

        public static StyleRule GetOrCreateInlineStyleRule(this VisualTreeAsset vta, VisualElementAsset vea, out bool wasCreated)
        {
            wasCreated = vea.ruleIndex < 0;
            if (wasCreated)
            {
                var inlineSheet = vta.GetOrCreateInlineStyleSheet();
                vea.ruleIndex = inlineSheet.AddRule();
            }

            return vta.inlineSheet.GetRule(vea.ruleIndex);
        }

        public static void ReplaceStyleSheetPaths(this VisualTreeAsset vta, string oldUssPath, string newUssPath)
        {
            if (oldUssPath == newUssPath)
                return;

            foreach (var element in vta.visualElementAssets)
            {
                var styleSheetPaths = element.GetStyleSheetPaths();
                if (styleSheetPaths != null)
                {
                    for (int i = 0; i < styleSheetPaths.Count(); ++i)
                    {
                        var styleSheetPath = styleSheetPaths[i];
                        if (styleSheetPath != oldUssPath && oldUssPath != String.Empty)
                            continue;

                        styleSheetPaths[i] = newUssPath;
                    }
                }

                // If we change the paths above, they are clearly not going to match
                // the styleSheets (assets) anymore. We can end up with the assets
                // added back later in the Save process.
                element.stylesheets.Clear();
            }
        }

        public static bool IsSelected(this VisualTreeAsset vta)
        {
            var foundElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            return foundElement != null;
        }

        public static void Swallow(this VisualTreeAsset vta, VisualElementAsset parent, VisualTreeAsset other)
        {
            var otherIdToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(other);

            if (parent == null)
                parent = vta.GetRootUXMLElement();

            var nextOrderInDocument = (vta.visualElementAssets.Count + vta.templateAssets.Count) * BuilderConstants.VisualTreeAssetOrderIncrement;

            foreach (var vea in other.visualElementAssets)
            {
                if (other.IsRootUXMLElement(vea))
                    continue;

                ReinitElementWithNewParentAsset(
                    vta, parent, other, otherIdToChildren, vea, ref nextOrderInDocument);

                vta.visualElementAssets.Add(vea);
            }

            foreach (var vea in other.templateAssets)
            {
                ReinitElementWithNewParentAsset(
                    vta, parent, other, otherIdToChildren, vea, ref nextOrderInDocument);

                if (!vta.TemplateExists(vea.templateAlias))
                {
                    vta.RegisterTemplate(vea.templateAlias, other.ResolveTemplate(vea.templateAlias));
                }

                vta.templateAssets.Add(vea);
            }

            VisualTreeAssetUtilities.ReOrderDocument(vta);
        }

        static void ReinitElementWithNewParentAsset(
            VisualTreeAsset vta, VisualElementAsset parent, VisualTreeAsset other,
            Dictionary<int, List<VisualElementAsset>> otherIdToChildren,
            VisualElementAsset vea, ref int nextOrderInDocument)
        {
            SwallowStyleRule(vta, other, vea);

            // Set new parent id on root elements.
            if (other.IsRootElement(vea) && parent != null)
                vea.parentId = parent.id;

            // Set order in document.
            vea.orderInDocument = nextOrderInDocument;
            nextOrderInDocument += BuilderConstants.VisualTreeAssetOrderIncrement;

            // Create new id and update parentId in children.
            var oldId = vea.id;
            vea.id = VisualTreeAssetUtilities.GenerateNewId(vta, vea);
            List<VisualElementAsset> children;
            if (otherIdToChildren.TryGetValue(oldId, out children) && children != null)
                foreach (var child in children)
                    child.parentId = vea.id;
        }

        static void SwallowStyleRule(VisualTreeAsset vta, VisualTreeAsset other, VisualElementAsset vea)
        {
            if (vea.ruleIndex < 0)
                return;

            if (vta.inlineSheet == null)
                vta.inlineSheet = StyleSheetUtilities.CreateInstance();

            var toStyleSheet = vta.inlineSheet;
            var fromStyleSheet = other.inlineSheet;

            var rule = fromStyleSheet.rules[vea.ruleIndex];

            // Add rule to StyleSheet.
            var rulesList = toStyleSheet.rules.ToList();
            var index = rulesList.Count;
            rulesList.Add(rule);
            toStyleSheet.rules = rulesList.ToArray();

            // Add property values to sheet.
            foreach (var property in rule.properties)
            {
                for (int i = 0; i < property.values.Length; ++i)
                {
                    var valueHandle = property.values[i];
                    valueHandle.valueIndex =
                        toStyleSheet.SwallowStyleValue(fromStyleSheet, valueHandle);
                    property.values[i] = valueHandle;
                }
            }

            vea.ruleIndex = index;
        }

        public static void ClearUndo(this VisualTreeAsset vta)
        {
            if (vta == null)
                return;

            Undo.ClearUndo(vta);

            if (vta.inlineSheet == null)
                return;

            Undo.ClearUndo(vta.inlineSheet);
        }

        public static void Destroy(this VisualTreeAsset vta)
        {
            if (vta == null)
                return;

            if (vta.inlineSheet != null)
                ScriptableObject.DestroyImmediate(vta.inlineSheet);

            ScriptableObject.DestroyImmediate(vta);
        }

        public static void AssignClassListFromAssetToElement(this VisualTreeAsset vta, VisualElementAsset asset, VisualElement element)
        {
            if (asset.classes != null)
            {
                for (int i = 0; i < asset.classes.Length; i++)
                    element.AddToClassList(asset.classes[i]);
            }
        }

        public static void AssignStyleSheetFromAssetToElement(this VisualTreeAsset vta, VisualElementAsset asset, VisualElement element)
        {
            if (asset.hasStylesheetPaths)
                for (int i = 0; i < asset.stylesheetPaths.Count; i++)
                    element.AddStyleSheetPath(asset.stylesheetPaths[i]);

            if (asset.hasStylesheets)
                for (int i = 0; i < asset.stylesheets.Count; ++i)
                    if (asset.stylesheets[i] != null)
                        element.styleSheets.Add(asset.stylesheets[i]);
        }

    }
}
