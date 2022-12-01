// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Rendering;
using ATarget = System.AttributeTargets;

namespace UnityEditor.ShaderKeywordFilter
{
    /* These constraint attributes are used for limiting which parts of the settings data
     structure can affect the variant filtering on specific conditions (tags, graphics API, etc)*/
    [System.AttributeUsage(ATarget.Class | ATarget.Struct | ATarget.Field, AllowMultiple = true)]
    public class TagConstraintAttribute : System.Attribute
    {
        public TagConstraintAttribute(bool negate, params string[] tags)
        {
            m_Negate = negate;
            m_Tags = tags;
        }

        internal bool ShouldApplyRules(params string[] currentTags)
        {
            for (int i = 0, n = currentTags.Length - 1; i < n; i += 2) // Tags should be in name/value pairs. Ignore possible odd one.
            {
                for (int j = 0, m = m_Tags.Length - 1; j < m; j += 2)
                {
                    if (currentTags[i].Equals(m_Tags[j]) && currentTags[i + 1].Equals(m_Tags[j + 1]))
                        return !m_Negate;
                }
            }

            return m_Negate;
        }

        internal bool Negated
        {
            get => m_Negate;
        }

        internal string[] Tags
        {
            get => m_Tags;
        }

        bool m_Negate;
        string[] m_Tags;
    }

    public class ApplyRulesIfTagsEqualAttribute : TagConstraintAttribute
    {
        public ApplyRulesIfTagsEqualAttribute(params string[] tags)
            : base(false, tags)
        {
        }
    }

    public class ApplyRulesIfTagsNotEqualAttribute : TagConstraintAttribute
    {
        public ApplyRulesIfTagsNotEqualAttribute(params string[] tags)
            : base(true, tags)
        {
        }
    }

    [System.AttributeUsage(ATarget.Class | ATarget.Struct | ATarget.Field, AllowMultiple = false)]
    public class GraphicsAPIConstraintAttribute : System.Attribute
    {
        public GraphicsAPIConstraintAttribute(bool negate, params GraphicsDeviceType[] graphicsDeviceTypes)
        {
            m_Negate = negate;
            m_GraphicsDeviceTypes = graphicsDeviceTypes;
        }

        internal bool ShouldApplyRules(GraphicsDeviceType currentGraphicDeviceType)
        {
            foreach (var device in m_GraphicsDeviceTypes)
            {
                if (device == currentGraphicDeviceType)
                    return !m_Negate;
            }

            return m_Negate;
        }

        internal bool Negated
        {
            get => m_Negate;
        }

        internal GraphicsDeviceType[] GraphicsAPIs
        {
            get => m_GraphicsDeviceTypes;
        }

        bool m_Negate;
        GraphicsDeviceType[] m_GraphicsDeviceTypes;
    }

    public class ApplyRulesIfGraphicsAPIAttribute : GraphicsAPIConstraintAttribute
    {
        public ApplyRulesIfGraphicsAPIAttribute(params GraphicsDeviceType[] graphicsDeviceTypes)
            : base(false, graphicsDeviceTypes)
        {
        }
    }

    public class ApplyRulesIfNotGraphicsAPIAttribute : GraphicsAPIConstraintAttribute
    {
        public ApplyRulesIfNotGraphicsAPIAttribute(params GraphicsDeviceType[] graphicsDeviceTypes)
            : base(true, graphicsDeviceTypes)
        {
        }
    }
}
