// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A <see cref="PortConnectorPart"/> with an icon showing the port type.
    /// </summary>
    class PortConnectorWithIconPart : PortConnectorPart
    {
        /// <summary>
        /// The uss name for the icon element.
        /// </summary>
        public static readonly string iconUssName = "icon";

        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorWithIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorWithIconPart"/>.</returns>
        public new static PortConnectorWithIconPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortModel && ownerElement is Port)
            {
                return new PortConnectorWithIconPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected Image m_Icon;

        PortModel PortModel
        {
            get => m_Model as PortModel;
        }

        TypeHandle m_CurrentTypeHandle;

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
        protected PortConnectorWithIconPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            base.BuildPartUI(container);

            m_Icon = new Image();
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement(iconUssName));
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;
            Root.Insert(1, m_Icon);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            Root.AddStylesheet_Internal("PortConnectorWithIconPart.uss");

            m_Icon.AddStylesheet_Internal("TypeIcons.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;

            if (m_CurrentTypeHandle != PortModel.DataTypeHandle)
            {
                m_OwnerElement.RootView.TypeHandleInfos.RemoveUssClasses(Port.dataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                m_CurrentTypeHandle = PortModel.DataTypeHandle;
                m_OwnerElement.RootView.TypeHandleInfos.AddUssClasses(Port.dataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
            }
        }
    }
}
