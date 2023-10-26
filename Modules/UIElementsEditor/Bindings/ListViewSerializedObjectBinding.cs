// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    class ListViewSerializedObjectBinding : BaseListViewSerializedObjectBinding
    {
        static ObjectPool<ListViewSerializedObjectBinding> s_Pool = new (() => new ListViewSerializedObjectBinding(), 32);

        ListView listView => boundElement as ListView;

        public ListViewSerializedObjectBinding()
        {
            m_DefaultBindItem = BindListViewItem;
        }

        public static BaseListViewSerializedObjectBinding GetFromPool()
        {
            return s_Pool.Get();
        }

        protected override void SetEditorViewController()
        {
            listView.SetViewController(new EditorListViewController());
        }

        protected override VisualElement MakeItem()
        {
            return new PropertyField();
        }

        protected override void SetDefaultCallbacks()
        {
            if (listView.makeItem == null)
            {
                listView.SetMakeItemWithoutNotify(m_DefaultMakeItem);
            }

            if (listView.bindItem == null)
            {
                listView.SetBindItemWithoutNotify(m_DefaultBindItem);
            }

            if (listView.unbindItem == null)
            {
                listView.unbindItem = m_DefaultUnbindItem;
            }
        }

        protected override bool HasDefaultBindItem()
        {
            return listView.bindItem == m_DefaultBindItem;
        }

        protected override void ResetCallbacks()
        {
            if (HasDefaultBindItem())
            {
                listView.SetBindItemWithoutNotify(null);
            }

            if (listView.makeItem == m_DefaultMakeItem)
            {
                listView.SetMakeItemWithoutNotify(null);
            }

            if (listView.unbindItem == m_DefaultUnbindItem)
            {
                listView.unbindItem = null;
            }
        }

        protected override void PoolRelease()
        {
            s_Pool.Release(this);
        }
    }
}
