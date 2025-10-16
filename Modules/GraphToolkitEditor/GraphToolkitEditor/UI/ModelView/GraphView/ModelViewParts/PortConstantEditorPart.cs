// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for a value editor for a port.
    /// </summary>
    [UnityRestricted]
    internal class PortConstantEditorPart : BaseModelViewPart
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
        public static PortConstantEditorPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
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
        VisualElement m_DragZone;

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
        protected PortConstantEditorPart(string name, Model model, ChildView ownerElement, string parentClassName)
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

        /// <summary>
        /// Define which element controls the drag zone
        /// </summary>
        public void SetDragZone(VisualElement dragZone)
        {
            if (dragZone != m_DragZone)
            {
                m_DragZone = dragZone;
                Dragger?.SetDragZone(m_DragZone);
                m_DragZone?.EnableInClassList(FloatField.labelDraggerVariantUssClassName, Dragger != null);
            }
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
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
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            var uiHintsChanged = visitor.ChangeHints.HasChange(ChangeHint.UIHints);
            BuildConstantEditor(uiHintsChanged);

            // Hide the editor if the port is connected.
            if (m_Editor != null)
            {
                m_Editor.UpdateDisplayedValue();

                if (m_Model is PortModel portModel)
                {
                    var ancestorIsConnected = false;
                    bool allSubPortsConnected = false;
                    var hideEditor = (portModel.GraphModel?.HideConnectedPortsEditor ?? true) && portModel.IsConnected();
                    if (!hideEditor && m_Editor.Field != null)
                    {
                        var parent = portModel.ParentPort;
                        while (parent != null)
                        {
                            if (parent.IsConnected())
                            {
                                ancestorIsConnected = true;
                                break;
                            }
                            parent = parent.ParentPort;
                        }

                        bool subPortHandled = false;
                        if (portModel.SubPorts.Count > 0)
                        {
                            if (m_Editor is ConstantField constantField)
                                subPortHandled = constantField.HandleEnabledStateWithWiredSubPorts();

                            // If it was not possible to specifically disable sub-field editors, check if all sub ports are connected and enable/disable the entire field if all sub ports are connected.
                            // It is better to leave the field enabled if at least one sub port is not connected because the user might want to change the value for that sub-field.
                            if (!subPortHandled)
                            {
                                allSubPortsConnected = true;
                                foreach (var subPort in portModel.SubPorts)
                                {
                                    if (!subPort.IsConnected())
                                    {
                                        allSubPortsConnected = false;
                                        break;
                                    }
                                }
                            }
                        }
                        m_Editor.Field.SetEnabled(!ancestorIsConnected && !allSubPortsConnected);
                        if (m_Editor.Field is IMixedValueSupport mixedValueSupport)
                            mixedValueSupport.showMixedValue = ancestorIsConnected || allSubPortsConnected;
                    }
                    m_Editor.style.display = hideEditor ? DisplayStyle.None : StyleKeyword.Null;
                }

            }
        }

        protected void BuildConstantEditor(bool uiHintsChanged = false)
        {
            if (m_Model is PortModel portModel && PortWantsEditor())
            {
                var isConnected = portModel.IsConnected();
                var embeddedValue = portModel.EmbeddedValue;
                var valueType = embeddedValue?.Type;

                // Rebuild editor if port data type changed.
                if (m_Editor != null && (uiHintsChanged || valueType != m_EditorDataType || isConnected != m_IsConnected || isConnected))
                {
                    if (Dragger != null)
                    {
                        m_DragZone.UnregisterCallback<MouseCaptureEvent>(m_Editor.OnLabelMouseCapture);
                        m_DragZone.UnregisterCallback<MouseCaptureOutEvent>(m_Editor.OnLabelMouseRelease);
                    }
                    m_Editor.RemoveFromHierarchy();
                    m_Editor = null;
                    Dragger = null;
                }

                m_IsConnected = isConnected;

                if (m_Editor == null && !m_IsConnected)
                {
                    if (portModel.Direction == PortDirection.Input && portModel.EmbeddedValue != null)
                    {
                        m_EditorDataType = portModel.EmbeddedValue.Type;
                        m_Editor = InlineValueEditor.CreateEditorForConstants(
                            m_OwnerElement.RootView, new[] { portModel }, new[] { portModel.EmbeddedValue });

                        if (portModel.DataTypeHandle == TypeHandle.Float && m_Editor.Field is IValueField<float> floatField)
                        {
                            Dragger = new FieldMouseDragger<float>(floatField);
                        }
                        else if (portModel.DataTypeHandle == TypeHandle.Double && m_Editor.Field is IValueField<double> doubleField)
                        {
                            Dragger = new FieldMouseDragger<double>(doubleField);
                        }
                        else if (portModel.DataTypeHandle == TypeHandle.Int && m_Editor.Field is IValueField<int> intField)
                        {
                            Dragger = new FieldMouseDragger<int>(intField);
                        }
                        else if (portModel.DataTypeHandle == TypeHandle.Long && m_Editor.Field is IValueField<long> longField)
                        {
                            Dragger = new FieldMouseDragger<long>(longField);
                        }
                        else if (portModel.DataTypeHandle == TypeHandle.UInt && m_Editor.Field is IValueField<uint> uintField)
                        {
                            Dragger = new FieldMouseDragger<uint>(uintField);
                        }

                        if (m_Editor != null)
                        {
                            m_Editor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorUssName));
                            m_Root.Add(m_Editor);

                            if (m_DragZone != null && Dragger != null)
                            {
                                m_DragZone.RegisterCallback<MouseCaptureEvent>(m_Editor.OnLabelMouseCapture);
                                m_DragZone.RegisterCallback<MouseCaptureOutEvent>(m_Editor.OnLabelMouseRelease);

                            }
                        }

                        if (m_DragZone != null)
                        {
                            Dragger?.SetDragZone(m_DragZone);
                            m_DragZone.EnableInClassList(FloatField.labelDraggerVariantUssClassName, Dragger != null);
                        }
                    }
                }
            }
        }
    }
}
