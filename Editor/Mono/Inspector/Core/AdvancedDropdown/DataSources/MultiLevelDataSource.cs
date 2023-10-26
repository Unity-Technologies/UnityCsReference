// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.IMGUI.Controls
{
    internal class MultiLevelDataSource : AdvancedDropdownDataSource
    {
        private string[] m_DisplayedOptions;
        internal string[] displayedOptions
        {
            set { m_DisplayedOptions = value; }
        }

        private string m_Label = "";
        internal string label
        {
            set { m_Label = value; }
        }

        private static int m_SelectedIndex;
        internal int selectedIndex
        {
            set { m_SelectedIndex = value; }
        }

        internal MultiLevelDataSource()
        {
        }

        public MultiLevelDataSource(string[] displayOptions)
        {
            m_DisplayedOptions = displayOptions;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            var rootGroup = new AdvancedDropdownItem(m_Label);
            m_SearchableElements = new List<AdvancedDropdownItem>();

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
                        var element = new MultiLevelItem(path, menuPath);
                        element.elementIndex = i;
                        parent.AddChild(element);
                        m_SearchableElements.Add(element);

                        if (i == m_SelectedIndex)
                        {
                            selectedIDs.Add(element.id);
//                            var tempParent = parent;
//                            AdvancedDropdownItem searchedItem = element;
                            //TODO fix selecting
//                            while (tempParent != null)
//                            {
//                                state.SetSelectedIndex(tempParent, tempParent.children.IndexOf(searchedItem));
//                                searchedItem = tempParent;
//                                tempParent = tempParent.parent;
//                            }
                        }
                        continue;
                    }

                    var groupPathId = paths[0];
                    for (int k = 1; k <= j; k++)
                        groupPathId += "/" + paths[k];

                    var group = parent.children.SingleOrDefault(c => ((MultiLevelItem)c).stringId == groupPathId);
                    if (group == null)
                    {
                        group = new MultiLevelItem(path, groupPathId);
                        parent.AddChild(group);
                    }
                    parent = group;
                }
            }
            return rootGroup;
        }

        class MultiLevelItem : AdvancedDropdownItem
        {
            internal string stringId;
            public MultiLevelItem(string path, string menuPath) : base(path)
            {
                stringId = menuPath;
                id = menuPath.GetHashCode();
            }

            public override string ToString()
            {
                return stringId;
            }
        }
    }
}
