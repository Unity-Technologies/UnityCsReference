// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Base class for views that are attached to <see cref="RootView"/>.
/// </summary>
abstract class ChildView : View
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
    /// <param name="view">The view to which the instance should be added.</param>
    /// <param name="context">The UI creation context.</param>
    protected void Setup(RootView view, IViewContext context = null)
    {
        Context = context;

        PartList = new ModelViewPartList();
        BuildPartList();

        AddToRootView(view);
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
