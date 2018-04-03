// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class UxmlTypeRestriction : IEquatable<UxmlTypeRestriction>
    {
        public virtual bool Equals(UxmlTypeRestriction other)
        {
            return this == other;
        }
    }

    public class UxmlValueMatches : UxmlTypeRestriction
    {
        public string regex;

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

    public class UxmlValueBounds : UxmlTypeRestriction
    {
        public string min;
        public string max;
        public bool excludeMin;
        public bool excludeMax;

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

    public class UxmlEnumeration : UxmlTypeRestriction
    {
        public List<string> values = new List<string>();

        public override bool Equals(UxmlTypeRestriction other)
        {
            UxmlEnumeration otherE = other as UxmlEnumeration;

            if (otherE == null)
            {
                return false;
            }

            return values.All(otherE.values.Contains) && values.Count == otherE.values.Count;
        }
    }
}
