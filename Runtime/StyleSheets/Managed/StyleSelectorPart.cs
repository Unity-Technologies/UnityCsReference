// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.StyleSheets
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
            return string.Format("[StyleSelectorPart: value={0}, type={1}]", value, type);
        }

        public static StyleSelectorPart CreateClass(string className)
        {
            return new StyleSelectorPart()
            {
                m_Type = StyleSelectorType.Class,
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
