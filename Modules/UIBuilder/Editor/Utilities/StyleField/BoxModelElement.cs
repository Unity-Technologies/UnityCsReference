// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    abstract class BoxModel : BindableElement
    {
        protected string[] m_BindingPathArray;
        public string[] bindingPathArray => m_BindingPathArray;
        internal abstract void UpdateUnitFromFields();
    }
    
    class BoxModelElement<TValueType, TControl> : BoxModel 
        where TControl : BaseField<TValueType>, new()
    {
        static readonly string k_MixedUnitLabel = "mixed";
        static readonly string k_BoxModelClassName = "unity-box-model";
        static readonly string k_CenterContentClassName = k_BoxModelClassName + "__center-content";
        static readonly string k_TextfieldClassName = k_BoxModelClassName + "__textfield";
        static readonly string k_ColorfieldClassName = k_BoxModelClassName + "__colorfield";
        static readonly string k_TitleClassName = k_BoxModelClassName + "__title";
        static readonly string k_CenterClassName = k_BoxModelClassName + "__center";
        
        private Label m_Title;
        private VisualElement m_Center;
        private VisualElement m_CenterContent;
        private VisualElement m_BackgroundElement;

        private BaseField<TValueType> m_LeftField;
        private BaseField<TValueType> m_RightField;
        private BaseField<TValueType> m_TopField;
        private BaseField<TValueType> m_BottomField;
        private List<BaseField<TValueType>> m_Fields;

        private bool m_NeedsUnit;

        public VisualElement background => m_BackgroundElement;
        
        public bool needsUnit
        {
            get => m_NeedsUnit;
            set
            {
                if (m_NeedsUnit == value)
                    return;
            
                m_NeedsUnit = value;
                if (!m_NeedsUnit) return;
                
                m_Title = new Label(GetUnitFromFields());
                m_Title.AddToClassList(k_TitleClassName);
                m_Title.AddToClassList(boxType.ToString().ToLowerInvariant());
                Add(m_Title);
            }
        }

        public BoxModelElement(BoxType boxType, bool needsUnit, VisualElement content,
            VisualElement topFieldContainer, VisualElement bottomFieldContainer,
            VisualElement leftFieldContainer, VisualElement rightFieldContainer)
        {
            this.boxType = boxType;
            
            m_CenterContent = content;
            m_CenterContent.AddToClassList(k_CenterContentClassName);

            m_TopField = new TControl();
            m_BottomField = new TControl();
            m_RightField = new TControl();
            m_LeftField = new TControl();
            
            m_Fields = new List<BaseField<TValueType>>() { m_TopField, m_BottomField, m_RightField, m_LeftField };
      
            foreach (var field in m_Fields) 
            { 
                if (typeof(TValueType) == typeof(string))
                    field.AddToClassList(k_TextfieldClassName);
                else if (typeof(TValueType) == typeof(Color))
                    field.AddToClassList(k_ColorfieldClassName);
            }

            SetBindingPaths();
            
            leftFieldContainer.Add(m_LeftField);
            rightFieldContainer.Add(m_RightField);
            topFieldContainer.Add(m_TopField);
            bottomFieldContainer.Add(m_BottomField);
            
            m_Center = new VisualElement();
            m_Center.AddToClassList(k_CenterClassName);
            m_Center.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_Center.Add(m_CenterContent);
            Add(m_Center);
            
            this.needsUnit = needsUnit;
        }

        private void SetBindingPaths()
        {
            switch (boxType)
            {
                case BoxType.Margin:
                    m_BindingPathArray = new[] { StylePropertyId.MarginLeft.UssName(), StylePropertyId.MarginRight.UssName(), 
                        StylePropertyId.MarginTop.UssName(), StylePropertyId.MarginBottom.UssName() };
                    m_LeftField.bindingPath = StylePropertyId.MarginLeft.UssName();
                    m_RightField.bindingPath = StylePropertyId.MarginRight.UssName();
                    m_TopField.bindingPath = StylePropertyId.MarginTop.UssName();
                    m_BottomField.bindingPath = StylePropertyId.MarginBottom.UssName();
                    break;
                case BoxType.BorderColor:
                    // to show the width and radius unit in the color box
                    m_BindingPathArray = new[] { StylePropertyId.BorderLeftWidth.UssName(), StylePropertyId.BorderRightWidth.UssName(), 
                        StylePropertyId.BorderTopWidth.UssName(), StylePropertyId.BorderBottomWidth.UssName(),
                        StylePropertyId.BorderTopLeftRadius.UssName(), StylePropertyId.BorderTopRightRadius.UssName(), 
                        StylePropertyId.BorderBottomLeftRadius.UssName(), StylePropertyId.BorderBottomRightRadius.UssName()
                    };
                    m_LeftField.bindingPath = StylePropertyId.BorderLeftColor.UssName();
                    m_RightField.bindingPath = StylePropertyId.BorderRightColor.UssName();
                    m_TopField.bindingPath = StylePropertyId.BorderTopColor.UssName();
                    m_BottomField.bindingPath = StylePropertyId.BorderBottomColor.UssName();
                    break;
                case BoxType.Padding:
                    m_BindingPathArray = new[] { StylePropertyId.PaddingLeft.UssName(), StylePropertyId.PaddingRight.UssName(), 
                        StylePropertyId.PaddingTop.UssName(), StylePropertyId.PaddingBottom.UssName() };
                    m_LeftField.bindingPath = StylePropertyId.PaddingLeft.UssName();
                    m_RightField.bindingPath = StylePropertyId.PaddingRight.UssName();
                    m_TopField.bindingPath = StylePropertyId.PaddingTop.UssName();
                    m_BottomField.bindingPath = StylePropertyId.PaddingBottom.UssName();
                    break;
                case BoxType.BorderWidth:
                    m_BindingPathArray = new[] { StylePropertyId.BorderLeftWidth.UssName(), StylePropertyId.BorderRightWidth.UssName(), 
                        StylePropertyId.BorderTopWidth.UssName(), StylePropertyId.BorderBottomWidth.UssName(),
                        StylePropertyId.BorderTopLeftRadius.UssName(), StylePropertyId.BorderTopRightRadius.UssName(), 
                        StylePropertyId.BorderBottomLeftRadius.UssName(), StylePropertyId.BorderBottomRightRadius.UssName()
                    };
                    m_LeftField.bindingPath = StylePropertyId.BorderLeftWidth.UssName();
                    m_RightField.bindingPath = StylePropertyId.BorderRightWidth.UssName();
                    m_TopField.bindingPath = StylePropertyId.BorderTopWidth.UssName();
                    m_BottomField.bindingPath = StylePropertyId.BorderBottomWidth.UssName();
                    break;
                case BoxType.BorderRadius:
                    m_BindingPathArray = new[] { StylePropertyId.BorderLeftWidth.UssName(), StylePropertyId.BorderRightWidth.UssName(), 
                        StylePropertyId.BorderTopWidth.UssName(), StylePropertyId.BorderBottomWidth.UssName(),
                        StylePropertyId.BorderTopLeftRadius.UssName(), StylePropertyId.BorderTopRightRadius.UssName(), 
                        StylePropertyId.BorderBottomLeftRadius.UssName(), StylePropertyId.BorderBottomRightRadius.UssName()
                    };
                    m_LeftField.bindingPath = StylePropertyId.BorderBottomLeftRadius.UssName();
                    m_RightField.bindingPath = StylePropertyId.BorderTopRightRadius.UssName();
                    m_TopField.bindingPath = StylePropertyId.BorderTopLeftRadius.UssName();
                    m_BottomField.bindingPath = StylePropertyId.BorderBottomRightRadius.UssName();
                    break;
            }
        }

        public BoxType boxType { get; private set; }
        
        internal override void UpdateUnitFromFields()
        {
            if (!needsUnit)
                return;
            
            var unit = GetUnitFromFields();
            
            m_Title.text = unit;
            
            foreach (var boxModelStyleField in m_Fields.Select(field => field as BoxModelStyleField))
            {
                if (boxModelStyleField != null)
                    boxModelStyleField.showUnit = unit == k_MixedUnitLabel;
            }
        }
        
        string GetUnitFromFields()
        {
            if (!needsUnit)
                return string.Empty;

            var unit = Dimension.Unit.Unitless;
            var singleUnit = true;
            var prevPath = m_BindingPathArray[0];

            foreach (var path in m_BindingPathArray)
            {
                unit = GetUnitFromField(path);
                if (unit != GetUnitFromField(prevPath))
                {
                    singleUnit = false;
                    break;
                }

                prevPath = path;
            }
            
            if (!singleUnit)
                return k_MixedUnitLabel;

            var found = StyleFieldConstants.DimensionUnitToStringMap.TryGetValue(unit,
                out var opt);
            if (found)
                return opt;

            return string.Empty;
        }

        Dimension.Unit GetUnitFromField(string styleName)
        {
            var inspector = GetFirstAncestorOfType<BuilderInspector>();
            if (inspector == null)
                return Dimension.Unit.Unitless;

            var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(styleName);
            var styleProperty = BuilderInspectorStyleFields.GetLastStyleProperty(inspector.currentRule, cSharpStyleName);

            if (styleProperty == null || styleProperty.IsVariable())
            {
                var val = StyleDebug.GetComputedStyleValue(inspector.currentVisualElement.computedStyle, styleName);
                var lengthUnit = BuilderInspectorStyleFields.GetComputedStyleLengthUnit(val);
                return StyleSheetUtilities.ConvertToDimensionUnit(lengthUnit);
            }

            var styleValue = styleProperty.values[0];
            
            if (styleValue.valueType == StyleValueType.Dimension)
            {
                var dimension = inspector.styleSheet.GetDimension(styleValue);
                return dimension.unit;
            }
            
            return Dimension.Unit.Unitless;
        }

        internal void AddBackground(VisualElement backgroundElement)
        {
            m_BackgroundElement = backgroundElement;
            Add(m_BackgroundElement);
            m_BackgroundElement.SendToBack();
        }
    }
}
