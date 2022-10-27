// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class GroupModel : GraphElementModel, IGroupItemModel, IGraphElementContainer, IRenamable, IHasTitle
    {
        [SerializeReference]
        List<IGroupItemModel> m_Items = new();

        /// <inheritdoc />
        [field: SerializeField]
        public virtual string Title { get; set; }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <inheritdoc />
        public virtual GroupModel ParentGroup { get; set; }

        /// <summary>
        /// The items in this group.
        /// </summary>
        public virtual IReadOnlyList<IGroupItemModel> Items => m_Items;

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GraphElementModels => Items.OfType<GraphElementModel>();

        /// <inheritdoc />
        public override IGraphElementContainer Container => ParentGroup;

        /// <inheritdoc />
        public virtual IEnumerable<GraphElementModel> ContainedModels => Items.SelectMany(t => Enumerable.Repeat(t as GraphElementModel, 1).Concat(t.ContainedModels));

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupModel" /> class.
        /// </summary>
        public GroupModel()
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

        /// <summary>
        /// Inserts an item at the given index.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <param name="index">The index at which insert the item. For index &lt;= 0, The item will be added at the beginning. For index &gt;= Items.Count, items will be added at the end./</param>
        /// <returns>The graph elements changed by this method.</returns>
        public virtual IEnumerable<GraphElementModel> InsertItem(IGroupItemModel itemModel, int index = int.MaxValue)
        {
            HashSet<GraphElementModel> changedModels = new HashSet<GraphElementModel>();

            GroupModel current = this;
            while (current != null)
            {
                if (ReferenceEquals(current, itemModel))
                    return Enumerable.Empty<GraphElementModel>();
                current = current.ParentGroup;
            }

            if (itemModel.ParentGroup != null)
                changedModels.UnionWith(itemModel.ParentGroup.RemoveItem(itemModel));

            changedModels.Add(this);
            itemModel.ParentGroup = this;
            index = Math.Clamp(index, 0, m_Items.Count);
            m_Items.Insert(index, itemModel);

            return changedModels;
        }

        /// <summary>
        /// Moves some items to this group.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="insertAfter">The item after which the new items will be added. To add the items at the end, pass null.</param>
        /// <returns>The graph elements changed by this method.</returns>
        public virtual IEnumerable<GraphElementModel> MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter)
        {
            if (insertAfter != null && !m_Items.Contains(insertAfter))
                return null;

            if (items.Contains((insertAfter)))
                return null;

            HashSet<GraphElementModel> changedModels = new HashSet<GraphElementModel>();
            foreach (var model in items)
            {
                if (model.ParentGroup != null)
                    changedModels.UnionWith(model.ParentGroup.RemoveItem(model));
            }

            // remove items from m_Items
            //   done by replacing m_Items with a copy that excludes items
            //   in most cases this is faster than doing many List.Remove
            var itemsCopy = new List<IGroupItemModel>(m_Items.Count);
            foreach (var item in m_Items)
            {
                if (!items.Contains(item))
                    itemsCopy.Add(item);
            }

            m_Items = itemsCopy;

            int insertIndex = m_Items.IndexOf(insertAfter);

            foreach (var model in items)
                changedModels.UnionWith(InsertItem(model, ++insertIndex));
            return changedModels;
        }

        /// <summary>
        /// Removes an item from the group.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        /// <returns>The graph elements changed by this method.</returns>
        public virtual IEnumerable<GraphElementModel> RemoveItem(IGroupItemModel itemModel)
        {
            HashSet<GraphElementModel> changedModels = new HashSet<GraphElementModel>();

            if (m_Items.Contains(itemModel))
            {
                itemModel.ParentGroup = null;

                m_Items.Remove(itemModel);

                changedModels.Add(this);
            }

            return changedModels;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_Items.Any(t => t == null))
            {
                m_Items = m_Items.Where(t => t != null).ToList();
            }
            foreach (var item in m_Items)
            {
                item.ParentGroup = this;
            }
        }

        /// <inheritdoc />
        public virtual void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            foreach (var element in elementModels)
                if (element is IGroupItemModel item)
                    RemoveItem(item);
        }

        /// <inheritdoc />
        public virtual void Rename(string name)
        {
            Title = name;
        }

        /// <inheritdoc />
        public virtual void Repair()
        {
            m_Items.RemoveAll(t=>t == null);
            foreach (var item in m_Items.OfType<GroupModel>())
            {
                item.Repair();
            }
        }
    }
}
