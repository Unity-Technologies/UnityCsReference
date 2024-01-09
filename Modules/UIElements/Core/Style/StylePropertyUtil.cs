// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static partial class StylePropertyUtil
    {
        internal static Dictionary<string, StylePropertyId> propertyNameToStylePropertyId
        {
            [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => s_NameToId;
        }

        internal static Dictionary<StylePropertyId, string> stylePropertyIdToPropertyName
        {
            [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => s_IdToName;
        }

        internal static Dictionary<string, string> ussNameToCSharpName
        {
            [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => s_UssNameToCSharpName;
        }

        internal static Dictionary<string, string> cSharpNameToUssName
        {
            [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => s_CSharpNameToUssName;
        }

        public static bool IsAnimatable(StylePropertyId id)
        {
            return s_AnimatableProperties.Contains(id);
        }

        public static IEnumerable<StylePropertyId> AllPropertyIds()
        {
            return s_IdToName.Keys;
        }
    }
}
