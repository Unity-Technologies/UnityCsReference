// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model that represents the content of the blackboard for a graph.
    /// </summary>
    [UnityRestricted]
    [Serializable]
    internal class BlackboardContentModel : Model
    {
        /// <summary>
        /// The GraphModel displayed in the blackboard.
        /// </summary>
        public GraphModel GraphModel => m_GraphTool.ToolState.GraphModel;

        bool m_HasCreatedVariable;
        VariableCreationInfos m_LastVariableInfos;
        readonly GraphTool m_GraphTool;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardContentModel"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="GraphTool"/>.</param>
        public BlackboardContentModel(GraphTool graphTool)
        {
            m_GraphTool = graphTool;
        }

        /// <summary>
        /// Whether the model is valid.
        /// </summary>
        /// <returns>Whether the model is valid.</returns>
        public virtual bool IsValid() => GraphModel != null;

        /// <summary>
        /// Gets the title of the blackboard.
        /// </summary>
        /// <returns>The title of the blackboard.</returns>
        public virtual string GetTitle()
        {
            return m_GraphTool.ToolState.CurrentGraphLabel;
        }

        /// <summary>
        /// Gets the subtitle of the blackboard.
        /// </summary>
        /// <returns>The subtitle of the blackboard.</returns>
        public virtual string GetSubTitle()
        {
            var graphModel = GraphModel;
            if (graphModel?.GraphObject is null)
                return "";

            if (graphModel.IsLocalSubgraph)
                return "(Local Subgraph)";

            var assetName = graphModel.Name;
            var prefix = GetTitle() == assetName ? "" : $"{assetName} ";
            return $"{prefix}({graphModel.GraphObject.GetType().Name.Nicify()})";
        }

        /// <summary>
        /// The <see cref="VariableCreationInfos"/> of the last created variable.
        /// </summary>
        public VariableCreationInfos LastVariableInfos
        {
            get
            {
                if (m_HasCreatedVariable)
                {
                    // Only return the last variable infos when at least one variable has been created in the graph.
                    return m_LastVariableInfos;
                }

                return DefaultVariableInfos;
            }
            set
            {
                m_LastVariableInfos = value;
                m_HasCreatedVariable = true;
            }
        }

        /// <summary>
        /// The <see cref="VariableCreationInfos"/> for the default variable to create when no variable has been created in the graph yet.
        /// </summary>
        public virtual VariableCreationInfos DefaultVariableInfos => new();

        /// <summary>
        /// Indicates whether the blackboard contains the default button.
        /// </summary>
        /// <returns><c>true</c> if the blackboard has the default button.</returns>
        /// <remarks>Use the default button to quickly create a variable in the blackboard using the <see cref="LastVariableInfos"/>.</remarks>
        public virtual bool HasDefaultButton()
        {
            return true;
        }

        /// <summary>
        /// Represents an item in the blackboard menu.
        /// </summary>
        /// <remarks>
        /// 'MenuItem' represents an entry in the blackboard menu, which is to create variable declaration models in the blackboard.
        /// Each menu item consists of a name and an associated action that executes when the item is selected.
        /// </remarks>
        [UnityRestricted]
        internal class MenuItem
        {
            /// <summary>
            /// The name of the menu item.
            /// </summary>
            public string name;
            /// <summary>
            /// The action performed when the menu item is selected.
            /// </summary>
            public Action action;
        }

        /// <summary>
        /// Populates the given <paramref name="menuItems"/> given a section, to create variable declaration models for a blackboard.
        /// </summary>
        /// <param name="sectionName">The name of the section in which the menu is added.</param>
        /// <param name="menuItems">An array of <see cref="MenuItem"/> to fill.</param>
        /// <param name="view">The view.</param>
        /// <param name="selectedGroup">The currently selected group model.</param>
        public virtual void PopulateCreateMenu(string sectionName, List<MenuItem> menuItems, RootView view, GroupModel selectedGroup = null)
        {
            if (sectionName != GraphModel.DefaultSectionName)
            {
                menuItems.Add(new MenuItem
                {
                    name = "Create " + sectionName,
                    action = () =>
                    {
                        view.Dispatch(new CreateGraphVariableDeclarationCommand(sectionName, VariableScope.Local, TypeHandle.Float, selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
                    }
                });
            }
        }
    }
}
