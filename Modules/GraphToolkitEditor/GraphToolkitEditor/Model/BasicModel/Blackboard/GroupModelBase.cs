// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine.Pool;

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
        public virtual IEnumerable<GraphElementModel> ContainedModels
        {
            get
            {
                foreach (var item in Items)
                {
                    if (item is GraphElementModel graphElement)
                        yield return graphElement;

                    foreach (var containedModel in item.ContainedModels)
                        yield return containedModel;
                }
            }
        }

        /// <inheritdoc />
        public virtual void Rename(string name)
        {
            Title = name;
        }

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GetGraphElementModels()
        {
            foreach (var item in Items)
            {
                if (item is GraphElementModel gem)
                    yield return gem;
            }
        }

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
            using var dispose = draggedObjects.OfTypeToPooledList<IGroupItemModel, GraphElementModel>(out var items);
            foreach (var obj in items)
            {
                if (obj is GroupModel vgm && this.IsIn(vgm))
                    return false;
            }

            using var disposeVariables = ListPool<VariableDeclarationModelBase>.Get(out var variables);
            RecurseGetVariables(items, variables);

            var section = GetSection();
            string sectionName = section.Title;

            if (variables.Count == 0) // We can always drag empty groups.
                return true;

            foreach (var t in variables)
            {
                if (t.GetSection() == section || GraphModel.CanConvertVariable(t, sectionName))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public abstract IGroupItemModel GetGroupItemInTargetGraph(GraphModel targetGraphModel, Dictionary<VariableDeclarationModelBase, VariableDeclarationModelBase> variableTranslation);

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly ContextualMenuItem[] k_ContextualMenuItems =
        [
            ContextualMenuHelpers.createVariableItem,
            ContextualMenuHelpers.createGroupItem,
            ContextualMenuHelpers.cutItem,
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.renameItem,
            ContextualMenuHelpers.duplicateItem,
            ContextualMenuHelpers.deleteItem,
            ContextualMenuHelpers.selectAllItem,
            ContextualMenuHelpers.selectUnusedItem
        ];
    }
}
