// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A helper to create placeholder models.
    /// </summary>
    static class PlaceholderModelHelper
    {
        public const string missingTypeWontBeRestored = "[Missing data: Model won't be restored.]";
        const string k_PlaceholderModelName = "! Missing ! {0}";
        const string k_ReferenceIdStr = "rid";

        /// <summary>
        /// Gets the missing type category of a model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The missing type category of the model.</returns>
        public static ManagedMissingTypeModelCategory ModelToMissingTypeCategory(Model model)
        {
            var category = ManagedMissingTypeModelCategory.Node;

            switch (model)
            {
                case ContextNodeModel:
                    category = ManagedMissingTypeModelCategory.ContextNode;
                    break;
                case WireModel:
                    category = ManagedMissingTypeModelCategory.Wire;
                    break;
                case VariableDeclarationModelBase:
                    category = ManagedMissingTypeModelCategory.VariableDeclaration;
                    break;
                case DeclarationModel:
                    category = ManagedMissingTypeModelCategory.PortalDeclaration;
                    break;
            }

            return category;
        }

        /// <summary>
        /// Tries to get a placeholder <see cref="GraphElementModel"/> from the graph with its guid.
        /// </summary>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="originalModelGuid">The guid of the model.</param>
        /// <param name="placeholderGraphElementModel">The returned model if found, null otherwise.</param>
        /// <returns>True if the placeholder was found, false otherwise.</returns>
        public static bool TryGetPlaceholderGraphElementModel(GraphModel graphModel, Hash128 originalModelGuid, out GraphElementModel placeholderGraphElementModel)
        {
            placeholderGraphElementModel = null;
            if (graphModel.TryGetModelFromGuid(originalModelGuid, out var model) && model is IPlaceholder)
            {
                placeholderGraphElementModel = model;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the capabilities of the placeholder model.
        /// </summary>
        /// <remarks>A placeholder should only be deletable and selectable.</remarks>
        /// <param name="model">The model.</param>
        public static void SetPlaceholderCapabilities(GraphElementModel model)
        {
            model.ClearCapabilities();
            model.SetCapability(Capabilities.Deletable, true);
            model.SetCapability(Capabilities.Selectable, true);
        }

        /// <summary>
        /// Tries to create a placeholder for a model.
        /// </summary>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="category">The category of the model that will be replaced by the placeholder.</param>
        /// <param name="referenceMissingType">The managed reference missing type of the model.</param>
        /// <param name="guid">The guid of the model.</param>
        /// <param name="createdPlaceholder">The placeholder if it was successfully created, null otherwise</param>
        /// <returns>True if the placeholder was created, false otherwise.</returns>
        public static bool TryCreatePlaceholder(GraphModel graphModel, ManagedMissingTypeModelCategory category, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;
            switch (category)
            {
                case ManagedMissingTypeModelCategory.Node:
                case ManagedMissingTypeModelCategory.BlockNode:
                    TryCreateNodePlaceholder(graphModel, referenceMissingType, guid, out createdPlaceholder);
                    break;
                case ManagedMissingTypeModelCategory.ContextNode:
                    TryCreateContextNodePlaceholder(graphModel, referenceMissingType, guid, out createdPlaceholder);
                    break;
                case ManagedMissingTypeModelCategory.VariableDeclaration:
                    TryCreateVariableDeclarationPlaceholder(graphModel, referenceMissingType, guid, out createdPlaceholder);
                    break;
                case ManagedMissingTypeModelCategory.Wire:
                    TryCreateWirePlaceholder(graphModel, referenceMissingType, guid, out createdPlaceholder);
                    break;
                case ManagedMissingTypeModelCategory.PortalDeclaration:
                    TryCreatePortalDeclarationPlaceholder(graphModel, referenceMissingType, guid, out createdPlaceholder);
                    break;
                default:
                    Debug.LogWarning("This category of missing model is not managed.");
                    break;
            }

            return createdPlaceholder != null;
        }

        static void TryCreateNodePlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper.TryParseString(referenceMissingType.serializedData, AbstractNodeModel.titleFieldName, 0, out var name) ||
                !YamlParsingHelper.TryParseVector2(referenceMissingType.serializedData, AbstractNodeModel.positionFieldName, 0, out var position))
                return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            var contextStr = BlockNodeModel.contextNodeModelFieldName;
            if (YamlParsingHelper.TryParseString(referenceMissingType.serializedData, contextStr, 0, out _))
            {
                // Block node model
                if (YamlParsingHelper.TryParseLong(referenceMissingType.serializedData, k_ReferenceIdStr, referenceMissingType.serializedData.IndexOf(contextStr), out var contextReferenceId))
                {
                    var contextNode = ManagedReferenceUtility.GetManagedReference(graphModel.GraphObject, contextReferenceId) as ContextNodeModel;
                    createdPlaceholder = graphModel.CreateBlockNodePlaceholder(name, guid, contextNode, referenceMissingType.referenceId);
                }
            }
            else
            {
                createdPlaceholder = graphModel.CreateNodePlaceholder(name, position, guid, referenceMissingType.referenceId);
            }
        }

        static void TryCreateContextNodePlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper.TryParseString(referenceMissingType.serializedData, AbstractNodeModel.titleFieldName, 0, out var name) ||
                !YamlParsingHelper.TryParseVector2(referenceMissingType.serializedData, AbstractNodeModel.positionFieldName, 0, out var position))
                return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            if (YamlParsingHelper.TryParseList(ContextNodeModel.blocksFieldName, k_ReferenceIdStr, referenceMissingType.serializedData, 0, out var listStr))
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var referenceIds = listStr.Select(long.Parse);
#pragma warning restore UA2001
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var blocks = referenceIds.Select(id => ManagedReferenceUtility.GetManagedReference(graphModel.GraphObject, id) as BlockNodeModel);
#pragma warning restore UA2001
                createdPlaceholder = graphModel.CreateContextNodePlaceholder(name, position, guid, blocks, referenceMissingType.referenceId);
            }
            else
            {
                createdPlaceholder = graphModel.CreateContextNodePlaceholder(name, position, guid, referenceId: referenceMissingType.referenceId);
            }
        }

        static void TryCreateVariableDeclarationPlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper.TryParseString(referenceMissingType.serializedData, DeclarationModel.nameFieldName, 0, out var name)) return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            createdPlaceholder = graphModel.CreateVariableDeclarationPlaceholder(name, guid, referenceMissingType.referenceId);
        }

        static void TryCreateWirePlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;
            var uniqueIdFieldStr = PortReference.uniqueIdFieldName;

            // Get FromPort data
            var fromPortIndex = referenceMissingType.serializedData.IndexOf(WireModel.k_FromPortReferenceFieldName, 0, StringComparison.Ordinal);

            if (fromPortIndex == -1
                || !YamlParsingHelper.TryParseGUID(referenceMissingType.serializedData,
                    PortReference.nodeModelGuidFieldName, PortReference.obsoleteNodeModelGuidFieldName, fromPortIndex, out var parsedFromNodeGuid)
                || !graphModel.TryGetModelFromGuid(parsedFromNodeGuid, out var fromModel)
                || fromModel is not PortNodeModel fromNodeModel
                || !YamlParsingHelper.TryParseString(referenceMissingType.serializedData, uniqueIdFieldStr, fromPortIndex, out var fromPortUniqueId))
                return;

            // Get ToPort data
            var toPortIndex = referenceMissingType.serializedData.IndexOf(WireModel.k_ToPortReferenceFieldName, 0, StringComparison.Ordinal);

            if (toPortIndex == -1
                || !YamlParsingHelper.TryParseGUID(referenceMissingType.serializedData,
                    PortReference.nodeModelGuidFieldName, PortReference.obsoleteNodeModelGuidFieldName, toPortIndex, out var parsedToNodeGuid)
                || !graphModel.TryGetModelFromGuid(parsedToNodeGuid, out var toModel)
                || toModel is not PortNodeModel toNodeModel
                || !YamlParsingHelper.TryParseString(referenceMissingType.serializedData, uniqueIdFieldStr, toPortIndex, out var toPortUniqueId))
                return;

            // Create the missing wire
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var toPort = toNodeModel.GetPorts().FirstOrDefault(p => p.UniqueName == toPortUniqueId);
#pragma warning restore UA2001
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var fromPort = fromNodeModel.GetPorts().FirstOrDefault(p => p.UniqueName == fromPortUniqueId);
#pragma warning restore UA2001

            if (fromPort != null && toPort != null)
                createdPlaceholder = graphModel.CreateWirePlaceholder(toPort, fromPort, guid, referenceMissingType.referenceId);
        }

        static void TryCreatePortalDeclarationPlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper.TryParseString(referenceMissingType.serializedData, DeclarationModel.nameFieldName, 0, out var name)) return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);
            createdPlaceholder = graphModel.CreatePortalDeclarationPlaceholder(name, guid, referenceMissingType.referenceId);
        }
    }
}
