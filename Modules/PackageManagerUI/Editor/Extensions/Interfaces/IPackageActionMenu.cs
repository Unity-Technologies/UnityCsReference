// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageActionMenu : IExtension
    {
        string text { get; set; }
        Texture2D icon { set; }
        string tooltip { get; set; }

        IPackageActionDropdownItem AddDropdownItem();
    }
}
