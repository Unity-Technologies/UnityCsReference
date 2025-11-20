// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardField : BlackboardElement
    {
        /// <summary>
        /// The USS class name added to <see cref="BlackboardField"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-blackboard-field";

        /// <summary>
        /// The USS class name added to the capsule.
        /// </summary>
        public static readonly string capsuleUssClassName = ussClassName.WithUssElement(CapsuleNodeView.capsuleName);

        /// <summary>
        /// The USS class name added to the labels.
        /// </summary>
        public static readonly string labelsElementUssClassName = ussClassName.WithUssElement("labels");

        /// <summary>
        /// The USS class name added to the name label.
        /// </summary>
        public static readonly string nameLabelUssClassName = ussClassName.WithUssElement("name-label");

        /// <summary>
        /// The USS class name added to the icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class name added to the highlighted modifier.
        /// </summary>
        public static readonly string highlightedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.highlightedUssModifier);

        /// <summary>
        /// The USS class name added to the placeholder modifier.
        /// </summary>
        public static readonly string placeholderUssClassName = ussClassName.WithUssModifier(GraphElementHelper.placeholderUssModifier);

        /// <summary>
        /// The USS class name added to the read only modifier.
        /// </summary>
        public static readonly string readOnlyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.readOnlyUssModifier);

        /// <summary>
        /// The USS class name added to the write only modifier.
        /// </summary>
        public static readonly string writeOnlyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.writeOnlyUssModifier);

        /// <summary>
        /// The USS class name added to the collapse button.
        /// </summary>
        public static readonly string collapseButtonUssClassName = ussClassName.WithUssElement("collapse-button");

        /// <summary>
        /// The USS class name added to the scope image in the capsule.
        /// </summary>
        public static readonly string scopeImageUssClassName = ussClassName.WithUssElement("scope-image");

        /// <summary>
        /// The expanded uss modifier class.
        /// </summary>
        public static readonly string expandedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.expandedUssModifier);

        /// <summary>
        /// The USS class name added to externally defined variables.
        /// </summary>
        public static readonly string externalUssClassName = ussClassName.WithUssModifier("external");

        static readonly CustomStyleProperty<Color> k_BorderColorProperty = new("--custom-border-color");
        static readonly CustomStyleProperty<Color> k_DefaultIconColorProperty = new("--graph-color-icon-default");
        static readonly CustomStyleProperty<Color> k_IconColorProperty = new("--unity-image-tint-color");
        static readonly CustomStyleProperty<Color> k_ScopeImageColorProperty = new("--scope-image-color");

        /// <summary>
        /// The element representing the variable capsule.
        /// </summary>
        protected VisualElement m_Capsule;

        /// <summary>
        /// The the variable open/collapse button.
        /// </summary>
        protected CollapseButton m_CollapseButton;

        /// <summary>
        /// The variable property view.
        /// </summary>
        protected BlackboardVariablePropertyView m_PropertyView;

        VisualElement m_SelectionBorder;
        VariableScopeImage m_ScopeImage;

        Color m_SelectionBorderColor;

        bool m_WasSelected;
        bool m_WasHighlighted;
        bool m_WasExpanded;
        TypeHandle m_CurrentTypeHandle;

        /// <summary>
        /// The element containing the icon.
        /// </summary>
        protected Image m_Icon;

        /// <summary>
        /// The <see cref="EditableLabel"/> containing the name of the field.
        /// </summary>
        public VisualElement NameLabel { get; protected set; }

        /// <summary>
        /// Whether the variable is expanded and therefore shows its properties in the blackboard.
        /// </summary>
        public bool Expanded => Model is VariableDeclarationModelBase vdm && (BlackboardView.BlackboardRootViewModel.ViewState?.GetVariableDeclarationModelExpanded(vdm) ?? false);

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardField"/> class.
        /// </summary>
        public BlackboardField()
        {
        }

        internal bool IsHighlighted() => ClassListContains(highlightedUssClassName);

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            m_Capsule = new VisualElement();
            m_Capsule.AddToClassList(capsuleUssClassName);
            m_Capsule.pickingMode = PickingMode.Ignore;
            Add(m_Capsule);

            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            m_Icon.pickingMode = PickingMode.Ignore;
            m_Capsule.Add(m_Icon);

            if (Model is GraphElementModel graphElementModel && graphElementModel.IsRenamable())
            {
                NameLabel = new EditableLabel { name = "name", EditActionName = "Rename"};
            }
            else
            {
                NameLabel = new Label { name = "name" };
            }
            NameLabel.AddToClassList(nameLabelUssClassName);

            m_Capsule.Add(NameLabel);

            m_ScopeImage = new VariableScopeImage();
            m_ScopeImage.AddToClassList(scopeImageUssClassName);
            m_ScopeImage.RegisterCallback<CustomStyleResolvedEvent>(ScopeImageStyleResolved);

            m_Capsule.Add(m_ScopeImage);

            m_SelectionBorder = CreateSelectionBorder();
            Add(m_SelectionBorder);
            m_SelectionBorder.generateVisualContent += GenerateBorderVisualContent;
            m_SelectionBorder.RegisterCallback<CustomStyleResolvedEvent>(OnBorderStyleResolved);

            RegisterCallback<MouseEnterEvent>(_ =>
            {
                m_SelectionBorder.MarkDirtyRepaint();
            });

            RegisterCallback<MouseLeaveEvent>(_ =>
            {
                m_SelectionBorder.MarkDirtyRepaint();
            });
            m_Capsule.RegisterCallback<GeometryChangedEvent>(_ => m_SelectionBorder.MarkDirtyRepaint());
            pickingMode = PickingMode.Ignore;
        }

        void ScopeImageStyleResolved(CustomStyleResolvedEvent e)
        {
            if (!e.customStyle.TryGetValue(k_ScopeImageColorProperty, out var tintColor))
                if (!e.customStyle.TryGetValue(k_IconColorProperty, out tintColor))
                    e.customStyle.TryGetValue(k_DefaultIconColorProperty, out tintColor);

            m_ScopeImage.Color = tintColor;
        }

        void OnBorderStyleResolved(CustomStyleResolvedEvent e)
        {
            bool changed = false;
            if (e.customStyle.TryGetValue(k_BorderColorProperty, out Color colorValue) && colorValue != m_SelectionBorderColor)
            {
                m_SelectionBorderColor = colorValue;
                changed = true;
            }

            if (changed)
                m_SelectionBorder.MarkDirtyRepaint();
        }

        void GenerateBorderVisualContent(MeshGenerationContext mgc)
        {
            if (!this.hasHoverPseudoState && !IsSelected() && !IsHighlighted())
                return;

            var capsuleRect = m_Capsule.parent.ChangeCoordinatesTo(m_SelectionBorder, m_Capsule.localBound);

            var color = m_SelectionBorderColor;
            var painter2D = mgc.painter2D;

            painter2D.strokeColor = color;

            float lineWidth = this.hasHoverPseudoState && IsSelected() ? 2 : 1;
            painter2D.lineWidth = lineWidth;

            capsuleRect.x -= lineWidth * 0.5f;
            capsuleRect.y -= lineWidth * 0.5f;
            capsuleRect.width += lineWidth;
            capsuleRect.height += lineWidth;

            painter2D.BeginPath();

            float topLeftRadius = m_Capsule.resolvedStyle.borderTopLeftRadius;
            if (topLeftRadius > 0)
                painter2D.MoveTo(new Vector2(capsuleRect.x + topLeftRadius + lineWidth, capsuleRect.y));
            else
                painter2D.MoveTo(capsuleRect.position);

            float topRightRadius = m_Capsule.resolvedStyle.borderTopRightRadius;

            if (topRightRadius > 0)
            {
                topRightRadius += lineWidth;
                painter2D.ArcTo(new Vector2(capsuleRect.xMax, capsuleRect.y),
                    new Vector2(capsuleRect.xMax , capsuleRect.y + topRightRadius),
                    topRightRadius);
            }
            else
            {
                painter2D.LineTo(new Vector2(capsuleRect.xMax, capsuleRect.y));
            }

            if (m_PropertyView != null && Expanded)
            {
                var propertyRect = m_PropertyView.parent.ChangeCoordinatesTo(m_SelectionBorder, m_PropertyView.localBound);

                propertyRect.x -= lineWidth * 0.5f;
                propertyRect.y -= lineWidth * 0.5f;
                propertyRect.width += lineWidth;
                propertyRect.height += lineWidth;

                painter2D.LineTo(new Vector2(capsuleRect.xMax, propertyRect.yMin));

                float propTopRightRadius = m_PropertyView.resolvedStyle.borderTopRightRadius;
                if (propTopRightRadius > 0 && capsuleRect.xMax < propertyRect.xMax - propTopRightRadius)
                {
                    propTopRightRadius += lineWidth;
                    painter2D.ArcTo(new Vector2(propertyRect.xMax, propertyRect.yMin),
                        new Vector2(propertyRect.xMax, propertyRect.yMin + propTopRightRadius),
                        propTopRightRadius);
                }
                else
                {
                    painter2D.LineTo(new Vector2(propertyRect.xMax, propertyRect.yMin));
                }

                float propBottomRightRadius = m_PropertyView.resolvedStyle.borderBottomRightRadius;
                if (propBottomRightRadius > 0)
                {
                    propBottomRightRadius += lineWidth;
                    painter2D.ArcTo(new Vector2(propertyRect.xMax, propertyRect.yMax),
                        new Vector2(propertyRect.xMax - propBottomRightRadius, propertyRect.yMax),
                        propBottomRightRadius);
                }
                else
                {
                    painter2D.LineTo(new Vector2(propertyRect.xMax, propertyRect.yMax));
                }

                float propBottomLeftRadius = m_PropertyView.resolvedStyle.borderBottomLeftRadius;
                if (propBottomLeftRadius > 0)
                {
                    propBottomLeftRadius += lineWidth;
                    painter2D.ArcTo(new Vector2(capsuleRect.x, propertyRect.yMax),
                        new Vector2(capsuleRect.x, propertyRect.yMax - propBottomLeftRadius),
                        propBottomLeftRadius);
                }
                else
                {
                    painter2D.LineTo(new Vector2(capsuleRect.x, propertyRect.yMax));
                }
            }
            else
            {
                float bottomRightRadius = m_Capsule.resolvedStyle.borderBottomRightRadius;
                if (bottomRightRadius > 0)
                {
                    bottomRightRadius += lineWidth;
                    //painter2D.LineTo(new Vector2(capsuleRect.xMax, capsuleRect.yMax));
                    painter2D.ArcTo(new Vector2(capsuleRect.xMax, capsuleRect.yMax),
                        new Vector2(capsuleRect.xMax - bottomRightRadius , capsuleRect.yMax),
                        bottomRightRadius);
                }
                else
                {
                    painter2D.LineTo(new Vector2(capsuleRect.xMax, capsuleRect.yMax));
                }

                float bottomLeftRadius = m_Capsule.resolvedStyle.borderBottomLeftRadius;
                if (bottomLeftRadius > 0)
                {
                    bottomLeftRadius += lineWidth;
                    painter2D.ArcTo(new Vector2(capsuleRect.x, capsuleRect.yMax),
                        new Vector2(capsuleRect.x  , capsuleRect.yMax - bottomLeftRadius),
                        bottomLeftRadius);
                }
                else
                {
                    painter2D.LineTo(new Vector2(capsuleRect.x, capsuleRect.yMax));
                }
            }

            if (topLeftRadius > 0)
            {
                topLeftRadius += lineWidth;
                painter2D.ArcTo(capsuleRect.position,
                    new Vector2(capsuleRect.x + topLeftRadius, capsuleRect.y),
                    topLeftRadius);
            }
            painter2D.ClosePath();
            painter2D.Stroke();
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            EnableInClassList(placeholderUssClassName, Model is VariableDeclarationModelBase and IPlaceholder);
            AddToClassList(ussClassName);

            if (Model is VariableDeclarationModelBase vdm && vdm is ExternalVariableDeclarationModelBase)
            {
                AddToClassList(externalUssClassName);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (Model is VariableDeclarationModelBase variableDeclarationModel)
            {
                if (visitor.ChangeHints.HasChange(ChangeHint.Data))
                {
                    m_ScopeImage.Scope = variableDeclarationModel.Scope;
                    m_ScopeImage.ReadWriteModifiers = variableDeclarationModel.Modifiers;

                    if (m_CurrentTypeHandle != variableDeclarationModel.DataType)
                    {
                        RootView.TypeHandleInfos.RemoveUssClasses(VariableScopeImage.scopeImageDataTypeClassNamePrefix, m_ScopeImage, m_CurrentTypeHandle);
                        RootView.TypeHandleInfos.RemoveUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                        m_CurrentTypeHandle = variableDeclarationModel.DataType;
                        RootView.TypeHandleInfos.AddUssClasses(VariableScopeImage.scopeImageDataTypeClassNamePrefix, m_ScopeImage, m_CurrentTypeHandle);
                        RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                    }

                    switch (NameLabel)
                    {
                        case EditableLabel editableLabel:
                            editableLabel.SetValueWithoutNotify(variableDeclarationModel.Title);
                            break;
                        case Label label:
                            label.text = variableDeclarationModel.Title;
                            break;
                    }

                    EnableInClassList(readOnlyUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Read) != 0);
                    EnableInClassList(writeOnlyUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Write) != 0);
                }

                bool borderDirty = false;
                bool isSelected = IsSelected();
                if (m_WasSelected != isSelected)
                {
                    m_WasSelected = isSelected;
                    borderDirty = true;
                }

                var highlight = BlackboardView.GraphTool.HighlighterState.GetDeclarationModelHighlighted(variableDeclarationModel);

                // Do not show highlight if selected.
                if (highlight && BlackboardView.BlackboardRootViewModel.SelectionState.IsSelected(variableDeclarationModel))
                    highlight = false;

                EnableInClassList(highlightedUssClassName, highlight);

                if (m_WasHighlighted != highlight)
                {
                    m_WasHighlighted = highlight;
                    borderDirty = true;
                }

                bool isExpanded = Expanded;
                EnableInClassList(expandedUssClassName, isExpanded);
                m_CollapseButton?.SetValueWithoutNotify(!isExpanded);

                if (m_WasExpanded != isExpanded)
                {
                    m_WasExpanded = isExpanded;
                    borderDirty = true;
                }

                if (borderDirty)
                    m_SelectionBorder.MarkDirtyRepaint();

                if (visitor.ChangeHints.HasChange(ChangeHint.Style))
                {
                    tooltip = (Model as VariableDeclarationModelBase)?.Tooltip ?? string.Empty;
                }
            }
        }

        /// <inheritdoc />
        public override void ActivateRename()
        {
            if (NameLabel is EditableLabel editableLabel)
            {
                GetFirstAncestorOfType<Blackboard>()?.ScrollToMakeVisible(this);
                editableLabel.BeginEditing();
            }
        }

        internal void SetBBVariablePropertyView()
        {
            m_SelectionBorder.BringToFront();
            foreach (var child in Children())
            {
                if (child is BlackboardVariablePropertyView prop)
                {
                    m_PropertyView = prop;
                    break;
                }
            }

            // If there are properties, there should be a collapse button.
            if (m_PropertyView?.PartList.GetPart(BlackboardVariablePropertyView.inspectorPartName) is FieldsInspector inspectorField && !inspectorField.IsEmpty)
                AddCollapseButton();
        }

        protected void AddCollapseButton()
        {
            m_CollapseButton = new CollapseButton(CollapseButton.collapseButtonName, OnCollapseButtonChange);
            m_CollapseButton.AddToClassList(collapseButtonUssClassName);
            m_Capsule.Add(m_CollapseButton);
            m_CollapseButton.SetValueWithoutNotify(!Expanded);
        }

        protected void OnCollapseButtonChange(ChangeEvent<bool> e)
        {
            if (Model is VariableDeclarationModelBase vdm)
            {
                RootView.Dispatch(new ExpandVariableDeclarationCommand(vdm, !e.newValue));
            }
        }
    }
}
