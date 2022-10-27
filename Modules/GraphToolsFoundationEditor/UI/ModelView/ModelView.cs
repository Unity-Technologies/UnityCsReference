// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for all UI element that displays a <see cref="GraphElementModel"/>.
    /// </summary>
    abstract class ModelView : BaseModelView
    {
        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        public Model Model { get; private set; }

        /// <summary>
        /// The view that owns this object.
        /// </summary>
        public RootView RootView { get; protected set; }

        /// <summary>
        /// The UI creation context.
        /// </summary>
        public IViewContext Context { get; private set; }

        public ModelViewPartList PartList { get; private set; }

        protected UIDependencies Dependencies { get; }

        ContextualMenuManipulator m_ContextualMenuManipulator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelView"/> class.
        /// </summary>
        protected ModelView()
        {
            Dependencies = new UIDependencies(this);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

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
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void SetupBuildAndUpdate(Model model, RootView view, IViewContext context = null)
        {
            Setup(model, view, context);
            BuildUI();
            UpdateFromModel();
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void Setup(Model model, RootView view, IViewContext context = null)
        {
            Model = model;
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

        /// <summary>
        /// Whether a manipulator is currently overriding the position of this UI element.
        /// </summary>
        /// <remarks>When this is set to true, this UI element position is not updated from the model.</remarks>
        public bool PositionIsOverriddenByManipulator { protected get; set; }

        /// <inheritdoc />
        public override void UpdateFromModel()
        {
            if (RootView?.GraphTool?.Preferences.GetBool(BoolPref.LogUIUpdate) ?? false)
            {
                Debug.Log($"Rebuilding {this}");
                if (RootView == null)
                {
                    Debug.LogWarning($"Updating a model UI that is not attached to a view: {this}");
                }
            }

            UpdateElementFromModel();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateFromModel();
            }

            Dependencies.UpdateDependencyLists();
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

        protected virtual void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            Dependencies.OnCustomStyleResolved(evt);
        }

        protected virtual void OnGeometryChanged(GeometryChangedEvent evt)
        {
            Dependencies.OnGeometryChanged(evt);
        }

        protected virtual void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            Dependencies.OnDetachedFromPanel(evt);
        }

        /// <summary>
        /// Tells whether the UI has some backward dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some backward dependencies has changed, false otherwise.</returns>
        public virtual bool HasBackwardsDependenciesChanged() => false;

        /// <summary>
        /// Tells whether the UI has some forward dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some forward dependencies has changed, false otherwise.</returns>
        public virtual bool HasForwardsDependenciesChanged() => false;

        /// <summary>
        /// Tells whether the UI has some dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some dependencies has changed, false otherwise.</returns>
        public virtual bool HasModelDependenciesChanged() => false;

        /// <summary>
        /// Adds graph elements to the forward dependencies list. A forward dependency is
        /// a graph element that should be updated whenever this model UI is updated.
        /// </summary>
        public virtual void AddForwardDependencies()
        {
        }

        /// <summary>
        /// Adds graph elements to the backward dependencies list. A backward dependency is
        /// a graph element that causes this model UI to be updated whenever it is updated.
        /// </summary>
        public virtual void AddBackwardDependencies()
        {
        }

        /// <summary>
        /// Adds graph elements to the model dependencies list. A model dependency is
        /// a graph element model that causes this model UI to be updated whenever it is updated.
        /// </summary>
        public virtual void AddModelDependencies()
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
            ViewForModel.AddOrReplaceModelView_Internal(this);

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

            Dependencies.ClearDependencyLists();
            ViewForModel.RemoveModelView_Internal(this);
            RootView = null;
        }

        /// <summary>
        /// Paste data in UI element.
        /// </summary>
        /// <param name="operation">The paste operation type.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="delta">The delta to add to element positions.</param>
        /// <param name="copyPasteData">The data to paste.</param>
        /// <returns>Returns true if the UI element handles the paste operation, false otherwise.</returns>
        public virtual bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            return false;
        }

        /// <summary>
        /// Place the focus on that element, which can be different things like starting to edit the title of a new element.
        /// </summary>
        public virtual void ActivateRename()
        {
        }

        /// <summary>
        /// Displays the UI to rename the element.
        /// </summary>
        /// <returns>True if the UI could be displayed. False otherwise.</returns>
        public virtual bool Rename()
        {
            var editableLabel = this.SafeQ<EditableLabel>();

            if (editableLabel != null)
            {
                // Execute after current event finished processing.
                schedule.Execute(() => editableLabel.BeginEditing()).ExecuteLater(0);
            }

            return editableLabel != null;
        }

        /// <summary>
        /// Returns whether the passed keyboard event is a rename event on this platform
        /// </summary>
        /// <param name="e">The event.</param>
        /// <return>Whether the event is a key rename event</return>
        public static bool IsRenameKey<T>(KeyboardEventBase<T> e) where T : KeyboardEventBase<T>, new()
        {
            return e.keyCode == KeyCode.F2 && (e.modifiers & ~EventModifiers.FunctionKey) == EventModifiers.None;
        }
    }
}
