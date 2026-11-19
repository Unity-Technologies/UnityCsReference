// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Abstract base class for UI parts.
    /// </summary>
    [UnityRestricted]
    internal abstract class ModelViewPart
    {
        /// <summary>
        /// The part name.
        /// </summary>
        public string PartName { get; }

        public ModelViewPartList PartList { get; } = new ModelViewPartList();

        /// <summary>
        /// The root visual element of the part.
        /// </summary>
        public abstract VisualElement Root { get; }

        protected string m_ParentClassName;

        protected ChildView m_OwnerElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModelViewPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected ModelViewPart(string name, ChildView ownerElement, string parentClassName)
        {
            PartName = name;
            m_OwnerElement = ownerElement;
            m_ParentClassName = parentClassName;
        }

        /// <summary>
        /// Builds the UI for the part.
        /// </summary>
        /// <param name="parent">The parent visual element to which the UI of the part will be attached.</param>
        public void BuildUITree(VisualElement parent)
        {
            BuildUI(parent);

            if (Root != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.BuildUITree(Root);
                }
            }
        }

        /// <summary>
        /// Called once the UI of the part has been built.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        public void PostBuildUITree()
        {
            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUITree();
            }

            PostBuildUI();
        }

        /// <summary>
        /// Fully updates the part by running all <see cref="ViewUpdateVisitor"/>s obtained from the <see cref="RootView"/>.
        /// </summary>
        public void DoCompleteUpdate()
        {
            var initializers = m_OwnerElement.RootView.GetChildViewUpdaters(m_OwnerElement);
            if (initializers != null)
            {
                foreach (var initializer in initializers)
                {
                    UpdateView(initializer);
                }
            }
        }

        /// <summary>
        /// Recursively updates this part and its children using the specified <see cref="ViewUpdateVisitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view.</param>
        public void UpdateView(ViewUpdateVisitor visitor)
        {
            visitor.Update(this);

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateView(visitor);
            }
        }

        /// <summary>
        /// Called when the part owner is added to a <see cref="GraphView"/>.
        /// </summary>
        public void OwnerAddedToView()
        {
            PartOwnerAddedToView();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.OwnerAddedToView();
            }
        }

        /// <summary>
        /// Called when the part owner is removed from a <see cref="GraphView"/>.
        /// </summary>
        public void OwnerRemovedFromView()
        {
            PartOwnerRemovedFromView();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.OwnerRemovedFromView();
            }
        }

        /// <summary>
        /// Creates the UI for this part.
        /// </summary>
        /// <param name="parent">The parent element to attach the created UI to.</param>
        protected abstract void BuildUI(VisualElement parent);

        /// <summary>
        /// Finalizes the building of the UI.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        protected virtual void PostBuildUI() { }

        /// <summary>
        /// Updates the part to reflect the assigned model.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view, which contains additional information on the work to perform.</param>
        public abstract void UpdateUIFromModel(UpdateFromModelVisitor visitor);

        /// <summary>
        /// Called when the part owner is added to a <see cref="GraphView"/>.
        /// </summary>
        /// <remarks>
        /// 'PartOwnerAddedToView' is called when the part owner is added to a <see cref="GraphView"/>. Override this method to perform additional
        /// setup or initialization when the part becomes part of the view, such as adding dependent models to the view.
        /// </remarks>
        protected virtual void PartOwnerAddedToView() { }

        /// <summary>
        /// Called when the part owner is removed from a <see cref="GraphView"/>.
        /// </summary>
        /// <remarks>
        /// 'PartOwnerRemovedFromView' is called when the part owner is removed from a <see cref="GraphView"/>. Override this method to perform cleanup tasks
        /// when the part is no longer part of the view, such as removing dependent models from the view.
        /// </remarks>
        protected virtual void PartOwnerRemovedFromView() { }
    }
}
