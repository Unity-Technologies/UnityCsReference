// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Controls the visibility of the <seealso cref="UxmlElementAttribute">UxmlElement</seealso> in the UI Builder Library projects tab.
    /// </summary>
    public enum LibraryVisibility
    {
        /// <summary>
        /// The element's visibility is not specified and is determined by the UI Builder.
        /// Unity hides elements with namespaces that start with "Unity", "UnityEngine", and "UnityEditor".
        /// </summary>
        Default,

        /// <summary>
        /// The element is visible in the UI Builder Library.
        /// </summary>
        Visible,

        /// <summary>
        /// The element is not visible in the UI Builder Library.
        /// </summary>
        Hidden
    }

    /// <summary>
    /// Declares a custom control.
    /// </summary>
    /// <remarks>
    /// To create a custom control, you must add the UxmlElement attribute to the custom control class definition.
    /// You must declare the custom control class as a partial class and inherit it from <see cref="VisualElement"/>
    /// or one of its derived classes.
    /// When an element is marked with the UxmlElement attribute, a corresponding <see cref="UxmlSerializedData"/>
    /// class is generated in the partial class.
    /// This data class contains a <see cref="SerializeField"/> for each field or property that was marked with the
    /// <see cref="UxmlAttributeAttribute"/> attribute.
    /// This serialized data allows for the element to be serialized from UXML and supports editing in the Attributes field of the
    /// Inspector window in  the UI Builder.
    /// The UxmlSerializedData includes a generated <see cref="UxmlSerializedData.CreateInstance"/> method that uses the default constructor. You can use the <see cref="UxmlCreateInstanceMethodAttribute"/> to replace the default behaviour and provide your own creation method.
    /// By default, the custom control appears in the Library tab in UI Builder.
    /// The @@visibility@@ field can be used to control the visibility of the custom control in the Library tab.
    ///
    /// For an example of migrating a custom control from `UxmlFactory` and `UxmlTraits` to the `UxmlElement` and `UxmlAttributes` system,
    /// refer to [[wiki:UpgradeGuideUnity6#enhanced-custom-controls-creation-with-uxml|Enhanced custom controls creation with UXML]].
    /// </remarks>
    /// <example>
    /// The following example creates a custom control with multiple attributes:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_ExampleElement.cs"/>
    /// </example>
    /// <example>
    /// The following UXML document uses the custom control:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_ExampleElement.uxml"/>
    /// </example>
    /// <example>
    /// When you create a custom control, the default name used in UXML and UI Builder is the element type name (C# class name).
    /// However, you can customize the name to make it easier to refer to the element.
    ///\\
    ///\\
    ///__Note__: You are still required to reference the classes' namespace in UXML.
    /// To create a custom name for an element, provide a value to the @@name@@ property.
    ///\\
    /// For example, if you create the following custom button:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_CustomButtonElement.cs"/>
    /// </example>
    /// <example>
    /// You can then reference the custom button in UXML with the custom name or its type:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_CustomButtonElement.uxml"/>
    /// </example>
    /// <example>
    /// Certain namespaces in Unity, specifically those starting with Unity, UnityEngine, and UnityEditor, are reserved.
    /// As a result, the elements within these namespaces are hidden from the UI Builder library by default.
    /// However, you can override this behavior by explicitly configuring the visibility of these elements:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_LibraryVisibility.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UxmlElementAttribute : Attribute
    {
        /// <summary>
        /// Provides a custom name for an element.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Controls the visibility of the element in the UI Builder Library.
        /// </summary>
        public LibraryVisibility visibility = LibraryVisibility.Default;

        /// <summary>
        /// Custom path to organize the element in UI Toolkit authoring tools (UI Builder Library, context menus, and UI Library).
        /// Sub-paths can be created by using a forward slash (/).
        /// If no value is provided, the element namespace will be used in the UI Builder Library and UI Library window.
        /// </summary>
        public string libraryPath;

        /// <summary>
        /// List of allowed children types for an element.
        /// </summary>
        internal readonly Type[] supportedChildTypes;

        /// <summary>
        /// Exposes a type of VisualElement to UXML and UI Builder
        /// </summary>
        public UxmlElementAttribute() : this(null)
        { }

        /// <summary>
        /// Declares a custom control with a custom element name.
        /// </summary>
        /// <param name="uxmlName">Provides a custom name for the element.</param>
        public UxmlElementAttribute(string uxmlName) : this(uxmlName, null)
        { }

        /// <summary>
        /// Declares a custom control with a custom element name and a list of supported child types.
        /// </summary>
        /// <param name="uxmlName">The name to use for the element in UXML, such as `my-custom-element`.</param>
        /// <param name="supportedTypes">An array of supported child element types. This can be passed as individual <c>Type</c> arguments.</param>
        public UxmlElementAttribute(string uxmlName, params Type[] supportedTypes)
        {
            name = uxmlName;
            supportedChildTypes = supportedTypes;
        }
    }

    /// <summary>
    /// Marks the generated <see cref="UxmlSerializedData"/> class as a partial class, allowing you to extend it such as when using <see cref="IUxmlSerializedDataCustomAttributeHandler"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class UxmlPartialSerializedDataAttribute : Attribute
    {
    }

    /// <summary>
    /// Declares a method to create instances in place of the default constructor.
    /// </summary>
    /// <remarks>
    /// Use this to provide custom instance creation logic, such as object pooling or dependency injection.
    /// The method must meet the following requirements:
    ///
    ///- Be a static method defined on the element type.
    ///- Have public or internal accessibility.
    ///- Accept no parameters.
    ///- Not be generic.
    ///- Return an instance of the element type (or null to fall back to the default constructor).
    ///
    /// If this attribute is not found on the element, the source generator uses the default constructor (`new T()`).
    /// If the method returns null, the source generator uses the default constructor as a fallback.
    /// </remarks>
    /// <example>
    /// The following example uses a custom creation method for object pooling:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlElement_CreateInstanceMethod.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class UxmlCreateInstanceMethodAttribute : Attribute
    {
    }

    ///<summary>
    /// Indicates that the generated field should have internal access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal sealed class UxmlInternalFieldAttribute : Attribute
    {
    }

    /// <summary>
    /// Declares that a field or property is associated with a UXML attribute.
    /// Convenience overload, shorthand for `Query&lt;T&gt;.Build().First().`
    /// </summary>
    /// <remarks>
    /// You can use the UxmlAttribute attribute to declare that a property or field is associated with a UXML attribute.
    /// When an element is marked with the UxmlElement attribute, a corresponding <see cref="UxmlSerializedData"/> class is
    /// generated in the partial class.
    /// This data class contains a <see cref="SerializeField"/> for each field or property that's marked with the UxmlAttribute attribute.
    /// When a field or property is associated with a UXML attribute, all of its attributes are transferred over to the serialized version.
    /// This allows for the support of custom property drawers and decorator attributes, such as <see cref="HeaderAttribute"/>, <see cref="TextAreaAttribute"/>,
    /// <see cref="RangeAttribute"/>, <see cref="TooltipAttribute"/>, and so on.
    /// </remarks>
    /// <example>
    /// The following example creates a custom control with custom attributes:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyElement.cs"/>
    /// </example>
    /// <example>
    /// Unity objects can also be UxmlAttributes and will contain a reference to the asset file when serialized as UXML.
    /// The following example creates a custom control with custom attributes.
    /// The types of attributes are UXML template and texture:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_CustomElement.cs"/>
    /// </example>
    /// <example>
    /// By default, when resolving the attribute name, the field or property name splits into lowercase words connected by hyphens.
    /// The original uppercase characters in the name are used to denote where the name should be split. For example, if the property name
    /// is @@myIntValue@@, the corresponding attribute name would be @@my-int-value@@.
    /// You can customize the attribute name through the UxmlAttribute name argument.
    /// The following example creates a custom control with customized attribute names:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_CustomAttributeNameExample.cs"/>
    /// </example>
    /// <example>
    /// Example UXML:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_CustomAttributeNameExample.uxml"/>
    /// </example>
    /// <example>
    /// If you've changed the name of an attribute and want to ensure that UXML files with the previous attribute name can still
    /// be loaded by UI Builder, use the @@obsoleteNames@@ argument.
    /// This argument matches attributes in the UXML to be applied to the attribute during serialization.
    /// UI Builder uses the new name when loading the UXML file.
    /// The following example creates a custom control with custom attributes that uses obsolete attribute names:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_CharacterDetails.cs"/>
    /// </example>
    /// <example>
    /// The following example UXML uses the obsolete names:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_CharacterDetails.uxml"/>
    /// </example>
    /// <example>
    /// When you create each SerializeField, all attributes are copied across.
    /// This allows you to use decorators and custom property drawers on fields in the UI Builder.
    /// The following example uses a custom control with decorators on its attribute fields:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_ExampleText.cs"/>
    /// </example>
    /// <example>
    /// The UI Builder displays the attributes with the decorators:
    ///
    ///
    ///{img UIB-decorators.png}
    ///
    /// The following example creates a custom control with a custom property drawer:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyDrawerExample.cs"/>
    /// </example>
    /// <example>
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyDrawerAttributePropertyDrawer.cs"/>
    /// </example>
    /// <example>
    ///{img UIB-propertydrawer.gif}
    ///
    /// You can use struct or class instances as attributes and even lists of struct or class instances in UXML.
    /// However, they must be convertible to and from a string and you must declare a [[UIElements.UxmlAttributeConverter_1]] to support this conversion.
    /// For an example of a field that supports more complex data, refer to [[UIElements.UxmlObjectReferenceAttribute]].
    /// The following example shows how a class instance can support a property and list:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithData.cs"/>
    /// </example>
    /// <example>
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithDataConverter.cs"/>
    /// </example>
    /// <example>
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_MyClassWithDataConverter.uxml"/>
    /// </example>
    /// <example>
    /// Strings can contain commas, encoded as @@%2C@@ in the UXML.
    /// When you authoring UXML directly, use the encoded form @@%2C@@ to ensure commas are not mistaken for list separators.
    /// The following C# code defines a custom control, MyStringListElement, with a @@my-string@@ attribute which represents a List of strings.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_StringList.cs"/>
    /// </example>
    /// <example>
    /// The following UXML example uses @@%2C@@ to separate words in the string list:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_StringList.uxml"/>
    /// </example>
    /// <example>
    /// When an attribute shares the same UXML name as an attribute from a child class,
    /// it takes precedence and overrides that field, appearing in the inspector instead.
    /// With this feature, you can substitute a field with a different type or a custom property.
    /// The following example demonstrates how to replace the IntegerField value with a SliderField.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_OverrideExample.cs"/>
    /// </example>
    /// <example>
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlAttribute_OverrideExampleEditor.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UxmlAttributeAttribute : Attribute
    {
        /// <summary>
        /// Provides a custom UXML name to the attribute.
        /// </summary>
        public string name;

        /// <summary>
        /// Provides support for obsolete UXML attribute names.
        /// </summary>
        public string[] obsoleteNames;

        /// <summary>
        /// Declares that a field or property is associated with a UXML attribute.
        /// </summary>
        public UxmlAttributeAttribute() : this(null, null)
        { }

        /// <summary>
        /// Declares that a field or property is associated with a UXML attribute.
        /// </summary>
        /// <param name="name">The name of the UXML attribute.</param>
        public UxmlAttributeAttribute(string name) : this(name, null)
        { }

        /// <summary>
        /// Declares that a field or property is associated with a UXML attribute.
        /// </summary>
        /// <param name="name">The name of the UXML attribute.</param>
        /// <param name="obsoleteNames">The Obsolete UXML attribute names.</param>
        public UxmlAttributeAttribute(string name, params string[] obsoleteNames)
        {
            this.name = name;
            this.obsoleteNames = obsoleteNames;
        }
    }

    /// <summary>
    /// Used to indicate the data binding path for an attribute, used by UI Builder.
    /// </summary>
    internal class UxmlAttributeBindingPathAttribute : Attribute
    {
        public string path { get; private set; }

        public UxmlAttributeBindingPathAttribute(string bindingPath)
        {
            path = bindingPath;
        }
    }

    /// <summary>
    /// Provides information about the expected type when applied to a Type field or property that has the <see cref="UxmlAttributeAttribute"/> attribute.
    /// </summary>
    /// <remarks>
    /// When defining a Type field or property with the <see cref="UxmlAttributeAttribute"/> in Unity, you can use the UxmlTypeReference
    /// attribute to specify the base type that the value should inherit from.
    /// This allows you to provide additional information about the expected type of the value and helps Unity ensure that the
    /// correct type is assigned to the attribute.
    /// </remarks>
    /// <example>
    /// The following example creates a custom control and restricts the attribute type to only accept values that are derived from [[VisualElement]]:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlTypeReferenceAttribute_TypeRestrictionExample.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class UxmlTypeReferenceAttribute : PropertyAttribute
    {
        /// <summary>
        /// The base type that the value inherits from.
        /// </summary>
        public Type baseType;

        /// <summary>
        /// Provides information about the expected type when applied to a Type field or property that has the <see cref="UxmlAttributeAttribute"/> attribute.
        /// </summary>
        public UxmlTypeReferenceAttribute() : this(null)
        { }

        /// <summary>
        /// Provides information about the expected type when applied to a Type field or property that has the <see cref="UxmlAttributeAttribute"/> attribute.
        /// </summary>
        /// <param name="baseType">The base type that the value inherits from.</param>
        public UxmlTypeReferenceAttribute(Type baseType)
        {
            this.baseType = baseType;
        }
    }

    /// <summary>
    /// Used for fields that are serialized but do not come from UXML data, such as <see cref="UxmlSerializedData.uxmlAssetId"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class UxmlIgnoreAttribute : Attribute
    {
    }

    /// <summary>
    /// Declares that a class can be instantiated from UXML and contain UXML attributes.
    /// The UxmlSerializedData includes a generated <see cref="UxmlSerializedData.CreateInstance"/> method that uses the default constructor. You can use the <see cref="UxmlCreateInstanceMethodAttribute"/> to replace the default behaviour and provide your own creation method.
    /// </summary>
    /// <remarks>
    /// A UXML object is a class that can be instantiated from UXML and contain UXML attributes.
    ///  By utilizing the <see cref="UxmlObjectReferenceAttribute"/> attribute, you can use a UXML object to associate complex data with a field, surpassing the capabilities of a single <see cref="UxmlAttributeAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UxmlObjectAttribute : Attribute
    {
    }

    /// <summary>
    /// Declares that a field or property is associated with nested UXML objects.
    /// </summary>
    /// <remarks>
    /// You can utilize the UxmlObjectReferenceAttribute to indicate that a property or field is
    /// linked to one or multiple UXML objects.
    /// By adding the <see cref="UxmlObjectAttribute"/> attribute to a class, you can declare a UXML object.
    /// You can use these UXML objects to associate complex data with a field,
    /// surpassing the capabilities of a single <see cref="UxmlAttributeAttribute"/>.
    /// The field type must be a UXML object or an interface. When using an interface, only UXML Object types are valid for UXML serialization.
    /// </remarks>
    /// <example>
    /// The following example shows a common use case for the UxmlObjectReferenceAttribute. It uses the attribute to associate a list of UXML objects with a field or property:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_MyColoredListField.cs"/>
    /// </example>
    /// <example>
    /// Example UXML:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_MyColoredListField.uxml"/>
    /// </example>
    /// <example>
    /// You can employ a base type and incorporate derived types as UXML objects.
    /// The following example customizes a button to exhibit different behaviors when clicked,
    /// such as displaying a label or playing a sound effect.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_InheritanceExample.cs"/>
    /// </example>
    /// <example>
    /// Example UXML:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_InheritanceExample.uxml"/>
    /// </example>
    /// <example>
    /// You can also use an interface with UXML objects that implement it.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_InterfaceExample.cs"/>
    /// </example>
    /// <example>
    /// The following example creates multiple UxmlObjects types with custom property drawers.
    /// It uses UxmlObjects to describe schools, teachers, and students:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_SchoolDistrict.cs"/>
    /// </example>
    /// <example>
    /// You can use a custom property drawer to display the UXML objects in the Inspector.
    /// The property drawer must be for the UxmlObject's UxmlSerializedData type.
    /// For example, to create a property drawer for the UxmlObject Student, the drawer must be for the type Studen.UxmlSerializedData.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_SchoolDistrictPropertyDrawers.cs"/>
    /// </example>
    /// <example>
    /// Example UXML:
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UxmlObject_SchoolDistrict.uxml"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class UxmlObjectReferenceAttribute : Attribute
    {
        /// <summary>
        /// The name of the nested UXML element that the UXML Objects are serialized to. __Note__: A null or empty value will result in the objects being serialized into the root.
        /// </summary>
        public string name;

        /// <summary>
        /// In UI Builder, when adding a UXML Object to a field that has multiple derived types,
        /// a dropdown list appears with a selection of available types that can be added to the field. By default,
        /// this list comprises all types that inherit from the UXML object type. You can use a parameter
        /// to specify a list of accepted types to be displayed, rather than showing all available types
        /// </summary>
        public Type[] types;

        /// <summary>
        /// Declares that a field or property is associated with nested UXML objects.
        /// </summary>
        public UxmlObjectReferenceAttribute() : this(null, null)
        {
        }

        /// <summary>
        /// Declares that a field or property is associated with nested UXML objects.
        /// </summary>
        /// <param name="uxmlName">The name of the nested UXML element that the UXML Objects are serialized to. __Note__: This field can not be null or empty.</param>
        public UxmlObjectReferenceAttribute(string uxmlName) : this(uxmlName, null)
        {
            if (uxmlName == "null")
                throw new ArgumentException("UxmlObjectReferenceAttribute name cannot be \"null\".");
        }

        /// <summary>
        /// Declares that a field or property is associated with nested UXML objects.
        /// </summary>
        /// <param name="uxmlName">The name of the nested UXML element that the UXML Objects are serialized to. __Note__: A null or empty value will result in the objects being serialized into the root.</param>
        /// <param name="acceptedTypes">In UI Builder, when adding a UXML Object to a field that has multiple derived types,
        /// a dropdown list appears with a selection of available types that can be added to the field. By default,
        /// this list comprises all types that inherit from the UXML object type. You can use a parameter
        /// to specify a list of accepted types to be displayed, rather than showing all available types.</param>
        public UxmlObjectReferenceAttribute(string uxmlName, params Type[] acceptedTypes)
        {
            name = uxmlName;
            types = acceptedTypes;
        }
    }
}
