// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    abstract class MultipleModelsView : BaseModelView
    {
        /// <summary>
        /// The models that backs the UI.
        /// </summary>
        public IEnumerable<Model> Models { get; private set; }

        /// <summary>
        /// The view that owns this object.
        /// </summary>
        public RootView RootView { get; protected set; }

        /// <summary>
        /// The UI creation context.
        /// </summary>
        public IViewContext Context { get; private set; }

        public ModelViewPartList PartList { get; private set; }

        ContextualMenuManipulator m_ContextualMenuManipulator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelView"/> class.
        /// </summary>
        protected MultipleModelsView()
        {
            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        /// <summary>
        /// Builds the list of parts for this UI Element.
        /// </summary>
        protected virtual void BuildPartList() { }

        /// <summary>
        /// Helper method that calls <see cref="Setup"/>, <see cref="BaseModelView.BuildUI"/> and <see cref="BaseModelView.UpdateFromModel"/>.
        /// </summary>
        /// <param name="models">The models that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void SetupBuildAndUpdate(IEnumerable<Model> models, RootView view, IViewContext context = null)
        {
            Setup(models, view, context);
            BuildUI();
            UpdateFromModel();
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="models">The models that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void Setup(IEnumerable<Model> models, RootView view, IViewContext context = null)
        {
            Models = models;
            RootView = view;
            Context = context;

            PartList = new ModelViewPartList();
            BuildPartList();
        }

        /// <inheritdoc />
        public override void BuildUI()
        {
            ClearElementUI();
            BuildElementUI();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.BuildUI(this);
            }

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUI();
            }

            PostBuildUI();
        }

        /// <inheritdoc />
        public override void UpdateFromModel()
        {
            UpdateElementFromModel();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateFromModel();
            }
        }

        /// <summary>
        /// Removes all children VisualElements.
        /// </summary>
        protected virtual void ClearElementUI()
        {
            Clear();
        }

        /// <summary>
        /// Build the UI for this instance: instantiates VisualElements, sets USS classes.
        /// </summary>
        protected virtual void BuildElementUI()
        {
        }

        /// <summary>
        /// Finalizes the building of the UI. Stylesheets are typically added here.
        /// </summary>
        protected virtual void PostBuildUI()
        {
        }

        /// <summary>
        /// Update the element to reflect the state of the attached model.
        /// </summary>
        protected virtual void UpdateElementFromModel()
        {
        }

        /// <summary>
        /// Callback to add menu items to the contextual menu.
        /// </summary>
        /// <param name="evt">The <see cref="ContextualMenuPopulateEvent"/>.</param>
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

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
