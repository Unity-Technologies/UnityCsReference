// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum SearchTextParams
    {
        None = 0,

        TechnicalName = 1 << 0,
        Description = 1 << 1,
        DisplayName = 1 << 2,
        VersionRelated = 1 << 3,
        TagKeyword = 1 << 4,
        Categories = 1 << 5,
        Author = 1 << 6,
        SignatureOrgName = 1 << 7,

        All =
            TechnicalName |
            Description |
            DisplayName |
            VersionRelated |
            TagKeyword |
            Categories |
            Author |
            SignatureOrgName
    }
}
