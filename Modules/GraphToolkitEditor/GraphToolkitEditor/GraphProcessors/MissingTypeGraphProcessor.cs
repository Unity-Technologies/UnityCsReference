// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor.Implementation;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Processor that reports an error for each node, state, block or context whose backing type is missing.
    /// </summary>
    class MissingTypeGraphProcessor : GraphProcessor
    {
        const string k_MissingTypeMessage = "Type definition missing. The script that defines this type was renamed, moved, or deleted. Delete this node or restore the script to fix the error.";

        readonly GraphModel m_GraphModel;

        public MissingTypeGraphProcessor(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        /// <inheritdoc />
        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
            var res = new ErrorsAndWarningsResult();

            foreach (var placeholder in m_GraphModel.Placeholders)
            {
                if (placeholder is NodePlaceholder or StatePlaceholder)
                    res.AddError(k_MissingTypeMessage, placeholder as Model);
            }

            // A wrapper can only be missing its definition while the asset holds a managed reference with a missing type, and that check is much cheaper than walking every node.
            if (m_GraphModel.GraphObject == null || !SerializationUtility.HasManagedReferencesWithMissingTypes(m_GraphModel.GraphObject))
                return res;

            foreach (var nodeModel in m_GraphModel.NodeAndBlockModels)
            {
                if (nodeModel is IUserModelImp { IsMissingDefinition: true })
                    res.AddError(k_MissingTypeMessage, nodeModel);
            }

            return res;
        }
    }
}
