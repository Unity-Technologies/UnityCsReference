// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Information to store to survive domain reload
    /// </summary>
    [Serializable]
    internal class SelectionManager
    {
        [SerializeField] private Selection ListSelection = new Selection();
        [SerializeField] private Selection SearchSelection = new Selection();
        [SerializeField] private Selection BuiltInSelection = new Selection();

        private PackageCollection Collection { get; set; }

        public Selection Selection
        {
            get
            {
                if (Collection == null)
                    return ListSelection;

                if (Collection.Filter == PackageFilter.All)
                    return SearchSelection;
                if (Collection.Filter == PackageFilter.Local)
                    return ListSelection;
                return BuiltInSelection;
            }
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
            ListSelection.SetCollection(collection);
            SearchSelection.SetCollection(collection);
            BuiltInSelection.SetCollection(collection);
        }

        public void ClearAll()
        {
            ListSelection.ClearSelectionInternal();
            SearchSelection.ClearSelectionInternal();
            BuiltInSelection.ClearSelectionInternal();
        }

        public void SetSelection(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return;

            if (Collection == null)
                return;

            var package = Collection.GetPackageByName(packageName);
            if (package != null)
            {
                Collection.SetFilter(package.IsBuiltIn ? PackageFilter.Modules : PackageFilter.Local);
                Selection.SetSelection(package);
            }
        }
    }
}
