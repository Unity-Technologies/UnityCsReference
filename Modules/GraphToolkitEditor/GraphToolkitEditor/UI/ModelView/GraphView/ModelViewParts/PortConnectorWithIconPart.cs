// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A <see cref="PortConnectorPart"/> with an icon showing the port type.
    /// </summary>
    [UnityRestricted]
    internal class PortConnectorWithIconPart : PortConnectorPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorWithIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorWithIconPart"/>.</returns>
        public new static PortConnectorWithIconPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is PortModel && ownerElement is Port)
            {
                return new PortConnectorWithIconPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The icon of the <see cref="PortConnectorWithIconPart"/>.
        /// </summary>
        protected Image m_Icon;

        TypeHandle m_CurrentTypeHandle;
        bool m_IconIsInline;

        /// <summary>
        /// The icon of the <see cref="PortConnectorWithIconPart"/>.
        /// </summary>
        public VisualElement Icon => m_Icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConnectorWithIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConnectorWithIconPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            m_Icon = new Image();
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement(GraphElementHelper.iconName));
            Root.Add(m_Icon);
            m_Icon.PlaceBehind(m_ConnectorLabel);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            Root.AddPackageStylesheet("PortConnectorWithIconPart.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (m_Model is PortModel portModel && m_CurrentTypeHandle != portModel.DataTypeHandle)
            {
                m_OwnerElement.RootView.TypeHandleInfos.RemoveUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                m_CurrentTypeHandle = portModel.DataTypeHandle;

                // Use registered style for the type if any.
                var typeStyle = portModel.GraphModel?.GetDataTypeStyle(portModel.PortDataType);
                if (typeStyle.HasValue)
                {
                    m_IconIsInline = true;
                    m_Icon.tintColor = typeStyle.Value.color;
                    m_Icon.image = typeStyle.Value.icon;
                }
                else
                {
                    // If the icon was previously set inline by a registered type style, we need to remove it and create a new Image so that Image.m_TintColorIsInline and Image.m_ImageIsInline are reset to enable USS styling.
                    if (m_IconIsInline)
                    {
                        // Remove old icon
                        var index = Root.IndexOf(m_Icon);
                        Root.Remove(m_Icon);

                        // Create and assign new icon
                        m_Icon = new Image();
                        m_Icon.AddToClassList(m_ParentClassName.WithUssElement(GraphElementHelper.iconName));
                        Root.Insert(index, m_Icon);
                        m_IconIsInline = false;
                    }

                    m_OwnerElement.RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                }
            }
        }
    }
}
