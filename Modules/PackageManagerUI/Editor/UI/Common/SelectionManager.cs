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
        private Selection ListSelection;
        private Selection SearchSelection;
        private Selection BuiltInSelection;
        private Selection InDevSelection;

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
                if (Collection.Filter == PackageFilter.InDevelopment)
                    return InDevSelection;

                return BuiltInSelection;
            }
        }

        public SelectionManager()
        {
            if (ListSelection == null)
                ListSelection = new Selection();
            if (SearchSelection == null)
                SearchSelection = new Selection();
            if (BuiltInSelection == null)
                BuiltInSelection = new Selection();
            if (InDevSelection == null)
                InDevSelection = new Selection();
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
            ListSelection.SetCollection(collection);
            SearchSelection.SetCollection(collection);
            BuiltInSelection.SetCollection(collection);
            InDevSelection.SetCollection(collection);
        }

        public void ClearAll()
        {
            ListSelection.ClearSelectionInternal();
            SearchSelection.ClearSelectionInternal();
            BuiltInSelection.ClearSelectionInternal();
            InDevSelection.ClearSelectionInternal();
        }

        public void SetSelection(string packageName, object filter, bool forceSelect = false)
        {
            if (string.IsNullOrEmpty(packageName) && filter is PackageFilter)
            {
                Collection.SetFilter((PackageFilter)filter);
                return;
            }

            if (Collection == null)
                return;

            var package = Collection.GetPackageByName(packageName);
            if (package == null)
                package = Collection.GetPackageByDisplayName(packageName);

            if (package != null)
            {
                Collection.SetFilter((PackageFilter?)filter ?? (package.IsBuiltIn ? PackageFilter.Modules : PackageFilter.Local));
                Selection.SetSelection(package, forceSelect);
            }
        }
    }
}
