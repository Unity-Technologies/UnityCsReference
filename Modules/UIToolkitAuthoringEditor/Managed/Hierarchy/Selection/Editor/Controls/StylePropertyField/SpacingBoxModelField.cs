// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    class SpacingBoxModel : VisualElement
    {
        static readonly string UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/StyleSectionsBoxModel";
        static readonly string StyleFieldUssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/StyleField";
        static readonly string MouseOverClassName = "mouse-over";
        static readonly string BoxModelClassName = "unity-box-model";
        static readonly string ViewClassName = BoxModelClassName + "__view";
        static readonly string ContainerClassName = BoxModelClassName + "__view__container";
        static readonly string ContainerMarginClassName = BoxModelClassName + "__container__margin";
        static readonly string ContainerPaddingClassName = BoxModelClassName + "__container__padding";
        static readonly string ContainerBorderClassName = BoxModelClassName + "__container__border";
        static readonly string ContainerContentClassName = BoxModelClassName + "__container__content";
        static readonly string TextfieldClassName = BoxModelClassName + "__textfield";
        static readonly string TextfieldContentClassName = TextfieldClassName + "__content-center";
        static readonly string TextfieldSpacerClassName = TextfieldClassName + "__spacer";
        static readonly string TextfieldCenterSpacerClassName = TextfieldClassName + "__center-spacer";
        static readonly string CheckerboardClassName = BoxModelClassName + "__repeat-checkerboard-background";
        static readonly string InspectorCompositeStyleRowElementClassName = "unity-builder-composite-style-row-element";

        private VisualElement m_Container;
        private VisualElement m_Layer1;
        private VisualElement m_Layer2;
        private BoxModelField<StyleLength, BoxModelEditableLabel> m_MarginBox;
        private BoxModelField<StyleLength, BoxModelEditableLabel> m_PaddingBox;
        private VisualElement m_ContentBox;
        private VisualElement m_BorderBox;

        public BoxModelField<StyleLength, BoxModelEditableLabel> marginBox => m_MarginBox;
        public BoxModelField<StyleLength, BoxModelEditableLabel> paddingBox => m_PaddingBox;

        // margin containers
        private VisualElement m_TopTextFieldMarginContainer = new();
        private VisualElement m_BottomTextFieldMarginContainer = new();
        private VisualElement m_LeftTextFieldMarginContainer = new();
        private VisualElement m_RightTextFieldMarginContainer = new();

        // padding containers
        private VisualElement m_TopTextFieldPaddingContainer = new();
        private VisualElement m_BottomTextFieldPaddingContainer = new();
        private VisualElement m_LeftTextFieldPaddingContainer = new();
        private VisualElement m_RightTextFieldPaddingContainer = new();

        private VisualElement m_ContentCenter = new();
        private VisualElement m_CenterSpacer = new();

        public SpacingBoxModel()
        {
            styleSheets.Add(EditorGUIUtility.Load(UssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load(StyleFieldUssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load(UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss") as StyleSheet);

            AddToClassList(ViewClassName);
            AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_Container = new VisualElement();
            m_Container.AddToClassList(ContainerClassName);
            m_Container.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_Layer1 = new VisualElement()
            {
                name = "BoxModelViewLayer1", classList = { InspectorCompositeStyleRowElementClassName }
            };
            m_Layer2 = new VisualElement()
            {
                name = "BoxModelViewLayer2", classList = { InspectorCompositeStyleRowElementClassName }
            };
            m_Layer2.pickingMode = PickingMode.Ignore;
            m_Layer2.StretchToParentSize();

            StyleContainers();

            m_ContentCenter.pickingMode = PickingMode.Ignore;
            m_ContentCenter.AddToClassList(TextfieldContentClassName);
            m_ContentCenter.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_CenterSpacer.pickingMode = PickingMode.Ignore;
            m_CenterSpacer.AddToClassList(TextfieldCenterSpacerClassName);

            m_ContentCenter.Add(m_LeftTextFieldMarginContainer);
            m_ContentCenter.Add(m_LeftTextFieldPaddingContainer);
            m_ContentCenter.Add(m_CenterSpacer);
            m_ContentCenter.Add(m_RightTextFieldPaddingContainer);
            m_ContentCenter.Add(m_RightTextFieldMarginContainer);

            m_Layer2.Add(m_TopTextFieldMarginContainer);
            m_Layer2.Add(m_TopTextFieldPaddingContainer);
            m_Layer2.Add(m_ContentCenter);
            m_Layer2.Add(m_BottomTextFieldPaddingContainer);
            m_Layer2.Add(m_BottomTextFieldMarginContainer);

            m_ContentBox = new VisualElement()
            {
                classList =
                {
                    InspectorCompositeStyleRowElementClassName,
                    BoxModelClassName, ContainerContentClassName
                }
            };
            m_ContentBox.tooltip = "Size";
            m_ContentBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Content);

            m_PaddingBox = new BoxModelField<StyleLength, BoxModelEditableLabel>(BoxType.Padding, true, m_ContentBox,
                m_TopTextFieldPaddingContainer, m_BottomTextFieldPaddingContainer,
                m_LeftTextFieldPaddingContainer, m_RightTextFieldPaddingContainer,
                () => new BoxModelEditableLabel());
            m_PaddingBox.tooltip = BoxType.Padding.ToString();
            m_PaddingBox.AddToClassList(BoxModelClassName);
            m_PaddingBox.AddToClassList(ContainerPaddingClassName);
            m_PaddingBox.AddToClassList(InspectorCompositeStyleRowElementClassName);
            m_PaddingBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Padding);

            m_PaddingBox.fields.ForEach(f =>
            {
                (f as BoxModelEditableLabel)?.AddValidation(new Syntax("padding"));
            });

            m_BorderBox = new VisualElement();
            m_BorderBox.tooltip = BoxType.Border.ToString();
            m_BorderBox.AddToClassList(BoxModelClassName);
            m_BorderBox.AddToClassList(ContainerBorderClassName);
            m_BorderBox.AddToClassList(InspectorCompositeStyleRowElementClassName);
            m_BorderBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Border);
            m_BorderBox.Add(m_PaddingBox);

            m_MarginBox = new BoxModelField<StyleLength, BoxModelEditableLabel>(BoxType.Margin, true, m_BorderBox,
                m_TopTextFieldMarginContainer, m_BottomTextFieldMarginContainer,
                m_LeftTextFieldMarginContainer, m_RightTextFieldMarginContainer,
                () => new BoxModelEditableLabel());

            m_MarginBox.tooltip = BoxType.Margin.ToString();
            m_MarginBox.AddToClassList(BoxModelClassName);
            m_MarginBox.AddToClassList(ContainerMarginClassName);
            m_MarginBox.AddToClassList(CheckerboardClassName);
            m_MarginBox.AddToClassList(InspectorCompositeStyleRowElementClassName);
            m_MarginBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Margin);

            m_Layer1.Add(m_MarginBox);

            m_Container.Add(m_Layer1);
            m_Container.Add(m_Layer2);
            Add(m_Container);

            RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        private void StyleContainers()
        {
            foreach (var container in new[]
                     {
                         m_TopTextFieldMarginContainer, m_TopTextFieldPaddingContainer,
                         m_BottomTextFieldMarginContainer, m_BottomTextFieldPaddingContainer,
                         m_LeftTextFieldMarginContainer, m_LeftTextFieldPaddingContainer,
                         m_RightTextFieldMarginContainer, m_RightTextFieldPaddingContainer
                     })
            {
                container.pickingMode = PickingMode.Ignore;
                container.AddToClassList(TextfieldSpacerClassName);
                container.AddToClassList(InspectorCompositeStyleRowElementClassName);
            }
        }

        private void OnMouseOver(MouseOverEvent evt, BoxType boxType)
        {
            m_MarginBox.EnableInClassList(MouseOverClassName, boxType == BoxType.Margin);
            m_BorderBox.EnableInClassList(MouseOverClassName, boxType == BoxType.Border);
            m_PaddingBox.EnableInClassList(MouseOverClassName, boxType == BoxType.Padding);
            m_ContentBox.EnableInClassList(MouseOverClassName, boxType == BoxType.Content);
            evt.StopPropagation();
        }

        private void OnMouseOut(MouseOutEvent evt)
        {
            m_MarginBox.EnableInClassList(MouseOverClassName, false);
            m_BorderBox.EnableInClassList(MouseOverClassName, false);
            m_PaddingBox.EnableInClassList(MouseOverClassName, false);
            m_ContentBox.EnableInClassList(MouseOverClassName, false);
            evt.StopPropagation();
        }
    }

    internal class SpacingBoxModelField : OverrideRow
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : OverrideRow.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new SpacingBoxModelField();
        }

        private SpacingBoxModel m_SpacingField;

        internal SpacingBoxModel spacingField => m_SpacingField;

        StyleLength m_MarginTop;
        StyleLength m_MarginRight;
        StyleLength m_MarginBottom;
        StyleLength m_MarginLeft;

        StyleLength m_PaddingTop;
        StyleLength m_PaddingRight;
        StyleLength m_PaddingBottom;
        StyleLength m_PaddingLeft;

        [CreateProperty]
        public StyleLength marginTop
        {
            get => m_MarginTop;
            set
            {
                if (m_MarginTop == value)
                    return;

                m_MarginTop = value;
                m_SpacingField.marginBox.topField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(marginTop));
            }
        }

        [CreateProperty]
        public StyleLength marginRight
        {
            get => m_MarginRight;
            set
            {
                if (m_MarginRight == value)
                    return;

                m_MarginRight = value;
                m_SpacingField.marginBox.rightField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(marginRight));
            }
        }

        [CreateProperty]
        public StyleLength marginBottom
        {
            get => m_MarginBottom;
            set
            {
                if (m_MarginBottom == value)
                    return;

                m_MarginBottom = value;
                m_SpacingField.marginBox.bottomField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(marginBottom));
            }
        }

        [CreateProperty]
        public StyleLength marginLeft
        {
            get => m_MarginLeft;
            set
            {
                if (m_MarginLeft == value)
                    return;

                m_MarginLeft = value;
                m_SpacingField.marginBox.leftField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(marginLeft));
            }
        }

        [CreateProperty]
        public StyleLength paddingTop
        {
            get => m_PaddingTop;
            set
            {
                if (m_PaddingTop == value)
                    return;

                m_PaddingTop = value;
                m_SpacingField.paddingBox.topField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(paddingTop));
            }
        }

        [CreateProperty]
        public StyleLength paddingRight
        {
            get => m_PaddingRight;
            set
            {
                if (m_PaddingRight == value)
                    return;

                m_PaddingRight = value;
                m_SpacingField.paddingBox.rightField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(paddingRight));
            }
        }

        [CreateProperty]
        public StyleLength paddingBottom
        {
            get => m_PaddingBottom;
            set
            {
                if (m_PaddingBottom == value)
                    return;

                m_PaddingBottom = value;
                m_SpacingField.paddingBox.bottomField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(paddingBottom));
            }
        }

        [CreateProperty]
        public StyleLength paddingLeft
        {
            get => m_PaddingLeft;
            set
            {
                if (m_PaddingLeft == value)
                    return;

                m_PaddingLeft = value;
                m_SpacingField.paddingBox.leftField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(paddingLeft));
            }
        }

        public SpacingBoxModelField()
        {
            m_SpacingField = new SpacingBoxModel();

            spacingField.marginBox.topField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => marginTop = evt.newValue);
            spacingField.marginBox.rightField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => marginRight = evt.newValue);
            spacingField.marginBox.bottomField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => marginBottom = evt.newValue);
            spacingField.marginBox.leftField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => marginLeft = evt.newValue);

            spacingField.paddingBox.topField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => paddingTop = evt.newValue);
            spacingField.paddingBox.rightField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => paddingRight = evt.newValue);
            spacingField.paddingBox.bottomField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => paddingBottom = evt.newValue);
            spacingField.paddingBox.leftField.RegisterCallback<ChangeEvent<StyleLength>>((evt) => paddingLeft = evt.newValue);

            Add(m_SpacingField);
        }

        void Refresh()
        {
            spacingField.marginBox.UpdateUnitFromFields();
            spacingField.paddingBox.UpdateUnitFromFields();
        }
    }
}
