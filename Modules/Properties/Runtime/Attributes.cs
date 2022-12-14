// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Properties
{
    /// <summary>
    /// Use this attribute to enable the source generator to run on this assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class GeneratePropertyBagsForAssemblyAttribute : Attribute
    {

    }

    /// <summary>
    /// Use this attribute to have a property generated for the member.
    /// </summary>
    /// <remarks>
    /// By default public fields will have properties generated.
    /// </remarks>
    /// <see cref="DontCreatePropertyAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [RequireAttributeUsages]
    public class CreatePropertyAttribute : RequiredMemberAttribute
    {
        /// <summary>
        /// Indicates if the property should generate a setter.
        /// </summary>
        /// <remarks>
        /// Setting this to <see langword="false"/> will not generate a setter for a readonly field or
        /// a get only property.
        /// </remarks>
        public bool ReadOnly { get; set; } = false;
    }

    /// <summary>
    /// Use this attribute to prevent have a property from being automatically generated on a public field.
    /// </summary>
    /// <see cref="CreatePropertyAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class DontCreatePropertyAttribute : Attribute
    {

    }

    /// <summary>
    /// A set of options to customize the behaviour of the code generator.
    /// </summary>
    [Flags]
    public enum TypeGenerationOptions
    {
        /// <summary>
        /// If this option is selected, no property bags will be generated.
        /// </summary>
        None = 0,

        /// <summary>
        /// If this option is selected, any inherited value types will have property bags generated.
        /// </summary>
        ValueType = 1 << 1,

        /// <summary>
        /// If this option is selected, any inherited reference types will have property bags generated.
        /// </summary>
        ReferenceType = 1 << 2,

        /// <summary>
        /// The default set of type options. This includes both <see cref="ValueType"/> and <see cref="ReferenceType"/>.
        /// </summary>
        Default = ValueType | ReferenceType
    }

    /// <summary>
    /// Use this attribute to have the properties source generator generate property bags for types implementing the specified interface.
    /// </summary>
    /// <remarks>
    /// If you need to generate a property bag for a specific type use <see cref="GeneratePropertyBagsForTypeAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GeneratePropertyBagsForTypesQualifiedWithAttribute : Attribute
    {
        /// <summary>
        /// The interface type to generate property bags for.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Options used for additional filtering.
        /// </summary>
        public TypeGenerationOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratePropertyBagsForTypesQualifiedWithAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The interface type to generate property bags for.</param>
        /// <param name="options">Additional type filtering options.</param>
        /// <exception cref="ArgumentException">The type is null or the given type is not an interface.</exception>
        public GeneratePropertyBagsForTypesQualifiedWithAttribute(Type type, TypeGenerationOptions options = TypeGenerationOptions.Default)
        {
            if (type == null)
            {
                throw new ArgumentException($"{nameof(type)} is null.");
            }

            if (!type.IsInterface)
            {
                throw new ArgumentException($"{nameof(GeneratePropertyBagsForTypesQualifiedWithAttribute)} Type must be an interface type.");
            }

            Type = type;
            Options = options;
        }
    }

    /// <summary>
    /// Use this attribute to have the source generator generate a property bag for a given type.
    /// This attribute works for the specified type ONLY, it does NOT include derived types.
    /// </summary>
    /// <remarks>
    /// If you need to generate property bags for types implementing a specific interface use <see cref="GeneratePropertyBagsForTypesQualifiedWithAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GeneratePropertyBagsForTypeAttribute : Attribute
    {
        /// <summary>
        /// The type to generate a property bag for.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratePropertyBagsForTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="type">The type to generate a property bag for.</param>
        /// <exception cref="ArgumentException">The specified type is not a valid container type.</exception>
        public GeneratePropertyBagsForTypeAttribute(Type type)
        {
            if (!TypeTraits.IsContainer(type))
                throw new ArgumentException($"{type.Name} is not a valid container type.");

            Type = type;
        }
    }

    /// <summary>
    /// Use this attribute to have the source generator generate property bags for a given type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false)]
    public class GeneratePropertyBagAttribute : Attribute
    {
    }
}
