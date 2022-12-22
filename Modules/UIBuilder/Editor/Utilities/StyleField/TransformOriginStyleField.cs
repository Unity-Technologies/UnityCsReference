// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;


namespace Unity.UI.Builder
{
    internal struct BuilderTransformOrigin : IEquatable<BuilderTransformOrigin>
    {
        public Dimension x;
        public Dimension y;
        public BuilderTransformOrigin(StyleTransformOrigin styleTransformOrigin)
            : this(styleTransformOrigin.value)
        {
        }

        public BuilderTransformOrigin(TransformOrigin transformOrigin)
        {
            x = new Dimension(transformOrigin.x.value, StyleSheetUtilities.ConvertToDimensionUnit(transformOrigin.x.unit));
            y = new Dimension(transformOrigin.y.value, StyleSheetUtilities.ConvertToDimensionUnit(transformOrigin.y.unit));
        }

        public BuilderTransformOrigin(float xValue, Dimension.Unit xUnit, float yValue, Dimension.Unit yUnit)
        {
            x = new Dimension(xValue, xUnit);
            y = new Dimension(yValue, yUnit);
        }

        public BuilderTransformOrigin(float xValue, float yValue, Dimension.Unit unit = Dimension.Unit.Percent)
        {
            x = new Dimension(xValue, unit);
            y = new Dimension(yValue, unit);
        }

        public static bool operator ==(BuilderTransformOrigin lhs, BuilderTransformOrigin rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(BuilderTransformOrigin lhs, BuilderTransformOrigin rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(BuilderTransformOrigin other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BuilderTransformOrigin))
            {
                return false;
            }

            var v = (BuilderTransformOrigin)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = -799583767;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"(x:{x}, y:{y})";
        }
    }

    [UsedImplicitly]
    class TransformOriginStyleField : BaseField<BuilderTransformOrigin>
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<TransformOriginStyleField, UxmlTraits> { }

        static readonly string s_FieldClassName = "unity-transform-origin-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginStyleField.uxml";
        static readonly string s_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginStyleField";
        static readonly string s_VisualInputName = "unity-visual-input";
        public static readonly string s_TransformOriginXFieldName = "x-field";
        public static readonly string s_TransformOriginYFieldName = "y-field";
        public static readonly string s_TransformOriginSelectorFieldName = "selector";
        const TransformOriginOffset k_TransformOriginOffset_None = 0;

        DimensionStyleField m_TransformOriginXField;
        DimensionStyleField m_TransformOriginYField;
        TransformOriginSelector m_Selector;

        public TransformOriginStyleField() : this(null) { }

        public TransformOriginStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPathNoExt + ".uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);

            template.CloneTree(this);

            visualInput = this.Q(s_VisualInputName);

            m_TransformOriginXField = this.Q<DimensionStyleField>(s_TransformOriginXFieldName);
            m_TransformOriginYField = this.Q<DimensionStyleField>(s_TransformOriginYFieldName);
            m_Selector = this.Q<TransformOriginSelector>(s_TransformOriginSelectorFieldName);

            m_TransformOriginXField.RegisterValueChangedCallback(e =>
            {
                UpdateTransformOriginField();
                e.StopPropagation();
            });
            m_TransformOriginYField.RegisterValueChangedCallback(e =>
            {
                UpdateTransformOriginField();
                e.StopPropagation();
            });

            m_Selector.pointSelected = OnPointClicked;

            m_TransformOriginXField.units.Add(StyleFieldConstants.UnitPercent);
            m_TransformOriginXField.populatesOptionsMenuFromParentRow = false;
            m_TransformOriginYField.units.Add(StyleFieldConstants.UnitPercent);
            m_TransformOriginYField.populatesOptionsMenuFromParentRow = false;

            m_TransformOriginXField.UpdateOptionsMenu();
            m_TransformOriginYField.UpdateOptionsMenu();

