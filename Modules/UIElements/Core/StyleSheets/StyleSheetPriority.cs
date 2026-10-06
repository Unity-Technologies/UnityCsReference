// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

// Cascade tier of a style sheet, compared before selector specificity. Sorting rank is
// Builtin < UserTheme < Default, so regular sheets override themes, which override Unity content.
// Default doubles as "unset": imported sheets with Default inherit the importing entry's tier.
[VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
internal enum StyleSheetPriority
{
    Default,
    Builtin,
    UserTheme,
}
