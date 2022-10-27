// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for onboarding providers, which displays the UI when there is no active graph.
    /// </summary>
    abstract class OnboardingProvider
    {
        protected const string k_PromptToCreateTitle = "Create {0}";
        protected const string k_ButtonText = "New {0}";
        protected const string k_PromptToCreate = "Create a new {0}";

        protected static VisualElement AddNewGraphButton<T>(
            ICommandTarget commandTarget,
            GraphTemplate template,
            string promptTitle = null,
            string buttonText = null,
            string prompt = null) where T : GraphAsset
        {
            promptTitle = promptTitle ?? string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            buttonText = buttonText ?? string.Format(k_ButtonText, template.GraphTypeName);
            prompt = prompt ?? string.Format(k_PromptToCreate, template.GraphTypeName);

            var container = new VisualElement();
            container.AddToClassList("onboarding-block");

            var label = new Label(prompt);
            container.Add(label);

            var button = new Button { text = buttonText };
            button.clicked += () =>
            {
                var graphAsset = GraphAssetCreationHelpers.PromptToCreateGraphAsset(typeof(T), template, promptTitle, prompt);
                commandTarget.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
            };
            container.Add(button);

            return container;
        }

        public abstract VisualElement CreateOnboardingElements(ICommandTarget commandTarget);

        public virtual bool GetGraphAndObjectFromSelection(ToolStateComponent toolState, Object selectedObject, out GraphAsset graphAsset, out GameObject boundObject)
        {
            graphAsset = null;
            boundObject = null;

            if (selectedObject is GraphAsset selectedObjectAsGraph)
            {
                // don't change the current object if it's the same graph
                var currentOpenedGraph = toolState.CurrentGraph;
                if (selectedObjectAsGraph == currentOpenedGraph.GetGraphAsset())
                {
                    graphAsset = currentOpenedGraph.GetGraphAsset();
                    boundObject = currentOpenedGraph.BoundObject;
                    return true;
                }
            }

            return false;
        }
    }
}
