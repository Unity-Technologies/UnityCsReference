// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageActionButton : IExtension
    {
        string text { get; set; }

        Texture2D icon { set; }
        string tooltip { get; set; }

        Action<PackageSelectionArgs> action { get; set; }

        IPackageActionDropdownItem AddDropdownItem();
    }
}
