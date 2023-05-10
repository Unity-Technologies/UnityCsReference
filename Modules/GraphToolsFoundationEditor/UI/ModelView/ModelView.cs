// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for all UI element that displays a <see cref="Model"/>.
    /// </summary>
    abstract class ModelView : ChildView
    {
        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        public Model Model { get; private set; }

        protected UIDependencies Dependencies { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelView"/> class.
        /// </summary>
        protected ModelView()
        {
            Dependencies = new UIDependencies(this);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        /// <summary>
        /// Helper method that calls <see cref="Setup"/>, <see cref="View.BuildUI"/> and <see cref="View.UpdateFromModel"/>.
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
            Setup(view, context);
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

            base.UpdateFromModel();

            Dependencies.UpdateDependencyLists();
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

        /// <inheritdoc />
        public override void AddToRootView(RootView view)
        {
            base.AddToRootView(view);
            ViewForModel.AddOrReplaceModelView_Internal(this);
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            Dependencies.ClearDependencyLists();
            ViewForModel.RemoveModelView_Internal(this);
            base.RemoveFromRootView();
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
