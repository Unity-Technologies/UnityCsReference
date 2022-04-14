// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class StylePropertyUtil
    {
        public static bool IsAnimatable(StylePropertyId id)
        {
            return s_AnimatableProperties.Contains(id);
        }
    }
}
