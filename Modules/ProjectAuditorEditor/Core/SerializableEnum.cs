// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A wrapper to enable serialization of enums
    /// </summary>
    [Serializable]
    public struct SerializableEnum<T> : IEquatable<T>, IEquatable<SerializableEnum<T>>, ISerializationCallbackReceiver
            where T : struct, Enum
    {
        /// <summary>
        /// Creates a new SerializableEnum.
        /// </summary>
        public SerializableEnum(T value) : this()
        {
            m_Value = value;
        }

        [SerializeField]
        private string m_String;

        private T m_Value;

        /// <summary>
        /// The value of this enum.
        /// </summary>
        public T Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// Called prior to serialization.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_String = m_Value.ToString();
        }

        /// <summary>
        /// Called after deserialization.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (Enum.TryParse(m_String, out T value))
                m_Value = value;
        }

        /// <summary>
        /// Implicit operator.
        /// </summary>
        public static implicit operator T(SerializableEnum<T> obj)
        {
            return obj.m_Value;
        }

        /// <summary>
        /// Generic implicit operator.
        /// </summary>
        public static implicit operator SerializableEnum<T>(T value)
        {
            return new SerializableEnum<T>(value);
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        public bool Equals(T other)
        {
            return m_Value.Equals(other);
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        public bool Equals(SerializableEnum<T> other)
        {
            return m_Value.Equals(other.m_Value);
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SerializableEnum<T> other)
                return Equals(other);

            if (obj is T otherEnum)
                return Equals(otherEnum);

            return false;
        }

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }
    }

    [CustomPropertyDrawer(typeof(SerializableEnum<>), true)]
    internal class SerializableEnumPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            var stringProp = property.FindPropertyRelative("_stringValue");
            var enumType = fieldInfo.FieldType.GenericTypeArguments[0];

            Enum enumValue = default;

            try
            {
                enumValue = (Enum)Enum.Parse(enumType, stringProp.stringValue);
            }
            catch (ArgumentException) { }

            if (enumType.IsDefined(typeof(FlagsAttribute), false))
            {
                enumValue = EditorGUI.EnumFlagsField(position, label, enumValue);
            }
            else
            {
                enumValue = EditorGUI.EnumPopup(position, label, enumValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                stringProp.stringValue = enumValue.ToString();
            }
        }
    }

    internal static class SerializableEnumExtensions
    {
        public static T[] ToValuesArray<T>(this SerializableEnum<T>[] array)
            where T : struct, Enum
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return array.Select(v => v.Value).ToArray();
#pragma warning restore UA2001
        }

        public static List<T> ToValuesList<T>(this SerializableEnum<T>[] array)
            where T : struct, Enum
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return new List<T>(array.Select(v => v.Value));
#pragma warning restore UA2001
        }

        public static SerializableEnum<T>[] ToSerializableArray<T>(this IEnumerable<T> enumerable)
            where T : struct, Enum
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return enumerable.Select(v => new SerializableEnum<T>(v)).ToArray();
#pragma warning restore UA2001
        }
    }
}
