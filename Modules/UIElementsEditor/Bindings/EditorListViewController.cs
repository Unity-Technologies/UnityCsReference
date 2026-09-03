// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    internal class EditorListViewController : ListViewController
    {
        SerializedObjectListControllerImpl m_SerializedObjectListControllerImpl;

        public EditorListViewController()
        {
            m_SerializedObjectListControllerImpl = new SerializedObjectListControllerImpl(this, () => itemsSource as SerializedObjectList);
        }

        public override int GetItemsCount()
        {
            return m_SerializedObjectListControllerImpl.GetItemsCount();
        }

        internal override int GetItemsMinCount()
        {
            return m_SerializedObjectListControllerImpl.GetItemsMinCount();
        }

        internal override bool ValidateItemCountChange(int newItemCount)
        {
            var list = itemsSource as SerializedObjectList;
            if (list == null)
                return true;

            // Growing past this would exceed the maximum serialized object size, and the underlying
            // resize would be rejected; refuse the change up front and let the user know.
            var max = list.maxArraySize;
            if (newItemCount > max)
            {
                EditorUtility.DisplayDialog(
                    L10n.Tr("Invalid array size", null),
                    string.Format(L10n.Tr("The array cannot exceed {0:N0} elements. The value was not applied.", null), max),
                    L10n.Tr("OK", null));
                return false;
            }

            return true;
        }

        public override void AddItems(int itemCount)
        {
            m_SerializedObjectListControllerImpl.AddItems(itemCount);
        }

        internal override void RemoveItems(int itemCount)
        {
            m_SerializedObjectListControllerImpl.RemoveItems(itemCount);
        }

        public override void RemoveItems(List<int> indices)
        {
            m_SerializedObjectListControllerImpl.RemoveItems(indices);
        }

        public override void RemoveItem(int index)
        {
            m_SerializedObjectListControllerImpl.RemoveItem(index);
        }

        public override void ClearItems()
        {
            m_SerializedObjectListControllerImpl.ClearItems();
        }

        public override void Move(int srcIndex, int destIndex)
        {
            m_SerializedObjectListControllerImpl.Move(srcIndex, destIndex);
        }
    }
}
