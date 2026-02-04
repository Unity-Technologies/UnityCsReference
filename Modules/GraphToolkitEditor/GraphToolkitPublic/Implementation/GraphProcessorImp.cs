// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor.Implementation
{
    class ErrorsAndWarningsImp : ErrorsAndWarningsResult, IErrorsAndWarnings
    {
        Model m_DefaultModel;
        public ErrorsAndWarningsImp(Model defaultModel)
        {
            m_DefaultModel = defaultModel;
        }

        AbstractNodeModel GetNodeModel(object context)
        {
            if (context is Node userNode)
                return userNode.m_Implementation;
            if (context is INode node)
                return (AbstractNodeModel)node;
            return null;
        }

        void IErrorsAndWarnings.LogError(object message, object context)
        {
            AddError(message.ToString(), GetNodeModel(context) ?? m_DefaultModel);
        }
        void IErrorsAndWarnings.LogWarning(object message, object context)
        {
            AddWarning(message.ToString(), GetNodeModel(context) ?? m_DefaultModel);
        }
        void IErrorsAndWarnings.Log(object message, object context)
        {
            AddMessage(message.ToString(), GetNodeModel(context) ?? m_DefaultModel);
        }
    }

    class GraphProcessorImp : GraphProcessor
    {
        GraphModelImp m_GraphModel;

        public GraphProcessorImp(GraphModelImp graphModel)
        {
            m_GraphModel = graphModel;
        }

        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
            return m_GraphModel.CallOnGraphChanged(changes);
        }
    }
}
