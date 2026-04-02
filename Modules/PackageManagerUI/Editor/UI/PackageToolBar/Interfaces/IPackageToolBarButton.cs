// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageToolBarButton
    {
        void Refresh(IPackageVersion version);
        void Refresh(IEnumerable<IPackage> packages);
        event Action onActionTriggered;
        VisualElement element { get; }
    }
}
