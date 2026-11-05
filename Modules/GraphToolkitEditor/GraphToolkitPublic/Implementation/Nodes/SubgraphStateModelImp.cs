// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor.Implementation;

class SubgraphStateModelImp : SubgraphStateModel, ISubgraphState
{
    public Graph GetSubgraphAsGraph()
    {
        var graphModel = GetSubgraphModel();
        if (graphModel is StateMachineImp)
        {
            // The subgraph is a state machine, which is not a valid Graph type for this method.
            return null;
        }

        return (graphModel as GraphModelImp)?.Graph as Graph;
    }

    public StateMachine GetSubgraphAsStateMachine()
    {
        var graphModel = GetSubgraphModel();

        return (graphModel as StateMachineImp)?.Graph as StateMachine;
    }

    protected override void OnDefineOptions(StateDefinitionScope scope)
    {
        base.OnDefineOptions(scope);

        GetSubgraphAsStateMachine()?.CallOnDefineSubgraphStateOptions(scope);
    }
}
