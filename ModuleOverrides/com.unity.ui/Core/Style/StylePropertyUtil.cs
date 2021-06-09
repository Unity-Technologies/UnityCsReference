// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class StylePropertyUtil
    {
        private static readonly HashSet<StylePropertyId> s_AnimatablePropertiesHash;

        static StylePropertyUtil()
        {
            s_AnimatablePropertiesHash = new HashSet<StylePropertyId>(s_AnimatableProperties);
        }

        public static bool IsAnimatable(StylePropertyId id)
        {
            return s_AnimatablePropertiesHash.Contains(id);
        }
    }
}
