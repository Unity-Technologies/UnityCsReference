// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for views that are attached to <see cref="RootView"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class ChildView : View
    {
        ContextualMenuManipulator m_ContextualMenuManipulator;

        /// <summary>
        /// The view that owns this object.
        /// </summary>
        public RootView RootView { get; protected set; }

        /// <summary>
        /// The UI creation context.
        /// </summary>
        public IViewContext Context { get; private set; }

        /// <summary>
        /// The part list.
        /// </summary>
        public ModelViewPartList PartList { get; private set; }

        /// <summary>
        /// The contextual menu manipulator.
        /// </summary>
        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildView"/> class.
        /// </summary>
        protected ChildView()
        {
            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        /// <summary>
        /// Builds the list of parts for this UI Element.
        /// </summary>
        protected virtual void BuildPartList() { }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="view">The view to which the instance is added.</param>
        /// <param name="context">The UI creation context.</param>
        protected void Setup(RootView view, IViewContext context = null)
        {
            Context = context;

            PartList = new ModelViewPartList();
            BuildPartList();

            AddToRootView(view);
        }

        /// <inheritdoc />
        public override void BuildUITree()
        {
            ClearUI();
            BuildUI();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.BuildUITree(this);
            }

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUITree();
            }

            PostBuildUI();
        }

        /// <summary>
        /// Fully updates the view by running all <see cref="ViewUpdateVisitor"/>s obtained from the <see cref="RootView"/>.
        /// </summary>
        public void DoCompleteUpdate()
        {
            var initializers = RootView.GetChildViewUpdaters(this);
            if (initializers != null)
            {
                foreach (var initializer in initializers)
                {
                    UpdateView(initializer);
                }
            }
        }

        /// <summary>
        /// Recursively updates this view and its children parts using the specified <see cref="ViewUpdateVisitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view.</param>
        public virtual void UpdateView(ViewUpdateVisitor visitor)
        {
            visitor.Update(this);

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateView(visitor);
            }
        }

        /// <summary>
        /// Removes all children VisualElements.
        /// </summary>
        protected virtual void ClearUI()
        {
            Clear();
        }

        /// <summary>
        /// Build the UI for this instance: instantiates VisualElements, sets USS classes.
        /// </summary>
        protected virtual void BuildUI() { }

        /// <summary>
        /// Finalizes the building of the UI. Stylesheets are typically added here.
        /// </summary>
        protected virtual void PostBuildUI() { }

        /// <summary>
        /// Update the element to reflect the state of the attached model.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view, which contains additional information on the work to perform.</param>
        public virtual void UpdateUIFromModel(UpdateFromModelVisitor visitor) { }

        /// <summary>
        /// Update the element to reflect the selected state of the attached model.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view, which contains additional information on the work to perform.</param>
        public virtual void UpdateUISelection(UpdateSelectionVisitor visitor) { }

        /// <summary>
        /// Callback to add menu items to the contextual menu.
        /// </summary>
        /// <param name="evt">The <see cref="ContextualMenuPopulateEvent"/>.</param>
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }

        /// <summary>
        /// Adds the instance to a view.
        /// </summary>
        /// <param name="view">The view to add the element to.</param>
        public virtual void AddToRootView(RootView view)
        {
            RootView = view;

            if (PartList != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.OwnerAddedToView();
                }
            }
        }

        /// <summary>
        /// Removes the instance from the view.
        /// </summary>
        public virtual void RemoveFromRootView()
        {
            if (PartList != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.OwnerRemovedFromView();
                }
            }

            RootView = null;
        }
    }
}
