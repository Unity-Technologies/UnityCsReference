// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper class to get the uss name of a <see cref="TypeHandle"/>.
    /// </summary>
    [UnityRestricted]
    internal class TypeHandleInfos
    {
        readonly Dictionary<string, string> m_CustomUssNames = new Dictionary<string, string>();

        /// <summary>
        /// Create a new instance of the <see cref="TypeHandleInfos"/> class.
        /// </summary>
        public TypeHandleInfos()
        {
            // Register usual types that would be badly converted by ToKebabCase()
            RegisterCustomTypeHandleUss(TypeHandle.Texture2D, "texture2d");
            RegisterCustomTypeHandleUss(TypeHandle.Texture2DArray, "texture2d-array");
            RegisterCustomTypeHandleUss(TypeHandle.Texture3D, "texture3d");
        }

        /// <summary>
        /// Register a custom uss name for a given <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="typeHandle">The type handle.</param>
        /// <param name="ussName">The uss name.</param>
        public void RegisterCustomTypeHandleUss(TypeHandle typeHandle, string ussName)
        {
            m_CustomUssNames[typeHandle.Identification] = ussName;
        }

        /// <summary>
        /// Register a custom uss name for a given <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="ussName">The uss name.</param>
        public void RegisterCustomTypeHandleUss(Type type, string ussName)
        {
            var typeHandle = type.GenerateTypeHandle();
            m_CustomUssNames[typeHandle.Identification] = ussName;
        }

        static string GetTypeName(TypeHandle typeHandle)
        {
            var typeName = typeHandle.Name;

            if (typeHandle.IsCustomTypeHandle())
            {
                // Avoid "SOMETYPE" being converted to s-o-m-e-t-y-p-e by the KebabCase algo.

                if (typeName == TypeHandle.MissingType.Identification)
                {
                    return "MissingType";
                }

                if (typeName == TypeHandle.Unknown.Identification)
                {
                    return "Unknown";
                }

                if (typeName == TypeHandle.ExecutionFlow.Identification)
                {
                    return "ExecutionFlow";
                }

                if (typeName == TypeHandle.Subgraph.Identification)
                {
                    return "Subgraph";
                }

                // Handle other internal ids in the form __ALLCAPS
                var isSpecial = true;
                if (typeName.Length > 2 && typeName[0] == '_' && typeName[1] == '_')
                {
                    for (int i = 2; i < typeName.Length; i++)
                    {
                        var t = typeName[i];
                        if (!char.IsUpper(t))
                        {
                            isSpecial = false;
                        }
                    }
                }
                else
                {
                    isSpecial = false;
                }

                if (isSpecial)
                {
                    return typeName.ToLowerInvariant();
                }
            }

            return typeName;
        }

        /// <summary>
        /// Retrieves the name of the <see cref="TypeHandle"/> for use in Unity Style Sheet (USS).
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/>.</param>
        /// <returns>The USS name to use for this <see cref="TypeHandle"/>.</returns>
        public string GetUssName(TypeHandle typeHandle)
        {
            m_CustomUssNames.TryGetValue(typeHandle.Identification, out string ussName);
            return ussName ?? GetTypeName(typeHandle).ToKebabCase();
        }

        /// <summary>
        /// Retrieves the optional additional name of the <see cref="TypeHandle"/> for use in Unity Style Sheet (USS).
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/>.</param>
        /// <returns>The additional USS name to use for this <see cref="TypeHandle"/> or null when there is none.</returns>
        public virtual string GetAdditionalUssName(TypeHandle typeHandle)
        {
            var type = typeHandle.Resolve();
            if (type.IsSubclassOf(typeof(Component)) && type != typeof(Component))
                return "component";
            if (type.IsSubclassOf(typeof(Texture)) && type != typeof(Texture))
                return "texture";
            if (typeof(IList).IsAssignableFrom(type))
                return "list";
            if (typeof(IDictionary).IsAssignableFrom(type))
                return "dictionary";
            if (type.IsEnum)
                return "enum";

            return null;
        }

        /// <summary>
        /// Removes all Unity Style Sheet (USS) classes associated with the specified <paramref name="prefix"/> and <paramref name="typeHandle"/> from the specified element.
        /// </summary>
        /// <param name="prefix">The USS class prefix.</param>
        /// <param name="element">The element.</param>
        /// <param name="typeHandle">The type handle.</param>
        /// <remarks>
        /// USS names are retrieved from <see cref="GetUssName"/> and <see cref="GetAdditionalUssName"/> using the provided <paramref name="typeHandle"/>. They are then
        /// combined with the <paramref name="prefix"/> to create USS classes. They are removed from the <paramref name="element"/>'s class list.
        /// </remarks>
        public void RemoveUssClasses(string prefix, VisualElement element, TypeHandle typeHandle)
        {
            if (!typeHandle.IsValid)
                return;

            element.RemoveFromClassList(prefix + GetUssName(typeHandle));

            var additionalTypeUssName = GetAdditionalUssName(typeHandle);
            if (additionalTypeUssName != null)
            {
                element.RemoveFromClassList(prefix + additionalTypeUssName);
            }
        }

        /// <summary>
        /// Adds Unity Style Sheet (USS) classes for this <paramref name="prefix"/> and <paramref name="typeHandle"/> on the element.
        /// </summary>
        /// <param name="prefix">The USS class prefix.</param>
        /// <param name="element">The element.</param>
        /// <param name="typeHandle">The type handle.</param>
        /// <remarks>
        /// USS names are retrieved from <see cref="GetUssName"/> and <see cref="GetAdditionalUssName"/> using the provided <paramref name="typeHandle"/>. They are then
        /// combined with the <paramref name="prefix"/> to create USS classes. They are added to the <paramref name="element"/>'s class list.
        /// </remarks>
        public void AddUssClasses(string prefix, VisualElement element, TypeHandle typeHandle)
        {
            if (!typeHandle.IsValid)
                return;

            element.AddToClassList(prefix + GetUssName(typeHandle));

            var additionalTypeUssName = GetAdditionalUssName(typeHandle);
            if (additionalTypeUssName != null)
            {
                element.AddToClassList(prefix + additionalTypeUssName);
            }
        }
    }
}
