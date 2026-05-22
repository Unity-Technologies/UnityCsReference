// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    struct SynchronizePathResult
    {
        public bool success { get; set; }
        public UxmlAsset uxmlAsset { get; set; }
        public object serializedData { get; set; }
        public UxmlSerializedDataDescription dataDescription { get; set; }
        public UxmlSerializedAttributeDescription attributeDescription { get; set; }
        public object attributeOwner { get; set; }
        public string propertyPath { get; set; }
    }

    internal static class BuilderAssetUtilities
    {
        public static string assetsPath { get; } = Application.dataPath;
        public static string projectPath { get; } = assetsPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
        public static string packagesPath { get; } = projectPath + "/Packages";

        static readonly Dictionary<string, string[]> s_PathPartsCache = new();
        static bool s_DocumentUndoRecorded;

        internal static readonly PropertyName UndoGroupPropertyKey = "__UnityUndoGroup";

        const string k_ArraySizePart = "size";

        // UxmlTraits

        // Sync path
        static readonly List<UxmlObjectAsset> s_TempUxmlAssets = new();
        static readonly object[] s_SingleUxmlSerializedData = new object[1];

        static string GetFullPath(string path)
        {
            return Path.GetFullPath(path).Replace("\\", "/");
        }

        public static bool IsPathInProject(string path)
        {
            var fullPath = GetFullPath(path);

            return fullPath.StartsWith(assetsPath, StringComparison.InvariantCultureIgnoreCase) ||
                   fullPath.StartsWith(packagesPath, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string GetPathRelativeToProject(string path)
        {
            if (!IsPathInProject(path))
                return null;
            var fullPath = GetFullPath(path);
            return fullPath.Substring(projectPath.Length + 1); // "/"
        }

        public static bool TryGetResourcesPathForAsset(Object asset, out ResolvedResourcePath resolvedResourcePath)
        {
            resolvedResourcePath = default;
            var assetPath = AssetDatabase.GetAssetPath(asset);

            if (TryGetResourcesPathForAsset(assetPath, out assetPath))
            {
                if (AssetDatabase.IsSubAsset(asset))
                    resolvedResourcePath = new ResolvedResourcePath(assetPath, asset.name);
                else
                    resolvedResourcePath = new ResolvedResourcePath(assetPath, null);
                return true;
            }

            return false;
        }

        public static bool TryGetResourcesPathForAsset(string path, out string assetPath)
        {
            assetPath = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Start by trying to find a "Resources" folder in the middle of the path.
            var resourcesFolder = "/Resources/";
            var lastResourcesFolderIndex = path.LastIndexOf(resourcesFolder, StringComparison.Ordinal);
            // Otherwise check if the "Resources" path is at the start.
            if (lastResourcesFolderIndex < 0)
            {
                if (path.StartsWith("Resources/"))
                {
                    lastResourcesFolderIndex = 0;
                    resourcesFolder = "Resources/";
                }
                else return false;
            }

            var lastResourcesSubstring = lastResourcesFolderIndex + resourcesFolder.Length;
            path = path.Substring(lastResourcesSubstring);
            var lastExtDot = path.LastIndexOf(".", StringComparison.Ordinal);

            if (lastExtDot == -1)
                return false;

            path = path.Substring(0, lastExtDot);
            assetPath = path;
            return true;
        }

        public static bool IsBuiltinPath(string assetPath)
        {
            return assetPath == "Resources/unity_builtin_extra";
        }

        public static bool ValidateAsset(VisualTreeAsset asset, string path)
        {
            string errorMessage = null;

            string errorTitle = null;

            if (asset == null)
            {
                if (string.IsNullOrEmpty(path))
                    path = "<unspecified>";

                if (path.StartsWith("Packages/"))
                    errorMessage = $"The asset at path {path} is not a UXML Document.\nNote, for assets inside Packages folder, the folder name for the package needs to match the actual official package name (ie. com.example instead of Example).";
                else
                    errorMessage = $"The asset at path {path} is not a UXML Document.";
                errorTitle = "Invalid Asset Type";
            }
            else if (asset.importedWithErrors)
            {
                if (string.IsNullOrEmpty(path))
                    path = AssetDatabase.GetAssetPath(asset);

                if (string.IsNullOrEmpty(path))
                    path = "<unspecified>";

                errorMessage = string.Format(BuilderConstants.InvalidUXMLDialogMessage, path);
                errorTitle = BuilderConstants.InvalidUXMLDialogTitle;
            }

            if (errorMessage != null)
            {
                BuilderDialogsUtility.DisplayDialog(errorTitle, errorMessage, "OK");
                Debug.LogError(errorMessage);
                return false;
            }

            return true;
        }

        public static bool AddStyleSheetToAsset(
            BuilderDocument document, string ussPath, int index = -1)
        {
            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(ussPath);

            string errorMessage = null;
            string errorTitle = null;

            if (styleSheet == null)
            {
                if (ussPath.StartsWith("Packages/"))
                    errorMessage = $"Asset at path {ussPath} is not a StyleSheet.\nNote, for assets inside Packages folder, the folder name for the package needs to match the actual official package name (ie. com.example instead of Example).";
                else
                    errorMessage = $"Asset at path {ussPath} is not a StyleSheet.";
                errorTitle = "Invalid Asset Type";
            }
            else if (styleSheet.importedWithErrors)
            {
                errorMessage = string.Format(BuilderConstants.InvalidUSSDialogMessage, ussPath);
                errorTitle = BuilderConstants.InvalidUSSDialogTitle;
            }

            if (errorMessage != null)
            {
                BuilderDialogsUtility.DisplayDialog(errorTitle, errorMessage, "OK");
                Debug.LogError(errorMessage);
                return false;
            }

            // Check if the stylesheet is already in the document
            if (document.IsStyleSheetInDocument(styleSheet))
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Add StyleSheet to UXML");

            document.AddStyleSheetToDocument(styleSheet, ussPath, index);
            return true;
        }

        public static void RemoveStyleSheetFromAsset(
            BuilderDocument document, int ussIndex)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Remove StyleSheet from UXML");

            document.RemoveStyleSheetFromDocument(ussIndex);
        }

        public static void RemoveStyleSheetsFromAsset(
            BuilderDocument document, int[] indexes)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Remove StyleSheets from UXML");

            foreach (var index in indexes)
            {
                document.RemoveStyleSheetFromDocument(index);
            }
        }

        public static bool ReorderStyleSheetsInAsset(
            BuilderDocument document, VisualElement styleSheetsContainerElement)
        {
            var reorderedUSSList = new List<StyleSheet>();
            foreach (var ussElement in styleSheetsContainerElement.Children())
                reorderedUSSList.Add(ussElement.GetStyleSheet());

            var openUXMLFile = document.activeOpenUXMLFile;

            // Check if the order would actually change
            bool orderChanged = false;
            for (int i = 0; i < openUXMLFile.openUSSFiles.Count && i < reorderedUSSList.Count; i++)
            {
                if (openUXMLFile.openUSSFiles[i].styleSheet != reorderedUSSList[i])
                {
                    orderChanged = true;
                    break;
                }
            }

            if (!orderChanged)
                return false;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Reorder StyleSheets in UXML");

            openUXMLFile.openUSSFiles.Sort((left, right) =>
            {
                var leftOrder = reorderedUSSList.IndexOf(left.styleSheet);
                var rightOrder = reorderedUSSList.IndexOf(right.styleSheet);
                return leftOrder.CompareTo(rightOrder);
            });

            var rootElement = openUXMLFile.visualTreeAsset.visualTreeNoAlloc;
            if (rootElement != null && rootElement.stylesheets != null)
            {
                rootElement.stylesheets.Sort((left, right) =>
                {
                    var leftOrder = reorderedUSSList.IndexOf(left);
                    var rightOrder = reorderedUSSList.IndexOf(right);
                    return leftOrder.CompareTo(rightOrder);
                });
            }

            return true;
        }

        public static VisualElementAsset AddElementToAsset(
            VisualTreeAsset visualTreeAsset, VisualElement ve, int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;

            /* If the current parent element is linked to a VisualTreeAsset, it could mean
             that our parent is the TemplateContainer belonging to our parent document and the
             current open document is a sub-document opened in-place. In such a case, we don't
             want to use our parent's VisualElementAsset, as that belongs to our parent document.
             So instead, we just use no parent, indicating that we are adding this new element
             to the root of our document.*/
            if (veParent != null && veParent.GetVisualTreeAsset() != visualTreeAsset)
            {
                // We must revisit this once we finalize how we want our container controls to work with accepting
                // specific types of controls. For now we're only applying this for ToggleButtonGroup but other controls
                // like RadioButtonGroup would also be applicable.
                if (veParent.parent is ToggleButtonGroup control)
                    veParent = control;

                veaParent = veParent.GetVisualElementAsset();
            }

            if (veaParent == null)
                veaParent = visualTreeAsset.visualTree; // UXML Root Element

            var vea = visualTreeAsset.AddVisualElementAssetFromVisualElement(veaParent, ve);
            visualTreeAsset.SetAssetAttributes(vea, ve);

            if (index >= 0)
                visualTreeAsset.ReparentElementInDocument(vea, veaParent, index);

            return vea;
        }

        public static VisualElementAsset AddElementToAsset(
            VisualTreeAsset visualTreeAsset, VisualElement ve,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeVisualElementAsset,
            int index = -1, bool registerUndo = true)
        {
            if (registerUndo)
            {
                Undo.RegisterCompleteObjectUndo(
                    visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);
            }

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;

            /* If the current parent element is linked to a VisualTreeAsset, it could mean
             that our parent is the TemplateContainer belonging to our parent document and the
             current open document is a sub-document opened in-place. In such a case, we don't
             want to use our parent's VisualElementAsset, as that belongs to our parent document.
             So instead, we just use no parent, indicating that we are adding this new element
             to the root of our document.*/
            if (veParent != null && veParent.GetVisualTreeAsset() != visualTreeAsset)
                veaParent = veParent.GetVisualElementAsset();

            if (veaParent == null)
                veaParent = visualTreeAsset.visualTree; // UXML Root Element

            var vea = makeVisualElementAsset(visualTreeAsset, veaParent, ve);
            ve.SetVisualElementAsset(vea);
            ve.SetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName, visualTreeAsset);

            if (index >= 0)
                visualTreeAsset.ReparentElementInDocument(vea, veaParent, index);

            return vea;
        }

        public static void ReparentElementInAsset(
            BuilderDocument document, VisualElement veToReparent, VisualElement newParent, int index = -1, bool undo = true)
        {
            var veaToReparent = veToReparent.GetVisualElementAsset();
            if (veaToReparent == null)
                return;

            if (undo)
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ReparentUIElementUndoMessage);

            VisualElementAsset veaNewParent = null;
            /* If the current parent element is linked to a VisualTreeAsset, it could mean
             that our parent is the TemplateContainer belonging to our parent document and the
             current open document is a sub-document opened in-place. In such a case, we don't
             want to use our parent's VisualElementAsset, as that belongs to our parent document.
             So instead, we just use no parent, indicating that we are adding this new element
             to the root of our document.*/
            if (newParent != null && newParent.GetVisualTreeAsset() != document.visualTreeAsset)
                veaNewParent = newParent.GetVisualElementAsset();

            if (veaNewParent == null)
                veaNewParent = document.visualTreeAsset.visualTree; // UXML Root Element

            document.visualTreeAsset.ReparentElementInDocument(veaToReparent, veaNewParent, index);
        }

        public static void ApplyAttributeOverridesToTreeAsset(List<TemplateAsset.AttributeOverride> attributeOverrides, VisualTreeAsset visualTreeAsset)
        {
            foreach (var attributeOverride in attributeOverrides)
            {
                var elementName = attributeOverride.m_NamesPath[^1];
                var overwrittenElements = visualTreeAsset.FindElementsByName(elementName);

                foreach (var overwrittenElement in overwrittenElements)
                {
                    if (IsElementInOverridePath(visualTreeAsset, overwrittenElement, attributeOverride))
                    {
                        overwrittenElement.SetAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value);
                    }
                }
            }
        }

        private static bool IsElementInOverridePath(VisualTreeAsset vta, UxmlAsset overwrittenElement, TemplateAsset.AttributeOverride attributeOverride)
        {
            var namesPath = new List<string>() { overwrittenElement.GetAttributeValue(nameof(VisualElement.name)) };

            while (overwrittenElement.HasParent())
            {
                var parent = overwrittenElement.parentAsset;
                parent.TryGetAttributeValue(nameof(VisualElement.name), out var parentName);

                if (!string.IsNullOrEmpty(parentName))
                {
                    namesPath.Insert(0, parentName);
                }

                overwrittenElement = parent;
            }

            return attributeOverride.NamesPathMatchesElementNamesPath(namesPath);
        }

        public static void CopyAttributeOverridesToChildTemplateAssets(TemplateContainer parentTemplateContainer, List<TemplateAsset.AttributeOverride> attributeOverrides, VisualTreeAsset visualTreeAsset)
        {
            foreach (var uxmlAsset in visualTreeAsset.DepthFirstTraversal())
            {
                if (uxmlAsset is not TemplateAsset templateAsset)
                    continue;
                var templateAssetVTA = visualTreeAsset.ResolveTemplate(templateAsset.templateAlias);

                foreach (var attributeOverride in attributeOverrides)
                {
                    // Find possible targeted elements
                    var targetElementName = attributeOverride.m_NamesPath[^1];
                    var targetedElements = parentTemplateContainer.Query<VisualElement>(targetElementName);

                    targetedElements.ForEach(element =>
                    {
                        var pathToParentTemplateContainer = targetElementName;
                        var currentParent = element.parent;
                        string pathToTemplateAsset = null;

                        while (currentParent != parentTemplateContainer)
                        {
                            if (currentParent is TemplateContainer tc)
                            {
                                if (!templateAsset.TryGetAttributeValue(nameof(VisualElement.name),
                                        out var templateAssetName))
                                {
                                    templateAssetName = "";
                                }

                                if (tc.templateSource == templateAssetVTA && tc.name == templateAssetName)
                                {
                                    pathToTemplateAsset = pathToParentTemplateContainer;
                                }
                            }

                            if (!string.IsNullOrEmpty(currentParent.name))
                            {
                                pathToParentTemplateContainer = currentParent.name + " " + pathToParentTemplateContainer;
                            }

                            currentParent = currentParent.parent;
                        }

                        if (pathToTemplateAsset != null && attributeOverride.NamesPathMatchesElementNamesPath(pathToParentTemplateContainer.Split()))
                        {
                            // Add attribute override to the template asset
                            templateAsset.SetAttributeOverride(attributeOverride.m_AttributeName, attributeOverride.m_Value, pathToTemplateAsset.Split());
                        }
                    });
                }
            }
        }

        public static void DeleteElementFromAsset(VisualTreeAsset visualTreeAsset, VisualElement ve, bool registerUndo = true)
        {
            var vea = ve.GetVisualElementAsset();
            if (vea == null)
                return;

            if (registerUndo)
            {
                Undo.RegisterCompleteObjectUndo(
                    visualTreeAsset, BuilderConstants.DeleteUIElementUndoMessage);
            }

            vea.RemoveFromHierarchy();
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, VisualElementAsset parent, VisualTreeAsset otherVta, bool registerUndo = true)
        {
            if (registerUndo)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);
            }

            document.visualTreeAsset.Swallow(parent, otherVta);
        }

        public static void TransferAssetToAsset(StyleSheet styleSheet, StyleSheet otherStyleSheet)
        {
            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            styleSheet.Swallow(otherStyleSheet);
        }

        public static void AddStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.AddStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.AddStyleClass(className);
        }

        public static void RemoveStyleClassFromElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.RemoveStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.RemoveStyleClass(className);
        }

        public static string GetVisualTreeAssetAssetName(VisualTreeAsset visualTreeAsset, bool hasUnsavedChanges) =>
            GetAssetName(visualTreeAsset, BuilderConstants.UxmlExtension, hasUnsavedChanges);

        public static string GetStyleSheetAssetName(StyleSheet styleSheet, bool hasUnsavedChanges) =>
            GetAssetName(styleSheet, BuilderConstants.UssExtension, hasUnsavedChanges);

        public static string GetAssetName(ScriptableObject asset, string extension, bool hasUnsavedChanges)
        {
            if (asset == null)
            {
                if (extension == BuilderConstants.UxmlExtension)
                    return BuilderConstants.ToolbarUnsavedFileDisplayText + extension;
                else
                    return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return BuilderConstants.ToolbarUnsavedFileDisplayText + extension;

            return Path.GetFileName(assetPath) + (hasUnsavedChanges ? BuilderConstants.ToolbarUnsavedFileSuffix : "");
        }

        public static TemplateContainer GetVisualElementRootTemplate(VisualElement visualElement)
        {
            TemplateContainer templateContainerParent = null;
            var parent = visualElement.parent;

            while (parent != null)
            {
                if (parent is TemplateContainer templateContainer && templateContainer.GetVisualElementAsset() != null)
                {
                    templateContainerParent = templateContainer;
                    break;
                }

                if (BuilderSharedStyles.IsDocumentElement(parent))
                {
                    break;
                }

                parent = parent.parent;
            }

            return templateContainerParent;
        }

        public static bool HasDynamicallyCreatedTemplateAncestor(VisualElement visualElement)
        {
            var parent = visualElement.parent;
            while (parent != null)
            {
                if (BuilderSharedStyles.IsDocumentElement(parent))
                {
                    return false;
                }

                if (parent is TemplateContainer
                    && parent.visualElementAsset == null
                    && parent.GetVisualElementAsset() == null)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        public static bool HasAttributeOverrideInRootTemplate(VisualElement visualElement, string attributeName)
        {
            var templateContainer = GetVisualElementRootTemplate(visualElement);
            var templateAsset = templateContainer?.GetVisualElementAsset() as TemplateAsset;
            var pathToTemplateAsset = templateAsset.GetPathToTemplateAsset(visualElement).ToList();

            return templateAsset?.attributeOverrides.Count(x => x.m_AttributeName == attributeName && x.NamesPathMatchesElementNamesPath(pathToTemplateAsset)) > 0;
        }

        public static List<CreationContext.AttributeOverrideRange> GetAccumulatedAttributeOverrides(VisualElement visualElement)
        {
            VisualElement parent = visualElement.parent;
            List<CreationContext.AttributeOverrideRange> attributeOverrideRanges = new();

            while (parent != null)
            {
                if (parent is TemplateContainer)
                {
                    TemplateAsset templateAsset;
                    if (parent.visualElementAsset != null)
                    {
                        templateAsset = parent.visualElementAsset as TemplateAsset;
                    }
                    else
                    {
                        templateAsset = parent.GetVisualElementAsset() as TemplateAsset;
                    }

                    if (templateAsset != null)
                    {
                        VisualTreeAsset visualTreeAsset = parent.GetVisualTreeAsset() ?? parent.GetInstancedVisualTreeAsset();
                        if (visualTreeAsset != null)
                        {
                            attributeOverrideRanges.Add(new CreationContext.AttributeOverrideRange(visualTreeAsset, templateAsset.attributeOverrides));
                        }
                    }

                    // We reached the root template
                    if (parent.GetVisualElementAsset() != null)
                    {
                        break;
                    }
                }

                parent = parent.parent;
            }

            // Parent attribute overrides have higher priority
            attributeOverrideRanges.Reverse();

            return attributeOverrideRanges;
        }

        public static bool WriteTextFileToDisk(string path, string content)
        {
            // Make sure the folders exist.
            var folder = Path.GetDirectoryName(path);
            if (folder != null && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var success = FileUtil.WriteTextFileToDisk(path, content, out string message);

            if (!success)
            {
                Debug.LogError(message);
                BuilderDialogsUtility.DisplayDialog("Save - " + path, message, "OK");
            }

            return success;
        }

        // Refresh GameView preview with the latest (unsaved to disk) changes.
        [Flags]
        public enum LiveReloadChanges
        {
            Hierarchy = 1,
            Styles = 2
        }
        public static void LiveReload(LiveReloadChanges changes)
        {
            if ((changes & LiveReloadChanges.Hierarchy) != 0)
                UIElementsUtility.InMemoryAssetsHierarchyHaveBeenChanged();
        }

        // Check if VisualElement will support type as a child. Assume true by default, unless explicitly false.
        public static bool IsSupportedChildType(VisualElement visualElement, Type type)
        {
            var fullName = visualElement?.GetType().FullName;
            UxmlSerializedDataDescription desc = null;
            if (!string.IsNullOrEmpty(fullName))
                desc = UxmlSerializedDataRegistry.GetDescription(fullName);

            if (desc != null && !desc.IsSupportedChild(type))
            {
                return false;
            }

            return true;
        }

        public static void UndoRecordDocument(BuilderUxmlAttributesEditingContext context, string reason)
        {
            if (context.undoEnabled)
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(context.visualTree, reason);
            }
        }

        public static SynchronizePathResult AddUxmlObjectToSerializedData(BuilderUxmlAttributesEditingContext context, SerializedProperty property, Type type, Dictionary<string, object> values = null)
        {
            Undo.RegisterCompleteObjectUndo(property.m_SerializedObject.targetObject, GetUndoMessage(property));

            if (property.isArray)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                property = property.GetArrayElementAtIndex(property.arraySize - 1);
            }

            var serializedObj = type != null ? UxmlSerializedDataCreator.CreateUxmlSerializedData(type.DeclaringType) : null;

            // sets the attributes of serializedObj from values
            if (values != null)
            {
                var description = UxmlSerializedDataRegistry.GetDescription(type.DeclaringType.FullName);
                foreach (var kvp in values)
                {
                    var propertyAttribute = description.FindAttributeWithPropertyName(kvp.Key);

                    if (propertyAttribute == null)
                    {
                        Debug.LogError($"Could not find attribute with name {kvp.Key} in {type.DeclaringType.FullName}");
                        continue;
                    }
                    propertyAttribute.SetSerializedValue(serializedObj, kvp.Value, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                }
            }
            property.managedReferenceValue = serializedObj;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return SyncUxmlObjectChanges(context, property.propertyPath);
        }

        public static SynchronizePathResult SyncUxmlObjectChanges(BuilderUxmlAttributesEditingContext context, string propertyPath)
        {
            if (context.batchedChangesController.isInsideUndoRedoUpdate)
                return default;

            var undoGroup = context.batchedChangesController.GetCurrentUndoGroup();
            Undo.IncrementCurrentGroup();
            var ret = SynchronizePath(context, propertyPath, true);
            CallDeserializeOnElement(context);
            context.NotifyAttributesChanged(null);
            Undo.CollapseUndoOperations(undoGroup);
            return ret;
        }

        /// <summary>
        /// Synchronizes the UXML serialized data to the current UXML asset and sub-UXML objects that are part of the path.
        /// __Note__: To synchronize the attribute owner when extracting, call <see cref="CallDeserializeOnElement"/>.
        /// </summary>
        /// <param name="propertyPath">The full serialized property path.</param>
        /// <param name="changeUxmlAssets">Whether to add missing UXML assets in the path.</param>
        /// <returns></returns>
        public static SynchronizePathResult SynchronizePath(BuilderUxmlAttributesEditingContext context, string propertyPath, bool changeUxmlAssets)
        {
            SynchronizePathResult result = default;

            if (string.IsNullOrEmpty(propertyPath))
                return result;

            // Cache the split so we don't have to do it every time.
            if (!s_PathPartsCache.TryGetValue(propertyPath, out var pathParts))
            {
                if (context.serializedBasePath == propertyPath)
                {
                    pathParts = Array.Empty<string>();
                }
                else
                {
                    pathParts = propertyPath[(context.serializedBasePath.Length + 1)..].Split('.');
                }

                s_PathPartsCache[propertyPath] = pathParts;
            }

            s_DocumentUndoRecorded = false;
            object currentUxmlSerializedData = context.uxmlSerializedData;
            UxmlAsset currentAttributesUxmlOwner = context.elementAsset;
            result.attributeOwner = context.element;
            result.propertyPath = propertyPath;

            for (int i = 0; i < pathParts.Length; ++i)
            {
                if (currentUxmlSerializedData == null)
                {
                    continue;
                }

                // Is the current value a list?
                if (currentUxmlSerializedData is IList serializedDataList)
                {
                    // Find the item index from the path and extract it.
                    var dataPath = pathParts[i + 1];

                    // Targeting the Array.size, nothing more to sync. The array size was synchronized in the previous
                    // loop and we don't want to return anything here because the result at this path is not part of UxmlSerializedData.
                    if (dataPath == k_ArraySizePart)
                    {
                        result.success = false;
                        return result;
                    }

                    var arrayItemIndexStart = dataPath.IndexOf('[') + 1;
                    var arrayItemIndexEnd = dataPath.IndexOf(']');
                    var indexString = dataPath.Substring(arrayItemIndexStart, arrayItemIndexEnd - arrayItemIndexStart);
                    var listIndex = int.Parse(indexString);

                    currentAttributesUxmlOwner = s_TempUxmlAssets[listIndex];
                    currentUxmlSerializedData = serializedDataList[listIndex];

                    if (result.attributeOwner is IList attributeOwnerList && listIndex < attributeOwnerList.Count)
                    {
                        result.attributeOwner = attributeOwnerList[listIndex];
                    }
                    else
                    {
                        result.attributeOwner = null; // We could not extract the value
                    }

                    i += 1;
                    continue;
                }

                result.dataDescription = UxmlSerializedDataRegistry.GetDescription(currentUxmlSerializedData.GetType().DeclaringType.FullName);

                var name = pathParts[i];
                result.attributeDescription = result.dataDescription.FindAttributeWithPropertyName(name);
                var attributeObjectDescription = result.attributeDescription as UxmlSerializedUxmlObjectAttributeDescription;
                if (attributeObjectDescription == null)
                    break;

                result.attributeDescription.TryGetValueFromObject(result.attributeOwner, out var updatedAttributeOwner);
                result.attributeOwner = updatedAttributeOwner;

                var parentUxmlSerializedData = currentUxmlSerializedData as UxmlSerializedData;
                currentUxmlSerializedData = result.attributeDescription.GetSerializedValue(parentUxmlSerializedData);
                var uxmlSerializedDataList = currentUxmlSerializedData as IList;

                // If we are not syncing a list then its a single field but we still treat it as a list.
                if (uxmlSerializedDataList == null)
                {
                    s_SingleUxmlSerializedData[0] = currentUxmlSerializedData;
                    uxmlSerializedDataList = s_SingleUxmlSerializedData;
                }

                if (!SyncUxmlAssetsFromSerializedData(context, uxmlSerializedDataList, parentUxmlSerializedData, currentAttributesUxmlOwner, attributeObjectDescription, changeUxmlAssets))
                {
                    if (!changeUxmlAssets)
                    {
                        result.uxmlAsset = currentAttributesUxmlOwner;
                        result.serializedData = currentUxmlSerializedData;
                        result.success = false;
                        return result;
                    }
                }

                if (!attributeObjectDescription.isList)
                    currentAttributesUxmlOwner = currentUxmlSerializedData == null ? null : s_TempUxmlAssets[0];
            }

            // We need to update the serialized object if we made changes.
            if (changeUxmlAssets)
                context.rootSerializedObject.UpdateIfRequiredOrScript();

            result.uxmlAsset = currentAttributesUxmlOwner;
            result.serializedData = currentUxmlSerializedData;
            result.success = true;
            return result;
        }

        static bool SyncUxmlAssetsFromSerializedData(BuilderUxmlAttributesEditingContext context, IList uxmlSerializedData, UxmlSerializedData parentUxmlSerialized, UxmlAsset parentAsset,
            UxmlSerializedUxmlObjectAttributeDescription attributeDescription, bool canMakeChanges)
        {
            bool contentsChanged = false;

            s_TempUxmlAssets.Clear();

            using var listPool = ListPool<UxmlObjectAsset>.Get(out var collectedUxmlAssets);
            parentAsset?.CollectUxmlObjectAssets(attributeDescription.rootName, collectedUxmlAssets);

            // Sync the list by checking each item is at the expected index and moving/adding items as needed.
            using var hashSetPool = HashSetPool<int>.Get(out var duplicateIds);
            for (int j = 0; j < uxmlSerializedData.Count; ++j)
            {
                var currentSerializedData = uxmlSerializedData[j] as UxmlSerializedData;

                // Avoid adding null uxml objects when attribute description is not a list
                if (!attributeDescription.isList && currentSerializedData == null)
                {
                    continue;
                }

                if (currentSerializedData != null && currentSerializedData.uxmlAssetId != 0)
                {
                    // When a list element is copied it may also copy the id of the original element.
                    // If the id has already been used we clear it so a new one can be assigned.
                    if (duplicateIds.Contains(currentSerializedData.uxmlAssetId))
                        currentSerializedData.uxmlAssetId = 0;
                }

                // Find matching UxmlObjectAsset
                if (!ExtractOrCreateUxmlSerializedDataUxmlAsset(context, currentSerializedData, parentUxmlSerialized, parentAsset,
                    attributeDescription, canMakeChanges, collectedUxmlAssets, out var foundUxmlAsset, j))
                {
                    if (!canMakeChanges)
                        return false;
                    contentsChanged = true;
                }

                duplicateIds.Add(foundUxmlAsset.id);
                s_TempUxmlAssets.Add(foundUxmlAsset);
            }

            var acceptedTypes = attributeDescription.uxmlObjectAcceptedTypes;

            // If we have uxml assets remaining then the serialized data must have been removed and we should do the same.
            foreach (var collectedUxmlAsset in collectedUxmlAssets)
            {
                if (collectedUxmlAsset == null)
                    continue;

                var isAcceptedType = false;
                foreach (var acceptedType in acceptedTypes)
                {
                    if (acceptedType.DeclaringType?.FullName == collectedUxmlAsset.fullTypeName)
                    {
                        isAcceptedType = true;
                        break;
                    }
                }

                // Do not delete the asset if it's not an accepted type.
                // This avoid deleting other objects of different types when having multiple UxmlObjectReferences with no name like in MultiColumnListView/TreeView
                if (!isAcceptedType && collectedUxmlAsset.fullTypeName != UxmlAsset.NullNodeType)
                {
                    s_TempUxmlAssets.Add(collectedUxmlAsset);
                    continue;
                }

                contentsChanged = true;
                RecordDocumentUndoOnce(context);

                // We need to do this to ensure that any dependencies are also removed.
                collectedUxmlAsset.RemoveAssetAndFieldParentIfEmpty();
            }

            if (contentsChanged)
                parentAsset.SetUxmlObjectAssets(attributeDescription.rootName, s_TempUxmlAssets);

            return true;
        }

        static bool ExtractOrCreateUxmlSerializedDataUxmlAsset(BuilderUxmlAttributesEditingContext context, UxmlSerializedData uxmlSerializedData, UxmlSerializedData parentUxmlSerialized,
            UxmlAsset parentAsset, UxmlSerializedUxmlObjectAttributeDescription attributeDescription, bool canMakeChanges,
            List<UxmlObjectAsset> uxmlObjectAssets, out UxmlObjectAsset uxmlAsset, int expectedIndex)
        {
            // If the asset id is 0 then we do not currently have a UxmlAsset for this serialized data
            if (uxmlSerializedData?.uxmlAssetId != 0)
            {
                // Check if the data is at the expected index.
                if (expectedIndex < uxmlObjectAssets.Count)
                {
                    if (uxmlObjectAssets[expectedIndex] != null &&
                        ((uxmlSerializedData == null && uxmlObjectAssets[expectedIndex]?.isNull == true) ||
                        uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[expectedIndex]?.id))
                    {
                        uxmlAsset = uxmlObjectAssets[expectedIndex];

                        // We dont remove the asset from the list as it will break the expected index but we do set it to null
                        uxmlObjectAssets[expectedIndex] = null;
                        return true;
                    }
                }

                if (!canMakeChanges)
                {
                    uxmlAsset = null;
                    return false;
                }

                RecordDocumentUndoOnce(context);

                // See if we can find it at another index
                for (int i = 0; i < uxmlObjectAssets.Count; ++i)
                {
                    if (uxmlObjectAssets[i] == null)
                        continue;

                    if ((uxmlSerializedData == null && uxmlObjectAssets[i].isNull) ||
                        uxmlSerializedData?.uxmlAssetId == uxmlObjectAssets[i].id)
                    {
                        uxmlAsset = uxmlObjectAssets[i];
                        uxmlObjectAssets[i] = null;
                        return false;
                    }
                }
            }

            if (!canMakeChanges)
            {
                uxmlAsset = null;
                return false;
            }

            RecordDocumentUndoOnce(context);

            attributeDescription.SetSerializedValueAttributeFlags(parentUxmlSerialized, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

            // We could not find the asset so we need to create a new one.
            uxmlAsset = CreateUxmlObjectAsset(context, attributeDescription, uxmlSerializedData, parentAsset);

            return false;
        }

        /// <summary>
        /// Checks if any serialized data fields are not set to their default values. If any are not, apply those changes to the UXML asset.
        /// </summary>
        /// <param name="uxmlSerializedData">The serialized data to check for non-default values.</param>
        /// <param name="uxmlAsset">The asset to apply the uxml attributes to.</param>
        public static void SyncSerializedDataToNewUxmlAsset(BuilderUxmlAttributesEditingContext context, UxmlSerializedData uxmlSerializedData, UxmlAsset uxmlAsset)
        {
            if (uxmlSerializedData == null)
                return;

            var description = UxmlSerializedDataRegistry.GetDescription(uxmlSerializedData.GetType().DeclaringType.FullName);
            bool madeChanges = false;
            foreach (var attribute in description.serializedAttributes)
            {
                if (attribute.isUxmlObject)
                {
                    madeChanges = true;
                    var attributeUxmlObjectDescription = attribute as UxmlSerializedUxmlObjectAttributeDescription;
                    attribute.SetSerializedValueAttributeFlags(uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);
                    if (attribute.isList)
                    {
                        // Extract the serialized data list
                        var serializedDataList = (IList)attribute.GetSerializedValue(uxmlSerializedData);
                        foreach (UxmlSerializedData serializedDataItem in serializedDataList)
                        {
                            CreateUxmlObjectAsset(context, attributeUxmlObjectDescription, serializedDataItem, uxmlAsset);
                        }
                    }
                    else
                    {
                        var serializedData = attribute.GetSerializedValue(uxmlSerializedData) as UxmlSerializedData;

                        // Avoid creating null objects when attribute description is not a list
                        if (serializedData != null)
                        {
                            CreateUxmlObjectAsset(context, attributeUxmlObjectDescription, serializedData, uxmlAsset);
                        }
                    }
                }
                else
                {
                    var attributeValue = attribute.GetSerializedValue(uxmlSerializedData);
                    if (!UxmlAttributeComparison.ObjectEquals(attributeValue, attribute.defaultValue))
                    {
                        madeChanges = true;
                        attribute.SetSerializedValueAttributeFlags(uxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

                        if (attributeValue == null || !UxmlAttributeConverter.TryConvertToString(attributeValue, context.visualTree, out var stringValue))
                            stringValue = attributeValue?.ToString();

                        using (new BuilderUxmlAttributesEditingContext.DisableUndoScope(context))
                        {
                            PostAttributeValueChange(context, attribute.name, stringValue, uxmlAsset);
                        }
                    }
                }
            }

            if (madeChanges)
                context.rootSerializedObject.UpdateIfRequiredOrScript();
        }

        static UxmlObjectAsset CreateUxmlObjectAsset(BuilderUxmlAttributesEditingContext context, UxmlSerializedUxmlObjectAttributeDescription attribute, UxmlSerializedData serializedData, UxmlAsset parentAsset)
        {
            var fullTypeName = serializedData == null ? UxmlAsset.NullNodeType : serializedData.GetType().DeclaringType.FullName;
            var xmlns = context.visualTree.FindUxmlNamespaceDefinitionForTypeName(parentAsset, fullTypeName);
            var uxmlAsset = context.visualTree.AddUxmlObject(parentAsset, attribute.rootName, fullTypeName, xmlns);

            // Assign the new asset id to the serialized data
            if (serializedData != null)
            {
                SyncSerializedDataToNewUxmlAsset(context, serializedData, uxmlAsset);
                serializedData.uxmlAssetId = uxmlAsset.id;
            }

            return uxmlAsset;
        }

        static void RecordDocumentUndoOnce(BuilderUxmlAttributesEditingContext context)
        {
            if (!s_DocumentUndoRecorded)
            {
                UndoRecordDocument(context,BuilderConstants.ModifyUxmlObject);
                s_DocumentUndoRecorded = true;
            }
            UndoRecordDocument(context, BuilderConstants.ModifyUxmlObject);
        }

        public static void CallDeserializeOnElement(BuilderUxmlAttributesEditingContext context, VisualElement element = null)
        {
            if (context.uxmlSerializedData == null)
                return;

            element ??= context.element;

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(element);
            context.uxmlSerializedData.Deserialize(element, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml | UxmlSerializedData.UxmlAttributeFlags.DefaultValue);
        }

        static void PostAttributeValueChange(BuilderUxmlAttributesEditingContext context, string attributeName, string value, UxmlAsset uxmlAsset = null)
        {
            UndoRecordDocument(context, BuilderConstants.ChangeAttributeValueUndoMessage);

            // Set value in asset.
            if (context.isInTemplateInstance)
            {
                TemplateContainer templateContainerParent = GetVisualElementRootTemplate(context.element);

                if (templateContainerParent != null)
                {
                    var templateAsset = templateContainerParent.GetVisualElementAsset() as TemplateAsset;
                    var currentVisualElementName = context.element.name;

                    if (!string.IsNullOrEmpty(currentVisualElementName))
                    {
                        var pathToTemplateAsset = templateAsset.GetPathToTemplateAsset(context.element);
                        templateAsset.SetAttributeOverride(attributeName, value, pathToTemplateAsset);

                        var elementsToChange = templateContainerParent.Query(currentVisualElementName).Where(v => v.GetType() == context.element.GetType());
                        elementsToChange.ForEach(x =>
                        {
                            var templateVea = x.GetVisualElementAssetInTemplate();

                            if (templateVea == null)
                                return;

                            if (!context.usesUxmlTraits)
                            {
                                UxmlSerializer.SyncVisualTreeAssetSerializedData(new CreationContext(context.visualTree), false);
                                CallDeserializeOnElement(context, x);
                            }
                        });
                    }
                }
            }
            else
            {
                uxmlAsset ??= context.elementAsset;
                uxmlAsset.SetAttribute(attributeName, value);
            }
        }

        public static string GetUndoMessage(SerializedProperty prop)
        {
            var undoMessage = $"Modified {prop.name}";
            if (prop.m_SerializedObject.targetObject.name != string.Empty)
                undoMessage += $" in {prop.m_SerializedObject.targetObject.name}";

            return undoMessage;
        }

        // Used to add the asset to the document root element without adding it to the visual tree asset.
        public static bool TryAddAssetToRootElement(BuilderPaneWindow paneWindow, VisualElement newElement,
            VisualTreeAsset visualTreeAsset, string relativePath, VisualElement destination = null, int index = -1)
        {
            if (newElement == null)
                return false;

            if (newElement is TemplateContainer)
            {
                if (!ValidateAsset(visualTreeAsset, relativePath))
                    return false;
            }

            if (destination == null)
                destination = paneWindow.document.primaryViewportWindow.documentRootElement;

            if (index >= 0)
                destination.Insert(index, newElement);
            else
                destination.Add(newElement);


            return true;
        }

        // If the asset is a template, it will be added to the document root element and the visual tree asset.
        public static VisualElement AddTemplateContainerToAsset(BuilderPaneWindow paneWindow, VisualTreeAsset visualTreeAsset, string relativePath, VisualElement destination = null, int index = -1)
        {
            var newElement = visualTreeAsset.CloneTree();

            if (!TryAddAssetToRootElement(paneWindow, newElement, visualTreeAsset, relativePath, destination, index))
                return null;

            newElement.SetProperty(BuilderConstants.LibraryItemLinkedTemplateContainerPathVEPropertyName, relativePath);
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeVisualElementAsset = (inVta, inParent, ve) =>
            {
                var vea = inVta.AddTemplateInstance(inParent, relativePath) as VisualElementAsset;
                ve.SetProperty(BuilderConstants.ElementLinkedInstancedVisualTreeAssetVEPropertyName, visualTreeAsset);
                return vea;
            };

            AddElementToAsset(paneWindow.document.visualTreeAsset, newElement, makeVisualElementAsset, index);

            return newElement;
        }

        public static string ImportAssetFromOutsideProject(string fullAssetPath)
        {
            var endsWithUxml = fullAssetPath.EndsWith(BuilderConstants.UxmlExtension);
            var endsWithUss = fullAssetPath.EndsWith(BuilderConstants.UssExtension);
            if (!endsWithUxml && !endsWithUss)
                return string.Empty;

            var data = File.ReadAllText(fullAssetPath);

            var fileName = Path.GetFileName(fullAssetPath);
            var assetRelativePath = AssetDatabase.GenerateUniqueAssetPath($"Assets/{fileName}");
            var uniqueFileName = Path.GetFileName(assetRelativePath);
            var newCopiedFilePath = Application.dataPath + $"/{uniqueFileName}";

            File.WriteAllText(newCopiedFilePath, data);
            AssetDatabase.ImportAsset(assetRelativePath);
            AssetDatabase.Refresh();
            return assetRelativePath;
        }

        public static void GetListOfPathsInDragAndDrop(List<string> result)
        {
            result.Clear();

            if (DragAndDrop.paths == null) return;

            foreach (var path in DragAndDrop.paths)
            {
                var splitPath = path.Split('/');
                result.Add(splitPath[splitPath.Length - 1]);
            }
        }

        public static bool DraggingBothUxmlAndUSS()
        {
            List<string> listOfPaths = new List<string>();
            GetListOfPathsInDragAndDrop(listOfPaths);
            bool draggingUXML = false;
            bool draggingUSS = false;

            foreach (var path in listOfPaths)
            {
                if (path.EndsWith(BuilderConstants.UxmlExtension))
                    draggingUXML = true;
                else if (path.EndsWith(BuilderConstants.UssExtension))
                    draggingUSS = true;
            }

            return draggingUSS && draggingUXML;
        }
    }
}
