// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base model for portals.
    /// </summary>
    /// <remarks>
    /// 'WirePortalModel' is the base model for portals, which are special nodes that come in pairs to represent a wire connection in a graph. Portals provide
    /// a way to improve graph organization by reducing visual clutter and preventing excessive overlapping connections. Portals are linked through a
    /// <see cref="DeclarationModel"/>, which determines the connection between multiple portals. Instead of directly connecting nodes with long wires,
    /// portals provide entry and exit points, which make the graph more readable and easier to manage.
    /// </remarks>
    [Serializable]
    [UnityRestricted]
    internal abstract class WirePortalModel : NodeModel, IHasDeclarationModel, IRenamable, ICloneable
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        DeclarationModel m_DeclarationModel;

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        SerializableGUID m_DeclarationModelGuid;
#pragma warning restore CS0618

        [SerializeField, HideInInspector]
        Hash128 m_DeclarationModelHashGuid;

        [SerializeField, HideInInspector]
        TypeHandle m_TypeHandle;

        /// <inheritdoc />
        public override bool CanHaveExpandablePorts => false;

        /// <inheritdoc />
        public DeclarationModel DeclarationModel
        {
            get
            {
                if (m_DeclarationModel == null && PlaceholderModelHelper.TryGetPlaceholderGraphElementModel(GraphModel, m_DeclarationModelHashGuid, out var placeholderModel))
                {
                    PlaceholderModelHelper.SetPlaceholderCapabilities(this);
                    return placeholderModel as DeclarationModel;
                }

                return m_DeclarationModel;
            }
        }

        /// <inheritdoc />
        public void SetDeclarationModel(DeclarationModel value)
        {
            if (m_DeclarationModel == value)
                return;
            m_DeclarationModel = value;
            m_DeclarationModelHashGuid = m_DeclarationModel.Guid;
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Gets the <see cref="TypeHandle"/> of the portal's port.
        /// </summary>
        /// <returns>The type.</returns>
        /// <remarks>
        /// 'GetPortDataTypeHandle' retrieves the <see cref="TypeHandle"/> of the port in a <see cref="WirePortalModel"/>. This <see cref="TypeHandle"/> represents the data type
        /// that the portal's port supports for connections. The port's data type is determined from the <see cref="DeclarationModel"/> associated with the
        /// portal, which ensures that the type is correctly aligned with the portal's intended functionality.
        /// </remarks>
        public TypeHandle GetPortDataTypeHandle()
        {
            if (m_DeclarationModel == null && GraphModel?.TryGetModelFromGuid(m_DeclarationModelHashGuid, out var model) == true && model is PortalDeclarationPlaceholder)
                return TypeHandle.MissingPort;

            // Type's identification of portals' ports are empty strings in the compatibility tests.
            if (m_TypeHandle.Identification == null)
                m_TypeHandle = TypeHandleHelpers.GenerateCustomTypeHandle("");
            return m_TypeHandle;
        }

        /// <summary>
        /// The type of the portal's port.
        /// </summary>
        public void SetPortDataTypeHandle(TypeHandle value)
        {
            if (m_TypeHandle == value)
                return;
            m_TypeHandle = value;
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// The <see cref="Unity.GraphToolkit.Editor.PortType"/> of the portal's port.
        /// </summary>
        public virtual PortType PortType => PortType.Default;

        /// <inheritdoc />
        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        /// <summary>
        /// The evaluation order for the portal when multiple portals are linked together.
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
            m_Capabilities.Remove(Editor.Capabilities.Collapsible);
            m_Capabilities.Remove(Editor.Capabilities.Colorable);
        }

        /// <inheritdoc />
        public void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            DeclarationModel?.Rename(newName);

            if (GraphModel is { CurrentGraphChangeDescription: not null })
            {
                using var dirtyScope = GraphModel.AssetDirtyScope();

                var references = GraphModel.FindReferencesInGraph<WirePortalModel>(DeclarationModel);
                foreach (var nodeModel in references)
                {
                    GraphModel.CurrentGraphChangeDescription.AddChangedModel(nodeModel, ChangeHint.Data);
                }
            }
        }

        /// <inheritdoc />
        public Model Clone()
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
        /// Indicates whether there can be one portal that has the same declaration and direction.
        /// </summary>
        /// <returns>True if there can be one portal that has the same declaration and direction</returns>
        public virtual bool CanHaveAnotherPortalWithSameDirectionAndDeclaration() => true;

        /// <summary>
        /// Indicates whether we can create an opposite portal for this portal.
        /// </summary>
        /// <returns>True if we can create an opposite portal for this portal.</returns>
        public virtual bool CanCreateOppositePortal()
        {
            return true;
        }

        /// <summary>
        /// Indicates whether we can revert this portal to a wire.
        /// </summary>
        /// <returns>True if we can revert this portal to a wire.</returns>
        public virtual bool CanRevertToWire()
        {
            // To be able to create a wire, the portal and the opposite portals need to be connected to another node.
            if (GetConnectedWires().Count() == 0)
                return false;

            var isEntryPortal = this is ISingleInputPortNodeModel;
            if (isEntryPortal)
            {
                foreach (var exitPortal in GraphModel.GetExitPortals(DeclarationModel))
                {
                    if (exitPortal.GetConnectedWires().Count() > 0)
                        return true;
                }
            }
            else
            {
                foreach (var entryPortal in GraphModel.GetEntryPortals(DeclarationModel))
                {
                    if (entryPortal.GetConnectedWires().Count() > 0)
                        return true;
                }
            }

            return false;
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

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var nodeMenuItems = base.ContextualMenuItems;
                var menuItems = new List<ContextualMenuItem>(nodeMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.createOppositePortalItem,
            ContextualMenuHelpers.revertToWireItem,
            ContextualMenuHelpers.revertAllToWiresItem,
        };
    }
}
