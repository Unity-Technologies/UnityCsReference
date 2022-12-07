// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class to display a UI to edit a property or field on a <see cref="GraphElementModel"/>.
    /// </summary>
    abstract class CustomizableModelPropertyField : BaseModelPropertyField
    {
        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");
        public static readonly string multilineModifier = "multiline";

        public static readonly string ussClassName = "ge-model-property-field";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string inputUssClassName = ussClassName + "__input";
        public static readonly string multilineUssClassName = ussClassName.WithUssModifier(multilineModifier);

        static Dictionary<Type, Type> s_CustomPropertyFieldBuilders;

        protected FieldInfo InspectedField { get; }

        /// <summary>
        /// Tries to create an instance of a custom property field builder provided by an implementation of <see cref="ICustomPropertyFieldBuilder{T}"/>.
        /// </summary>
        /// <param name="customPropertyFieldBuilder">On exit, the custom property field buidler instance, or null if none was created.</param>
        /// <typeparam name="T">The type for which to get the custom property field builder.</typeparam>
        protected static void TryCreateCustomPropertyFieldBuilder<T>(out ICustomPropertyFieldBuilder<T> customPropertyFieldBuilder)
        {
            TryCreateCustomPropertyFieldBuilder(typeof(T), out var customPropertyDrawerNonTyped);
            customPropertyFieldBuilder = customPropertyDrawerNonTyped as ICustomPropertyFieldBuilder<T>;
        }

        /// <summary>
        /// Tries to create an instance of a custom property field builder provided by an implementation of <see cref="ICustomPropertyFieldBuilder{T}"/>.
        /// </summary>
        /// <param name="propertyType">The type for which to get the custom property field builder, or null if none was created..</param>
        /// <param name="customPropertyFieldBuilder">On exit, the custom property field builder instance.</param>
        protected static void TryCreateCustomPropertyFieldBuilder(Type propertyType, out ICustomPropertyFieldBuilder customPropertyFieldBuilder)
        {
            if (s_CustomPropertyFieldBuilders == null)
            {
                s_CustomPropertyFieldBuilders = new Dictionary<Type, Type>();

                var assemblies = AssemblyCache_Internal.CachedAssemblies_Internal.ToList();
                var customPropertyBuilderTypes = TypeCache.GetTypesDerivedFrom<ICustomPropertyFieldBuilder>();

                foreach (var customPropertyBuilderType in customPropertyBuilderTypes)
                {
                    if (customPropertyBuilderType.IsGenericType ||
                        customPropertyBuilderType.IsAbstract ||
                        !customPropertyBuilderType.IsClass ||
                        !assemblies.Contains(customPropertyBuilderType.Assembly))
                        continue;

                    var interfaces = customPropertyBuilderType.GetInterfaces();
                    var cpfInterface = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomPropertyFieldBuilder<>));

                    if (cpfInterface != null)
                    {
                        var typeParam = cpfInterface.GenericTypeArguments[0];
                        s_CustomPropertyFieldBuilders.Add(typeParam, customPropertyBuilderType);
                    }
                }
            }

            if (s_CustomPropertyFieldBuilders.TryGetValue(propertyType, out var propertyBuilderType))
            {
                customPropertyFieldBuilder = Activator.CreateInstance(propertyBuilderType) as ICustomPropertyFieldBuilder;
            }
            else
            {
                customPropertyFieldBuilder = null;
            }
        }

        float m_LabelWidthRatio;
        float m_LabelExtraPadding;

        /// <summary>
        /// The label for the field.
        /// </summary>
        protected string Label { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomizableModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="label">The label for the field.</param>
        /// <param name="inspectedField">The inspected <see cref="FieldInfo"/> or null.</param>
        protected CustomizableModelPropertyField(ICommandTarget commandTarget, string label, FieldInfo inspectedField)
        : base(commandTarget)
        {
            Label = label ?? "";

            InspectedField = inspectedField;

            AddToClassList(ussClassName);
        }

        protected virtual VisualElement CreateDefaultFieldForType(Type type, string fieldTooltip)
        {
            // PF TODO Eventually, add support for nested properties, arrays and Enum Flags.

            //if (EditorGUI.HasVisibleChildFields())
            //    return CreateFoldout();

            //m_ChildrenContainer = null;

            if (type == typeof(long))
            {
                return ConfigureField(new LongField { isDelayed = true, tooltip = fieldTooltip });
            }

            if (type == typeof(int))
            {
                return ConfigureField(new IntegerField { isDelayed = true, tooltip = fieldTooltip });
            }

            if (type == typeof(bool))
            {
                return ConfigureField(new Toggle { tooltip = fieldTooltip });
            }

            if (type == typeof(float))
            {
                return ConfigureField(new FloatField { isDelayed = true, tooltip = fieldTooltip });
            }

            if (type == typeof(double))
            {
                return ConfigureField(new DoubleField { isDelayed = true, tooltip = fieldTooltip });
            }

            if (type == typeof(string))
            {
                TextField field;
                if (InspectedField != null && InspectedField.FieldType == typeof(string) && InspectedField.GetCustomAttribute<MultilineAttribute>() != null)
                {
                    AddToClassList(multilineUssClassName);
                    field = new TextField() { isDelayed = true, tooltip = fieldTooltip, multiline = true };
                }
                else
                {
                    field = new TextField { isDelayed = true, tooltip = fieldTooltip };
                }
                return ConfigureField(field);
            }

            if (type == typeof(Color))
            {
                return ConfigureField(new ColorField { tooltip = fieldTooltip });
            }

            if (type == typeof(GameObject))
            {
                var field = new ObjectField { allowSceneObjects = true, objectType = type, tooltip = fieldTooltip };
                return ConfigureField(field);
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                var field = new ObjectField { allowSceneObjects = false, objectType = type, tooltip = fieldTooltip };
                return ConfigureField(field);
            }

            if (type == typeof(LayerMask))
            {
                return ConfigureField(new LayerMaskField { tooltip = fieldTooltip });
            }

            if (typeof(Enum).IsAssignableFrom(type))
            {
                /*if (propertyType.IsDefined(typeof(FlagsAttribute), false))
                {
                    var field = new EnumFlagsField { tooltip = fieldTooltip };
                    return ConfigureField(field);
                }
                else*/
                {
                    var enumValues = Enum.GetValues(type);
                    var defaultValue = enumValues.Length > 0 ? enumValues.GetValue(0) as Enum : null;
                    var field = new EnumField(defaultValue) { tooltip = fieldTooltip };
                    return ConfigureField(field);
                }
            }

            if (type == typeof(Vector2))
            {
                return ConfigureField(new Vector2Field { tooltip = fieldTooltip });
            }

            if (type == typeof(Vector3))
            {
                return ConfigureField(new Vector3Field { tooltip = fieldTooltip });
            }

            if (type == typeof(Vector4))
            {
                return ConfigureField(new Vector4Field { tooltip = fieldTooltip });
            }

            if (type == typeof(Rect))
            {
                return ConfigureField(new RectField { tooltip = fieldTooltip });
            } /*
            if (propertyType is SerializedPropertyType.ArraySize)
            {
                var field = new IntegerField { tooltip = fieldTooltip };
                field.SetValueWithoutNotify(property.intValue); // This avoids the OnValueChanged/Rebind feedback loop.
                field.isDelayed = true; // To match IMGUI. Also, focus is lost anyway due to the rebind.
                field.RegisterValueChangedCallback((e) => { UpdateArrayFoldout(e, this, m_ParentPropertyField); });
                return ConfigureField<IntegerField, int>(field, property);
            }*/

            if (type == typeof(char))
            {
                var field = new TextField { isDelayed = true, tooltip = fieldTooltip };
                field.maxLength = 1;
                return ConfigureField(field);
            }

            if (type == typeof(AnimationCurve))
            {
                return ConfigureField(new CurveField { tooltip = fieldTooltip });
            }

            if (type == typeof(Bounds))
            {
                return ConfigureField(new BoundsField { tooltip = fieldTooltip });
            }

            if (type == typeof(Gradient))
            {
                return ConfigureField(new GradientField { tooltip = fieldTooltip });
            }

            if (type == typeof(Vector2Int))
            {
                return ConfigureField(new Vector2IntField { tooltip = fieldTooltip });
            }

            if (type == typeof(Vector3Int))
            {
                return ConfigureField(new Vector3IntField { tooltip = fieldTooltip });
            }

            if (type == typeof(RectInt))
            {
                return ConfigureField(new RectIntField { tooltip = fieldTooltip });
            }

            if (type == typeof(BoundsInt))
            {
                return ConfigureField(new BoundsIntField { tooltip = fieldTooltip });
            }

            return null;
        }

        /// <summary>
        /// Configures the field so it is displayed correctly.
        /// </summary>
        /// <param name="field">The field to configure.</param>
        /// <typeparam name="TFieldValue">The type of value displayed by the field.</typeparam>
        /// <returns>The field.</returns>
        // Stolen from PropertyField.
        protected VisualElement ConfigureField<TFieldValue>(BaseField<TFieldValue> field)
        {
            field.label = Label ?? "";

            field.labelElement.PreallocForMoreClasses(2);
            field.labelElement.AddToClassList(labelUssClassName);
            var baseField = field.SafeQ(null, BaseField<TFieldValue>.inputUssClassName);
            baseField.PreallocForMoreClasses(2);
            baseField.AddToClassList(inputUssClassName);

            // Style this like a PropertyField
            AddToClassList(PropertyField.ussClassName);
            field.labelElement.AddToClassList(PropertyField.labelUssClassName);
            baseField.AddToClassList(PropertyField.inputUssClassName);

            // These default values are based off IMGUI
            m_LabelWidthRatio = 0.45f;
            m_LabelExtraPadding = 2.0f;

            field.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            if (!(parent is ModelInspector inspectorElement))
                return field;

            field.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                // Calculate all extra padding from the containing element's contents
                var totalPadding = resolvedStyle.paddingLeft + resolvedStyle.paddingRight +
                    resolvedStyle.marginLeft + resolvedStyle.marginRight;

                // Get inspector element padding next
                totalPadding += inspectorElement.resolvedStyle.paddingLeft +
                    inspectorElement.resolvedStyle.paddingRight +
                    inspectorElement.resolvedStyle.marginLeft +
                    inspectorElement.resolvedStyle.marginRight;

                var labelElement = field.labelElement;

                // Then get label padding
                totalPadding += labelElement.resolvedStyle.paddingLeft + labelElement.resolvedStyle.paddingRight +
                    labelElement.resolvedStyle.marginLeft + labelElement.resolvedStyle.marginRight;

                // Then get base field padding
                totalPadding += field.resolvedStyle.paddingLeft + field.resolvedStyle.paddingRight +
                    field.resolvedStyle.marginLeft + field.resolvedStyle.marginRight;

                // Not all visual input controls have the same padding so we can't base our total padding on
                // that information.  Instead we add a flat value to totalPadding to best match the hard coded
                // calculation in IMGUI
                totalPadding += m_LabelExtraPadding;

                // Formula to follow IMGUI label width settings
                var newWidth = resolvedStyle.width * m_LabelWidthRatio - totalPadding;
                if (Mathf.Abs(labelElement.resolvedStyle.width - newWidth) > Mathf.Epsilon)
                    labelElement.style.width = newWidth;
            });

            return field;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio))
            {
                m_LabelWidthRatio = labelWidthRatio;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding))
            {
                m_LabelExtraPadding = labelExtraPadding;
            }
        }
    }
}
