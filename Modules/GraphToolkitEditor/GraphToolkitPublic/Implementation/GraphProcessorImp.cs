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
            switch (context)
            {
                case Node userNode:
                    return userNode.m_Implementation;
                case State userState:
                    return userState.m_Implementation;
                case SelfTransition userTransition:
                    return userTransition.m_Implementation;
                case Condition userCondition:
                    return GetTransitionSupport(userCondition.m_Implementation);
                case TransitionSupportModel transitionSupport:
                    return transitionSupport;
                case TransitionModel rule:
                    return rule.TransitionSupportModel;
                case ConditionModel condition:
                    return GetTransitionSupport(condition);
                case PortModel port:
                    return port;
                case INode node:
                    return node as AbstractNodeModel;
                case IState state:
                    return state as StateModel;
                default:
                    return null;
            }
        }

        internal static Model GetTransitionSupport(ConditionModel conditionModel)
        {
            for (var condition = conditionModel; condition != null; condition = condition.Parent)
            {
                if (condition.Transition?.TransitionSupportModel != null)
                    return condition.Transition.TransitionSupportModel;
            }

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

        public void LogError(object message, object context, ILogAction logAction)
        {
            AddError(message.ToString(), GetModel(context) ?? m_DefaultModel, logAction, userData: context);
        }
        public void LogWarning(object message, object context, ILogAction logAction)
        {
            AddWarning(message.ToString(), GetModel(context) ?? m_DefaultModel, logAction, userData: context);
        }
        public void Log(object message, object context, ILogAction logAction)
        {
            AddMessage(message.ToString(), GetModel(context) ?? m_DefaultModel, logAction, userData: context);
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
