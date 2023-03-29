// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a value editor for a port.
    /// </summary>
    class PortConstantEditorPart : BaseModelViewPart
    {
        public static readonly string constantEditorUssName = "constant-editor";

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConstantEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConstantEditorPart"/>.</returns>
        public static PortConstantEditorPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortModel)
            {
                return new PortConstantEditorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected BaseModelPropertyField m_Editor;

        Type m_EditorDataType;
        bool m_IsConnected;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// The <see cref="BaseFieldMouseDragger"/> that can be used to change the value of the port.
        /// </summary>
        public BaseFieldMouseDragger Dragger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConstantEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConstantEditorPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <summary>
        /// Determines whether a constant editor should be displayed for a port.
        /// </summary>
        /// <returns>True if there should be a port constant editor for the port.</returns>
        protected virtual bool PortWantsEditor()
        {
            var portModel = m_Model as PortModel;
            var isPortal = portModel?.NodeModel is WirePortalModel;
            return portModel != null && portModel.Direction == PortDirection.Input && !isPortal;
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is PortModel portModel && PortWantsEditor())
            {
                InitRoot(container);
                if (portModel.EmbeddedValue != null)
                    BuildConstantEditor();
            }
        }

        void InitRoot(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            BuildConstantEditor();
            m_Editor?.UpdateDisplayedValue();
        }

        protected void BuildConstantEditor()
        {
            if (m_Model is PortModel portModel && PortWantsEditor())
            {
                // Rebuild editor if port data type changed.
                if (m_Editor != null && (portModel.EmbeddedValue?.Type != m_EditorDataType || portModel.IsConnected() != m_IsConnected))
                {
                    m_Editor.RemoveFromHierarchy();
                    m_Editor = null;
                    Dragger = null;
                }

                if (m_Editor == null)
                {
                    if (portModel.Direction == PortDirection.Input && portModel.EmbeddedValue != null)
                    {
                        m_EditorDataType = portModel.EmbeddedValue.Type;
                        m_IsConnected = portModel.IsConnected();
                        m_Editor = InlineValueEditor.CreateEditorForConstants(
                            m_OwnerElement.RootView, new[] { portModel }, new[] { portModel.EmbeddedValue }, false);

                        if(portModel.DataTypeHandle == TypeHandle.Float)
                        {
                            Dragger = new FieldMouseDragger<float>((IValueField<float>)m_Editor.Field);
                        }
                        else if(portModel.DataTypeHandle == TypeHandle.Double)
                        {
                            Dragger = new FieldMouseDragger<double>((IValueField<double>)m_Editor.Field);
                        }
                        else if(portModel.DataTypeHandle == TypeHandle.Int)
                        {
                            Dragger = new FieldMouseDragger<int>((IValueField<int>)m_Editor.Field);
                        }
                        else if(portModel.DataTypeHandle == TypeHandle.Long)
                        {
                            Dragger = new FieldMouseDragger<long>((IValueField<long>)m_Editor.Field);
                        }
                        else if(portModel.DataTypeHandle == TypeHandle.UInt)
                        {
                            Dragger = new FieldMouseDragger<uint>((IValueField<uint>)m_Editor.Field);
                        }

                        if (m_Editor != null)
                        {
                            m_Editor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorUssName));
                            m_Root.Add(m_Editor);
                        }
                    }
                }
            }
        }
    }
}
