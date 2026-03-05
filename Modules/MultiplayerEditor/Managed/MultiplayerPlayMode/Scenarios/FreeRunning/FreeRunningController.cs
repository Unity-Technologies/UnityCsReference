// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class FreeRunningController : InstanceControllerDecorator<FreeRunningController.InstanceSettings>
    {
        [Serializable]
        internal struct InstanceSettings
        {
            public RunModeState RunMode;
        }

        [InitializeOnLoadMethod]
        static void Register()
        {
            InstanceExtensionManager.RegisterDecorator<LocalPlayerController, FreeRunningController>();
        }

        protected internal override VisualElement CreateControllerUI(Instance instance)
        {
            return new Label("Free Running Mode");
        }

        class TestNode : ExecutionNode
        {
            public TestNode(string name) : base(name)
            {
            }

            protected override Task ExecuteAsync(CancellationToken cancellationToken)
            {
                UnityEngine.Debug.Log("Executing Free Running Node");
                throw new Exception("This node is meant to throw an exception to demonstrate error handling in Free Running Mode.");
            }
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph graph)
        {
            graph.AddNode(new TestNode("Free Running Node"), ExecutionStage.Validate);
        }
    }
}
