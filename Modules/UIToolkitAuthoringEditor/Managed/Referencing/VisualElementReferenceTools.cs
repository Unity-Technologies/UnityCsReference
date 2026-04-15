// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class VisualElementReferenceTools
{
    public static string GenerateVisualElementAssetLabel(VisualElementAsset vea, bool fallbackToTypeName = true)
    {
        if (vea.serializedData is VisualElement.UxmlSerializedData uxmlData)
        {
            if (!string.IsNullOrEmpty(uxmlData.nameValue))
                return uxmlData.nameValue;
            else if (fallbackToTypeName)
                return uxmlData.GetType().DeclaringType.Name;
        }
        else
        {
            if (vea.isRoot)
                return vea.visualTreeAsset.name;
            else if (fallbackToTypeName)
                return vea.GetType().Name;
        }

        return null;
    }

    public static string GenerateVisualElementLabel(VisualElement ve)
    {
        if (!string.IsNullOrEmpty(ve.name))
            return ve.name;
        return ve.GetType().Name;
    }

    /// <summary>
    /// Attempts to create a reference to a <see cref="VisualElement"/> within a <see cref="PanelRenderer"/>.
    /// </summary>
    /// <param name="element">The element to reference.</param>
    /// <param name="panelRenderer">The <see cref="PanelRenderer"/> that contains the target visual element.</param>
    /// <param name="authoringIdPath">The resolved path to the element.</param>
    /// <returns>
    /// <see langword="true"/> if the reference was successfully created; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreateReference(VisualElement element, out PanelRenderer panelRenderer, out AuthoringIdPath authoringIdPath)
    {
        return TryCreateReference(element, out panelRenderer, out authoringIdPath, true);
    }

    /// <summary>
    /// Attempts to create a reference to a <see cref="VisualElement"/> within a <see cref="PanelRenderer"/>.
    /// </summary>
    /// <param name="element">The element to reference.</param>
    /// <param name="panelRenderer">The <see cref="PanelRenderer"/> that contains the target visual element.</param>
    /// <param name="authoringIdPath">The resolved path to the element.</param>
    /// <param name="addMissingAuthoringIds">If <see langword="true"/>, missing authoring IDs are added to the UXML. If <see langword="false"/>, the method returns false when authoring IDs are missing.</param>
    /// <returns>
    /// <see langword="true"/> if the reference was successfully created; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreateReference(VisualElement element, out PanelRenderer panelRenderer, out AuthoringIdPath authoringIdPath, bool addMissingAuthoringIds)
    {
        panelRenderer = default;
        authoringIdPath = default;

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        var panelComponent = element.FindRootPanelComponent();
        if (panelComponent is not PanelRenderer renderer)
        {
            Debug.LogWarning($"Cannot reference the element '{GenerateVisualElementLabel(element)}' because referencing requires it to be part of a PanelRenderer. It is currently part of a '{panelComponent.GetType().Name}' component.");
            return false;
        }

        if (element is not IPanelComponentRootElement && element.visualElementAsset == null)
        {
            Debug.LogWarning($"Can not reference a temporary element {element.name} ({element.GetType().Name}).", renderer);
            return false;
        }

        panelRenderer = renderer;
        return TryCreateReference(panelRenderer, element, out authoringIdPath, addMissingAuthoringIds);
    }

    /// <summary>
    /// Creates a reference to a <see cref="VisualElement"/> within a <see cref="PanelRenderer"/> using the specified AuthoringIdPath.
    /// If any elements in the path are missing an authoring-id attribute, the attribute will be added during this process.
    /// </summary>
    /// <param name="panelRenderer">The <see cref="PanelRenderer"/> that contains the target visual element.</param>
    /// <param name="authoringIdPath">The sequence of authoring IDs representing the element path.</param>
    /// <returns>
    /// <see langword="true"/> if the reference was successfully created; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreateReference(PanelRenderer panelRenderer, AuthoringIdPath authoringIdPath)
    {
        if (panelRenderer == null)
            throw new ArgumentNullException(nameof(panelRenderer));
        if (panelRenderer.visualTreeAsset == null)
            throw new NullReferenceException("The PanelRenderer must have a VisualTreeAsset assigned to create a reference.");

        // Root reference, nothing to do
        if (authoringIdPath.isRootReference)
            return true;

        using var _ = ListPool<VisualElementAsset>.Get(out var pathToElement);
        var foundElement = panelRenderer.visualTreeAsset.FindElementByPath(authoringIdPath.path, pathToElement);
        if (foundElement == null)
            return false;

        // Check for missing authoring IDs in the path
        using var missingIdsPool = ListPool<VisualElementAsset>.Get(out var missingAuthoringIds);
        foreach (var vea in pathToElement)
        {
            if (!vea.hasAuthoringId)
                missingAuthoringIds.Add(vea);
        }

        AddMissingAuthoringIds(missingAuthoringIds);
        return true;
    }

    /// <summary>
    /// Creates a reference to a <see cref="VisualElement"/> within a <see cref="PanelRenderer"/>.
    /// </summary>
    /// <param name="panelRenderer">The <see cref="PanelRenderer"/> that contains the target visual element.</param>
    /// <param name="visualElement">The element to reference.</param>
    /// <param name="authoringIdPath">The resolved path to the element.</param>
    public static void CreateReference(PanelRenderer panelRenderer, VisualElement visualElement, out AuthoringIdPath authoringIdPath)
    {
        TryCreateReference(panelRenderer, visualElement, out authoringIdPath, true);
    }

    /// <summary>
    /// Attempts to create a reference to a <see cref="VisualElement"/> within a <see cref="PanelRenderer"/>.
    /// </summary>
    /// <param name="panelRenderer">The <see cref="PanelRenderer"/> that contains the target visual element.</param>
    /// <param name="visualElement">The element to reference.</param>
    /// <param name="authoringIdPath">The resolved path to the element.</param>
    /// <param name="addMissingAuthoringIds">If <see langword="true"/>, missing authoring IDs are added to the UXML. If <see langword="false"/>, the method returns false when authoring IDs are missing.</param>
    /// <returns>
    /// <see langword="true"/> if the reference was successfully created; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreateReference(PanelRenderer panelRenderer, VisualElement visualElement, out AuthoringIdPath authoringIdPath, bool addMissingAuthoringIds)
    {
        authoringIdPath = default;

        if (panelRenderer == null)
            throw new ArgumentNullException(nameof(panelRenderer));
        if (visualElement == null)
            throw new ArgumentNullException(nameof(visualElement));
        if (visualElement.visualElementAsset == null)
            throw new Exception("The visual element must be part of a VisualTreeAsset to create a reference.");

        if (visualElement is IPanelComponentRootElement rootElement)
        {
            // We use id 0 to represent the root.
            authoringIdPath = new AuthoringIdPath([0]);
            return true;
        }

        var rootComponent = visualElement.FindRootPanelComponent();
        if (!ReferenceEquals(rootComponent, panelRenderer))
            throw new Exception("The visual element is not part of the specified PanelRenderer.");

        using var pathPool = ListPool<VisualElementAsset>.Get(out var pathElements);
        using var missingIdsPool = ListPool<VisualElementAsset>.Get(out var missingAuthoringIds);

        pathElements.Add(visualElement.visualElementAsset);
        int[] pathIds;

        // Walk up to the root, collecting TemplateContainers
        var currentElement = visualElement;
        currentElement = currentElement.parent;
        while (currentElement != null)
        {
            if (currentElement is TemplateContainer template && currentElement is not IPanelComponentRootElement)
            {
                pathElements.Add(template.visualElementAsset);
            }
            currentElement = currentElement.parent;
        }

        // Reverse the path to go from root to leaf
        pathElements.Reverse();

        pathIds = new int[pathElements.Count];
        for (int i = 0; i < pathElements.Count; i++)
        {
            var vea = pathElements[i];
            pathIds[i] = vea.id;
            if (!vea.hasAuthoringId)
                missingAuthoringIds.Add(vea);
        }

        if (missingAuthoringIds.Count > 0)
        {
            if (!addMissingAuthoringIds)
                return false;

            AddMissingAuthoringIds(missingAuthoringIds);
        }

        authoringIdPath = new AuthoringIdPath(pathIds);
        return true;
    }

    static void AddMissingAuthoringIds(List<VisualElementAsset> missingAuthoringIds)
    {
        if (missingAuthoringIds.Count == 0)
            return;

        using (new AssetDatabase.AssetEditingScope())
        {
            var exporter = VisualTreeAssetExporter.Default;

            foreach (var vea in missingAuthoringIds)
            {
                var vtaPath = AssetDatabase.GetAssetPath(vea.visualTreeAsset);

                // Make the current ID persistent by setting it in the asset
                if (UxmlAttributeConverter.TryConvertToString(vea.id, vea.visualTreeAsset, out string idAsString))
                {
                    vea.SetAttribute(UxmlAsset.AuthoringIdAttribute, idAsString);
                    vea.hasAuthoringId = true;
                }

                var uxml = exporter.ToUxmlString(vea.visualTreeAsset);
                File.WriteAllText(vtaPath, uxml);
                AssetDatabase.ImportAsset(vtaPath);
            }
        }
    }
}
