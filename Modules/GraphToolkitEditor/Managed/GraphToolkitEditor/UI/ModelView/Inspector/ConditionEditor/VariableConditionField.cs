// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An object-field-like control to choose the blackboard variable compared by a variable condition,
    /// through the item library.
    /// </summary>
    /// <remarks>
    /// This is a UI wrapper: it presents the variable as an object the user can pick, while the model only
    /// stores the variable's GUID.
    /// </remarks>
    internal class VariableConditionField : VisualElement
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public static readonly string ussClassName = "ge-variable-condition-field";

        /// <summary>
        /// The USS class name added to the variable type icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");

        /// <summary>
        /// The USS class name added to the variable name label.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName.WithUssElement("label");

        /// <summary>
        /// The USS class name added to the picker button.
        /// </summary>
        public static readonly string pickerUssClassName = ussClassName.WithUssElement("picker");

        /// <summary>
        /// The USS class name added when a compatible variable is being dragged over this field.
        /// </summary>
        public static readonly string dropHighlightUssClassName = ussClassName.WithUssModifier("drop-highlight");

        readonly RootView m_RootView;
        readonly GraphModel m_GraphModel;
        readonly Image m_Icon;
        readonly Label m_Label;
        TypeHandle m_IconType;

        /// <summary>
        /// Raised when the user chooses a variable through the picker.
        /// </summary>
        public event Action<VariableDeclarationModelBase> variableChosen;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableConditionField"/> class.
        /// </summary>
        /// <param name="rootView">The view the field belongs to.</param>
        /// <param name="graphModel">The graph whose variables can be chosen.</param>
        public VariableConditionField(RootView rootView, GraphModel graphModel)
        {
            m_RootView = rootView;
            m_GraphModel = graphModel;

            AddToClassList(ussClassName);
            focusable = true;

            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            Add(m_Icon);

            m_Label = new Label();
            m_Label.AddToClassList(labelUssClassName);
            Add(m_Label);

            var pickerButton = new VisualElement();
            pickerButton.AddToClassList(pickerUssClassName);
            pickerButton.AddManipulator(new Clickable(OpenPicker));
            Add(pickerButton);

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        /// <summary>
        /// Updates the displayed variable using the condition model's display title.
        /// </summary>
        /// <param name="model">The condition model whose variable to display.</param>
        public void SetVariable(VariableConditionModel model)
        {
            m_Label.text = model.GetDisplayTitle();
            var variable = model.Variable;

            var type = variable?.DataType ?? default;
            if (type != m_IconType)
            {
                if (m_IconType.IsValid)
                    m_RootView.TypeHandleInfos.RemoveUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, m_IconType);
                m_IconType = type;
                if (type.IsValid)
                    m_RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, type);
            }
            m_Icon.style.display = type.IsValid ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
            {
                OpenPicker();
                evt.StopPropagation();
            }
        }

        void OpenPicker()
        {
            if (m_GraphModel == null)
                return;

            var position = new Vector2(worldBound.x, worldBound.yMax);
            VariableConditionPicker.Show(m_RootView, m_GraphModel, position, variable => variableChosen?.Invoke(variable));
        }

        VariableDeclarationModelBase GetCompatibleVariable()
        {
            var dragged = SelectionDropper.GetDraggedElements();
            foreach (var model in dragged)
            {
                if (model is VariableDeclarationModelBase v && v.GraphModel == m_GraphModel)
                    return v;
            }
            return null;
        }

        void OnDragEnter(DragEnterEvent evt)
        {
            if (GetCompatibleVariable() != null)
                AddToClassList(dropHighlightUssClassName);
        }

        void OnDragLeave(DragLeaveEvent evt)
        {
            RemoveFromClassList(dropHighlightUssClassName);
        }

        void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (GetCompatibleVariable() != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.StopPropagation();
            }
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            var variable = GetCompatibleVariable();
            if (variable == null)
                return;

            DragAndDrop.AcceptDrag();
            RemoveFromClassList(dropHighlightUssClassName);
            variableChosen?.Invoke(variable);
            evt.StopPropagation();
        }

        void OnDragExited(DragExitedEvent evt)
        {
            RemoveFromClassList(dropHighlightUssClassName);
        }
    }
}
