// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    class MultiColumnListViewSerializedObjectBinding : BaseListViewSerializedObjectBinding
    {
        private bool m_HasDefaultBindItem;

        MultiColumnListView multiColumnListView => boundElement as MultiColumnListView;

        public static BaseListViewSerializedObjectBinding GetFromPool()
        {
            return new MultiColumnListViewSerializedObjectBinding();
        }

        protected override void SetEditorViewController()
        {
            var controller = new EditorMultiColumnListViewController(multiColumnListView.columns,
                multiColumnListView.sortColumnDescriptions, new List<SortColumnDescription>(multiColumnListView.sortedColumns));
            multiColumnListView.SetViewController(controller);
        }

        protected override VisualElement MakeItem()
        {
            return new PropertyField
            {
                label = string.Empty
            };
        }

        protected override void SetDefaultCallbacks()
        {
            foreach (var column in multiColumnListView.columns)
            {
                column.makeCell ??= m_DefaultMakeItem;

                if (column.bindCell == null)
                {
                    m_HasDefaultBindItem = true;

                    column.bindCell = (ve, i) =>
                    {
                        var item = m_DataList[i];
                        var itemProp = item as SerializedProperty;
                        var rowBindingPath = itemProp.propertyPath;
                        var bindingPath = string.IsNullOrEmpty(column.bindingPath) ?
                            rowBindingPath : string.Join(".", rowBindingPath, column.bindingPath);
                        BindListViewItem(ve, bindingPath);
                    };
                }

                column.unbindCell ??= m_DefaultUnbindItem;
            }
        }

        protected override bool HasDefaultBindItem()
        {
            return m_HasDefaultBindItem;
        }

        protected override void ResetCallbacks()
        {
            foreach(var column in multiColumnListView.columns)
            {
                if (HasDefaultBindItem())
                {
                    column.bindCell = null;
                }

                if (column.makeCell == m_DefaultMakeItem)
                {
                    column.makeCell = null;
                }

                if (column.unbindCell == m_DefaultUnbindItem)
                {
                    column.unbindCell = null;
                }
            }
        }

        protected override void PoolRelease()
        {
            m_HasDefaultBindItem = false;
        }
    }
}
