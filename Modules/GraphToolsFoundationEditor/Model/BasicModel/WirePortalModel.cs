// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
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

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_DeclarationModelGuid;
#pragma warning restore CS0618

        [SerializeField]
        Hash128 m_DeclarationModelHashGuid;

        [SerializeField]
        TypeHandle m_TypeHandle;

        /// <inheritdoc />
        public DeclarationModel DeclarationModel
        {
            get
            {
                if (m_DeclarationModel == null && GraphModel.TryGetModelFromGuid(m_DeclarationModelHashGuid, out var model) && model is PortalDeclarationPlaceholder missingDeclarationModel)
                {
                    this.SetCapability(Editor.Capabilities.Movable, false);
                    this.SetCapability(Editor.Capabilities.Copiable, false);
                    this.SetCapability(Editor.Capabilities.Droppable, false);

                    return missingDeclarationModel;
                }
                this.SetCapability(Editor.Capabilities.Movable, true);
                this.SetCapability(Editor.Capabilities.Copiable, true);
                this.SetCapability(Editor.Capabilities.Droppable, true);
                return m_DeclarationModel;
            }
            set
            {
                if (m_DeclarationModel == value)
                    return;
                m_DeclarationModel = value;
                m_DeclarationModelHashGuid = m_DeclarationModel.Guid;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The type of the portal's port.
        /// </summary>
        public TypeHandle PortDataTypeHandle
        {
            get
            {
                if (m_DeclarationModel == null && GraphModel.TryGetModelFromGuid(m_DeclarationModelHashGuid, out var model) && model is PortalDeclarationPlaceholder)
                    return TypeHandle.MissingPort;

                // Type's identification of portals' ports are empty strings in the compatibility tests.
                if (m_TypeHandle.Identification == null)
                    m_TypeHandle = TypeHandle.Create_Internal("");
                return m_TypeHandle;
            }
            set
            {
                if (m_TypeHandle == value)
                    return;
                m_TypeHandle = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
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

            var references = GraphModel.FindReferencesInGraph<WirePortalModel>(DeclarationModel).OfType<AbstractNodeModel>();
            GraphModel.CurrentGraphChangeDescription?.AddChangedModels(references, ChangeHint.Data);
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

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

#pragma warning disable CS0612
            m_DeclarationModelGuid = m_DeclarationModelHashGuid;
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

#pragma warning disable CS0612
            m_DeclarationModelHashGuid = m_DeclarationModelGuid;
#pragma warning restore CS0612
        }
    }
}