            value = new BuilderTransformOrigin()
            {
                x = new Dimension { value = 0, unit = Dimension.Unit.Pixel },
                y = new Dimension { value = 0, unit = Dimension.Unit.Pixel }
            };
        }

        public override void SetValueWithoutNotify(BuilderTransformOrigin newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_TransformOriginXField.SetValueWithoutNotify(value.x.ToString());
            m_TransformOriginYField.SetValueWithoutNotify(value.y.ToString());
            UpdateSelector();
        }

        void UpdateTransformOriginField()
        {
            // Rebuild value from sub fields
            value = new BuilderTransformOrigin()
            {
                x = new Dimension { value = m_TransformOriginXField.length, unit = m_TransformOriginXField.unit },
                y = new Dimension { value = m_TransformOriginYField.length, unit = m_TransformOriginYField.unit }
            };
        }

        void OnPointClicked(float x, float y)
        {
            value = new BuilderTransformOrigin()
            {
                x = new Dimension { value = x * 100, unit = Dimension.Unit.Percent },
                y = new Dimension { value = y * 100, unit = Dimension.Unit.Percent }
            };
        }

        static TransformOriginOffset GetOffsetFromEnumString(string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                if (Enum.TryParse(stringValue, true, out TransformOriginOffset offset))
                {
                    return offset;
                }
            }

            return k_TransformOriginOffset_None;
        }

        static TransformOriginOffset GetOffsetFromDim(Dimension dim, bool horizontal)
        {
            TransformOriginOffset offset = k_TransformOriginOffset_None;

            if (Mathf.Approximately(dim.value, 0))
            {
                offset = horizontal ? TransformOriginOffset.Left : TransformOriginOffset.Top;
            }
            else if (dim.unit == Dimension.Unit.Percent)
            {
                if (Mathf.Approximately(dim.value, 50))
                {
                    offset = TransformOriginOffset.Center;
                }
                else if (Mathf.Approximately(dim.value, 100))
                {
                    offset = horizontal ? TransformOriginOffset.Right : TransformOriginOffset.Bottom;
                }
            }

            return offset;
        }

        static bool IsOffsetHorizontal(TransformOriginOffset offset)
        {
            return offset == TransformOriginOffset.Left || offset == TransformOriginOffset.Center || offset == TransformOriginOffset.Right;
        }

        static bool IsOffsetVertical(TransformOriginOffset offset)
        {
            return offset == TransformOriginOffset.Top || offset == TransformOriginOffset.Center || offset == TransformOriginOffset.Bottom;
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;
            var newXOriginOffset = GetOffsetFromDim(value.x, true);
            var newYOriginOffset = GetOffsetFromDim(value.y, false);

            // if the transform-origin property was already defined then ...
            if (!isNewValue)
            {
                // if only one value is specified
                // eg: 
                //    - transform-origin: 10px
                //    - transform-origin: 40%
                //    - transform-origin: left
                //    - transform-origin: top
                if (stylePropertyValueCount == 1)
                {
                    // If the current value is a dimension then it means that only the x value was specified and y is defaulted to 'center'.
                    // eg: transform-origin: 20px;
                    if (styleProperty.values[0].valueType == StyleValueType.Dimension)
                    {
                        // If the new x value is an enum then clear data
                        // or if the new y value is anything but 'center' (default value) then clear data
                        if (newXOriginOffset != k_TransformOriginOffset_None || newYOriginOffset != TransformOriginOffset.Center)
                        {
                            isNewValue = true;
                        }
                        else
                        {
                            styleSheet.SetValue(styleProperty.values[0], value.x);
                        }
                    }
                    // if the current value is a enum then the value can be x or y depending on the value. In this case, the other value is defaulted to 'center'
                    // eg:
                    //     - transform-origin: left
                    //     - transform-origin: top
                    else
                    {
                        var offset = GetOffsetFromEnumString(styleSheet.ReadEnum(styleProperty.values[0]).ToLower());

                        // if the current specified enum value is x
                        if (IsOffsetHorizontal(offset))
                        {
                            // if the new x value is a dimension then clear data
                            if (newXOriginOffset == k_TransformOriginOffset_None)
                            {
                                isNewValue = true;
                            }
                            // otherwise, if it is an enum value
                            else
                            {
                                // If the new x value is 'center' and that the new y is an enum but 'center' then write the new y instead
                                if (newXOriginOffset == TransformOriginOffset.Center && newYOriginOffset != k_TransformOriginOffset_None && newYOriginOffset != TransformOriginOffset.Center)
                                {
                                    styleSheet.SetValue(styleProperty.values[0], newYOriginOffset);
                                }
                                // if the new y value is anything but 'center' (default value) then clear data
                                else if (newYOriginOffset != TransformOriginOffset.Center)
                                {
                                    isNewValue = true;
                                }
                                else
                                {
                                    styleSheet.SetValue(styleProperty.values[0], newXOriginOffset);
                                }
                            }
                        }
                        // If the current specified enum value is on y
                        else
                        {
                            // If the new y value is a dimension then clear data
                            if (newYOriginOffset == k_TransformOriginOffset_None)
                            {
                                isNewValue = true;
                            }
                            else
                            {
                                // If the new y value is 'center' then clear data 
                                if (newYOriginOffset == TransformOriginOffset.Center)
                                {
                                    // If the new x value is an enum then write it instead
                                    if (newXOriginOffset == k_TransformOriginOffset_None)
                                    {
                                        styleSheet.SetValue(styleProperty.values[0], newXOriginOffset);
                                    }
                                    else
                                    {
                                        isNewValue = true;
                                    }
                                }
                                // if the new x value is anything but 'center' then clear data...
                                else if (newXOriginOffset != TransformOriginOffset.Center)
                                {
                                    isNewValue = true;
                                }
                                // otherwise if it is a enum...
                                else
                                {
                                    styleSheet.SetValue(styleProperty.values[0], newYOriginOffset);
                                }
                            }
                        }
                    }
                }
                // if two values are specified
                // eg: 
                //    - transform-origin: 10px 40% [z optionally]
                //    - transform-origin: 40% top
                //    - transform-origin: left 30%
                //    - transform-origin: top 
                else if (stylePropertyValueCount <= 3)
                {
                    int xIndex = 0;
                    int yIndex = 1;

                    // If the first value is enum but a horizontal enum then flip the indexes
                    if (styleProperty.values[0].valueType == StyleValueType.Enum && !IsOffsetHorizontal(GetOffsetFromEnumString(styleSheet.ReadEnum(styleProperty.values[0]).ToLower())))
                    {
                        xIndex = 1;
                        yIndex = 0;
                    }

                    // if the current x is a dimension and the new x is an enum
                    // or if the current x is an enum and the new x is an dimension
                    // or if the current y is a dimension and the new y is an enum
                    // or if the current y is an enum and the new y is an dimension
                    // then clear data
                    if ((styleProperty.values[xIndex].valueType == StyleValueType.Dimension && newXOriginOffset != k_TransformOriginOffset_None)
                        || (styleProperty.values[xIndex].valueType == StyleValueType.Enum && newXOriginOffset == k_TransformOriginOffset_None)
                        || (styleProperty.values[yIndex].valueType == StyleValueType.Dimension && newYOriginOffset != k_TransformOriginOffset_None)
                        || (styleProperty.values[yIndex].valueType == StyleValueType.Enum && newYOriginOffset == k_TransformOriginOffset_None))
                    {
                        isNewValue = true;
                    }
                    // if the y value is 'center' then clear data to ignore it because it matches the default value
                    else if (newYOriginOffset == TransformOriginOffset.Center)
                    {
                        isNewValue = true;
                    }
                    // The x value is 'center' and the y value is any enum but 'center' then clear data to only add the y value 
                    else if (xIndex == 0 && newXOriginOffset == TransformOriginOffset.Center && newYOriginOffset != k_TransformOriginOffset_None && newYOriginOffset != TransformOriginOffset.Center)
                    {
                        isNewValue = true;
                    }
                    else
                    {
                        if (styleProperty.values[xIndex].valueType == StyleValueType.Dimension)
                            styleSheet.SetValue(styleProperty.values[xIndex], value.x);
                        else
                            styleSheet.SetValue(styleProperty.values[xIndex], newXOriginOffset);

                        if (styleProperty.values[yIndex].valueType == StyleValueType.Dimension)
                            styleSheet.SetValue(styleProperty.values[yIndex], value.y);
                        else
                            styleSheet.SetValue(styleProperty.values[yIndex], newYOriginOffset);
                    }
                }
                else
                {
                    isNewValue = true;
                }

                if (isNewValue)
                {
                    Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                    styleProperty.values = new StyleValueHandle[0];
                }
            }

            // if the property is not already specified then ...
            if (isNewValue)
            {
                // if the x value is  a dimension than add it
                if (newXOriginOffset == k_TransformOriginOffset_None)
                    styleSheet.AddValue(styleProperty, value.x);
                // otherwise, if it is an enum ...
                else
                {
                    // The x value is 'center' and the y value is any enum but 'center' then only add the y value 
                    if (newXOriginOffset == TransformOriginOffset.Center && newYOriginOffset != k_TransformOriginOffset_None && newYOriginOffset != TransformOriginOffset.Center)
                    {
                        styleSheet.AddValue(styleProperty, newYOriginOffset);
                        return true;
                    }
                    styleSheet.AddValue(styleProperty, newXOriginOffset);
                }

                if (newYOriginOffset == k_TransformOriginOffset_None)
                    styleSheet.AddValue(styleProperty, value.y);
                // otherwise, if it is an enum ...
                else
                {
                    // The y value is 'center' then ignore it as it matches the default value 
                    if (newYOriginOffset != TransformOriginOffset.Center)
                    {
                        styleSheet.AddValue(styleProperty, newYOriginOffset);
                        return true;
                    }
                }
            }
            return isNewValue;
        }

        void UpdateSelector()
        {
            float posX = float.NaN;
            float posY = float.NaN;

            // Show the indicator if x and y are 0
            if (value.x.unit == Dimension.Unit.Pixel)
            {
                if (Mathf.Approximately(value.x.value, 0))
                    posX = 0;
            }
            else
            {
                posX = value.x.value / 100;
            }

            if (value.y.unit == Dimension.Unit.Pixel)
            {
                if (Mathf.Approximately(value.y.value, 0))
                    posY = 0;
            }
            else
            {
                posY = value.y.value / 100;
            }

            m_Selector.originX = posX;
            m_Selector.originY = posY;
        }
    }
}
