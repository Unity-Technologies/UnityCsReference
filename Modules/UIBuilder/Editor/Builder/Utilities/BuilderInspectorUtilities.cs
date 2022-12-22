// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderInspectorUtilities
    {
        public static bool HasOverriddenField(VisualElement ve)
        {
            return ve.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) != null;
        }
    }
}
