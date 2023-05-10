// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Abstract base class for UI parts.
    /// </summary>
    abstract class ModelViewPart
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
        public void BuildUI(VisualElement parent)
        {
            BuildPartUI(parent);

            if (Root != null)
            {
                for (var i = 0; i < PartList.Parts.Count; i++)
                {
                    var component = PartList.Parts[i];
                    component.BuildUI(Root);
                }
            }
        }

        /// <summary>
        /// Called once the UI has been built.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        public void PostBuildUI()
        {
            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.PostBuildUI();
            }

            PostBuildPartUI();
        }

        /// <summary>
        /// Updates the part using the associated model.
        /// </summary>
        public void UpdateFromModel()
        {
            UpdatePartFromModel();

            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                component.UpdateFromModel();
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
        protected abstract void BuildPartUI(VisualElement parent);

        /// <summary>
        /// Finalizes the building of the UI.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        protected virtual void PostBuildPartUI() { }

        /// <summary>
        /// Updates the part to reflect the assigned model.
        /// </summary>
        protected abstract void UpdatePartFromModel();

        protected virtual void PartOwnerAddedToView() { }
        protected virtual void PartOwnerRemovedFromView() { }
    }
}
