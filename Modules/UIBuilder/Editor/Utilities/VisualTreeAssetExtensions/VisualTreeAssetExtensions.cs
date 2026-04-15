// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetExtensions
    {
        public static VisualTreeAsset DeepCopy(this VisualTreeAsset vta, bool syncSerializedData = true)
        {
            var newTreeAsset = VisualTreeAssetUtilities.CreateInstanceWithHideFlags();

            if (syncSerializedData)
                UxmlSerializer.CreateSerializedDataOverrides(vta);
            vta.DeepOverwrite(newTreeAsset);

            // The DeepOverwrite will mark the vta dirty, so we need to
            // restore the dirty flag since we are just making a fresh copy.
            EditorUtility.ClearDirty(newTreeAsset);

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
            other.SetupReferences();

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

        internal static string GenerateUXML(this VisualTreeAsset vta)
        {
            string result = null;
            try
            {
                result = VisualTreeAssetExporter.Default.ToUxmlString(vta);
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
            VisualTreeAssetLinkedCloneTree.CloneTree(vta, target, null, null);
        }

        public static VisualElementAsset GetRootUXMLElement(this VisualTreeAsset vta)
        {
            return vta.visualTreeNoAlloc;
        }

        public static bool IsRootElement(this VisualTreeAsset vta, VisualElementAsset vea)
        {
            return vea.parentAsset?.isRoot ?? false;
        }

        internal static VisualElementAsset FindElementByType(this VisualTreeAsset vta, string fullTypeName)
        {
            using var _ = ListPool<UxmlAsset>.Get(out var list);

            list.AddRange(vta.DepthFirstTraversal());

            foreach (var vea in list)
            {
                if (vea is TemplateAsset)
                    continue;
                if (vea is not VisualElementAsset visualElementAsset)
                    continue;
                if (vea.fullTypeName == fullTypeName)
                    return visualElementAsset;
            }
            foreach (var vea in list)
            {
                if (vea is not TemplateAsset templateAsset)
                    continue;

                if (vea.fullTypeName  == fullTypeName)
                    return templateAsset;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByType(this VisualTreeAsset vta, string fullTypeName)
        {
            var foundList = new List<VisualElementAsset>();

            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(vta.DepthFirstTraversal());

            foreach (var vea in list)
            {
                if (vea is TemplateAsset)
                    continue;
                if (vea is not VisualElementAsset visualElementAsset)
                    continue;
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(visualElementAsset);
            }
            foreach (var vea in list)
            {
                if (vea is not TemplateAsset templateAsset)
                    continue;
                if (vea.fullTypeName == fullTypeName)
                    foundList.Add(templateAsset);
            }
            return foundList;
        }

        internal static VisualElementAsset FindElementByName(this VisualTreeAsset vta, string name)
        {
            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(vta.DepthFirstTraversal());

            foreach (var vea in list)
            {
                if (vea is TemplateAsset)
                    continue;
                if (vea is not VisualElementAsset visualElementAsset)
                    continue;
                vea.TryGetAttributeValue("name", out var currentName);
                if (currentName == name)
                    return visualElementAsset;
            }
            foreach (var vea in list)
            {
                if (vea is not TemplateAsset templateAsset)
                    continue;
                vea.TryGetAttributeValue("name", out var currentName);
                if (currentName == name)
                    return templateAsset;
            }
            return null;
        }

        internal static List<VisualElementAsset> FindElementsByName(this VisualTreeAsset vta, string name)
        {
            var foundList = new List<VisualElementAsset>();

            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(vta.DepthFirstTraversal());

            foreach (var vea in list)
            {
                if (vea is TemplateAsset)
                    continue;
                if (vea is not VisualElementAsset visualElementAsset)
                    continue;
                vea.TryGetAttributeValue("name", out var currentName);
                if (currentName == name)
                    foundList.Add(visualElementAsset);
            }
            foreach (var vea in list)
            {
                if (vea is not TemplateAsset templateAsset)
                    continue;
                vea.TryGetAttributeValue("name", out var currentName);
                if (currentName == name)
                    foundList.Add(templateAsset);
            }
            return foundList;
        }

        internal static List<VisualElementAsset> FindElementsByClass(this VisualTreeAsset vta, string className)
        {
            var foundList = new List<VisualElementAsset>();

            using var _ = ListPool<UxmlAsset>.Get(out var list);
            list.AddRange(vta.DepthFirstTraversal());

            foreach (var vea in list)
            {
                if (vea is TemplateAsset)
                    continue;
                if (vea is not VisualElementAsset visualElementAsset)
                    continue;
                if (visualElementAsset.classes.Contains(className))
                    foundList.Add(visualElementAsset);
            }
            foreach (var vea in list)
            {
                if (vea is not TemplateAsset templateAsset)
                    continue;
                if (templateAsset.classes.Contains(className))
                    foundList.Add(templateAsset);
            }
            return foundList;
        }

        public static void UpdateUsingEntries(this VisualTreeAsset vta)
        {
            var usings = vta.usings;
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

        public static string GetPathFromTemplateName(this VisualTreeAsset vta, string templateName)
        {
            var templateAsset = vta.ResolveTemplate(templateName);
            if (templateAsset == null)
                return null;

            return AssetDatabase.GetAssetPath(templateAsset);
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
            if (templateToCheck != null)
            {
                var usings = templateToCheck.usings;
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

        internal static void SetAssetAttributes(this VisualTreeAsset vta, VisualElementAsset vea, VisualElement visualElement)
        {
            visualElement.SetVisualElementAsset(vea);
            visualElement.SetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName, vta);
        }

        public static StyleSheet GetOrCreateInlineStyleSheet(this VisualTreeAsset vta)
        {
            if (vta.inlineSheet == null)
                vta.inlineSheet = UnityEngine.UIElements.StyleSheetUtility.CreateInstanceWithHideFlags();
            return vta.inlineSheet;
        }

        public static void ReplaceStyleSheetPaths(this VisualTreeAsset vta, string oldUssPath, string newUssPath)
        {
            if (oldUssPath == newUssPath)
                return;

            foreach (var ua in vta.DepthFirstTraversal())
            {
                if (ua is TemplateAsset)
                    continue;
                if (ua is not VisualElementAsset element)
                    continue;
                var styleSheetPaths = element.GetStyleSheetPaths();
                if (styleSheetPaths != null)
                {
                    for (int i = 0; i < styleSheetPaths.Count; ++i)
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
            if (asset.hasStylesheets)
                for (int i = 0; i < asset.stylesheets.Count; ++i)
                    if (asset.stylesheets[i] != null)
                        element.styleSheets.Add(asset.stylesheets[i]);
        }

        public static void RemoveBinding(this VisualTreeAsset vta, VisualElementAsset element, string property)
        {
            var uxmlBinding = element.FindUxmlBinding(property);
            uxmlBinding?.RemoveAssetAndFieldParentIfEmpty();
        }

        public static string GetSerializedPath(this UxmlAsset asset)
        {
            using var _builder = StringBuilderPool.Get(out var sb);
            using var _parents = ListPool<(UxmlAsset a, int i)>.Get(out var parents);
            var previous = asset;
            var current = asset;

            while (null != current)
            {
                if (current == previous)
                {
                    parents.Add((current, -1));
                }
                else
                {
                    var childIndex = -1;
                    for (var i = 0; i < current.childCount; ++i)
                    {
                        if (current[i] == previous)
                        {
                            childIndex = i;
                            break;
                        }
                    }
                    parents.Add((current, childIndex));
                }

                previous = current;
                current = current.parentAsset;
            }

            if (parents.Count == 0 || !parents[^1].a.isRoot)
                throw new InvalidOperationException("The asset is not part of a UXML document.");

            sb.Append("m_VisualTree");
            for (var i = parents.Count - 1; i >= 0; --i)
            {
                if (parents[i].i < 0)
                    break;
                sb.Append($".m_Children.Array.data[{parents[i].i}]");
            }

            sb.Append(".");
            sb.Append(BuilderConstants.UxmlSerializedDataFieldName);

            return sb.ToString();
        }
    }
}
