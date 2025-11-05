// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The base class for models having <see cref="IGroupItemModel"/> items.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class GroupModelBase : GraphElementModel, IGroupItemModel, IGraphElementContainer, IRenamable, IHasTitle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupModelBase" /> class.
        /// </summary>
        public GroupModelBase()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Droppable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Collapsible,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Renamable
            });
        }

        /// <inheritdoc />
        public abstract string Title { get; set; }

        /// <summary>
        /// The list of items contained in this group.
        /// </summary>
        public abstract IReadOnlyList<IGroupItemModel> Items { get; }

        /// <inheritdoc />
        public GroupModelBase ParentGroup { get; set; }

        /// <inheritdoc />
        public virtual IEnumerable<GraphElementModel> ContainedModels => Items.SelectMany(t => Enumerable.Repeat(t as GraphElementModel, 1).Concat(t.ContainedModels));

        /// <inheritdoc />
        public virtual void Rename(string name)
        {
            Title = name;
        }

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GetGraphElementModels() => Items.OfType<GraphElementModel>();

        /// <inheritdoc />
        public virtual void RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels) { }

        /// <inheritdoc />
        public virtual bool Repair()
        {
            return false;
        }

        /// <summary>
        /// Returns the section for this Group.
        /// </summary>
        /// <returns>The section for this Group.</returns>
        public virtual SectionModel GetSection() => ParentGroup.GetSection();

        static void RecurseGetVariables(IEnumerable<IGroupItemModel> items, List<VariableDeclarationModelBase> variables)
        {
            foreach (var item in items)
            {
                if (item is VariableDeclarationModelBase variable)
                    variables.Add(variable);
                else if (item is GroupModel group)
                    RecurseGetVariables(group.Items, variables);
            }
        }

        /// <summary>
        /// Returns whether this element can accept elements as its items.
        /// </summary>
        /// <param name="draggedObjects">The dragged elements.</param>
        /// <returns>Whether this element can accept elements as its items.</returns>
        public virtual bool CanAcceptDrop(IEnumerable<GraphElementModel> draggedObjects)
        {
            var items = draggedObjects.OfType<IGroupItemModel>();

            foreach (var obj in items)
            {
                if (obj is GroupModel vgm && this.IsIn(vgm))
                    return false;
            }
            var variables = new List<VariableDeclarationModelBase>();
            RecurseGetVariables(items, variables);

            var section = GetSection();
            string sectionName = section.Title;

            if (variables.Count == 0) // We can always drag empty groups.
                return true;

            if (variables.All(t => t.GetSection() != section && !GraphModel.CanConvertVariable(t, sectionName)))
                return false;

            return true;
        }

        /// <inheritdoc />
        public abstract IGroupItemModel GetGroupItemInTargetGraph(GraphModel targetGraphModel, Dictionary<VariableDeclarationModelBase, VariableDeclarationModelBase> variableTranslation);
    }
}
