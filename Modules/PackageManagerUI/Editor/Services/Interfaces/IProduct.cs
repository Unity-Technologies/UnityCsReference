// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IProduct
    {
        long id { get; }
        bool isHidden { get; }

        string productUrl { get; }

        string description { get; }
        string latestReleaseNotes { get; }

        IReadOnlyList<string> labels { get; }
        IReadOnlyList<PackageImage> images { get; }

        DateTime? firstPublishedDate { get; }
        DateTime? purchasedTime { get; }
    }
}
