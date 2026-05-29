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

        static Model GetModel(object context)
        {
            AbstractNodeModel nodeModel;
            if (context is Node userNode)
                nodeModel = userNode.m_Implementation;
            else
            {
                if (context is INode node)
                    nodeModel = node as AbstractNodeModel;
                else
                {
                    nodeModel = null;
                }
            }

            if (nodeModel != null)
                return nodeModel;
            if (context is PortModel port)
                return port;
            return null;
        }

        void IErrorsAndWarnings.LogError(object message, object context)
        {
            AddError(message.ToString(), GetModel(context) ?? m_DefaultModel, userData: context);
        }
        void IErrorsAndWarnings.LogWarning(object message, object context)
        {
            AddWarning(message.ToString(), GetModel(context) ?? m_DefaultModel, userData: context);
        }
        void IErrorsAndWarnings.Log(object message, object context)
        {
            AddMessage(message.ToString(), GetModel(context) ?? m_DefaultModel, userData: context);
        }

        public void LogError(object message, object context, GraphLogAction graphLogAction)
        {
            AddError(message.ToString(), GetModel(context) ?? m_DefaultModel, graphLogAction, userData: context);
        }
        public void LogWarning(object message, object context, GraphLogAction graphLogAction)
        {
            AddWarning(message.ToString(), GetModel(context) ?? m_DefaultModel, graphLogAction, userData: context);
        }
        public void Log(object message, object context, GraphLogAction graphLogAction)
        {
            AddMessage(message.ToString(), GetModel(context) ?? m_DefaultModel, graphLogAction, userData: context);
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
