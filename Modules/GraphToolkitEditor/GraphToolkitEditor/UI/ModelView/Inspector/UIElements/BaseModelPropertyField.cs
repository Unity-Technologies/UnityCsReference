// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class to display a UI to edit a property or field.
    /// </summary>
    /// <remarks>
    /// 'BaseModelPropertyField' is the base class to display a UI to edit properties or fields.
    /// It is used in the graph inspector and on nodes that contain ports with embedded constants.
    /// </remarks>
    [UnityRestricted]
    internal abstract class BaseModelPropertyField : VisualElement
    {
        /// <summary>
        /// The USS class name of a <see cref="BaseModelPropertyField"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-model-property-field";

        /// <summary>
        /// The USS class name of the label of a <see cref="BaseModelPropertyField"/>.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        /// <summary>
        /// The USS class name of the input of a <see cref="BaseModelPropertyField"/>.
        /// </summary>
        public static readonly string inputUssClassName = ussClassName.WithUssElement(GraphElementHelper.inputName);

        /// <summary>
        /// The name of a <see cref="BaseModelPropertyField"/> change button.
        /// </summary>
        public static readonly string changeButtonName = "change-button";

        /// <summary>
        /// The name of a field.
        /// </summary>
        public static readonly string fieldName = "field";

        /// <summary>
        /// The name of an icon.
        /// </summary>
        public static readonly string iconName = GraphElementHelper.iconName;

        /// <summary>
        /// The name of a label.
        /// </summary>
        public static readonly string labelName = GraphElementHelper.labelName;

        /// <summary>
        /// the string to use when the field have different values (mixed values).
        /// </summary>
        public static readonly string mixedValueString = "\u2014";

        /// <summary>
        ///  The command dispatcher.
        /// </summary>
        public ICommandTarget CommandTarget { get; }

        /// <summary>
        /// The <see cref="VisualElement"/> that is the field used to change the value.
        /// </summary>
        public VisualElement Field { get; protected set; }

        /// <summary>
        /// The label, if any, of the PropertyField.
        /// </summary>
        public Label LabelElement { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomizableModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        protected BaseModelPropertyField(ICommandTarget commandTarget)
        {
            CommandTarget = commandTarget;

            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Sets the field elements and applies styles so the field look like a <see cref="PropertyField"/>.
        /// </summary>
        /// <param name="fieldElement">The field element.</param>
        /// <param name="labelElement">The label element.</param>
        /// <param name="fieldTooltip">The tooltip of the field.</param>
        protected void Setup(Label labelElement, VisualElement fieldElement, string fieldTooltip)
        {
            // Stolen from PropertyField.

            Field = fieldElement;
            LabelElement = labelElement;

            if (LabelElement == null)
                return;

            LabelElement.PreallocForMoreClasses(2);
            LabelElement.AddToClassList(labelUssClassName);
            LabelElement.tooltip = fieldTooltip;
            var baseField = Field.SafeQ(null, BaseField<string>.inputUssClassName);
            baseField?.PreallocForMoreClasses(2);
            baseField?.AddToClassList(inputUssClassName);

            // Style this like a PropertyField
            AddToClassList(PropertyField.ussClassName);
            LabelElement.AddToClassList(PropertyField.labelUssClassName);
            baseField?.AddToClassList(PropertyField.inputUssClassName);

            SetScalarLabelDragCallback();
        }


        /// <summary>
        /// Sets the callbacks to start and stop merging undoable commands when dragging the label of a scalar field.
        /// </summary>
        void SetScalarLabelDragCallback()
        {
            if (Field == null)
                return;

            void SetupLabels<T>()
            {
                Field.Query<BaseField<T>>().ForEach(t =>
                {
                    var label = t.labelElement;
                    if (label != null)
                    {
                        label.RegisterCallback<MouseCaptureEvent>(OnLabelMouseCapture);
                        label.RegisterCallback<MouseCaptureOutEvent>(OnLabelMouseRelease);
                    }
                });
            }

            SetupLabels<float>();
            SetupLabels<int>();
            SetupLabels<long>();
            SetupLabels<double>();
        }

        internal void OnLabelMouseCapture(MouseCaptureEvent e)
        {
            (CommandTarget as IUndoableCommandMerger)?.StartMergingUndoableCommands();
        }

        internal void OnLabelMouseRelease(MouseCaptureOutEvent e)
        {
            (CommandTarget as IUndoableCommandMerger)?.StopMergingUndoableCommands();
        }

        /// <summary>
        /// Updates the value displayed by the custom UI.
        /// </summary>
        public abstract void UpdateDisplayedValue();
    }
}
