// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngineInternal
{
    public enum TypeInferenceRules
    {
        /// <summary>
        /// (typeof(T)) as T
        /// </summary>
        TypeReferencedByFirstArgument,

        /// <summary>
        /// (, typeof(T)) as T
        /// </summary>
        TypeReferencedBySecondArgument,

        /// <summary>
        /// (typeof(T)) as (T)
        /// </summary>
        ArrayOfTypeReferencedByFirstArgument,

        /// <summary>
        /// (T) as T
        /// </summary>
        TypeOfFirstArgument,
    }

    /// <summary>
    /// Adds a special type inference rule to a method.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class TypeInferenceRuleAttribute : Attribute
    {
        private readonly string _rule;

        public TypeInferenceRuleAttribute(TypeInferenceRules rule)
            : this(rule.ToString())
        {
        }

        public TypeInferenceRuleAttribute(string rule)
        {
            _rule = rule;
        }

        public override string ToString()
        {
            return _rule;
        }
    }
}
