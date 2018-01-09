// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class StatefulAdvancedDropdown
    {
        [NonSerialized]
        private AdvancedDropdownWindow s_Instance;

        public Action<string> onSelected { get; set; }
        public string Label { get; set; }
        public int SelectedIndex { get; set; }
        public string[] DisplayedOptions { get; set; }

        public void Show(Rect rect)
        {
            if (s_Instance != null)
            {
                s_Instance.Close();
                s_Instance = null;
            }

            s_Instance = ScriptableObject.CreateInstance<AdvancedDropdownWindow>();

            var dataSource = new AdvancedDropdownSimpleDataSource();
            dataSource.DisplayedOptions = DisplayedOptions;
            dataSource.SelectedIndex = SelectedIndex;
            dataSource.Label = Label;

            s_Instance.dataSource = dataSource;

            s_Instance.onSelected += w => onSelected(w.GetIdOfSelectedItem());

            s_Instance.Init(rect);
        }
    }
}
