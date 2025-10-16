// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A button to open a graph associated with a node.
    /// </summary>
    [UnityRestricted]
    internal class OpenGraphButton : NodeToolbarButton
    {
        /// <summary>
        /// The name of a <see cref="OpenGraphButton"/>.
        /// </summary>
        public static readonly string openGraphButtonName = "open-graph-button";

        /// <summary>
        /// The USS class name added to a <see cref="OpenGraphButton"/>.
        /// </summary>
        public static readonly string openGraphButtonUssClassName = "ge-open-graph-button";

        /// <summary>
        /// The USS class name added to the icon of a <see cref="OpenGraphButton"/>.
        /// </summary>
        public static readonly string openGraphButtonIconElementUssClassName = openGraphButtonUssClassName.WithUssElement(GraphElementHelper.iconName);

        string m_BlackboardTitle;
        GraphModel m_GraphModelToOpen;
        GraphView m_GraphView;
        LoadGraphCommand.LoadStrategies m_LoadStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenGraphButton"/> class.
        /// </summary>
        /// <param name="buttonName">The name of the button, used to identify it.</param>
        /// <param name="onClick">The action when the button is clicked.</param>
        /// <param name="showCallback">A callback after the button is shown on the node. Buttons are shown on hover, else they aren't.</param>
        public OpenGraphButton(string buttonName, Action onClick, Action<bool> showCallback = null)
            : base(buttonName, onClick, null, showCallback)
        {
            AddToClassList(openGraphButtonUssClassName);
            AddToClassList(NodeView.ussClassName.WithUssElement(openGraphButtonName));
            Icon.AddToClassList(openGraphButtonIconElementUssClassName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenGraphButton"/> class.
        /// </summary>
        /// <param name="buttonName">The name of the button, used to identify it.</param>
        /// <param name="graphToOpen">The graph to open by pressing the button.</param>
        /// <param name="graphView">The graph view containing the button.</param>
        /// <param name="blackboardTitle">The title to be displayed in the blackboard after opening the graph.</param>
        /// <param name="loadStrategy">The load strategy when opening the graph.</param>
        /// <param name="showCallback">A callback after the button is shown on the node. Buttons are shown on hover, else they aren't.</param>
        public OpenGraphButton(string buttonName, GraphModel graphToOpen, GraphView graphView, string blackboardTitle = "", LoadGraphCommand.LoadStrategies loadStrategy = LoadGraphCommand.LoadStrategies.PushOnStack, Action<bool> showCallback = null)
            : this(buttonName, null, showCallback)
        {
            m_GraphModelToOpen = graphToOpen;
            m_BlackboardTitle = blackboardTitle;
            m_GraphView = graphView;
            m_LoadStrategy = loadStrategy;
        }

        /// <inheritdoc/>
        protected override void OnClick()
        {
            if (m_GraphModelToOpen == null)
                return;

            m_GraphView.Dispatch(new LoadGraphCommand(m_GraphModelToOpen, m_LoadStrategy, title: m_BlackboardTitle));
            if (m_GraphView.Window is GraphViewEditorWindow graphViewWindow)
                graphViewWindow.UpdateWindowsWithSameCurrentGraph(false);
        }
    }
}
