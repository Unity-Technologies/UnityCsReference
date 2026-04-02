// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="BlackboardGroup"/> Part containing all the group's items.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardGroupItemsPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardGroupItemsPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is GroupModelBase)
            {
                return new BlackboardGroupItemsPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        GroupModelBase GroupModel => m_Model as GroupModelBase;

        protected BlackboardGroupItemsPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement();
            m_Root.AddToClassList(m_ParentClassName.WithUssElement("items"));
            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var existingItems = Root.Children().OfType<BlackboardElement>().ToList();
#pragma warning restore UA2001
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var existingItemModels = new HashSet<Model>(existingItems.Select(t => t.Model));
#pragma warning restore UA2001


            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var newItemsModels = GroupModel.Items
#pragma warning restore UA2001
                .Where(t => t is not VariableDeclarationModelBase v || GroupModel.GraphModel.IsVariableVisibleInBlackboard(v)).ToList();

            foreach (var ui in existingItems)
            {
                if (newItemsModels.Contains(ui.Model as IGroupItemModel))
                    continue;

                ui.RemoveFromRootView();
                ui.RemoveFromHierarchy();
            }

            foreach (var vgm in newItemsModels)
            {
                if (vgm is not GraphElementModel graphElementModel)
                    continue;
                if (existingItemModels.Contains(graphElementModel))
                    continue;

                var ui = ModelViewFactory.CreateUI<ModelView>(m_OwnerElement.RootView, graphElementModel);
                if (ui == null)
                    continue;

                Root.Add(ui);
            }

            //Sort the ui in the same order as in the model.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            List<ModelView> items = Root.Children().OfType<ModelView>().ToList();
#pragma warning restore UA2001

            if (items.Count == 0)
                return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            List<IGroupItemModel> itemModels = newItemsModels.ToList();
#pragma warning restore UA2001
            ModelView firstItem = items.FirstOrDefault();
            IGroupItemModel firstModel = itemModels[0];
            if (firstItem == null || !ReferenceEquals(firstItem.Model, firstModel))
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                firstItem = items.First(t => ReferenceEquals(t.Model, firstModel));
#pragma warning restore UA2001
                Root.Insert(0, firstItem);
                items.Remove(firstItem);
                items.Insert(0, firstItem);
            }

            ModelView prevItem = firstItem;
            for (int i = 1; i < itemModels.Count; ++i)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                ModelView currentItem = items.First(t => ReferenceEquals(t.Model, itemModels[i]));
#pragma warning restore UA2001
                if (items[i] != currentItem)
                {
                    currentItem.PlaceInFront(prevItem);
                    items.Remove(currentItem);
                    items.Insert(i, currentItem);
                }

                prevItem = currentItem;
            }
        }
    }
}
