// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal struct StyleSelectorPart
    {
        [SerializeField]
        string m_Value;

        public string value
        {
            get
            {
                return m_Value;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Value = value;
            }
        }

        [SerializeField]
        StyleSelectorType m_Type;

        public StyleSelectorType type
        {
            get
            {
                return m_Type;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Type = value;
            }
        }

        // Used at runtime as a cache
        internal object tempData;

        // Cached UniqueStyleString ID for ID/Class/Type selectors
        // Set during CalculateHashes(), used during style matching
        // Value of -1 means not yet cached
        [NonSerialized]
        internal int cachedUniqueStyleStringId;

        public override string ToString()
        {
            return string.Format("[StyleSelectorPart: value={0}, type={1}]", value, type);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StyleSelectorPart))
                return false;

            var other = (StyleSelectorPart)obj;
            // Only compare logical identity (type and value), ignore runtime cache fields
            return m_Type == other.m_Type && m_Value == other.m_Value;
        }

        public override int GetHashCode()
        {
            // Only hash logical identity, ignore runtime cache fields
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + m_Type.GetHashCode();
                hash = hash * 23 + (m_Value != null ? m_Value.GetHashCode() : 0);
                return hash;
            }
        }

        // Note that we still store class name as string for now instead of UniqueStyleString.
        public static StyleSelectorPart CreateClass(string className)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.Class,
                m_Value = className
            };
        }

        public static StyleSelectorPart CreatePseudoClass(string className)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.PseudoClass,
                m_Value = className
            };
        }

        public static StyleSelectorPart CreateId(string Id)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.ID,
                m_Value = Id
            };
        }

        public static StyleSelectorPart CreateType(Type t)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.Type,
                m_Value = t.Name
            };
        }

        public static StyleSelectorPart CreateType(string typeName)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.Type,
                m_Value = typeName
            };
        }

        public static StyleSelectorPart CreatePredicate(object predicate)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.Predicate,
                tempData = predicate
            };
        }

        public static StyleSelectorPart CreateWildCard()
        {
            return new StyleSelectorPart() {m_Value = "*", m_Type = StyleSelectorType.Wildcard};
        }
    }
}
