// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Build.Profile.Internal
{
    /// <summary>
    /// Internal implementation of <see cref="AdvancedDropdownDataSource"/> for
    /// <see cref="AddSettingsDropdownWindow"/>. Generates dropdown items from
    /// a list of keys and display names.
    /// </summary>
    class AddSettingsDropdownDataSource : AdvancedDropdownDataSource
    {
        internal class AddSettingsDropdownItem : AdvancedDropdownItem
        {
            public int Key { get; private set; }

            public AddSettingsDropdownItem(int key, string displayName) : base(displayName)
            {
                this.Key = key;
            }
        }

        static readonly string k_AddSettings = L10n.Tr("Add Settings");
        IAddSettingsDataProvider m_Provider;

        public AddSettingsDropdownDataSource(IAddSettingsDataProvider settingsTypeProvider)
        {
            m_Provider = settingsTypeProvider;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem(k_AddSettings);
            foreach (var (key, name) in m_Provider.FetchSettings())
            {
                var item = new AddSettingsDropdownItem(key, name);
                root.AddChild(item);
            }
            return root;
        }
    }
}

