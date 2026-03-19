// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    enum Bool
    {
        True = 1,
        False = 0
    }
    /// <summary>
    /// Base class to display a UI to edit a property or field on a <see cref="GraphElementModel"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class CustomizableModelPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class added to <see cref="CustomizableModelPropertyField"/> when it has multi lines.
        /// </summary>
        public static readonly string multilineUssClassName = ussClassName.WithUssModifier(GraphElementHelper.multilineUssModifier);

        static Dictionary<Type, Type> s_CustomPropertyFieldBuilders;

        Func<object, object> m_ValueToDisplay = null;
        Func<object, object> m_ValueFromDisplay = null;

        protected Type DisplayFieldValueType { get; set; } = null;

        protected FieldInfo InspectedField { get; }

        protected bool m_ForceUpdate;

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

                var assemblies = AssemblyCache.CachedAssemblies;
                var customPropertyBuilderTypes = TypeCache.GetTypesDerivedFrom<ICustomPropertyFieldBuilder>();

                foreach (var customPropertyBuilderType in customPropertyBuilderTypes)
                {
                    if (customPropertyBuilderType.IsGenericType ||
                        customPropertyBuilderType.IsAbstract ||
                        !customPropertyBuilderType.IsClass ||
                        !assemblies.Contains(customPropertyBuilderType.Assembly))
                        continue;

                    var interfaces = customPropertyBuilderType.GetInterfaces();
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var cpfInterface = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICustomPropertyFieldBuilder<>));
#pragma warning restore UA2001

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

        /// <summary>
        /// The label for the field.
        /// </summary>
        protected string Label { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomizableModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTarget">The target to dispatch commands to when the field is edited.</param>
        /// <param name="label">The label for the field.</param>
        /// <param name="inspectedField">The inspected <see cref="FieldInfo"/> or null.</param>
        protected CustomizableModelPropertyField(ICommandTarget commandTarget, string label, FieldInfo inspectedField)
            : base(commandTarget)
        {
            this.AddPackageStylesheet("CustomizableModelPropertyField.uss");

            Label = label ?? "";
            InspectedField = inspectedField;
        }

        protected object ValueFromDisplay(object displayValue)
        {
            return m_ValueFromDisplay != null ? m_ValueFromDisplay(displayValue) : displayValue;
        }

        protected object ValueToDisplay(object value)
        {
            return m_ValueToDisplay != null ? m_ValueToDisplay(value) : value;
        }

        /// <summary>
        /// The types that have a default field in the <see cref="CustomizableModelPropertyField"/>.
        /// </summary>
        public static readonly Type[] DefaultSupportedTypes = new[]
        {
            typeof(long),
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(Color),
            typeof(GameObject),
            typeof(Object),
            typeof(LayerMask),
            typeof(Enum),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Rect),
            typeof(char),
            typeof(AnimationCurve),
            typeof(Bounds),
            typeof(Gradient),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(RectInt),
            typeof(BoundsInt)
        };

        void CheckAttributesCompatibility(Type type, IReadOnlyList<Attribute> attributes)
        {
            if (attributes == null)
                return;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case TextAreaAttribute when type != typeof(string):
                        Debug.LogWarning(InspectedField != null
                            ? $"Attempted to use TextAreaAttribute on field '{InspectedField.Name}', which is not of type string. The attribute will be ignored."
                            : "Attempted to use TextAreaAttribute on a non-string field. The attribute will be ignored.");
                        break;
                    case EnumAttribute when type != typeof(string):
                        Debug.LogWarning(InspectedField != null
                            ? $"Attempted to use EnumAttribute on field '{InspectedField.Name}', which is not of type string array. The attribute will be ignored."
                            : "Attempted to use EnumAttribute on a non string array field. The attribute will be ignored.");
                        break;
                    case MultilineAttribute when type != typeof(string):
                        Debug.LogWarning(InspectedField != null
                            ? $"Attempted to use MultilineAttribute on field '{InspectedField.Name}', which is not of type string. The attribute will be ignored."
                            : "Attempted to use MultilineAttribute on a non-string field. The attribute will be ignored.");
                        break;
                    case BoolDropDownAttribute when type != typeof(bool):
                        Debug.LogWarning(InspectedField != null
                            ? $"Attempted to use BoolDropDownAttribute on field '{InspectedField.Name}', which is not of type bool. The attribute will be ignored."
                            : "Attempted to use BoolDropDownAttribute on a non-bool field. The attribute will be ignored.");
                        break;
                }
            }
        }

        void CreateListEditor(Type type, string fieldTooltip)
        {
            var eventType = typeof(ChangeEvent<>).MakeGenericType(type);
            var getPooledMethod = eventType.GetMethod("GetPooled", BindingFlags.Public | BindingFlags.Static);

            ListPropertyField listPropertyField = null;
            Action<IList> onListChange = (newList) =>
            {
                var evt = (EventBase)getPooledMethod.Invoke(null, new object[] { newList, newList });
                evt.target = listPropertyField; 
                listPropertyField.SendEvent(evt);
            };

            listPropertyField = new ListPropertyField(type, onListChange);
            
            var labelElement = new Label(Label);
            labelElement.tooltip = fieldTooltip;
            labelElement.AddToClassList(BaseField<object>.labelUssClassName);
            
            Setup(labelElement, listPropertyField, fieldTooltip);
        }

        /// <summary>
        /// Whether to use the list editor (ListView) for the given type. Override to restrict list UI to specific contexts (e.g. variable/constant node only).
        /// </summary>
        /// <param name="type">The constant value type.</param>
        /// <returns>True to create the list editor for this type; false to fall through to other field types.</returns>
        protected virtual bool ShouldCreateListEditorForType(Type type)
        {
            return TypeExtensions.IsListOrArray(type);
        }

        /// <summary>
        /// Creates a default field for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="fieldTooltip">The tooltip displayed when hovering on the field.</param>
        /// <param name="attributes">The <see cref="Attribute"/>s associated with the field.</param>
        /// <remarks>
        /// 'CreateDefaultFieldForType' creates a default field for the specified type, configuring the field based on the type and any associated attributes.
        /// It also displays a tooltip that provides helpful context or instructions when you hover over the field.
        /// </remarks>
        protected virtual void CreateDefaultFieldForType(Type type, string fieldTooltip, IReadOnlyList<Attribute> attributes = null)
        {
            // PF TODO Eventually, add support for nested properties, arrays and Enum Flags.

            //if (EditorGUI.HasVisibleChildFields())
            //    return CreateFoldout();

            //m_ChildrenContainer = null;

            CheckAttributesCompatibility(type, attributes);

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            bool isDelayed = attributes?.FirstOrDefault(t => t is DelayedAttribute) != null;
            #pragma warning restore UA2001


            if (TypeExtensions.IsListOrArray(type))
            {
                if (ShouldCreateListEditorForType(type))
                {
                    CreateListEditor(type, fieldTooltip);
                }
                return;
            }


            if (type == typeof(long))
            {
                Setup(new LongField { isDelayed = isDelayed }, fieldTooltip);
            }

            if (type == typeof(int))
            {
                var intField = new IntegerField { isDelayed = isDelayed };
                Setup(intField, fieldTooltip);

                if (attributes != null && attributes.HasAny(t => t is RangeAttribute))
                {
                    RangeAttribute rangeAttribute = attributes.First(t => t is RangeAttribute) as RangeAttribute;
                    var minValueAttribute = rangeAttribute.min;
                    var maxValueAttribute = rangeAttribute.max;

                    intField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue < minValueAttribute)
                            intField.value = (int)minValueAttribute;
                        else if (evt.newValue > maxValueAttribute)
                            intField.value = (int)maxValueAttribute;
                    });
                }
            }

            if (type == typeof(bool))
            {
                if (InspectedField != null && InspectedField.GetCustomAttribute<BoolDropDownAttribute>() != null)
                    Setup(new EnumField(Bool.False) { tooltip = fieldTooltip }, fieldTooltip);
                else
                    Setup(new Toggle { tooltip = fieldTooltip }, fieldTooltip);
            }

            if (type == typeof(float))
            {
                Setup(new FloatField { isDelayed = isDelayed }, fieldTooltip);
            }

            if (type == typeof(double))
            {
                Setup(new DoubleField { isDelayed = isDelayed }, fieldTooltip);
            }

            if (type == typeof(string))
            {
                TextAreaAttribute textAreaAttr = null;
                EnumAttribute enumAttr = null;
                MultilineAttribute multilineAttr = null;

                if (attributes != null)
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute is TextAreaAttribute textAreaAttribute)
                            textAreaAttr = textAreaAttribute;

                        else if (attribute is EnumAttribute enumAttribute)
                            enumAttr = enumAttribute;

                        else if (attribute is MultilineAttribute multilineAttribute)
                            multilineAttr = multilineAttribute;
                    }
                }

                if (textAreaAttr != null && multilineAttr != null)
                    Debug.LogWarning($"TextAreaAttribute and MultilineAttribute cannot be used together. MultilineAttribute will be ignored.");

                // Handle text area attribute
                if (textAreaAttr != null)
                {
                    var textAreaField = TextAreaFieldHelper.CreateTextAreaField(textAreaAttr, isDelayed);
                    textAreaField.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                    {
                        TextAreaFieldHelper.UpdateTextAreaHeight(textAreaAttr, textAreaField, textAreaField.text);
                    });
                    Setup(textAreaField, fieldTooltip);
                    return;
                }

                // Handle enum attribute
                if (enumAttr != null)
                {
                    var enumValues = enumAttr.Values;
                    var defaultValue = enumValues.Length > 0 ? enumValues.GetValue(0).ToString() : null;
                    var dropDownField = new DropdownField(new List<string>(enumValues), defaultValue) { tooltip = fieldTooltip };
                    Setup(dropDownField, fieldTooltip);
                    return;
                }

                // Other cases
                TextField textField;

                // Handle multiline attribute
                if (multilineAttr != null)
                {
                    AddToClassList(multilineUssClassName);
                    textField = new TextField() { isDelayed = isDelayed, multiline = true };
                }
                else
                {
                    textField = new TextField { isDelayed = isDelayed };
                }
                Setup(textField, fieldTooltip);
            }

            if (type == typeof(Color))
            {
                Setup(new ColorField(), fieldTooltip);
            }

            if (type == typeof(GameObject))
            {
                var field = new ObjectField { allowSceneObjects = true, objectType = type };
                Setup(field, fieldTooltip);
            }
            else if (typeof(Object).IsAssignableFrom(type)) // every Object type except GameObject
            {
                var field = new ObjectField { allowSceneObjects = false, objectType = type };
                Setup(field, fieldTooltip);
            }

            if (type == typeof(LayerMask))
            {
                Setup(new LayerMaskField(), fieldTooltip);

                m_ValueToDisplay = v => ((LayerMask)v).value;
                m_ValueFromDisplay = v => (LayerMask)(int)v;
                DisplayFieldValueType = typeof(int);
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
                    var field = new EnumField(defaultValue);
                    Setup(field, fieldTooltip);

                    m_ValueToDisplay = v =>
                    {
                        if (typeof(EnumValueReference).IsAssignableFrom(v.GetType()))
                            return ((EnumValueReference)v).ValueAsEnum();
                        // Enums in collections are not wrapped in EnumValueReference
                        return v;
                    };
                    m_ValueFromDisplay = v =>
                    {
                        if (typeof(EnumValueReference).IsAssignableFrom(v.GetType()))
                            return new EnumValueReference((Enum)v);
                        // Enums in collections are not wrapped in EnumValueReference
                        return v;
                    };
                    DisplayFieldValueType = typeof(Enum);
                }
            }

            if (type == typeof(Vector2))
            {
                Setup(new Vector2Field(), fieldTooltip);
            }

            if (type == typeof(Vector3))
            {
                Setup(new Vector3Field(), fieldTooltip);
            }

            if (type == typeof(Vector4))
            {
                Setup(new Vector4Field(), fieldTooltip);
            }

            if (type == typeof(Rect))
            {
                Setup(new RectField(), fieldTooltip);
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
                var field = new TextField { isDelayed = true };
                field.maxLength = 1;
                field.AddToClassList("unity-char-field");
                Setup(field, fieldTooltip);

                m_ValueToDisplay = v => new string((char)v, 1);
                m_ValueFromDisplay = v =>
                {
                    var s = (string)v;
                    return s.Length > 0 ? s[0] : (char)0;
                };
                DisplayFieldValueType = typeof(string);
            }

            if (type == typeof(AnimationCurve))
            {
                Setup(new CurveField() { renderMode = CurveField.RenderMode.Mesh }, fieldTooltip);
            }

            if (type == typeof(Bounds))
            {
                Setup(new BoundsField(), fieldTooltip);
            }

            if (type == typeof(Gradient))
            {
                Setup(new GradientField(), fieldTooltip);
            }

            if (type == typeof(Vector2Int))
            {
                Setup(new Vector2IntField(), fieldTooltip);
            }

            if (type == typeof(Vector3Int))
            {
                Setup(new Vector3IntField(), fieldTooltip);
            }

            if (type == typeof(RectInt))
            {
                Setup(new RectIntField(), fieldTooltip);
            }

            if (type == typeof(BoundsInt))
            {
                Setup(new BoundsIntField(), fieldTooltip);
            }
        }

        /// <summary>
        /// Configures the field so it is displayed correctly.
        /// </summary>
        /// <param name="field">The field to configure.</param>
        /// <param name="fieldTooltip">The tooltip of the field.</param>
        /// <typeparam name="TFieldValue">The type of value displayed by the field.</typeparam>
        protected void Setup<TFieldValue>(BaseField<TFieldValue> field, string fieldTooltip)
        {
            field.label = Label ?? "";
            Setup(field.labelElement, field, fieldTooltip);

            if (field is TextInputBaseField<TFieldValue> { isDelayed: false })
            {
                field.RegisterCallback<ExecuteCommandEvent>(
                    e =>
                    {
                        if (e.commandName == EventCommandNamesBridge.UndoRedoPerformed)
                            m_ForceUpdate = true;
                    }
                );
                field.RegisterCallback<BlurEvent>(_ => UpdateDisplayedValue());
                field.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        m_ForceUpdate = true;
                        UpdateDisplayedValue();
                    }
                });
            }
        }
    }
}
