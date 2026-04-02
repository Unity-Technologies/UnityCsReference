// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class LibraryTypeKey : IEquatable<LibraryTypeKey>
    {
        /// <summary>
        /// Hold the element type.
        /// </summary>
        public Type type;

        /// <summary>
        /// Contains a unique identifier of this type.
        /// </summary>
        public string id;

        /// <summary>
        /// Contains the display name of this type.
        /// </summary>
        public string name;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The element type.</param>
        public LibraryTypeKey(Type type) : this(type, null, null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The element type.</param>
        /// <param name="id">The unique identifier of this type.</param>
        public LibraryTypeKey(Type type, string id) : this(type, id, GenerateDisplayName(type, id))
        {
        }

        /// <summary>
        /// Constructor with an explicit display name.
        /// </summary>
        /// <param name="type">The element type.</param>
        /// <param name="id">The unique identifier of this type.</param>
        /// <param name="name">The name to be displayed of this type.</param>
        public LibraryTypeKey(Type type, string id, string name)
        {
            this.type = type;
            this.id = id;
            this.name = name;
        }

        public bool Equals(LibraryTypeKey other)
        {
            return other != null &&
                   type == other.type &&
                   string.Equals(id, other.id, StringComparison.Ordinal) &&
                   string.Equals(name, other.name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id);
        }

        /// <summary>
        /// Find and use the UXML custom attribute name if present, otherwise we keep the type's name.
        /// </summary>
        static string GenerateDisplayName(Type type, string id)
        {
            if (type == null)
                return null;

            var name = type.Name;
            var uxmlElementAttribute = type.GetCustomAttribute<UxmlElementAttribute>();

            if (uxmlElementAttribute != null && !string.IsNullOrEmpty(uxmlElementAttribute.name) && uxmlElementAttribute.name != type.Name)
            {
                name = id != null && id.EndsWith(uxmlElementAttribute.name, StringComparison.Ordinal) ? uxmlElementAttribute.name : name;
            }

            return UnityEditor.ObjectNames.NicifyVariableName(name);
        }
    }
}
