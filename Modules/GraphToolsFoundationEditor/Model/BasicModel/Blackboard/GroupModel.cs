// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class GroupModel : GraphElementModel, IGroupItemModel, IGraphElementContainer, IRenamable, IHasTitle
    {
        [SerializeReference]
        List<IGroupItemModel> m_Items = new();

        [SerializeField, FormerlySerializedAs("Title")]
        string m_Title;

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                m_Title = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

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
        public override IEnumerable<GraphElementModel> DependentModels => base.DependentModels.Concat(GraphElementModels);

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
        public virtual void InsertItem(IGroupItemModel itemModel, int index = int.MaxValue)
        {
            GroupModel current = this;
            while (current != null)
            {
                if (ReferenceEquals(current, itemModel))
                    return;
                current = current.ParentGroup;
            }

            itemModel.ParentGroup?.RemoveItem(itemModel);

            GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Grouping);
            itemModel.ParentGroup = this;
            index = Math.Clamp(index, 0, m_Items.Count);
            m_Items.Insert(index, itemModel);
        }

        /// <summary>
        /// Moves some items to this group.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="insertAfter">The item after which the new items will be added. To add the items at the end, pass null.</param>
        public virtual void MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter)
        {
            if (insertAfter != null && !m_Items.Contains(insertAfter))
                return;

            if (items.Contains((insertAfter)))
                return;

            foreach (var model in items)
            {
                model.ParentGroup?.RemoveItem(model);
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
                InsertItem(model, ++insertIndex);
        }

        /// <summary>
        /// Removes an item from the group.
        /// </summary>
        /// <param name="itemModel">The item.</param>
        public virtual void RemoveItem(IGroupItemModel itemModel)
        {
            if (m_Items.Contains(itemModel))
            {
                itemModel.ParentGroup = null;

                m_Items.Remove(itemModel);

                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Grouping);
            }
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
