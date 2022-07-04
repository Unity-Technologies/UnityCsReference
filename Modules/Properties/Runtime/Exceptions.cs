// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    /// <summary>
    /// The exception that is thrown when trying to visit a container with no property bag.
    /// </summary>
    [Serializable]
    public class MissingPropertyBagException : Exception
    {
        /// <summary>
        /// The type which triggered the exception.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingPropertyBagException"/> class with a specified type.
        /// </summary>
        /// <param name="type">The type for which no property bag was found.</param>
        public MissingPropertyBagException(Type type) : base(GetMessageForType(type))
        {
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingPropertyBagException"/> class with a specified type and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="type">The type for which no property bag was found.</param>
        /// <param name="inner">The inner exception reference.</param>
        public MissingPropertyBagException(Type type, Exception inner) : base(GetMessageForType(type), inner)
        {
            Type = type;
        }

        static string GetMessageForType(Type type)
        {
            return $"No PropertyBag was found for Type=[{type.FullName}]. Please make sure all types are declared ahead of time using [{nameof(GeneratePropertyBagAttribute)}], [{nameof(GeneratePropertyBagsForTypeAttribute)}] or [{nameof(GeneratePropertyBagsForTypesQualifiedWithAttribute)}]";
        }
    }

    /// <summary>
    /// The exception that is thrown when trying to visit an invalid container type.
    /// </summary>
    [Serializable]
    public class InvalidContainerTypeException : Exception
    {
        /// <summary>
        /// The type which triggered the exception.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingPropertyBagException"/> class with a specified type.
        /// </summary>
        /// <param name="type">The invalid container type.</param>
        public InvalidContainerTypeException(Type type) : base(GetMessageForType(type))
        {
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingPropertyBagException"/> class with a specified type and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="type">The invalid container type.</param>
        /// <param name="inner">The inner exception reference.</param>
        public InvalidContainerTypeException(Type type, Exception inner) : base(GetMessageForType(type), inner)
        {
            Type = type;
        }

        static string GetMessageForType(Type type)
        {
            return $"Invalid container Type=[{type.Name}.{type.Name}]";
        }
    }

    /// <summary>
    /// The exception that is thrown when trying to resolve an invalid path.
    /// </summary>
    [Serializable]
    public class InvalidPathException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPathException"/> class with a specified path.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InvalidPathException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPathException"/> class with a specified type and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception reference.</param>
        public InvalidPathException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
