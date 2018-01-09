// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor
{
    internal class AdvancedDropdownSimpleDataSource : AdvancedDropdownDataSource
    {
        private string[] m_DisplayedOptions;
        public string[] DisplayedOptions {set { m_DisplayedOptions = value; }}

        private string m_Label;
        public string Label { set { m_Label = value; } }

        private static int m_SelectedIndex;
        public int SelectedIndex { set { m_SelectedIndex = value; } }

        protected override AdvancedDropdownItem FetchData()
        {
            var rootGroup = new AdvancedDropdownItem(m_Label, -1);

            for (int i = 0; i < m_DisplayedOptions.Length; i++)
            {
                var menuPath = m_DisplayedOptions[i];
                var paths = menuPath.Split('/');

                AdvancedDropdownItem parent = rootGroup;
                for (var j = 0; j < paths.Length; j++)
                {
                    var path = paths[j];
                    if (j == paths.Length - 1)
                    {
                        var element = new AdvancedDropdownItem(path, menuPath, i);
                        element.searchable = true;
                        element.SetParent(parent);
                        parent.AddChild(element);

                        if (i == m_SelectedIndex)
                        {
                            var tempParent = parent;
                            AdvancedDropdownItem searchedItem = element;
                            while (tempParent != null)
                            {
                                tempParent.selectedItem = tempParent.children.IndexOf(searchedItem);
                                searchedItem = tempParent;
                                tempParent = tempParent.parent;
                            }
                        }
                        continue;
                    }

                    var groupPathId = "";
                    for (int k = 0; k <= j; k++)
                        groupPathId += paths[k] + ".";

                    var group = parent.children.SingleOrDefault(c => c.id == groupPathId);
                    if (group == null)
                    {
                        group = new AdvancedDropdownItem(path, groupPathId, -1);
                        group.SetParent(parent);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            return rootGroup;
        }
    }
}
