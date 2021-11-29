// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class TreeViewHelpers<T, TDefaultController> where TDefaultController : BaseTreeViewController, IDefaultTreeViewController<T>
    {
        internal static void SetRootItems(BaseTreeView treeView, IList<TreeViewItemData<T>> rootItems, Func<TDefaultController> createController)
        {
            if (treeView.viewController is TDefaultController defaultController)
            {
                defaultController.SetRootItems(rootItems);
            }
            else
            {
                var defaultTreeViewController = createController.Invoke();
                treeView.SetViewController(defaultTreeViewController);
                defaultTreeViewController.SetRootItems(rootItems);
            }
        }

        internal static IEnumerable<TreeViewItemData<T>> GetSelectedItems(BaseTreeView treeView)
        {
            if (treeView.viewController is TDefaultController defaultController)
            {
                foreach (var index in treeView.selectedIndices)
                {
                    yield return defaultController.GetTreeViewItemDataForIndex(index);
                }
            }
            else if (treeView.viewController?.GetType().GetGenericTypeDefinition() == typeof(TDefaultController).GetGenericTypeDefinition())
            {
                var objectType = treeView.viewController?.GetType().GetGenericArguments()[0];
                throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({objectType}) and is not recognized by the controller.");
            }
            else
            {
                throw new ArgumentException($"GetSelectedItems<T>() only works when using the default controller. Use your controller along with the selectedIndices enumerable instead.");
            }
        }

        internal static T GetItemDataForIndex(BaseTreeView treeView, int index)
        {
            // Support default case
            if (treeView.viewController is TDefaultController defaultController)
                return defaultController.GetDataForIndex(index);

            // Support user-defined controller case.
            var obj = treeView.viewController?.GetItemForIndex(index);
            var objectType = obj?.GetType();
            if (objectType == typeof(T))
                return (T)obj;

            if (objectType == null && treeView.viewController?.GetType().GetGenericTypeDefinition() == typeof(TDefaultController).GetGenericTypeDefinition())
            {
                objectType = treeView.viewController?.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({objectType}) and is not recognized by the controller.");
        }

        internal static T GetItemDataForId(BaseTreeView treeView, int id)
        {
            // Support default case
            if (treeView.viewController is TDefaultController defaultController)
                return defaultController.GetDataForId(id);

            // Support user-defined controller case.
            var obj = treeView.viewController?.GetItemForIndex(treeView.viewController.GetIndexForId(id));
            var objectType = obj?.GetType();
            if (objectType == typeof(T))
                return (T)obj;

            if (objectType == null && treeView.viewController?.GetType().GetGenericTypeDefinition() == typeof(TDefaultController).GetGenericTypeDefinition())
            {
                objectType = treeView.viewController?.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({objectType}) and is not recognized by the controller.");
        }

        internal static void AddItem(BaseTreeView treeView, TreeViewItemData<T> item, int parentId = -1, int childIndex = -1, bool rebuildTree = true)
        {
            if (treeView.viewController is TDefaultController defaultController)
            {
                defaultController.AddItem(item, parentId, childIndex, rebuildTree);

                if (rebuildTree)
                    treeView.RefreshItems();

                return;
            }

            Type dataSourceType = null;
            if (treeView.viewController?.GetType().GetGenericTypeDefinition() == typeof(TDefaultController).GetGenericTypeDefinition())
            {
                dataSourceType = treeView.viewController?.GetType().GetGenericArguments()[0];
            }

            throw new ArgumentException($"Type parameter ({typeof(T)}) differs from data source ({dataSourceType})and is not recognized by the controller.");
        }
    }
}
