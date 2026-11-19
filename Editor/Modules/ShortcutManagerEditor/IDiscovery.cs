// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShortcutManagement
{
    interface IDiscovery
    {
        IEnumerable<ShortcutEntry> GetAllShortcuts();
    }

    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    interface IShortcutEntryDiscoveryInfo
    {
        ShortcutEntry GetShortcutEntry();
        string GetFullMemberName();
        int GetLineNumber();
        string GetFilePath();
    }

    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    interface IDiscoveryShortcutProvider
    {
        IEnumerable<IShortcutEntryDiscoveryInfo> GetDefinedShortcuts();
    }
}
