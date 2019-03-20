// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class Selection
    {
        [SerializeField] public List<ItemState> States;
        [NonSerialized] private IEnumerable<PackageVersion> Cache;
        [NonSerialized] private PackageCollection Collection;

        public IEnumerable<PackageInfo> Selected
        {
            get { return SelectedVersions.Select(v => v.Version); }
        }

        public IEnumerable<PackageVersion> SelectedVersions
        {
            get
            {
                if (Cache == null)
                    Cache = States.Where(s => s.Selected).Select(GetVersion)
                        .Where(p => p != null);    // Ignore packages that are not found

                return Cache;
            }
        }

        public event Action<IEnumerable<PackageVersion>> OnChanged = delegate {};

        public Selection()
        {
            States = new List<ItemState>();
        }

        private PackageVersion GetVersion(ItemState state)
        {
            return Collection.GetPackageVersion(state.PackageId);
        }

        public void ClearSelection()
        {
            SetSelection((PackageInfo)null);
        }

        internal void ClearSelectionInternal()
        {
            States.ForEach(s => {s.Selected = false;});
            ClearEmpty();
        }

        // Clear useless states
        private void ClearEmpty()
        {
            Cache = null;
            States.RemoveAll(s => !s.Expanded && !s.SeeAllVersions && !s.Selected);
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
        }

        public void AddSelection(PackageInfo version)
        {
            if (IsSelected(version))
                return;

            var state = State(version);
            state.Selected = true;
            OnChanged(SelectedVersions);
        }

        public void TriggerNewSelection()
        {
            OnChanged(SelectedVersions);
        }

         
        public void SetSelection(PackageInfo version)
        {
            if (IsSelected(version))
                return;

            ClearSelectionInternal();

            if (version == null)
                OnChanged(Enumerable.Empty<PackageVersion>());
            else
                AddSelection(version);
        }

        public void SetSelection(Package package)
        {
            var packgeVersion = package != null ? package.VersionToDisplay : null;
            SetSelection(packgeVersion);
        }

        public PackageVersion FirstSelected
        {
            get
            {
                return SelectedVersions.FirstOrDefault();
            }
        }

        public bool IsSelected(PackageInfo version)
        {
            return ByVersion(version).Any(p => p.Selected);
        }

        private IEnumerable<ItemState> ByVersion(PackageInfo version)
        {
            return version != null ? States.Where(v => v.PackageId == version.PackageId) : Enumerable.Empty<ItemState>();
        }

        // Don't return ItemState outside of this class otherwise changes to it won't be propagated by events
        private ItemState State(PackageInfo version)
        {
            var state = ByVersion(version).FirstOrDefault();
            if (state == null)
            {
                Cache = null;
                state = new ItemState(version.PackageId);
                States.Add(state);
            }

            return state;
        }

        public void SetSeeAllVersions(PackageInfo version, bool value)
        {
            State(version).SeeAllVersions = value;
        }

        public bool IsSeeAllVersions(PackageInfo version)
        {
            return State(version).SeeAllVersions;
        }

        public void SetExpanded(PackageInfo version, bool value)
        {
            State(version).Expanded = value;
        }

        public bool IsExpanded(PackageInfo version)
        {
            return State(version).Expanded;
        }
    }
}
