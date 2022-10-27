// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a value editor for a <see cref="ConstantNodeModel"/>.
    /// </summary>
    class ConstantNodeEditorPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstantNodeEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="ConstantNodeEditorPart"/>.</returns>
        public static ConstantNodeEditorPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is ConstantNodeModel)
            {
                return new ConstantNodeEditorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantNodeEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected ConstantNodeEditorPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public static readonly string constantEditorElementUssClassName = "constant-editor-part";
        public static readonly string labelUssName = "constant-editor-label";

        protected Label m_Label;
        protected BaseModelPropertyField m_ConstantEditor;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ConstantNodeModel constantNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_Label = new Label { name = labelUssName };
                m_Label.AddToClassList(m_ParentClassName.WithUssElement(labelUssName));
                m_Root.Add(m_Label);

                m_ConstantEditor = InlineValueEditor.CreateEditorForConstants(
                    m_OwnerElement.RootView, new[] { constantNodeModel }, new[] { constantNodeModel.Value }, constantNodeModel.IsLocked);

                if (m_ConstantEditor != null)
                {
                    m_ConstantEditor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorElementUssClassName));
                    m_Root.Add(m_ConstantEditor);
                }
                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is ConstantNodeModel constantNodeModel)
            {
                if (constantNodeModel.Value is IStringWrapperConstantModel icm)
                {
                    if (!TokenEditorNeedsLabel)
                        m_Label.style.display = DisplayStyle.None;
                    else
                        m_Label.text = icm.Label;
                }
            }

            m_ConstantEditor?.UpdateDisplayedValue();
        }

        static TypeHandle[] s_PropsToHideLabel =
        {
            TypeHandle.Int,
            TypeHandle.Float,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            TypeHandle.String
        };

        bool TokenEditorNeedsLabel
        {
            get
            {
                if (m_Model is ConstantNodeModel constantNodeModel)
                    return !s_PropsToHideLabel.Contains(constantNodeModel.Type.GenerateTypeHandle());
                return true;
            }
        }
    }
}
