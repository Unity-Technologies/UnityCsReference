// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Model.Internals;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Timeline.Foundation.Model
{
    delegate void FieldDelegate<in T>(T arg, Object target);

    /// <summary>
    /// Represents a collection of <cref>Type</cref> keys and <cref>FieldDelegate</cref> values that can be filtered by a list of <cref>String</cref> fields.
    /// The (optional) fields must be found on the given <cref>Type</cref>
    /// </summary>
    /// <typeparam name="TArg">Type of objects the <cref>FieldDelegate</cref> act on</typeparam>
    class FieldDelegateLookup<TArg>
    {
        static readonly Type k_UnityEngineObjectType = typeof(Object);
        public delegate bool Comparer(SerializedObject lhs, SerializedObject rhs);

        readonly Dictionary<Type, List<FieldListDelegateContainer<TArg>>> m_Dictionary = new();

        /// <summary>
        /// Gets all the pairs of <cref>Type</cref> and the fields that have been stored for them
        /// </summary>
        /// <returns>An IEnumerable of Types and their registered fields</returns>
        public IEnumerable<(Type, string)> GetAllFields()
        {
            foreach ((Type type, List<FieldListDelegateContainer<TArg>> containers) in GetTypeDelegateContainers_Internal())
                foreach (FieldListDelegateContainer<TArg> container in containers)
                    foreach (string field in container.fields)
                        yield return (type, field);
        }

        /// <summary>
        /// Registers a <cref>FieldDelegate</cref> for <cref>Type</cref> T.
        /// The delegate is valid for modifications to any field on an object of Type T
        /// </summary>
        /// <param name="del">The <cref>FieldDelegate</cref> to register</param>
        /// <param name="customComparer">(optional) Delegate to use to compare <cref>SerializedObject</cref> in place of <cref>SerializedProperty.DataEquals</cref></param>
        /// <typeparam name="T">The Type to register the delegate to</typeparam>
        public void RegisterDelegate<T>(FieldDelegate<TArg> del, Comparer customComparer = null) where T : Object
        {
            Type type = typeof(T);
            if (!m_Dictionary.TryGetValue(type, out List<FieldListDelegateContainer<TArg>> delegates))
            {
                delegates = new List<FieldListDelegateContainer<TArg>>();
                m_Dictionary.Add(type, delegates);
            }

            delegates.Add(new FieldListDelegateContainer<TArg>(new List<string>(), del, customComparer));
        }

        /// <summary>
        /// Registers a <cref>FieldDelegate</cref> for <cref>Type</cref> T.
        /// The delegate is valid for modifications to the given field on an object of Type T
        /// </summary>
        /// <param name="field">The field name whose modifications you wish to track</param>
        /// <param name="del">The <cref>FieldDelegate</cref> to register</param>
        /// <param name="customComparer">(optional) Delegate to use to compare <cref>SerializedObject</cref> in place of <cref>SerializedProperty.DataEquals</cref></param>
        /// <typeparam name="T">The Type to register the delegate to</typeparam>
        public void RegisterDelegate<T>(string field, FieldDelegate<TArg> del, Comparer customComparer = null) where T : Object
        {
            Type type = typeof(T);
            if (!m_Dictionary.TryGetValue(type, out List<FieldListDelegateContainer<TArg>> delegates))
            {
                delegates = new List<FieldListDelegateContainer<TArg>>();
                m_Dictionary.Add(type, delegates);
            }

            delegates.Add(new FieldListDelegateContainer<TArg>(new List<string> { field }, del, customComparer));
        }

        /// <summary>
        /// Registers a <cref>FieldDelegate</cref> for <cref>Type</cref> T.
        /// The delegate is valid for modifications to any of the given fields on an object of Type T
        /// </summary>
        /// <param name="fields">The list of fields name whose modifications you wish to track</param>
        /// <param name="del">The <cref>FieldDelegate</cref> to register</param>
        /// <param name="customComparer">(optional) Delegate to use to compare <cref>SerializedObject</cref> in place of <cref>SerializedProperty.DataEquals</cref></param>
        /// <typeparam name="T">The Type to register the delegate to</typeparam>
        public void RegisterDelegate<T>(IReadOnlyList<string> fields, FieldDelegate<TArg> del, Comparer customComparer = null) where T : Object
        {
            Type type = typeof(T);
            if (!m_Dictionary.TryGetValue(type, out List<FieldListDelegateContainer<TArg>> delegates))
            {
                delegates = new List<FieldListDelegateContainer<TArg>>();
                m_Dictionary.Add(type, delegates);
            }

            delegates.Add(new FieldListDelegateContainer<TArg>(fields, del, customComparer));
        }

        internal bool TryGetContainers_Internal(Type type, out List<FieldListDelegateContainer<TArg>> commands)
        {
            return TryGetContainers(type, true, out commands);
        }

        internal IReadOnlyDictionary<Type, List<FieldListDelegateContainer<TArg>>> GetTypeDelegateContainers_Internal() => m_Dictionary;

        bool TryGetContainers(Type type, bool checkBaseTypes, out List<FieldListDelegateContainer<TArg>> commands)
        {
            if (!k_UnityEngineObjectType.IsAssignableFrom(type))
            {
                commands = null;
                return false;
            }

            if (!GetTypeDelegateContainers_Internal().TryGetValue(type, out commands))
                commands = new List<FieldListDelegateContainer<TArg>>();

            if (checkBaseTypes)
                AppendBaseTypeContainers(type.BaseType, commands);

            return commands.Count > 0;
        }

        /// <summary>
        /// Gets the <cref>FieldDelegate</cref>s to invoke after comparing the two SerializedObjects
        /// </summary>
        /// <param name="lhs">The first SerializedObject to compare</param>
        /// <param name="rhs">The second SerializedObject to compare</param>
        /// <param name="checkBaseTypes">If true, the method will also return delegates for base types of the targets of the SerializedObjects</param>
        /// <returns>A collection of <cref>FieldDelegates</cref> to invoke</returns>
        public IEnumerable<FieldDelegate<TArg>> GetDelegates(SerializedObject lhs, SerializedObject rhs, bool checkBaseTypes = true)
        {
            if (TryGetContainers(GetTargetType(lhs, rhs), checkBaseTypes, out List<FieldListDelegateContainer<TArg>> delegateContainers))
            {
                foreach (FieldListDelegateContainer<TArg> container in delegateContainers)
                {
                    FieldDelegate<TArg> act = container.GetDelegate(lhs, rhs);
                    if (act != null)
                    {
                        yield return act;
                    }
                }
            }
        }

        void AppendBaseTypeContainers(Type baseType, List<FieldListDelegateContainer<TArg>> commands)
        {
            if (baseType == null)
            {
                throw new ArgumentNullException(nameof(baseType));
            }

            if (GetTypeDelegateContainers_Internal().TryGetValue(baseType, out List<FieldListDelegateContainer<TArg>> baseCommands))
            {
                commands.AddRange(baseCommands);
            }

            Type baseBaseType = baseType.BaseType;
            if (baseType == k_UnityEngineObjectType || baseBaseType == k_UnityEngineObjectType || baseBaseType == null) // we can't look further than this
                return;

            AppendBaseTypeContainers(baseBaseType, commands);
        }

        /// <summary>
        /// Gets the <cref>Type</cref> of the targets of two serialized objects
        /// </summary>
        /// <param name="lhs">The first SerializedObject to check</param>
        /// <param name="rhs">The second SerializedObject to check</param>
        /// <returns><cref>Type</cref> of the targets of two serialized objects</returns>
        /// <exception cref="ArgumentNullException">Thrown if both arguments are null</exception>
        /// <exception cref="ArgumentException">Thrown if the targets of the two SerializedObjects are not the same type</exception>
        public static Type GetTargetType(SerializedObject lhs, SerializedObject rhs)
        {
            if (lhs == null && rhs == null)
                throw new ArgumentNullException(string.Empty, "At least one argument must not be null");

            if (lhs != null && rhs != null)
            {
                var type = lhs.targetObject.GetType();
                var rhsType = rhs.targetObject.GetType();
                if (type != rhsType)
                    throw new ArgumentException($"Type mismatch! '{type.Name} != {rhsType.Name}'");

                return type;
            }

            System.Object target = lhs != null ? lhs.targetObject : rhs.targetObject;
            return target.GetType();
        }
    }
}
