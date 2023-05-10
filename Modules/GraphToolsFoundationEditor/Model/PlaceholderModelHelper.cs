// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
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
        internal static ManagedMissingTypeModelCategory ModelToMissingTypeCategory_Internal(Model model)
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
                case VariableDeclarationModel:
                    category = ManagedMissingTypeModelCategory.VariableDeclaration;
                    break;
                case DeclarationModel:
                    category = ManagedMissingTypeModelCategory.PortalDeclaration;
                    break;
            }

            return category;
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
        internal static bool TryCreatePlaceholder_Internal(GraphModel graphModel, ManagedMissingTypeModelCategory category, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
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

            if (!YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, AbstractNodeModel.titleFieldName_Internal, 0, out var name) ||
                !YamlParsingHelper_Internal.TryParseVector2(referenceMissingType.serializedData, AbstractNodeModel.positionFieldName_Internal, 0, out var position))
                return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            var contextStr = BlockNodeModel.contextNodeModelFieldName_Internal;
            if (YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, contextStr, 0, out _))
            {
                // Block node model
                if (YamlParsingHelper_Internal.TryParseLong(referenceMissingType.serializedData, k_ReferenceIdStr, referenceMissingType.serializedData.IndexOf(contextStr), out var contextReferenceId))
                {
                    var contextNode = ManagedReferenceUtility.GetManagedReference(graphModel.Asset, contextReferenceId) as ContextNodeModel;
                    createdPlaceholder = graphModel.CreateBlockNodePlaceholder_Internal(name, guid, contextNode, referenceMissingType.referenceId);
                }
            }
            else
            {
                createdPlaceholder = graphModel.CreateNodePlaceholder_Internal(name, position, guid, referenceMissingType.referenceId);
            }
        }

        static void TryCreateContextNodePlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, AbstractNodeModel.titleFieldName_Internal, 0, out var name) ||
                !YamlParsingHelper_Internal.TryParseVector2(referenceMissingType.serializedData, AbstractNodeModel.positionFieldName_Internal, 0, out var position))
                return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            if (YamlParsingHelper_Internal.TryParseList(ContextNodeModel.blocksFieldName_Internal, k_ReferenceIdStr, referenceMissingType.serializedData, 0, out var listStr))
            {
                var referenceIds = listStr.Select(long.Parse);
                var blocks = referenceIds.Select(id => ManagedReferenceUtility.GetManagedReference(graphModel.Asset, id) as BlockNodeModel);
                createdPlaceholder = graphModel.CreateContextNodePlaceholder_Internal(name, position, guid, blocks, referenceMissingType.referenceId);
            }
            else
            {
                createdPlaceholder = graphModel.CreateContextNodePlaceholder_Internal(name, position, guid, referenceId: referenceMissingType.referenceId);
            }
        }

        static void TryCreateVariableDeclarationPlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, DeclarationModel.nameFieldName_Internal, 0, out var name)) return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);

            createdPlaceholder = graphModel.CreateVariableDeclarationPlaceholder_Internal(name, guid, referenceMissingType.referenceId);
        }

        static void TryCreateWirePlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;
            var uniqueIdFieldStr = PortReference.uniqueIdFieldName_Internal;

            // Get FromPort data
            var fromPortIndex = referenceMissingType.serializedData.IndexOf(WireModel.fromPortReferenceFieldName_Internal, 0, StringComparison.Ordinal);

            if (fromPortIndex == -1
                || !YamlParsingHelper_Internal.TryParseGUID(referenceMissingType.serializedData,
                    PortReference.nodeModelGuidFieldName_Internal, PortReference.obsoleteNodeModelGuidFieldName_Internal, fromPortIndex, out var parsedFromNodeGuid)
                || !graphModel.TryGetModelFromGuid(parsedFromNodeGuid, out var fromModel)
                || fromModel is not PortNodeModel fromNodeModel
                || !YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, uniqueIdFieldStr, fromPortIndex, out var fromPortUniqueId))
                return;

            // Get ToPort data
            var toPortIndex = referenceMissingType.serializedData.IndexOf(WireModel.toPortReferenceFieldName_Internal, 0, StringComparison.Ordinal);

            if (toPortIndex == -1
                || !YamlParsingHelper_Internal.TryParseGUID(referenceMissingType.serializedData,
                    PortReference.nodeModelGuidFieldName_Internal, PortReference.obsoleteNodeModelGuidFieldName_Internal, toPortIndex, out var parsedToNodeGuid)
                || !graphModel.TryGetModelFromGuid(parsedToNodeGuid, out var toModel)
                || toModel is not PortNodeModel toNodeModel
                || !YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, uniqueIdFieldStr, toPortIndex, out var toPortUniqueId))
                return;

            // Create the missing wire
            var toPort = toNodeModel.Ports.FirstOrDefault(p => p.UniqueName == toPortUniqueId);
            var fromPort = fromNodeModel.Ports.FirstOrDefault(p => p.UniqueName == fromPortUniqueId);

            if (fromPort != null && toPort != null)
                createdPlaceholder = graphModel.CreateWirePlaceholder_Internal(toPort, fromPort, guid, referenceMissingType.referenceId);
        }

        static void TryCreatePortalDeclarationPlaceholder(GraphModel graphModel, ManagedReferenceMissingType referenceMissingType, Hash128 guid, out IPlaceholder createdPlaceholder)
        {
            createdPlaceholder = null;

            if (!YamlParsingHelper_Internal.TryParseString(referenceMissingType.serializedData, DeclarationModel.nameFieldName_Internal, 0, out var name)) return;

            name = string.Format(k_PlaceholderModelName, string.IsNullOrEmpty(name) ? referenceMissingType.className : name);
            createdPlaceholder = graphModel.CreatePortalDeclarationPlaceholder_Internal(name, guid, referenceMissingType.referenceId);
        }
    }
}
