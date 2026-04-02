// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Displays a property field to edit a IHasTitle model's title.
    /// </summary>
    [UnityRestricted]
    internal class GraphElementTitlePropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="GraphElementTitlePropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-graph-element-title-property-field";

        /// <summary>
        /// The USS class name added to the icon element.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement(iconName);

        /// <summary>
        /// The USS class name added to the icon element when empty.
        /// </summary>
        public static readonly string emptyIconUssClassName = iconUssClassName.WithUssModifier(GraphElementHelper.emptyUssModifier);

        /// <summary>
        /// The USS class name added to the text field element.
        /// </summary>
        public static readonly string fieldUssClassName = ussClassName.WithUssElement(fieldName);

        /// <summary>
        /// The USS class name added to the label element.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(labelName);

        Image m_Icon;

        IReadOnlyList<IHasTitle> m_Models;

        IReadOnlyList<string> m_CurrentIconClasses;

        ModelInspectorView m_ModelInspectorView;


        TextField TextField => Field as TextField;

        /// <summary>
        /// Creates an instance of <see cref="GraphElementTitlePropertyField"/>.
        /// </summary>
        /// <param name="modelInspectorView">The model inspector view.</param>
        /// <param name="models">The models. May be <see cref="IRenamable"/> as well.</param>
        public GraphElementTitlePropertyField(ModelInspectorView modelInspectorView, IReadOnlyList<IHasTitle> models)
            : base(modelInspectorView)
        {
            m_ModelInspectorView = modelInspectorView;
            this.AddPackageStylesheet("GraphElementTitlePropertyField.uss");
            m_Models = models;
            AddToClassList(ussClassName);
            m_Icon = new Image();
            Add(m_Icon);
            m_Icon.AddToClassList(iconUssClassName);

            Field = new TextField();
            TextField.isDelayed = true;
            Field.AddToClassList(fieldUssClassName);
            Add(Field);
            LabelElement = new Label();
            LabelElement.AddToClassList(labelUssClassName);
            Add(LabelElement);
            Field.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
        }

        void OnTitleChange(ChangeEvent<string> e)
        {
            if (e.newValue == mixedValueString)
                return;

            string newValue = e.newValue;
            if (newValue.Contains(mixedValueString))
                newValue = newValue.Replace(mixedValueString, string.Empty);

            var modelsToRename = new List<IRenamable>();
            for (int i = 0; i < m_Models.Count; i++)
            {
                if (m_Models[i] is GraphElementModel ge && ge.HasCapability(Capabilities.Renamable) && m_Models[i] is IRenamable renamable)
                {
                    modelsToRename.Add(renamable);
                }
                else if (m_Models[i] is IHasDeclarationModel hasDeclaration && hasDeclaration.DeclarationModel.HasCapability(Capabilities.Renamable) && hasDeclaration.DeclarationModel is IRenamable declRenamable)
                {
                    modelsToRename.Add(declRenamable);
                }
            }
            CommandTarget.Dispatch(new RenameElementsCommand(modelsToRename, newValue));
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
            bool sameTitle = true;
            string firstTitle = m_Models[0].Title;
            bool sameIcon = true;
            var firstUssClasses = m_ModelInspectorView.GetIconUssClassesForModel(m_Models[0] as GraphElementModel);
            if (firstUssClasses == null)
                sameIcon = false;

            bool allRenamable = m_Models[0] is IRenamable || m_Models[0] is IHasDeclarationModel and IRenamable;

            if (m_Models[0] is GraphElementModel firstGraphElementModel)
            {
                allRenamable = firstGraphElementModel.HasCapability(Capabilities.Renamable) || firstGraphElementModel is IHasDeclarationModel hasDeclaration && hasDeclaration.DeclarationModel.HasCapability(Capabilities.Renamable);
            }

            for (int i = 1; i < m_Models.Count && (sameTitle || sameIcon); ++i)
            {
                if (sameTitle && m_Models[i].Title != firstTitle)
                    sameTitle = false;

                if (allRenamable && (m_Models[i] is not IRenamable || m_Models[i] is IHasDeclarationModel and not IRenamable))
                    allRenamable = false;

                if (allRenamable && m_Models[i] is GraphElementModel ge && !ge.HasCapability(Capabilities.Renamable) && ge is IHasDeclarationModel hasDeclarationModel && !hasDeclarationModel.DeclarationModel.HasCapability(Capabilities.Renamable))
                    allRenamable = false;

                if (sameIcon && !ListExtensions.ListEquals(m_ModelInspectorView.GetIconUssClassesForModel(m_Models[i] as GraphElementModel), firstUssClasses))
                    sameIcon = false;
            }

            if (sameIcon)
            {
                if (!ListExtensions.ListEquals(m_CurrentIconClasses, firstUssClasses))
                {
                    if (m_CurrentIconClasses != null)
                    {
                        foreach (var ussClass in m_CurrentIconClasses)
                        {
                            m_Icon.RemoveFromClassList(ussClass);
                        }
                    }

                    m_CurrentIconClasses = firstUssClasses;

                    foreach (var ussClass in m_CurrentIconClasses)
                    {
                        m_Icon.AddToClassList(ussClass);
                    }
                }

                m_Icon.RemoveFromClassList(emptyIconUssClassName);
            }
            else
            {
                if (m_CurrentIconClasses != null)
                {
                    foreach (var ussClass in m_CurrentIconClasses)
                    {
                        m_Icon.RemoveFromClassList(ussClass);
                    }
                }

                m_Icon.AddToClassList(emptyIconUssClassName);
            }

            var model = m_Models[0] as VariableDeclarationModel;
            if (model != null)
            {
                bool overrideIcon = true;
                Type elementStyle = model.DataType.Resolve();
                (Texture2D icon, Color color)? typeStyle = model.GraphModel.GetDataTypeStyle(elementStyle);

                if (!typeStyle.HasValue && elementStyle.IsListOrArray())
                {
                    typeStyle = model.GraphModel.GetDataTypeStyle(elementStyle.GetCollectionElementType());
                    overrideIcon = false;
                }

                if (typeStyle.HasValue)
                {
                    if (overrideIcon)
                        m_Icon.image = typeStyle.Value.icon;
                    m_Icon.tintColor = typeStyle.Value.color;
                }
            }

            if (allRenamable)
            {
                if (sameTitle)
                {
                    TextField.SetValueWithoutNotify(firstTitle);
                }
                else
                {
                    TextField.SetValueWithoutNotify(mixedValueString);
                }

                if (Field.parent == null)
                    Add(Field);
                if (LabelElement.parent != null)
                    LabelElement.RemoveFromHierarchy();
            }
            else
            {
                if (sameTitle)
                {
                    LabelElement.text = firstTitle;
                }
                else
                {
                    LabelElement.text = "Graph Elements";
                }

                if (LabelElement.parent == null)
                    Add(LabelElement);
                if (Field.parent != null)
                    Field.RemoveFromHierarchy();
            }
        }
    }
}
