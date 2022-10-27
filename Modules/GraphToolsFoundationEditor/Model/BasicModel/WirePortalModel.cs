// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base implementation for portals.
    /// </summary>
    [Serializable]
    abstract class WirePortalModel : NodeModel, IHasDeclarationModel, IRenamable, ICloneable
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        DeclarationModel m_DeclarationModel;

        [SerializeField]
        TypeHandle m_TypeHandle;

        /// <inheritdoc />
        public DeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = value;
        }

        /// <summary>
        /// The type of the portal's port.
        /// </summary>
        public TypeHandle PortDataTypeHandle
        {
            get
            {
                // Type's identification of portals' ports are empty strings in the compatibility tests.
                if (m_TypeHandle.Identification == null)
                    m_TypeHandle = TypeHandle.Create_Internal("");
                return m_TypeHandle;
            }
            set => m_TypeHandle = value;
        }

        /// <inheritdoc />
        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        /// <summary>
        /// Evaluation order for the portal, when multiple portals are linked together.
        /// </summary>
        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WirePortalModel"/> class.
        /// </summary>
        protected WirePortalModel()
        {
            m_Capabilities.Add(Editor.Capabilities.Renamable);
        }

        /// <inheritdoc />
        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            DeclarationModel?.Rename(newName);
        }

        /// <inheritdoc />
        public GraphElementModel Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = CloneHelpers.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        /// <summary>
        /// Whether there can be one portal that has the same declaration and direction.
        /// </summary>
        /// <returns>True if there can be one portal that has the same declaration and direction</returns>
        public virtual bool CanHaveAnotherPortalWithSameDirectionAndDeclaration() => true;

        /// <summary>
        /// Whether we can create an opposite portal for this portal.
        /// </summary>
        /// <returns>True if we can create an opposite portal for this portal.</returns>
        public virtual bool CanCreateOppositePortal()
        {
            return true;
        }
    }
}
