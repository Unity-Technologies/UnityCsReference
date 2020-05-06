using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements.Debugger
{
    internal class BoxModelView : VisualElement
    {
        enum BoxType
        {
            Margin,
            Border,
            Padding,
            Content
        }

        enum SideChanged
        {
            Left,
            Right,
            Top,
            Bottom
        }
        private HighlightOverlayPainter m_OverlayPainter;

        private VisualElement m_Container;
        private VisualElement m_Layer1;
        private VisualElement m_Layer2;
        private BoxModelElement m_MarginBox;
        private BoxModelElement m_BorderBox;
        private BoxModelElement m_PaddingBox;
        private ContentBox m_ContentBox;

        private VisualElement m_TopTextFieldMarginContainer = new VisualElement();
        private VisualElement m_TopTextFieldBorderContainer = new VisualElement();
        private VisualElement m_TopTextFieldPaddingContainer = new VisualElement();
        private VisualElement m_BottomTextFieldMarginContainer = new VisualElement();
        private VisualElement m_BottomTextFieldBorderContainer = new VisualElement();
        private VisualElement m_BottomTextFieldPaddingContainer = new VisualElement();
        private VisualElement m_ContentSpacer = new VisualElement();

        private VisualElement m_SelectedElement;
        public VisualElement selectedElement
        {
            get { return m_SelectedElement; }
            set { SelectElement(value); }
        }

        public BoxModelView()
        {
            AddToClassList("box-model-view");
            visible = false;

            m_OverlayPainter = new HighlightOverlayPainter();

            m_Container = new VisualElement();
            m_Container.AddToClassList("box-model-view-container");

            m_Layer1 = new VisualElement() { name = "BoxModelViewLayer1"};
            m_Layer2 = new VisualElement() { name = "BoxModelViewLayer2"};
            m_Layer2.style.position = Position.Absolute;
            m_Layer2.pickingMode = PickingMode.Ignore;
            m_Layer2.StretchToParentSize();

            m_TopTextFieldMarginContainer.pickingMode = PickingMode.Ignore;
            m_TopTextFieldMarginContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_BottomTextFieldMarginContainer.pickingMode = PickingMode.Ignore;
            m_BottomTextFieldMarginContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_TopTextFieldBorderContainer.pickingMode = PickingMode.Ignore;
            m_TopTextFieldBorderContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_BottomTextFieldBorderContainer.pickingMode = PickingMode.Ignore;
            m_BottomTextFieldBorderContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_TopTextFieldPaddingContainer.pickingMode = PickingMode.Ignore;
            m_TopTextFieldPaddingContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_BottomTextFieldPaddingContainer.pickingMode = PickingMode.Ignore;
            m_BottomTextFieldPaddingContainer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_ContentSpacer.pickingMode = PickingMode.Ignore;
            m_ContentSpacer.AddToClassList("box-model-textfield-top-bottom-spacer");

            m_Layer2.Add(m_TopTextFieldMarginContainer);
            m_Layer2.Add(m_TopTextFieldBorderContainer);
            m_Layer2.Add(m_TopTextFieldPaddingContainer);
            m_Layer2.Add(m_ContentSpacer);
            m_Layer2.Add(m_BottomTextFieldPaddingContainer);
            m_Layer2.Add(m_BottomTextFieldBorderContainer);
            m_Layer2.Add(m_BottomTextFieldMarginContainer);

            m_ContentBox = new ContentBox();
            m_ContentBox.AddToClassList("box-model");
            m_ContentBox.AddToClassList("box-model-container-content");
            m_ContentBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Content);

            m_PaddingBox = new BoxModelElement(BoxType.Padding, m_ContentBox,
                m_TopTextFieldPaddingContainer, m_BottomTextFieldPaddingContainer);
            m_PaddingBox.AddToClassList("box-model");
            m_PaddingBox.AddToClassList("box-model-container-padding");
            m_PaddingBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Padding);

            m_BorderBox = new BoxModelElement(BoxType.Border, m_PaddingBox,
                m_TopTextFieldBorderContainer, m_BottomTextFieldBorderContainer);
            m_BorderBox.AddToClassList("box-model");
            m_BorderBox.AddToClassList("box-model-container-border");
            m_BorderBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Border);

            m_MarginBox = new BoxModelElement(BoxType.Margin, m_BorderBox,
                m_TopTextFieldMarginContainer, m_BottomTextFieldMarginContainer);
            m_MarginBox.AddToClassList("box-model");
            m_MarginBox.AddToClassList("box-model-container-margin");
            m_MarginBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Margin);

            m_Layer1.Add(m_MarginBox);

            m_Container.Add(m_Layer1);
            m_Container.Add(m_Layer2);

            var spacerLeft = new VisualElement() { style = { flexGrow = 1 } };
            var spacerRight = new VisualElement() { style = { flexGrow = 1 } };
            Add(spacerLeft);
            Add(m_Container);
            Add(spacerRight);

            RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        public void Refresh(MeshGenerationContext mgc)
        {
            m_OverlayPainter.Draw(mgc);

            m_MarginBox.SyncValues();
            m_BorderBox.SyncValues();
            m_PaddingBox.SyncValues();
            m_ContentBox.SyncValues();
        }

        private void SelectElement(VisualElement ve)
        {
            m_SelectedElement = ve;
            visible = m_SelectedElement != null;

            m_MarginBox.SelectElement(ve);
            m_BorderBox.SelectElement(ve);
            m_PaddingBox.SelectElement(ve);
            m_ContentBox.SelectElement(ve);
        }

        private void OnMouseOver(MouseOverEvent evt, BoxType boxType)
        {
            if (m_SelectedElement == null)
                return;

            m_OverlayPainter.ClearOverlay();
            switch (boxType)
            {
                case BoxType.Margin:
                    m_OverlayPainter.AddOverlay(m_SelectedElement, OverlayContent.Margin);
                    m_MarginBox.RemoveFromClassList("box-model-white");
                    m_BorderBox.AddToClassList("box-model-white");
                    m_PaddingBox.AddToClassList("box-model-white");
                    m_ContentBox.AddToClassList("box-model-white");
                    break;
                case BoxType.Border:
                    m_OverlayPainter.AddOverlay(m_SelectedElement, OverlayContent.Border);
                    m_BorderBox.RemoveFromClassList("box-model-white");
                    m_MarginBox.AddToClassList("box-model-white");
                    m_PaddingBox.AddToClassList("box-model-white");
                    m_ContentBox.AddToClassList("box-model-white");
                    break;
                case BoxType.Padding:
                    m_OverlayPainter.AddOverlay(m_SelectedElement, OverlayContent.Padding);
                    m_MarginBox.AddToClassList("box-model-white");
                    m_PaddingBox.RemoveFromClassList("box-model-white");
                    m_BorderBox.AddToClassList("box-model-white");
                    m_ContentBox.AddToClassList("box-model-white");
                    break;
                case BoxType.Content:
                    m_OverlayPainter.AddOverlay(m_SelectedElement, OverlayContent.Content);
                    m_ContentBox.RemoveFromClassList("box-model-white");
                    m_MarginBox.AddToClassList("box-model-white");
                    m_PaddingBox.AddToClassList("box-model-white");
                    m_BorderBox.AddToClassList("box-model-white");
                    break;
            }
            m_SelectedElement.panel?.visualTree.MarkDirtyRepaint();

            evt.StopPropagation();
        }

        private void OnMouseOut(MouseOutEvent evt)
        {
            m_OverlayPainter.ClearOverlay();
            m_MarginBox.RemoveFromClassList("box-model-white");
            m_BorderBox.RemoveFromClassList("box-model-white");
            m_PaddingBox.RemoveFromClassList("box-model-white");
            m_ContentBox.RemoveFromClassList("box-model-white");

            m_SelectedElement.panel?.visualTree.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        class BoxModelElement : VisualElement
        {
            private Label m_Title;
            private VisualElement m_Left;
            private VisualElement m_Right;
            private VisualElement m_Center;
            private VisualElement m_CenterTop;
            private VisualElement m_CenterContent;
            private VisualElement m_CenterBottom;

            private IntegerField m_LeftTextField;
            private IntegerField m_RightTextField;
            private IntegerField m_TopTextField;
            private IntegerField m_BottomTextField;

            // For layout purpose since real TextField are in another layer
            private IntegerField m_FakeTopTextField;
            private IntegerField m_FakeBottomTextField;
            private VisualElement m_SelectedElement;

            public BoxModelElement(BoxType boxType, VisualElement content,
                                   VisualElement topTextFieldContainer, VisualElement bottomTextFieldContainer)
            {
                string title = "";
                switch (boxType)
                {
                    case BoxType.Margin:
                        title = "margin";
                        break;
                    case BoxType.Border:
                        title = "border";
                        break;
                    case BoxType.Padding:
                        title = "padding";
                        break;
                }

                this.boxType = boxType;

                m_Title = new Label(title);
                m_Title.AddToClassList("box-model-title");

                m_CenterContent = content;
                m_CenterContent.AddToClassList("box-model-center-content");

                m_LeftTextField = new IntegerField();
                m_LeftTextField.AddToClassList("box-model-textfield");
                m_LeftTextField.RegisterValueChangedCallback(e => OnTextFieldValueChanged(e, SideChanged.Left));

                m_RightTextField = new IntegerField();
                m_RightTextField.AddToClassList("box-model-textfield");
                m_RightTextField.RegisterValueChangedCallback(e => OnTextFieldValueChanged(e, SideChanged.Right));

                m_TopTextField = new IntegerField();
                m_TopTextField.AddToClassList("box-model-textfield");
                m_TopTextField.RegisterValueChangedCallback(e => OnTextFieldValueChanged(e, SideChanged.Top));

                m_FakeTopTextField = new IntegerField();
                m_FakeTopTextField.AddToClassList("box-model-textfield");
                m_FakeTopTextField.visible = false;

                m_BottomTextField = new IntegerField();
                m_BottomTextField.AddToClassList("box-model-textfield");
                m_BottomTextField.RegisterValueChangedCallback(e => OnTextFieldValueChanged(e, SideChanged.Bottom));

                m_FakeBottomTextField = new IntegerField();
                m_FakeBottomTextField.AddToClassList("box-model-textfield");
                m_FakeBottomTextField.visible = false;

                m_Left = new VisualElement();
                m_Left.AddToClassList("box-model-side");
                m_Left.Add(m_LeftTextField);

                m_Right = new VisualElement();
                m_Right.AddToClassList("box-model-side");
                m_Right.Add(m_RightTextField);

                m_Center = new VisualElement();
                m_Center.AddToClassList("box-model-center");

                m_CenterTop = new VisualElement();
                m_CenterTop.AddToClassList("box-model-center-top");
                m_CenterTop.Add(m_FakeTopTextField);
                topTextFieldContainer.Add(m_TopTextField);

                m_CenterBottom = new VisualElement();
                m_CenterBottom.AddToClassList("box-model-center-bottom");
                m_CenterBottom.Add(m_FakeBottomTextField);
                bottomTextFieldContainer.Add(m_BottomTextField);

                m_Center.Add(m_CenterTop);
                m_Center.Add(m_CenterContent);
                m_Center.Add(m_CenterBottom);

                Add(m_Title);
                Add(m_Left);
                Add(m_Center);
                Add(m_Right);

                // Sync styles values
                schedule.Execute(SyncValues).Every(100);
            }

            public BoxType boxType { get; private set; }

            public void SelectElement(VisualElement ve)
            {
                m_SelectedElement = ve;
                InitStyleValues();
            }

            private void InitStyleValues()
            {
                if (m_SelectedElement != null)
                {
                    switch (boxType)
                    {
                        case BoxType.Margin:
                            InitOnMargins();
                            break;
                        case BoxType.Border:
                            InitOnBorders();
                            break;
                        case BoxType.Padding:
                            InitOnPaddings();
                            break;
                    }
                }
                else
                {
                    m_LeftTextField.SetValueWithoutNotify(0);
                    m_RightTextField.SetValueWithoutNotify(0);
                    m_TopTextField.SetValueWithoutNotify(0);
                    m_BottomTextField.SetValueWithoutNotify(0);
                }
            }

            private void InitOnMargins()
            {
                var selectedStyle = m_SelectedElement.resolvedStyle;

                var value = (int)selectedStyle.marginLeft;
                if (m_LeftTextField.value != value)
                    m_LeftTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.marginRight;
                if (m_RightTextField.value != value)
                    m_RightTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.marginTop;
                if (m_TopTextField.value != value)
                    m_TopTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.marginBottom;
                if (m_BottomTextField.value != value)
                    m_BottomTextField.SetValueWithoutNotify(value);
            }

            private void InitOnBorders()
            {
                var selectedStyle = m_SelectedElement.resolvedStyle;

                var value = (int)selectedStyle.borderLeftWidth;
                if (m_LeftTextField.value != value)
                    m_LeftTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.borderRightWidth;
                if (m_RightTextField.value != value)
                    m_RightTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.borderTopWidth;
                if (m_TopTextField.value != value)
                    m_TopTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.borderBottomWidth;
                if (m_BottomTextField.value != value)
                    m_BottomTextField.SetValueWithoutNotify(value);
            }

            private void InitOnPaddings()
            {
                var selectedStyle = m_SelectedElement.resolvedStyle;

                var value = (int)selectedStyle.paddingLeft;
                if (m_LeftTextField.value != value)
                    m_LeftTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.paddingRight;
                if (m_RightTextField.value != value)
                    m_RightTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.paddingTop;
                if (m_TopTextField.value != value)
                    m_TopTextField.SetValueWithoutNotify(value);

                value = (int)selectedStyle.paddingBottom;
                if (m_BottomTextField.value != value)
                    m_BottomTextField.SetValueWithoutNotify(value);
            }

            private void OnTextFieldValueChanged(ChangeEvent<int> evt, SideChanged sideChanged)
            {
                var selectedStyle = m_SelectedElement.style;
                if (boxType == BoxType.Margin)
                {
                    switch (sideChanged)
                    {
                        case SideChanged.Left:
                            selectedStyle.marginLeft = evt.newValue;
                            break;
                        case SideChanged.Right:
                            selectedStyle.marginRight = evt.newValue;
                            break;
                        case SideChanged.Top:
                            selectedStyle.marginTop = evt.newValue;
                            break;
                        case SideChanged.Bottom:
                            selectedStyle.marginBottom = evt.newValue;
                            break;
                    }
                }
                else if (boxType == BoxType.Border)
                {
                    switch (sideChanged)
                    {
                        case SideChanged.Left:
                            selectedStyle.borderLeftWidth = evt.newValue;
                            break;
                        case SideChanged.Right:
                            selectedStyle.borderRightWidth = evt.newValue;
                            break;
                        case SideChanged.Top:
                            selectedStyle.borderTopWidth = evt.newValue;
                            break;
                        case SideChanged.Bottom:
                            selectedStyle.borderBottomWidth = evt.newValue;
                            break;
                    }
                }
                else if (boxType == BoxType.Padding)
                {
                    switch (sideChanged)
                    {
                        case SideChanged.Left:
                            selectedStyle.paddingLeft = evt.newValue;
                            break;
                        case SideChanged.Right:
                            selectedStyle.paddingRight = evt.newValue;
                            break;
                        case SideChanged.Top:
                            selectedStyle.paddingTop = evt.newValue;
                            break;
                        case SideChanged.Bottom:
                            selectedStyle.paddingBottom = evt.newValue;
                            break;
                    }
                }
            }

            public void SyncValues()
            {
                if (m_SelectedElement != null)
                {
                    InitStyleValues();
                }
            }
        }

        class ContentBox : VisualElement
        {
            private VisualElement m_TextContainer;
            private IntegerField m_WidthTextField;
            private IntegerField m_HeightTextField;
            private Label m_XLabel;

            private VisualElement m_SelectedElement;

            public ContentBox()
            {
                m_TextContainer = new VisualElement();
                m_TextContainer.AddToClassList("box-model-content-text-container");

                m_WidthTextField = new IntegerField();
                m_WidthTextField.AddToClassList("box-model-textfield");

                m_HeightTextField = new IntegerField();
                m_HeightTextField.AddToClassList("box-model-textfield");

                m_XLabel = new Label("x");

                m_TextContainer.Add(m_WidthTextField);
                m_TextContainer.Add(m_XLabel);
                m_TextContainer.Add(m_HeightTextField);

                m_WidthTextField.value = 0;
                m_HeightTextField.value = 0;

                m_WidthTextField.RegisterValueChangedCallback(OnWidthChanged);
                m_HeightTextField.RegisterValueChangedCallback(OnHeightChanged);

                Add(m_TextContainer);

                // Sync styles values
                schedule.Execute(SyncValues).Every(100);
            }

            public void SelectElement(VisualElement ve)
            {
                m_SelectedElement = ve;

                if (ve != null)
                {
                    m_WidthTextField.SetValueWithoutNotify((int)m_SelectedElement.contentRect.width);
                    m_HeightTextField.SetValueWithoutNotify((int)m_SelectedElement.contentRect.height);
                }
                else
                {
                    m_WidthTextField.SetValueWithoutNotify(0);
                    m_HeightTextField.SetValueWithoutNotify(0);
                }
            }

            private void OnWidthChanged(ChangeEvent<int> evt)
            {
                var style = m_SelectedElement.resolvedStyle;
                var newValue = evt.newValue + style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
                m_SelectedElement.style.width = newValue;
            }

            private void OnHeightChanged(ChangeEvent<int> evt)
            {
                var style = m_SelectedElement.resolvedStyle;
                var newValue = evt.newValue + style.paddingTop + style.paddingBottom + style.borderTopWidth + style.borderBottomWidth;
                m_SelectedElement.style.height = newValue;
            }

            public void SyncValues()
            {
                if (m_SelectedElement != null)
                {
                    var value = (int)m_SelectedElement.contentRect.width;
                    if (m_WidthTextField.value != value)
                        m_WidthTextField.SetValueWithoutNotify(value);

                    value = (int)m_SelectedElement.contentRect.height;
                    if (m_HeightTextField.value != value)
                        m_HeightTextField.SetValueWithoutNotify(value);
                }
            }
        }
    }
}
