using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
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
            internal set
            {
                m_Type = value;
            }
        }

        // Used at runtime as a cache
        internal object tempData;

        public override string ToString()
        {
            return UnityString.Format("[StyleSelectorPart: value={0}, type={1}]", value, type);
        }

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
            return new StyleSelectorPart() {m_Type = StyleSelectorType.Wildcard};
        }
    }
}
