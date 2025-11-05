// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    class BorderBoxModel : VisualElement
    {
        static readonly string UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/StyleSectionsBoxModel";
        static readonly string StyleFieldUssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/StyleField";
        static readonly string BoxModelClassName = "unity-box-model";
        static readonly string ViewClassName = BoxModelClassName + "__view";
        static readonly string ContainerClassName = BoxModelClassName + "__view__container";
        static readonly string ContainerColorClassName = BoxModelClassName + "__container__color";
        static readonly string ContainerBorderWidthClassName = BoxModelClassName + "__container__border-width";
        static readonly string ContainerBorderRadiusClassName = BoxModelClassName + "__container__border-radius";
        static readonly string ContainerContentClassName = BoxModelClassName + "__container__content";
        static readonly string TextfieldClassName = BoxModelClassName + "__textfield";
        static readonly string TextfieldContentClassName = TextfieldClassName + "__content-center";
        static readonly string TextfieldContentTopClassName = TextfieldClassName + "__content-top";
        static readonly string TextfieldContentBottomClassName = TextfieldClassName + "__content-bottom";
        static readonly string TextfieldSpacerClassName = TextfieldClassName + "__spacer";
        static readonly string TextfieldCenterSpacerClassName = TextfieldClassName + "__center-spacer";
        static readonly string CheckerboardClassName = BoxModelClassName + "__repeat-checkerboard-background";
        static readonly string BorderElementClassName = BoxModelClassName + "__border-element";
        static readonly string BorderElementVerticalClassName = BorderElementClassName + "__vertical";
        static readonly string BorderElementHorizontalClassName = BorderElementClassName + "__horizontal";
        static readonly string BordeLayer2ClassName = BoxModelClassName + "__border-layer-2";
        static readonly string InspectorCompositeStyleRowElementClassName = "unity-builder-composite-style-row-element";

        private VisualElement m_Container;
        private VisualElement m_Layer1;
        private VisualElement m_Layer2;
        private BoxModelField<StyleColor, StyleColorField> m_ColorBox;
        private BoxModelField<StyleLength, BoxModelEditableLabel> m_BorderWidthBox;
        private BoxModelField<StyleLength, BoxModelEditableLabel> m_BorderRadiusBox;
        private VisualElement m_ContentBox;

        // color field containers
        private VisualElement m_TopColorFieldContainer = new VisualElement();
        private VisualElement m_BottomColorFieldContainer = new VisualElement();
        private VisualElement m_LeftColorFieldContainer = new VisualElement();
        private VisualElement m_RightColorFieldContainer = new VisualElement();

        // border fields (width and radius) containers
        private VisualElement m_TopBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_BottomBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_LeftBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_RightBorderWidthFieldContainer = new VisualElement();

        private VisualElement m_TopBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_BottomBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_LeftBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_RightBorderRadiusFieldContainer = new VisualElement();

        private VisualElement m_ContentCenter = new VisualElement();
        private VisualElement m_ContentTop = new VisualElement();
        private VisualElement m_ContentBottom = new VisualElement();
        private VisualElement m_CenterSpacer = new VisualElement();

        internal BoxModelField<StyleColor, StyleColorField> colorBox => m_ColorBox;
        internal BoxModelField<StyleLength, BoxModelEditableLabel> borderWidthBox => m_BorderWidthBox;
        internal BoxModelField<StyleLength, BoxModelEditableLabel> borderRadiusBox => m_BorderRadiusBox;

        public BorderBoxModel()
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
                name = "BoxModelViewLayer2", classList =
                {
                    InspectorCompositeStyleRowElementClassName,
                    BordeLayer2ClassName
                }
            };
            m_Layer2.pickingMode = PickingMode.Ignore;
            m_Layer2.StretchToParentSize();

            StyleContainers();

            m_ContentCenter.pickingMode = PickingMode.Ignore;
            m_ContentCenter.AddToClassList(TextfieldContentClassName);
            m_ContentCenter.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_ContentTop.pickingMode = PickingMode.Ignore;
            m_ContentTop.AddToClassList(TextfieldContentTopClassName);
            m_ContentTop.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_ContentBottom.pickingMode = PickingMode.Ignore;
            m_ContentBottom.AddToClassList(TextfieldContentBottomClassName);
            m_ContentBottom.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_CenterSpacer.pickingMode = PickingMode.Ignore;
            m_CenterSpacer.AddToClassList(TextfieldCenterSpacerClassName);

            m_ContentCenter.Add(m_LeftColorFieldContainer);
            m_ContentCenter.Add(m_LeftBorderWidthFieldContainer);
            m_ContentCenter.Add(m_CenterSpacer);
            m_ContentCenter.Add(m_RightBorderWidthFieldContainer);
            m_ContentCenter.Add(m_RightColorFieldContainer);

            m_ContentTop.Add(m_TopBorderRadiusFieldContainer);
            m_ContentTop.Add(m_TopBorderWidthFieldContainer);
            m_ContentTop.Add(m_RightBorderRadiusFieldContainer);

            m_ContentBottom.Add(m_LeftBorderRadiusFieldContainer);
            m_ContentBottom.Add(m_BottomBorderWidthFieldContainer);
            m_ContentBottom.Add(m_BottomBorderRadiusFieldContainer);

            m_Layer2.Add(m_TopColorFieldContainer);
            m_Layer2.Add(m_ContentTop);
            m_Layer2.Add(m_ContentCenter);
            m_Layer2.Add(m_ContentBottom);
            m_Layer2.Add(m_BottomColorFieldContainer);

            m_ContentBox = new VisualElement()
            {
                classList =
                {
                    InspectorCompositeStyleRowElementClassName,
                    BoxModelClassName, ContainerContentClassName
                }
            };

            m_BorderWidthBox = new BoxModelField<StyleLength, BoxModelEditableLabel>(BoxType.BorderWidth, true, m_ContentBox,
                m_TopBorderWidthFieldContainer, m_BottomBorderWidthFieldContainer,
                m_LeftBorderWidthFieldContainer, m_RightBorderWidthFieldContainer,
                () => new BoxModelEditableLabel());
            m_BorderWidthBox.AddToClassList(BoxModelClassName);
            m_BorderWidthBox.AddToClassList(ContainerBorderWidthClassName);
            m_BorderWidthBox.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_BorderWidthBox.fields.ForEach(f =>
            {
                (f as BoxModelEditableLabel)?.AddValidation(new Syntax("border-width"));
            });

            m_BorderRadiusBox = new BoxModelField<StyleLength, BoxModelEditableLabel>(BoxType.BorderRadius, true, new VisualElement(),
                m_TopBorderRadiusFieldContainer, m_BottomBorderRadiusFieldContainer,
                m_LeftBorderRadiusFieldContainer, m_RightBorderRadiusFieldContainer,
                () => new BoxModelEditableLabel());
            m_BorderRadiusBox.AddToClassList(BoxModelClassName);
            m_BorderRadiusBox.AddToClassList(ContainerBorderRadiusClassName);
            m_BorderRadiusBox.AddToClassList(InspectorCompositeStyleRowElementClassName);
            m_BorderWidthBox.Add(m_BorderRadiusBox);

            m_BorderRadiusBox.fields.ForEach(f =>
            {
                (f as BoxModelEditableLabel)?.AddValidation(new Syntax("border-radius"));
            });

            // visual elements used for border style
            var borderElement1 = new VisualElement() { classList = { BorderElementClassName, BorderElementVerticalClassName } };
            var borderElement2 = new VisualElement() { classList = { BorderElementClassName, BorderElementHorizontalClassName } };
            m_BorderWidthBox.Add(borderElement1);
            m_BorderWidthBox.Add(borderElement2);

            m_ColorBox = new BoxModelField<StyleColor, StyleColorField>(BoxType.BorderColor, true, m_BorderWidthBox,
                m_TopColorFieldContainer, m_BottomColorFieldContainer,
                m_LeftColorFieldContainer, m_RightColorFieldContainer,
                () => new StyleColorField());

            m_ColorBox.fields.ForEach(f =>
            {
                if (f is StyleColorField field) field.containsAffordance = false;
            });

            m_ColorBox.AddToClassList(BoxModelClassName);
            m_ColorBox.AddToClassList(ContainerColorClassName);
            m_ColorBox.AddToClassList(CheckerboardClassName);
            m_ColorBox.AddToClassList(InspectorCompositeStyleRowElementClassName);

            m_Layer1.Add(m_ColorBox);

            m_Container.Add(m_Layer1);
            m_Container.Add(m_Layer2);
            Add(m_Container);
        }

        private void StyleContainers()
        {
            foreach (var container in new[]
                     {
                         m_TopColorFieldContainer, m_TopBorderWidthFieldContainer,
                         m_BottomColorFieldContainer, m_BottomBorderWidthFieldContainer,
                         m_LeftColorFieldContainer, m_LeftBorderWidthFieldContainer,
                         m_RightColorFieldContainer, m_RightBorderWidthFieldContainer,
                         m_TopBorderRadiusFieldContainer, m_BottomBorderRadiusFieldContainer,
                         m_LeftBorderRadiusFieldContainer, m_RightBorderRadiusFieldContainer
                     })
            {
                container.pickingMode = PickingMode.Ignore;
                container.AddToClassList(TextfieldSpacerClassName);
                container.AddToClassList(InspectorCompositeStyleRowElementClassName);
            }
        }
    }

    internal class BorderBoxModelField : OverrideRow
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : OverrideRow.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new BorderBoxModelField();
        }

        private BorderBoxModel m_BorderField;

        internal BorderBoxModel borderField => m_BorderField;

        StyleFloat m_BorderTopWidth;
        StyleFloat m_BorderRightWidth;
        StyleFloat m_BorderBottomWidth;
        StyleFloat m_BorderLeftWidth;

        StyleLength m_BorderTopRightRadius;
        StyleLength m_BorderTopLeftRadius;
        StyleLength m_BorderBottomRightRadius;
        StyleLength m_BorderBottomLeftRadius;

        StyleColor m_BorderTopColor;
        StyleColor m_BorderRightColor;
        StyleColor m_BorderBottomColor;
        StyleColor m_BorderLeftColor;

        [CreateProperty]
        public StyleFloat borderTopWidth
        {
            get => m_BorderTopWidth;
            set
            {
                if (m_BorderTopWidth == value)
                    return;

                m_BorderTopWidth = value;
                borderField.borderWidthBox.topField.SetValueWithoutNotify(value.value);

                Refresh();

                NotifyPropertyChanged(nameof(borderTopWidth));
            }
        }

        [CreateProperty]
        public StyleFloat borderRightWidth
        {
            get => m_BorderRightWidth;
            set
            {
                if (m_BorderRightWidth == value)
                    return;

                m_BorderRightWidth = value;
                borderField.borderWidthBox.rightField.SetValueWithoutNotify(value.value);

                Refresh();

                NotifyPropertyChanged(nameof(borderRightWidth));
            }
        }

        [CreateProperty]
        public StyleFloat borderBottomWidth
        {
            get => m_BorderBottomWidth;
            set
            {
                if (m_BorderBottomWidth == value)
                    return;

                m_BorderBottomWidth = value;
                borderField.borderWidthBox.bottomField.SetValueWithoutNotify(value.value);

                Refresh();

                NotifyPropertyChanged(nameof(borderBottomWidth));
            }
        }

        [CreateProperty]
        public StyleFloat borderLeftWidth
        {
            get => m_BorderLeftWidth;
            set
            {
                if (m_BorderLeftWidth == value)
                    return;

                m_BorderLeftWidth = value;
                borderField.borderWidthBox.leftField.SetValueWithoutNotify(value.value);

                Refresh();

                NotifyPropertyChanged(nameof(borderLeftWidth));
            }
        }

        [CreateProperty]
        public StyleLength borderTopRightRadius
        {
            get => m_BorderTopRightRadius;
            set
            {
                if (m_BorderTopRightRadius == value)
                    return;

                m_BorderTopRightRadius = value;
                borderField.borderRadiusBox.rightField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderTopRightRadius));
            }
        }

        [CreateProperty]
        public StyleLength borderTopLeftRadius
        {
            get => m_BorderTopLeftRadius;
            set
            {
                if (m_BorderTopLeftRadius == value)
                    return;

                m_BorderTopLeftRadius = value;
                borderField.borderRadiusBox.topField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderTopLeftRadius));
            }
        }

        [CreateProperty]
        public StyleLength borderBottomRightRadius
        {
            get => m_BorderBottomRightRadius;
            set
            {
                if (m_BorderBottomRightRadius == value)
                    return;

                m_BorderBottomRightRadius = value;
                borderField.borderRadiusBox.bottomField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderBottomRightRadius));
            }
        }

        [CreateProperty]
        public StyleLength borderBottomLeftRadius
        {
            get => m_BorderBottomLeftRadius;
            set
            {
                if (m_BorderBottomLeftRadius == value)
                    return;

                m_BorderBottomLeftRadius = value;
                borderField.borderRadiusBox.leftField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderBottomLeftRadius));
            }
        }


        [CreateProperty]
        public StyleColor borderTopColor
        {
            get => m_BorderTopColor;
            set
            {
                if (m_BorderTopColor == value)
                    return;

                m_BorderTopColor = value;
                borderField.colorBox.topField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderTopColor));
            }
        }

        [CreateProperty]
        public StyleColor borderRightColor
        {
            get => m_BorderRightColor;
            set
            {
                if (m_BorderRightColor == value)
                    return;

                m_BorderRightColor = value;
                borderField.colorBox.rightField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderRightColor));
            }
        }

        [CreateProperty]
        public StyleColor borderBottomColor
        {
            get => m_BorderBottomColor;
            set
            {
                if (m_BorderBottomColor == value)
                    return;

                m_BorderBottomColor = value;
                borderField.colorBox.bottomField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderBottomColor));
            }
        }

        [CreateProperty]
        public StyleColor borderLeftColor
        {
            get => m_BorderLeftColor;
            set
            {
                if (m_BorderLeftColor == value)
                    return;

                m_BorderLeftColor = value;
                borderField.colorBox.leftField.SetValueWithoutNotify(value);

                Refresh();

                NotifyPropertyChanged(nameof(borderLeftColor));
            }
        }


        public BorderBoxModelField()
        {
            m_BorderField = new BorderBoxModel();

            borderField.borderWidthBox.topField.RegisterValueChangedCallback(e => { borderTopWidth = e.newValue.value.value; });
            borderField.borderWidthBox.rightField.RegisterValueChangedCallback(e => { borderRightWidth = e.newValue.value.value; });
            borderField.borderWidthBox.bottomField.RegisterValueChangedCallback(e => { borderBottomWidth = e.newValue.value.value; });
            borderField.borderWidthBox.leftField.RegisterValueChangedCallback(e => { borderLeftWidth = e.newValue.value.value; });

            borderField.borderRadiusBox.topField.RegisterValueChangedCallback(e => { borderTopLeftRadius = e.newValue; });
            borderField.borderRadiusBox.rightField.RegisterValueChangedCallback(e => { borderTopRightRadius = e.newValue; });
            borderField.borderRadiusBox.bottomField.RegisterValueChangedCallback(e => { borderBottomRightRadius = e.newValue; });
            borderField.borderRadiusBox.leftField.RegisterValueChangedCallback(e => { borderBottomLeftRadius = e.newValue; });

            borderField.colorBox.topField.RegisterValueChangedCallback(e => { borderTopColor = e.newValue; });
            borderField.colorBox.rightField.RegisterValueChangedCallback(e => { borderRightColor = e.newValue; });
            borderField.colorBox.bottomField.RegisterValueChangedCallback(e => { borderBottomColor = e.newValue; });
            borderField.colorBox.leftField.RegisterValueChangedCallback(e => { borderLeftColor = e.newValue; });

            Add(m_BorderField);
        }

        void Refresh()
        {
            // Only need to update the color box as it contains the unit title for the other two boxes.
            borderField.colorBox.UpdateUnitFromFields();
        }
    }
}
