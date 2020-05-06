using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class to restricts the value of an attribute.
    /// </summary>
    public abstract class UxmlTypeRestriction : IEquatable<UxmlTypeRestriction>
    {
        /// <summary>
        /// Indicates whether the current <see cref="UxmlTypeRestriction"/> object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>True if the otheer object is equal to this one.</returns>
        public virtual bool Equals(UxmlTypeRestriction other)
        {
            return this == other;
        }
    }

    /// <summary>
    /// Restricts the value of an attribute to match a regular expression.
    /// </summary>
    public class UxmlValueMatches : UxmlTypeRestriction
    {
        /// <summary>
        /// The regular expression that should be matched by the value.
        /// </summary>
        public string regex { get; set; }

        /// <summary>
        /// Indicates whether the current <see cref="UxmlValueMatches"/> object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>True if the otheer object is equal to this one.</returns>
        public override bool Equals(UxmlTypeRestriction other)
        {
            UxmlValueMatches otherVM = other as UxmlValueMatches;

            if (otherVM == null)
            {
                return false;
            }

            return regex == otherVM.regex;
        }
    }

    /// <summary>
    /// Restricts the value of an attribute to be within the specified bounds.
    /// </summary>
    public class UxmlValueBounds : UxmlTypeRestriction
    {
        /// <summary>
        /// The minimum value for the attribute.
        /// </summary>
        public string min { get; set; }
        /// <summary>
        /// The maximum value for the attribute.
        /// </summary>
        public string max { get; set; }
        /// <summary>
        /// True if the bounds exclude <see cref="min"/>.
        /// </summary>
        public bool excludeMin { get; set; }
        /// <summary>
        /// True if the bounds exclude <see cref="max"/>.
        /// </summary>
        public bool excludeMax { get; set; }

        /// <summary>
        /// Indicates whether the current <see cref="UxmlValueBounds"/> object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>True if the otheer object is equal to this one.</returns>
        public override bool Equals(UxmlTypeRestriction other)
        {
            UxmlValueBounds otherVB = other as UxmlValueBounds;

            if (otherVB == null)
            {
                return false;
            }

            return ((min == otherVB.min) && (max == otherVB.max) && (excludeMin == otherVB.excludeMin) && (excludeMax == otherVB.excludeMax));
        }
    }

    /// <summary>
    /// Restricts the value of an attribute to be taken from a list of values.
    /// </summary>
    public class UxmlEnumeration : UxmlTypeRestriction
    {
        List<string> m_Values = new List<string>();

        /// <summary>
        /// The list of values the attribute can take.
        /// </summary>
        public IEnumerable<string> values
        {
            get { return m_Values; }
            set { m_Values = value.ToList(); }
        }

        /// <summary>
        /// Indicates whether the current <see cref="UxmlEnumeration"/> object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>True if the otheer object is equal to this one.</returns>
        public override bool Equals(UxmlTypeRestriction other)
        {
            UxmlEnumeration otherE = other as UxmlEnumeration;

            if (otherE == null)
            {
                return false;
            }

            return values.All(otherE.values.Contains) && values.Count() == otherE.values.Count();
        }
    }
}
