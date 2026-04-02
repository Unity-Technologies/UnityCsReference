// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.UIToolkit.Editor;
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
    internal static class BuilderAssetUtilities
    {
        public static string assetsPath { get; } = Application.dataPath;
        public static string projectPath { get; } = assetsPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
        public static string packagesPath { get; } = projectPath + "/Packages";

        static bool s_DocumentUndoRecorded;

        internal static readonly PropertyName UndoGroupPropertyKey = "__UnityUndoGroup";

        static string GetFullPath(string path)
        {
            return Path.GetFullPath(path).Replace("\\", "/");
        }

        public static bool IsPathInProject(string path, bool allowImmutable = true)
        {
            return !string.IsNullOrWhiteSpace(GetPathRelativeToProject(path, allowImmutable));
        }

        public static string GetPathRelativeToProject(string path, bool allowImmutable = true)
        {
            var fullPath = Path.GetFullPath(path);
            var fullPathWithUnitySeparators = fullPath.Replace("\\", "/");

            if (fullPathWithUnitySeparators.StartsWith(Application.dataPath))
            {
                return "Assets" + fullPathWithUnitySeparators.Substring(Application.dataPath.Length);
            }

            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (fullPath.StartsWith(package.resolvedPath))
                {
                    if (!allowImmutable && package.source != UnityEditor.PackageManager.PackageSource.Local && package.source != UnityEditor.PackageManager.PackageSource.Embedded)
                            return null;

                    return package.assetPath + fullPathWithUnitySeparators.Substring(package.resolvedPath.Length);
                }
            }

            return null;
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
                        if (overwrittenElement.serializedData != null)
                            UxmlSerializer.TryParseSerializedAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value, overwrittenElement.serializedData, new CreationContext(visualTreeAsset));
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
            return UxmlAssetUtilities.GetVisualElementRootTemplate(visualElement, VisualElementExtensions.GetVisualElementAsset);
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var pathToTemplateAsset = templateAsset.GetPathToTemplateAsset(visualElement).ToList();
#pragma warning restore UA2001

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return templateAsset?.attributeOverrides.Count(x => x.m_AttributeName == attributeName && x.NamesPathMatchesElementNamesPath(pathToTemplateAsset)) > 0;
#pragma warning restore UA2001
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
            s_DocumentUndoRecorded = false;

            Action<VisualTreeAsset, VisualElement> handleTemplateOverride = null;
            if (context.isInTemplateInstance)
            {
                handleTemplateOverride = (vta, element) =>
                {
                    CallDeserializeOnElement(context, element);
                };
            }

            var result = UxmlAssetUtilities.SynchronizePath(
                context.visualTree,
                context.uxmlSerializedData,
                context.elementAsset,
                context.serializedBasePath,
                propertyPath,
                changeUxmlAssets,
                context.element,
                () => RecordDocumentUndoOnce(context),
                context.isInTemplateInstance,
                handleTemplateOverride,
                VisualElementExtensions.GetVisualElementAsset);

            // We need to update the serialized object if we made changes.
            if (changeUxmlAssets)
                context.rootSerializedObject.UpdateIfRequiredOrScript();

            return result;
        }

        static void RecordDocumentUndoOnce(BuilderUxmlAttributesEditingContext context)
        {
            if (!s_DocumentUndoRecorded)
            {
                UndoRecordDocument(context,BuilderConstants.ModifyUxmlObject);
                s_DocumentUndoRecorded = true;
            }
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
