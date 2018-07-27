// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.ShortcutManagement
{
    interface IDiscovery
    {
        IEnumerable<ShortcutEntry> GetAllShortcuts();
    }

    interface IShortcutEntryDiscoveryInfo
    {
        ShortcutEntry GetShortcutEntry();
        string GetFullMemberName();
        int GetLineNumber();
        string GetFilePath();
    }

    interface IDiscoveryShortcutProvider
    {
        IEnumerable<IShortcutEntryDiscoveryInfo> GetDefinedShortcuts();
    }
}
