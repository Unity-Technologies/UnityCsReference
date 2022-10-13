// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IVisualStateList : IEnumerable<VisualState>
    {
        long countLoaded { get; }
        long countTotal { get; }

        VisualState Get(string packageUniqueId);
        bool Contains(string packageUniqueId);
    }
}
