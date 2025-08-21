// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// Builds a list of types given inputs from various overloads of <see cref="ObjectSelector.Show"/>,
    /// encapsulating all workarounds and special cases that need to be handled.
    /// </summary>
    /// <remarks>
    /// This is generally called from various versions of <see cref="EditorGUI.ObjectField"/> and <see cref="ObjectField.ShowObjectSelector"/>.
    /// </remarks>
    class RequiredTypeList
    {
        private static readonly Regex s_MatchPPtrTypeName = new Regex(@"PPtr\<(\w+)\>");

        public IReadOnlyList<string> typeNames => m_Names;
        public IReadOnlyList<Type> types => m_Types;
        public IReadOnlyList<Type> typesForDisplay => m_DisplayTypes;

        SerializedProperty m_Property;
        Type m_TypeFromProperty;

        List<Type> m_DisplayTypes = new();
        List<string> m_Names = new();
        List<Type> m_Types = new();

        public RequiredTypeList(IEnumerable<Type> requiredTypes, SerializedProperty property)
        {
            m_Property = property;

            foreach(var type in requiredTypes)
            {
                if (type is { IsInterface: true })
                {
                    foreach (var implementedType in TypeCache.GetTypesDerivedFrom(type))
                    {
                        AddType(implementedType);
                    }
                }
                else
                {
                    AddType(type);
                }

                if (type != null)
                    m_DisplayTypes.Add(type);
            }

            if (m_TypeFromProperty != null)
                m_DisplayTypes.Add(m_TypeFromProperty);

            if (m_DisplayTypes.Count == 0)
                throw new ArgumentException("No valid types in required type list");
        }

        public string GenerateTitleContent()
        {
            var text = "Select " + ObjectNames.NicifyVariableName(m_DisplayTypes[0].Name);

            for (int i = 1; i < m_DisplayTypes.Count; i++)
            {
                var typeName = ObjectNames.NicifyVariableName(m_DisplayTypes[i].Name);
                text += (i == m_DisplayTypes.Count - 1 ? " or " : ", ") + typeName;
            }

            return text;
        }

        void AddType(Type requiredType)
        {
            requiredType ??= m_TypeFromProperty;

            // This handles elements of the required type list that are null,
            // which is a case previously handled when this required type list
            // is built from a SerializedProperty.
            // In this case, we first try to return a field info from the property,
            // if that fails - try to match the string name against the pptr type name.
            if (requiredType == null)
            {
                ScriptAttributeUtility.GetFieldInfoFromProperty(m_Property, out m_TypeFromProperty);

                requiredType ??= m_TypeFromProperty;

                if (requiredType == null)
                {
                    // case 951876: built-in types do not actually have reflectable fields, so their object types must be extracted from the type string
                    // this works because built-in types will only ever have serialized references to other built-in types, which this window's filter expects as unqualified names
                    var propertyTypeName = s_MatchPPtrTypeName.Match(m_Property.type).Groups[1].Value;
                    requiredType = m_TypeFromProperty = GetUnityObjectType(propertyTypeName);
                }
            }

            if (requiredType == null)
                return;

            // type filter requires unqualified names for built-in types, but will prioritize them over user types, so ensure user types are namespace-qualified
            bool isUser =
                typeof(ScriptableObject).IsAssignableFrom(requiredType) ||
                typeof(MonoBehaviour).IsAssignableFrom(requiredType);

            string typeName = isUser ? requiredType.FullName : requiredType.Name;

            m_Names.Add(typeName);
            m_Types.Add(requiredType);
        }

        static Type GetUnityObjectType(string typeName)
        {
            var objectTypes = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>();
            foreach (var objectType in objectTypes)
            {
                if (objectType.FullName == typeName || objectType.Name == typeName)
                    return objectType;
            }
            return null;
        }
    }
}
