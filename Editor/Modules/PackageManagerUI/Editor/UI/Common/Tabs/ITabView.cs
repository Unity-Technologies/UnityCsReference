// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ITabView<T> where T : ITabElement
    {
        event Action<T, T> onTabSwitched;

        IEnumerable<T> tabs { get; }

        string selectedTabId { get; }

        void AddTab(T tab);

        void RemoveTab(T tab);

        void ClearTabs();

        T GetTab(string id);

        A GetTab<A>(string id) where A : T;

        // return boolean indicating whether selection was successful or not; mostly for handling case of
        //  trying to select a serialized tab which is not present in the selected package
        bool SelectTab(string id);
    }
}
